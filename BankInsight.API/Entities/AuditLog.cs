using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("action")]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [Column("entity_type")]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [Column("entity_id")]
    [MaxLength(50)]
    public string? EntityId { get; set; }

    [Column("user_id")]
    [MaxLength(50)]
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public Staff? User { get; set; }

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("old_values")]
    [MaxLength(2000)]
    public string? OldValues { get; set; } // JSON string

    [Column("new_values")]
    [MaxLength(2000)]
    public string? NewValues { get; set; } // JSON string

    [Column("ip_address")]
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Column("payload_json")]
    public string? PayloadJson { get; set; }

    [Column("is_success")]
    public bool IsSuccess { get; set; } = true;

    [Column("failure_reason")]
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "SUCCESS"; // SUCCESS, FAILED, PENDING

    [Column("error_message")]
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(50)]
    public string? CreatedBy { get; set; }
}
