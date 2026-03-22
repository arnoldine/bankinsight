using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("privilege_leases")]
public class PrivilegeLease
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("staff_id")]
    [MaxLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }

    [Required]
    [Column("permission")]
    [MaxLength(100)]
    public string Permission { get; set; } = string.Empty;

    [Required]
    [Column("reason")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [Column("approved_by")]
    [MaxLength(50)]
    public string ApprovedBy { get; set; } = string.Empty;

    [Column("approved_at")]
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    [Column("starts_at")]
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; }

    [Column("revoked_by")]
    [MaxLength(50)]
    public string? RevokedBy { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
