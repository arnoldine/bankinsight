using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("customer_media_assets")]
public class CustomerMediaAsset
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("media_type")]
    [MaxLength(30)]
    public string MediaType { get; set; } = string.Empty;

    [Column("media_side")]
    [MaxLength(10)]
    public string? MediaSide { get; set; }

    [Column("file_name")]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Column("content_type")]
    [MaxLength(100)]
    public string ContentType { get; set; } = "image/png";

    [Column("data_url")]
    public string DataUrl { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("file_size_bytes")]
    public long? FileSizeBytes { get; set; }

    [Column("uploaded_by")]
    [MaxLength(100)]
    public string? UploadedBy { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
