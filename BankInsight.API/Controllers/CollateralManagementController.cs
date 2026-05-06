using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/collateral-management")]
public class CollateralManagementController : ControllerBase
{
    private readonly CollateralManagementService _service;

    public CollateralManagementController(CollateralManagementService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    [RequirePermission("loans.view")]
    public async Task<ActionResult<CollateralManagementSummaryDto>> GetSummary()
    {
        return Ok(await _service.GetSummaryAsync());
    }

    [HttpPut("collateral/{id}")]
    [RequirePermission("loans.edit")]
    public async Task<ActionResult<CollateralRecordDto>> UpdateCollateral(string id, [FromBody] UpdateCollateralRecordRequest request)
    {
        var result = await _service.UpdateCollateralAsync(id, request);
        if (result == null)
        {
            return NotFound(new { message = "Collateral record not found" });
        }

        return Ok(result);
    }

    [HttpPut("covenants/{id}")]
    [RequirePermission("loans.edit")]
    public async Task<ActionResult<CovenantRecordDto>> UpdateCovenant(string id, [FromBody] UpdateCovenantRecordRequest request)
    {
        var result = await _service.UpdateCovenantAsync(id, request);
        if (result == null)
        {
            return NotFound(new { message = "Covenant record not found" });
        }

        return Ok(result);
    }
}
