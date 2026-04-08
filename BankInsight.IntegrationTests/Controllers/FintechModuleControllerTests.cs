using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;

namespace BankInsight.IntegrationTests.Controllers;

public class FintechModuleControllerTests : IntegrationTestBase
{
    public FintechModuleControllerTests(TestWebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task CreateDepositAddress_ReturnsBankInsightManagedAddress()
    {
        var request = new
        {
            walletId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            asset = "USDT",
            network = "TRON"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/crypto/deposits/addresses", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DepositAddressResponse>();
        payload.Should().NotBeNull();
        payload!.Asset.Should().Be("USDT");
        payload.Network.Should().Be("TRON");
        payload.WalletAddress.Should().StartWith("BI-USDT-TRON-");
    }

    [Fact]
    public async Task CreateAndListReconciliationItems_SucceedsThroughSharedBankInsightApiHost()
    {
        var request = new
        {
            reconciliationType = "ManualAdjustment",
            externalReference = "EXT-RECON-001",
            internalReference = "INT-RECON-001",
            amount = 125.50m,
            currency = "GHS",
            notes = "Shared host fintech reconciliation test"
        };

        var createResponse = await Client.PostAsJsonAsync("/api/v1/reconciliation/items", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ReconciliationItemResponse>();
        created.Should().NotBeNull();
        created!.ReconciliationType.Should().Be("ManualAdjustment");

        var listResponse = await Client.GetAsync("/api/v1/reconciliation/items");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await listResponse.Content.ReadFromJsonAsync<List<ReconciliationItemResponse>>();
        items.Should().NotBeNull();
        items!.Should().Contain(item => item.ReconciliationItemId == created.ReconciliationItemId);
    }

    [Fact]
    public async Task FintechAdminTransfersEndpoint_IsServedByBankInsightApi()
    {
        var response = await Client.GetAsync("/api/v1/admin/transfers?page=1&pageSize=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<TransferExplorerItemResponse>>();
        payload.Should().NotBeNull();
        payload!.Page.Should().Be(1);
        payload.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task BankWebhook_DuplicateCallback_IsAcceptedOnceThenIgnored()
    {
        var payoutRequest = new
        {
            sourceWalletId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            bankCode = "058",
            accountNumber = "0123456789",
            amount = 25.00m,
            currency = "GHS",
            accountName = "Sandbox Beneficiary",
            destinationCountryCode = "GH",
            narrative = "Webhook replay test"
        };

        Client.DefaultRequestHeaders.Remove("Idempotency-Key");
        Client.DefaultRequestHeaders.Add("Idempotency-Key", $"itest-{Guid.NewGuid():N}");

        var createResponse = await Client.PostAsJsonAsync("/api/v1/transfers/bank", payoutRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var transfer = await createResponse.Content.ReadFromJsonAsync<TransferResponse>();
        transfer.Should().NotBeNull();
        transfer!.ProviderReference.Should().NotBeNullOrWhiteSpace();

        var payload = $$"""
        {
          "event":"transfer.failed",
          "data":{
            "transfer_code":"{{transfer.ProviderReference}}",
            "status":"failed",
            "reason":"Sandbox duplicate callback test"
          }
        }
        """;

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/bank/paystack-bank-gh");
        firstRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        firstRequest.Headers.Add("x-paystack-signature", ComputePaystackSignature(payload, "test-paystack-secret"));

        var firstResponse = await Client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var syncResult = await firstResponse.Content.ReadFromJsonAsync<TransferStatusSyncResult>();
        syncResult.Should().NotBeNull();
        syncResult!.TransferOrderId.Should().Be(transfer.TransferId);
        syncResult.TransferStatus.Should().Be("Reversed");

        using var duplicateRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/bank/paystack-bank-gh");
        duplicateRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        duplicateRequest.Headers.Add("x-paystack-signature", ComputePaystackSignature(payload, "test-paystack-secret"));

        var duplicateResponse = await Client.SendAsync(duplicateRequest);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var duplicatePayload = await duplicateResponse.Content.ReadAsStringAsync();
        duplicatePayload.Should().Contain("DuplicateIgnored");

        var auditResponse = await Client.GetAsync($"/api/v1/admin/audit/TransferOrder/{transfer.TransferId}");
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auditEvents = await auditResponse.Content.ReadFromJsonAsync<List<AuditEventResponse>>();
        auditEvents.Should().NotBeNull();
        auditEvents!.Should().Contain(x => x.Action == "WebhookApplied");
        auditEvents.Should().Contain(x => x.Action == "WebhookDuplicateIgnored");
    }

    private static string ComputePaystackSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private sealed record DepositAddressResponse(string WalletAddress, string Asset, string Network, int RequiredConfirmations);
    private sealed record ReconciliationItemResponse(Guid ReconciliationItemId, string ReconciliationType, string ExternalReference, string InternalReference, decimal Amount, string Currency, string Status, string Notes);
    private sealed record PagedResponse<T>(int Page, int PageSize, int TotalCount, IReadOnlyCollection<T> Items);
    private sealed record TransferExplorerItemResponse(Guid TransferOrderId, string Type, string Channel, string Status, string RiskStatus, string ComplianceStatus, string? PartnerReference, decimal Amount, string CreatedBy, DateTimeOffset CreatedAtUtc);
    private sealed record TransferResponse(Guid TransferId, string Status, string RiskStatus, string ComplianceStatus, string? ProviderReference);
    private sealed record TransferStatusSyncResult(Guid TransferOrderId, string ProviderReference, string TransferStatus, string ProviderStatus, string? FailureReason);
    private sealed record AuditEventResponse(Guid AuditEventId, string Action, string EntityType, string EntityId, string ActorId, DateTimeOffset CreatedAtUtc, string? BeforeJson, string? AfterJson);
}
