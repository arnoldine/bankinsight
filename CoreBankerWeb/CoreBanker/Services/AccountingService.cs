using System.Net;

namespace CoreBanker.Services
{
    public class AccountingService : ApiClientBase
    {
        public AccountingService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<List<JournalEntryDto>> GetJournalEntriesAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<JournalEntryDto>>("/api/gl/journal-entries", cancellationToken);
            return result ?? new List<JournalEntryDto>();
        }

        public async Task<List<GLAccountDto>> GetGLAccountsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<GLAccountDto>>("/api/gl/accounts", cancellationToken);
            return result ?? new List<GLAccountDto>();
        }

        public async Task<JournalEntryDto?> PostJournalEntryAsync(PostJournalEntryRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<PostJournalEntryRequest, JournalEntryDto>("/api/gl/journal-entries", request, cancellationToken);
        }

        public async Task<GLAccountDto?> CreateGLAccountAsync(CreateGlAccountRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateGlAccountRequest, GLAccountDto>("/api/gl/accounts", request, cancellationToken);
        }

        public async Task<SeedChartOfAccountsResponseDto?> SeedRegulatoryChartAsync(string regionCode = "GH", CancellationToken cancellationToken = default)
        {
            return await PostAsync<SeedChartOfAccountsRequestDto, SeedChartOfAccountsResponseDto>("/api/gl/accounts/seed-regulatory", new SeedChartOfAccountsRequestDto { RegionCode = regionCode }, cancellationToken);
        }

        public async Task<IncomeStatementDto?> GetIncomeStatementAsync(DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
        {
            var query = $"periodStart={Uri.EscapeDataString(periodStart.ToString("O"))}&periodEnd={Uri.EscapeDataString(periodEnd.ToString("O"))}";
            return await GetFinancialReportAsync<IncomeStatementDto>($"/api/reports/financial/income-statement?{query}", $"/api/financial-reports/income-statement?{query}", cancellationToken);
        }

        public async Task<CashFlowStatementDto?> GetCashFlowStatementAsync(DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
        {
            var query = $"periodStart={Uri.EscapeDataString(periodStart.ToString("O"))}&periodEnd={Uri.EscapeDataString(periodEnd.ToString("O"))}";
            return await GetFinancialReportAsync<CashFlowStatementDto>($"/api/reports/financial/cash-flow?{query}", $"/api/financial-reports/cash-flow?{query}", cancellationToken);
        }

        public async Task<TrialBalanceDto?> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
        {
            var query = $"asOfDate={Uri.EscapeDataString(asOfDate.ToString("O"))}";
            return await GetFinancialReportAsync<TrialBalanceDto>($"/api/reports/financial/trial-balance?{query}", $"/api/financial-reports/trial-balance?{query}", cancellationToken);
        }

        private async Task<T?> GetFinancialReportAsync<T>(string primaryPath, string fallbackPath, CancellationToken cancellationToken)
        {
            try
            {
                return await GetAsync<T>(primaryPath, cancellationToken);
            }
            catch (ApiClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return await GetAsync<T>(fallbackPath, cancellationToken);
            }
        }
    }

    public class CreateGlAccountRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Currency { get; set; }
        public bool IsHeader { get; set; }
    }

    public class SeedChartOfAccountsRequestDto
    {
        public string RegionCode { get; set; } = "GH";
    }

    public class SeedChartOfAccountsResponseDto
    {
        public string RegionCode { get; set; } = string.Empty;
        public string StandardName { get; set; } = string.Empty;
        public int InsertedCount { get; set; }
        public int ExistingCount { get; set; }
        public int TotalStandardAccounts { get; set; }
        public List<string> InsertedCodes { get; set; } = new();
    }

    public class GLAccountDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Currency { get; set; }
        public decimal Balance { get; set; }
        public bool IsHeader { get; set; }
    }

    public class PostJournalEntryRequest
    {
        public string? Reference { get; set; }
        public string? Description { get; set; }
        public string? PostedBy { get; set; }
        public List<JournalLineDto> Lines { get; set; } = new();
    }

    public class JournalLineDto
    {
        public string AccountCode { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class JournalEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string? Reference { get; set; }
        public string? Description { get; set; }
        public string? PostedBy { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public List<JournalLineResponseDto> Lines { get; set; } = new();
    }

    public class JournalLineResponseDto
    {
        public int Id { get; set; }
        public string JournalId { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class IncomeStatementDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<IncomeStatementLineItemDto> RevenueItems { get; set; } = new();
        public List<IncomeStatementLineItemDto> ExpenseItems { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class IncomeStatementLineItemDto
    {
        public string LineItem { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class CashFlowStatementDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<CashFlowLineItemDto> OperatingActivities { get; set; } = new();
        public List<CashFlowLineItemDto> InvestingActivities { get; set; } = new();
        public List<CashFlowLineItemDto> FinancingActivities { get; set; } = new();
        public decimal NetOperatingCashFlow { get; set; }
        public decimal NetInvestingCashFlow { get; set; }
        public decimal NetFinancingCashFlow { get; set; }
        public decimal NetChangeInCash { get; set; }
    }

    public class CashFlowLineItemDto
    {
        public string Activity { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class TrialBalanceDto
    {
        public DateTime AsOfDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<TrialBalanceAccountDto> Accounts { get; set; } = new();
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public bool IsBalanced { get; set; }
    }

    public class TrialBalanceAccountDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
    }
}
