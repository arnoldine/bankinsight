using System.Globalization;

namespace CoreBanker.Services
{
    public class AdminService : ApiClientBase
    {
        public AdminService(HttpClient httpClient) : base(httpClient) { }

        public async Task<List<ConfigItemDto>> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<ConfigItemDto>>("/api/config", cancellationToken);
            return result ?? new List<ConfigItemDto>();
        }

        public async Task UpdateConfigAsync(List<ConfigItemDto> items, CancellationToken cancellationToken = default)
        {
            await PostAsync<List<ConfigItemDto>, object>("/api/config", items, cancellationToken);
        }

        public Task<OrassProfileDto?> GetOrassProfileAsync(CancellationToken cancellationToken = default)
            => GetAsync<OrassProfileDto>("/api/orass/profile", cancellationToken);

        public Task<OrassReadinessDto?> GetOrassReadinessAsync(CancellationToken cancellationToken = default)
            => GetAsync<OrassReadinessDto>("/api/orass/readiness", cancellationToken);

        public async Task<List<OrassQueueItemDto>> GetOrassQueueAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<OrassQueueItemDto>>("/api/orass/queue", cancellationToken);
            return result ?? new List<OrassQueueItemDto>();
        }

        public async Task<List<OrassSubmissionHistoryItemDto>> GetOrassHistoryAsync(int take = 20, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<OrassSubmissionHistoryItemDto>>($"/api/orass/history?take={take}", cancellationToken);
            return result ?? new List<OrassSubmissionHistoryItemDto>();
        }

        public Task<OrassSubmissionHistoryItemDto?> SubmitOrassReturnAsync(int returnId, CancellationToken cancellationToken = default)
            => PostAsync<object, OrassSubmissionHistoryItemDto>($"/api/orass/submit/{returnId}", new { }, cancellationToken);

        public Task<OrassSubmissionEvidenceDto?> GetOrassEvidenceAsync(int returnId, CancellationToken cancellationToken = default)
            => GetAsync<OrassSubmissionEvidenceDto>($"/api/orass/evidence/{returnId}", cancellationToken);

        public Task<OrassSubmissionHistoryItemDto?> AcknowledgeOrassReturnAsync(int returnId, UpdateOrassAcknowledgementRequest request, CancellationToken cancellationToken = default)
            => PostAsync<UpdateOrassAcknowledgementRequest, OrassSubmissionHistoryItemDto>($"/api/orass/acknowledge/{returnId}", request, cancellationToken);

        public Task<OrassReconciliationResultDto?> ReconcileOrassAcknowledgementsAsync(CancellationToken cancellationToken = default)
            => PostAsync<object, OrassReconciliationResultDto>("/api/orass/reconcile", new { }, cancellationToken);

        public static bool GetBoolConfig(IEnumerable<ConfigItemDto> items, string key, bool defaultValue = false)
        {
            var raw = items.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
            return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
        }

        public static string GetStringConfig(IEnumerable<ConfigItemDto> items, string key, string defaultValue = "")
        {
            return items.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.Value ?? defaultValue;
        }

        public static decimal? GetDecimalConfig(IEnumerable<ConfigItemDto> items, string key)
        {
            var raw = items.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }
    }

    public class ConfigItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ConfigSnapshotDto
    {
        public bool LoanCreditBureauRequiredForApproval { get; set; }
        public bool EodSchedulerEnabled { get; set; }
        public string EodSchedulerTimeUtc { get; set; } = "23:00";
        public string EodSchedulerLastRunDate { get; set; } = string.Empty;
        public string ActiveCreditProvider { get; set; } = string.Empty;
        public decimal? DefaultTransactionLimit { get; set; }
    }

    public class OrassProfileDto
    {
        public bool Enabled { get; set; }
        public string InstitutionCode { get; set; } = string.Empty;
        public string SubmissionMode { get; set; } = "TEST";
        public string EndpointUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string CertificateAlias { get; set; } = string.Empty;
        public string SourceReportCode { get; set; } = string.Empty;
        public bool AutoSubmit { get; set; }
        public string CutoffTimeUtc { get; set; } = "17:00";
        public string FallbackEmail { get; set; } = string.Empty;
        public string? LastSubmissionAt { get; set; }
    }

    public class OrassReadinessDto
    {
        public bool ProfileConfigured { get; set; }
        public bool ReadyForSubmission { get; set; }
        public string SubmissionMode { get; set; } = "TEST";
        public string SourceReportCode { get; set; } = string.Empty;
        public int PendingReturns { get; set; }
        public int ReturnsReadyForSubmission { get; set; }
        public string[] MissingRequirements { get; set; } = [];
        public string[] Notes { get; set; } = [];
        public string? LastPreparedReturnDate { get; set; }
        public string? LastSubmissionAt { get; set; }
    }

    public class OrassQueueItemDto
    {
        public int Id { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public string ReportingPeriodStart { get; set; } = string.Empty;
        public string ReportingPeriodEnd { get; set; } = string.Empty;
        public string SubmissionStatus { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public bool IsReadyForSubmission { get; set; }
        public string ValidationStatus { get; set; } = string.Empty;
        public string[] ValidationMessages { get; set; } = [];
    }

    public class OrassSubmissionHistoryItemDto
    {
        public int Id { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public string SubmissionStatus { get; set; } = string.Empty;
        public string? SubmissionDate { get; set; }
        public string SubmittedBy { get; set; } = string.Empty;
        public string BogReferenceNumber { get; set; } = string.Empty;
        public string TransportStatus { get; set; } = string.Empty;
        public string AcknowledgementStatus { get; set; } = string.Empty;
        public string? AcknowledgementReference { get; set; }
        public string? AcknowledgedAt { get; set; }
        public string? TransportMessage { get; set; }
        public string[] ValidationMessages { get; set; } = [];
    }

    public class OrassSubmissionEvidenceDto
    {
        public int ReturnId { get; set; }
        public string TransmissionId { get; set; } = string.Empty;
        public string SubmissionMode { get; set; } = string.Empty;
        public string EndpointUrl { get; set; } = string.Empty;
        public string TransportStatus { get; set; } = string.Empty;
        public string AcknowledgementStatus { get; set; } = string.Empty;
        public string? AcknowledgementReference { get; set; }
        public string? SubmittedAt { get; set; }
        public string? AcknowledgedAt { get; set; }
        public string? PayloadHash { get; set; }
        public string? ProviderStatusCode { get; set; }
        public string? TransportMessage { get; set; }
        public string[] Notes { get; set; } = [];
    }

    public class OrassReconciliationResultDto
    {
        public int ScannedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int PendingCount { get; set; }
        public string ExecutionMode { get; set; } = string.Empty;
        public string ExecutedAt { get; set; } = string.Empty;
        public string[] Notes { get; set; } = [];
    }

    public class UpdateOrassAcknowledgementRequest
    {
        public string Status { get; set; } = "RECEIVED";
        public string? AcknowledgementReference { get; set; }
        public string? Message { get; set; }
    }
}
