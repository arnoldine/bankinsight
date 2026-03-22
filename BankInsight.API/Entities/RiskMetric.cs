using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Entities;

[Table("risk_metrics")]
public class RiskMetric
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("metric_date")]
    public DateTime MetricDate { get; set; }

    [Required]
    [Column("metric_type")]
    [StringLength(50)]
    public string MetricType { get; set; } = null!; // VaR, LCR, NSFR, CurrencyExposure, InterestRateRisk

    [Column("currency")]
    [StringLength(3)]
    public string? Currency { get; set; } // For currency-specific metrics

    [Column("metric_value")]
    [Precision(18, 6)]
    public decimal MetricValue { get; set; }

    [Column("threshold")]
    [Precision(18, 6)]
    public decimal? Threshold { get; set; } // Regulatory or internal limit

    [Column("threshold_breached")]
    public bool ThresholdBreached { get; set; }

    [Column("confidence_level")]
    [Precision(5, 2)]
    public decimal? ConfidenceLevel { get; set; } // For VaR calculations (e.g., 95%, 99%)

    [Column("time_horizon_days")]
    public int? TimeHorizonDays { get; set; } // For VaR (1-day, 10-day, etc.)

    [Column("calculation_method")]
    [StringLength(100)]
    public string? CalculationMethod { get; set; } // Historical, Monte Carlo, Parametric

    [Column("position_snapshot")]
    [StringLength(4000)]
    public string? PositionSnapshot { get; set; } // JSON snapshot of positions

    [Column("exposure_details")]
    [StringLength(4000)]
    public string? ExposureDetails { get; set; } // JSON details of exposure breakdown

    [Column("calculated_by")]
    [StringLength(50)]
    public string? CalculatedBy { get; set; }

    [ForeignKey("CalculatedBy")]
    public Staff? Calculator { get; set; }

    [Column("calculated_at")]
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    [Column("reviewed_by")]
    [StringLength(50)]
    public string? ReviewedBy { get; set; }

    [ForeignKey("ReviewedBy")]
    public Staff? Reviewer { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "Calculated"; // Calculated, Reviewed, Escalated

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column("alert_triggered")]
    public bool AlertTriggered { get; set; }

    [Column("alert_sent_at")]
    public DateTime? AlertSentAt { get; set; }
}
