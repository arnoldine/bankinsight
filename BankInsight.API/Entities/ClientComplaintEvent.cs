using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("client_complaint_events")]
public class ClientComplaintEvent
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("complaint_id")]
    [MaxLength(50)]
    public string ComplaintId { get; set; } = string.Empty;

    [ForeignKey(nameof(ComplaintId))]
    public ClientComplaint? Complaint { get; set; }

    [Required]
    [Column("event_type")]
    [MaxLength(50)]
    public string EventType { get; set; } = "STATUS_UPDATE";

    [Required]
    [Column("title")]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("visibility")]
    [MaxLength(20)]
    public string Visibility { get; set; } = "CUSTOMER";

    [Column("actor_id")]
    [MaxLength(50)]
    public string? ActorId { get; set; }

    [Column("actor_name")]
    [MaxLength(100)]
    public string? ActorName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
