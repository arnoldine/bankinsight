using System.Net;
using System.Text;
using BankInsight.API.Services;
using FluentAssertions;
using HybridTransfer.Application.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BankInsight.IntegrationTests.Services;

public class PaystackBankTransferProviderTests
{
    [Fact]
    public async Task InitiatePayoutAsync_WithPaystackLiveMode_UsesResolveRecipientAndTransferFlow()
    {
        var handler = new RecordingMessageHandler();
        handler.AddResponse("https://api.paystack.co/bank/resolve?account_number=0123456789&bank_code=057", HttpStatusCode.OK, """
            {"status":true,"message":"Account number resolved","data":{"account_number":"0123456789","account_name":"ACME GHANA LTD"}}
            """);
        handler.AddResponse("https://api.paystack.co/transferrecipient", HttpStatusCode.OK, """
            {"status":true,"message":"Transfer recipient created successfully","data":{"recipient_code":"RCP_paystack123"}}
            """);
        handler.AddResponse("https://api.paystack.co/transfer", HttpStatusCode.OK, """
            {"status":true,"message":"Transfer has been queued","data":{"reference":"bi-bank-ref","status":"pending","transfer_code":"TRF_paystack123"}}
            """);

        var options = Options.Create(new FintechProviderOptions
        {
            BankTransfer = new RailProviderOptions
            {
                Mode = "Live",
                BaseUrl = "https://api.paystack.co",
                ApiKey = "sk_test_123",
                ApiKeyHeaderName = "Authorization",
                ApiKeyPrefix = "Bearer",
                ProviderCode = "paystack-bank-gh",
                PayoutPath = "/transfer",
                RecipientPath = "/transferrecipient",
                ResolvePath = "/bank/resolve?account_number={accountNumber}&bank_code={bankCode}",
                SourceAccount = "balance",
                ReferencePrefix = "BI"
            }
        });

        var provider = new BankInsightBankTransferProvider(options, new TestHttpClientFactory(handler), NullLogger<BankInsightBankTransferProvider>.Instance);
        var transferId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var result = await provider.InitiatePayoutAsync(new BankPayoutInstruction(transferId, "057", "0123456789", 125.50m, "GHS", "Vendor settlement"), CancellationToken.None);

        result.Accepted.Should().BeTrue();
        result.ProviderReference.Should().Be("TRF_paystack123");
        result.RawStatus.Should().Be("pending");

        handler.Requests.Should().HaveCount(3);
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri!.ToString().Should().Be("https://api.paystack.co/bank/resolve?account_number=0123456789&bank_code=057");
        handler.Requests[0].Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Requests[0].Headers.Authorization!.Parameter.Should().Be("sk_test_123");

        handler.Requests[1].Method.Should().Be(HttpMethod.Post);
        var recipientJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        recipientJson.Should().Contain("\"type\":\"ghipss\"");
        recipientJson.Should().Contain("\"name\":\"ACME GHANA LTD\"");
        recipientJson.Should().Contain("\"currency\":\"GHS\"");

        handler.Requests[2].Method.Should().Be(HttpMethod.Post);
        var transferJson = await handler.Requests[2].Content!.ReadAsStringAsync();
        transferJson.Should().Contain("\"source\":\"balance\"");
        transferJson.Should().Contain("\"amount\":12550");
        transferJson.Should().Contain("\"recipient\":\"RCP_paystack123\"");
        transferJson.Should().Contain("\"currency\":\"GHS\"");
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public TestHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false);
        }
    }

    private sealed class RecordingMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses = new(StringComparer.Ordinal);
        public List<HttpRequestMessage> Requests { get; } = new();

        public void AddResponse(string url, HttpStatusCode statusCode, string json)
        {
            _responses[url] = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(await CloneAsync(request));

            if (_responses.TryGetValue(request.RequestUri!.ToString(), out var response))
            {
                return response;
            }

            throw new InvalidOperationException($"No response configured for {request.RequestUri}");
        }

        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var content = await request.Content.ReadAsStringAsync();
                clone.Content = new StringContent(content, Encoding.UTF8, request.Content.Headers.ContentType?.MediaType ?? "application/json");
            }

            return clone;
        }
    }
}
