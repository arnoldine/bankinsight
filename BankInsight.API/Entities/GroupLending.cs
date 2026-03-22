using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("lending_centers")]
public class LendingCenter
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = "BR001";

    [Column("center_code")]
    [MaxLength(50)]
    public string CenterCode { get; set; } = string.Empty;

    [Column("center_name")]
    [MaxLength(150)]
    public string CenterName { get; set; } = string.Empty;

    [Column("meeting_day_of_week")]
    [MaxLength(20)]
    public string? MeetingDayOfWeek { get; set; }

    [Column("meeting_location")]
    [MaxLength(200)]
    public string? MeetingLocation { get; set; }

    [Column("assigned_officer_id")]
    [MaxLength(50)]
    public string? AssignedOfficerId { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";
}

[Table("product_group_rules")]
public class ProductGroupRule
{
    [Key]
    [Column("product_id")]
    [MaxLength(50)]
    public string ProductId { get; set; } = string.Empty;

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Column("min_members_required")]
    public int MinMembersRequired { get; set; }

    [Column("max_members_allowed")]
    public int MaxMembersAllowed { get; set; }

    [Column("min_weeks")]
    public int? MinWeeks { get; set; }

    [Column("max_weeks")]
    public int? MaxWeeks { get; set; }

    [Column("requires_compulsory_savings")]
    public bool RequiresCompulsorySavings { get; set; }

    [Column("min_savings_to_loan_ratio")]
    public decimal? MinSavingsToLoanRatio { get; set; }

    [Column("requires_group_approval_meeting")]
    public bool RequiresGroupApprovalMeeting { get; set; }

    [Column("requires_joint_liability")]
    public bool RequiresJointLiability { get; set; }

    [Column("allow_top_up")]
    public bool AllowTopUp { get; set; }

    [Column("allow_reschedule")]
    public bool AllowReschedule { get; set; }

    [Column("max_cycle_number")]
    public int? MaxCycleNumber { get; set; }

    [Column("cycle_increment_rules_json")]
    public string? CycleIncrementRulesJson { get; set; }

    [Column("default_repayment_frequency")]
    [MaxLength(20)]
    public string DefaultRepaymentFrequency { get; set; } = "Weekly";

    [Column("default_interest_method")]
    [MaxLength(30)]
    public string DefaultInterestMethod { get; set; } = "Flat";

    [Column("penalty_policy_json")]
    public string? PenaltyPolicyJson { get; set; }

    [Column("attendance_rule_json")]
    public string? AttendanceRuleJson { get; set; }

    [Column("eligibility_rule_json")]
    public string? EligibilityRuleJson { get; set; }

    [Column("meeting_collection_rule_json")]
    public string? MeetingCollectionRuleJson { get; set; }

    [Column("allocation_order_json")]
    public string? AllocationOrderJson { get; set; }

    [Column("accounting_profile_json")]
    public string? AccountingProfileJson { get; set; }

    [Column("disclosure_template")]
    [MaxLength(100)]
    public string? DisclosureTemplate { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("product_eligibility_rules")]
public class ProductEligibilityRule
{
    [Key]
    [Column("product_id")]
    [MaxLength(50)]
    public string ProductId { get; set; } = string.Empty;

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Column("requires_kyc_complete")]
    public bool RequiresKycComplete { get; set; } = true;

    [Column("block_on_severe_arrears")]
    public bool BlockOnSevereArrears { get; set; } = true;

    [Column("max_allowed_exposure")]
    public decimal? MaxAllowedExposure { get; set; }

    [Column("min_membership_days")]
    public int? MinMembershipDays { get; set; }

    [Column("min_attendance_rate")]
    public decimal? MinAttendanceRate { get; set; }

    [Column("require_credit_bureau_check")]
    public bool RequireCreditBureauCheck { get; set; }

    [Column("credit_bureau_provider")]
    [MaxLength(50)]
    public string? CreditBureauProvider { get; set; }

    [Column("minimum_credit_score")]
    public int? MinimumCreditScore { get; set; }

    [Column("rule_json")]
    public string? RuleJson { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("group_loan_applications")]
