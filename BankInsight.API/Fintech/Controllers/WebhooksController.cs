using System.Security.Cryptography;
using System.Text;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Application.Services;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly WebhookProcessor _webhookProcessor;
    private readonly BankTransferLifecycleService _bankTransferLifecycleService;
    private readonly WebhookReceiptService _webhookReceiptService;
    private readonly AuditTrailService _auditTrailService;
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly FintechProviderOptions _providerOptions;

    public WebhooksController(WebhookProcessor webhookProcessor, BankTransferLifecycleService bankTransferLifecycleService, WebhookReceiptService webhookReceiptService, AuditTrailService auditTrailService, ITransferOrderRepository transferOrderRepository, IOptions<FintechProviderOptions> providerOptions)
    {
        _webhookProcessor = webhookProcessor;
        _bankTransferLifecycleService = bankTransferLifecycleService;
        _webhookReceiptService = webhookReceiptService;
        _auditTrailService = auditTrailService;
        _transferOrderRepository = transferOrderRepository;
        _providerOptions = providerOptions.Value;
    }

    [HttpPost("mobile-money/{providerCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MobileMoney(string providerCode, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers[_providerOptions.Webhook.SignatureHeaderName].ToString();
        var result = _webhookProcessor.Process(new WebhookEnvelope(providerCode, "mobile-money-callback", "external-ref", payload, signature));
        return result.Accepted ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("bank/{providerCode}")]
    [ProducesResponseType(typeof(TransferStatusSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Bank(string providerCode, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers[_providerOptions.Webhook.SignatureHeaderName].ToString();
        var result = _webhookProcessor.Process(new WebhookEnvelope(providerCode, "bank-transfer-callback", "external-ref", payload, signature));
        if (!result.Accepted)
        {
            return Unauthorized(result);
        }

        if (await _webhookReceiptService.HasProcessedAsync(providerCode, result.ProviderReference, payload, cancellationToken))
        {
            var transfer = await _transferOrderRepository.GetByPartnerReferenceAsync(result.ProviderReference, cancellationToken);
            await _auditTrailService.RecordAsync(
                $"webhook:{providerCode}",
                "Webhook",
                "WebhookDuplicateIgnored",
                transfer is null ? "WebhookReceipt" : "TransferOrder",
                transfer?.Id.ToString() ?? result.ProviderReference,
                null,
                new
                {
                    providerCode,
                    providerReference = result.ProviderReference,
                    eventType = result.EventType
                },
                cancellationToken);
            return Accepted(new { decision = "DuplicateIgnored", providerReference = result.ProviderReference });
        }

        var syncResult = await _bankTransferLifecycleService.ApplyBankTransferCallbackAsync(result.ProviderReference, result.ProviderStatus ?? "received", result.FailureReason, $"webhook:{providerCode}", cancellationToken);
        await _webhookReceiptService.RecordAsync(providerCode, result.ProviderReference, result.EventType, payload, cancellationToken);
        await _auditTrailService.RecordAsync(
            $"webhook:{providerCode}",
            "Webhook",
            "WebhookApplied",
            "TransferOrder",
            syncResult.TransferOrderId.ToString(),
            null,
            new
            {
                providerCode,
                providerReference = syncResult.ProviderReference,
                providerStatus = syncResult.ProviderStatus,
                transferStatus = syncResult.TransferStatus,
                syncResult.FailureReason
            },
            cancellationToken);
        return Ok(syncResult);
    }
}
