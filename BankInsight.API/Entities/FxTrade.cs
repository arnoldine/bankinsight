using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Entities;

[Table("fx_trades")]
public class FxTrade
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("deal_number")]
    [StringLength(50)]
    public string DealNumber { get; set; } = null!; // FX-YYYYMMDD-XXXXX

    [Required]
    [Column("trade_date")]
    public DateTime TradeDate { get; set; }

    [Required]
    [Column("value_date")]
    public DateTime ValueDate { get; set; } // Settlement date

    [Required]
    [Column("trade_type")]
    [StringLength(20)]
    public string TradeType { get; set; } = null!; // Spot, Forward, Swap

    [Required]
    [Column("direction")]
    [StringLength(10)]
    public string Direction { get; set; } = null!; // Buy, Sell

    [Required]
    [Column("base_currency")]
    [StringLength(3)]
    public string BaseCurrency { get; set; } = null!;

    [Required]
    [Column("base_amount")]
    [Precision(18, 2)]
    public decimal BaseAmount { get; set; }

    [Required]
    [Column("counter_currency")]
    [StringLength(3)]
    public string CounterCurrency { get; set; } = null!;

    [Required]
    [Column("counter_amount")]
    [Precision(18, 2)]
    public decimal CounterAmount { get; set; }

    [Required]
    [Column("exchange_rate")]
    [Precision(18, 6)]
    public decimal ExchangeRate { get; set; }

    [Column("customer_rate")]
    [Precision(18, 6)]
    public decimal? CustomerRate { get; set; } // Rate offered to customer

    [Column("spread")]
    [Precision(18, 6)]
    public decimal? Spread { get; set; } // Profit margin

    [Column("customer_id")]
    [StringLength(50)]
    public string? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    [Column("counterparty")]
    [StringLength(200)]
    public string? Counterparty { get; set; } // Bank or institution

    [Required]
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Settled, Cancelled

    [Column("settlement_status")]
    [StringLength(20)]
    public string? SettlementStatus { get; set; } // Pending, Settled, Failed

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

    [Column("settled_at")]
    public DateTime? SettledAt { get; set; }

    [Column("profit_loss")]
    [Precision(18, 2)]
    public decimal? ProfitLoss { get; set; } // P&L on the trade

    [Column("narration")]
    [StringLength(500)]
    public string? Narration { get; set; }

    [Column("reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
