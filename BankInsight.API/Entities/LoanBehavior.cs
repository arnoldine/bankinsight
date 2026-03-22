using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

public enum PaymentSourceType
{
    Cash,
    MoMo,      // Mobile Money
    ACH,       // Automated Clearing House
    Internal,  // Direct Account Transfer
    Cheque
}

public enum BogClassificationTier
{
    Current,       // DPD 0
    Oversight,     // DPD 1-30
    Substandard,   // DPD 31-90
    Doubtful,      // DPD 91-180
    Loss           // DPD 181+
}

[Table("loan_repayment_behavior")]
public class LoanRepaymentBehavior
{
    [Key]
    [Column("repayment_id")]
    public Guid RepaymentId { get; set; } = Guid.NewGuid();

    [Column("loan_id")]
    public Guid LoanId { get; set; }

    [Column("transaction_id")]
    public Guid TransactionId { get; set; }

    [Column("payment_date")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Column("payment_source")]
    public PaymentSourceType PaymentSource { get; set; }

    [Column("payment_reference")]
    [StringLength(100)]
    public string? PaymentReference { get; set; }

    [Column("total_paid")]
    public decimal TotalPaid { get; set; }

    [Column("principal_allocated")]
    public decimal PrincipalAllocated { get; set; }

    [Column("interest_allocated")]
    public decimal InterestAllocated { get; set; }

    [Column("penalty_allocated")]
    public decimal PenaltyAllocated { get; set; }

    [Column("fees_allocated")]
    public decimal FeesAllocated { get; set; }

    [Column("days_past_due_upon_payment")]
    public int DaysPastDueUponPayment { get; set; }

    [Column("is_first_payment_default")]
    public bool IsFirstPaymentDefault { get; set; }

    [Column("late_pay_trend_score")]
    public int LatePayTrendScore { get; set; }
}

[Table("loan_bog_classification")]
public class LoanBogClassification
{
    [Key]
    [Column("classification_id")]
    public Guid ClassificationId { get; set; } = Guid.NewGuid();

    [Column("loan_id")]
    public Guid LoanId { get; set; }

    [Column("evaluation_date")]
    public DateTime EvaluationDate { get; set; }

    [Column("days_past_due")]
    public int DaysPastDue { get; set; }

    [Column("classification")]
    public BogClassificationTier Classification { get; set; }

    [Column("outstanding_principal")]
    public decimal OutstandingPrincipal { get; set; }

    [Column("outstanding_interest")]
    public decimal OutstandingInterest { get; set; }

    [Column("provisioning_amount")]
    public decimal ProvisioningAmount { get; set; }
}
