using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("client_kyc_case_events")]
public class ClientKycCaseEvent
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("kyc_case_id")]
    [MaxLength(50)]
    public string KycCaseId { get; set; } = string.Empty;

    [ForeignKey(nameof(KycCaseId))]
    public ClientKycCase? KycCase { get; set; }

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

    [Column("actor_id")]
    [MaxLength(50)]
    public string? ActorId { get; set; }

    [Column("actor_name")]
    [MaxLength(100)]
    public string? ActorName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
