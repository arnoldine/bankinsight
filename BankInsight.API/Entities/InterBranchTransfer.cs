using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("inter_branch_transfers")]
public class InterBranchTransfer
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("from_branch_id")]
    [MaxLength(50)]
    public string FromBranchId { get; set; } = string.Empty;

    [ForeignKey(nameof(FromBranchId))]
    public Branch? FromBranch { get; set; }

    [Required]
    [Column("to_branch_id")]
    [MaxLength(50)]
    public string ToBranchId { get; set; } = string.Empty;

    [ForeignKey(nameof(ToBranchId))]
    public Branch? ToBranch { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("reference")]
    [MaxLength(100)]
    public string? Reference { get; set; }

    [Column("narration")]
    [MaxLength(500)]
    public string? Narration { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [Required]
    [Column("initiated_by")]
    [MaxLength(50)]
    public string InitiatedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(InitiatedBy))]
    public Staff? Initiator { get; set; }

    [Column("approved_by")]
    [MaxLength(50)]
    public string? ApprovedBy { get; set; }

    [ForeignKey(nameof(ApprovedBy))]
    public Staff? Approver { get; set; }

    [Column("sent_by")]
    [MaxLength(50)]
    public string? SentBy { get; set; }

    [ForeignKey(nameof(SentBy))]
    public Staff? Sender { get; set; }

    [Column("received_by")]
    [MaxLength(50)]
    public string? ReceivedBy { get; set; }

    [ForeignKey(nameof(ReceivedBy))]
    public Staff? Receiver { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("dispatched_at")]
    public DateTime? DispatchedAt { get; set; }

    [Column("received_at")]
    public DateTime? ReceivedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("rejection_reason")]
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}
