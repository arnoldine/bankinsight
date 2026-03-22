using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class DisburseLoanRequest : IValidatableObject
{
    [StringLength(50, ErrorMessage = "LoanId must not exceed 50 characters")]
    public string? LoanId { get; set; }

    [StringLength(50, ErrorMessage = "Cif must not exceed 50 characters")]
    public string? Cif { get; set; }

    [StringLength(50, ErrorMessage = "GroupId must not exceed 50 characters")]
    public string? GroupId { get; set; }

    [StringLength(50, ErrorMessage = "ProductCode must not exceed 50 characters")]
    public string? ProductCode { get; set; }

    [Range(0.01, 999999999.99, ErrorMessage = "Principal must be between 0.01 and 999999999.99")]
    public decimal? Principal { get; set; }

    [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
    public decimal? Rate { get; set; }

    [Range(1, 360, ErrorMessage = "TermMonths must be between 1 and 360")]
    public int? TermMonths { get; set; }

    [StringLength(50, ErrorMessage = "CollateralType must not exceed 50 characters")]
    public string? CollateralType { get; set; }

    [Range(0, 999999999.99, ErrorMessage = "CollateralValue must be between 0 and 999999999.99")]
    public decimal? CollateralValue { get; set; }

    [StringLength(100, ErrorMessage = "ClientReference must not exceed 100 characters")]
    public string? ClientReference { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(LoanId))
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Cif))
        {
            yield return new ValidationResult("Cif is required when LoanId is not provided", new[] { nameof(Cif) });
        }

        if (string.IsNullOrWhiteSpace(ProductCode))
        {
            yield return new ValidationResult("ProductCode is required when LoanId is not provided", new[] { nameof(ProductCode) });
        }

        if (!Principal.HasValue)
        {
            yield return new ValidationResult("Principal is required when LoanId is not provided", new[] { nameof(Principal) });
        }

        if (!Rate.HasValue)
        {
            yield return new ValidationResult("Rate is required when LoanId is not provided", new[] { nameof(Rate) });
        }

        if (!TermMonths.HasValue)
        {
            yield return new ValidationResult("TermMonths is required when LoanId is not provided", new[] { nameof(TermMonths) });
        }
    }
}

public class LoanDto
{
    public string Id { get; set; } = string.Empty;
    public string Cif { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public int TermMonths { get; set; }
    public DateOnly? DisbursementDate { get; set; }
    public string ParBucket { get; set; } = string.Empty;
    public decimal? OutstandingBalance { get; set; }
    public string? CollateralType { get; set; }
    public decimal? CollateralValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? InterestMethod { get; set; }
    public string? RepaymentFrequency { get; set; }
    public string? ScheduleType { get; set; }
    public string? LoanProductId { get; set; }
}
public class ConfigureLoanProductRequest
{
    [Required]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string ProductType { get; set; } = "DigitalLoan30Days";

    [Required]
    public string InterestMethod { get; set; } = "Flat";

    [Required]
    public string RepaymentFrequency { get; set; } = "Monthly";

    [Range(1, 260)]
    public int TermInPeriods { get; set; }

    [Range(0, 100)]
    public decimal AnnualInterestRate { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal MinAmount { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal MaxAmount { get; set; }
}

public class ApplyLoanRequest
{
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(50)]
    public string? GroupId { get; set; }

    [Required]
    [StringLength(50)]
    public string LoanProductId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Principal { get; set; }

    [Range(0, 100)]
    public decimal AnnualInterestRate { get; set; }

    [Range(1, 360)]
    public int TermInPeriods { get; set; }

    [Required]
    public string InterestMethod { get; set; } = "Flat";

    [Required]
    public string RepaymentFrequency { get; set; } = "Monthly";

    [Required]
    public string ScheduleType { get; set; } = "Monthly";

    [StringLength(100)]
    public string? ClientReference { get; set; }
}

public class ApproveLoanRequest
{
    [Required]
    [StringLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [StringLength(250)]
    public string? DecisionNotes { get; set; }
}

public class DisburseApprovedLoanRequest
{
    [Required]
    [StringLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ClientReference { get; set; }
}

public class RepayLoanRequest
{
    [Required]
    [StringLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [StringLength(100)]
    public string? ClientReference { get; set; }
}

public class CheckCreditRequest
{
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(50)]
    public string? LoanId { get; set; }

