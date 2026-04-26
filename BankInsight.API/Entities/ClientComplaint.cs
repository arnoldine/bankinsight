using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("client_complaints")]
public class ClientComplaint
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("reference")]
    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    [Required]
    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("submitted_by_user_id")]
    [MaxLength(50)]
    public string? SubmittedByUserId { get; set; }

    [Required]
    [Column("category")]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Column("summary")]
    [MaxLength(255)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    [Column("details")]
    public string Details { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "ACKNOWLEDGED";

    [Column("owner_team")]
    [MaxLength(100)]
    public string OwnerTeam { get; set; } = "Customer Operations";

    [Column("sla_due_at")]
    public DateTime SlaDueAt { get; set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClientComplaintEvent> Events { get; set; } = new List<ClientComplaintEvent>();
    public ICollection<ClientComplaintAttachment> Attachments { get; set; } = new List<ClientComplaintAttachment>();
}
