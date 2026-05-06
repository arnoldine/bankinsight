using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("reconciliation_exceptions")]
public class ReconciliationException
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("category")]
    [MaxLength(30)]
    public string Category { get; set; } = string.Empty;

    [Column("source_system")]
    [MaxLength(50)]
    public string SourceSystem { get; set; } = string.Empty;

    [Column("reference")]
    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "OPEN";

    [Column("severity")]
    [MaxLength(20)]
    public string Severity { get; set; } = "MEDIUM";

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("owner_user_id")]
    [MaxLength(50)]
    public string? OwnerUserId { get; set; }

    [Column("summary")]
    [MaxLength(255)]
    public string Summary { get; set; } = string.Empty;

    [Column("detail")]
    [MaxLength(2000)]
    public string Detail { get; set; } = string.Empty;

    [Column("detected_at")]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    [Column("due_at")]
    public DateTime? DueAt { get; set; }

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("retry_count")]
    public int RetryCount { get; set; }

    [Column("last_attempt_at")]
    public DateTime? LastAttemptAt { get; set; }

    [Column("workflow_stage")]
    [MaxLength(40)]
    public string? WorkflowStage { get; set; }

    [Column("resolution_code")]
    [MaxLength(40)]
    public string? ResolutionCode { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
