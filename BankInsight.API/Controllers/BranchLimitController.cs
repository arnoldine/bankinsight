using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankInsight.API.DTOs;
using BankInsight.API.Services;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchLimitController : ControllerBase
{
    private readonly IBranchLimitService _limitService;

    public BranchLimitController(IBranchLimitService limitService)
    {
        _limitService = limitService;
    }

    [HttpPost]
    public async Task<ActionResult<BranchLimitDto>> CreateLimit([FromBody] CreateBranchLimitRequest request)
    {
        try
        {
            var limit = await _limitService.CreateLimitAsync(request);
            return Ok(limit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BranchLimitDto>> UpdateLimit(int id, [FromBody] CreateBranchLimitRequest request)
    {
        try
        {
            var limit = await _limitService.UpdateLimitAsync(id, request);
            return Ok(limit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BranchLimitDto>> GetLimit(int id)
    {
        var limit = await _limitService.GetLimitAsync(id);
        
        if (limit == null)
        {
            return NotFound(new { message = "Limit not found" });
        }

        return Ok(limit);
    }

    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<List<BranchLimitDto>>> GetBranchLimits(string branchId)
    {
        var limits = await _limitService.GetBranchLimitsAsync(branchId);
        return Ok(limits);
    }

    [HttpGet]
    public async Task<ActionResult<List<BranchLimitDto>>> GetAllLimits()
    {
        var limits = await _limitService.GetAllLimitsAsync();
        return Ok(limits);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteLimit(int id)
    {
        var result = await _limitService.DeleteLimitAsync(id);
        
        if (!result)
        {
            return NotFound(new { message = "Limit not found" });
        }

        return Ok(new { message = "Limit deleted successfully" });
    }

    [HttpPost("validate")]
    public async Task<ActionResult<bool>> ValidateTransaction([FromBody] ValidateTransactionRequest request)
    {
        var isValid = await _limitService.ValidateTransactionAgainstLimitsAsync(
            request.BranchId, 
            request.TransactionType, 
            request.Amount, 
            request.Currency);

        return Ok(new { isValid });
    }
}

public class ValidateTransactionRequest
{
    public string BranchId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
}
