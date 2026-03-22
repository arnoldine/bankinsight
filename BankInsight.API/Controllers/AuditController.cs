using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditLoggingService _auditLoggingService;

    public AuditController(IAuditLoggingService auditLoggingService)
    {
        _auditLoggingService = auditLoggingService;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int limit = 100)
    {
        var logs = await _auditLoggingService.GetAuditLogsAsync(limit);

        var result = logs.Select(log => new
        {
            id = log.Id.ToString(),
            timestamp = log.CreatedAt,
            user = log.User?.Name ?? log.UserId ?? "System",
            action = log.Action,
            details = log.Description ?? log.ErrorMessage ?? string.Empty,
            module = log.EntityType,
            status = log.Status == "FAILED" ? "FAILURE" : log.Status,
            entityType = log.EntityType,
            entityId = log.EntityId,
            ipAddress = log.IpAddress,
            oldValues = log.OldValues,
            newValues = log.NewValues,
            errorMessage = log.ErrorMessage
        });

        return Ok(result);
    }
}
