using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace BankInsight.API.Services;

public class AuthService
{
    private const string MfaChallengePrefix = "mfa_challenge_";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly ISessionService _sessionService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly ISuspiciousActivityService _suspiciousActivityService;
    private readonly IPrivilegeLeaseService _privilegeLeaseService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IEmailAlertService _emailAlertService;
    private readonly IHostEnvironment _hostEnvironment;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration config,
        ISessionService sessionService,
        ILoginAttemptService loginAttemptService,
        ISuspiciousActivityService suspiciousActivityService,
        IPrivilegeLeaseService privilegeLeaseService,
        IAuditLoggingService auditLoggingService,
        IEmailAlertService emailAlertService,
        IHostEnvironment hostEnvironment)
    {
        _context = context;
        _config = config;
        _sessionService = sessionService;
        _loginAttemptService = loginAttemptService;
        _suspiciousActivityService = suspiciousActivityService;
        _privilegeLeaseService = privilegeLeaseService;
        _auditLoggingService = auditLoggingService;
        _emailAlertService = emailAlertService;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string ipAddress, string? userAgent)
    {
        var user = await LoadUserAsync(request.Email);
        if (user == null)
        {
            await _loginAttemptService.LogAttemptAsync(request.Email, false, "User not found", ipAddress, userAgent);
            await _suspiciousActivityService.HandleFailedLoginAsync(request.Email, ipAddress, "User not found");
            return null;
        }

        var isLocked = await _loginAttemptService.IsAccountLockedAsync(request.Email);
        if (isLocked)
        {
            await _loginAttemptService.LogAttemptAsync(
                request.Email,
                false,
                "Account locked due to multiple failed attempts",
                ipAddress,
                userAgent,
                user.Id);
            await _suspiciousActivityService.HandleFailedLoginAsync(request.Email, ipAddress, "Account locked");
            return null;
        }

        var passwordValid = ValidatePassword(user, request.Password, out var needsRehash);
        if (!passwordValid)
        {
            await _loginAttemptService.LogAttemptAsync(request.Email, false, "Invalid password", ipAddress, userAgent, user.Id);
            await _suspiciousActivityService.HandleFailedLoginAsync(request.Email, ipAddress, "Invalid password");
            return null;
        }

        if (needsRehash)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            await _context.SaveChangesAsync();
        }

        var permissionBundle = await BuildPermissionBundleAsync(user);
        var requiresMfa = RequiresMfa(user, permissionBundle.AllPermissions);

        await _loginAttemptService.LogAttemptAsync(request.Email, true, null, ipAddress, userAgent, user.Id);

        if (requiresMfa)
        {
            var challenge = await CreateMfaChallengeAsync(user, ipAddress, userAgent);
            await _auditLoggingService.LogActionAsync(
                "MFA_CHALLENGE_CREATED",
                "Staff",
                user.Id,
                user.Id,
                "Login requires MFA verification before a session can be issued.",
                ipAddress,
                userAgent,
                "PENDING",
                newValues: new
                {
                    challenge.DeliveryChannel,
                    challenge.DeliveryHint,
                    challenge.ExpiresAt
                });

            return new LoginResponse
            {
                MfaRequired = true,
                MfaToken = challenge.Token,
                DeliveryChannel = challenge.DeliveryChannel,
                DeliveryHint = challenge.DeliveryHint,
                DeliveryStatus = challenge.DeliveryStatus,
                DeliveryMessage = challenge.DeliveryMessage,
                MfaExpiresAtUtc = challenge.ExpiresAt,
                AllowedFactors = ["otp"],
                DebugCode = _hostEnvironment.IsDevelopment() ? challenge.DebugCode : null
            };
        }

        return await IssueAuthenticatedSessionAsync(user, permissionBundle, ipAddress, userAgent);
    }

    public async Task<LoginResponse?> VerifyMfaAsync(VerifyMfaRequest request, string ipAddress, string? userAgent)
    {
        var challengeRow = await _context.SystemConfigs.FirstOrDefaultAsync(config => config.Key == GetMfaChallengeKey(request.MfaToken));
        if (challengeRow == null)
        {
            return null;
        }

        var challenge = DeserializeChallenge(challengeRow.Value);
        if (challenge == null || challenge.ExpiresAt <= DateTime.UtcNow)
        {
            challengeRow.Value = string.Empty;
            challengeRow.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return null;
        }

        if (challenge.Attempts >= 5)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Code, challenge.CodeHash))
        {
            challenge.Attempts += 1;
            challengeRow.Value = JsonSerializer.Serialize(challenge);
            challengeRow.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return null;
        }

        var user = await LoadUserByIdAsync(challenge.UserId);
        if (user == null)
        {
            return null;
        }

        _context.SystemConfigs.Remove(challengeRow);
        await _context.SaveChangesAsync();

        var permissionBundle = await BuildPermissionBundleAsync(user);
        await _auditLoggingService.LogActionAsync(
            "MFA_CHALLENGE_COMPLETED",
            "Staff",
            user.Id,
            user.Id,
            "Login MFA verification completed successfully.",
            ipAddress,
            userAgent,
            "SUCCESS");

        return await IssueAuthenticatedSessionAsync(user, permissionBundle, ipAddress, userAgent);
    }

    private async Task<Staff?> LoadUserAsync(string email)
    {
        return await _context.Staff
            .Include(s => s.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(s => s.Email == email);
    }

    private async Task<Staff?> LoadUserByIdAsync(string userId)
    {
        return await _context.Staff
            .Include(s => s.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(s => s.Id == userId);
    }

    private static bool ValidatePassword(Staff user, string password, out bool needsRehash)
    {
        needsRehash = false;

        if (!string.IsNullOrWhiteSpace(user.PasswordHash) &&
            (user.PasswordHash.StartsWith("$2a$") || user.PasswordHash.StartsWith("$2b$") || user.PasswordHash.StartsWith("$2y$")))
        {
            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        var valid = user.PasswordHash == password;
        needsRehash = valid;
        return valid;
    }

    private async Task<(string RoleName, string RoleId, string[] AllPermissions, string[] LeasedPermissions)> BuildPermissionBundleAsync(Staff user)
    {
        var role = user.UserRoles.FirstOrDefault()?.Role;
        var directPermissions = user.UserRoles
            .SelectMany(ur => ur.Role?.RolePermissions ?? Enumerable.Empty<RolePermission>())
            .Select(rp => rp.Permission?.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var leasedPermissions = (await _privilegeLeaseService.GetActiveLeasedPermissionsAsync(user.Id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return (
            role?.Name ?? "Unknown",
            user.UserRoles.FirstOrDefault()?.RoleId ?? string.Empty,
            directPermissions.Concat(leasedPermissions).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            leasedPermissions
        );
    }

    private bool RequiresMfa(Staff user, IEnumerable<string> permissions)
    {
        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var enforcedPermissions = new[]
        {
            "reports.submit",
            "reports.approve",
            "roles.manage",
            "permissions.manage",
            "users.manage",
            "gl.approve",
            "audit.view"
        };

        return permissions.Any(permission => enforcedPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase));
    }

    private async Task<LoginResponse> IssueAuthenticatedSessionAsync(
        Staff user,
        (string RoleName, string RoleId, string[] AllPermissions, string[] LeasedPermissions) permissionBundle,
        string ipAddress,
        string? userAgent)
    {
        var token = CreateJwtToken(user, permissionBundle);
        var session = await _sessionService.CreateSessionAsync(user, token, ipAddress, userAgent);
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            User = new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = permissionBundle.RoleName,
                role_id = permissionBundle.RoleId,
                branch_id = user.BranchId,
                status = user.Status,
                roleName = permissionBundle.RoleName,
                permissions = permissionBundle.AllPermissions,
                leasedPermissions = permissionBundle.LeasedPermissions
            },
            Token = token,
            RefreshToken = session.RefreshToken,
            MfaRequired = false
        };
    }

    private string CreateJwtToken(
        Staff user,
        (string RoleName, string RoleId, string[] AllPermissions, string[] LeasedPermissions) permissionBundle)
    {
        var secret = _config["JwtSettings:Secret"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("JWT secret key is not configured");
        }

        var issuer = _config["JwtSettings:Issuer"] ?? "BankInsight";
        var audience = _config["JwtSettings:Audience"] ?? "BankInsightAPI";
        var key = Encoding.ASCII.GetBytes(secret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name ?? string.Empty),
            new(ClaimTypes.Email, user.Email),
            new("branch_id", user.BranchId ?? string.Empty),
            new("role_id", permissionBundle.RoleId),
            new("access_scope_type", user.AccessScopeType.ToString()),
            new(ClaimTypes.Role, permissionBundle.RoleName)
        };

        foreach (var permission in permissionBundle.AllPermissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        foreach (var leasedPermission in permissionBundle.LeasedPermissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("leased_permissions", leasedPermission));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    private async Task<MfaChallengeRecord> CreateMfaChallengeAsync(Staff user, string ipAddress, string? userAgent)
    {
        var token = Guid.NewGuid().ToString("N");
        var code = Random.Shared.Next(100000, 999999).ToString();
        var challenge = new MfaChallengeRecord
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Attempts = 0,
            DeliveryChannel = "EMAIL_OTP",
            DeliveryHint = MaskEmail(user.Email),
            DebugCode = _hostEnvironment.IsDevelopment() ? code : null,
            RequestedIpAddress = ipAddress,
            RequestedUserAgent = userAgent
        };

        var key = GetMfaChallengeKey(token);
        var row = new SystemConfig
        {
            Id = $"CFG{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Key = key,
            Value = JsonSerializer.Serialize(challenge),
            Description = $"MFA challenge for {user.Id}",
            UpdatedAt = DateTime.UtcNow
        };

        _context.SystemConfigs.Add(row);
        await _context.SaveChangesAsync();

        try
        {
            await _emailAlertService.SendEmailAsync(
                user.Email,
                "Your BankInsight verification code",
                $"Use this one-time verification code to complete your sign-in: {code}\n\nThe code expires in 5 minutes. If you did not attempt to sign in, please contact your administrator immediately.",
                new
                {
                    userId = user.Id,
                    deliveryChannel = challenge.DeliveryChannel,
                    expiresAt = challenge.ExpiresAt
                },
                category: "MFA_OTP");
        }
        catch (Exception ex)
        {
            await _auditLoggingService.LogActionAsync(
                "MFA_OTP_DELIVERY_FAILED",
                "Staff",
                user.Id,
                user.Id,
                $"The one-time login code could not be delivered to {challenge.DeliveryHint}.",
                ipAddress,
                userAgent,
                "FAILED",
                errorMessage: ex.Message,
                newValues: new
                {
                    challenge.DeliveryChannel,
                    challenge.DeliveryHint,
                    challenge.ExpiresAt
                });

            throw new InvalidOperationException(
                $"We could not deliver your verification code to {challenge.DeliveryHint}. Please try again in a moment or contact your administrator.",
                ex);
        }

        await _auditLoggingService.LogActionAsync(
            "MFA_OTP_GENERATED",
            "Staff",
            user.Id,
            user.Id,
            $"A one-time login code was generated for delivery to {challenge.DeliveryHint}. Use code {code} in controlled non-production environments only.",
            ipAddress,
            userAgent,
            "PENDING");

        challenge.DeliveryStatus = "sent";
        challenge.DeliveryMessage = $"A 6-digit verification code was sent to {challenge.DeliveryHint}. Check your inbox, spam, or promotions folders. The code expires in 5 minutes.";

        return challenge;
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2 || parts[0].Length < 2)
        {
            return "***";
        }

        return $"{parts[0][0]}***{parts[0][^1]}@{parts[1]}";
    }

    private static string GetMfaChallengeKey(string token) => $"{MfaChallengePrefix}{token}";

    private static MfaChallengeRecord? DeserializeChallenge(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MfaChallengeRecord>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private sealed class MfaChallengeRecord
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CodeHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int Attempts { get; set; }
        public string DeliveryChannel { get; set; } = "EMAIL_OTP";
        public string DeliveryHint { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = "pending";
        public string DeliveryMessage { get; set; } = string.Empty;
        public string? DebugCode { get; set; }
        public string RequestedIpAddress { get; set; } = string.Empty;
        public string? RequestedUserAgent { get; set; }
    }
}
