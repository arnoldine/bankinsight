namespace CoreBanker.Services
{
    public class ReportingService : ApiClientBase
    {
        public ReportingService(HttpClient httpClient) : base(httpClient) { }

        public async Task<List<ReportDefinitionDto>> GetReportCatalogAsync(string? reportType = null, CancellationToken cancellationToken = default)
        {
            var requestUri = string.IsNullOrWhiteSpace(reportType)
                ? "/api/Reporting/definitions"
                : $"/api/Reporting/definitions?reportType={Uri.EscapeDataString(reportType)}";

            var result = await GetAsync<List<ReportDefinitionApiModel>>(requestUri, cancellationToken);
            return (result ?? new List<ReportDefinitionApiModel>()).ConvertAll(MapDefinition);
        }

        public async Task<ReportRunDto?> GenerateReportAsync(int reportId, string format = "JSON", CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, ReportRunDto>($"/api/Reporting/generate/{reportId}?format={Uri.EscapeDataString(format)}", new { }, cancellationToken);
        }

        public async Task<List<ReportRunDto>> GetReportHistoryAsync(int reportId, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<ReportRunDto>>($"/api/Reporting/history/{reportId}?pageSize={pageSize}", cancellationToken);
            return result ?? new List<ReportRunDto>();
        }

        public async Task<ReportRunDto?> GetReportRunAsync(int runId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<ReportRunDto>($"/api/Reporting/runs/{runId}", cancellationToken);
        }

        private static ReportDefinitionDto MapDefinition(ReportDefinitionApiModel report)
        {
            return new ReportDefinitionDto
            {
                Id = report.Id,
                ReportCode = report.ReportCode ?? string.Empty,
                ReportName = report.ReportName ?? string.Empty,
                Description = report.Description ?? string.Empty,
                ReportType = report.ReportType ?? string.Empty,
                Frequency = report.Frequency ?? string.Empty,
                TemplateFormat = report.TemplateFormat ?? string.Empty,
                IsActive = report.IsActive,
                RequiresApproval = report.RequiresApproval,
                CreatedAt = report.CreatedAt
            };
        }

        private sealed class ReportDefinitionApiModel
        {
            public int Id { get; set; }
            public string? ReportCode { get; set; }
            public string? ReportName { get; set; }
            public string? Description { get; set; }
            public string? ReportType { get; set; }
            public string? Frequency { get; set; }
            public string? TemplateFormat { get; set; }
            public bool IsActive { get; set; }
            public bool RequiresApproval { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }

    public class ReportDefinitionDto
    {
        public int Id { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string TemplateFormat { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool RequiresApproval { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReportRunDto
    {
        public int Id { get; set; }
        public int ReportDefinitionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public string Format { get; set; } = string.Empty;
        public long? ExecutionTimeMs { get; set; }
    }
}
