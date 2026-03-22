namespace BankInsight.API.DTOs;

public class CreateProductRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public decimal? InterestRate { get; set; }
    public string? InterestMethod { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? MinTerm { get; set; }
    public int? MaxTerm { get; set; }
    public int? DefaultTerm { get; set; }
    public string? Status { get; set; }
    public string? LendingMethodology { get; set; }
    public bool IsGroupLoanEnabled { get; set; }
    public bool SupportsJointLiability { get; set; }
    public bool RequiresCenter { get; set; }
    public bool RequiresGroup { get; set; }
    public string? DefaultRepaymentFrequency { get; set; }
    public string[]? AllowedRepaymentFrequencies { get; set; }
    public bool SupportsWeeklyRepayment { get; set; }
    public int? MinimumGroupSize { get; set; }
    public int? MaximumGroupSize { get; set; }
    public bool RequiresCompulsorySavings { get; set; }
    public decimal? MinimumSavingsToLoanRatio { get; set; }
    public bool RequiresGroupApprovalMeeting { get; set; }
    public bool UsesMemberLevelUnderwriting { get; set; }
    public bool UsesGroupLevelApproval { get; set; }
    public string? LoanCyclePolicyType { get; set; }
    public int? MaxCycleNumber { get; set; }
    public string? GraduatedCycleLimitRulesJson { get; set; }
    public string? AttendanceRuleType { get; set; }
    public string? ArrearsEligibilityRuleType { get; set; }
    public string? GroupGuaranteePolicyType { get; set; }
    public string? MeetingCollectionMode { get; set; }
    public bool AllowBatchDisbursement { get; set; }
    public bool AllowMemberLevelDisbursementAdjustment { get; set; }
    public bool AllowTopUpWithinGroup { get; set; }
    public bool AllowRescheduleWithinGroup { get; set; }
    public string? GroupPenaltyPolicy { get; set; }
    public string? GroupDelinquencyPolicy { get; set; }
    public string? GroupOfficerAssignmentMode { get; set; }
    public ProductGroupRulesDto? GroupRules { get; set; }
    public ProductEligibilityRulesDto? EligibilityRules { get; set; }
}

public class UpdateProductRequest : CreateProductRequest
{
}
