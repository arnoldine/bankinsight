using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("permissions")]
public class Permission
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("code")]
    [MaxLength(100)]
    public string Code { get; set; } = default!;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    [Required]
    [Column("module")]
    [MaxLength(100)]
    public string Module { get; set; } = default!;

    [Column("description")]
    [MaxLength(500)]
    public string Description { get; set; } = default!;

    [Column("is_system_permission")]
    public bool IsSystemPermission { get; set; } = true;

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
