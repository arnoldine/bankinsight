using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("workspace_preferences")]
public class WorkspacePreference
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [Column("staff_id")]
    [MaxLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [Required]
    [Column("workspace_key")]
    [MaxLength(100)]
    public string WorkspaceKey { get; set; } = string.Empty;

    [Column("view_name")]
    [MaxLength(150)]
    public string? ViewName { get; set; }

    [Column("route")]
    [MaxLength(200)]
    public string? Route { get; set; }

    [Column("filter_json", TypeName = "jsonb")]
    public string? FilterJson { get; set; }

    [Column("is_favorite")]
    public bool IsFavorite { get; set; }

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
