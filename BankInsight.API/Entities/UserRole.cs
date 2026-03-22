using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("user_roles")]
public class UserRole
{
    [Column("user_id")]
    [MaxLength(50)]
    public string UserId { get; set; } = default!;

    [ForeignKey(nameof(UserId))]
    public Staff User { get; set; } = default!;

    [Column("role_id")]
    [MaxLength(50)]
    public string RoleId { get; set; } = default!;

    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = default!;

    [Column("assigned_at_utc")]
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("assigned_by_user_id")]
    public Guid? AssignedByUserId { get; set; }
}
