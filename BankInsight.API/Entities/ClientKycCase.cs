using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("client_kyc_cases")]
public class ClientKycCase
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

    [Required]
    [Column("status")]
    [MaxLength(30)]
    public string Status { get; set; } = "SUBMITTED";

    [Required]
    [Column("reason")]
    [MaxLength(255)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [Column("summary")]
    [MaxLength(1000)]
    public string Summary { get; set; } = string.Empty;

    [Column("reviewer_user_id")]
    [MaxLength(50)]
    public string? ReviewerUserId { get; set; }

    [Column("reviewer_name")]
    [MaxLength(100)]
    public string? ReviewerName { get; set; }

    [Column("decision_note")]
    [MaxLength(1000)]
    public string? DecisionNote { get; set; }

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClientKycCaseEvent> Events { get; set; } = new List<ClientKycCaseEvent>();
}
