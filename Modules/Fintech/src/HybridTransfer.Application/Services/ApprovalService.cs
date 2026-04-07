using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Domain.Common;
using HybridTransfer.Domain.Transfers;

namespace HybridTransfer.Application.Services;

public sealed class ApprovalService
{
    private readonly IApprovalRequestRepository _approvalRequestRepository;
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly TransferExecutionService _transferExecutionService;

    public ApprovalService(
        IApprovalRequestRepository approvalRequestRepository,
        ITransferOrderRepository transferOrderRepository,
        TransferExecutionService transferExecutionService)
    {
        _approvalRequestRepository = approvalRequestRepository;
        _transferOrderRepository = transferOrderRepository;
        _transferExecutionService = transferExecutionService;
    }

    public async Task<ApprovalRequest> CreateApprovalRequestAsync(Guid transferOrderId, string actionCode, string requestedBy, string reason, CancellationToken cancellationToken)
    {
        var request = new ApprovalRequest(Guid.NewGuid(), transferOrderId, actionCode, "Pending", requestedBy, null, reason, DateTimeOffset.UtcNow);
        await _approvalRequestRepository.SaveAsync(request, cancellationToken);
        return request;
    }

    public async Task<ApprovalDecisionResult?> ApproveAsync(Guid approvalRequestId, ApprovalDecisionRequest decision, CancellationToken cancellationToken)
    {
        var approvalRequest = await _approvalRequestRepository.GetByIdAsync(approvalRequestId, cancellationToken);
        if (approvalRequest is null)
        {
            return null;
        }

        var updated = approvalRequest with
        {
            Status = string.Equals(decision.Decision, "approve", StringComparison.OrdinalIgnoreCase) ? "Approved" : "Rejected",
            ApprovedBy = decision.ApprovedBy
        };

        await _approvalRequestRepository.SaveAsync(updated, cancellationToken);

        TransferResponse? transferResponse = null;
        var transfer = await _transferOrderRepository.GetByIdAsync(updated.TransferOrderId, cancellationToken);
        if (transfer is not null)
        {
            if (updated.Status == "Approved")
            {
                transfer.Authorize(decision.ApprovedBy);
                await _transferOrderRepository.SaveAsync(transfer, cancellationToken);
                transferResponse = await _transferExecutionService.ResumeApprovedTransferAsync(transfer.Id, cancellationToken);
            }
            else
            {
                transfer.SetOutcome(TransferStatus.Failed, decision.DecisionNotes);
                await _transferOrderRepository.SaveAsync(transfer, cancellationToken);
                transferResponse = new TransferResponse(transfer.Id, transfer.Status.ToString(), transfer.RiskStatus.ToString(), transfer.ComplianceStatus.ToString(), transfer.PartnerReference);
            }
        }

        var queueItem = new ApprovalQueueItemResponse(updated.Id, updated.TransferOrderId, updated.ActionCode, updated.Status, updated.RequestedBy, updated.Reason, updated.CreatedAtUtc);
        return new ApprovalDecisionResult(queueItem, transferResponse);
    }

    public async Task<IReadOnlyCollection<ApprovalQueueItemResponse>> GetPendingAsync(CancellationToken cancellationToken)
    {
        var items = await _approvalRequestRepository.GetPendingAsync(cancellationToken);
        return items.Select(x => new ApprovalQueueItemResponse(x.Id, x.TransferOrderId, x.ActionCode, x.Status, x.RequestedBy, x.Reason, x.CreatedAtUtc)).ToArray();
    }
}

public sealed record ApprovalDecisionResult(ApprovalQueueItemResponse Approval, TransferResponse? Transfer);
