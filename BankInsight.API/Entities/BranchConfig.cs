using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("branch_configs")]
public class BranchConfig
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = string.Empty;

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Required]
    [Column("config_key")]
    [MaxLength(100)]
    public string ConfigKey { get; set; } = string.Empty;

    [Column("config_value")]
    public string? ConfigValue { get; set; }

    [Column("data_type")]
    [MaxLength(20)]
    public string DataType { get; set; } = "string";

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
