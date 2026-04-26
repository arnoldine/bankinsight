using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("digital_investment_profiles")]
public class DigitalInvestmentProfile
{
    [Key]
    [Column("id")]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("account_id")]
    [StringLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Required]
    [Column("customer_id")]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Required]
    [Column("funding_account_id")]
    [StringLength(50)]
    public string FundingAccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(FundingAccountId))]
    public Account? FundingAccount { get; set; }

    [Required]
    [Column("product_code")]
    [StringLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Column("tenor_days")]
    public int TenorDays { get; set; }

    [Column("rate")]
    public decimal Rate { get; set; }

    [Column("payout_option")]
    [StringLength(30)]
    public string PayoutOption { get; set; } = "AT_MATURITY";

    [Column("auto_rollover")]
    public bool AutoRollover { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("start_date")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [Column("maturity_date")]
    public DateTime MaturityDate { get; set; }

    [Column("matured_at")]
    public DateTime? MaturedAt { get; set; }

    [Column("liquidated_at")]
    public DateTime? LiquidatedAt { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
