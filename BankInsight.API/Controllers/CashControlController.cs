using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/cash-control")]
[Authorize]
public class CashControlController : ControllerBase
{
    private readonly ICashControlService _cashControlService;

    public CashControlController(ICashControlService cashControlService)
    {
        _cashControlService = cashControlService;
    }

    [HttpGet("vault-cash-position")]
    public async Task<ActionResult<List<VaultCashPositionDto>>> GetVaultCashPosition([FromQuery] string? branchId, [FromQuery] string currency = "GHS")
    {
        var positions = await _cashControlService.GetVaultCashPositionAsync(branchId, currency);
        return Ok(positions);
    }

    [HttpGet("branch-cash-summary")]
    public async Task<ActionResult<List<BranchCashSummaryDto>>> GetBranchCashSummary([FromQuery] string? branchId, [FromQuery] string currency = "GHS")
    {
        var summaries = await _cashControlService.GetBranchCashSummaryAsync(branchId, currency);
        return Ok(summaries);
    }

    [HttpGet("reconciliation")]
    public async Task<ActionResult<CashReconciliationSummaryDto>> GetCashReconciliation([FromQuery] string currency = "GHS")
    {
        var summary = await _cashControlService.GetCashReconciliationSummaryAsync(currency);
        return Ok(summary);
    }

    [HttpGet("transit-items")]
    public async Task<ActionResult<List<CashTransitItemDto>>> GetCashTransitItems([FromQuery] string currency = "GHS")
    {
        var items = await _cashControlService.GetCashTransitItemsAsync(currency);
        return Ok(items);
    }
}
