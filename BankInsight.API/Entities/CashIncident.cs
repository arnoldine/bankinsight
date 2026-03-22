using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("cash_incidents")]
public class CashIncident
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = string.Empty;

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("store_type")]
    [MaxLength(30)]
    public string StoreType { get; set; } = string.Empty;

    [Column("store_id")]
    [MaxLength(100)]
    public string StoreId { get; set; } = string.Empty;

    [Column("incident_type")]
    [MaxLength(30)]
    public string IncidentType { get; set; } = string.Empty;

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "OPEN";

    [Column("reference")]
    [MaxLength(100)]
    public string? Reference { get; set; }

    [Column("narration")]
    [MaxLength(1000)]
    public string? Narration { get; set; }

    [Column("reported_by")]
    [MaxLength(50)]
    public string? ReportedBy { get; set; }

    [ForeignKey(nameof(ReportedBy))]
    public Staff? ReportedByStaff { get; set; }

    [Column("resolved_by")]
    [MaxLength(50)]
    public string? ResolvedBy { get; set; }

    [ForeignKey(nameof(ResolvedBy))]
    public Staff? ResolvedByStaff { get; set; }

    [Column("reported_at")]
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }
}
