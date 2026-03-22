using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Entities;

[Table("fx_rates")]
public class FxRate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("base_currency")]
    [StringLength(3)]
    public string BaseCurrency { get; set; } = "GHS"; // Base currency (GHS)

    [Required]
    [Column("target_currency")]
    [StringLength(3)]
    public string TargetCurrency { get; set; } = null!; // USD, EUR, GBP, etc.

    [Required]
    [Column("buy_rate")]
    [Precision(18, 6)]
    public decimal BuyRate { get; set; } // Bank buys foreign currency at this rate

    [Required]
    [Column("sell_rate")]
    [Precision(18, 6)]
    public decimal SellRate { get; set; } // Bank sells foreign currency at this rate

    [Column("mid_rate")]
    [Precision(18, 6)]
    public decimal? MidRate { get; set; } // (Buy + Sell) / 2

    [Column("official_rate")]
    [Precision(18, 6)]
    public decimal? OfficialRate { get; set; } // Official BoG interbank rate

    [Required]
    [Column("rate_date")]
    public DateTime RateDate { get; set; } // Date for which rate is valid

    [Column("source")]
    [StringLength(100)]
    public string Source { get; set; } = "Manual"; // BoG API, Manual, Reuters, etc.

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true; // Current active rate

    [Column("notes")]
    [StringLength(500)]
    public string? Notes { get; set; }
}
