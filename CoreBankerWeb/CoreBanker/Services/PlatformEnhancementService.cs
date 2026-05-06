using CoreBanker.State;

namespace CoreBanker.Services;

public class PlatformEnhancementService : ApiClientBase
{
    public PlatformEnhancementService(HttpClient httpClient, AppState appState)
        : base(httpClient, appState)
    {
    }

    public Task<OperationsControlCenterVm?> GetOperationsControlSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync<OperationsControlCenterVm>("api/operations-control/summary", cancellationToken);

    public Task<Customer360Vm?> GetCustomer360Async(string customerId, CancellationToken cancellationToken = default)
        => GetAsync<Customer360Vm>($"api/customer360/{Uri.EscapeDataString(customerId)}", cancellationToken);

    public Task<List<ProductListItemVm>?> GetProductsAsync(CancellationToken cancellationToken = default)
        => GetAsync<List<ProductListItemVm>>("api/products", cancellationToken);

    public Task<List<CollectionCaseVm>?> GetCollectionCasesAsync(CancellationToken cancellationToken = default)
        => GetAsync<List<CollectionCaseVm>>("api/collections/cases", cancellationToken);

    public Task<CollectionCaseVm?> UpdateCollectionCaseAsync(string caseId, UpdateCollectionCaseRequestVm request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateCollectionCaseRequestVm, CollectionCaseVm>($"api/collections/cases/{Uri.EscapeDataString(caseId)}", request, cancellationToken);

    public Task<CollectionCaseVm?> ExecuteCollectionActionAsync(string caseId, ExecuteCollectionActionRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<ExecuteCollectionActionRequestVm, CollectionCaseVm>($"api/collections/cases/{Uri.EscapeDataString(caseId)}/actions", request, cancellationToken);

    public Task<ProductSimulationResultVm?> SimulateProductAsync(string productId, ProductSimulationRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<ProductSimulationRequestVm, ProductSimulationResultVm>($"api/products/{Uri.EscapeDataString(productId)}/simulate", request, cancellationToken);

    public Task<object?> UpdateProductLifecycleAsync(string productId, ProductLifecycleUpdateRequestVm request, CancellationToken cancellationToken = default)
        => PutAsync<ProductLifecycleUpdateRequestVm, object>($"api/products/{Uri.EscapeDataString(productId)}/lifecycle", request, cancellationToken);

    public Task<ReconciliationHubSummaryVm?> GetReconciliationSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync<ReconciliationHubSummaryVm>("api/reconciliation-hub/summary", cancellationToken);

    public Task<ReconciliationExceptionVm?> UpdateReconciliationExceptionAsync(string id, UpdateReconciliationExceptionRequestVm request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateReconciliationExceptionRequestVm, ReconciliationExceptionVm>($"api/reconciliation-hub/exceptions/{Uri.EscapeDataString(id)}", request, cancellationToken);

    public Task<ReconciliationExceptionVm?> RetryReconciliationExceptionAsync(string id, RetryReconciliationExceptionRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<RetryReconciliationExceptionRequestVm, ReconciliationExceptionVm>($"api/reconciliation-hub/exceptions/{Uri.EscapeDataString(id)}/retry", request, cancellationToken);

    public Task<SettlementInstructionVm?> CreateSettlementInstructionAsync(CreateSettlementInstructionRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<CreateSettlementInstructionRequestVm, SettlementInstructionVm>("api/reconciliation-hub/settlement-instructions", request, cancellationToken);

    public Task<CollateralManagementSummaryVm?> GetCollateralSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync<CollateralManagementSummaryVm>("api/collateral-management/summary", cancellationToken);

    public Task<CollateralRecordVm?> UpdateCollateralAsync(string id, UpdateCollateralRecordRequestVm request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateCollateralRecordRequestVm, CollateralRecordVm>($"api/collateral-management/collateral/{Uri.EscapeDataString(id)}", request, cancellationToken);

    public Task<CovenantRecordVm?> UpdateCovenantAsync(string id, UpdateCovenantRecordRequestVm request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateCovenantRecordRequestVm, CovenantRecordVm>($"api/collateral-management/covenants/{Uri.EscapeDataString(id)}", request, cancellationToken);

    public Task<DeveloperPortalSummaryVm?> GetDeveloperPortalSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync<DeveloperPortalSummaryVm>("api/developer-portal/summary", cancellationToken);

    public Task<PartnerApplicationVm?> CreatePartnerApplicationAsync(CreatePartnerApplicationRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<CreatePartnerApplicationRequestVm, PartnerApplicationVm>("api/developer-portal/partner-applications", request, cancellationToken);

    public Task<PartnerApplicationVm?> RotatePartnerSandboxKeyAsync(string id, CancellationToken cancellationToken = default)
        => PostAsync<object, PartnerApplicationVm>($"api/developer-portal/partner-applications/{Uri.EscapeDataString(id)}/rotate-sandbox-key", new { }, cancellationToken);

    public Task<PartnerApplicationVm?> PromotePartnerApplicationAsync(string id, PromotePartnerApplicationRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<PromotePartnerApplicationRequestVm, PartnerApplicationVm>($"api/developer-portal/partner-applications/{Uri.EscapeDataString(id)}/promote", request, cancellationToken);

    public Task<WebhookSubscriptionVm?> CreateWebhookSubscriptionAsync(CreateWebhookSubscriptionRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<CreateWebhookSubscriptionRequestVm, WebhookSubscriptionVm>("api/developer-portal/webhook-subscriptions", request, cancellationToken);

    public Task<WebhookDeliveryLogVm?> ReplayWebhookAsync(ReplayWebhookRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<ReplayWebhookRequestVm, WebhookDeliveryLogVm>("api/developer-portal/webhook-subscriptions/replay", request, cancellationToken);

    public Task<RelationshipBankingSummaryVm?> GetRelationshipBankingSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync<RelationshipBankingSummaryVm>("api/supervisory/relationship-banking", cancellationToken);

    public Task<RelationshipPortfolioDetailVm?> GetRelationshipPortfolioDetailAsync(string customerId, CancellationToken cancellationToken = default)
        => GetAsync<RelationshipPortfolioDetailVm>($"api/supervisory/relationship-banking/{Uri.EscapeDataString(customerId)}", cancellationToken);

    public Task<List<AssignableStaffItemVm>?> GetAssignableStaffAsync(CancellationToken cancellationToken = default)
        => GetAsync<List<AssignableStaffItemVm>>("api/supervisory/relationship-banking/staff-directory", cancellationToken);

    public Task<RelationshipCustomerItemVm?> AssignRelationshipOwnerAsync(AssignRelationshipOwnerRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<AssignRelationshipOwnerRequestVm, RelationshipCustomerItemVm>("api/supervisory/relationship-banking/assign-owner", request, cancellationToken);

    public Task<DigitalChannelOperationsSummaryVm?> GetDigitalChannelOperationsSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync<DigitalChannelOperationsSummaryVm>("api/supervisory/digital-channel-operations", cancellationToken);

    public Task<RegulatoryIntelligenceSummaryVm?> GetRegulatoryIntelligenceSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync<RegulatoryIntelligenceSummaryVm>("api/supervisory/regulatory-intelligence", cancellationToken);

    public Task<RegulatoryVarianceItemVm?> ResolveRegulatoryVarianceAsync(ResolveRegulatoryVarianceRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<ResolveRegulatoryVarianceRequestVm, RegulatoryVarianceItemVm>("api/supervisory/regulatory-intelligence/variances/resolve", request, cancellationToken);

    public Task<RegulatoryVarianceItemVm?> ReopenRegulatoryVarianceAsync(ResolveRegulatoryVarianceRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<ResolveRegulatoryVarianceRequestVm, RegulatoryVarianceItemVm>("api/supervisory/regulatory-intelligence/variances/reopen", request, cancellationToken);

    public Task<RegulatoryVarianceItemVm?> AssignRegulatoryVarianceAsync(AssignRegulatoryVarianceRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<AssignRegulatoryVarianceRequestVm, RegulatoryVarianceItemVm>("api/supervisory/regulatory-intelligence/variances/assign", request, cancellationToken);
}

public sealed class OperationsControlCenterVm
{
    public string BusinessDate { get; set; } = string.Empty;
    public string PlatformStatus { get; set; } = string.Empty;
    public List<OperationsMetricVm> Metrics { get; set; } = new();
    public List<OperationsWorkItemVm> WorkItems { get; set; } = new();
}

public sealed class OperationsMetricVm
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
}

public sealed class OperationsWorkItemVm
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? RouteHint { get; set; }
    public decimal? Amount { get; set; }
    public int? Count { get; set; }
    public string? Reference { get; set; }
}

public sealed class Customer360Vm
{
    public CustomerProfileVm Profile { get; set; } = new();
    public CustomerFinancialSummaryVm FinancialSummary { get; set; } = new();
    public List<CustomerAccount360Vm> Accounts { get; set; } = new();
    public List<CustomerLoan360Vm> Loans { get; set; } = new();
    public List<CustomerInvestment360Vm> Investments { get; set; } = new();
    public List<CustomerTransaction360Vm> RecentTransactions { get; set; } = new();
    public List<CustomerEngagement360Vm> EngagementTimeline { get; set; } = new();
}

public sealed class CustomerProfileVm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GhanaCard { get; set; }
    public string? KycLevel { get; set; }
    public string? RiskRating { get; set; }
    public CustomerKycReadinessVm KycReadiness { get; set; } = new();
}

public sealed class CustomerKycReadinessVm
{
    public bool IsReadyForAccountOpening { get; set; }
    public bool IsReadyForLoanOrigination { get; set; }
    public List<string> MissingRequirements { get; set; } = new();
}

public sealed class CustomerFinancialSummaryVm
{
    public int ActiveAccountCount { get; set; }
    public int ActiveLoanCount { get; set; }
    public int ActiveInvestmentCount { get; set; }
    public decimal TotalDeposits90Days { get; set; }
    public decimal TotalWithdrawals90Days { get; set; }
    public decimal TotalBalances { get; set; }
    public decimal TotalOutstandingLoans { get; set; }
    public decimal TotalInvestmentBook { get; set; }
    public string PrimaryCurrency { get; set; } = "GHS";
    public decimal EstimatedAnnualRevenue { get; set; }
    public string RelationshipOwner { get; set; } = "Unassigned";
}

public sealed class CustomerAccount360Vm
{
    public string Id { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Balance { get; set; }
    public DateTime OpenDate { get; set; }
}

public sealed class CustomerLoan360Vm
{
    public string Id { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal OutstandingBalance { get; set; }
    public string ParBucket { get; set; } = "0";
    public string? RepaymentFrequency { get; set; }
}

public sealed class CustomerInvestment360Vm
{
    public string Id { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public DateTime? MaturityDate { get; set; }
}

public sealed class CustomerTransaction360Vm
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

public sealed class CustomerEngagement360Vm
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime At { get; set; }
}

public sealed class CollectionCaseVm
{
    public string Id { get; set; } = string.Empty;
    public string LoanId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string RecoveryStage { get; set; } = string.Empty;
    public int DelinquencyDays { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal AmountInArrears { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? NextActionDate { get; set; }
    public DateTime? PromiseToPayDate { get; set; }
    public decimal? PromiseToPayAmount { get; set; }
    public DateTime? LastContactAt { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public DateTime? NextEscalationDate { get; set; }
    public string? Notes { get; set; }
    public string? RecoveryStrategy { get; set; }
    public string? LegalStatus { get; set; }
    public decimal? SettlementAmount { get; set; }
    public DateTime? SettlementExpiryDate { get; set; }
    public string? AssignedAgency { get; set; }
    public string? RepossessionStatus { get; set; }
    public string? ApprovalStatus { get; set; }
    public decimal? WriteOffRecommendedAmount { get; set; }
    public string? WriteOffReason { get; set; }
    public List<CollectionCaseEventVm> Events { get; set; } = new();
}

public sealed class CollectionCaseEventVm
{
    public string EventType { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ProductListItemVm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal? InterestRate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? DefaultTerm { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string LifecycleStatus { get; set; } = "DRAFT";
    public int VersionNumber { get; set; } = 1;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? RetiredAt { get; set; }
}

public sealed class UpdateCollectionCaseRequestVm
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? RecoveryStage { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? NextActionDate { get; set; }
    public DateTime? PromiseToPayDate { get; set; }
    public decimal? PromiseToPayAmount { get; set; }
    public decimal? SettlementAmount { get; set; }
    public DateTime? SettlementExpiryDate { get; set; }
    public string? Notes { get; set; }
    public string? RecoveryStrategy { get; set; }
    public string? LegalStatus { get; set; }
    public string? AssignedAgency { get; set; }
    public string? RepossessionStatus { get; set; }
    public string? ApprovalStatus { get; set; }
    public decimal? WriteOffRecommendedAmount { get; set; }
    public string? WriteOffReason { get; set; }
    public string EventType { get; set; } = "NOTE";
    public string Detail { get; set; } = string.Empty;
}

public sealed class ExecuteCollectionActionRequestVm
{
    public string ActionType { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime? PromiseToPayDate { get; set; }
    public decimal? PromiseToPayAmount { get; set; }
    public decimal? SettlementAmount { get; set; }
    public DateTime? SettlementExpiryDate { get; set; }
    public DateTime? NextActionDate { get; set; }
    public string? AssignedAgency { get; set; }
    public string? WriteOffReason { get; set; }
}

public sealed class ProductLifecycleUpdateRequestVm
{
    public string LifecycleStatus { get; set; } = "DRAFT";
    public DateTime? EffectiveFrom { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductSimulationRequestVm
{
    public decimal Amount { get; set; }
    public int? TermMonths { get; set; }
    public decimal? AnnualRateOverride { get; set; }
}

public sealed class ProductSimulationResultVm
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }
    public decimal AnnualRate { get; set; }
    public decimal ProjectedInterest { get; set; }
    public decimal ProjectedMaturityValue { get; set; }
    public decimal? ProjectedInstallment { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public sealed class ReconciliationHubSummaryVm
{
    public List<ReconciliationMetricVm> Metrics { get; set; } = new();
    public List<ReconciliationExceptionVm> Exceptions { get; set; } = new();
    public List<SettlementInstructionVm> SettlementInstructions { get; set; } = new();
}

public sealed class ReconciliationMetricVm
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

public sealed class ReconciliationExceptionVm
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public string? OwnerUserId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? WorkflowStage { get; set; }
    public string? ResolutionCode { get; set; }
}

public sealed class UpdateReconciliationExceptionRequestVm
{
    public string? Status { get; set; }
    public string? OwnerUserId { get; set; }
    public string? Detail { get; set; }
    public string? WorkflowStage { get; set; }
    public string? ResolutionCode { get; set; }
}

public sealed class RetryReconciliationExceptionRequestVm
{
    public string? Detail { get; set; }
}

public sealed class SettlementInstructionVm
{
    public string Id { get; set; } = string.Empty;
    public string ReconciliationExceptionId { get; set; } = string.Empty;
    public string InstructionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public string? SettlementAccount { get; set; }
    public string? Counterparty { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class CreateSettlementInstructionRequestVm
{
    public string ReconciliationExceptionId { get; set; } = string.Empty;
    public string InstructionType { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public string? SettlementAccount { get; set; }
    public string? Counterparty { get; set; }
    public DateTime? DueAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class CollateralManagementSummaryVm
{
    public List<CollateralRecordVm> CollateralItems { get; set; } = new();
    public List<CovenantRecordVm> Covenants { get; set; } = new();
    public int ExpiringValuationsCount { get; set; }
    public int OverdueCovenantsCount { get; set; }
}

public sealed class CollateralRecordVm
{
    public string Id { get; set; } = string.Empty;
    public string LoanId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CollateralType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal RegisteredValue { get; set; }
    public decimal CurrentValuation { get; set; }
    public DateTime? ValuationDate { get; set; }
    public DateTime? ValuationExpiryDate { get; set; }
    public string PerfectionStatus { get; set; } = string.Empty;
    public string? DocumentReference { get; set; }
    public string? CustodyLocation { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class CovenantRecordVm
{
    public string Id { get; set; } = string.Empty;
    public string LoanId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CovenantType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public string Detail { get; set; } = string.Empty;
}

public sealed class UpdateCollateralRecordRequestVm
{
    public decimal? CurrentValuation { get; set; }
    public DateTime? ValuationDate { get; set; }
    public DateTime? ValuationExpiryDate { get; set; }
    public string? PerfectionStatus { get; set; }
    public string? DocumentReference { get; set; }
    public string? CustodyLocation { get; set; }
    public string? Status { get; set; }
}

public sealed class UpdateCovenantRecordRequestVm
{
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public string? Detail { get; set; }
}

public sealed class DeveloperPortalSummaryVm
{
    public List<DeveloperPortalMetricVm> Metrics { get; set; } = new();
    public List<ApiProductDefinitionVm> Products { get; set; } = new();
    public List<PartnerApplicationVm> PartnerApplications { get; set; } = new();
    public List<WebhookSubscriptionVm> WebhookSubscriptions { get; set; } = new();
    public List<WebhookDeliveryLogVm> DeliveryLogs { get; set; } = new();
    public List<WebhookEventCatalogItemVm> EventCatalog { get; set; } = new();
}

public sealed class SupervisoryMetricVm
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
}

public sealed class RelationshipBankingSummaryVm
{
    public List<SupervisoryMetricVm> Metrics { get; set; } = new();
    public List<RelationshipCustomerItemVm> TopRelationships { get; set; } = new();
    public List<RelationshipManagerPerformanceItemVm> ManagerPerformance { get; set; } = new();
    public List<RelationshipEngagementItemVm> RecentEngagements { get; set; } = new();
    public List<AssignableStaffItemVm> AssignableStaff { get; set; } = new();
}

public sealed class RelationshipCustomerItemVm
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public int ActiveAccountCount { get; set; }
    public int ActiveLoanCount { get; set; }
    public int ActiveInvestmentCount { get; set; }
    public decimal DepositBalance { get; set; }
    public decimal LoanExposure { get; set; }
    public decimal InvestmentBalance { get; set; }
    public decimal EstimatedRelationshipValue { get; set; }
    public decimal EstimatedAnnualRevenue { get; set; }
    public int HouseholdOrGroupLinks { get; set; }
    public int OpenComplaintCount { get; set; }
    public string RiskSummary { get; set; } = string.Empty;
    public string? RelationshipOwnerUserId { get; set; }
    public string RelationshipOwner { get; set; } = "Unassigned";
    public DateTime? LastEngagementAt { get; set; }
}

public sealed class RelationshipPortfolioDetailVm
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string RiskSummary { get; set; } = string.Empty;
    public string? RelationshipOwnerUserId { get; set; }
    public string RelationshipOwner { get; set; } = "Unassigned";
    public decimal DepositBalance { get; set; }
    public decimal InvestmentBalance { get; set; }
    public decimal LoanExposure { get; set; }
    public decimal EstimatedAnnualRevenue { get; set; }
    public decimal EstimatedRelationshipValue { get; set; }
    public DateTime? LastEngagementAt { get; set; }
    public int OpenComplaintCount { get; set; }
    public int HouseholdOrGroupLinks { get; set; }
    public List<RelationshipPortfolioBreakdownItemVm> ProductBreakdown { get; set; } = new();
    public List<RelationshipEngagementItemVm> RecentEngagements { get; set; } = new();
}

public sealed class RelationshipPortfolioBreakdownItemVm
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Balance { get; set; }
    public decimal Contribution { get; set; }
}

public sealed class RelationshipManagerPerformanceItemVm
{
    public string RelationshipOwner { get; set; } = "Unassigned";
    public int CustomerCount { get; set; }
    public decimal DepositBalance { get; set; }
    public decimal LoanExposure { get; set; }
    public decimal EstimatedAnnualRevenue { get; set; }
    public int HighRiskRelationships { get; set; }
    public int OpenComplaintCount { get; set; }
}

public sealed class AssignRelationshipOwnerRequestVm
{
    public string CustomerId { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public string? AssignmentNote { get; set; }
}

public sealed class AssignableStaffItemVm
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BranchId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class RelationshipEngagementItemVm
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public sealed class DigitalChannelOperationsSummaryVm
{
    public List<SupervisoryMetricVm> Metrics { get; set; } = new();
    public List<DigitalChannelMetricVm> ChannelMetrics { get; set; } = new();
    public List<DigitalSessionRiskItemVm> SessionRiskItems { get; set; } = new();
    public List<DigitalComplaintItemVm> ComplaintQueue { get; set; } = new();
    public List<DigitalKycItemVm> KycQueue { get; set; } = new();
}

public sealed class DigitalChannelMetricVm
{
    public string ChannelName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TransactionVolume { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public sealed class DigitalSessionRiskItemVm
{
    public string SessionId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string RiskLabel { get; set; } = string.Empty;
}

public sealed class DigitalComplaintItemVm
{
    public string ComplaintId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerTeam { get; set; } = string.Empty;
    public DateTime SlaDueAt { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public sealed class DigitalKycItemVm
{
    public string KycCaseId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? ReviewerName { get; set; }
}

public sealed class RegulatoryIntelligenceSummaryVm
{
    public List<SupervisoryMetricVm> Metrics { get; set; } = new();
    public RegulatoryReadinessVm Readiness { get; set; } = new();
    public List<RegulatoryQueueItemVm> Queue { get; set; } = new();
    public List<RegulatoryHistoryItemVm> History { get; set; } = new();
    public List<RegulatoryVarianceItemVm> Variances { get; set; } = new();
}

public sealed class RegulatoryReadinessVm
{
    public bool ProfileConfigured { get; set; }
    public bool ReadyForSubmission { get; set; }
    public string SubmissionMode { get; set; } = string.Empty;
    public string SourceReportCode { get; set; } = string.Empty;
    public int PendingReturns { get; set; }
    public int ReturnsReadyForSubmission { get; set; }
    public List<string> MissingRequirements { get; set; } = new();
    public List<string> Notes { get; set; } = new();
    public string? LastPreparedReturnDate { get; set; }
    public string? LastSubmissionAt { get; set; }
}

public sealed class RegulatoryQueueItemVm
{
    public int Id { get; set; }
    public string ReturnType { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public string ReportingPeriodStart { get; set; } = string.Empty;
    public string ReportingPeriodEnd { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public bool IsReadyForSubmission { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public List<string> ValidationMessages { get; set; } = new();
}

public sealed class RegulatoryHistoryItemVm
{
    public int Id { get; set; }
    public string ReturnType { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = string.Empty;
    public string? SubmissionDate { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string BogReferenceNumber { get; set; } = string.Empty;
    public string TransportStatus { get; set; } = string.Empty;
    public string AcknowledgementStatus { get; set; } = string.Empty;
    public string? AcknowledgementReference { get; set; }
    public string? AcknowledgedAt { get; set; }
    public string? TransportMessage { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
}

public sealed class RegulatoryVarianceItemVm
{
    public string Reference { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string ActionHint { get; set; } = string.Empty;
    public string ResolutionStatus { get; set; } = "OPEN";
    public string? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public string? AssignedByName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RegulatoryVarianceEventVm> Events { get; set; } = new();
}

public sealed class ResolveRegulatoryVarianceRequestVm
{
    public string Reference { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
}

public sealed class AssignRegulatoryVarianceRequestVm
{
    public string Reference { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public string? AssignmentNote { get; set; }
}

public sealed class RegulatoryVarianceEventVm
{
    public string EventType { get; set; } = string.Empty;
    public string? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
    public string Detail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class DeveloperPortalMetricVm
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

public sealed class ApiProductDefinitionVm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string AuthModel { get; set; } = string.Empty;
    public string BasePath { get; set; } = string.Empty;
    public string DocumentationPath { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; }
    public bool SupportsWebhooks { get; set; }
    public bool SupportsSandbox { get; set; }
    public string ScopeSummary { get; set; } = string.Empty;
    public DateTime? LastPublishedAt { get; set; }
}

public sealed class PartnerApplicationVm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public List<string> ApiProductIds { get; set; } = new();
    public string SandboxKeyPreview { get; set; } = string.Empty;
    public string? ProductionKeyPreview { get; set; }
    public DateTime? LastKeyRotatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? ProductionKeyActivatedAt { get; set; }
}

public sealed class WebhookSubscriptionVm
{
    public string Id { get; set; } = string.Empty;
    public string PartnerApplicationId { get; set; } = string.Empty;
    public string PartnerApplicationName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SigningSecretPreview { get; set; } = string.Empty;
    public DateTime? LastDeliveryAt { get; set; }
    public string? LastDeliveryStatus { get; set; }
}

public sealed class WebhookDeliveryLogVm
{
    public string Id { get; set; } = string.Empty;
    public string WebhookSubscriptionId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public int? ResponseCode { get; set; }
    public int AttemptNumber { get; set; }
    public string? FailureReason { get; set; }
    public DateTime DeliveredAt { get; set; }
}

public sealed class WebhookEventCatalogItemVm
{
    public string EventName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class CreatePartnerApplicationRequestVm
{
    public string Name { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public List<string> ApiProductIds { get; set; } = new();
}

public sealed class CreateWebhookSubscriptionRequestVm
{
    public string PartnerApplicationId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
}

public sealed class PromotePartnerApplicationRequestVm
{
    public string Environment { get; set; } = "PRODUCTION";
}

public sealed class ReplayWebhookRequestVm
{
    public string WebhookSubscriptionId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
}
