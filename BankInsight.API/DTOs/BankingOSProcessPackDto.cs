using System.Collections.Generic;

namespace BankInsight.API.DTOs;

public class BankingOSProcessPackDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<string> LifecycleEnvelope { get; set; } = new();
    public List<BankingOSProcessDefinitionDto> Processes { get; set; } = new();
}

public class BankingOSProcessDefinitionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<BankingOSProcessStageDto> Stages { get; set; } = new();
}

public class BankingOSProcessStageDto
{
    public string StageCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string? FormCode { get; set; }
    public List<string> Actions { get; set; } = new();
    public BankingOSStageScreenSchemaDto? Screen { get; set; }
}

public class BankingOSSeedFormDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public object Layout { get; set; } = new();
    public List<BankingOSSeedFieldDto> Fields { get; set; } = new();
}

public class BankingOSSeedFieldDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public List<string>? Options { get; set; }
}

public class BankingOSThemePackDto
{
    public List<BankingOSSeedThemeDto> Themes { get; set; } = new();
}

public class BankingOSSeedThemeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> Tokens { get; set; } = new();
}

public class BankingOSThemeCatalogItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsSeeded { get; set; }
    public bool IsPublished { get; set; }
    public int TokenCount { get; set; }
}

public class BankingOSPublishBundleDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReleaseChannel { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public List<string> Processes { get; set; } = new();
    public List<string> Forms { get; set; } = new();
    public List<string> Themes { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public string LastAction { get; set; } = string.Empty;
    public string LastActionBy { get; set; } = string.Empty;
    public string LastActionAtUtc { get; set; } = string.Empty;
}

public class BankingOSProcessCatalogItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsSeeded { get; set; }
    public bool IsPublished { get; set; }
    public int StageCount { get; set; }
}

public class BankingOSFormCatalogItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsSeeded { get; set; }
    public bool IsPublished { get; set; }
    public int FieldCount { get; set; }
}

public class BankingOSSaveFormDraftRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public Dictionary<string, object?> Layout { get; set; } = new();
    public List<BankingOSSeedFieldDto> Fields { get; set; } = new();
}

public class BankingOSSaveProcessDraftRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public List<BankingOSProcessStageDto> Stages { get; set; } = new();
}

public class BankingOSSaveThemeDraftRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Tokens { get; set; } = new();
}

public class BankingOSBundleActionRequest
{
    public string Actor { get; set; } = "system";
    public string? Notes { get; set; }
}

public class BankingOSTaskContextDto
{
    public Guid TaskId { get; set; }
    public string TaskStatus { get; set; } = string.Empty;
    public string StepCode { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public BankingOSProcessStageDto? Stage { get; set; }
    public BankingOSSeedFormDto? Form { get; set; }
    public List<string> AllowedActions { get; set; } = new();
    public List<BankingOSTaskActionDescriptorDto> Actions { get; set; } = new();
    public bool RequiresClaim { get; set; }
    public List<string> RequiredFieldIds { get; set; } = new();
    public List<BankingOSFieldValidationRuleDto> ValidationRules { get; set; } = new();
    public BankingOSTaskScreenSchemaDto? Screen { get; set; }
    public BankingOSProductLaunchOptionDto? SelectedProduct { get; set; }
    public string CompletionOutcome { get; set; } = string.Empty;
    public bool RejectionAllowed { get; set; }
}

public class BankingOSTaskActionRequest
{
    public string? Remarks { get; set; }
    public string? PayloadJson { get; set; }
}

public class BankingOSTaskActionDescriptorDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public bool RequiresRemarks { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsEnabled { get; set; }
    public string? DisabledReason { get; set; }
}

public class BankingOSStageScreenSchemaDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BannerTone { get; set; } = string.Empty;
    public string BannerMessage { get; set; } = string.Empty;
    public List<BankingOSStageScreenSectionDto> Sections { get; set; } = new();
}

public class BankingOSStageScreenSectionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public List<string> FieldIds { get; set; } = new();
}

public class BankingOSFieldValidationRuleDto
{
    public string FieldId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BankingOSTaskScreenSchemaDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BannerTone { get; set; } = string.Empty;
    public string BannerMessage { get; set; } = string.Empty;
    public List<BankingOSTaskScreenSectionDto> Sections { get; set; } = new();
}

public class BankingOSTaskScreenSectionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public List<string> FieldIds { get; set; } = new();
}

public class BankingOSProductConfigurationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal? InterestRate { get; set; }
    public string? InterestMethod { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? MinTerm { get; set; }
    public int? MaxTerm { get; set; }
    public int? DefaultTerm { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string LendingMethodology { get; set; } = "INDIVIDUAL";
    public bool IsGroupLoanEnabled { get; set; }
    public bool SupportsJointLiability { get; set; }
    public bool RequiresCenter { get; set; }
    public bool RequiresGroup { get; set; }
    public string DefaultRepaymentFrequency { get; set; } = "Monthly";
    public List<string> AllowedRepaymentFrequencies { get; set; } = new();
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
    public BankingOSProductGroupRulesDto? GroupRules { get; set; }
    public BankingOSProductEligibilityRulesDto? EligibilityRules { get; set; }
}

public class BankingOSProductGroupRulesDto
{
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

public class BankingOSProductEligibilityRulesDto
{
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

public class BankingOSProductLaunchOptionDto
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal? InterestRate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? DefaultTerm { get; set; }
    public int? MinTerm { get; set; }
    public int? MaxTerm { get; set; }
    public string DefaultRepaymentFrequency { get; set; } = "Monthly";
    public List<string> AllowedRepaymentFrequencies { get; set; } = new();
    public List<string> EligibilityHints { get; set; } = new();
}

public class BankingOSLaunchContextDto
{
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public BankingOSSeedFormDto? PrimaryForm { get; set; }
    public List<BankingOSProductLaunchOptionDto> ProductOptions { get; set; } = new();
    public List<string> ValidationHints { get; set; } = new();
}

public class BankingOSLaunchProcessRequest
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? PayloadJson { get; set; }
}