public class GroupLoanApplication
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    [MaxLength(50)]
    public string GroupId { get; set; } = string.Empty;

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    [Column("loan_cycle_no")]
    public int LoanCycleNo { get; set; }

    [Column("application_date")]
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

    [Column("product_id")]
    [MaxLength(50)]
    public string ProductId { get; set; } = string.Empty;

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = "BR001";

    [Column("officer_id")]
    [MaxLength(50)]
    public string? OfficerId { get; set; }

    [Column("status")]
    [MaxLength(30)]
    public string Status { get; set; } = "DRAFT";

    [Column("total_approved_amount")]
    public decimal TotalApprovedAmount { get; set; }

    [Column("total_requested_amount")]
    public decimal TotalRequestedAmount { get; set; }

    [Column("total_disbursed_amount")]
    public decimal TotalDisbursedAmount { get; set; }

    [Column("approval_date")]
    public DateTime? ApprovalDate { get; set; }

    [Column("disbursement_date")]
    public DateTime? DisbursementDate { get; set; }

    [Column("meeting_reference")]
    [MaxLength(50)]
    public string? MeetingReference { get; set; }

    [Column("group_resolution_reference")]
    [MaxLength(50)]
    public string? GroupResolutionReference { get; set; }

    [Column("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Column("disclosed_terms_snapshot_json")]
    public string? DisclosedTermsSnapshotJson { get; set; }

    public ICollection<GroupLoanApplicationMember> Members { get; set; } = new List<GroupLoanApplicationMember>();
}

[Table("group_loan_application_members")]
public class GroupLoanApplicationMember
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("group_loan_application_id")]
    [MaxLength(50)]
    public string GroupLoanApplicationId { get; set; } = string.Empty;

    [ForeignKey(nameof(GroupLoanApplicationId))]
    public GroupLoanApplication? GroupLoanApplication { get; set; }

    [Column("group_member_id")]
    [MaxLength(50)]
    public string GroupMemberId { get; set; } = string.Empty;

    [ForeignKey(nameof(GroupMemberId))]
    public GroupMember? GroupMember { get; set; }

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Column("requested_amount")]
    public decimal RequestedAmount { get; set; }

    [Column("approved_amount")]
    public decimal ApprovedAmount { get; set; }

    [Column("disbursed_amount")]
    public decimal DisbursedAmount { get; set; }

    [Column("tenure_weeks")]
    public int TenureWeeks { get; set; }

    [Column("interest_rate")]
    public decimal InterestRate { get; set; }

    [Column("interest_method")]
    [MaxLength(30)]
    public string InterestMethod { get; set; } = "Flat";

    [Column("repayment_frequency")]
    [MaxLength(20)]
    public string RepaymentFrequency { get; set; } = "Weekly";

    [Column("loan_purpose")]
    [MaxLength(250)]
    public string? LoanPurpose { get; set; }

    [Column("score_result")]
    [MaxLength(100)]
    public string? ScoreResult { get; set; }

    [Column("eligibility_status")]
    [MaxLength(30)]
    public string EligibilityStatus { get; set; } = "PENDING";

    [Column("credit_bureau_check_id")]
    public Guid? CreditBureauCheckId { get; set; }

    [Column("existing_exposure_amount")]
    public decimal ExistingExposureAmount { get; set; }

    [Column("savings_balance_at_application")]
    public decimal SavingsBalanceAtApplication { get; set; }

    [Column("guarantor_notes")]
    [MaxLength(500)]
    public string? GuarantorNotes { get; set; }

    [Column("status")]
    [MaxLength(30)]
    public string Status { get; set; } = "DRAFT";
}

[Table("group_loan_accounts")]
public class GroupLoanAccount
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("loan_account_id")]
    [MaxLength(50)]
    public string LoanAccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanAccountId))]
    public LoanAccount? LoanAccount { get; set; }

    [Column("group_id")]
    [MaxLength(50)]
    public string GroupId { get; set; } = string.Empty;

    [Column("group_loan_application_id")]
    [MaxLength(50)]
    public string GroupLoanApplicationId { get; set; } = string.Empty;

    [Column("group_member_id")]
    [MaxLength(50)]
    public string GroupMemberId { get; set; } = string.Empty;

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Column("loan_cycle_no")]
    public int LoanCycleNo { get; set; }

    [Column("group_guarantee_reference")]
    [MaxLength(100)]
    public string? GroupGuaranteeReference { get; set; }

    [Column("is_under_joint_liability")]
    public bool IsUnderJointLiability { get; set; }

    [Column("meeting_day_of_week")]
    [MaxLength(20)]
    public string? MeetingDayOfWeek { get; set; }

    [Column("assigned_officer_id")]
    [MaxLength(50)]
    public string? AssignedOfficerId { get; set; }

    [Column("restructured_flag")]
    public bool RestructuredFlag { get; set; }

    [Column("impairment_stage_hint")]
    [MaxLength(20)]
    public string? ImpairmentStageHint { get; set; }
}

[Table("group_meetings")]
public class GroupMeeting
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    [MaxLength(50)]
    public string GroupId { get; set; } = string.Empty;

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    [Column("center_id")]
    [MaxLength(50)]
    public string? CenterId { get; set; }

    [Column("meeting_date")]
    public DateOnly MeetingDate { get; set; }

    [Column("meeting_type")]
    [MaxLength(30)]
    public string MeetingType { get; set; } = "REGULAR";

    [Column("location")]
    [MaxLength(200)]
    public string? Location { get; set; }

    [Column("officer_id")]
    [MaxLength(50)]
    public string? OfficerId { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "OPEN";

    [Column("attendance_count")]
    public int AttendanceCount { get; set; }

    [Column("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    public ICollection<GroupMeetingAttendance> Attendances { get; set; } = new List<GroupMeetingAttendance>();
}

[Table("group_meeting_attendance")]
public class GroupMeetingAttendance
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("group_meeting_id")]
    [MaxLength(50)]
    public string GroupMeetingId { get; set; } = string.Empty;

    [ForeignKey(nameof(GroupMeetingId))]
    public GroupMeeting? GroupMeeting { get; set; }

    [Column("group_member_id")]
    [MaxLength(50)]
    public string GroupMemberId { get; set; } = string.Empty;

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Column("attendance_status")]
    [MaxLength(20)]
    public string AttendanceStatus { get; set; } = "PRESENT";

    [Column("arrival_time")]
    public DateTime? ArrivalTime { get; set; }

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }
}

