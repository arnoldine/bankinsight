using System.Globalization;

namespace CoreBanker.Services
{
    public class AccountService : ApiClientBase
    {
        public AccountService(HttpClient httpClient) : base(httpClient) { }

        public async Task<List<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<AccountApiModel>>("/api/accounts", cancellationToken);
            return (result ?? new List<AccountApiModel>()).ConvertAll(MapAccount);
        }

        public async Task<List<AccountDto>> GetAccountsByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<AccountApiModel>>($"/api/accounts/customer/{customerId}", cancellationToken);
            return (result ?? new List<AccountApiModel>()).ConvertAll(MapAccount);
        }

        public async Task<AccountDto?> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedRequest = new CreateAccountRequest
            {
                CustomerId = request.CustomerId.Trim(),
                BranchId = NormalizeBranchId(request.BranchId),
                Type = NormalizeAccountType(request.Type),
                Currency = NormalizeCurrency(request.Currency),
                ProductCode = string.IsNullOrWhiteSpace(request.ProductCode) ? DefaultProductCode(request.Type) : request.ProductCode.Trim().ToUpperInvariant()
            };

            var account = await PostAsync<CreateAccountRequest, AccountApiModel>("/api/accounts", normalizedRequest, cancellationToken);
            return account is null ? null : MapAccount(account);
        }

        public async Task<AccountDto?> GetAccountByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var account = await GetAsync<AccountApiModel>($"/api/accounts/{id}", cancellationToken);
            return account is null ? null : MapAccount(account);
        }

        private static AccountDto MapAccount(AccountApiModel account)
        {
            return new AccountDto
            {
                Id = account.Id ?? string.Empty,
                CustomerId = account.CustomerId ?? string.Empty,
                BranchId = NormalizeBranchId(account.BranchId),
                Type = NormalizeAccountType(account.Type),
                Currency = NormalizeCurrency(account.Currency),
                ProductCode = account.ProductCode ?? string.Empty,
                Balance = account.Balance ?? 0m,
                LienAmount = account.LienAmount ?? 0m,
                AvailableBalance = Math.Max(0m, (account.Balance ?? 0m) - (account.LienAmount ?? 0m)),
                Status = NormalizeAccountStatus(account.Status),
                LastTransDate = ParseDate(account.LastTransDate),
                CreatedAt = ParseDate(account.CreatedAt)
            };
        }

        private static string NormalizeAccountType(string? value)
        {
            var normalized = (value ?? "SAVINGS").Trim().ToUpperInvariant();
            return normalized switch
            {
                "CURRENT" => "CURRENT",
                "FIXED_DEPOSIT" => "FIXED_DEPOSIT",
                _ => "SAVINGS"
            };
        }

        private static string NormalizeAccountStatus(string? value)
        {
            var normalized = (value ?? "ACTIVE").Trim().ToUpperInvariant();
            return normalized switch
            {
                "DORMANT" => "DORMANT",
                "FROZEN" => "FROZEN",
                "INACTIVE" => "FROZEN",
                _ => "ACTIVE"
            };
        }

        private static string NormalizeCurrency(string? value)
        {
            return string.Equals(value?.Trim(), "USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "GHS";
        }

        private static string NormalizeBranchId(string? branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId))
            {
                return "BR001";
            }

            var trimmed = branchId.Trim().ToUpperInvariant();
            if (trimmed.StartsWith("BR", StringComparison.Ordinal))
            {
                return trimmed;
            }

            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            return string.IsNullOrWhiteSpace(digits) ? "BR001" : $"BR{digits.PadLeft(3, '0')}";
        }

        private static string DefaultProductCode(string? type)
        {
            return NormalizeAccountType(type) switch
            {
                "CURRENT" => "CUR001",
                "FIXED_DEPOSIT" => "FD001",
                _ => "SAV001"
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

        private sealed class AccountApiModel
        {
            public string? Id { get; set; }
            public string? CustomerId { get; set; }
            public string? BranchId { get; set; }
            public string? Type { get; set; }
            public string? Currency { get; set; }
            public decimal? Balance { get; set; }
            public decimal? LienAmount { get; set; }
            public string? Status { get; set; }
            public string? ProductCode { get; set; }
            public string? LastTransDate { get; set; }
            public string? CreatedAt { get; set; }
        }
    }

    public class AccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string BranchId { get; set; } = "BR001";
        public string Type { get; set; } = "SAVINGS";
        public string Currency { get; set; } = "GHS";
        public string ProductCode { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal LienAmount { get; set; }
        public decimal AvailableBalance { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public DateTime? LastTransDate { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateAccountRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string Type { get; set; } = "SAVINGS";
        public string? Currency { get; set; }
        public string? ProductCode { get; set; }
    }
}
