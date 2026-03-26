using System.Globalization;

namespace CoreBanker.Services
{
    public class TellerService : ApiClientBase
    {
        public TellerService(HttpClient httpClient) : base(httpClient) { }

        public async Task<TellerTransactionResult> PostTransactionAsync(TellerTransactionRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedRequest = new TellerTransactionRequest
            {
                AccountId = request.AccountId.Trim(),
                Type = NormalizeTransactionType(request.Type),
                Amount = request.Amount,
                Narration = string.IsNullOrWhiteSpace(request.Narration) ? null : request.Narration.Trim(),
                TellerId = string.IsNullOrWhiteSpace(request.TellerId) ? null : request.TellerId.Trim(),
                ClientReference = string.IsNullOrWhiteSpace(request.ClientReference) ? null : request.ClientReference.Trim()
            };

            var response = await PostAsync<TellerTransactionRequest, TellerTransactionApiModel>("/api/transactions", normalizedRequest, cancellationToken);
            return response is null
                ? new TellerTransactionResult()
                : new TellerTransactionResult
                {
                    Id = response.Id ?? string.Empty,
                    AccountId = response.AccountId ?? string.Empty,
                    Type = NormalizeTransactionType(response.Type),
                    Amount = response.Amount ?? 0m,
                    Status = string.IsNullOrWhiteSpace(response.Status) ? "POSTED" : response.Status.Trim().ToUpperInvariant(),
                    Reference = response.Reference ?? string.Empty,
                    Date = ParseDate(response.Date)
                };
        }

        private static string NormalizeTransactionType(string? value)
        {
            var normalized = (value ?? "DEPOSIT").Trim().ToUpperInvariant();
            return normalized switch
            {
                "WITHDRAWAL" => "WITHDRAWAL",
                "TRANSFER" => "TRANSFER",
                "LOAN_REPAYMENT" => "LOAN_REPAYMENT",
                _ => "DEPOSIT"
            };
        }

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : null;
        }

        private sealed class TellerTransactionApiModel
        {
            public string? Id { get; set; }
            public string? AccountId { get; set; }
            public string? Type { get; set; }
            public decimal? Amount { get; set; }
            public string? Date { get; set; }
            public string? Status { get; set; }
            public string? Reference { get; set; }
        }
    }

    public class TellerTransactionRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string Type { get; set; } = "DEPOSIT";
        public decimal Amount { get; set; }
        public string? Narration { get; set; }
        public string? TellerId { get; set; }
        public string? ClientReference { get; set; }
    }

    public class TellerTransactionResult
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string Type { get; set; } = "DEPOSIT";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "POSTED";
        public string Reference { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
    }
}
