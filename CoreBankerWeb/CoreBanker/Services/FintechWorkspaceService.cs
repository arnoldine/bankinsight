using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoreBanker.State;

namespace CoreBanker.Services;

public sealed class FintechWorkspaceService : ApiClientBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IConfiguration _configuration;
    private readonly AppState _appState;

    public FintechWorkspaceService(HttpClient httpClient, AppState appState, IConfiguration configuration)
        : base(httpClient, appState)
    {
        _configuration = configuration;
        _appState = appState;
    }

    public async Task<FintechWorkspaceSnapshot> GetWorkspaceSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var healthTask = GetHealthAsync(cancellationToken);
        var walletsTask = FintechGetAsync<List<FintechWalletSummary>>("wallets", cancellationToken);
        var alertsTask = FintechGetAsync<List<FintechAlert>>("risk/alerts", cancellationToken);
        var approvalsTask = FintechGetAsync<List<FintechApprovalQueueItem>>("admin/approvals", cancellationToken);
        var reconciliationTask = FintechGetAsync<List<FintechReconciliationItem>>("reconciliation/items", cancellationToken);
        var transfersTask = FintechGetAsync<FintechPagedResponse<FintechTransferExplorerItem>>("admin/transfers?page=1&pageSize=8", cancellationToken);
        var duplicateWebhooksTask = FintechGetAsync<FintechPagedResponse<FintechAuditEvent>>("admin/audit?page=1&pageSize=5&action=WebhookDuplicateIgnored", cancellationToken);
        var divergenceTask = FintechGetAsync<FintechPagedResponse<FintechAuditEvent>>("admin/audit?page=1&pageSize=5&action=ProviderLedgerDivergenceDetected", cancellationToken);

        await Task.WhenAll(healthTask, walletsTask, alertsTask, approvalsTask, reconciliationTask, transfersTask, duplicateWebhooksTask, divergenceTask);

        return new FintechWorkspaceSnapshot
        {
            Health = await healthTask,
            BankInsightWorkspaceUrl = GetBankInsightWorkspaceUrl(),
            FintechAdminUrl = GetFintechAdminUrl(),
            SwaggerUrl = GetSwaggerUrl(),
            Wallets = await walletsTask ?? [],
            Alerts = await alertsTask ?? [],
            Approvals = await approvalsTask ?? [],
            ReconciliationItems = await reconciliationTask ?? [],
            Transfers = (await transfersTask)?.Items ?? [],
            OperationsWatch = new FintechOperationsWatch
            {
                DuplicateWebhookEvents = (await duplicateWebhooksTask)?.Items ?? [],
                DivergenceEvents = (await divergenceTask)?.Items ?? []
            }
        };
    }

    public async Task<FintechTransferInvestigation> GetTransferInvestigationAsync(string transferOrderId, CancellationToken cancellationToken = default)
    {
        var transferTask = FintechGetAsync<FintechTransferDetail>($"admin/transfers/{Uri.EscapeDataString(transferOrderId)}", cancellationToken);
        var journalsTask = FintechGetAsync<List<FintechJournalEntryDetail>>($"admin/transfers/{Uri.EscapeDataString(transferOrderId)}/journals", cancellationToken);
        var auditTask = FintechGetAsync<List<FintechAuditEvent>>($"admin/audit/TransferOrder/{Uri.EscapeDataString(transferOrderId)}", cancellationToken);

        await Task.WhenAll(transferTask, journalsTask, auditTask);

        return new FintechTransferInvestigation
        {
            Transfer = await transferTask ?? new FintechTransferDetail(),
            Journals = await journalsTask ?? [],
            AuditEvents = await auditTask ?? []
        };
    }

    public Task<FintechApprovalDecisionResult?> DecideApprovalAsync(string approvalRequestId, FintechApprovalDecisionRequest request, CancellationToken cancellationToken = default)
        => FintechPostAsync<FintechApprovalDecisionRequest, FintechApprovalDecisionResult>($"admin/approvals/{Uri.EscapeDataString(approvalRequestId)}/decision", request, cancellationToken);

    public Task<FintechReconciliationItem?> CreateReconciliationItemAsync(FintechManualReconciliationRequest request, CancellationToken cancellationToken = default)
        => FintechPostAsync<FintechManualReconciliationRequest, FintechReconciliationItem>("reconciliation/items", request, cancellationToken);

    public string GetBankInsightWorkspaceUrl()
        => _configuration["FintechPlatform:BankInsightWorkspaceUrl"]?.Trim()
            ?? "http://localhost:3001";

    public string GetFintechAdminUrl()
        => _configuration["FintechPlatform:AdminPortalUrl"]?.Trim()
            ?? "https://localhost:7020";

    public string GetSwaggerUrl()
        => _configuration["FintechPlatform:SwaggerUrl"]?.Trim()
            ?? $"{ResolveApiBaseUrl()}/swagger";

    private async Task<FintechHealthStatus> GetHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var message = CreateRequest(HttpMethod.Get, ResolveHealthUrl());
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            return new FintechHealthStatus
            {
                Status = response.IsSuccessStatusCode ? "Healthy" : "Unavailable",
                CheckedAtUtc = DateTimeOffset.UtcNow
            };
        }
        catch
        {
            return new FintechHealthStatus
            {
                Status = "Unavailable",
                CheckedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }

    private async Task<T?> FintechGetAsync<T>(string relativePath, CancellationToken cancellationToken)
    {
        using var message = CreateRequest(HttpMethod.Get, BuildApiUrl(relativePath));
        using var response = await _httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private async Task<TResponse?> FintechPostAsync<TRequest, TResponse>(string relativePath, TRequest request, CancellationToken cancellationToken)
    {
        using var message = CreateRequest(HttpMethod.Post, BuildApiUrl(relativePath));
        message.Content = JsonContent.Create(request, options: JsonOptions);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var message = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(_appState.AccessToken))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _appState.AccessToken);
        }

        return message;
    }

    private string BuildApiUrl(string relativePath)
        => $"{ResolveApiBaseUrl().TrimEnd('/')}/{relativePath.TrimStart('/')}";

    private string ResolveApiBaseUrl()
        => (_configuration["FintechPlatform:ApiBaseUrl"]?.TrimEnd('/')
            ?? "http://localhost:5176/api/v1").TrimEnd('/');

    private string ResolveHealthUrl()
        => _configuration["FintechPlatform:HealthUrl"]?.Trim()
            ?? "http://localhost:5176/health";
}

