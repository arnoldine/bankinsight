namespace CoreBanker.Services
{
    public class RiskService : ApiClientBase
    {
        public RiskService(HttpClient httpClient) : base(httpClient) { }

        public async Task<RiskDashboardDto?> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<RiskDashboardDto>("/api/RiskAnalytics/dashboard", cancellationToken);
        }

        public async Task<List<RiskMetricDto>> GetMetricsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<RiskMetricDto>>("/api/RiskAnalytics", cancellationToken);
            return result ?? new List<RiskMetricDto>();
        }

        public async Task<List<RiskMetricDto>> GetAlertsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<RiskMetricDto>>("/api/RiskAnalytics/alerts", cancellationToken);
            return result ?? new List<RiskMetricDto>();
        }

        public async Task<RiskMetricDto?> CalculateVarAsync(DateTime metricDate, string currency, decimal confidenceLevel = 95m, int timeHorizonDays = 1, CancellationToken cancellationToken = default)
        {
            var path = $"/api/RiskAnalytics/var?metricDate={Uri.EscapeDataString(metricDate.ToString("O"))}&currency={Uri.EscapeDataString(currency)}&confidenceLevel={confidenceLevel}&timeHorizonDays={timeHorizonDays}";
            return await GetAsync<RiskMetricDto>(path, cancellationToken);
        }

        public async Task<RiskMetricDto?> CalculateLcrAsync(DateTime metricDate, CancellationToken cancellationToken = default)
        {
            var path = $"/api/RiskAnalytics/lcr?metricDate={Uri.EscapeDataString(metricDate.ToString("O"))}";
            return await GetAsync<RiskMetricDto>(path, cancellationToken);
        }

        public async Task<RiskMetricDto?> CalculateCurrencyExposureAsync(DateTime metricDate, string currency, CancellationToken cancellationToken = default)
        {
            var path = $"/api/RiskAnalytics/currency-exposure?metricDate={Uri.EscapeDataString(metricDate.ToString("O"))}&currency={Uri.EscapeDataString(currency)}";
            return await GetAsync<RiskMetricDto>(path, cancellationToken);
        }

        public async Task RunDailyCalculationsAsync(CancellationToken cancellationToken = default)
        {
            await PostAsync<object, object>("/api/RiskAnalytics/daily-calculations", new { }, cancellationToken);
        }

        public async Task<AccountFeeDto?> AssessAccountFeeAsync(AssessAccountFeeRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<AssessAccountFeeRequest, AccountFeeDto>("/api/fees", request, cancellationToken);
        }
    }

    public record RiskMetricDto(
        int Id,
        DateTime MetricDate,
        string MetricType,
        string? Currency,
        decimal MetricValue,
        decimal? Threshold,
        bool ThresholdBreached,
        decimal? ConfidenceLevel,
        int? TimeHorizonDays,
        string? CalculationMethod,
        string Status,
        string? CalculatedByName,
        DateTime CalculatedAt,
        string? ReviewedByName,
        DateTime? ReviewedAt,
        bool AlertTriggered,
        string? Notes
    );

    public record RiskDashboardDto(
        DateTime AsOfDate,
        decimal VaRValue,
        decimal VaRThreshold,
        bool VaRBreached,
        decimal LcrValue,
        decimal LcrThreshold,
        bool LcrBreached,
        Dictionary<string, decimal> CurrencyExposure,
        List<RiskMetricDto> RecentAlerts
    );

    public class AssessAccountFeeRequest
    {
        public string FeeCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public string? Narration { get; set; }
        public string? ClientReference { get; set; }
    }

    public class AccountFeeDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string FeeCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Narration { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; }
    }
}
