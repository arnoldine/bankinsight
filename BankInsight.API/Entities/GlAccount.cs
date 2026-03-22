using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("gl_accounts")]
public class GlAccount
{
    [Key]
    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("category")]
    [MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("balance")]
    public decimal Balance { get; set; } = 0;

    [Column("is_header")]
    public bool IsHeader { get; set; } = false;
}
