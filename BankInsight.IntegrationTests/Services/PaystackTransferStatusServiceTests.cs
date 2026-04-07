using System.Net;
using System.Text;
using BankInsight.API.Services;
using FluentAssertions;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.Services;
using HybridTransfer.Infrastructure.Persistence;
using HybridTransfer.Domain.Common;
using HybridTransfer.Domain.Transfers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BankInsight.IntegrationTests.Services;

public class PaystackTransferStatusServiceTests
{
    [Fact]
    public async Task VerifyBankTransferAsync_MapsSuccessfulPaystackStatusToPendingSettlement()
    {
        var repository = new InMemoryTransferOrderRepository();
        var auditRepository = new InMemoryAuditEventRepository();
        var auditTrail = new AuditTrailService(auditRepository);
        var transfer = new TransferOrder(TransferType.BankPayout, TransferChannel.Bank, "Wallet", Guid.Parse("11111111-1111-1111-1111-111111111111"), "057:0123456789", 125.50m, "customer", "idem-verify-1");
        transfer.Authorize("checker");
        transfer.Submit("TRF_paystack123");
        await repository.SaveAsync(transfer, CancellationToken.None);

        var handler = new RecordingMessageHandler();
        handler.AddResponse("https://api.paystack.co/transfer/verify/TRF_paystack123", HttpStatusCode.OK, """
            {"status":true,"message":"Transfer fetched","data":{"reference":"bi-bank-ref","status":"success","transfer_code":"TRF_paystack123"}}
            """);

        var provider = new BankInsightBankTransferProvider(
            Options.Create(new FintechProviderOptions
            {
                BankTransfer = new RailProviderOptions
                {
                    Mode = "Live",
                    BaseUrl = "https://api.paystack.co",
                    ApiKey = "sk_test_123",
                    ApiKeyHeaderName = "Authorization",
                    ApiKeyPrefix = "Bearer",
                    ProviderCode = "paystack-bank-gh",
                    StatusPath = "/transfer/verify/{reference}"
                }
            }),
            new TestHttpClientFactory(handler),
            NullLogger<BankInsightBankTransferProvider>.Instance);

        var service = new ProviderTransferStatusService(repository, provider, auditTrail);
        var result = await service.VerifyBankTransferAsync("TRF_paystack123", "ops-user", CancellationToken.None);

        result.TransferStatus.Should().Be(TransferStatus.PendingSettlement.ToString());
        result.ProviderStatus.Should().Be("success");

        var updated = await repository.GetByPartnerReferenceAsync("TRF_paystack123", CancellationToken.None);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(TransferStatus.PendingSettlement);
    }

    [Fact]
    public async Task ApplyBankTransferCallbackAsync_MapsFailedStatusToFailedTransfer()
    {
        var repository = new InMemoryTransferOrderRepository();
        var auditRepository = new InMemoryAuditEventRepository();
        var auditTrail = new AuditTrailService(auditRepository);
        var transfer = new TransferOrder(TransferType.BankPayout, TransferChannel.Bank, "Wallet", Guid.Parse("11111111-1111-1111-1111-111111111111"), "057:0123456789", 125.50m, "customer", "idem-callback-1");
        transfer.Authorize("checker");
        transfer.Submit("TRF_paystack999");
        await repository.SaveAsync(transfer, CancellationToken.None);

        var provider = new BankInsightBankTransferProvider(
            Options.Create(new FintechProviderOptions { BankTransfer = new RailProviderOptions { Mode = "Mock" } }),
            new TestHttpClientFactory(new RecordingMessageHandler()),
            NullLogger<BankInsightBankTransferProvider>.Instance);

        var service = new ProviderTransferStatusService(repository, provider, auditTrail);
        var result = await service.ApplyBankTransferCallbackAsync("TRF_paystack999", "failed", "insufficient balance", "webhook:paystack-bank-gh", CancellationToken.None);

        result.TransferStatus.Should().Be(TransferStatus.Failed.ToString());
        result.FailureReason.Should().Be("insufficient balance");

        var updated = await repository.GetByPartnerReferenceAsync("TRF_paystack999", CancellationToken.None);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(TransferStatus.Failed);
        updated.FailureReason.Should().Be("insufficient balance");
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public TestHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class RecordingMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses = new(StringComparer.Ordinal);

        public void AddResponse(string url, HttpStatusCode statusCode, string json)
        {
            _responses[url] = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.TryGetValue(request.RequestUri!.ToString(), out var response))
            {
                return Task.FromResult(response);
            }

            throw new InvalidOperationException($"No response configured for {request.RequestUri}");
        }
    }
}

