using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BankInsight.API.Services;
using BankInsight.API.DTOs;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FxTradingController : ControllerBase
{
    private readonly IFxTradingService _fxTradingService;

    public FxTradingController(IFxTradingService fxTradingService)
    {
        _fxTradingService = fxTradingService;
    }

    [HttpPost]
    public async Task<ActionResult<FxTradeDto>> CreateTrade([FromBody] CreateFxTradeRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
        var trade = await _fxTradingService.CreateTradeAsync(userId, request);
        return CreatedAtAction(nameof(GetTrade), new { id = trade.Id }, trade);
    }

    [HttpPost("approve")]
    public async Task<ActionResult<FxTradeDto>> ApproveTrade([FromBody] ApproveFxTradeRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
            var trade = await _fxTradingService.ApproveTradeAsync(userId, request);
            return Ok(trade);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("settle")]
    public async Task<ActionResult<FxTradeDto>> SettleTrade([FromBody] SettleFxTradeRequest request)
    {
        try
        {
            var trade = await _fxTradingService.SettleTradeAsync(request);
            return Ok(trade);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FxTradeDto>> GetTrade(int id)
    {
        var trade = await _fxTradingService.GetTradeAsync(id);
        if (trade == null)
            return NotFound();
        
        return Ok(trade);
    }

    [HttpGet("deal/{dealNumber}")]
    public async Task<ActionResult<FxTradeDto>> GetTradeByDealNumber(string dealNumber)
    {
        var trade = await _fxTradingService.GetTradeByDealNumberAsync(dealNumber);
        if (trade == null)
            return NotFound();
        
        return Ok(trade);
    }

    [HttpGet]
    public async Task<ActionResult<List<FxTradeDto>>> GetTrades(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? status = null)
    {
        var trades = await _fxTradingService.GetTradesAsync(fromDate, toDate, status);
        return Ok(trades);
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<FxTradeDto>>> GetPendingTrades()
    {
        var trades = await _fxTradingService.GetPendingTradesAsync();
        return Ok(trades);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<FxTradeStatsDto>> GetTradeStats(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var stats = await _fxTradingService.GetTradeStatsAsync(fromDate, toDate);
        return Ok(stats);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelTrade(int id, [FromQuery] string reason)
    {
        try
        {
            var result = await _fxTradingService.CancelTradeAsync(id, reason);
            if (!result)
                return NotFound();
            
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
