using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("approval_requests")]
public class ApprovalRequest
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("workflow_id")]
    [MaxLength(50)]
    public string? WorkflowId { get; set; }

    [ForeignKey(nameof(WorkflowId))]
    public Workflow? Workflow { get; set; }

    [Required]
    [Column("entity_type")]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [Column("entity_id")]
    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty;

    [Column("requester_id")]
    [MaxLength(50)]
    public string? RequesterId { get; set; }

    [ForeignKey(nameof(RequesterId))]
    public Staff? Requester { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("current_step")]
    public int CurrentStep { get; set; } = 0;

    [Column("approved_by_user_id")]
    public Guid? ApprovedByUserId { get; set; }

    [Column("approved_at_utc")]
    public DateTime? ApprovedAtUtc { get; set; }

    [Column("payload_json")]
    public string? PayloadJson { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("reference_no")]
    public string? ReferenceNo { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
