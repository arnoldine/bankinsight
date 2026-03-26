using System.Globalization;

namespace CoreBanker.Services
{
    public class AuditService : ApiClientBase
    {
        public AuditService(HttpClient httpClient) : base(httpClient) { }

        public async Task<List<AuditLogDto>> GetAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default)
        {
            var logs = await GetAsync<List<AuditLogApiModel>>($"/api/audit?limit={limit}", cancellationToken);
            return (logs ?? new List<AuditLogApiModel>()).ConvertAll(MapAuditLog);
        }

        private static AuditLogDto MapAuditLog(AuditLogApiModel log)
        {
            return new AuditLogDto
            {
                Id = log.Id ?? string.Empty,
                Action = log.Action ?? string.Empty,
                User = log.User ?? "System",
                Date = ParseDate(log.Timestamp),
                Details = log.Details ?? string.Empty,
                Module = log.Module ?? string.Empty,
                Status = string.IsNullOrWhiteSpace(log.Status) ? "SUCCESS" : log.Status.Trim().ToUpperInvariant()
            };
        }

        private static DateTime ParseDate(string? value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTime.UtcNow;
        }

        private sealed class AuditLogApiModel
        {
            public string? Id { get; set; }
            public string? Timestamp { get; set; }
            public string? User { get; set; }
            public string? Action { get; set; }
            public string? Details { get; set; }
            public string? Module { get; set; }
            public string? Status { get; set; }
        }
    }

    public class AuditLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Details { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Status { get; set; } = "SUCCESS";
    }
}
