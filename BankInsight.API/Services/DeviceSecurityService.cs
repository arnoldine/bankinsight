using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BankInsight.API.Services;

public interface IDeviceSecurityService
{
    Task<List<SecurityDeviceDto>> GetDevicesAsync();
    Task<SecurityDeviceDto> RegisterDeviceAsync(RegisterTerminalDeviceRequest request, string? actorUserId);
    Task<SecurityDeviceDto> ExecuteDeviceActionAsync(string deviceId, DeviceActionRequest request, string? actorUserId);
    Task<DeviceScanResultDto> ScanOutdatedDevicesAsync(string? actorUserId);
    Task<List<TransactionIrregularityDto>> GetIrregularTransactionsAsync(int hours, int limit);
    Task<SecuritySummaryDto> GetSecuritySummaryAsync(int sinceHours);
}

public class DeviceSecurityService : IDeviceSecurityService
{
    private const string DeviceConfigPrefix = "device_registry:";
    private const string MinVersionConfigKey = "device_min_supported_version";
    private const string DefaultMinimumVersion = "2.0.0";

    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IConfiguration _configuration;

    public DeviceSecurityService(
        ApplicationDbContext context,
        IAuditLoggingService auditLoggingService,
        IConfiguration configuration)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _configuration = configuration;
    }

    public async Task<List<SecurityDeviceDto>> GetDevicesAsync()
    {
        var devices = await LoadDevicesAsync();
        var branches = await _context.Branches.ToDictionaryAsync(branch => branch.Id, branch => branch.Name);
        var staff = await _context.Staff.ToDictionaryAsync(user => user.Id, user => user.Name);

        return devices
            .OrderBy(device => device.Device.Name)
            .ThenBy(device => device.Device.Id)
            .Select(device => MapDevice(device.Device, branches, staff))
            .ToList();
    }

    public async Task<SecurityDeviceDto> RegisterDeviceAsync(RegisterTerminalDeviceRequest request, string? actorUserId)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            throw new InvalidOperationException("Device ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Device name is required.");
        }

        var deviceId = request.DeviceId.Trim();
        var existing = await FindDeviceConfigAsync(deviceId);
        var currentMinimumVersion = await GetMinimumSupportedVersionAsync();
        var minimumVersion = string.IsNullOrWhiteSpace(request.MinimumSupportedVersion)
            ? currentMinimumVersion
            : request.MinimumSupportedVersion.Trim();

        var now = DateTime.UtcNow;
        var device = existing?.Device ?? new ManagedSecurityDeviceRecord
        {
            Id = deviceId,
            CreatedAt = now,
            LastSeenAt = now,
        };

        var previousState = existing?.Device != null ? JsonSerializer.Serialize(existing.Device) : null;

        device.Name = request.Name.Trim();
        device.DeviceType = string.IsNullOrWhiteSpace(request.DeviceType) ? "CASH_TERMINAL" : request.DeviceType.Trim().ToUpperInvariant();
        device.BranchId = NormalizeNull(request.BranchId);
        device.AssignedStaffId = NormalizeNull(request.AssignedStaffId);
        device.SerialNumber = NormalizeNull(request.SerialNumber);
        device.IpAddress = NormalizeNull(request.IpAddress);
        device.SoftwareVersion = string.IsNullOrWhiteSpace(request.SoftwareVersion) ? "1.0.0" : request.SoftwareVersion.Trim();
        device.MinimumSupportedVersion = minimumVersion;
        device.Notes = NormalizeNull(request.Notes);
        device.LastSeenAt = now;
        device.UpdatedAt = now;
        device.SoftwareStatus = DetermineSoftwareStatus(device.SoftwareVersion, minimumVersion);
        device.Status = device.SoftwareStatus == "OUTDATED" ? "FLAGGED" : "ACTIVE";
        device.BlockReason = device.SoftwareStatus == "OUTDATED" ? "Device is running outdated software and needs review." : null;
        if (device.SoftwareStatus == "COMPLIANT")
        {
            device.LastPatchedAt ??= now;
        }

        await SaveDeviceAsync(device, existing?.ConfigRow);

        await _auditLoggingService.LogActionAsync(
            action: existing == null ? "SECURITY_DEVICE_REGISTERED" : "SECURITY_DEVICE_UPDATED",
            entityType: "SECURITY_DEVICE",
            entityId: device.Id,
            userId: actorUserId,
            description: $"Security device {(existing == null ? "registered" : "updated")}: {device.Name}",
            status: "SUCCESS",
            oldValues: previousState,
            newValues: device);

        return await GetDeviceByIdAsync(device.Id)
            ?? throw new InvalidOperationException("Registered device could not be reloaded.");
    }

    public async Task<SecurityDeviceDto> ExecuteDeviceActionAsync(string deviceId, DeviceActionRequest request, string? actorUserId)
    {
        var existing = await FindDeviceConfigAsync(deviceId);
        if (existing == null)
        {
            throw new InvalidOperationException("Device not found.");
        }

        var device = existing.Device;
        var previousState = JsonSerializer.Serialize(device);
        var action = (request.Action ?? string.Empty).Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.MinimumSupportedVersion))
        {
            device.MinimumSupportedVersion = request.MinimumSupportedVersion.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.SoftwareVersion))
        {
            device.SoftwareVersion = request.SoftwareVersion.Trim();
            device.LastPatchedAt = now;
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            device.Notes = request.Notes.Trim();
        }

        switch (action)
        {
            case "BLOCK":
                device.Status = "BLOCKED";
                device.BlockReason = NormalizeNull(request.Reason) ?? "Blocked by operations.";
                device.LastBlockedAt = now;
                break;
            case "UNBLOCK":
                device.Status = DetermineSoftwareStatus(device.SoftwareVersion, device.MinimumSupportedVersion) == "OUTDATED"
                    ? "FLAGGED"
                    : "ACTIVE";
                device.BlockReason = NormalizeNull(request.Reason);
                break;
            case "ISOLATE":
                device.Status = "ISOLATED";
                device.BlockReason = NormalizeNull(request.Reason) ?? "Isolated pending software remediation.";
                device.LastBlockedAt = now;
                break;
            case "FLAG":
                device.Status = "FLAGGED";
                device.BlockReason = NormalizeNull(request.Reason) ?? "Flagged for review.";
                break;
            case "PATCH":
                device.SoftwareStatus = DetermineSoftwareStatus(device.SoftwareVersion, device.MinimumSupportedVersion);
                device.Status = device.SoftwareStatus == "OUTDATED" ? "FLAGGED" : "ACTIVE";
                if (device.SoftwareStatus == "COMPLIANT")
                {
                    device.BlockReason = NormalizeNull(request.Reason);
                }
                break;
            default:
                throw new InvalidOperationException("Unsupported device action.");
        }

        device.SoftwareStatus = DetermineSoftwareStatus(device.SoftwareVersion, device.MinimumSupportedVersion);
        if (device.SoftwareStatus == "OUTDATED" && action == "UNBLOCK")
        {
            device.Status = "FLAGGED";
        }

        device.UpdatedAt = now;
        device.LastSeenAt = now;
        await SaveDeviceAsync(device, existing.ConfigRow);

        await _auditLoggingService.LogActionAsync(
            action: $"SECURITY_DEVICE_{action}",
            entityType: "SECURITY_DEVICE",
            entityId: device.Id,
            userId: actorUserId,
            description: $"Security device action {action} executed for {device.Name}",
            status: "SUCCESS",
            oldValues: previousState,
            newValues: device);

        return await GetDeviceByIdAsync(device.Id)
            ?? throw new InvalidOperationException("Updated device could not be reloaded.");
    }

    public async Task<DeviceScanResultDto> ScanOutdatedDevicesAsync(string? actorUserId)
    {
        var minimumVersion = await GetMinimumSupportedVersionAsync();
        var devices = await LoadDevicesAsync();
        var flaggedCount = 0;
        var outdatedCount = 0;

        foreach (var entry in devices)
        {
            var device = entry.Device;
            var previousState = JsonSerializer.Serialize(device);
            device.MinimumSupportedVersion = minimumVersion;
            device.SoftwareStatus = DetermineSoftwareStatus(device.SoftwareVersion, minimumVersion);

            if (device.SoftwareStatus == "OUTDATED")
            {
                outdatedCount++;
                if (device.Status == "ACTIVE" || device.Status == "PENDING_SETUP")
                {
                    device.Status = "FLAGGED";
                    device.BlockReason = "Flagged by outdated software scan.";
                    flaggedCount++;
                }
            }
            else if (device.Status == "FLAGGED" && string.Equals(device.BlockReason, "Flagged by outdated software scan.", StringComparison.OrdinalIgnoreCase))
            {
                device.Status = "ACTIVE";
                device.BlockReason = null;
            }

            device.UpdatedAt = DateTime.UtcNow;
            await SaveDeviceAsync(device, entry.ConfigRow);

            if (!string.Equals(previousState, JsonSerializer.Serialize(device), StringComparison.Ordinal))
            {
                await _auditLoggingService.LogActionAsync(
                    action: "SECURITY_DEVICE_OUTDATED_SCAN",
                    entityType: "SECURITY_DEVICE",
                    entityId: device.Id,
                    userId: actorUserId,
                    description: $"Outdated software scan evaluated {device.Name}",
                    status: device.SoftwareStatus == "OUTDATED" ? "ALERT" : "SUCCESS",
                    oldValues: previousState,
                    newValues: device);
            }
        }

        return new DeviceScanResultDto
        {
            MinimumSupportedVersion = minimumVersion,
            ScannedCount = devices.Count,
            OutdatedCount = outdatedCount,
            FlaggedCount = flaggedCount,
            Devices = await GetDevicesAsync(),
            ScannedAt = DateTime.UtcNow,
        };
    }

    public async Task<List<TransactionIrregularityDto>> GetIrregularTransactionsAsync(int hours, int limit)
    {
        var safeHours = Math.Clamp(hours, 1, 24 * 30);
        var safeLimit = Math.Clamp(limit, 1, 500);
        var since = DateTime.UtcNow.AddHours(-safeHours);
        var threshold = GetLargeTransactionThreshold();

        var transactions = await _context.Transactions
            .Include(transaction => transaction.Account)
                .ThenInclude(account => account.Customer)
            .Include(transaction => transaction.Teller)
            .Where(transaction => transaction.Date >= since)
            .OrderByDescending(transaction => transaction.Date)
            .ToListAsync();

        var irregularities = new List<TransactionIrregularityDto>();

        foreach (var transaction in transactions)
        {
            var flags = new List<string>();
            var riskScore = 0;
            var txDate = transaction.Date;

            if (transaction.Amount >= threshold)
            {
                flags.Add($"Amount exceeds configured threshold of {threshold:N2}.");
                riskScore += 45;
            }

            if (txDate.Hour < 6 || txDate.Hour >= 20)
            {
                flags.Add("Transaction posted outside standard banking hours.");
                riskScore += 20;
            }

            var fifteenMinuteWindow = transactions.Count(candidate =>
                candidate.AccountId == transaction.AccountId &&
                Math.Abs((candidate.Date - txDate).TotalMinutes) <= 15);
            if (fifteenMinuteWindow >= 3)
            {
                flags.Add("Rapid repeat activity detected on the same account within 15 minutes.");
                riskScore += 25;
            }

            var sameValueDailyCount = transactions.Count(candidate =>
                candidate.AccountId == transaction.AccountId &&
                candidate.Amount == transaction.Amount &&
                Math.Abs((candidate.Date - txDate).TotalHours) <= 24);
            if (sameValueDailyCount >= 3)
            {
                flags.Add("Repeated same-value transactions detected within 24 hours.");
                riskScore += 20;
            }

            if (string.Equals(transaction.Type, "WITHDRAWAL", StringComparison.OrdinalIgnoreCase) && transaction.Amount >= threshold * 0.5m)
            {
                flags.Add("Large cash withdrawal requires teller review.");
                riskScore += 15;
            }

            if (flags.Count == 0)
            {
                continue;
            }

            irregularities.Add(new TransactionIrregularityDto
            {
                Id = $"IRR-{transaction.Id}",
                TransactionId = transaction.Id,
                Reference = transaction.Reference,
                AccountId = transaction.AccountId,
                CustomerId = transaction.Account?.CustomerId,
                CustomerName = transaction.Account?.Customer?.Name,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Severity = GetSeverity(riskScore),
                RiskScore = riskScore,
                Summary = flags[0],
                Flags = flags,
                TellerId = transaction.TellerId,
                TellerName = transaction.Teller?.Name,
                Status = transaction.Status,
                TransactionDate = txDate,
                DetectedAt = DateTime.UtcNow,
            });
        }

        return irregularities
            .OrderByDescending(item => item.RiskScore)
            .ThenByDescending(item => item.TransactionDate)
            .Take(safeLimit)
            .ToList();
    }

    public async Task<SecuritySummaryDto> GetSecuritySummaryAsync(int sinceHours)
    {
        var safeHours = Math.Clamp(sinceHours, 1, 24 * 30);
        var since = DateTime.UtcNow.AddHours(-safeHours);
        var devices = await LoadDevicesAsync();
        var irregular = await GetIrregularTransactionsAsync(safeHours, 200);

        var failedLoginCount = await _context.LoginAttempts.CountAsync(attempt => !attempt.Success && attempt.AttemptedAt >= since);
        var securityAlertCount = await _context.AuditLogs.CountAsync(log => log.Action.StartsWith("SECURITY_ALERT_") && log.CreatedAt >= since);
        var largeTransactionAlertCount = await _context.AuditLogs.CountAsync(log => log.Action == "SECURITY_ALERT_LARGE_TRANSACTION" && log.CreatedAt >= since);
        var minimumVersion = await GetMinimumSupportedVersionAsync();
        var deviceList = devices.Select(entry => entry.Device).ToList();

        return new SecuritySummaryDto
        {
            WindowHours = safeHours,
            FailedLoginCount = failedLoginCount,
            SecurityAlertCount = securityAlertCount,
            LargeTransactionAlertCount = largeTransactionAlertCount,
            RegisteredDevices = deviceList.Count,
            ActiveDevices = deviceList.Count(device => device.Status == "ACTIVE"),
            BlockedDevices = deviceList.Count(device => device.Status == "BLOCKED"),
            IsolatedDevices = deviceList.Count(device => device.Status == "ISOLATED"),
            OutdatedDevices = deviceList.Count(device => DetermineSoftwareStatus(device.SoftwareVersion, minimumVersion) == "OUTDATED"),
            IrregularActivityCount = irregular.Count,
            MinimumSupportedVersion = minimumVersion,
            GeneratedAt = DateTime.UtcNow,
        };
    }

    private async Task<List<DeviceConfigEnvelope>> LoadDevicesAsync()
    {
        var configs = await _context.SystemConfigs
            .Where(config => config.Key.StartsWith(DeviceConfigPrefix))
            .OrderBy(config => config.Key)
            .ToListAsync();

        var minimumVersion = await GetMinimumSupportedVersionAsync();
        var devices = new List<DeviceConfigEnvelope>();
        foreach (var config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.Value))
            {
                continue;
            }

            var device = JsonSerializer.Deserialize<ManagedSecurityDeviceRecord>(config.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (device == null)
            {
                continue;
            }

            device.Id = string.IsNullOrWhiteSpace(device.Id) ? config.Key.Substring(DeviceConfigPrefix.Length) : device.Id;
            device.MinimumSupportedVersion = string.IsNullOrWhiteSpace(device.MinimumSupportedVersion) ? minimumVersion : device.MinimumSupportedVersion;
            device.SoftwareStatus = DetermineSoftwareStatus(device.SoftwareVersion, device.MinimumSupportedVersion);
            devices.Add(new DeviceConfigEnvelope(config, device));
        }

        return devices;
    }

    private async Task<DeviceConfigEnvelope?> FindDeviceConfigAsync(string deviceId)
    {
        var config = await _context.SystemConfigs.FirstOrDefaultAsync(row => row.Key == DeviceConfigPrefix + deviceId);
        if (config == null || string.IsNullOrWhiteSpace(config.Value))
        {
            return null;
        }

        var minimumVersion = await GetMinimumSupportedVersionAsync();
        var device = JsonSerializer.Deserialize<ManagedSecurityDeviceRecord>(config.Value, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (device == null)
        {
            return null;
        }

        device.Id = string.IsNullOrWhiteSpace(device.Id) ? deviceId : device.Id;
        device.MinimumSupportedVersion = string.IsNullOrWhiteSpace(device.MinimumSupportedVersion) ? minimumVersion : device.MinimumSupportedVersion;
        device.SoftwareStatus = DetermineSoftwareStatus(device.SoftwareVersion, device.MinimumSupportedVersion);
        return new DeviceConfigEnvelope(config, device);
    }

    private async Task SaveDeviceAsync(ManagedSecurityDeviceRecord device, SystemConfig? existingRow = null)
    {
        var row = existingRow ?? await _context.SystemConfigs.FirstOrDefaultAsync(config => config.Key == DeviceConfigPrefix + device.Id);
        if (row == null)
        {
            row = new SystemConfig
            {
                Id = ("CFG_DEV_" + Guid.NewGuid().ToString("N"))[..20],
                Key = DeviceConfigPrefix + device.Id,
                Description = $"Security device registry entry for {device.Name}",
            };
            _context.SystemConfigs.Add(row);
        }

        row.Value = JsonSerializer.Serialize(device);
        row.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task<SecurityDeviceDto?> GetDeviceByIdAsync(string deviceId)
    {
        var existing = await FindDeviceConfigAsync(deviceId);
        if (existing == null)
        {
            return null;
        }

        var branches = await _context.Branches.ToDictionaryAsync(branch => branch.Id, branch => branch.Name);
        var staff = await _context.Staff.ToDictionaryAsync(user => user.Id, user => user.Name);
        return MapDevice(existing.Device, branches, staff);
    }

    private decimal GetLargeTransactionThreshold()
    {
        var fromConfig = _configuration.GetValue<decimal>("Security:SuspiciousActivity:LargeTransactionThreshold");
        return fromConfig > 0 ? fromConfig : 100000m;
    }

    private async Task<string> GetMinimumSupportedVersionAsync()
    {
        var configured = await _context.SystemConfigs
            .Where(config => config.Key == MinVersionConfigKey)
            .Select(config => config.Value)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(configured) ? DefaultMinimumVersion : configured.Trim();
    }

    private static SecurityDeviceDto MapDevice(ManagedSecurityDeviceRecord device, IReadOnlyDictionary<string, string> branches, IReadOnlyDictionary<string, string> staff)
    {
        branches.TryGetValue(device.BranchId ?? string.Empty, out var branchName);
        staff.TryGetValue(device.AssignedStaffId ?? string.Empty, out var staffName);

        return new SecurityDeviceDto
        {
            Id = device.Id,
            Name = device.Name,
            DeviceType = device.DeviceType,
            Status = device.Status,
            SoftwareStatus = DetermineSoftwareStatus(device.SoftwareVersion, device.MinimumSupportedVersion),
            SoftwareVersion = device.SoftwareVersion,
            MinimumSupportedVersion = device.MinimumSupportedVersion,
            BranchId = device.BranchId,
            BranchName = branchName,
            AssignedStaffId = device.AssignedStaffId,
            AssignedStaffName = staffName,
            SerialNumber = device.SerialNumber,
            IpAddress = device.IpAddress,
            Notes = device.Notes,
            BlockReason = device.BlockReason,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt,
            LastSeenAt = device.LastSeenAt,
            LastPatchedAt = device.LastPatchedAt,
            LastBlockedAt = device.LastBlockedAt,
        };
    }

    private static string DetermineSoftwareStatus(string? version, string? minimumVersion)
    {
        var current = string.IsNullOrWhiteSpace(version) ? "0.0.0" : version.Trim();
        var minimum = string.IsNullOrWhiteSpace(minimumVersion) ? DefaultMinimumVersion : minimumVersion.Trim();
        return CompareVersions(current, minimum) < 0 ? "OUTDATED" : "COMPLIANT";
    }

    private static int CompareVersions(string left, string right)
    {
        var leftParts = left.Split('.', '-', '_');
        var rightParts = right.Split('.', '-', '_');
        var max = Math.Max(leftParts.Length, rightParts.Length);

        for (var index = 0; index < max; index++)
        {
            var leftValue = index < leftParts.Length && int.TryParse(leftParts[index], out var parsedLeft) ? parsedLeft : 0;
            var rightValue = index < rightParts.Length && int.TryParse(rightParts[index], out var parsedRight) ? parsedRight : 0;

            if (leftValue != rightValue)
            {
                return leftValue.CompareTo(rightValue);
            }
        }

        return 0;
    }

    private static string GetSeverity(int riskScore)
    {
        if (riskScore >= 70)
        {
            return "HIGH";
        }

        if (riskScore >= 40)
        {
            return "MEDIUM";
        }

        return "LOW";
    }

    private static string? NormalizeNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class ManagedSecurityDeviceRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "CASH_TERMINAL";
        public string Status { get; set; } = "ACTIVE";
        public string SoftwareStatus { get; set; } = "COMPLIANT";
        public string SoftwareVersion { get; set; } = "1.0.0";
        public string MinimumSupportedVersion { get; set; } = DefaultMinimumVersion;
        public string? BranchId { get; set; }
        public string? AssignedStaffId { get; set; }
        public string? SerialNumber { get; set; }
        public string? IpAddress { get; set; }
        public string? Notes { get; set; }
        public string? BlockReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeenAt { get; set; }
        public DateTime? LastPatchedAt { get; set; }
        public DateTime? LastBlockedAt { get; set; }
    }

    private sealed record DeviceConfigEnvelope(SystemConfig ConfigRow, ManagedSecurityDeviceRecord Device);
}