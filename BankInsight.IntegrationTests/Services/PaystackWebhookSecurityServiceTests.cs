using System.Security.Cryptography;
using System.Text;
using BankInsight.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace BankInsight.IntegrationTests.Services;

public class PaystackWebhookSecurityServiceTests
{
    [Fact]
    public void VerifySignature_WithValidPaystackHmac_ReturnsTrue()
    {
        const string secret = "sk_test_123";
        const string payload = "{\"event\":\"transfer.success\",\"data\":{\"transfer_code\":\"TRF_123\"}}";

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        var service = new BankInsightWebhookSecurityService(Options.Create(new FintechProviderOptions
        {
            BankTransfer = new RailProviderOptions { ApiKey = secret, ProviderCode = "paystack-bank-gh" },
            Webhook = new WebhookProviderOptions { SharedSecret = "fallback-secret", SignatureHeaderName = "x-paystack-signature" }
        }));

        service.VerifySignature("paystack-bank-gh", payload, signature).Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_WithInvalidPaystackHmac_ReturnsFalse()
    {
        const string payload = "{\"event\":\"transfer.failed\",\"data\":{\"transfer_code\":\"TRF_123\"}}";

        var service = new BankInsightWebhookSecurityService(Options.Create(new FintechProviderOptions
        {
            BankTransfer = new RailProviderOptions { ApiKey = "sk_test_123", ProviderCode = "paystack-bank-gh" },
            Webhook = new WebhookProviderOptions { SharedSecret = "fallback-secret", SignatureHeaderName = "x-paystack-signature" }
        }));

        service.VerifySignature("paystack-bank-gh", payload, "bad-signature").Should().BeFalse();
    }
}
