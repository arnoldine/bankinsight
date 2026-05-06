using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("relationship_ownership_assignments")]
public class RelationshipOwnershipAssignment
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

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

    [Column("assignment_note")]
    [MaxLength(2000)]
    public string? AssignmentNote { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
