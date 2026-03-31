using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("product_charge_definitions")]
public class ProductChargeDefinition
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("product_id")]
    [MaxLength(50)]
    public string ProductId { get; set; } = string.Empty;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("charge_type")]
    [MaxLength(20)]
    public string ChargeType { get; set; } = "FEE";

    [Required]
    [Column("calculation_type")]
    [MaxLength(20)]
    public string CalculationType { get; set; } = "FLAT";

    [Column("flat_amount")]
    public decimal? FlatAmount { get; set; }

    [Column("rate")]
    public decimal? Rate { get; set; }

    [Column("minimum_amount")]
    public decimal? MinimumAmount { get; set; }

    [Column("maximum_amount")]
    public decimal? MaximumAmount { get; set; }

    [Column("apply_on")]
    [MaxLength(30)]
    public string ApplyOn { get; set; } = "MANUAL";

    [Column("income_gl_code")]
    [MaxLength(50)]
    public string? IncomeGlCode { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
