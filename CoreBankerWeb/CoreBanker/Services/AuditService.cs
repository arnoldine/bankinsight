using System.Globalization;

namespace CoreBanker.Services
{
    public class AuditService : ApiClientBase
    {
        public AuditService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

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
                Status = string.IsNullOrWhiteSpace(log.Status) ? "SUCCESS" : log.Status.Trim().ToUpperInvariant(),
                EntityType = log.EntityType ?? string.Empty,
                EntityId = log.EntityId ?? string.Empty,
                IpAddress = log.IpAddress ?? string.Empty,
                OldValues = log.OldValues ?? string.Empty,
                NewValues = log.NewValues ?? string.Empty,
                ErrorMessage = log.ErrorMessage ?? string.Empty
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
            public string? EntityType { get; set; }
            public string? EntityId { get; set; }
            public string? IpAddress { get; set; }
            public string? OldValues { get; set; }
            public string? NewValues { get; set; }
            public string? ErrorMessage { get; set; }
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
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string OldValues { get; set; } = string.Empty;
        public string NewValues { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
