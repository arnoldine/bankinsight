using System.Text.Json;

namespace CoreBanker.Services
{
    public class ExtensibilityService : ApiClientBase
    {
        public ExtensibilityService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<List<BankingOSProcessCatalogItemDto>> GetProcessCatalogAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<BankingOSProcessCatalogItemDto>>("/api/bankingos/process-catalog", cancellationToken);
            return result ?? new List<BankingOSProcessCatalogItemDto>();
        }

        public async Task<List<BankingOSFormCatalogItemDto>> GetFormCatalogAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<BankingOSFormCatalogItemDto>>("/api/bankingos/form-catalog", cancellationToken);
            return result ?? new List<BankingOSFormCatalogItemDto>();
        }

        public async Task<List<BankingOSThemeCatalogItemDto>> GetThemeCatalogAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<BankingOSThemeCatalogItemDto>>("/api/bankingos/theme-catalog", cancellationToken);
            return result ?? new List<BankingOSThemeCatalogItemDto>();
        }

        public async Task<List<BankingOSPublishBundleDto>> GetPublishBundlesAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<BankingOSPublishBundleDto>>("/api/bankingos/publish-bundles", cancellationToken);
            return result ?? new List<BankingOSPublishBundleDto>();
        }

        public async Task<List<BankingOSProductConfigurationDto>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<BankingOSProductConfigurationDto>>("/api/bankingos/products", cancellationToken);
            return result ?? new List<BankingOSProductConfigurationDto>();
        }

        public Task<BankingOSProductConfigurationDto?> CreateProductAsync(BankingOSProductConfigurationDto request, CancellationToken cancellationToken = default)
            => PostAsync<BankingOSProductConfigurationDto, BankingOSProductConfigurationDto>("/api/bankingos/products", request, cancellationToken);

        public Task<BankingOSProductConfigurationDto?> UpdateProductAsync(string id, BankingOSProductConfigurationDto request, CancellationToken cancellationToken = default)
            => PutAsync<BankingOSProductConfigurationDto, BankingOSProductConfigurationDto>($"/api/bankingos/products/{Uri.EscapeDataString(id)}", request, cancellationToken);

        public async Task<BankingOSSeedFormDto?> GetSeedFormAsync(string code, CancellationToken cancellationToken = default)
        {
            return await GetAsync<BankingOSSeedFormDto>($"/api/bankingos/forms/{Uri.EscapeDataString(code)}", cancellationToken);
        }

        public async Task<List<ProcessDefinitionDto>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<ProcessDefinitionDto>>("/api/WorkflowDefinition", cancellationToken);
            return result ?? new List<ProcessDefinitionDto>();
        }

        public async Task<List<WorkflowTaskDto>> GetMyTasksAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<WorkflowTaskDto>>("/api/WorkflowRuntime/tasks/my", cancellationToken);
            return result ?? new List<WorkflowTaskDto>();
        }

        public async Task<List<WorkflowTaskDto>> GetClaimableTasksAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<WorkflowTaskDto>>("/api/WorkflowRuntime/tasks/claimable", cancellationToken);
            return result ?? new List<WorkflowTaskDto>();
        }

        public async Task ClaimTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            await PostAsync<object, object>($"/api/WorkflowRuntime/tasks/{taskId}/claim", new { }, cancellationToken);
        }

        public async Task<BankingOSTaskContextDto?> GetTaskContextAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<BankingOSTaskContextDto>($"/api/bankingos/tasks/{taskId}/context", cancellationToken);
        }

        public async Task CompleteTaskAsync(Guid taskId, BankingOSTaskActionRequest request, CancellationToken cancellationToken = default)
        {
            await PostAsync<BankingOSTaskActionRequest, object>($"/api/bankingos/tasks/{taskId}/complete", request, cancellationToken);
        }

        public async Task RejectTaskAsync(Guid taskId, BankingOSTaskActionRequest request, CancellationToken cancellationToken = default)
        {
            await PostAsync<BankingOSTaskActionRequest, object>($"/api/bankingos/tasks/{taskId}/reject", request, cancellationToken);
        }

        public async Task<BankingOSPublishBundleDto?> SubmitBundleAsync(string code, BankingOSBundleActionRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<BankingOSBundleActionRequest, BankingOSPublishBundleDto>($"/api/bankingos/publish-bundles/{Uri.EscapeDataString(code)}/submit", request, cancellationToken);
        }

        public async Task<BankingOSPublishBundleDto?> ApproveBundleAsync(string code, BankingOSBundleActionRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<BankingOSBundleActionRequest, BankingOSPublishBundleDto>($"/api/bankingos/publish-bundles/{Uri.EscapeDataString(code)}/approve", request, cancellationToken);
        }

        public async Task<BankingOSPublishBundleDto?> RejectBundleAsync(string code, BankingOSBundleActionRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<BankingOSBundleActionRequest, BankingOSPublishBundleDto>($"/api/bankingos/publish-bundles/{Uri.EscapeDataString(code)}/reject", request, cancellationToken);
        }

        public async Task<BankingOSPublishBundleDto?> PromoteBundleAsync(string code, BankingOSBundleActionRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<BankingOSBundleActionRequest, BankingOSPublishBundleDto>($"/api/bankingos/publish-bundles/{Uri.EscapeDataString(code)}/promote", request, cancellationToken);
        }

        public async Task<ProductGroupRulesDto?> GetProductGroupRulesAsync(string productId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<ProductGroupRulesDto>($"/api/group-lending/product-designer/loan-products/{Uri.EscapeDataString(productId)}/group-rules", cancellationToken);
        }

        public async Task<ProductGroupRulesDto?> SaveProductGroupRulesAsync(string productId, ProductGroupRulesDto request, CancellationToken cancellationToken = default)
        {
            return await PutAsync<ProductGroupRulesDto, ProductGroupRulesDto>($"/api/group-lending/product-designer/loan-products/{Uri.EscapeDataString(productId)}/group-rules", request, cancellationToken);
        }

        public async Task<ProductEligibilityRulesDto?> GetProductEligibilityRulesAsync(string productId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<ProductEligibilityRulesDto>($"/api/group-lending/product-designer/loan-products/{Uri.EscapeDataString(productId)}/eligibility-rules", cancellationToken);
        }

        public async Task<ProductEligibilityRulesDto?> SaveProductEligibilityRulesAsync(string productId, ProductEligibilityRulesDto request, CancellationToken cancellationToken = default)
        {
            return await PutAsync<ProductEligibilityRulesDto, ProductEligibilityRulesDto>($"/api/group-lending/product-designer/loan-products/{Uri.EscapeDataString(productId)}/eligibility-rules", request, cancellationToken);
        }
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
        public string Status { get; set; } = "DRAFT";
        public string? LendingMethodology { get; set; }
        public bool IsGroupLoanEnabled { get; set; }
        public bool SupportsJointLiability { get; set; }
        public bool RequiresCenter { get; set; }
        public bool RequiresGroup { get; set; }
        public string DefaultRepaymentFrequency { get; set; } = "Monthly";
        public string[] AllowedRepaymentFrequencies { get; set; } = [];
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

    public class BankingOSSeedFormDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Status { get; set; } = string.Empty;
        public JsonElement Layout { get; set; }
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

    public class ProcessDefinitionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string TriggerType { get; set; } = string.Empty;
        public string? TriggerEventType { get; set; }
        public bool IsSystemProcess { get; set; }
        public bool IsActive { get; set; }
    }

    public class WorkflowTaskDto
    {
        public Guid Id { get; set; }
        public Guid ProcessInstanceId { get; set; }
        public Guid ProcessStepDefinitionId { get; set; }
        public string StepCode { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedToUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ClaimedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
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
        public BankingOSSeedFormDto? Form { get; set; }
        public List<string> AllowedActions { get; set; } = new();
        public bool RequiresClaim { get; set; }
        public List<string> RequiredFieldIds { get; set; } = new();
        public List<BankingOSFieldValidationRuleDto> ValidationRules { get; set; } = new();
        public BankingOSTaskScreenSchemaDto? Screen { get; set; }
        public string CompletionOutcome { get; set; } = string.Empty;
        public bool RejectionAllowed { get; set; }
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

    public class BankingOSTaskActionRequest
    {
        public string? Remarks { get; set; }
        public string? PayloadJson { get; set; }
    }

    public class BankingOSBundleActionRequest
    {
        public string Actor { get; set; } = "system";
        public string? Notes { get; set; }
    }

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
}
