using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("regulatory_variance_events")]
public class RegulatoryVarianceEvent
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

    [Required]
    [Column("event_type")]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [Column("performed_by_user_id")]
    [MaxLength(50)]
    public string? PerformedByUserId { get; set; }

    [Column("performed_by_name")]
    [MaxLength(150)]
    public string? PerformedByName { get; set; }

    [Column("detail")]
    [MaxLength(2000)]
    public string Detail { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
