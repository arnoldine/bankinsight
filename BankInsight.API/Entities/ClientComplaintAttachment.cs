using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("client_complaint_attachments")]
public class ClientComplaintAttachment
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
    [Column("file_name")]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Column("content_type")]
    [MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    [Required]
    [Column("data_url")]
    public string DataUrl { get; set; } = string.Empty;

    [Column("uploaded_by")]
    [MaxLength(100)]
    public string? UploadedBy { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING_SCAN";

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
