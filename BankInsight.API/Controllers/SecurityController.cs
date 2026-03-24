using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/security")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDeviceSecurityService _deviceSecurityService;
    private readonly ISessionService _sessionService;
    private readonly ICurrentUserContext _currentUser;

    public SecurityController(
        ApplicationDbContext context,
        IDeviceSecurityService deviceSecurityService,
        ISessionService sessionService,
        ICurrentUserContext currentUser)
    {
        _context = context;
        _deviceSecurityService = deviceSecurityService;
        _sessionService = sessionService;
        _currentUser = currentUser;
    }

    [HttpGet("alerts")]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<IActionResult> GetSecurityAlerts([FromQuery] int limit = 100)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);

        var alerts = await _context.AuditLogs
            .Where(a => a.Action.StartsWith("SECURITY_ALERT_"))
            .OrderByDescending(a => a.CreatedAt)
            .Take(safeLimit)
            .Select(a => new SecurityAlertDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                UserId = a.UserId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                Status = a.Status,
                ErrorMessage = a.ErrorMessage,
                NewValues = a.NewValues,
                CreatedAt = a.CreatedAt,
            })
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpGet("failed-logins")]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<IActionResult> GetFailedLogins([FromQuery] int sinceMinutes = 60, [FromQuery] int limit = 100)
    {
        var safeMinutes = Math.Clamp(sinceMinutes, 1, 24 * 60);
        var safeLimit = Math.Clamp(limit, 1, 500);
        var since = DateTime.UtcNow.AddMinutes(-safeMinutes);

        var failedLogins = await _context.LoginAttempts
            .Where(a => !a.Success && a.AttemptedAt >= since)
            .OrderByDescending(a => a.AttemptedAt)
            .Take(safeLimit)
            .Select(a => new FailedLoginAttemptDto
            {
                Id = a.Id,
                Email = a.Email,
                IpAddress = a.IpAddress,
                FailureReason = a.FailureReason,
                UserAgent = a.UserAgent,
                AttemptedAt = a.AttemptedAt,
            })
            .ToListAsync();

        return Ok(failedLogins);
    }

    [HttpGet("sessions")]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<IActionResult> GetSecuritySessions()
    {
        var sessions = await _sessionService.GetActiveSessionsAsync();
        return Ok(sessions);
    }

    [HttpGet("summary")]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<IActionResult> GetSecuritySummary([FromQuery] int sinceHours = 24)
    {
        var summary = await _deviceSecurityService.GetSecuritySummaryAsync(sinceHours);
        return Ok(summary);
    }

    [HttpGet("devices")]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<IActionResult> GetDevices()
    {
        var devices = await _deviceSecurityService.GetDevicesAsync();
        return Ok(devices);
    }

    [HttpPost("devices")]
    [HasPermission(AppPermissions.Users.Manage)]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterTerminalDeviceRequest request)
    {
        var device = await _deviceSecurityService.RegisterDeviceAsync(request, _currentUser.UserId);
        return Ok(device);
    }

    [HttpPost("devices/{deviceId}/actions")]
    [HasPermission(AppPermissions.Users.Manage)]
    public async Task<IActionResult> ExecuteDeviceAction(string deviceId, [FromBody] DeviceActionRequest request)
    {
        var device = await _deviceSecurityService.ExecuteDeviceActionAsync(deviceId, request, _currentUser.UserId);
        return Ok(device);
    }

    [HttpPost("devices/scan-outdated")]
    [HasPermission(AppPermissions.Users.Manage)]
    public async Task<IActionResult> ScanOutdatedDevices()
    {
        var result = await _deviceSecurityService.ScanOutdatedDevicesAsync(_currentUser.UserId);
        return Ok(result);
    }

    [HttpGet("irregular-transactions")]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<IActionResult> GetIrregularTransactions([FromQuery] int hours = 72, [FromQuery] int limit = 100)
    {
        var irregularities = await _deviceSecurityService.GetIrregularTransactionsAsync(hours, limit);
        return Ok(irregularities);
    }
}
