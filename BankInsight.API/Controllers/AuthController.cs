using System;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ISessionService _sessionService;

    public AuthController(AuthService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var result = await _authService.LoginAsync(request, ipAddress, userAgent);
        if (result == null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(result);
    }

    [HttpPost("mfa/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var result = await _authService.VerifyMfaAsync(request, ipAddress, userAgent);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid or expired verification code" });
        }

        return Ok(result);
    }

    [HttpGet("validate")]
    [Authorize]
    public IActionResult Validate()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Invalid or expired token" });
        }

        return Ok(new { valid = true });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var permissions = User.FindAll("permissions").Select(claim => claim.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        return Ok(new
        {
            id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            name = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            role,
            permissions
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _sessionService.RefreshTokenAsync(request.RefreshToken);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var authorizationHeader = HttpContext.Request.Headers.Authorization.ToString();
        var token = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader["Bearer ".Length..].Trim()
            : string.Empty;

        await _sessionService.InvalidateCurrentSessionAsync(token);
        return Ok(new { message = "Logged out successfully" });
    }
}