[Table("group_collection_batches")]
public class GroupCollectionBatch
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    [MaxLength(50)]
    public string GroupId { get; set; } = string.Empty;

    [Column("group_meeting_id")]
    [MaxLength(50)]
    public string? GroupMeetingId { get; set; }

    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = "BR001";

    [Column("officer_id")]
    [MaxLength(50)]
    public string? OfficerId { get; set; }

    [Column("collection_date")]
    public DateOnly CollectionDate { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "OPEN";

    [Column("total_collected_amount")]
    public decimal TotalCollectedAmount { get; set; }

    [Column("total_expected_amount")]
    public decimal TotalExpectedAmount { get; set; }

    [Column("variance_amount")]
    public decimal VarianceAmount { get; set; }

    [Column("channel")]
    [MaxLength(30)]
    public string Channel { get; set; } = "CASH";

    [Column("reference_no")]
    [MaxLength(100)]
    public string? ReferenceNo { get; set; }

    public ICollection<GroupCollectionBatchLine> Lines { get; set; } = new List<GroupCollectionBatchLine>();
}

[Table("group_collection_batch_lines")]
public class GroupCollectionBatchLine
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("batch_id")]
    [MaxLength(50)]
    public string BatchId { get; set; } = string.Empty;

    [ForeignKey(nameof(BatchId))]
    public GroupCollectionBatch? Batch { get; set; }

    [Column("loan_account_id")]
    [MaxLength(50)]
    public string LoanAccountId { get; set; } = string.Empty;

    [Column("group_member_id")]
    [MaxLength(50)]
    public string GroupMemberId { get; set; } = string.Empty;

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Column("expected_installment")]
    public decimal ExpectedInstallment { get; set; }

    [Column("amount_collected")]
    public decimal AmountCollected { get; set; }

    [Column("principal_component")]
    public decimal PrincipalComponent { get; set; }

    [Column("interest_component")]
    public decimal InterestComponent { get; set; }

    [Column("penalty_component")]
    public decimal PenaltyComponent { get; set; }

    [Column("savings_component")]
    public decimal SavingsComponent { get; set; }

    [Column("fee_component")]
    public decimal FeeComponent { get; set; }

    [Column("arrears_recovered")]
    public decimal ArrearsRecovered { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";
}

[Table("group_guarantee_links")]
public class GroupGuaranteeLink
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("group_loan_application_id")]
    [MaxLength(50)]
    public string GroupLoanApplicationId { get; set; } = string.Empty;

    [Column("group_member_id")]
    [MaxLength(50)]
    public string GroupMemberId { get; set; } = string.Empty;

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Column("guarantee_type")]
    [MaxLength(50)]
    public string GuaranteeType { get; set; } = "JOINT_LIABILITY";

    [Column("liability_percentage")]
    public decimal LiabilityPercentage { get; set; }

    [Column("guarantee_notes")]
    [MaxLength(500)]
    public string? GuaranteeNotes { get; set; }
}

[Table("group_loan_delinquency_snapshots")]
public class GroupLoanDelinquencySnapshot
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("loan_account_id")]
    [MaxLength(50)]
    public string LoanAccountId { get; set; } = string.Empty;

    [Column("group_id")]
    [MaxLength(50)]
    public string GroupId { get; set; } = string.Empty;

    [Column("group_member_id")]
    [MaxLength(50)]
    public string GroupMemberId { get; set; } = string.Empty;

    [Column("snapshot_date")]
    public DateOnly SnapshotDate { get; set; }

    [Column("days_past_due")]
    public int DaysPastDue { get; set; }

    [Column("installments_in_arrears")]
    public int InstallmentsInArrears { get; set; }

    [Column("outstanding_principal")]
    public decimal OutstandingPrincipal { get; set; }

    [Column("outstanding_interest")]
    public decimal OutstandingInterest { get; set; }

    [Column("outstanding_penalty")]
    public decimal OutstandingPenalty { get; set; }

    [Column("par_bucket")]
    [MaxLength(20)]
    public string ParBucket { get; set; } = "0";

    [Column("classification")]
    [MaxLength(30)]
    public string Classification { get; set; } = "CURRENT";

    [Column("is_npl")]
    public bool IsNpl { get; set; }
}
