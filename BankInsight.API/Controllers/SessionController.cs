using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankInsight.API.DTOs;
using BankInsight.API.Security;
using BankInsight.API.Services;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _sessionService.RefreshTokenAsync(request.RefreshToken);
        
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        return Ok(result);
    }

    [HttpPost("{sessionId}/invalidate")]
    [HasPermission(AppPermissions.Users.Manage)]
    public async Task<ActionResult> InvalidateSession(string sessionId)
    {
        var result = await _sessionService.InvalidateSessionAsync(sessionId);
        
        if (!result)
        {
            return NotFound(new { message = "Session not found" });
        }

        return Ok(new { message = "Session invalidated successfully" });
    }

    [HttpPost("invalidate-all")]
    public async Task<ActionResult> InvalidateAllSessions()
    {
        var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(staffId))
        {
            return Unauthorized();
        }

        await _sessionService.InvalidateAllUserSessionsAsync(staffId);
        
        return Ok(new { message = "All sessions invalidated successfully" });
    }

    [HttpGet("active")]
    [HasPermission(AppPermissions.Users.Manage)]
    public async Task<ActionResult<List<SessionDto>>> GetActiveSessions()
    {
        var sessions = await _sessionService.GetActiveSessionsAsync();
        return Ok(sessions);
    }

    [HttpGet("user/{staffId}")]
    public async Task<ActionResult<List<SessionDto>>> GetUserSessions(string staffId)
    {
        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var canManageUsers = User.FindAll("permissions").Any(claim => string.Equals(claim.Value, AppPermissions.Users.Manage, StringComparison.OrdinalIgnoreCase));
        if (!canManageUsers && !string.Equals(callerId, staffId, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var sessions = await _sessionService.GetUserSessionsAsync(staffId);
        return Ok(sessions);
    }

    [HttpGet("stats")]
    [HasPermission(AppPermissions.Users.Manage)]
    public async Task<ActionResult<SessionStatsDto>> GetSessionStats()
    {
        var stats = await _sessionService.GetSessionStatsAsync();
        return Ok(stats);
    }
}
