using System.Threading.Tasks;
using System.Security.Claims;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/approvals")]
public class ApprovalController : ControllerBase
{
    private readonly ApprovalService _approvalService;

    public ApprovalController(ApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet]
    [RequirePermission("VIEW_APPROVALS")]
    public async Task<IActionResult> GetApprovals()
    {
        var approvals = await _approvalService.GetApprovalsAsync();
        return Ok(approvals);
    }

    [HttpPost]
    [RequirePermission("CREATE_APPROVALS")]
    public async Task<IActionResult> CreateApproval([FromBody] CreateApprovalRequest request)
    {
        var approval = await _approvalService.CreateApprovalAsync(request);
        return StatusCode(201, approval);
    }

    [HttpPut("{id}")]
    [RequirePermission("MANAGE_APPROVALS")]
    public async Task<IActionResult> UpdateApproval(string id, [FromBody] UpdateApprovalRequest request)
    {
        var actingUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var approval = await _approvalService.UpdateApprovalAsync(id, request, actingUserId);
        if (approval == null) return NotFound(new { message = "Approval request not found" });
        return Ok(approval);
    }
}
