using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("settlement_instructions")]
public class SettlementInstruction
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("reconciliation_exception_id")]
    [MaxLength(50)]
    public string ReconciliationExceptionId { get; set; } = string.Empty;

    [ForeignKey(nameof(ReconciliationExceptionId))]
    public ReconciliationException? ReconciliationException { get; set; }

    [Column("instruction_type")]
    [MaxLength(40)]
    public string InstructionType { get; set; } = "MANUAL_TRANSFER";

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("settlement_account")]
    [MaxLength(80)]
    public string? SettlementAccount { get; set; }

    [Column("counterparty")]
    [MaxLength(120)]
    public string? Counterparty { get; set; }

    [Column("due_at")]
    public DateTime? DueAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("webhook_delivery_logs")]
public class WebhookDeliveryLog
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("webhook_subscription_id")]
    [MaxLength(50)]
    public string WebhookSubscriptionId { get; set; } = string.Empty;

    [ForeignKey(nameof(WebhookSubscriptionId))]
    public WebhookSubscription? WebhookSubscription { get; set; }

    [Column("event_name")]
    [MaxLength(80)]
    public string EventName { get; set; } = string.Empty;

    [Column("delivery_status")]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "PENDING";

    [Column("response_code")]
    public int? ResponseCode { get; set; }

    [Column("attempt_number")]
    public int AttemptNumber { get; set; } = 1;

    [Column("failure_reason")]
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    [Column("delivered_at")]
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
}
