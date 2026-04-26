using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

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
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [StringLength(50)]
    public string? BranchId { get; set; }

    [StringLength(3)]
    public string? Currency { get; set; }

    [Range(0, 999999999.99)]
    public decimal InitialDepositAmount { get; set; }

    [StringLength(50)]
    public string? FundingAccountId { get; set; }

    public bool IsConfidential { get; set; }

    [StringLength(50)]
    public string? OwnerStaffId { get; set; }
}

public class DigitalSavingsTransferRequest
{
    [Required]
    [StringLength(50)]
    public string CounterpartyAccountId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(500)]
    public string Narration { get; set; } = string.Empty;
}

public class CreateDigitalInvestmentRequest
{
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string FundingAccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Principal { get; set; }

    [Range(0, 100)]
    public decimal Rate { get; set; }

    [Range(1, 3650)]
    public int TenorDays { get; set; }

    [StringLength(30)]
    public string PayoutOption { get; set; } = "AT_MATURITY";

    public bool AutoRollover { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class DigitalInvestmentActionRequest
{
    [StringLength(50)]
    public string? FundingAccountId { get; set; }

    [Range(0, 999999999.99)]
    public decimal? Amount { get; set; }

    public DateTime? NewMaturityDate { get; set; }

    [Range(0, 100)]
    public decimal? NewRate { get; set; }

    [StringLength(50)]
    public string? DestinationAccountId { get; set; }

    [Range(0, 999999999.99)]
    public decimal? PenaltyAmount { get; set; }

    [StringLength(1000)]
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
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(50)]
    public string? LoanProductId { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal? Principal { get; set; }

    [StringLength(50)]
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
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LoanProductId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Principal { get; set; }

    [StringLength(50)]
    public string? ServicingAccountId { get; set; }

    [StringLength(50)]
    public string? CollateralAccountId { get; set; }

    [StringLength(100)]
    public string? ClientReference { get; set; }
}
