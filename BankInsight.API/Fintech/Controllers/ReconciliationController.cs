using HybridTransfer.Application.DTOs;
using HybridTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/reconciliation")]
public sealed class ReconciliationController : ControllerBase
{
    private readonly ReconciliationService _reconciliationService;

    public ReconciliationController(ReconciliationService reconciliationService)
    {
        _reconciliationService = reconciliationService;
    }

    [HttpGet("items")]
    [ProducesResponseType(typeof(IEnumerable<ReconciliationItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReconciliationItemResponse>>> Items(CancellationToken cancellationToken)
    {
        var items = await _reconciliationService.GetOpenItemsAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ReconciliationItemResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReconciliationItemResponse>> Create([FromBody] ManualReconciliationRequest request, CancellationToken cancellationToken)
    {
        var item = await _reconciliationService.RegisterBreakAsync(request, cancellationToken);
        return Created($"/api/v1/reconciliation/items/{item.ReconciliationItemId}", item);
    }
}
