using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreBanker.State;

namespace CoreBanker.Services;

public class MicrofinanceService : ApiClientBase
{
    public MicrofinanceService(HttpClient httpClient, AppState appState)
        : base(httpClient, appState)
    {
    }

    public Task<MicrofinanceSummaryVm?> GetSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync<MicrofinanceSummaryVm>("/api/microfinance/summary", cancellationToken);

    public async Task<List<CustomerSearchItemVm>> SearchCustomersAsync(string query, CancellationToken cancellationToken = default)
        => await GetAsync<List<CustomerSearchItemVm>>($"/api/microfinance/customers/search?query={Uri.EscapeDataString(query)}", cancellationToken) ?? new List<CustomerSearchItemVm>();

    public async Task<List<AccountSearchItemVm>> SearchAccountsAsync(string query, string? customerId = null, CancellationToken cancellationToken = default)
    {
        var requestUri = string.IsNullOrWhiteSpace(customerId)
            ? $"/api/microfinance/accounts/search?query={Uri.EscapeDataString(query)}"
            : $"/api/microfinance/accounts/search?query={Uri.EscapeDataString(query)}&customerId={Uri.EscapeDataString(customerId)}";
        return await GetAsync<List<AccountSearchItemVm>>(requestUri, cancellationToken) ?? new List<AccountSearchItemVm>();
    }

    public async Task<List<MicrofinanceLoanPolicyVm>> GetLoanPoliciesAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<MicrofinanceLoanPolicyVm>>("/api/microfinance/loan-policies", cancellationToken) ?? new List<MicrofinanceLoanPolicyVm>();

    public Task<CollectorAssignmentVm?> UpsertAssignmentAsync(UpsertCollectorAssignmentRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<UpsertCollectorAssignmentRequestVm, CollectorAssignmentVm>("/api/microfinance/assignments", request, cancellationToken);

    public Task<FieldCollectionBatchVm?> OpenBatchAsync(OpenFieldCollectionBatchRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<OpenFieldCollectionBatchRequestVm, FieldCollectionBatchVm>("/api/microfinance/batches", request, cancellationToken);

    public Task<FieldCollectionBatchLineVm?> RecordCollectionAsync(string batchId, RecordFieldCollectionRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<RecordFieldCollectionRequestVm, FieldCollectionBatchLineVm>($"/api/microfinance/batches/{Uri.EscapeDataString(batchId)}/collections", request, cancellationToken);

    public Task<FieldCollectionBatchVm?> SubmitBatchAsync(string batchId, SubmitFieldCollectionBatchRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<SubmitFieldCollectionBatchRequestVm, FieldCollectionBatchVm>($"/api/microfinance/batches/{Uri.EscapeDataString(batchId)}/submit", request, cancellationToken);

    public Task<FieldCollectionBatchVm?> SettleBatchAsync(string batchId, SettleFieldCollectionBatchRequestVm request, CancellationToken cancellationToken = default)
        => PostAsync<SettleFieldCollectionBatchRequestVm, FieldCollectionBatchVm>($"/api/microfinance/batches/{Uri.EscapeDataString(batchId)}/settle", request, cancellationToken);
}

public class MicrofinanceSummaryVm
{
    public DateTime BusinessDate { get; set; }
    public List<MicrofinanceMetricVm> Metrics { get; set; } = new();
    public List<FieldStaffDirectoryItemVm> FieldStaff { get; set; } = new();
    public List<CollectorAssignmentVm> ActiveAssignments { get; set; } = new();
    public List<FieldCollectionBatchVm> OpenBatches { get; set; } = new();
    public List<CompulsorySavingsAlertVm> CompulsorySavingsAlerts { get; set; } = new();
    public List<MicrofinanceLoanPolicyVm> LoanPolicies { get; set; } = new();
}

public class MicrofinanceMetricVm
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
}

public class FieldStaffDirectoryItemVm
{
    public string StaffId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? BranchId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CustomerSearchItemVm
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? BranchId { get; set; }
    public string? PhoneNumber { get; set; }
}

public class AccountSearchItemVm
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal AvailableBalance { get; set; }
    public bool IsCompulsorySavings { get; set; }
}

public class MicrofinanceLoanPolicyVm
{
    public string LoanProductId { get; set; } = string.Empty;
    public string LoanProductCode { get; set; } = string.Empty;
    public string LoanProductName { get; set; } = string.Empty;
    public string RepaymentFrequency { get; set; } = string.Empty;
    public bool RequiresCompulsorySavings { get; set; }
    public decimal? MinimumSavingsToLoanRatio { get; set; }
}

public class CollectorAssignmentVm
{
    public string Id { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? RouteCode { get; set; }
    public string? MeetingDay { get; set; }
    public string CollectionFrequency { get; set; } = string.Empty;
    public string? TargetDepositAccountId { get; set; }
    public string? TargetLoanId { get; set; }
    public bool IsPrimaryCollector { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime? NextCollectionDate { get; set; }
}

public class FieldCollectionBatchVm
{
    public string Id { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public DateTime BusinessDate { get; set; }
    public string? RouteCode { get; set; }
    public string CollectionType { get; set; } = string.Empty;
    public decimal OpeningFloat { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal SettledAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Status { get; set; } = string.Empty;
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? SettledAtUtc { get; set; }
    public string? SettlementReference { get; set; }
    public string? Notes { get; set; }
    public List<FieldCollectionBatchLineVm> Lines { get; set; } = new();
}

public class FieldCollectionBatchLineVm
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? AssignmentId { get; set; }
    public string? TargetAccountId { get; set; }
    public string? TargetLoanId { get; set; }
    public string CollectionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Status { get; set; } = string.Empty;
    public string? Narrative { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime CollectedAtUtc { get; set; }
    public string? ReceiptNumber { get; set; }
}

public class CompulsorySavingsAlertVm
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string LoanProductName { get; set; } = string.Empty;
    public decimal RequiredAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal ShortfallAmount { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class UpsertCollectorAssignmentRequestVm
{
    public string StaffId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? RouteCode { get; set; }
    public string? MeetingDay { get; set; }
    public string CollectionFrequency { get; set; } = "DAILY";
    public string? TargetDepositAccountId { get; set; }
    public string? TargetLoanId { get; set; }
    public bool IsPrimaryCollector { get; set; } = true;
}

public class OpenFieldCollectionBatchRequestVm
{
    public string StaffId { get; set; } = string.Empty;
    public DateTime? BusinessDate { get; set; }
    public string? RouteCode { get; set; }
    public string CollectionType { get; set; } = "SAVINGS";
    public decimal OpeningFloat { get; set; }
    public string Currency { get; set; } = "GHS";
    public string? Notes { get; set; }
}

public class RecordFieldCollectionRequestVm
{
    public string? AssignmentId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? TargetAccountId { get; set; }
    public string? TargetLoanId { get; set; }
    public string CollectionType { get; set; } = "SAVINGS";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string? Narrative { get; set; }
    public string? ExternalReference { get; set; }
}

public class SubmitFieldCollectionBatchRequestVm
{
    public string? Notes { get; set; }
}

public class SettleFieldCollectionBatchRequestVm
{
    public decimal SettledAmount { get; set; }
    public string? SettlementReference { get; set; }
    public string? Notes { get; set; }
}
