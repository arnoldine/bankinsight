using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("user_sessions")]
public class UserSession
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("staff_id")]
    [MaxLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }

    [Required]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("refresh_token")]
    public string? RefreshToken { get; set; }

    [Required]
    [Column("ip_address")]
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("last_activity")]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("logout_at")]
    public DateTime? LogoutAt { get; set; }
}


