using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("branches")]
public class Branch
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
    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("location")]
    [MaxLength(255)]
    public string? Location { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";
}
