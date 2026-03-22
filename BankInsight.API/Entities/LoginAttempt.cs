using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("login_attempts")]
public class LoginAttempt
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("email")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("ip_address")]
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    [Column("user_agent")]
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Required]
    [Column("success")]
    public bool Success { get; set; }

    [Column("failure_reason")]
    [MaxLength(255)]
    public string? FailureReason { get; set; }

    [Column("attempted_at")]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    [Column("staff_id")]
    [MaxLength(50)]
    public string? StaffId { get; set; }

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }
}
