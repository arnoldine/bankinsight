using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.Services;
using HybridTransfer.Domain.Common;

namespace HybridTransfer.Application.Services;

public sealed class ProviderTransferStatusService
{
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly IProviderTransferStatusProvider _providerTransferStatusProvider;
    private readonly AuditTrailService _auditTrailService;

    public ProviderTransferStatusService(
        ITransferOrderRepository transferOrderRepository,
        IProviderTransferStatusProvider providerTransferStatusProvider,
        AuditTrailService auditTrailService)
    {
        _transferOrderRepository = transferOrderRepository;
        _providerTransferStatusProvider = providerTransferStatusProvider;
        _auditTrailService = auditTrailService;
    }

    public async Task<TransferStatusSyncResult> VerifyBankTransferAsync(string providerReference, string actorId, CancellationToken cancellationToken)
    {
        var transfer = await _transferOrderRepository.GetByPartnerReferenceAsync(providerReference, cancellationToken)
            ?? throw new InvalidOperationException($"Transfer with provider reference '{providerReference}' was not found.");

        var before = new { transfer.Id, Status = transfer.Status.ToString(), transfer.PartnerReference, transfer.FailureReason };
        var providerStatus = await _providerTransferStatusProvider.GetBankTransferStatusAsync(providerReference, cancellationToken);
        if (!providerStatus.Found)
        {
            throw new InvalidOperationException($"Provider status for '{providerReference}' was not found.");
        }

        ApplyOutcome(transfer, providerStatus.RawStatus, providerStatus.FailureReason);
        await _transferOrderRepository.SaveAsync(transfer, cancellationToken);
        await _auditTrailService.RecordAsync(actorId, "Service", "Transfer.ProviderStatusSynced", "TransferOrder", transfer.Id.ToString(), before, new
        {
            transfer.Id,
            Status = transfer.Status.ToString(),
            transfer.PartnerReference,
            transfer.FailureReason,
            ProviderStatus = providerStatus.RawStatus
        }, cancellationToken);

        return new TransferStatusSyncResult(transfer.Id, providerStatus.ProviderReference, transfer.Status.ToString(), providerStatus.RawStatus, transfer.FailureReason);
    }

    public async Task<TransferStatusSyncResult> ApplyBankTransferCallbackAsync(string providerReference, string providerStatus, string? failureReason, string actorId, CancellationToken cancellationToken)
    {
        var transfer = await _transferOrderRepository.GetByPartnerReferenceAsync(providerReference, cancellationToken)
            ?? throw new InvalidOperationException($"Transfer with provider reference '{providerReference}' was not found.");

        var before = new { transfer.Id, Status = transfer.Status.ToString(), transfer.PartnerReference, transfer.FailureReason };
        ApplyOutcome(transfer, providerStatus, failureReason);
        await _transferOrderRepository.SaveAsync(transfer, cancellationToken);
        await _auditTrailService.RecordAsync(actorId, "Service", "Transfer.ProviderCallbackApplied", "TransferOrder", transfer.Id.ToString(), before, new
        {
            transfer.Id,
            Status = transfer.Status.ToString(),
            transfer.PartnerReference,
            transfer.FailureReason,
            ProviderStatus = providerStatus
        }, cancellationToken);

        return new TransferStatusSyncResult(transfer.Id, providerReference, transfer.Status.ToString(), providerStatus, transfer.FailureReason);
    }

    private static void ApplyOutcome(HybridTransfer.Domain.Transfers.TransferOrder transfer, string providerStatus, string? failureReason)
    {
        var normalized = providerStatus.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "success":
            case "successful":
                transfer.MarkPendingSettlement();
                break;
            case "failed":
            case "reversed":
            case "abandoned":
                transfer.SetOutcome(TransferStatus.Failed, failureReason ?? providerStatus);
                break;
            default:
                transfer.Submit(transfer.PartnerReference ?? providerStatus);
                break;
        }
    }
}

public sealed record TransferStatusSyncResult(Guid TransferOrderId, string ProviderReference, string TransferStatus, string ProviderStatus, string? FailureReason);
