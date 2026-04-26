namespace CoreBanker.Services;

public class DigitalBankingService : ApiClientBase
{
    public DigitalBankingService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

    public Task<DigitalBankingDashboardDto?> GetDashboardAsync(CancellationToken cancellationToken = default)
        => GetAsync<DigitalBankingDashboardDto>("/api/digital-banking/dashboard", cancellationToken);

    public async Task<List<DigitalBankingProductDto>> GetSavingsProductsAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<DigitalBankingProductDto>>("/api/digital-banking/savings/products", cancellationToken) ?? [];

    public async Task<List<AccountListItemDto>> GetCustomerSavingsAccountsAsync(string customerId, CancellationToken cancellationToken = default)
        => await GetAsync<List<AccountListItemDto>>($"/api/digital-banking/savings/accounts/{Uri.EscapeDataString(customerId)}", cancellationToken) ?? [];

    public Task<AccountListItemDto?> OpenSavingsAccountAsync(OpenDigitalSavingsAccountRequest request, CancellationToken cancellationToken = default)
        => PostAsync<OpenDigitalSavingsAccountRequest, AccountListItemDto>("/api/digital-banking/savings/accounts", request, cancellationToken);

    public Task<AccountListItemDto?> FundSavingsAccountAsync(string accountId, DigitalSavingsTransferRequest request, CancellationToken cancellationToken = default)
        => PostAsync<DigitalSavingsTransferRequest, AccountListItemDto>($"/api/digital-banking/savings/accounts/{Uri.EscapeDataString(accountId)}/fund", request, cancellationToken);

    public Task<AccountListItemDto?> WithdrawSavingsAccountAsync(string accountId, DigitalSavingsTransferRequest request, CancellationToken cancellationToken = default)
        => PostAsync<DigitalSavingsTransferRequest, AccountListItemDto>($"/api/digital-banking/savings/accounts/{Uri.EscapeDataString(accountId)}/withdraw", request, cancellationToken);

    public Task<DigitalInvestmentPortfolioDto?> GetInvestmentPortfolioAsync(string? customerId = null, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(customerId)
            ? "/api/digital-banking/investments/portfolio"
            : $"/api/digital-banking/investments/portfolio?customerId={Uri.EscapeDataString(customerId)}";
        return GetAsync<DigitalInvestmentPortfolioDto>(url, cancellationToken);
    }

