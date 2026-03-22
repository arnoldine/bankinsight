using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class ProductGroupRulesDto
{
    public string ProductId { get; set; } = string.Empty;
    public int MinMembersRequired { get; set; }
    public int MaxMembersAllowed { get; set; }
    public int? MinWeeks { get; set; }
    public int? MaxWeeks { get; set; }
    public bool RequiresCompulsorySavings { get; set; }
    public decimal? MinSavingsToLoanRatio { get; set; }
    public bool RequiresGroupApprovalMeeting { get; set; }
    public bool RequiresJointLiability { get; set; }
    public bool AllowTopUp { get; set; }
    public bool AllowReschedule { get; set; }
    public int? MaxCycleNumber { get; set; }
    public string? CycleIncrementRulesJson { get; set; }
    public string DefaultRepaymentFrequency { get; set; } = "Weekly";
    public string DefaultInterestMethod { get; set; } = "Flat";
    public string? PenaltyPolicyJson { get; set; }
    public string? AttendanceRuleJson { get; set; }
    public string? EligibilityRuleJson { get; set; }
    public string? MeetingCollectionRuleJson { get; set; }
    public string? AllocationOrderJson { get; set; }
    public string? AccountingProfileJson { get; set; }
    public string? DisclosureTemplate { get; set; }
}

public class ProductEligibilityRulesDto
{
    public string ProductId { get; set; } = string.Empty;
    public bool RequiresKycComplete { get; set; } = true;
    public bool BlockOnSevereArrears { get; set; } = true;
    public decimal? MaxAllowedExposure { get; set; }
    public int? MinMembershipDays { get; set; }
    public decimal? MinAttendanceRate { get; set; }
    public bool RequireCreditBureauCheck { get; set; }
    public string? CreditBureauProvider { get; set; }
    public int? MinimumCreditScore { get; set; }
    public string? RuleJson { get; set; }
}

public class GroupMemberSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string MemberRole { get; set; } = "MEMBER";
    public string Status { get; set; } = "ACTIVE";
    public string KycStatus { get; set; } = "PENDING";
    public bool IsEligibleForLoan { get; set; }
    public int CurrentLoanCycle { get; set; }
    public decimal CurrentExposure { get; set; }
    public bool ArrearsFlag { get; set; }
}

