using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Entities;

[Table("treasury_positions")]
public class TreasuryPosition
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("position_date")]
    public DateTime PositionDate { get; set; } // Date of position

    [Required]
    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = null!; // GHS, USD, EUR, etc.

    [Required]
    [Column("opening_balance")]
    [Precision(18, 2)]
    public decimal OpeningBalance { get; set; }

    [Column("deposits")]
    [Precision(18, 2)]
    public decimal Deposits { get; set; }

    [Column("withdrawals")]
    [Precision(18, 2)]
    public decimal Withdrawals { get; set; }

    [Column("fx_gains_losses")]
    [Precision(18, 2)]
    public decimal FxGainsLosses { get; set; } // Realized FX gains/losses

    [Column("other_movements")]
    [Precision(18, 2)]
    public decimal OtherMovements { get; set; }

    [Required]
    [Column("closing_balance")]
    [Precision(18, 2)]
    public decimal ClosingBalance { get; set; }

    [Column("nostro_balance")]
    [Precision(18, 2)]
    public decimal? NostroBalance { get; set; } // Balance in correspondent bank accounts

    [Column("vault_balance")]
    [Precision(18, 2)]
    public decimal? VaultBalance { get; set; } // Physical cash in vaults

    [Column("overnight_placement")]
    [Precision(18, 2)]
    public decimal? OvernightPlacement { get; set; } // Money market placements

    [Column("exposure_limit")]
    [Precision(18, 2)]
    public decimal? ExposureLimit { get; set; }

    [Column("position_status")]
    [StringLength(50)]
    public string PositionStatus { get; set; } = "Open"; // Open, Closed, Reconciled

    [Column("reconciled_at")]
    public DateTime? ReconciledAt { get; set; }

    [Column("reconciled_by")]
    [StringLength(50)]
    public string? ReconciledBy { get; set; }

    [ForeignKey("ReconciledBy")]
    public Staff? Reconciler { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }
}
