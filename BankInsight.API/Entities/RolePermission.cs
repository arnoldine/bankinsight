using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("role_permissions")]
public class RolePermission
{
    [Column("role_id")]
    [MaxLength(50)]
    public string RoleId { get; set; } = default!;

    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = default!;

    [Column("permission_id")]
    public Guid PermissionId { get; set; }

    [ForeignKey(nameof(PermissionId))]
    public Permission Permission { get; set; } = default!;
}
