using System.Text.Json;
using System.Text.RegularExpressions;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IWafService
{
    Task<WafProfileDto> GetProfileAsync(int incidentLimit = 25);
    Task<WafProfileDto> UpdateProfileAsync(UpdateWafProfileRequest request, string? actorUserId);
    Task<WafEvaluationResult?> EvaluateRequestAsync(HttpContext context);
    Task RecordIncidentAsync(HttpContext context, WafEvaluationResult evaluation, string? actorUserId = null);
}

public sealed class WafService : IWafService
{
    private const string ConfigPrefix = "security:waf:";
    private const string EnabledKey = ConfigPrefix + "enabled";
    private const string ModeKey = ConfigPrefix + "mode";
    private const string MaxBodyKey = ConfigPrefix + "max_request_body_bytes";
    private const string SqliKey = ConfigPrefix + "block_sql_injection";
    private const string XssKey = ConfigPrefix + "block_xss";
    private const string TraversalKey = ConfigPrefix + "block_path_traversal";
    private const string BadBotsKey = ConfigPrefix + "block_bad_bots";
    private const string PathsKey = ConfigPrefix + "protected_paths";
    private const string TrustedIpsKey = ConfigPrefix + "trusted_ips";
    private const string BlockedAgentsKey = ConfigPrefix + "blocked_user_agents";

    private static readonly Regex SqlInjectionPattern = new(
        @"(?i)(union(\s+all)?\s+select|select\s+.+\s+from|drop\s+table|insert\s+into|delete\s+from|or\s+1\s*=\s*1|--|/\*|\bexec\b|\bxp_)",
        RegexOptions.Compiled);

    private static readonly Regex XssPattern = new(
        @"(?i)(<script|javascript:|onerror\s*=|onload\s*=|<iframe|<svg|alert\s*\()",
        RegexOptions.Compiled);

    private static readonly Regex TraversalPattern = new(
        @"(?i)(\.\./|\.\.\\|%2e%2e%2f|%2e%2e\\|%2e%2e%5c)",
        RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public WafService(ApplicationDbContext context, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
    }

    public async Task<WafProfileDto> GetProfileAsync(int incidentLimit = 25)
    {
        var configRows = await _context.SystemConfigs
            .Where(row => row.Key.StartsWith(ConfigPrefix))
            .ToListAsync();

        var profile = BuildProfile(configRows);
        var since = DateTime.UtcNow.AddHours(-24);
        profile.DetectedCount24Hours = await _context.AuditLogs.CountAsync(log => log.Action == "WAF_DETECTED" && log.CreatedAt >= since);
        profile.BlockedCount24Hours = await _context.AuditLogs.CountAsync(log => log.Action == "WAF_BLOCKED" && log.CreatedAt >= since);
        profile.UpdatedAt = configRows.Count == 0 ? null : configRows.Max(row => row.UpdatedAt);
        profile.GeneratedAt = DateTime.UtcNow;

        var incidents = await _context.AuditLogs
            .Where(log => log.Action == "WAF_DETECTED" || log.Action == "WAF_BLOCKED")
            .OrderByDescending(log => log.CreatedAt)
            .Take(Math.Clamp(incidentLimit, 1, 100))
            .ToListAsync();

        profile.RecentIncidents = incidents.Select(MapIncident).ToList();
        return profile;
    }

    public async Task<WafProfileDto> UpdateProfileAsync(UpdateWafProfileRequest request, string? actorUserId)
    {
        var now = DateTime.UtcNow;
        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EnabledKey] = request.Enabled.ToString(),
            [ModeKey] = string.Equals(request.Mode, "PREVENTION", StringComparison.OrdinalIgnoreCase) ? "PREVENTION" : "DETECTION",
            [MaxBodyKey] = Math.Clamp(request.MaxRequestBodyBytes, 1024, 10 * 1024 * 1024).ToString(),
            [SqliKey] = request.BlockSqlInjection.ToString(),
            [XssKey] = request.BlockXss.ToString(),
            [TraversalKey] = request.BlockPathTraversal.ToString(),
            [BadBotsKey] = request.BlockBadBots.ToString(),
            [PathsKey] = string.Join('\n', SanitizeList(request.ProtectedPaths, GetDefaultProtectedPaths())),
            [TrustedIpsKey] = string.Join('\n', SanitizeList(request.TrustedIps, Array.Empty<string>())),
            [BlockedAgentsKey] = string.Join('\n', SanitizeList(request.BlockedUserAgents, GetDefaultBlockedAgents())),
        };

