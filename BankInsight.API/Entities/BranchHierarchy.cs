using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("branch_hierarchy")]
public class BranchHierarchy
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

    [Column("parent_branch_id")]
    [MaxLength(50)]
    public string? ParentBranchId { get; set; }

    [ForeignKey(nameof(ParentBranchId))]
    public Branch? ParentBranch { get; set; }

    [Required]
    [Column("level")]
    public int Level { get; set; } = 0;

    [Column("path")]
    [MaxLength(500)]
    public string? Path { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
