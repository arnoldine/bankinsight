using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Entities;

[Table("investments")]
public class Investment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("investment_number")]
    [StringLength(50)]
    public string InvestmentNumber { get; set; } = null!; // INV-YYYYMMDD-XXXXX

    [Required]
    [Column("investment_type")]
    [StringLength(50)]
    public string InvestmentType { get; set; } = null!; // MoneyMarket, TreasuryBill, Bond, FixedDeposit

    [Required]
    [Column("instrument")]
    [StringLength(100)]
    public string Instrument { get; set; } = null!; // 91-Day TB, 182-Day TB, etc.

    [Required]
    [Column("counterparty")]
    [StringLength(200)]
    public string Counterparty { get; set; } = null!; // Bank of Ghana, Bank name, etc.

    [Required]
    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = null!;

    [Required]
    [Column("principal_amount")]
    [Precision(18, 2)]
    public decimal PrincipalAmount { get; set; }

    [Required]
    [Column("interest_rate")]
    [Precision(18, 6)]
    public decimal InterestRate { get; set; } // Annual rate

    [Column("discount_rate")]
    [Precision(18, 6)]
    public decimal? DiscountRate { get; set; } // For discount instruments like T-Bills

    [Required]
    [Column("placement_date")]
    public DateTime PlacementDate { get; set; }

    [Required]
    [Column("maturity_date")]
    public DateTime MaturityDate { get; set; }

    [Column("tenor_days")]
    public int TenorDays { get; set; } // Number of days

    [Column("interest_amount")]
    [Precision(18, 2)]
    public decimal? InterestAmount { get; set; }

    [Column("maturity_value")]
    [Precision(18, 2)]
    public decimal? MaturityValue { get; set; } // Principal + Interest

    [Column("purchase_price")]
    [Precision(18, 2)]
    public decimal? PurchasePrice { get; set; } // For discount instruments

    [Column("yield_to_maturity")]
    [Precision(18, 6)]
    public decimal? YieldToMaturity { get; set; }

    [Required]
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "Active"; // Active, Matured, Rolled-Over, Liquidated

    [Column("rollover_to")]
    public int? RolloverTo { get; set; } // ID of new investment if rolled over

    [ForeignKey("RolloverTo")]
    public Investment? RolloverInvestment { get; set; }

    [Column("initiated_by")]
    [StringLength(50)]
    public string InitiatedBy { get; set; } = null!;

    [ForeignKey("InitiatedBy")]
    public Staff Initiator { get; set; } = null!;

    [Column("approved_by")]
    [StringLength(50)]
    public string? ApprovedBy { get; set; }

    [ForeignKey("ApprovedBy")]
    public Staff? Approver { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("matured_at")]
    public DateTime? MaturedAt { get; set; }

    [Column("settlement_account")]
    [StringLength(50)]
    public string? SettlementAccount { get; set; } // GL account for settlements

    [Column("accrued_interest")]
    [Precision(18, 2)]
    public decimal AccruedInterest { get; set; } // Current accrued interest

    [Column("last_accrual_date")]
    public DateTime? LastAccrualDate { get; set; }

    [Column("reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