        var existingRows = await _context.SystemConfigs
            .Where(row => updates.Keys.Contains(row.Key))
            .ToDictionaryAsync(row => row.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in updates)
        {
            if (existingRows.TryGetValue(pair.Key, out var row))
            {
                row.Value = pair.Value;
                row.UpdatedAt = now;
            }
            else
            {
                _context.SystemConfigs.Add(new SystemConfig
                {
                    Id = Guid.NewGuid().ToString("N")[..32],
                    Key = pair.Key,
                    Value = pair.Value,
                    UpdatedAt = now,
                    Description = "Security WAF runtime configuration",
                });
            }
        }

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            action: "SECURITY_WAF_UPDATED",
            entityType: "SECURITY_WAF",
            entityId: "default",
            userId: actorUserId,
            description: "Web application firewall policy updated.",
            status: "SUCCESS",
            newValues: updates);

        return await GetProfileAsync();
    }

    public async Task<WafEvaluationResult?> EvaluateRequestAsync(HttpContext context)
    {
        var configRows = await _context.SystemConfigs
            .AsNoTracking()
            .Where(row => row.Key.StartsWith(ConfigPrefix))
            .ToListAsync();
        var profile = BuildProfile(configRows);
        if (!profile.Enabled)
        {
            return null;
        }

        var requestPath = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value ?? string.Empty : string.Empty;
        var requestTarget = $"{requestPath}{query}";
        var normalizedTarget = SafeDecode(requestTarget);
        var clientIp = GetClientIp(context);

        if (profile.TrustedIps.Any(ip => string.Equals(ip, clientIp, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        if (profile.ProtectedPaths.Count > 0 &&
            !profile.ProtectedPaths.Any(path => requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var userAgent = context.Request.Headers.UserAgent.ToString();

        if (profile.BlockBadBots && profile.BlockedUserAgents.Any(agent => userAgent.Contains(agent, StringComparison.OrdinalIgnoreCase)))
        {
            return CreateEvaluation(profile, "BAD_BOT", "Blocked scanner or automation user-agent detected.", requestPath, clientIp, userAgent);
        }

        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > profile.MaxRequestBodyBytes)
        {
            return CreateEvaluation(profile, "BODY_SIZE", $"Request body exceeded {profile.MaxRequestBodyBytes} bytes.", requestPath, clientIp, userAgent);
        }

        if (profile.BlockPathTraversal && TraversalPattern.IsMatch(normalizedTarget))
        {
            return CreateEvaluation(profile, "PATH_TRAVERSAL", "Path traversal pattern detected in request target.", requestPath, clientIp, userAgent);
        }

        if (profile.BlockSqlInjection && SqlInjectionPattern.IsMatch(normalizedTarget))
        {
            return CreateEvaluation(profile, "SQL_INJECTION", "SQL injection pattern detected in request target.", requestPath, clientIp, userAgent);
        }

        if (profile.BlockXss && XssPattern.IsMatch(normalizedTarget))
        {
            return CreateEvaluation(profile, "XSS", "Cross-site scripting pattern detected in request target.", requestPath, clientIp, userAgent);
        }

        return null;
    }

    public async Task RecordIncidentAsync(HttpContext context, WafEvaluationResult evaluation, string? actorUserId = null)
    {
        var outcome = evaluation.ShouldBlock ? "BLOCKED" : "DETECTED";
        await _auditLoggingService.LogActionAsync(
            action: evaluation.ShouldBlock ? "WAF_BLOCKED" : "WAF_DETECTED",
            entityType: "HTTP_REQUEST",
            entityId: context.TraceIdentifier,
            userId: actorUserId,
            description: evaluation.Description,
            ipAddress: evaluation.IpAddress,
            userAgent: evaluation.UserAgent,
            status: "SUCCESS",
            newValues: new
            {
                evaluation.RuleCode,
                Outcome = outcome,
                evaluation.Mode,
                evaluation.RequestPath,
                Method = context.Request.Method,
                evaluation.IpAddress,
                evaluation.UserAgent,
            });
    }

    private static WafEvaluationResult CreateEvaluation(WafProfileDto profile, string ruleCode, string description, string requestPath, string? ipAddress, string? userAgent)
    {
        var preventionMode = string.Equals(profile.Mode, "PREVENTION", StringComparison.OrdinalIgnoreCase);
        return new WafEvaluationResult
        {
            RuleCode = ruleCode,
            Description = description,
            RequestPath = requestPath,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Mode = preventionMode ? "PREVENTION" : "DETECTION",
            ShouldBlock = preventionMode,
        };
    }

    private static WafProfileDto BuildProfile(IEnumerable<SystemConfig> rows)
    {
        var map = rows.ToDictionary(row => row.Key, row => row.Value, StringComparer.OrdinalIgnoreCase);
        return new WafProfileDto
        {
            Enabled = ReadBool(map, EnabledKey, false),
            Mode = ReadString(map, ModeKey, "DETECTION"),
            MaxRequestBodyBytes = ReadInt(map, MaxBodyKey, 262144),
            BlockSqlInjection = ReadBool(map, SqliKey, true),
            BlockXss = ReadBool(map, XssKey, true),
            BlockPathTraversal = ReadBool(map, TraversalKey, true),
            BlockBadBots = ReadBool(map, BadBotsKey, true),
            ProtectedPaths = ReadLines(map, PathsKey, GetDefaultProtectedPaths()),
            TrustedIps = ReadLines(map, TrustedIpsKey, Array.Empty<string>()),
            BlockedUserAgents = ReadLines(map, BlockedAgentsKey, GetDefaultBlockedAgents()),
        };
    }

    private static WafIncidentDto MapIncident(AuditLog log)
    {
        string ruleCode = string.Empty;
        string mode = "DETECTION";
        string outcome = log.Action == "WAF_BLOCKED" ? "BLOCKED" : "DETECTED";
        string requestPath = log.Description ?? "Unknown path";
        string method = "UNKNOWN";

        if (!string.IsNullOrWhiteSpace(log.NewValues))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<WafIncidentPayload>(log.NewValues, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

                if (payload is not null)
                {
                    ruleCode = payload.RuleCode ?? ruleCode;
                    mode = payload.Mode ?? mode;
                    outcome = payload.Outcome ?? outcome;
                    requestPath = payload.RequestPath ?? requestPath;
                    method = payload.Method ?? method;
                }
            }
            catch
            {
                // Keep a safe degraded incident record.
            }
        }

        return new WafIncidentDto
        {
            AuditLogId = log.Id,
            Action = log.Action,
            Outcome = outcome,
            Mode = mode,
            RuleCode = ruleCode,
            Method = method,
            RequestPath = requestPath,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Description = log.Description,
            DetectedAt = log.CreatedAt,
        };
    }

    private static List<string> SanitizeList(IEnumerable<string>? values, IEnumerable<string> fallback)
    {
        var source = values is null ? fallback : values;
        return source
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> map, string key, bool fallback)
        => map.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;

    private static int ReadInt(IReadOnlyDictionary<string, string> map, string key, int fallback)
        => map.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    private static string ReadString(IReadOnlyDictionary<string, string> map, string key, string fallback)
        => map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;

    private static List<string> ReadLines(IReadOnlyDictionary<string, string> map, string key, IEnumerable<string> fallback)
        => map.TryGetValue(key, out var value)
            ? value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : fallback.ToList();

    private static IReadOnlyList<string> GetDefaultProtectedPaths() =>
        new[] { "/api/auth", "/api/payments", "/api/migration", "/api/report", "/api/security" };

    private static IReadOnlyList<string> GetDefaultBlockedAgents() =>
        new[] { "sqlmap", "nikto", "acunetix", "nmap", "masscan", "dirbuster", "nessus" };

    private static string SafeDecode(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }

    private static string? GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private sealed class WafIncidentPayload
    {
        public string? RuleCode { get; set; }
        public string? Outcome { get; set; }
        public string? Mode { get; set; }
        public string? RequestPath { get; set; }
        public string? Method { get; set; }
    }
}

public sealed class WafEvaluationResult
{
    public string RuleCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Mode { get; set; } = "DETECTION";
    public bool ShouldBlock { get; set; }
}
