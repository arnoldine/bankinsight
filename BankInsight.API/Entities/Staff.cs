using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("staff")]
public class Staff
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("password_hash")]
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [Column("access_scope_type")]
    public AccessScopeType AccessScopeType { get; set; } = AccessScopeType.All;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    [Column("branch_id")]
    [MaxLength(50)]
    public string? BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("avatar_initials")]
    [MaxLength(5)]
    public string? AvatarInitials { get; set; }

    [Column("clerk_user_id")]
    [MaxLength(100)]
    public string? ClerkUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
