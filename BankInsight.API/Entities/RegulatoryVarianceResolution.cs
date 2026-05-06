using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("regulatory_variance_resolutions")]
public class RegulatoryVarianceResolution
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [Column("reference")]
    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    [Required]
    [Column("return_type")]
    [MaxLength(100)]
    public string ReturnType { get; set; } = string.Empty;

    [Column("resolution_status")]
    [MaxLength(20)]
    public string ResolutionStatus { get; set; } = "OPEN";

    [Column("owner_user_id")]
    [MaxLength(50)]
    public string? OwnerUserId { get; set; }

    [Column("owner_name")]
    [MaxLength(150)]
    public string? OwnerName { get; set; }

    [Column("assigned_by_user_id")]
    [MaxLength(50)]
    public string? AssignedByUserId { get; set; }

    [Column("assigned_by_name")]
    [MaxLength(150)]
    public string? AssignedByName { get; set; }

    [Column("assigned_at")]
    public DateTime? AssignedAt { get; set; }

    [Column("resolution_note")]
    [MaxLength(2000)]
    public string? ResolutionNote { get; set; }

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
