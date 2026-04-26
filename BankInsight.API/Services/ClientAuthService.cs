using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BankInsight.API.Services;

public class ClientAuthService
{
    private const string ClientMfaChallengePrefix = "client_mfa_challenge_";
    private const string ClientRegistrationChallengePrefix = "client_registration_challenge_";
    private const string ClientPasswordResetChallengePrefix = "client_password_reset_challenge_";
    private const string ClientStepUpChallengePrefix = "client_step_up_challenge_";
    private const string ClientStepUpVerifiedPrefix = "client_step_up_verified_";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailAlertService _emailAlertService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IHostEnvironment _hostEnvironment;

    public ClientAuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmailAlertService emailAlertService,
        IAuditLoggingService auditLoggingService,
        IHostEnvironment hostEnvironment)
    {
        _context = context;
        _configuration = configuration;
        _emailAlertService = emailAlertService;
        _auditLoggingService = auditLoggingService;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<ClientLoginResponse?> LoginAsync(ClientLoginRequest request, string ipAddress, string? userAgent)
    {
        var credential = await LoadCredentialAsync(request.Email);
        if (credential?.Customer == null || !credential.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash))
        {
            return null;
        }

        if (credential.MfaEnabled)
        {
            var challenge = await CreateVerificationChallengeAsync(
                ClientMfaChallengePrefix,
                "LOGIN",
                credential,
                ipAddress,
                userAgent,
                5,
                "Your BankInsight Client verification code");

            return new ClientLoginResponse
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

        return await IssueAuthenticatedSessionAsync(credential, ipAddress, userAgent);
    }

    public async Task<ClientVerificationChallengeResponse> RegisterAsync(ClientRegisterRequest request, string ipAddress, string? userAgent)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingCredential = await _context.Set<CustomerCredential>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LoginEmail.ToLower() == normalizedEmail);

        if (existingCredential != null)
        {
            throw new InvalidOperationException("A client account with that email already exists.");
        }

        var customerId = await GenerateCustomerIdAsync();
        var credentialId = await GenerateCredentialIdAsync();

        var customer = new Customer
        {
            Id = customerId,
            Type = "INDIVIDUAL",
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            Phone = request.Phone.Trim(),
            DigitalAddress = string.IsNullOrWhiteSpace(request.DigitalAddress) ? null : request.DigitalAddress.Trim(),
            GhanaCard = string.IsNullOrWhiteSpace(request.GhanaCard) ? null : request.GhanaCard.Trim(),
            KycLevel = "Tier 1",
            RiskRating = "Low",
            BranchId = "BR001",
            CreatedAt = DateTime.UtcNow
        };

        var credential = new CustomerCredential
        {
            Id = credentialId,
            CustomerId = customerId,
            Customer = customer,
            LoginEmail = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = false,
            MfaEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        _context.Set<CustomerCredential>().Add(credential);
        await _context.SaveChangesAsync();

        var challenge = await CreateVerificationChallengeAsync(
            ClientRegistrationChallengePrefix,
            "REGISTRATION",
            credential,
            ipAddress,
            userAgent,
            10,
            "Verify your BankInsight Client registration");

        await _auditLoggingService.LogActionAsync(
            "CLIENT_REGISTRATION_STARTED",
            "CUSTOMER",
            customer.Id,
            credential.Id,
            $"Client registration started for {normalizedEmail}.",
            ipAddress,
            userAgent,
            "SUCCESS");

        return MapChallengeResponse(challenge);
    }

    public async Task<ClientLoginResponse?> VerifyRegistrationAsync(ClientVerifyRegistrationRequest request, string ipAddress, string? userAgent)
    {
        var challenge = await VerifyChallengeAsync(ClientRegistrationChallengePrefix, request.RegistrationToken, request.Code);
        if (challenge == null)
        {
            return null;
        }

        var credential = await _context.Set<CustomerCredential>()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == challenge.CustomerCredentialId);

        if (credential?.Customer == null)
        {
            return null;
        }

        credential.IsActive = true;
        await RemoveSystemConfigAsync(BuildConfigKey(ClientRegistrationChallengePrefix, request.RegistrationToken));
        await _context.SaveChangesAsync();

        return await IssueAuthenticatedSessionAsync(credential, ipAddress, userAgent);
    }

    public async Task<ClientLoginResponse?> VerifyMfaAsync(ClientVerifyMfaRequest request, string ipAddress, string? userAgent)
    {
        var challenge = await VerifyChallengeAsync(ClientMfaChallengePrefix, request.MfaToken, request.Code);
        if (challenge == null)
        {
            return null;
        }

        var credential = await _context.Set<CustomerCredential>()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == challenge.CustomerCredentialId && c.IsActive);
        if (credential?.Customer == null)
        {
            return null;
        }

        await RemoveSystemConfigAsync(BuildConfigKey(ClientMfaChallengePrefix, request.MfaToken));
        await _context.SaveChangesAsync();

        return await IssueAuthenticatedSessionAsync(credential, ipAddress, userAgent);
    }

    public async Task<ClientLoginResponse?> ResendMfaAsync(ClientResendMfaRequest request, string ipAddress, string? userAgent)
    {
        var existing = await LoadChallengeAsync(ClientMfaChallengePrefix, request.MfaToken);
        if (existing is not { } loaded)
        {
            return null;
        }

        var credential = await _context.Set<CustomerCredential>()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == loaded.Challenge.CustomerCredentialId && c.IsActive);
        if (credential?.Customer == null)
        {
            return null;
        }

        var code = Random.Shared.Next(100000, 999999).ToString();
        loaded.Challenge.CodeHash = BCrypt.Net.BCrypt.HashPassword(code);
        loaded.Challenge.ExpiresAt = DateTime.UtcNow.AddMinutes(5);
        loaded.Challenge.Attempts = 0;
        loaded.Challenge.DeliveryStatus = "sent";
        loaded.Challenge.DeliveryMessage = $"A new 6-digit verification code was sent to {loaded.Challenge.DeliveryHint}. The code expires in 5 minutes.";
        loaded.Challenge.DebugCode = _hostEnvironment.IsDevelopment() ? code : null;
        loaded.Challenge.RequestedIpAddress = ipAddress;
        loaded.Challenge.RequestedUserAgent = userAgent;
        loaded.Row.Value = JsonSerializer.Serialize(loaded.Challenge);
        loaded.Row.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await SendOtpAsync(credential.Customer, code, loaded.Challenge.ExpiresAt, "Your BankInsight Client verification code");

        return new ClientLoginResponse
        {
            MfaRequired = true,
            MfaToken = loaded.Challenge.Token,
            DeliveryChannel = loaded.Challenge.DeliveryChannel,
            DeliveryHint = loaded.Challenge.DeliveryHint,
            DeliveryStatus = loaded.Challenge.DeliveryStatus,
            DeliveryMessage = loaded.Challenge.DeliveryMessage,
            MfaExpiresAtUtc = loaded.Challenge.ExpiresAt,
            AllowedFactors = ["otp"],
            DebugCode = _hostEnvironment.IsDevelopment() ? code : null
        };
    }

    public async Task<ClientIdentityDto?> GetCurrentUserAsync(string credentialId)
    {
        var credential = await _context.Set<CustomerCredential>()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == credentialId && c.IsActive);
        return credential?.Customer == null ? null : MapIdentity(credential);
    }

    public async Task<ClientLoginResponse?> RefreshAsync(ClientRefreshTokenRequest request)
    {
        var hashed = HashToken(request.RefreshToken);
        var session = await _context.Set<ClientChannelSession>()
            .Include(s => s.CustomerCredential)
                .ThenInclude(c => c!.Customer)
            .FirstOrDefaultAsync(s => s.IsActive && (s.RefreshToken == request.RefreshToken || s.RefreshToken == hashed));

        if (session?.CustomerCredential?.Customer == null || session.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var token = CreateJwtToken(session.CustomerCredential);
        var refreshToken = GenerateRefreshToken();
        session.Token = token;
        session.RefreshToken = HashToken(refreshToken);
        session.ExpiresAt = DateTime.UtcNow.AddHours(12);
        session.LastActivity = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ClientLoginResponse
        {
            User = MapIdentity(session.CustomerCredential),
            Token = token,
            RefreshToken = refreshToken
        };
    }

    public async Task<ClientPasswordResetStartResponse> StartPasswordResetAsync(ClientStartPasswordResetRequest request, string ipAddress, string? userAgent)
    {
        var credential = await LoadCredentialAsync(request.Email);
        if (credential?.Customer == null || !credential.IsActive)
        {
            return new ClientPasswordResetStartResponse
            {
                Accepted = true,
                DeliveryMessage = "If the email exists in BankInsight Client, a verification code will be sent shortly."
            };
        }

        var challenge = await CreateVerificationChallengeAsync(
            ClientPasswordResetChallengePrefix,
            "PASSWORD_RESET",
            credential,
            ipAddress,
            userAgent,
            10,
            "Reset your BankInsight Client password");

        await _auditLoggingService.LogActionAsync(
            "CLIENT_PASSWORD_RESET_STARTED",
            "CUSTOMER",
            credential.CustomerId,
            credential.Id,
            $"Password reset started for customer {credential.CustomerId}.",
            ipAddress,
            userAgent,
            "SUCCESS");

        return new ClientPasswordResetStartResponse
        {
            Accepted = true,
            ResetToken = challenge.Token,
            DeliveryHint = challenge.DeliveryHint,
            DeliveryChannel = challenge.DeliveryChannel,
            DeliveryStatus = challenge.DeliveryStatus,
            DeliveryMessage = challenge.DeliveryMessage,
            ExpiresAtUtc = challenge.ExpiresAt,
            DebugCode = _hostEnvironment.IsDevelopment() ? challenge.DebugCode : null
        };
    }

    public async Task<ClientOperationResponse?> CompletePasswordResetAsync(ClientCompletePasswordResetRequest request)
    {
        var challenge = await VerifyChallengeAsync(ClientPasswordResetChallengePrefix, request.ResetToken, request.Code);
        if (challenge == null)
        {
            return null;
        }

        var credential = await _context.Set<CustomerCredential>()
            .FirstOrDefaultAsync(c => c.Id == challenge.CustomerCredentialId && c.IsActive);
        if (credential == null)
        {
            return null;
        }

        credential.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        var sessions = await _context.Set<ClientChannelSession>()
            .Where(s => s.CustomerCredentialId == credential.Id && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.LogoutAt = DateTime.UtcNow;
        }

        await RemoveSystemConfigAsync(BuildConfigKey(ClientPasswordResetChallengePrefix, request.ResetToken));
        await _context.SaveChangesAsync();

        return new ClientOperationResponse
        {
            Success = true,
            Message = "Your password has been reset. Please sign in again."
        };
    }

    public async Task<ClientVerificationChallengeResponse?> InitiateStepUpAsync(string credentialId, ClientStartStepUpRequest request, string ipAddress, string? userAgent)
    {
        var credential = await _context.Set<CustomerCredential>()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == credentialId && c.IsActive);
        if (credential?.Customer == null)
        {
            return null;
        }

        var allowedFactors = GetAvailableStepUpFactors(credential);
        var requestedFactor = string.IsNullOrWhiteSpace(request.Factor)
            ? (allowedFactors.Contains("pin") ? "pin" : "otp")
            : request.Factor.Trim().ToLowerInvariant();
        if (!allowedFactors.Contains(requestedFactor))
        {
            throw new InvalidOperationException("The requested verification factor is not available for this account.");
        }

        var challenge = await CreateVerificationChallengeAsync(
            ClientStepUpChallengePrefix,
            request.Purpose.Trim().ToUpperInvariant(),
            credential,
            ipAddress,
            userAgent,
            5,
            $"Approve {request.Purpose.Trim()}",
            requestedFactor);

        return MapChallengeResponse(challenge, allowedFactors);
    }

    public async Task<ClientVerifiedStepUpResponse?> VerifyStepUpAsync(string credentialId, ClientVerifyStepUpRequest request)
    {
        var challenge = await VerifyChallengeAsync(ClientStepUpChallengePrefix, request.ChallengeToken, request.Code);
        if (challenge == null || !string.Equals(challenge.CustomerCredentialId, credentialId, StringComparison.Ordinal))
        {
            return null;
        }

        var verifiedToken = Guid.NewGuid().ToString("N");
        var verifiedRecord = new VerifiedStepUpRecord
        {
            Token = verifiedToken,
            CustomerCredentialId = credentialId,
            Purpose = challenge.Purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _context.SystemConfigs.Add(new SystemConfig
        {
            Id = $"CFG{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Key = BuildConfigKey(ClientStepUpVerifiedPrefix, verifiedToken),
            Value = JsonSerializer.Serialize(verifiedRecord),
            Description = $"Verified step-up for {credentialId}",
            UpdatedAt = DateTime.UtcNow
        });

        await RemoveSystemConfigAsync(BuildConfigKey(ClientStepUpChallengePrefix, request.ChallengeToken));
        await _context.SaveChangesAsync();

        return new ClientVerifiedStepUpResponse
        {
            StepUpToken = verifiedToken,
            Purpose = verifiedRecord.Purpose,
            ExpiresAtUtc = verifiedRecord.ExpiresAt,
            Factor = challenge.Factor
        };
    }

    public async Task<ClientOperationResponse?> SetTransactionPinAsync(string credentialId, ClientSetTransactionPinRequest request)
    {
        var credential = await _context.Set<CustomerCredential>()
            .FirstOrDefaultAsync(c => c.Id == credentialId && c.IsActive);
        if (credential == null || !BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash))
        {
            return null;
        }

        credential.TransactionPinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin.Trim());
        await _context.SaveChangesAsync();

        return new ClientOperationResponse
        {
            Success = true,
            Message = "Transaction PIN saved successfully."
        };
    }

    public async Task<bool> ConsumeStepUpTokenAsync(string credentialId, string stepUpToken, string purpose)
    {
        var row = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == BuildConfigKey(ClientStepUpVerifiedPrefix, stepUpToken));
        if (row == null)
        {
            return false;
        }

        var verified = DeserializeVerifiedStepUp(row.Value);
        if (verified == null ||
            verified.ExpiresAt <= DateTime.UtcNow ||
            !string.Equals(verified.CustomerCredentialId, credentialId, StringComparison.Ordinal) ||
            !string.Equals(verified.Purpose, purpose, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _context.SystemConfigs.Remove(row);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task LogoutAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var session = await _context.Set<ClientChannelSession>().FirstOrDefaultAsync(s => s.Token == token && s.IsActive);
        if (session == null)
        {
            return;
        }

        session.IsActive = false;
        session.LogoutAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> VerifyCurrentPasswordAsync(string credentialId, string password)
    {
        var credential = await _context.Set<CustomerCredential>().FirstOrDefaultAsync(c => c.Id == credentialId && c.IsActive);
        return credential != null && BCrypt.Net.BCrypt.Verify(password, credential.PasswordHash);
    }

    private async Task<CustomerCredential?> LoadCredentialAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Set<CustomerCredential>()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.LoginEmail.ToLower() == normalizedEmail);
    }

    private async Task<ClientLoginResponse> IssueAuthenticatedSessionAsync(CustomerCredential credential, string ipAddress, string? userAgent)
    {
        var token = CreateJwtToken(credential);
        var refreshToken = GenerateRefreshToken();

        _context.Set<ClientChannelSession>().Add(new ClientChannelSession
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerCredentialId = credential.Id,
            CustomerId = credential.CustomerId,
            Token = token,
            RefreshToken = HashToken(refreshToken),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12),
            LastActivity = DateTime.UtcNow,
            IsActive = true
        });

        credential.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CLIENT_LOGIN_SUCCESS",
            "CUSTOMER",
            credential.CustomerId,
            credential.Id,
            $"Client login session created for customer {credential.CustomerId}.",
            ipAddress,
            userAgent,
            "SUCCESS");

        return new ClientLoginResponse
        {
            User = MapIdentity(credential),
            Token = token,
            RefreshToken = refreshToken
        };
    }

    private string CreateJwtToken(CustomerCredential credential)
    {
        var issuer = _configuration["JwtSettings:ClientIssuer"] ?? "BankInsightClientAuth";
        var audience = _configuration["JwtSettings:ClientAudience"] ?? "BankInsightClientAPI";
        var key = JwtSecretResolver.ResolveBytes(_configuration);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, credential.Id),
            new(ClaimTypes.Name, credential.Customer?.Name ?? credential.LoginEmail),
            new(ClaimTypes.Email, credential.LoginEmail),
            new(ClaimTypes.Role, "ClientCustomer"),
            new("actor_type", "customer"),
            new("customer_id", credential.CustomerId),
            new("token_family", "client_channel"),
            new("amr", credential.MfaEnabled ? "otp" : "pwd")
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private async Task<ClientChallengeRecord> CreateVerificationChallengeAsync(
        string prefix,
        string purpose,
        CustomerCredential credential,
        string ipAddress,
        string? userAgent,
        int expiryMinutes,
        string emailSubject,
        string factor = "otp")
    {
        factor = factor.Trim().ToLowerInvariant();
        var code = factor == "pin"
            ? null
            : Random.Shared.Next(100000, 999999).ToString();
        var challenge = new ClientChallengeRecord
        {
            Token = Guid.NewGuid().ToString("N"),
            CustomerCredentialId = credential.Id,
            Purpose = purpose,
            CodeHash = factor == "pin"
                ? credential.TransactionPinHash ?? string.Empty
                : BCrypt.Net.BCrypt.HashPassword(code!),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Attempts = 0,
            DeliveryChannel = factor == "pin" ? "TRANSACTION_PIN" : "EMAIL_OTP",
            DeliveryHint = factor == "pin" ? "4-digit transaction PIN" : MaskEmail(credential.LoginEmail),
            DeliveryStatus = factor == "pin" ? "ready" : "sent",
            DeliveryMessage = factor == "pin"
                ? "Enter your 4-digit transaction PIN to approve this action."
                : $"A 6-digit verification code was sent to {MaskEmail(credential.LoginEmail)}. The code expires in {expiryMinutes} minutes.",
            DebugCode = factor == "pin" ? null : (_hostEnvironment.IsDevelopment() ? code : null),
            RequestedIpAddress = ipAddress,
            RequestedUserAgent = userAgent,
            Factor = factor
        };

        _context.SystemConfigs.Add(new SystemConfig
        {
            Id = $"CFG{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Key = BuildConfigKey(prefix, challenge.Token),
            Value = JsonSerializer.Serialize(challenge),
            Description = $"Client challenge for {credential.CustomerId}",
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        if (factor != "pin")
        {
            await SendOtpAsync(credential.Customer!, code!, challenge.ExpiresAt, emailSubject);
        }
        return challenge;
    }

    private async Task<(SystemConfig Row, ClientChallengeRecord Challenge)?> LoadChallengeAsync(string prefix, string token)
    {
        var row = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == BuildConfigKey(prefix, token));
        if (row == null)
        {
            return null;
        }

        var challenge = DeserializeChallenge(row.Value);
        return challenge == null ? null : (row, challenge);
    }

    private async Task<ClientChallengeRecord?> VerifyChallengeAsync(string prefix, string token, string code)
    {
        var loaded = await LoadChallengeAsync(prefix, token);
        if (loaded is not { } challengeRow)
        {
            return null;
        }

        var challenge = challengeRow.Challenge;
        if (challenge.ExpiresAt <= DateTime.UtcNow || challenge.Attempts >= 5)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(code, challenge.CodeHash))
        {
            challenge.Attempts += 1;
            challengeRow.Row.Value = JsonSerializer.Serialize(challenge);
            challengeRow.Row.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return null;
        }

        return challenge;
    }

    private async Task RemoveSystemConfigAsync(string key)
    {
        var row = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
        if (row != null)
        {
            _context.SystemConfigs.Remove(row);
        }
    }

    private async Task<string> GenerateCustomerIdAsync()
    {
        var prefix = $"CIF-{DateTime.UtcNow:yyMM}";
        string candidate;
        do
        {
            candidate = $"{prefix}-{Random.Shared.Next(10000, 99999)}";
        } while (await _context.Customers.AnyAsync(c => c.Id == candidate));

        return candidate;
    }

    private async Task<string> GenerateCredentialIdAsync()
    {
        string candidate;
        do
        {
            candidate = $"CCRED{Random.Shared.Next(100000, 999999)}";
        } while (await _context.Set<CustomerCredential>().AnyAsync(c => c.Id == candidate));

        return candidate;
    }

    private async Task SendOtpAsync(Customer customer, string code, DateTime expiresAt, string subject)
    {
        await _emailAlertService.SendEmailAsync(
            customer.Email ?? string.Empty,
            subject,
            $"Use this verification code to continue: {code}\n\nThe code expires at {expiresAt:O}.",
            new { customerId = customer.Id, expiresAt },
            category: "CLIENT_MFA_OTP");
    }

    private ClientVerificationChallengeResponse MapChallengeResponse(ClientChallengeRecord challenge, string[]? allowedFactors = null)
    {
        return new ClientVerificationChallengeResponse
        {
            ChallengeRequired = true,
            ChallengeToken = challenge.Token,
            DeliveryChannel = challenge.DeliveryChannel,
            DeliveryHint = challenge.DeliveryHint,
            DeliveryStatus = challenge.DeliveryStatus,
            DeliveryMessage = challenge.DeliveryMessage,
            ExpiresAtUtc = challenge.ExpiresAt,
            DebugCode = _hostEnvironment.IsDevelopment() ? challenge.DebugCode : null,
            Factor = challenge.Factor,
            AllowedFactors = allowedFactors ?? [challenge.Factor]
        };
    }

    private static ClientIdentityDto MapIdentity(CustomerCredential credential)
    {
        return new ClientIdentityDto
        {
            UserId = credential.Id,
            CustomerId = credential.CustomerId,
            Name = credential.Customer?.Name ?? credential.LoginEmail,
            Email = credential.LoginEmail,
            Role = "ClientCustomer",
            Permissions = [],
            HasTransactionPin = !string.IsNullOrWhiteSpace(credential.TransactionPinHash)
        };
    }

    private static string[] GetAvailableStepUpFactors(CustomerCredential credential)
    {
        var factors = new List<string> { "otp" };
        if (!string.IsNullOrWhiteSpace(credential.TransactionPinHash))
        {
            factors.Insert(0, "pin");
        }

        return factors.ToArray();
    }

    private static string GenerateRefreshToken()
    {
        var random = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return Convert.ToBase64String(random);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string BuildConfigKey(string prefix, string token) => $"{prefix}{token}";

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2 || parts[0].Length < 2)
        {
            return "***";
        }

        return $"{parts[0][0]}***{parts[0][^1]}@{parts[1]}";
    }

    private static ClientChallengeRecord? DeserializeChallenge(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ClientChallengeRecord>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static VerifiedStepUpRecord? DeserializeVerifiedStepUp(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<VerifiedStepUpRecord>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private sealed class ClientChallengeRecord
    {
        public string Token { get; set; } = string.Empty;
        public string CustomerCredentialId { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string CodeHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int Attempts { get; set; }
        public string DeliveryChannel { get; set; } = string.Empty;
        public string DeliveryHint { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public string DeliveryMessage { get; set; } = string.Empty;
        public string? DebugCode { get; set; }
        public string RequestedIpAddress { get; set; } = string.Empty;
        public string? RequestedUserAgent { get; set; }
        public string Factor { get; set; } = "otp";
    }

    private sealed class VerifiedStepUpRecord
    {
        public string Token { get; set; } = string.Empty;
        public string CustomerCredentialId { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
