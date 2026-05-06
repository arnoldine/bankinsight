using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/microfinance")]
public class MicrofinanceController : ControllerBase
{
    private readonly MicrofinanceService _service;

    public MicrofinanceController(MicrofinanceService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? collectorStaffId, CancellationToken cancellationToken)
        => Ok(await _service.GetSummaryAsync(collectorStaffId, cancellationToken));

    [HttpGet("customers/search")]
    public async Task<IActionResult> SearchCustomers([FromQuery] string query, CancellationToken cancellationToken)
        => Ok(await _service.SearchCustomersAsync(query, cancellationToken));

    [HttpGet("accounts/search")]
    public async Task<IActionResult> SearchAccounts([FromQuery] string query, [FromQuery] string? customerId, CancellationToken cancellationToken)
        => Ok(await _service.SearchAccountsAsync(query, customerId, cancellationToken));

    [HttpGet("loan-policies")]
    public async Task<IActionResult> GetLoanPolicies(CancellationToken cancellationToken)
        => Ok(await _service.GetLoanPoliciesAsync(cancellationToken));

    [HttpPost("assignments")]
    public async Task<IActionResult> UpsertAssignment([FromBody] UpsertCollectorAssignmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.UpsertAssignmentAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("batches")]
    public async Task<IActionResult> OpenBatch([FromBody] OpenFieldCollectionBatchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.OpenBatchAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("batches/{batchId}/collections")]
    public async Task<IActionResult> RecordCollection(string batchId, [FromBody] RecordFieldCollectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RecordCollectionAsync(batchId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("batches/{batchId}/submit")]
    public async Task<IActionResult> SubmitBatch(string batchId, [FromBody] SubmitFieldCollectionBatchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.SubmitBatchAsync(batchId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("batches/{batchId}/settle")]
    public async Task<IActionResult> SettleBatch(string batchId, [FromBody] SettleFieldCollectionBatchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.SettleBatchAsync(batchId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
