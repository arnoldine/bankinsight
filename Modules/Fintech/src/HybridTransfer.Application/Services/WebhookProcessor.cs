using System.Text.Json;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;

namespace HybridTransfer.Application.Services;

public sealed class WebhookProcessor
{
    private readonly IWebhookSecurityService _webhookSecurityService;

    public WebhookProcessor(IWebhookSecurityService webhookSecurityService)
    {
        _webhookSecurityService = webhookSecurityService;
    }

    public WebhookProcessingResult Process(WebhookEnvelope envelope)
    {
        if (!_webhookSecurityService.VerifySignature(envelope.ProviderCode, envelope.Payload, envelope.SignatureHeader))
        {
            return new WebhookProcessingResult(false, "InvalidSignature", envelope.EventType, envelope.ExternalReference, null, null);
        }

        using var document = JsonDocument.Parse(envelope.Payload);
        var root = document.RootElement;
        var providerStatus = root.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString()
            : root.TryGetProperty("event", out var eventElement)
                ? eventElement.GetString()
                : "received";

        var providerReference = envelope.ExternalReference;
        string? failureReason = null;

        if (root.TryGetProperty("data", out var dataElement))
        {
            if (dataElement.TryGetProperty("transfer_code", out var transferCodeElement))
            {
                providerReference = transferCodeElement.GetString() ?? providerReference;
            }
            else if (dataElement.TryGetProperty("reference", out var referenceElement))
            {
                providerReference = referenceElement.GetString() ?? providerReference;
            }

            if (dataElement.TryGetProperty("status", out var nestedStatusElement))
            {
                providerStatus = nestedStatusElement.GetString() ?? providerStatus;
            }

            if (dataElement.TryGetProperty("reason", out var reasonElement))
            {
                failureReason = reasonElement.GetString();
            }
        }

        return new WebhookProcessingResult(true, "Accepted", envelope.EventType, providerReference, providerStatus, failureReason);
    }
}

public sealed record WebhookProcessingResult(bool Accepted, string Decision, string EventType, string ProviderReference, string? ProviderStatus, string? FailureReason);