    [StringLength(50)]
    public string? ProviderName { get; set; }
}

public class CreditCheckDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string? LoanId { get; set; }
    public int Score { get; set; }
    public string RiskBand { get; set; } = "UNKNOWN";
    public string RiskGrade { get; set; } = "UNKNOWN";
    public string Decision { get; set; } = "REVIEW";
    public string Recommendation { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string InquiryReference { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}

public class GenerateLoanScheduleRequest
{
    [Range(0.01, 999999999.99)]
    public decimal Principal { get; set; }

    [Range(0, 100)]
    public decimal AnnualInterestRate { get; set; }

    [Range(1, 360)]
    public int TermInPeriods { get; set; }

    [Required]
    public string InterestMethod { get; set; } = "Flat";

    [Required]
    public string RepaymentFrequency { get; set; } = "Monthly";

    [Required]
    public string ScheduleType { get; set; } = "Monthly";

    public DateOnly? StartDate { get; set; }
}

public class LoanScheduleLineDto
{
    public int Period { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Principal { get; set; }
    public decimal Interest { get; set; }
    public decimal Installment { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class LoanScheduleGenerationResultDto
{
    public decimal Principal { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public string InterestMethod { get; set; } = string.Empty;
    public string RepaymentFrequency { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = string.Empty;
    public List<LoanScheduleLineDto> Lines { get; set; } = new();
}

public class LoanRepayRequest
{
    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be between 0.01 and 999999999.99")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "AccountId is required")]
    [StringLength(50, ErrorMessage = "AccountId must not exceed 50 characters")]
    public string AccountId { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ClientReference must not exceed 100 characters")]
    public string? ClientReference { get; set; }
}

public class LoanScheduleDto
{
    public int Period { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Principal { get; set; }
    public decimal Interest { get; set; }
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., "DUE", "PAID"
    public DateOnly? PaidDate { get; set; }
}

public class LoanAccrualSnapshotDto
{
    public string LoanId { get; set; } = string.Empty;
    public DateOnly AsOfDate { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public decimal AnnualRate { get; set; }
    public decimal DailyAccruedInterest { get; set; }
    public decimal AccruedInterestToDate { get; set; }
    public int DaysPastDue { get; set; }
    public string ParBucket { get; set; } = "0";
}

public class AssessLoanPenaltyRequest
{
    [Required(ErrorMessage = "PenaltyRate is required")]
    [Range(0, 100, ErrorMessage = "PenaltyRate must be between 0 and 100")]
    public decimal PenaltyRate { get; set; }

    [StringLength(200, ErrorMessage = "Reason must not exceed 200 characters")]
    public string? Reason { get; set; }

    [StringLength(100, ErrorMessage = "ClientReference must not exceed 100 characters")]
    public string? ClientReference { get; set; }
}

public class LoanPenaltyDto
{
    public string LoanId { get; set; } = string.Empty;
    public decimal PenaltyAmount { get; set; }
    public decimal PenaltyRate { get; set; }
    public int DaysPastDue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; }
}

public class AssessAccountFeeRequest
{
    [Required(ErrorMessage = "FeeCode is required")]
    [StringLength(50, ErrorMessage = "FeeCode must not exceed 50 characters")]
    public string FeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be between 0.01 and 999999999.99")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "AccountId is required")]
    [StringLength(50, ErrorMessage = "AccountId must not exceed 50 characters")]
    public string AccountId { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Narration must not exceed 200 characters")]
    public string? Narration { get; set; }

    [StringLength(100, ErrorMessage = "ClientReference must not exceed 100 characters")]
    public string? ClientReference { get; set; }
}

public class AccountFeeDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string FeeCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Narration { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; }
}

public class LoanClassificationDto
{
    public string LoanId { get; set; } = string.Empty;
    public string BogTier { get; set; } = string.Empty; // Current, Oversight, Substandard, Doubtful, Loss
    public int DaysPastDue { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public decimal OutstandingInterest { get; set; }
    public decimal ProvisioningAmount { get; set; }
    public decimal ProvisioningRate { get; set; }
    public DateTime EvaluationDate { get; set; }
}

public class AppraiseLoanRequest
{
    [Required]
    [StringLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [Required]
    public string Decision { get; set; } = "Reviewed";

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class LoanRestructureRequest
{
    [Required]
    [StringLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [Range(1, 360)]
    public int NewTermInPeriods { get; set; }

    [Range(0, 100)]
    public decimal? NewAnnualRate { get; set; }

    public string? NewRepaymentFrequency { get; set; }

    [StringLength(250)]
    public string Reason { get; set; } = "Restructure";
}

public class LoanRepaymentReversalRequest
{
    [Required]
    [StringLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [Required]
    public Guid RepaymentId { get; set; }

    [StringLength(200)]
    public string Reason { get; set; } = "Reversal";
}

public class LoanAccrualBatchRequest
{
    public DateOnly? AsOfDate { get; set; }

    [StringLength(50)]
    public string? LoanId { get; set; }
}

public class LoanAccrualBatchResultDto
{
    public DateOnly AsOfDate { get; set; }
    public int LoansProcessed { get; set; }
    public decimal TotalInterestAccrued { get; set; }
    public decimal TotalPenaltyAccrued { get; set; }
    public List<string> JournalIds { get; set; } = new();
}

public class LoanWriteOffRequest
{
    [Required]
    [StringLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [StringLength(250)]
    public string Reason { get; set; } = "Write-off";
}

public class LoanRecoveryRequest
{
    [Required]
    [StringLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Reference { get; set; }
}

public class LoanStatementDto
{
    public string LoanId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal TotalInterestPaid { get; set; }
    public decimal TotalPenaltyPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<LoanScheduleDto> Schedule { get; set; } = new();
}

public class LoanDelinquencyDashboardDto
{
    public int TotalActiveLoans { get; set; }
    public int NonAccrualLoans { get; set; }
    public decimal PortfolioAtRisk30 { get; set; }
    public decimal PortfolioAtRisk90 { get; set; }
    public Dictionary<string, int> AgingBuckets { get; set; } = new();
}

public class LoanProfitabilityItemDto
{
    public string GroupingKey { get; set; } = string.Empty;
    public decimal InterestIncome { get; set; }
    public decimal ProcessingFeeIncome { get; set; }
    public decimal PenaltyIncome { get; set; }
    public decimal ImpairmentExpense { get; set; }
    public decimal RecoveryIncome { get; set; }
    public decimal NetContribution { get; set; }
}

public class LoanProfitabilityReportDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public List<LoanProfitabilityItemDto> ProductLevel { get; set; } = new();
    public List<LoanProfitabilityItemDto> BranchLevel { get; set; } = new();
}

public class LoanBalanceSheetSummaryDto
{
    public decimal GrossLoanPortfolio { get; set; }
    public decimal AccruedInterestReceivable { get; set; }
    public decimal AccruedPenaltyReceivable { get; set; }
    public decimal ImpairmentAllowance { get; set; }
    public decimal NetLoanPortfolio { get; set; }
}

public class LoanBalanceSheetReportDto
{
    public DateOnly AsOfDate { get; set; }
    public LoanBalanceSheetSummaryDto Total { get; set; } = new();
    public List<LoanProfitabilityItemDto> BranchContributions { get; set; } = new();
}

public class LoanGlPostingDto
{
    public string JournalId { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LoanGlLineDto> Lines { get; set; } = new();
}

public class LoanGlLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class ConfigureLoanAccountingProfileRequest
{
    [Required]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LoanProductId { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string LoanPortfolioGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string InterestIncomeGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string ProcessingFeeIncomeGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PenaltyIncomeGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string InterestReceivableGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PenaltyReceivableGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string ImpairmentExpenseGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string ImpairmentAllowanceGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string RecoveryIncomeGl { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string DisbursementFundingGl { get; set; } = string.Empty;

    public string RepaymentAllocationOrder { get; set; } = "[\"Penalty\",\"Fees\",\"Interest\",\"Principal\"]";
}

public class CreditBureauProviderDto
{
    public string ProviderName { get; set; } = string.Empty;
}

