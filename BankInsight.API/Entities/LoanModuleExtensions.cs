using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

public enum LoanProductType
{
    DigitalLoan30Days,
    WeeklyGroupLoan,
    MonthlyBusinessLoan,
    MonthlyConsumerLoan
}

public enum LoanInterestMethod
{
    Flat,
    ReducingBalance
}

public enum LoanRepaymentFrequency
{
    Weekly,
    Monthly,
    Bullet
}

public enum LoanAppraisalStatus
{
    Pending,
    Reviewed,
    Approved,
    Rejected
}

public enum LoanImpairmentStatus
{
    Performing,
    Stage1,
    Stage2,
    Stage3,
    WrittenOff
}

[Table("loan_products")]
public class LoanProduct
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("product_type")]
    public LoanProductType ProductType { get; set; }

    [Required]
    [Column("interest_method")]
    public LoanInterestMethod InterestMethod { get; set; }

    [Required]
    [Column("repayment_frequency")]
    public LoanRepaymentFrequency RepaymentFrequency { get; set; }

    [Column("term_in_periods")]
    public int TermInPeriods { get; set; }

    [Column("annual_interest_rate")]
    public decimal AnnualInterestRate { get; set; }

    [Column("min_amount")]
    public decimal MinAmount { get; set; }

    [Column("max_amount")]
    public decimal MaxAmount { get; set; }

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}

[Table("loan_repayments")]
public class LoanRepayment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("loan_id")]
    [MaxLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("repayment_date")]
    public DateTime RepaymentDate { get; set; } = DateTime.UtcNow;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("principal_component")]
    public decimal PrincipalComponent { get; set; }

    [Column("interest_component")]
    public decimal InterestComponent { get; set; }

    [Column("penalty_component")]
    public decimal PenaltyComponent { get; set; }

    [Column("reference")]
    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    [Column("processed_by")]
    [MaxLength(50)]
    public string? ProcessedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_reversal")]
    public bool IsReversal { get; set; }

    [Column("reversal_reference")]
    [MaxLength(100)]
    public string? ReversalReference { get; set; }
}

[Table("loan_accounts")]
public class LoanAccount
{
    [Key]
    [Column("loan_id")]
    [MaxLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("branch_id")]
    [MaxLength(50)]
    public string? BranchId { get; set; }

    [Column("appraisal_status")]
    public LoanAppraisalStatus AppraisalStatus { get; set; } = LoanAppraisalStatus.Pending;

    [Column("appraisal_notes")]
    [MaxLength(500)]
    public string? AppraisalNotes { get; set; }

    [Column("appraised_at")]
    public DateTime? AppraisedAt { get; set; }

    [Column("appraised_by")]
    [MaxLength(50)]
    public string? AppraisedBy { get; set; }

    [Column("is_non_accrual")]
    public bool IsNonAccrual { get; set; }

    [Column("is_suspended_interest")]
    public bool IsSuspendedInterest { get; set; }

    [Column("delinquency_days")]
    public int DelinquencyDays { get; set; }

    [Column("arrears_bucket")]
    [MaxLength(20)]
    public string ArrearsBucket { get; set; } = "0";

    [Column("exposure_amount")]
    public decimal ExposureAmount { get; set; }

    [Column("concentration_group")]
    [MaxLength(100)]
    public string? ConcentrationGroup { get; set; }

    [Column("last_reviewed_at")]
    public DateTime? LastReviewedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("loan_accruals")]
public class LoanAccrual
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("loan_id")]
    [MaxLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("accrual_date")]
    public DateOnly AccrualDate { get; set; }

    [Column("interest_accrued")]
    public decimal InterestAccrued { get; set; }

    [Column("penalty_accrued")]
    public decimal PenaltyAccrued { get; set; }

    [Column("is_posted")]
    public bool IsPosted { get; set; }

    [Column("journal_id")]
    [MaxLength(50)]
    public string? JournalId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("loan_impairments")]
