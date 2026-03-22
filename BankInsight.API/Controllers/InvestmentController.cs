using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BankInsight.API.Services;
using BankInsight.API.DTOs;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/treasury/investments")]
[Authorize]
public class InvestmentController : ControllerBase
{
    private readonly IInvestmentService _investmentService;

    public InvestmentController(IInvestmentService investmentService)
    {
        _investmentService = investmentService;
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentDto>> CreateInvestment([FromBody] CreateInvestmentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
        var investment = await _investmentService.CreateInvestmentAsync(userId, request);
        return CreatedAtAction(nameof(GetInvestment), new { id = investment.Id }, investment);
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<InvestmentDto>> ApproveInvestment(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
            var investment = await _investmentService.ApproveInvestmentAsync(userId, id);
            return Ok(investment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("rollover")]
    public async Task<ActionResult<InvestmentDto>> RolloverInvestment([FromBody] RolloverInvestmentRequest request)
    {
        try
        {
            var investment = await _investmentService.RolloverInvestmentAsync(request);
            return Ok(investment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("liquidate")]
    public async Task<ActionResult<InvestmentDto>> LiquidateInvestment([FromBody] LiquidateInvestmentRequest request)
    {
        try
        {
            var investment = await _investmentService.LiquidateInvestmentAsync(request);
            return Ok(investment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/mature")]
    public async Task<ActionResult<InvestmentDto>> MaturityInvestment(int id)
    {
        try
        {
            var investment = await _investmentService.MaturityInvestmentAsync(id);
            return Ok(investment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvestmentDto>> GetInvestment(int id)
    {
        var investment = await _investmentService.GetInvestmentAsync(id);
        if (investment == null)
            return NotFound();
        
        return Ok(investment);
    }

    [HttpGet("number/{investmentNumber}")]
    public async Task<ActionResult<InvestmentDto>> GetInvestmentByNumber(string investmentNumber)
    {
        var investment = await _investmentService.GetInvestmentByNumberAsync(investmentNumber);
        if (investment == null)
            return NotFound();
        
        return Ok(investment);
    }

    [HttpGet]
    public async Task<ActionResult<List<InvestmentDto>>> GetInvestments(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? currency = null)
    {
        var investments = await _investmentService.GetInvestmentsAsync(status, type, currency);
        return Ok(investments);
    }

    [HttpGet("maturing")]
    public async Task<ActionResult<List<InvestmentDto>>> GetMaturingInvestments(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var investments = await _investmentService.GetMaturingInvestmentsAsync(fromDate, toDate);
        return Ok(investments);
    }

    [HttpGet("portfolio")]
    public async Task<ActionResult<InvestmentPortfolioDto>> GetPortfolioSummary()
    {
        var portfolio = await _investmentService.GetPortfolioSummaryAsync();
        return Ok(portfolio);
    }

    [HttpPost("{id}/accrue")]
    public async Task<ActionResult> AccrueInterest(int id)
    {
        try
        {
            await _investmentService.AccrueInterestAsync(id);
            return Ok(new { message = "Interest accrued successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("accrue-all")]
    public async Task<ActionResult> RunDailyAccrual()
    {
        await _investmentService.RunDailyAccrualAsync();
        return Ok(new { message = "Daily accrual completed" });
    }
}
