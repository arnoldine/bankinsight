using System.Security.Cryptography;
using System.Text;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;

namespace HybridTransfer.Application.Services;

public sealed class ReconciliationService
{
    private readonly IReconciliationRepository _reconciliationRepository;

    public ReconciliationService(IReconciliationRepository reconciliationRepository)
    {
        _reconciliationRepository = reconciliationRepository;
    }

    public async Task<ReconciliationItemResponse> RegisterBreakAsync(ManualReconciliationRequest request, CancellationToken cancellationToken)
    {
        var item = new ReconciliationItem(
            Guid.NewGuid(),
            request.ReconciliationType,
            request.ExternalReference,
            request.InternalReference,
            request.Amount,
            request.Currency,
            "Open",
            request.Notes,
            DateTimeOffset.UtcNow);

        await _reconciliationRepository.SaveAsync(item, cancellationToken);
        return new ReconciliationItemResponse(item.Id, item.ReconciliationType, item.ExternalReference, item.InternalReference, item.Amount, item.Currency, item.Status, item.Notes);
    }

    public Task<ReconciliationItemResponse> RegisterSystemBreakAsync(string reconciliationType, string externalReference, string internalReference, decimal amount, string currency, string notes, CancellationToken cancellationToken)
        => RegisterBreakAsync(new ManualReconciliationRequest(reconciliationType, externalReference, internalReference, amount, currency, notes), cancellationToken);

    public async Task<IReadOnlyCollection<ReconciliationItemResponse>> GetOpenItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _reconciliationRepository.GetOpenItemsAsync(cancellationToken);
        return items.Select(x => new ReconciliationItemResponse(x.Id, x.ReconciliationType, x.ExternalReference, x.InternalReference, x.Amount, x.Currency, x.Status, x.Notes)).ToArray();
    }
}

public sealed class WebhookReceiptService
{
    private readonly IWebhookReceiptRepository _webhookReceiptRepository;

    public WebhookReceiptService(IWebhookReceiptRepository webhookReceiptRepository)
    {
        _webhookReceiptRepository = webhookReceiptRepository;
    }

    public async Task<bool> HasProcessedAsync(string providerCode, string providerReference, string payload, CancellationToken cancellationToken)
    {
        var payloadHash = ComputeHash(payload);
        return await _webhookReceiptRepository.ExistsAsync(providerCode, providerReference, payloadHash, cancellationToken);
    }

    public Task RecordAsync(string providerCode, string providerReference, string eventType, string payload, CancellationToken cancellationToken)
    {
        var receipt = new WebhookReceiptRecord(Guid.NewGuid(), providerCode, providerReference, ComputeHash(payload), eventType, DateTimeOffset.UtcNow);
        return _webhookReceiptRepository.SaveAsync(receipt, cancellationToken);
    }

    private static string ComputeHash(string payload)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
