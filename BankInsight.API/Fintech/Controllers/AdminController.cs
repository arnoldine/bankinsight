using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ApprovalService _approvalService;
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly IJournalRepository _journalRepository;
    private readonly AuditTrailService _auditTrailService;
    private readonly OperationsExplorerService _operationsExplorerService;
    private readonly ComplianceExplorerService _complianceExplorerService;

    public AdminController(
        ApprovalService approvalService,
        ITransferOrderRepository transferOrderRepository,
        IJournalRepository journalRepository,
        AuditTrailService auditTrailService,
        OperationsExplorerService operationsExplorerService,
        ComplianceExplorerService complianceExplorerService)
    {
        _approvalService = approvalService;
        _transferOrderRepository = transferOrderRepository;
        _journalRepository = journalRepository;
        _auditTrailService = auditTrailService;
        _operationsExplorerService = operationsExplorerService;
        _complianceExplorerService = complianceExplorerService;
    }

    [HttpGet("approvals")]
    [ProducesResponseType(typeof(IEnumerable<ApprovalQueueItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ApprovalQueueItemResponse>>> PendingApprovals(CancellationToken cancellationToken)
    {
        var approvals = await _approvalService.GetPendingAsync(cancellationToken);
        return Ok(approvals);
    }

    [HttpGet("approvals/explorer")]
    [ProducesResponseType(typeof(PagedResponse<ApprovalExplorerItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ApprovalExplorerItemResponse>>> SearchApprovals([FromQuery] ApprovalExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var approvals = await _complianceExplorerService.SearchApprovalsAsync(request, cancellationToken);
        return Ok(approvals);
    }

    [HttpPost("approvals/{approvalRequestId:guid}/decision")]
    [ProducesResponseType(typeof(ApprovalDecisionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalDecisionResult>> Decide(Guid approvalRequestId, [FromBody] ApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        var response = await _approvalService.ApproveAsync(approvalRequestId, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("transfers")]
    [ProducesResponseType(typeof(PagedResponse<TransferExplorerItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TransferExplorerItemResponse>>> SearchTransfers([FromQuery] TransferExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await _operationsExplorerService.SearchTransfersAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("transfers/{transferOrderId:guid}")]
    [ProducesResponseType(typeof(TransferDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferDetailResponse>> GetTransfer(Guid transferOrderId, CancellationToken cancellationToken)
    {
        var transfer = await _transferOrderRepository.GetByIdAsync(transferOrderId, cancellationToken);
        if (transfer is null)
        {
            return NotFound();
        }

        return Ok(new TransferDetailResponse(transfer.Id, transfer.Type.ToString(), transfer.Channel.ToString(), transfer.Status.ToString(), transfer.RiskStatus.ToString(), transfer.ComplianceStatus.ToString(), transfer.PartnerReference, transfer.FailureReason, transfer.Amount, transfer.Fee, transfer.SourceWalletId));
    }

    [HttpGet("transfers/{transferOrderId:guid}/journals")]
    [ProducesResponseType(typeof(IEnumerable<JournalEntryDetailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<JournalEntryDetailResponse>>> GetTransferJournals(Guid transferOrderId, CancellationToken cancellationToken)
    {
        var entries = await _journalRepository.GetByTransferOrderIdAsync(transferOrderId, cancellationToken);
        return Ok(entries.Select(OperationsExplorerService.ToJournalDetail).ToArray());
    }

    [HttpGet("journals")]
    [ProducesResponseType(typeof(PagedResponse<JournalEntryExplorerItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<JournalEntryExplorerItemResponse>>> SearchJournals([FromQuery] JournalExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await _operationsExplorerService.SearchJournalsAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("journals/{journalEntryId:guid}")]
    [ProducesResponseType(typeof(JournalEntryDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JournalEntryDetailResponse>> GetJournal(Guid journalEntryId, CancellationToken cancellationToken)
    {
        var entry = await _journalRepository.GetByIdAsync(journalEntryId, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(OperationsExplorerService.ToJournalDetail(entry));
    }

    [HttpGet("audit")]
    [ProducesResponseType(typeof(PagedResponse<AuditEventExplorerItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AuditEventExplorerItemResponse>>> SearchAudit([FromQuery] AuditExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var response = await _operationsExplorerService.SearchAuditAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("audit/{entityType}/{entityId}")]
    [ProducesResponseType(typeof(IEnumerable<AuditEventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditEventResponse>>> GetAudit(string entityType, string entityId, CancellationToken cancellationToken)
    {
        var items = await _auditTrailService.GetForEntityAsync(entityType, entityId, cancellationToken);
        return Ok(items);
    }
}
