using System.Globalization;

namespace CoreBanker.Services
{
    public class TransactionService : ApiClientBase
    {
        public TransactionService(HttpClient httpClient) : base(httpClient) { }

        public async Task<List<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<TransactionApiModel>>("/api/transactions", cancellationToken);
            return (result ?? new List<TransactionApiModel>()).ConvertAll(MapTransaction);
        }

        private static TransactionDto MapTransaction(TransactionApiModel txn)
        {
            return new TransactionDto
            {
                Id = txn.Id ?? string.Empty,
                AccountId = txn.AccountId ?? string.Empty,
                Type = NormalizeType(txn.Type),
                Amount = txn.Amount ?? 0m,
                Date = ParseDate(txn.Date),
                Status = NormalizeStatus(txn.Status),
                Narration = txn.Narration ?? string.Empty,
                Reference = txn.Reference ?? string.Empty,
                TellerId = txn.TellerId ?? string.Empty
            };
        }

        private static DateTime ParseDate(string? value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTime.UtcNow;
        }

        private static string NormalizeType(string? value)
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

        private static string NormalizeStatus(string? value)
        {
            var normalized = (value ?? "POSTED").Trim().ToUpperInvariant();
            return normalized switch
            {
                "PENDING" => "PENDING",
                "FAILED" => "FAILED",
                _ => "POSTED"
            };
        }

        private sealed class TransactionApiModel
        {
            public string? Id { get; set; }
            public string? AccountId { get; set; }
            public string? Type { get; set; }
            public decimal? Amount { get; set; }
            public string? Date { get; set; }
            public string? Status { get; set; }
            public string? Narration { get; set; }
            public string? Reference { get; set; }
            public string? TellerId { get; set; }
        }
    }

    public class TransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = "POSTED";
        public string Narration { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string TellerId { get; set; } = string.Empty;
    }
}
