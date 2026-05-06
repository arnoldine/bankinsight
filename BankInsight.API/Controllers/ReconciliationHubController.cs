using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/reconciliation-hub")]
public class ReconciliationHubController : ControllerBase
{
    private readonly ReconciliationHubService _service;

    public ReconciliationHubController(ReconciliationHubService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    [RequirePermission("gl.view")]
    public async Task<ActionResult<ReconciliationHubSummaryDto>> GetSummary()
    {
        return Ok(await _service.GetSummaryAsync());
    }

    [HttpPut("exceptions/{id}")]
    [RequirePermission("gl.post")]
    public async Task<ActionResult<ReconciliationExceptionDto>> UpdateException(string id, [FromBody] UpdateReconciliationExceptionRequest request)
    {
        var result = await _service.UpdateExceptionAsync(id, request);
        if (result == null)
        {
            return NotFound(new { message = "Reconciliation exception not found" });
        }

        return Ok(result);
    }

    [HttpPost("exceptions/{id}/retry")]
    [RequirePermission("gl.post")]
    public async Task<ActionResult<ReconciliationExceptionDto>> RetryException(string id, [FromBody] RetryReconciliationExceptionRequest request)
    {
        var result = await _service.RetryExceptionAsync(id, request);
        if (result == null)
        {
            return NotFound(new { message = "Reconciliation exception not found" });
        }

        return Ok(result);
    }

    [HttpPost("settlement-instructions")]
    [RequirePermission("gl.post")]
    public async Task<ActionResult<SettlementInstructionDto>> CreateSettlementInstruction([FromBody] CreateSettlementInstructionRequest request)
        => Ok(await _service.CreateSettlementInstructionAsync(request));
}
