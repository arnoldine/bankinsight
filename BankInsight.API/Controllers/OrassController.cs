using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/orass")]
public class OrassController : ControllerBase
{
    private readonly IOrassService _orassService;

    public OrassController(IOrassService orassService)
    {
        _orassService = orassService;
    }

    [HttpGet("profile")]
    [RequirePermission("VIEW_CONFIG")]
    public async Task<ActionResult<OrassProfileDto>> GetProfile()
    {
        return Ok(await _orassService.GetProfileAsync());
    }

    [HttpGet("readiness")]
    [RequirePermission("VIEW_CONFIG")]
    public async Task<ActionResult<OrassReadinessDto>> GetReadiness()
    {
        return Ok(await _orassService.GetReadinessAsync());
    }

    [HttpGet("queue")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<IReadOnlyList<OrassQueueItemDto>>> GetQueue()
    {
        return Ok(await _orassService.GetQueueAsync());
    }

    [HttpGet("history")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<IReadOnlyList<OrassSubmissionHistoryItemDto>>> GetHistory([FromQuery] int take = 20)
    {
        return Ok(await _orassService.GetHistoryAsync(take));
    }

    [HttpPost("submit/{returnId:int}")]
    [HasPermission(AppPermissions.Reports.Submit)]
    public async Task<ActionResult<OrassSubmissionHistoryItemDto>> Submit(int returnId)
    {
        try
        {
            var result = await _orassService.SubmitAsync(returnId, User.Identity?.Name ?? "system");
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("evidence/{returnId:int}")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<OrassSubmissionEvidenceDto>> GetEvidence(int returnId)
    {
        var result = await _orassService.GetEvidenceAsync(returnId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("acknowledge/{returnId:int}")]
    [HasPermission(AppPermissions.Reports.Submit)]
    public async Task<ActionResult<OrassSubmissionHistoryItemDto>> UpdateAcknowledgement(int returnId, [FromBody] UpdateOrassAcknowledgementRequest request)
    {
        try
        {
            var result = await _orassService.UpdateAcknowledgementAsync(returnId, request, User.Identity?.Name ?? "system");
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reconcile")]
    [HasPermission(AppPermissions.Reports.Submit)]
    public async Task<ActionResult<OrassReconciliationResultDto>> ReconcileAcknowledgements()
    {
        return Ok(await _orassService.ReconcileAcknowledgementsAsync(User.Identity?.Name ?? "system"));
    }
}
