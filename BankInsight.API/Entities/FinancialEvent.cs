using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("financial_events")]
public class FinancialEvent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("event_type")]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Column("entity_type")]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [Column("entity_id")]
    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(3)]
    public string Currency { get; set; } = "GHS";

    [Column("branch_id")]
    [MaxLength(50)]
    public string? BranchId { get; set; }

    [Column("reference")]
    [MaxLength(100)]
    public string? Reference { get; set; }

    [Column("payload_json", TypeName = "jsonb")]
    public string PayloadJson { get; set; } = "{}";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(50)]
    public string? CreatedBy { get; set; }
}