public sealed class FintechWorkspaceSnapshot
{
    public FintechHealthStatus Health { get; set; } = new();
    public string BankInsightWorkspaceUrl { get; set; } = string.Empty;
    public string FintechAdminUrl { get; set; } = string.Empty;
    public string SwaggerUrl { get; set; } = string.Empty;
    public List<FintechWalletSummary> Wallets { get; set; } = [];
    public List<FintechAlert> Alerts { get; set; } = [];
    public List<FintechApprovalQueueItem> Approvals { get; set; } = [];
    public List<FintechReconciliationItem> ReconciliationItems { get; set; } = [];
    public List<FintechTransferExplorerItem> Transfers { get; set; } = [];
    public FintechOperationsWatch OperationsWatch { get; set; } = new();
}

public sealed class FintechHealthStatus
{
    public string Status { get; set; } = "Unavailable";
    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class FintechWalletSummary
{
    public string WalletId { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class FintechAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string AlertCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class FintechApprovalQueueItem
{
    public string ApprovalRequestId { get; set; } = string.Empty;
    public string TransferOrderId { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class FintechReconciliationItem
{
    public string ReconciliationItemId { get; set; } = string.Empty;
    public string ReconciliationType { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string InternalReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class FintechTransferExplorerItem
{
    public string TransferOrderId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskStatus { get; set; } = string.Empty;
    public string ComplianceStatus { get; set; } = string.Empty;
    public string? PartnerReference { get; set; }
    public decimal Amount { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class FintechTransferDetail
{
    public string TransferOrderId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskStatus { get; set; } = string.Empty;
    public string ComplianceStatus { get; set; } = string.Empty;
    public string? PartnerReference { get; set; }
    public string? FailureReason { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public string SourceWalletId { get; set; } = string.Empty;
}

public sealed class FintechJournalLine
{
    public string LedgerAccountId { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Narrative { get; set; } = string.Empty;
}

public sealed class FintechJournalEntryDetail
{
    public string JournalEntryId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SourceModule { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? TransferOrderId { get; set; }
    public string? ReversalOfJournalEntryId { get; set; }
    public List<FintechJournalLine> Lines { get; set; } = [];
}

public sealed class FintechAuditEvent
{
    public string AuditEventId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}

public sealed class FintechOperationsWatch
{
    public List<FintechAuditEvent> DuplicateWebhookEvents { get; set; } = [];
    public List<FintechAuditEvent> DivergenceEvents { get; set; } = [];
}

public sealed class FintechTransferInvestigation
{
    public FintechTransferDetail Transfer { get; set; } = new();
    public List<FintechJournalEntryDetail> Journals { get; set; } = [];
    public List<FintechAuditEvent> AuditEvents { get; set; } = [];
}

public sealed class FintechApprovalDecisionRequest
{
    public string ApprovedBy { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string DecisionNotes { get; set; } = string.Empty;
}

public sealed class FintechTransferResponse
{
    public string TransferId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskStatus { get; set; } = string.Empty;
    public string ComplianceStatus { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
}

public sealed class FintechApprovalDecisionResult
{
    public FintechApprovalQueueItem Approval { get; set; } = new();
    public FintechTransferResponse? Transfer { get; set; }
}

public sealed class FintechManualReconciliationRequest
{
    public string ReconciliationType { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string InternalReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Notes { get; set; } = string.Empty;
}

public sealed class FintechPagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<T> Items { get; set; } = [];
}
