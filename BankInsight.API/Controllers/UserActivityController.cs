using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserActivityController : ControllerBase
{
    private readonly IUserActivityService _activityService;

    public UserActivityController(IUserActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpPost]
    public async Task<ActionResult> LogActivity([FromBody] CreateActivityRequest request)
    {
        var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(staffId))
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        await _activityService.LogActivityAsync(staffId, request, ipAddress, userAgent, null);

        return Ok(new { message = "Activity logged successfully" });
    }

    [HttpGet("user/{staffId}")]
    public async Task<ActionResult<List<UserActivityDto>>> GetUserActivities(string staffId, [FromQuery] int limit = 100)
    {
        var activities = await _activityService.GetUserActivitiesAsync(staffId, limit);
        return Ok(activities);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<List<UserActivityDto>>> GetRecentActivities([FromQuery] int limit = 100)
    {
        var activities = await _activityService.GetRecentActivitiesAsync(limit);
        return Ok(activities);
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<List<UserActivityDto>>> GetActivitiesByEntity(string entityType, string entityId, [FromQuery] int limit = 100)
    {
        var activities = await _activityService.GetActivitiesByEntityAsync(entityType, entityId, limit);
        return Ok(activities);
    }

    [HttpGet("report/{staffId}")]
    public async Task<ActionResult<UserActivityReportDto>> GetUserActivityReport(
        string staffId, 
        [FromQuery] DateTime? fromDate, 
        [FromQuery] DateTime? toDate)
    {
        var report = await _activityService.GetUserActivityReportAsync(staffId, fromDate, toDate);
        return Ok(report);
    }
}
