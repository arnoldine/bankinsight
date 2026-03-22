using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BankInsight.API.Services;
using BankInsight.API.DTOs;
using System.Security.Claims;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RiskAnalyticsController : ControllerBase
{
    private readonly IRiskAnalyticsService _riskService;

    public RiskAnalyticsController(IRiskAnalyticsService riskService)
    {
        _riskService = riskService;
    }

    [HttpGet("var")]
    public async Task<ActionResult<RiskMetricDto>> CalculateVaR(
        [FromQuery] DateTime metricDate,
        [FromQuery] string currency,
        [FromQuery] decimal confidenceLevel = 95m,
        [FromQuery] int timeHorizonDays = 1)
    {
        try
        {
            var metric = await _riskService.CalculateVaRAsync(metricDate, currency, confidenceLevel, timeHorizonDays);
            return Ok(metric);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("lcr")]
    public async Task<ActionResult<RiskMetricDto>> CalculateLcr([FromQuery] DateTime metricDate)
    {
        var metric = await _riskService.CalculateLcrAsync(metricDate);
        return Ok(metric);
    }

    [HttpGet("currency-exposure")]
    public async Task<ActionResult<RiskMetricDto>> CalculateCurrencyExposure(
        [FromQuery] DateTime metricDate,
        [FromQuery] string currency)
    {
        var metric = await _riskService.CalculateCurrencyExposureAsync(metricDate, currency);
        return Ok(metric);
    }

    [HttpPost]
    public async Task<ActionResult<RiskMetricDto>> CreateMetric([FromBody] CreateRiskMetricRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var metric = await _riskService.CreateMetricAsync(userId, request);
        return CreatedAtAction(nameof(GetMetric), new { id = metric.Id }, metric);
    }

    [HttpPost("review")]
    public async Task<ActionResult<RiskMetricDto>> ReviewMetric([FromBody] ReviewRiskMetricRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
            var metric = await _riskService.ReviewMetricAsync(userId, request);
            return Ok(metric);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RiskMetricDto>> GetMetric(int id)
    {
        var metric = await _riskService.GetMetricAsync(id);
        if (metric == null)
            return NotFound();
        
        return Ok(metric);
    }

    [HttpGet]
    public async Task<ActionResult<List<RiskMetricDto>>> GetMetrics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? metricType = null)
    {
        var metrics = await _riskService.GetMetricsAsync(fromDate, toDate, metricType);
        return Ok(metrics);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<List<RiskMetricDto>>> GetAlerts([FromQuery] DateTime? fromDate = null)
    {
        var alerts = await _riskService.GetAlertsAsync(fromDate);
        return Ok(alerts);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<RiskDashboardDto>> GetRiskDashboard([FromQuery] DateTime? asOfDate = null)
    {
        var dashboard = await _riskService.GetRiskDashboardAsync(asOfDate);
        return Ok(dashboard);
    }

    [HttpPost("daily-calculations")]
    public async Task<ActionResult> RunDailyCalculations()
    {
        try
        {
            await _riskService.RunDailyRiskCalculationsAsync();
            return Ok(new { message = "Daily risk calculations completed" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
