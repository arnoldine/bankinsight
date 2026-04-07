using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Domain.Ledger;

namespace HybridTransfer.Application.Services;

public sealed class OperationsExplorerService
{
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly IJournalRepository _journalRepository;
    private readonly IAuditEventRepository _auditEventRepository;

    public OperationsExplorerService(
        ITransferOrderRepository transferOrderRepository,
        IJournalRepository journalRepository,
        IAuditEventRepository auditEventRepository)
    {
        _transferOrderRepository = transferOrderRepository;
        _journalRepository = journalRepository;
        _auditEventRepository = auditEventRepository;
    }

    public async Task<PagedResponse<TransferExplorerItemResponse>> SearchTransfersAsync(TransferExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var criteria = new TransferSearchCriteria(
            request.Status,
            request.Channel,
            request.Reference,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            NormalizePage(request.Page),
            NormalizePageSize(request.PageSize));

        var result = await _transferOrderRepository.SearchAsync(criteria, cancellationToken);
        var items = result.Items
            .Select(transfer => new TransferExplorerItemResponse(
                transfer.Id,
                transfer.Type.ToString(),
                transfer.Channel.ToString(),
                transfer.Status.ToString(),
                transfer.RiskStatus.ToString(),
                transfer.ComplianceStatus.ToString(),
                transfer.PartnerReference,
                transfer.Amount,
                transfer.CreatedBy,
                transfer.CreatedAtUtc))
            .ToArray();

        return new PagedResponse<TransferExplorerItemResponse>(result.Page, result.PageSize, result.TotalCount, items);
    }

    public async Task<PagedResponse<JournalEntryExplorerItemResponse>> SearchJournalsAsync(JournalExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var criteria = new JournalSearchCriteria(
            request.Status,
            request.SourceModule,
            request.Reference,
            request.TransferOrderId,
            request.BookingFromUtc,
            request.BookingToUtc,
            NormalizePage(request.Page),
            NormalizePageSize(request.PageSize));

        var result = await _journalRepository.SearchAsync(criteria, cancellationToken);
        var items = result.Items
            .Select(entry => new JournalEntryExplorerItemResponse(
                entry.Id,
                entry.Reference,
                entry.Status.ToString(),
                entry.SourceModule,
                entry.TransferOrderId,
                entry.ReversalOfJournalEntryId,
                entry.Lines.Sum(x => x.Debit),
                entry.Lines.Sum(x => x.Credit),
                entry.Lines.Count,
                entry.BookingDate))
            .ToArray();

        return new PagedResponse<JournalEntryExplorerItemResponse>(result.Page, result.PageSize, result.TotalCount, items);
    }

    public async Task<PagedResponse<AuditEventExplorerItemResponse>> SearchAuditAsync(AuditExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var criteria = new AuditEventSearchCriteria(
            request.EntityType,
            request.EntityId,
            request.Action,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            NormalizePage(request.Page),
            NormalizePageSize(request.PageSize));

        var result = await _auditEventRepository.SearchAsync(criteria, cancellationToken);
        var items = result.Items
            .Select(item => new AuditEventExplorerItemResponse(
                item.Id,
                item.Action,
                item.EntityType,
                item.EntityId,
                item.ActorId,
                item.ActorType,
                item.CreatedAtUtc))
            .ToArray();

        return new PagedResponse<AuditEventExplorerItemResponse>(result.Page, result.PageSize, result.TotalCount, items);
    }

    public static JournalEntryDetailResponse ToJournalDetail(JournalEntry entry)
        => new(
            entry.Id,
            entry.Reference,
            entry.Status.ToString(),
            entry.SourceModule,
            entry.IdempotencyKey,
            entry.TransferOrderId,
            entry.ReversalOfJournalEntryId,
            entry.Lines.Select(line => new JournalLineResponse(line.LedgerAccountId, line.Debit, line.Credit, line.Currency, line.Narrative)).ToArray());

    private static int NormalizePage(int page) => page <= 0 ? 1 : page;

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        <= 0 => 25,
        > 100 => 100,
        _ => pageSize
    };
}