    public Task<DigitalInvestmentProfileDto?> CreateInvestmentAsync(CreateDigitalInvestmentRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateDigitalInvestmentRequest, DigitalInvestmentProfileDto>("/api/digital-banking/investments", request, cancellationToken);

    public Task<DigitalInvestmentProfileDto?> TopUpInvestmentAsync(string profileId, DigitalInvestmentActionRequest request, CancellationToken cancellationToken = default)
        => PostAsync<DigitalInvestmentActionRequest, DigitalInvestmentProfileDto>($"/api/digital-banking/investments/{Uri.EscapeDataString(profileId)}/top-up", request, cancellationToken);

    public Task<DigitalInvestmentProfileDto?> RolloverInvestmentAsync(string profileId, DigitalInvestmentActionRequest request, CancellationToken cancellationToken = default)
        => PostAsync<DigitalInvestmentActionRequest, DigitalInvestmentProfileDto>($"/api/digital-banking/investments/{Uri.EscapeDataString(profileId)}/rollover", request, cancellationToken);

    public Task<DigitalInvestmentProfileDto?> LiquidateInvestmentAsync(string profileId, DigitalInvestmentActionRequest request, CancellationToken cancellationToken = default)
        => PostAsync<DigitalInvestmentActionRequest, DigitalInvestmentProfileDto>($"/api/digital-banking/investments/{Uri.EscapeDataString(profileId)}/liquidate", request, cancellationToken);

    public Task<DigitalLoanEligibilityDto?> CheckLoanEligibilityAsync(CheckDigitalLoanEligibilityRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CheckDigitalLoanEligibilityRequest, DigitalLoanEligibilityDto>("/api/digital-banking/loans/eligibility", request, cancellationToken);

    public Task<LoanDto?> ApplyLoanAsync(CreateDigitalLoanApplicationRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateDigitalLoanApplicationRequest, LoanDto>("/api/digital-banking/loans/apply", request, cancellationToken);

    public Task<LoanDto?> RepayLoanAsync(string loanId, DigitalLoanRepaymentRequest request, CancellationToken cancellationToken = default)
        => PostAsync<DigitalLoanRepaymentRequest, LoanDto>($"/api/digital-banking/loans/{Uri.EscapeDataString(loanId)}/repay", request, cancellationToken);

    public Task<LoanDto?> RestructureLoanAsync(DigitalLoanRestructureRequest request, CancellationToken cancellationToken = default)
        => PostAsync<DigitalLoanRestructureRequest, LoanDto>("/api/digital-banking/loans/restructure", request, cancellationToken);
}

public class DigitalBankingDashboardDto
{
    public int ActiveSavingsAccounts { get; set; }
    public decimal TotalSavingsBalance { get; set; }
    public int ActiveInvestmentProfiles { get; set; }
    public decimal TotalInvestmentBalance { get; set; }
    public int ActiveLoans { get; set; }
    public decimal TotalLoanExposure { get; set; }
    public int PendingApprovals { get; set; }
}

public class DigitalBankingProductDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal? InterestRate { get; set; }
    public string? InterestMethod { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? DefaultTerm { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public class OpenDigitalSavingsAccountRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? BranchId { get; set; }
    public string? Currency { get; set; }
    public decimal InitialDepositAmount { get; set; }
    public string? FundingAccountId { get; set; }
}

public class DigitalSavingsTransferRequest
{
    public string CounterpartyAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Narration { get; set; } = string.Empty;
}

public class CreateDigitalInvestmentRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string FundingAccountId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public int TenorDays { get; set; }
    public string PayoutOption { get; set; } = "AT_MATURITY";
    public bool AutoRollover { get; set; }
    public string? Notes { get; set; }
}

public class DigitalInvestmentActionRequest
{
    public string? FundingAccountId { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? NewMaturityDate { get; set; }
    public decimal? NewRate { get; set; }
    public string? DestinationAccountId { get; set; }
    public decimal? PenaltyAmount { get; set; }
    public string? Notes { get; set; }
}

public class DigitalInvestmentProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string FundingAccountId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public int TenorDays { get; set; }
    public string PayoutOption { get; set; } = "AT_MATURITY";
    public bool AutoRollover { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime StartDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public decimal ProjectedMaturityValue { get; set; }
    public string? Notes { get; set; }
}

public class DigitalInvestmentPortfolioDto
{
    public int ActiveProfiles { get; set; }
    public decimal TotalPrincipal { get; set; }
    public decimal TotalProjectedMaturityValue { get; set; }
    public Dictionary<string, decimal> ByCurrency { get; set; } = new();
    public List<DigitalInvestmentProfileDto> Items { get; set; } = new();
}

public class CheckDigitalLoanEligibilityRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string? LoanProductId { get; set; }
    public decimal? Principal { get; set; }
    public string? ProviderName { get; set; }
}

public class DigitalLoanEligibilityDto
{
    public bool IsEligible { get; set; }
    public List<string> Reasons { get; set; } = new();
    public CreditCheckDto CreditCheck { get; set; } = new();
}

public class CreateDigitalLoanApplicationRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string LoanProductId { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public string? ServicingAccountId { get; set; }
    public string? CollateralAccountId { get; set; }
    public string? ClientReference { get; set; }
}

public class DigitalLoanRepaymentRequest
{
    public decimal Amount { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string? ClientReference { get; set; }
}

public class DigitalLoanRestructureRequest
{
    public string LoanId { get; set; } = string.Empty;
    public int NewTermInPeriods { get; set; }
    public decimal? NewAnnualRate { get; set; }
    public string? NewRepaymentFrequency { get; set; }
    public string Reason { get; set; } = "Restructure";
}
