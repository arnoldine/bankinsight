namespace BankInsight.API.DTOs;

public class Customer360Response
{
    public CustomerProfileResponse Profile { get; set; } = new();
    public Customer360FinancialSummaryDto FinancialSummary { get; set; } = new();
    public List<Customer360AccountDto> Accounts { get; set; } = new();
    public List<Customer360LoanDto> Loans { get; set; } = new();
    public List<Customer360InvestmentDto> Investments { get; set; } = new();
    public List<Customer360TransactionDto> RecentTransactions { get; set; } = new();
    public List<Customer360EngagementItemDto> EngagementTimeline { get; set; } = new();
}

public class Customer360FinancialSummaryDto
{
    public int ActiveAccountCount { get; set; }
    public int ActiveLoanCount { get; set; }
    public int ActiveInvestmentCount { get; set; }
    public decimal TotalDeposits90Days { get; set; }
    public decimal TotalWithdrawals90Days { get; set; }
    public decimal TotalBalances { get; set; }
    public decimal TotalOutstandingLoans { get; set; }
    public decimal TotalInvestmentBook { get; set; }
    public string PrimaryCurrency { get; set; } = "GHS";
    public decimal EstimatedAnnualRevenue { get; set; }
    public string RelationshipOwner { get; set; } = "Unassigned";
}

public class Customer360AccountDto
{
    public string Id { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Balance { get; set; }
    public DateTime OpenDate { get; set; }
}

public class Customer360LoanDto
{
    public string Id { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal OutstandingBalance { get; set; }
    public string ParBucket { get; set; } = "0";
    public string? RepaymentFrequency { get; set; }
}

public class Customer360InvestmentDto
{
    public string Id { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public DateTime? MaturityDate { get; set; }
}

public class Customer360TransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

public class Customer360EngagementItemDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public DateTime At { get; set; }
}
