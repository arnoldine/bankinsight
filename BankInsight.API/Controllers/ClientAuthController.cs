using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/client-auth")]
public class ClientAuthController : ControllerBase
{
    private readonly ClientAuthService _clientAuthService;

    public ClientAuthController(ClientAuthService clientAuthService)
    {
        _clientAuthService = clientAuthService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] ClientLoginRequest request)
    {
        var result = await _clientAuthService.LoginAsync(request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HttpContext.Request.Headers.UserAgent.ToString());
        return result == null ? Unauthorized(new { message = "Invalid email or password" }) : Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] ClientRegisterRequest request)
    {
        try
        {
            var result = await _clientAuthService.RegisterAsync(request,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                HttpContext.Request.Headers.UserAgent.ToString());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("register/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyRegistration([FromBody] ClientVerifyRegistrationRequest request)
    {
        var result = await _clientAuthService.VerifyRegistrationAsync(request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HttpContext.Request.Headers.UserAgent.ToString());
        return result == null ? Unauthorized(new { message = "Invalid or expired registration verification code" }) : Ok(result);
    }

    [HttpPost("mfa/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyMfa([FromBody] ClientVerifyMfaRequest request)
    {
        var result = await _clientAuthService.VerifyMfaAsync(request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HttpContext.Request.Headers.UserAgent.ToString());
        return result == null ? Unauthorized(new { message = "Invalid or expired verification code" }) : Ok(result);
    }

    [HttpPost("mfa/resend")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendMfa([FromBody] ClientResendMfaRequest request)
    {
        var result = await _clientAuthService.ResendMfaAsync(request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HttpContext.Request.Headers.UserAgent.ToString());
        return result == null ? Unauthorized(new { message = "Your verification session is no longer active. Please sign in again." }) : Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Policy = "ClientCustomer")]
    public async Task<IActionResult> Me()
    {
        var credentialId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var user = await _clientAuthService.GetCurrentUserAsync(credentialId);
        return user == null ? Unauthorized(new { message = "Invalid client session." }) : Ok(user);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] ClientRefreshTokenRequest request)
    {
        var result = await _clientAuthService.RefreshAsync(request);
        return result == null ? Unauthorized(new { message = "Invalid or expired refresh token" }) : Ok(result);
    }

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    public async Task<IActionResult> StartPasswordReset([FromBody] ClientStartPasswordResetRequest request)
    {
        var result = await _clientAuthService.StartPasswordResetAsync(request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HttpContext.Request.Headers.UserAgent.ToString());
        return Ok(result);
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> CompletePasswordReset([FromBody] ClientCompletePasswordResetRequest request)
    {
        var result = await _clientAuthService.CompletePasswordResetAsync(request);
        return result == null ? Unauthorized(new { message = "Invalid or expired password reset verification code" }) : Ok(result);
    }

    [HttpPost("step-up/initiate")]
    [Authorize(Policy = "ClientCustomer")]
    public async Task<IActionResult> InitiateStepUp([FromBody] ClientStartStepUpRequest request)
    {
        var credentialId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _clientAuthService.InitiateStepUpAsync(credentialId, request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HttpContext.Request.Headers.UserAgent.ToString());
        return result == null ? Unauthorized(new { message = "Invalid client session." }) : Ok(result);
    }

    [HttpPost("step-up/verify")]
    [Authorize(Policy = "ClientCustomer")]
    public async Task<IActionResult> VerifyStepUp([FromBody] ClientVerifyStepUpRequest request)
    {
        var credentialId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _clientAuthService.VerifyStepUpAsync(credentialId, request);
        return result == null ? Unauthorized(new { message = "Invalid or expired verification code" }) : Ok(result);
    }

    [HttpPost("transaction-pin")]
    [Authorize(Policy = "ClientCustomer")]
    public async Task<IActionResult> SetTransactionPin([FromBody] ClientSetTransactionPinRequest request)
    {
        var credentialId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _clientAuthService.SetTransactionPinAsync(credentialId, request);
        return result == null ? Unauthorized(new { message = "Unable to save transaction PIN with the supplied credentials." }) : Ok(result);
    }

    [HttpPost("logout")]
    [Authorize(Policy = "ClientCustomer")]
    public async Task<IActionResult> Logout()
    {
        var authorizationHeader = HttpContext.Request.Headers.Authorization.ToString();
        var token = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader["Bearer ".Length..].Trim()
            : string.Empty;

        await _clientAuthService.LogoutAsync(token);
        return Ok(new { message = "Logged out successfully" });
    }
}