public class LendingGroupDto
{
    public string Id { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string? CenterId { get; set; }
    public string? GroupCode { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? MeetingDayOfWeek { get; set; }
    public string MeetingFrequency { get; set; } = "Weekly";
    public string? MeetingLocation { get; set; }
    public string? AssignedOfficerId { get; set; }
    public string? ChairpersonCustomerId { get; set; }
    public string? SecretaryCustomerId { get; set; }
    public string? TreasurerCustomerId { get; set; }
    public DateOnly? FormationDate { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public bool IsJointLiabilityEnabled { get; set; }
    public int? MaxMembers { get; set; }
    public string? Notes { get; set; }
    public List<GroupMemberSummaryDto> Members { get; set; } = new();
}

public class CreateLendingGroupRequest
{
    [Required]
    [StringLength(150)]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(50)]
    public string BranchId { get; set; } = "BR001";

    [StringLength(50)]
    public string? CenterId { get; set; }

    [StringLength(50)]
    public string? GroupCode { get; set; }

    [StringLength(20)]
    public string? MeetingDayOfWeek { get; set; }

    [StringLength(20)]
    public string MeetingFrequency { get; set; } = "Weekly";

    [StringLength(200)]
    public string? MeetingLocation { get; set; }

    [StringLength(50)]
    public string? AssignedOfficerId { get; set; }

    [StringLength(50)]
    public string? ChairpersonCustomerId { get; set; }

    [StringLength(50)]
    public string? SecretaryCustomerId { get; set; }

    [StringLength(50)]
    public string? TreasurerCustomerId { get; set; }

    public DateOnly? FormationDate { get; set; }
    public bool IsJointLiabilityEnabled { get; set; }
    public int? MaxMembers { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLendingGroupRequest : CreateLendingGroupRequest
{
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "ACTIVE";
}

public class AddLendingGroupMemberRequest
{
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(30)]
    public string MemberRole { get; set; } = "MEMBER";

    public bool IsFoundingMember { get; set; }
    public decimal ShareContribution { get; set; }
    public bool GuarantorIndicator { get; set; }
    public string? SocialCollateralNotes { get; set; }
}

public class CreateLendingCenterRequest
{
    [Required]
    [StringLength(50)]
    public string BranchId { get; set; } = "BR001";

    [Required]
    [StringLength(50)]
    public string CenterCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string CenterName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? MeetingDayOfWeek { get; set; }

    [StringLength(200)]
    public string? MeetingLocation { get; set; }

    [StringLength(50)]
    public string? AssignedOfficerId { get; set; }
}

public class LendingCenterDto : CreateLendingCenterRequest
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
}

public class GroupLoanApplicationMemberRequest
{
    [Required]
    [StringLength(50)]
    public string GroupMemberId { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal RequestedAmount { get; set; }

    [Range(1, 260)]
    public int TenureWeeks { get; set; }

    [Range(0, 100)]
    public decimal InterestRate { get; set; }

    [Required]
    public string InterestMethod { get; set; } = "Flat";

    [Required]
    public string RepaymentFrequency { get; set; } = "Weekly";

    public string? LoanPurpose { get; set; }
    public decimal SavingsBalanceAtApplication { get; set; }
    public string? GuarantorNotes { get; set; }
}

public class CreateGroupLoanApplicationRequest
{
    [Required]
    [StringLength(50)]
    public string GroupId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ProductId { get; set; } = string.Empty;

    [StringLength(50)]
    public string BranchId { get; set; } = "BR001";

    [StringLength(50)]
    public string? OfficerId { get; set; }

    public int LoanCycleNo { get; set; }
    public string? MeetingReference { get; set; }
    public string? GroupResolutionReference { get; set; }
    public string? Notes { get; set; }
    public List<GroupLoanApplicationMemberRequest> Members { get; set; } = new();
}

public class ReviewGroupLoanApplicationRequest
{
    public string DecisionNotes { get; set; } = string.Empty;
    public Dictionary<string, decimal>? ApprovedAmounts { get; set; }
}

public class ApproveGroupLoanApplicationRequest
{
    public string DecisionNotes { get; set; } = string.Empty;
}

public class RejectGroupLoanApplicationRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class DisburseGroupLoanApplicationRequest
{
    public DateOnly? DisbursementDate { get; set; }
    public string? ClientReference { get; set; }
}

public class GroupLoanApplicationMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupMemberId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal DisbursedAmount { get; set; }
    public int TenureWeeks { get; set; }
    public decimal InterestRate { get; set; }
    public string InterestMethod { get; set; } = "Flat";
    public string RepaymentFrequency { get; set; } = "Weekly";
    public string? LoanPurpose { get; set; }
    public string EligibilityStatus { get; set; } = "PENDING";
    public decimal ExistingExposureAmount { get; set; }
    public decimal SavingsBalanceAtApplication { get; set; }
    public string? ScoreResult { get; set; }
    public string Status { get; set; } = "DRAFT";
}

public class GroupLoanApplicationDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int LoanCycleNo { get; set; }
    public DateTime ApplicationDate { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string? OfficerId { get; set; }
    public string Status { get; set; } = "DRAFT";
    public decimal TotalApprovedAmount { get; set; }
    public decimal TotalRequestedAmount { get; set; }
    public decimal TotalDisbursedAmount { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateTime? DisbursementDate { get; set; }
    public string? MeetingReference { get; set; }
    public string? GroupResolutionReference { get; set; }
    public string? Notes { get; set; }
    public List<GroupLoanApplicationMemberDto> Members { get; set; } = new();
}

public class CreateGroupMeetingRequest
{
    [Required]
    [StringLength(50)]
    public string GroupId { get; set; } = string.Empty;

    [StringLength(50)]
    public string? CenterId { get; set; }

    public DateOnly MeetingDate { get; set; }
    public string MeetingType { get; set; } = "REGULAR";
    public string? Location { get; set; }
    public string? OfficerId { get; set; }
    public string? Notes { get; set; }
}

public class GroupMeetingAttendanceRequest
{
    public List<GroupMeetingAttendanceLineRequest> Attendances { get; set; } = new();
}

public class GroupMeetingAttendanceLineRequest
{
    [Required]
    public string GroupMemberId { get; set; } = string.Empty;
    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public string AttendanceStatus { get; set; } = "PRESENT";
    public DateTime? ArrivalTime { get; set; }
    public string? Notes { get; set; }
}

public class GroupMeetingDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? CenterId { get; set; }
    public DateOnly MeetingDate { get; set; }
    public string MeetingType { get; set; } = "REGULAR";
    public string? Location { get; set; }
    public string? OfficerId { get; set; }
    public string Status { get; set; } = "OPEN";
    public int AttendanceCount { get; set; }
    public string? Notes { get; set; }
    public List<GroupMeetingAttendanceLineRequest> Attendances { get; set; } = new();
}

public class CreateGroupCollectionBatchRequest
{
    [Required]
    public string GroupId { get; set; } = string.Empty;
    public string? GroupMeetingId { get; set; }
    public string BranchId { get; set; } = "BR001";
    public string? OfficerId { get; set; }
    public DateOnly CollectionDate { get; set; }
    public string Channel { get; set; } = "CASH";
    public string? ReferenceNo { get; set; }
    public List<GroupCollectionBatchLineRequest> Lines { get; set; } = new();
}

public class GroupCollectionBatchLineRequest
{
    [Required]
    public string LoanAccountId { get; set; } = string.Empty;
    [Required]
    public string GroupMemberId { get; set; } = string.Empty;
    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public decimal ExpectedInstallment { get; set; }
    public decimal AmountCollected { get; set; }
    public decimal SavingsComponent { get; set; }
}

public class GroupCollectionBatchDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string? GroupMeetingId { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public string? OfficerId { get; set; }
    public DateOnly CollectionDate { get; set; }
    public string Status { get; set; } = "OPEN";
    public decimal TotalCollectedAmount { get; set; }
    public decimal TotalExpectedAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public string Channel { get; set; } = "CASH";
    public string? ReferenceNo { get; set; }
    public List<GroupCollectionBatchLineDto> Lines { get; set; } = new();
}

public class GroupCollectionBatchLineDto
{
    public string Id { get; set; } = string.Empty;
    public string LoanAccountId { get; set; } = string.Empty;
    public string GroupMemberId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal ExpectedInstallment { get; set; }
    public decimal AmountCollected { get; set; }
    public decimal PrincipalComponent { get; set; }
    public decimal InterestComponent { get; set; }
    public decimal PenaltyComponent { get; set; }
    public decimal SavingsComponent { get; set; }
    public decimal FeeComponent { get; set; }
    public decimal ArrearsRecovered { get; set; }
    public string Status { get; set; } = "PENDING";
}

public class GroupRepaymentRequest
{
    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }
    [Required]
    public string AccountId { get; set; } = string.Empty;
    public string? ClientReference { get; set; }
}

public class GroupParReportItemDto
{
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public decimal OutstandingPrincipal { get; set; }
    public int DaysPastDue { get; set; }
    public string ParBucket { get; set; } = "0";
}

public class GroupPortfolioSummaryDto
{
    public int ActiveGroups { get; set; }
    public int ActiveMembers { get; set; }
    public decimal TotalPortfolio { get; set; }
    public decimal Par30 { get; set; }
    public decimal WeeklyDueThisWeek { get; set; }
    public decimal CollectionsThisWeek { get; set; }
}
