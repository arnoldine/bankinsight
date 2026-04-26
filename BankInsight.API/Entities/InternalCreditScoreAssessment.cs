using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("internal_credit_score_assessments")]
public class InternalCreditScoreAssessment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Column("loan_id")]
    [MaxLength(50)]
    public string? LoanId { get; set; }

    [Column("score")]
    public int Score { get; set; }

    [Column("probability_good")]
    public decimal ProbabilityGood { get; set; }

    [Column("risk_band")]
    [MaxLength(20)]
    public string RiskBand { get; set; } = "UNKNOWN";

    [Column("risk_grade")]
    [MaxLength(20)]
    public string RiskGrade { get; set; } = "UNKNOWN";

    [Column("decision")]
    [MaxLength(20)]
    public string Decision { get; set; } = "REVIEW";

    [Column("recommendation")]
    [MaxLength(200)]
    public string Recommendation { get; set; } = "Manual review";

    [Column("model_version")]
    [MaxLength(50)]
    public string ModelVersion { get; set; } = "ml-credit-v1";

    [Column("training_sample_count")]
    public int TrainingSampleCount { get; set; }

    [Column("feature_payload", TypeName = "jsonb")]
    public string FeaturePayload { get; set; } = "{}";

    [Column("checked_at")]
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
