using System.Text.Json.Serialization;

namespace CoreBanker.Auth;

public sealed class LoginRequestDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class VerifyMfaRequestDto
{
    [JsonPropertyName("mfaToken")]
    public string MfaToken { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

public sealed class ResendMfaRequestDto
{
    [JsonPropertyName("mfaToken")]
    public string MfaToken { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequestDto
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class AuthUserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = [];
}

public sealed class AuthResponseDto
{
    [JsonPropertyName("user")]
    public AuthUserDto? User { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("mfaRequired")]
    public bool MfaRequired { get; set; }

    [JsonPropertyName("mfaToken")]
    public string? MfaToken { get; set; }

    [JsonPropertyName("deliveryChannel")]
    public string? DeliveryChannel { get; set; }

    [JsonPropertyName("deliveryHint")]
    public string? DeliveryHint { get; set; }

    [JsonPropertyName("deliveryStatus")]
    public string? DeliveryStatus { get; set; }

    [JsonPropertyName("deliveryMessage")]
    public string? DeliveryMessage { get; set; }

    [JsonPropertyName("debugCode")]
    public string? DebugCode { get; set; }
}

public sealed class PersistedSessionDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class AuthChallengeResult
{
    public bool RequiresMfa { get; init; }
    public string? MfaToken { get; init; }
    public string? DeliveryHint { get; init; }
    public string? DeliveryChannel { get; init; }
    public string? DeliveryMessage { get; init; }
    public string? DebugCode { get; init; }
}
