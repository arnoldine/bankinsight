using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Domain.Common;
using HybridTransfer.Domain.Transfers;

namespace HybridTransfer.Application.Services;

public sealed class TransferExecutionService
{
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly IMobileMoneyProvider _mobileMoneyProvider;
    private readonly IBankTransferProvider _bankTransferProvider;

    public TransferExecutionService(
        ITransferOrderRepository transferOrderRepository,
        IMobileMoneyProvider mobileMoneyProvider,
        IBankTransferProvider bankTransferProvider)
    {
        _transferOrderRepository = transferOrderRepository;
        _mobileMoneyProvider = mobileMoneyProvider;
        _bankTransferProvider = bankTransferProvider;
    }

    public async Task<TransferResponse?> ResumeApprovedTransferAsync(Guid transferOrderId, CancellationToken cancellationToken)
    {
        var transfer = await _transferOrderRepository.GetByIdAsync(transferOrderId, cancellationToken);
        if (transfer is null)
        {
            return null;
        }

        if (transfer.Status != TransferStatus.Authorized)
        {
            return new TransferResponse(transfer.Id, transfer.Status.ToString(), transfer.RiskStatus.ToString(), transfer.ComplianceStatus.ToString(), transfer.PartnerReference);
        }

        if (transfer.Channel == TransferChannel.MobileMoney)
        {
            var parts = transfer.DestinationDetails.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException("Mobile money destination details are malformed.");
            }

            var providerResult = await _mobileMoneyProvider.InitiatePayoutAsync(
                new MobileMoneyPayoutInstruction(transfer.Id, parts[1], parts[0], transfer.Amount, "GHS", "Approved mobile money payout"),
                cancellationToken);

            transfer.Submit(providerResult.ProviderReference);
        }
        else if (transfer.Channel == TransferChannel.Bank)
        {
            var parts = transfer.DestinationDetails.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException("Bank destination details are malformed.");
            }

            var providerResult = await _bankTransferProvider.InitiatePayoutAsync(
                new BankPayoutInstruction(transfer.Id, parts[0], parts[1], transfer.Amount, "GHS", "Approved bank payout"),
                cancellationToken);

            transfer.Submit(providerResult.ProviderReference);
        }

        await _transferOrderRepository.SaveAsync(transfer, cancellationToken);
        return new TransferResponse(transfer.Id, transfer.Status.ToString(), transfer.RiskStatus.ToString(), transfer.ComplianceStatus.ToString(), transfer.PartnerReference);
    }
}
