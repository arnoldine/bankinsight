using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BankInsight.API.Services;
using BankInsight.API.DTOs;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TreasuryPositionController : ControllerBase
{
    private readonly ITreasuryPositionService _positionService;

    public TreasuryPositionController(ITreasuryPositionService positionService)
    {
        _positionService = positionService;
    }

    [HttpPost]
    public async Task<ActionResult<TreasuryPositionDto>> CreatePosition([FromBody] CreateTreasuryPositionRequest request)
    {
        try
        {
            var position = await _positionService.CreatePositionAsync(request);
            return CreatedAtAction(nameof(GetPosition), new { id = position.Id }, position);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Failed to create position: {ex.Message}", inner = ex.InnerException?.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TreasuryPositionDto>> UpdatePosition(int id, [FromBody] UpdateTreasuryPositionRequest request)
    {
        try
        {
            var position = await _positionService.UpdatePositionAsync(id, request);
            return Ok(position);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/reconcile")]
    public async Task<ActionResult<TreasuryPositionDto>> ReconcilePosition(int id, [FromBody] ReconcilePositionRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
            var position = await _positionService.ReconcilePositionAsync(id, userId, request);
            return Ok(position);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<List<PositionSummaryDto>>> GetPositionSummary()
    {
        var summary = await _positionService.GetPositionSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("latest/{currency}")]
    public async Task<ActionResult<TreasuryPositionDto>> GetLatestPosition(string currency)
    {
        var position = await _positionService.GetLatestPositionAsync(currency);
        if (position == null)
            return NotFound($"No position found for currency {currency}");
        
        return Ok(position);
    }

    [HttpGet]
    public async Task<ActionResult<List<TreasuryPositionDto>>> GetPositions(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? currency = null)
    {
        var positions = await _positionService.GetPositionsAsync(fromDate, toDate, currency);
        return Ok(positions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TreasuryPositionDto>> GetPosition(int id)
    {
        var position = await _positionService.GetPositionAsync(id);
        if (position == null)
            return NotFound();
        
        return Ok(position);
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<TreasuryPositionDto>> ClosePosition(int id, [FromQuery] decimal closingBalance)
    {
        try
        {
            var position = await _positionService.ClosePositionAsync(id, closingBalance);
            return Ok(position);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
