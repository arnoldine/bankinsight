using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BankInsight.API.Services;

public interface IRiskAnalyticsService
{
    Task<RiskMetricDto> CalculateVaRAsync(DateTime metricDate, string currency, decimal confidenceLevel, int timeHorizonDays);
    Task<RiskMetricDto> CalculateLcrAsync(DateTime metricDate);
    Task<RiskMetricDto> CalculateCurrencyExposureAsync(DateTime metricDate, string currency);
    Task<RiskMetricDto> CreateMetricAsync(string? calculatedBy, CreateRiskMetricRequest request);
    Task<RiskMetricDto> ReviewMetricAsync(string reviewedBy, ReviewRiskMetricRequest request);
    Task<RiskMetricDto?> GetMetricAsync(int id);
    Task<List<RiskMetricDto>> GetMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? metricType = null);
    Task<List<RiskMetricDto>> GetAlertsAsync(DateTime? fromDate = null);
    Task<RiskDashboardDto> GetRiskDashboardAsync(DateTime? asOfDate = null);
    Task RunDailyRiskCalculationsAsync();
}

public class RiskAnalyticsService : IRiskAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RiskAnalyticsService> _logger;

    public RiskAnalyticsService(ApplicationDbContext context, ILogger<RiskAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RiskMetricDto> CalculateVaRAsync(
        DateTime metricDate, 
        string currency, 
        decimal confidenceLevel, 
        int timeHorizonDays)
    {
        _logger.LogInformation(
            "Calculating VaR for {Currency} at {Confidence}% confidence over {Horizon} days",
            currency, confidenceLevel, timeHorizonDays);

        // Get historical positions for the currency
        var historicalPositions = await _context.TreasuryPositions
            .Where(p => p.Currency == currency.ToUpper() 
                     && p.PositionDate <= metricDate.Date 
                     && p.PositionDate >= metricDate.Date.AddDays(-90))
            .OrderBy(p => p.PositionDate)
            .ToListAsync();

        if (historicalPositions.Count < 30)
        {
            throw new InvalidOperationException(
                $"Insufficient historical data for VaR calculation. Need at least 30 days, found {historicalPositions.Count}");
        }

        // Calculate daily returns
        var returns = new List<decimal>();
        for (int i = 1; i < historicalPositions.Count; i++)
        {
            var previousBalance = historicalPositions[i - 1].ClosingBalance;
            var currentBalance = historicalPositions[i].ClosingBalance;
            
            if (previousBalance != 0)
            {
                var dailyReturn = (currentBalance - previousBalance) / previousBalance;
                returns.Add(dailyReturn);
            }
        }

        // Historical VaR calculation
        returns.Sort();
        var percentileIndex = (int)Math.Ceiling((1 - confidenceLevel / 100) * returns.Count) - 1;
        percentileIndex = Math.Max(0, Math.Min(percentileIndex, returns.Count - 1));
        
        var varReturn = returns[percentileIndex];
        var currentPosition = historicalPositions.Last().ClosingBalance;
        var varValue = Math.Abs(currentPosition * varReturn * (decimal)Math.Sqrt(timeHorizonDays));

        // Set threshold (example: 5% of current position)
        var threshold = Math.Abs(currentPosition * 0.05m);

        var positionSnapshot = JsonSerializer.Serialize(new
        {
            Currency = currency,
            CurrentPosition = currentPosition,
            HistoricalDays = historicalPositions.Count,
            ReturnCount = returns.Count
        });

        var metric = new RiskMetric
        {
            MetricDate = metricDate.Date,
            MetricType = "VaR",
            Currency = currency.ToUpper(),
            MetricValue = varValue,
            Threshold = threshold,
            ThresholdBreached = varValue > threshold,
            ConfidenceLevel = confidenceLevel,
            TimeHorizonDays = timeHorizonDays,
            CalculationMethod = "Historical",
            PositionSnapshot = positionSnapshot,
            Status = varValue > threshold ? "Escalated" : "Calculated",
            AlertTriggered = varValue > threshold,
            AlertSentAt = varValue > threshold ? DateTime.UtcNow : null,
            CalculatedAt = DateTime.UtcNow
        };

        _context.RiskMetrics.Add(metric);
        await _context.SaveChangesAsync();

        _logger.LogInformation("VaR calculated: {VaR} (Threshold: {Threshold})", varValue, threshold);

        return await GetMetricDtoAsync(metric.Id);
    }

    public async Task<RiskMetricDto> CalculateLcrAsync(DateTime metricDate)
    {
        _logger.LogInformation("Calculating Liquidity Coverage Ratio (LCR) for {Date}", metricDate);

        // Get all currency positions
        var positions = await _context.TreasuryPositions
            .Where(p => p.PositionDate == metricDate.Date)
            .ToListAsync();

        // High Quality Liquid Assets (HQLA)
        var hqla = positions.Sum(p => Math.Max(0, p.VaultBalance ?? 0) + Math.Max(0, p.NostroBalance ?? 0));

        // Net Cash Outflows (simplified - would need more sophisticated calculation)
        var deposits = positions.Sum(p => p.Deposits);
        var withdrawals = positions.Sum(p => p.Withdrawals);
        var netCashOutflow = Math.Max(withdrawals - deposits * 0.95m, 1); // 5% runoff assumption

        // LCR = HQLA / Net Cash Outflows (should be >= 100%)
        var lcr = (hqla / netCashOutflow) * 100;

        var threshold = 100m; // Regulatory minimum

        var positionSnapshot = JsonSerializer.Serialize(new
        {
            HQLA = hqla,
            NetCashOutflow = netCashOutflow,
            Deposits = deposits,
            Withdrawals = withdrawals
        });

        var metric = new RiskMetric
        {
            MetricDate = metricDate.Date,
            MetricType = "LCR",
            MetricValue = lcr,
            Threshold = threshold,
            ThresholdBreached = lcr < threshold,
            CalculationMethod = "Basel III",
            PositionSnapshot = positionSnapshot,
            Status = lcr < threshold ? "Escalated" : "Calculated",
            AlertTriggered = lcr < threshold,
            AlertSentAt = lcr < threshold ? DateTime.UtcNow : null,
            CalculatedAt = DateTime.UtcNow,
            Notes = "LCR = High Quality Liquid Assets / Net Cash Outflows over 30 days"
        };

        _context.RiskMetrics.Add(metric);
        await _context.SaveChangesAsync();

        _logger.LogInformation("LCR calculated: {LCR}% (Threshold: {Threshold}%)", lcr, threshold);

        return await GetMetricDtoAsync(metric.Id);
    }

    public async Task<RiskMetricDto> CalculateCurrencyExposureAsync(DateTime metricDate, string currency)
    {
        _logger.LogInformation("Calculating currency exposure for {Currency}", currency);

        var position = await _context.TreasuryPositions
            .Where(p => p.Currency == currency.ToUpper() && p.PositionDate == metricDate.Date)
            .FirstOrDefaultAsync();

        var exposureValue = position?.ClosingBalance ?? 0;
        var exposureLimit = position?.ExposureLimit ?? 0;

        var threshold = exposureLimit > 0 ? exposureLimit : decimal.MaxValue;

        var exposureDetails = JsonSerializer.Serialize(new
        {
            Currency = currency,
            Position = exposureValue,
            Limit = exposureLimit,
            UtilizationPercent = exposureLimit > 0 ? (exposureValue / exposureLimit) * 100 : 0
        });

        var metric = new RiskMetric
        {
            MetricDate = metricDate.Date,
            MetricType = "CurrencyExposure",
            Currency = currency.ToUpper(),
            MetricValue = Math.Abs(exposureValue),
            Threshold = threshold,
            ThresholdBreached = Math.Abs(exposureValue) > threshold,
            ExposureDetails = exposureDetails,
            Status = Math.Abs(exposureValue) > threshold ? "Escalated" : "Calculated",
            AlertTriggered = Math.Abs(exposureValue) > threshold,
            AlertSentAt = Math.Abs(exposureValue) > threshold ? DateTime.UtcNow : null,
            CalculatedAt = DateTime.UtcNow
        };

        _context.RiskMetrics.Add(metric);
        await _context.SaveChangesAsync();

        return await GetMetricDtoAsync(metric.Id);
    }

    public async Task<RiskMetricDto> CreateMetricAsync(string? calculatedBy, CreateRiskMetricRequest request)
    {
        var metric = new RiskMetric
        {
            MetricDate = request.MetricDate.Date.ToUniversalTime(),
            MetricType = request.MetricType,
            Currency = request.Currency?.ToUpper(),
            MetricValue = request.MetricValue,
            Threshold = request.Threshold,
            ThresholdBreached = request.Threshold.HasValue && request.MetricValue > request.Threshold.Value,
            ConfidenceLevel = request.ConfidenceLevel,
            TimeHorizonDays = request.TimeHorizonDays,
            CalculationMethod = request.CalculationMethod,
            PositionSnapshot = request.PositionSnapshot,
            ExposureDetails = request.ExposureDetails,
            CalculatedBy = calculatedBy,
            Status = "Calculated",
            Notes = request.Notes,
            AlertTriggered = request.Threshold.HasValue && request.MetricValue > request.Threshold.Value,
            CalculatedAt = DateTime.UtcNow
        };

        if (metric.AlertTriggered)
            metric.AlertSentAt = DateTime.UtcNow;

        _context.RiskMetrics.Add(metric);
        await _context.SaveChangesAsync();

        return await GetMetricDtoAsync(metric.Id);
    }

    public async Task<RiskMetricDto> ReviewMetricAsync(string reviewedBy, ReviewRiskMetricRequest request)
    {
        var metric = await _context.RiskMetrics
            .Include(m => m.Calculator)
            .Include(m => m.Reviewer)
            .FirstOrDefaultAsync(m => m.Id == request.MetricId);

        if (metric == null)
            throw new InvalidOperationException($"Risk metric with ID {request.MetricId} not found");

        metric.Status = request.Status;
        metric.ReviewedBy = reviewedBy;
        metric.ReviewedAt = DateTime.UtcNow;
        metric.Notes = string.IsNullOrEmpty(metric.Notes) 
            ? request.Notes 
            : $"{metric.Notes}\n{request.Notes}";

        await _context.SaveChangesAsync();

        return MapToDto(metric);
    }

    public async Task<RiskMetricDto?> GetMetricAsync(int id)
    {
        return await GetMetricDtoAsync(id);
    }

    public async Task<List<RiskMetricDto>> GetMetricsAsync(
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        string? metricType = null)
    {
        var query = _context.RiskMetrics
            .Include(m => m.Calculator)
            .Include(m => m.Reviewer)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(m => m.MetricDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(m => m.MetricDate <= toDate.Value.Date);

        if (!string.IsNullOrEmpty(metricType))
            query = query.Where(m => m.MetricType == metricType);

        var metrics = await query
            .OrderByDescending(m => m.MetricDate)
            .ThenByDescending(m => m.CalculatedAt)
            .ToListAsync();

        return metrics.Select(MapToDto).ToList();
    }

    public async Task<List<RiskMetricDto>> GetAlertsAsync(DateTime? fromDate = null)
    {
        var query = _context.RiskMetrics
            .Include(m => m.Calculator)
            .Include(m => m.Reviewer)
            .Where(m => m.AlertTriggered);

        if (fromDate.HasValue)
            query = query.Where(m => m.MetricDate >= fromDate.Value.Date);

        var metrics = await query
            .OrderByDescending(m => m.MetricDate)
            .ToListAsync();

        return metrics.Select(MapToDto).ToList();
    }

    public async Task<RiskDashboardDto> GetRiskDashboardAsync(DateTime? asOfDate = null)
    {
        var targetDate = asOfDate?.Date ?? DateTime.UtcNow.Date;

        var metrics = await _context.RiskMetrics
            .Include(m => m.Calculator)
            .Include(m => m.Reviewer)
            .Where(m => m.MetricDate == targetDate)
            .ToListAsync();

        var varMetric = metrics.FirstOrDefault(m => m.MetricType == "VaR");
        var lcrMetric = metrics.FirstOrDefault(m => m.MetricType == "LCR");

        var currencyExposures = metrics
            .Where(m => m.MetricType == "CurrencyExposure" && m.Currency != null)
            .ToDictionary(m => m.Currency!, m => m.MetricValue);

        var recentAlerts = await _context.RiskMetrics
            .Include(m => m.Calculator)
            .Include(m => m.Reviewer)
            .Where(m => m.AlertTriggered && m.MetricDate >= targetDate.AddDays(-7))
            .OrderByDescending(m => m.MetricDate)
            .Take(10)
            .ToListAsync();

        return new RiskDashboardDto(
            targetDate,
            varMetric?.MetricValue ?? 0,
            varMetric?.Threshold ?? 0,
            varMetric?.ThresholdBreached ?? false,
            lcrMetric?.MetricValue ?? 0,
            lcrMetric?.Threshold ?? 100,
            lcrMetric?.ThresholdBreached ?? false,
            currencyExposures,
            recentAlerts.Select(MapToDto).ToList()
        );
    }

    public async Task RunDailyRiskCalculationsAsync()
    {
        var calculationDate = DateTime.UtcNow.Date;
        _logger.LogInformation("Running daily risk calculations for {Date}", calculationDate);

        try
        {
            // Calculate VaR for main currencies
            var currencies = new[] { "USD", "EUR", "GBP" };
            foreach (var currency in currencies)
            {
                try
                {
                    // Check if positions exist
                    var hasPositions = await _context.TreasuryPositions
                        .AnyAsync(p => p.Currency == currency && p.PositionDate <= calculationDate);

                    if (hasPositions)
                    {
                        await CalculateVaRAsync(calculationDate, currency, 95m, 1);
                        await CalculateCurrencyExposureAsync(calculationDate, currency);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating VaR for {Currency}", currency);
                }
            }

            // Calculate LCR
            var hasAnyPositions = await _context.TreasuryPositions
                .AnyAsync(p => p.PositionDate == calculationDate);

            if (hasAnyPositions)
            {
                await CalculateLcrAsync(calculationDate);
            }

            _logger.LogInformation("Daily risk calculations completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running daily risk calculations");
            throw;
        }
    }

    private async Task<RiskMetricDto> GetMetricDtoAsync(int id)
    {
        var metric = await _context.RiskMetrics
            .Include(m => m.Calculator)
            .Include(m => m.Reviewer)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (metric == null)
            throw new InvalidOperationException($"Risk metric with ID {id} not found");

        return MapToDto(metric);
    }

    private static RiskMetricDto MapToDto(RiskMetric metric)
    {
        return new RiskMetricDto(
            metric.Id,
            metric.MetricDate,
            metric.MetricType,
            metric.Currency,
            metric.MetricValue,
            metric.Threshold,
            metric.ThresholdBreached,
            metric.ConfidenceLevel,
            metric.TimeHorizonDays,
            metric.CalculationMethod,
            metric.Status,
            metric.Calculator?.Name,
            metric.CalculatedAt,
            metric.Reviewer?.Name,
            metric.ReviewedAt,
            metric.AlertTriggered,
            metric.Notes
        );
    }
}
