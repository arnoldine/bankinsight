using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("collection_cases")]
public class CollectionCase
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("loan_id")]
    [MaxLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "OPEN";

    [Column("priority")]
    [MaxLength(20)]
    public string Priority { get; set; } = "MEDIUM";

    [Column("recovery_stage")]
    [MaxLength(30)]
    public string RecoveryStage { get; set; } = "EARLY_ARREARS";

    [Column("delinquency_days")]
    public int DelinquencyDays { get; set; }

    [Column("outstanding_balance")]
    public decimal OutstandingBalance { get; set; }

    [Column("amount_in_arrears")]
    public decimal AmountInArrears { get; set; }

    [Column("assigned_to")]
    [MaxLength(50)]
    public string? AssignedTo { get; set; }

    [Column("next_action_date")]
    public DateTime? NextActionDate { get; set; }

    [Column("promise_to_pay_date")]
    public DateTime? PromiseToPayDate { get; set; }

    [Column("promise_to_pay_amount")]
    public decimal? PromiseToPayAmount { get; set; }

    [Column("last_contact_at")]
    public DateTime? LastContactAt { get; set; }

    [Column("last_payment_at")]
    public DateTime? LastPaymentAt { get; set; }

    [Column("next_escalation_date")]
    public DateTime? NextEscalationDate { get; set; }

    [Column("notes")]
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Column("recovery_strategy")]
    [MaxLength(100)]
    public string? RecoveryStrategy { get; set; }

    [Column("legal_status")]
    [MaxLength(30)]
    public string? LegalStatus { get; set; }

    [Column("settlement_amount")]
    public decimal? SettlementAmount { get; set; }

    [Column("settlement_expiry_date")]
    public DateTime? SettlementExpiryDate { get; set; }

    [Column("assigned_agency")]
    [MaxLength(120)]
    public string? AssignedAgency { get; set; }

    [Column("repossession_status")]
    [MaxLength(30)]
    public string? RepossessionStatus { get; set; }

    [Column("approval_status")]
    [MaxLength(30)]
    public string? ApprovalStatus { get; set; }

    [Column("write_off_recommended_amount")]
    public decimal? WriteOffRecommendedAmount { get; set; }

    [Column("write_off_reason")]
    [MaxLength(500)]
    public string? WriteOffReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CollectionCaseEvent> Events { get; set; } = new List<CollectionCaseEvent>();
}

[Table("collection_case_events")]
public class CollectionCaseEvent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("case_id")]
    [MaxLength(50)]
    public string CaseId { get; set; } = string.Empty;

    [ForeignKey(nameof(CaseId))]
    public CollectionCase? Case { get; set; }

    [Column("event_type")]
    [MaxLength(30)]
    public string EventType { get; set; } = "NOTE";

    [Column("performed_by")]
    [MaxLength(50)]
    public string? PerformedBy { get; set; }

    [Column("detail")]
    [MaxLength(2000)]
    public string Detail { get; set; } = string.Empty;

    [Column("metadata_json")]
    public string? MetadataJson { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
