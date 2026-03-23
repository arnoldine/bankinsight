using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 255 characters")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public object? User { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public bool MfaRequired { get; set; }
    public string? MfaToken { get; set; }
    public string? DeliveryChannel { get; set; }
    public string? DeliveryHint { get; set; }
    public string? DeliveryStatus { get; set; }
    public string? DeliveryMessage { get; set; }
    public DateTime? MfaExpiresAtUtc { get; set; }
    public string[] AllowedFactors { get; set; } = [];
    public string? DebugCode { get; set; }
}

public class VerifyMfaRequest
{
    [Required(ErrorMessage = "MFA token is required")]
    public string MfaToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be exactly 6 digits")]
    public string Code { get; set; } = string.Empty;
}

public class ResendMfaRequest
{
    [Required(ErrorMessage = "MFA token is required")]
    public string MfaToken { get; set; } = string.Empty;
}