public class LoanImpairment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("loan_id")]
    [MaxLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("stage")]
    public LoanImpairmentStatus Stage { get; set; } = LoanImpairmentStatus.Performing;

    [Column("allowance_amount")]
    public decimal AllowanceAmount { get; set; }

    [Column("impairment_expense")]
    public decimal ImpairmentExpense { get; set; }

    [Column("is_written_off")]
    public bool IsWrittenOff { get; set; }

    [Column("written_off_at")]
    public DateTime? WrittenOffAt { get; set; }

    [Column("recovery_amount")]
    public decimal RecoveryAmount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("loan_accounting_profiles")]
public class LoanAccountingProfile
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("loan_product_id")]
    [MaxLength(50)]
    public string LoanProductId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanProductId))]
    public LoanProduct? LoanProduct { get; set; }

    [Column("loan_portfolio_gl")]
    [MaxLength(20)]
    public string LoanPortfolioGl { get; set; } = string.Empty;

    [Column("interest_income_gl")]
    [MaxLength(20)]
    public string InterestIncomeGl { get; set; } = string.Empty;

    [Column("processing_fee_income_gl")]
    [MaxLength(20)]
    public string ProcessingFeeIncomeGl { get; set; } = string.Empty;

    [Column("penalty_income_gl")]
    [MaxLength(20)]
    public string PenaltyIncomeGl { get; set; } = string.Empty;

    [Column("interest_receivable_gl")]
    [MaxLength(20)]
    public string InterestReceivableGl { get; set; } = string.Empty;

    [Column("penalty_receivable_gl")]
    [MaxLength(20)]
    public string PenaltyReceivableGl { get; set; } = string.Empty;

    [Column("impairment_expense_gl")]
    [MaxLength(20)]
    public string ImpairmentExpenseGl { get; set; } = string.Empty;

    [Column("impairment_allowance_gl")]
    [MaxLength(20)]
    public string ImpairmentAllowanceGl { get; set; } = string.Empty;

    [Column("recovery_income_gl")]
    [MaxLength(20)]
    public string RecoveryIncomeGl { get; set; } = string.Empty;

    [Column("disbursement_funding_gl")]
    [MaxLength(20)]
    public string DisbursementFundingGl { get; set; } = string.Empty;

    [Column("repayment_allocation_order", TypeName = "jsonb")]
    public string RepaymentAllocationOrder { get; set; } = "[\"Penalty\",\"Fees\",\"Interest\",\"Principal\"]";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("loan_disclosures")]
public class LoanDisclosure
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("loan_id")]
    [MaxLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("disclosure_text")]
    public string DisclosureText { get; set; } = string.Empty;

    [Column("accepted")]
    public bool Accepted { get; set; }

    [Column("accepted_at")]
    public DateTime? AcceptedAt { get; set; }

    [Column("channel")]
    [MaxLength(30)]
    public string Channel { get; set; } = "DIGITAL";
}

[Table("credit_bureau_checks")]
public class CreditBureauCheck
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("loan_id")]
    [MaxLength(50)]
    public string? LoanId { get; set; }

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Required]
    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Column("bureau_name")]
    [MaxLength(100)]
    public string BureauName { get; set; } = "DefaultCRB";

    [Column("provider_name")]
    [MaxLength(100)]
    public string ProviderName { get; set; } = "DefaultProvider";

    [Column("inquiry_reference")]
    [MaxLength(100)]
    public string InquiryReference { get; set; } = string.Empty;

    [Column("score")]
    public int Score { get; set; }

    [Column("risk_band")]
    [MaxLength(20)]
    public string RiskBand { get; set; } = "UNKNOWN";

    [Column("risk_grade")]
    [MaxLength(20)]
    public string RiskGrade { get; set; } = "UNKNOWN";

    [Column("decision")]
    [MaxLength(20)]
    public string Decision { get; set; } = "REVIEW";

    [Column("recommendation")]
    [MaxLength(100)]
    public string Recommendation { get; set; } = "Manual review";

    [Column("request_payload", TypeName = "jsonb")]
    public string? RequestPayload { get; set; }

    [Column("raw_response", TypeName = "jsonb")]
    public string? RawResponse { get; set; }

    [Column("is_timeout")]
    public bool IsTimeout { get; set; }

    [Column("retry_count")]
    public int RetryCount { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "SUCCESS";

    [Column("checked_at")]
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
