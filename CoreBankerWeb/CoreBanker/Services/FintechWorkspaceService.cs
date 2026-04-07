using CoreBanker.State;

namespace CoreBanker.Services;

public sealed class FintechWorkspaceService : ApiClientBase
{
    private readonly IConfiguration _configuration;

    public FintechWorkspaceService(HttpClient httpClient, AppState appState, IConfiguration configuration)
        : base(httpClient, appState)
    {
        _configuration = configuration;
    }

    public async Task<FintechWorkspaceSnapshot> GetWorkspaceSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var health = await GetHealthAsync(cancellationToken);

        return new FintechWorkspaceSnapshot
        {
            Health = health,
            BankInsightWorkspaceUrl = GetBankInsightWorkspaceUrl(),
            FintechAdminUrl = GetFintechAdminUrl(),
            SwaggerUrl = GetSwaggerUrl(),
            SupportedFlows =
            [
                "Custodial crypto deposits and internal GHS wallet balances",
                "Wallet transfers to mobile money, bank accounts, and internal recipients",
                "Compliance review, approval queues, and investigation workflows",
                "Reconciliation, provider status sync, and payout exception handling"
            ],
            GovernanceControls =
            [
                "Double-entry ledger with pending, settlement, and reversal posting states",
                "Paystack sandbox-ready bank payout verification and replay-safe webhooks",
                "Provider-ledger divergence breaks with reconciliation escalation",
                "Audit-backed callback handling and maker-checker approval flow"
            ]
        };
    }

    public string GetBankInsightWorkspaceUrl()
        => _configuration["FintechPlatform:BankInsightWorkspaceUrl"]?.Trim()
            ?? "http://localhost:5176";

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
            using var response = await _httpClient.GetAsync($"{ResolveApiBaseUrl()}/health", cancellationToken);
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

    private string ResolveApiBaseUrl()
        => (_configuration["FintechPlatform:ApiBaseUrl"]?.TrimEnd('/')
            ?? "http://localhost:5176").TrimEnd('/');
}

public sealed class FintechWorkspaceSnapshot
{
    public FintechHealthStatus Health { get; set; } = new();
    public string BankInsightWorkspaceUrl { get; set; } = string.Empty;
    public string FintechAdminUrl { get; set; } = string.Empty;
    public string SwaggerUrl { get; set; } = string.Empty;
    public List<string> SupportedFlows { get; set; } = [];
    public List<string> GovernanceControls { get; set; } = [];
}

public sealed class FintechHealthStatus
{
    public string Status { get; set; } = "Unavailable";
    public DateTimeOffset CheckedAtUtc { get; set; }
}
