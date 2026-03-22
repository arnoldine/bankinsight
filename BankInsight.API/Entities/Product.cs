using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BankInsight.API.Entities;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("type")]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("interest_rate")]
    public decimal? InterestRate { get; set; }

    [Column("interest_method")]
    [MaxLength(50)]
    public string? InterestMethod { get; set; }

    [Column("min_amount")]
    public decimal? MinAmount { get; set; }

    [Column("max_amount")]
    public decimal? MaxAmount { get; set; }

    [Column("min_term")]
    public int? MinTerm { get; set; }

    [Column("max_term")]
    public int? MaxTerm { get; set; }

    [Column("default_term")]
    public int? DefaultTerm { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("lending_methodology")]
    [MaxLength(30)]
    public string LendingMethodology { get; set; } = "INDIVIDUAL";

    [Column("is_group_loan_enabled")]
    public bool IsGroupLoanEnabled { get; set; }

    [Column("supports_joint_liability")]
    public bool SupportsJointLiability { get; set; }

    [Column("requires_center")]
    public bool RequiresCenter { get; set; }

    [Column("requires_group")]
    public bool RequiresGroup { get; set; }

    [Column("default_repayment_frequency")]
    [MaxLength(20)]
    public string DefaultRepaymentFrequency { get; set; } = "Monthly";

    [Column("allowed_repayment_frequencies_json")]
    public string AllowedRepaymentFrequenciesJson { get; set; } = "[\"Monthly\"]";

    [Column("supports_weekly_repayment")]
    public bool SupportsWeeklyRepayment { get; set; }

    [Column("minimum_group_size")]
    public int? MinimumGroupSize { get; set; }

    [Column("maximum_group_size")]
    public int? MaximumGroupSize { get; set; }

    [Column("requires_compulsory_savings")]
    public bool RequiresCompulsorySavings { get; set; }

    [Column("minimum_savings_to_loan_ratio")]
    public decimal? MinimumSavingsToLoanRatio { get; set; }

    [Column("requires_group_approval_meeting")]
    public bool RequiresGroupApprovalMeeting { get; set; }

    [Column("uses_member_level_underwriting")]
    public bool UsesMemberLevelUnderwriting { get; set; }

    [Column("uses_group_level_approval")]
    public bool UsesGroupLevelApproval { get; set; }

    [Column("loan_cycle_policy_type")]
    [MaxLength(50)]
    public string? LoanCyclePolicyType { get; set; }

    [Column("max_cycle_number")]
    public int? MaxCycleNumber { get; set; }

    [Column("graduated_cycle_limit_rules_json")]
    public string? GraduatedCycleLimitRulesJson { get; set; }

    [Column("attendance_rule_type")]
    [MaxLength(50)]
    public string? AttendanceRuleType { get; set; }

    [Column("arrears_eligibility_rule_type")]
    [MaxLength(50)]
    public string? ArrearsEligibilityRuleType { get; set; }

    [Column("group_guarantee_policy_type")]
    [MaxLength(50)]
    public string? GroupGuaranteePolicyType { get; set; }

    [Column("meeting_collection_mode")]
    [MaxLength(50)]
    public string? MeetingCollectionMode { get; set; }

    [Column("allow_batch_disbursement")]
    public bool AllowBatchDisbursement { get; set; }

    [Column("allow_member_level_disbursement_adjustment")]
    public bool AllowMemberLevelDisbursementAdjustment { get; set; }

    [Column("allow_top_up_within_group")]
    public bool AllowTopUpWithinGroup { get; set; }

    [Column("allow_reschedule_within_group")]
    public bool AllowRescheduleWithinGroup { get; set; }

    [Column("group_penalty_policy")]
    [MaxLength(50)]
    public string? GroupPenaltyPolicy { get; set; }

    [Column("group_delinquency_policy")]
    [MaxLength(50)]
    public string? GroupDelinquencyPolicy { get; set; }

    [Column("group_officer_assignment_mode")]
    [MaxLength(50)]
    public string? GroupOfficerAssignmentMode { get; set; }

    [JsonIgnore]
    public ProductGroupRule? GroupRule { get; set; }

    [JsonIgnore]
    public ProductEligibilityRule? EligibilityRule { get; set; }
}
