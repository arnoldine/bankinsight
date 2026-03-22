using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("user_activities")]
public class UserActivity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("staff_id")]
    [MaxLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }

    [Required]
    [Column("action")]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [Column("entity_type")]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [Column("entity_id")]
    [MaxLength(50)]
    public string? EntityId { get; set; }

    [Column("before_value")]
    public string? BeforeValue { get; set; }

    [Column("after_value")]
    public string? AfterValue { get; set; }

    [Column("ip_address")]
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("session_id")]
    [MaxLength(50)]
    public string? SessionId { get; set; }

    [ForeignKey(nameof(SessionId))]
    public UserSession? Session { get; set; }
}
