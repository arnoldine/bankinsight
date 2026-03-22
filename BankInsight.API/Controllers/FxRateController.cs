using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BankInsight.API.Services;
using BankInsight.API.DTOs;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FxRateController : ControllerBase
{
    private readonly IFxRateService _fxRateService;

    public FxRateController(IFxRateService fxRateService)
    {
        _fxRateService = fxRateService;
    }

    [HttpPost]
    public async Task<ActionResult<FxRateDto>> CreateRate([FromBody] CreateFxRateRequest request)
    {
        var rate = await _fxRateService.CreateRateAsync(request);
        return CreatedAtAction(nameof(GetRate), new { id = rate.Id }, rate);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FxRateDto>> UpdateRate(int id, [FromBody] UpdateFxRateRequest request)
    {
        try
        {
            var rate = await _fxRateService.UpdateRateAsync(id, request);
            return Ok(rate);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FxRateDto>> GetRate(int id)
    {
        var rate = await _fxRateService.GetRateAsync(id);
        if (rate == null)
            return NotFound();
        
        return Ok(rate);
    }

    [HttpGet]
    public async Task<ActionResult<List<FxRateDto>>> GetAllRates(
        [FromQuery] DateTime? rateDate = null,
        [FromQuery] bool activeOnly = true)
    {
        var rates = await _fxRateService.GetAllRatesAsync(rateDate, activeOnly);
        return Ok(rates);
    }

    [HttpGet("latest/{baseCurrency}/{targetCurrency}")]
    public async Task<ActionResult<FxRateDto>> GetLatestRate(string baseCurrency, string targetCurrency)
    {
        var rate = await _fxRateService.GetLatestRateAsync(baseCurrency, targetCurrency);
        if (rate == null)
            return NotFound($"No rate found for {baseCurrency}/{targetCurrency}");
        
        return Ok(rate);
    }

    [HttpGet("history/{baseCurrency}/{targetCurrency}")]
    public async Task<ActionResult<List<FxRateHistoryDto>>> GetRateHistory(
        string baseCurrency,
        string targetCurrency,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var history = await _fxRateService.GetRateHistoryAsync(baseCurrency, targetCurrency, fromDate, toDate);
        return Ok(history);
    }

    [HttpPost("convert")]
    public async Task<ActionResult<decimal>> ConvertCurrency(
        [FromQuery] decimal amount,
        [FromQuery] string fromCurrency,
        [FromQuery] string toCurrency,
        [FromQuery] DateTime? rateDate = null)
    {
        try
        {
            var result = await _fxRateService.ConvertCurrencyAsync(amount, fromCurrency, toCurrency, rateDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRate(int id)
    {
        var result = await _fxRateService.DeleteRateAsync(id);
        if (!result)
            return NotFound();
        
        return NoContent();
    }

    [HttpPost("sync-bog")]
    public async Task<ActionResult<List<FxRateDto>>> SyncRatesFromBog([FromQuery] DateTime? rateDate = null)
    {
        try
        {
            var targetDate = rateDate ?? DateTime.UtcNow;
            var rates = await _fxRateService.SyncRatesFromBogAsync(targetDate);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
