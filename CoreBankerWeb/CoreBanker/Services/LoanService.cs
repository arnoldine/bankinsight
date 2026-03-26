using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class LoanService : ApiClientBase
    {
        public LoanService(HttpClient httpClient) : base(httpClient) { }

        // API model for compatibility with backend
        public class LoanApiModel
        {
            public string? Id { get; set; }
            public string? CustomerName { get; set; }
            public string? ProductName { get; set; }
            public decimal? Principal { get; set; }
            public decimal? OutstandingBalance { get; set; }
            public string? Status { get; set; }
        }

        private static string NormalizeStatus(string? value)
        {
            var normalized = (value ?? "ACTIVE").Trim().ToUpperInvariant();
            if (normalized == "PENDING") return "Pending";
            if (normalized == "CLOSED" || normalized == "WRITTEN_OFF") return "Closed";
            return "Active";
        }

        private static LoanDto MapLoan(LoanApiModel loan)
        {
            return new LoanDto
            {
                Id = loan.Id ?? string.Empty,
                ClientName = loan.CustomerName ?? string.Empty,
                Product = loan.ProductName ?? string.Empty,
                Principal = loan.Principal ?? 0,
                Outstanding = loan.OutstandingBalance ?? 0,
                Status = NormalizeStatus(loan.Status)
            };
        }

        public async Task<List<LoanDto>> GetLoansAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<LoanApiModel>>("/api/loans", cancellationToken);
            return (result ?? new List<LoanApiModel>()).ConvertAll(MapLoan);
        }
    }

    public class LoanDto
    {
        public string Id { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public decimal Principal { get; set; }
        public decimal Outstanding { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
