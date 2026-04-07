using HybridTransfer.Application.Abstractions;
using HybridTransfer.Domain.Common;
using HybridTransfer.Domain.Transfers;

namespace HybridTransfer.Application.Services;

public sealed class TransferPostingPolicyService
{
    public void EnsurePendingPayoutAllowed(TransferOrder transfer)
    {
        if (transfer.Status != TransferStatus.Submitted)
        {
            throw new InvalidOperationException($"Transfer {transfer.Id} is in status '{transfer.Status}' and cannot move to pending payout accounting.");
        }
    }

    public void EnsureSettlementAllowed(TransferOrder transfer)
    {
        if (transfer.Status is not (TransferStatus.Submitted or TransferStatus.PendingSettlement))
        {
            throw new InvalidOperationException($"Transfer {transfer.Id} is in status '{transfer.Status}' and cannot be settled.");
        }
    }

    public void EnsureReversalAllowed(TransferOrder transfer)
    {
        if (transfer.Status is not (TransferStatus.Submitted or TransferStatus.PendingSettlement or TransferStatus.Failed))
        {
            throw new InvalidOperationException($"Transfer {transfer.Id} is in status '{transfer.Status}' and cannot be reversed.");
        }

        if (transfer.Status == TransferStatus.Reversed)
        {
            throw new InvalidOperationException($"Transfer {transfer.Id} has already been reversed.");
        }
    }
}
