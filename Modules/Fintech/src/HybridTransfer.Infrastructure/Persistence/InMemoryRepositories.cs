using HybridTransfer.Application.Abstractions;
using HybridTransfer.Domain.Ledger;
using HybridTransfer.Domain.Transfers;
using Microsoft.EntityFrameworkCore;

namespace HybridTransfer.Infrastructure.Persistence;

public sealed class InMemoryJournalRepository : IJournalRepository
{
    private readonly Dictionary<string, JournalEntry> _entriesByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, JournalEntry> _entriesById = new();

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken)
        => Task.FromResult(_entriesByKey.ContainsKey(idempotencyKey));

    public Task<bool> ExistsReversalAsync(Guid reversedJournalEntryId, CancellationToken cancellationToken)
        => Task.FromResult(_entriesById.Values.Any(x => x.ReversalOfJournalEntryId == reversedJournalEntryId));

    public Task SaveAsync(JournalEntry entry, CancellationToken cancellationToken)
    {
        _entriesByKey[entry.IdempotencyKey] = entry;
        _entriesById[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetByIdAsync(Guid journalEntryId, CancellationToken cancellationToken)
        => Task.FromResult(_entriesById.TryGetValue(journalEntryId, out var entry) ? entry : null);

    public Task<IReadOnlyCollection<JournalEntry>> GetByTransferOrderIdAsync(Guid transferOrderId, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyCollection<JournalEntry>)_entriesById.Values.Where(x => x.TransferOrderId == transferOrderId).OrderByDescending(x => x.BookingDate).ToArray());

    public Task<PagedResult<JournalEntry>> SearchAsync(JournalSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _entriesById.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(x => string.Equals(x.Status.ToString(), criteria.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.SourceModule))
        {
            query = query.Where(x => string.Equals(x.SourceModule, criteria.SourceModule, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Reference))
        {
            query = query.Where(x => x.Reference.Contains(criteria.Reference, StringComparison.OrdinalIgnoreCase) || x.IdempotencyKey.Contains(criteria.Reference, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.TransferOrderId.HasValue)
        {
            query = query.Where(x => x.TransferOrderId == criteria.TransferOrderId.Value);
        }

        if (criteria.BookingFromUtc.HasValue)
        {
            query = query.Where(x => x.BookingDate >= criteria.BookingFromUtc.Value);
        }

        if (criteria.BookingToUtc.HasValue)
        {
            query = query.Where(x => x.BookingDate <= criteria.BookingToUtc.Value);
        }

        var totalCount = query.Count();
        var items = query.OrderByDescending(x => x.BookingDate).Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize).ToArray();
        return Task.FromResult(new PagedResult<JournalEntry>(items, criteria.Page, criteria.PageSize, totalCount));
    }
}

public sealed class InMemoryTransferOrderRepository : ITransferOrderRepository
{
    private readonly Dictionary<string, TransferOrder> _entriesByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, TransferOrder> _entriesById = new();

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken)
        => Task.FromResult(_entriesByKey.ContainsKey(idempotencyKey));

    public Task SaveAsync(TransferOrder transferOrder, CancellationToken cancellationToken)
    {
        _entriesByKey[transferOrder.IdempotencyKey] = transferOrder;
        _entriesById[transferOrder.Id] = transferOrder;
        return Task.CompletedTask;
    }

    public Task<TransferOrder?> GetByIdAsync(Guid transferOrderId, CancellationToken cancellationToken)
        => Task.FromResult(_entriesById.TryGetValue(transferOrderId, out var transfer) ? transfer : null);

    public Task<TransferOrder?> GetByPartnerReferenceAsync(string partnerReference, CancellationToken cancellationToken)
        => Task.FromResult(_entriesById.Values.FirstOrDefault(x => string.Equals(x.PartnerReference, partnerReference, StringComparison.OrdinalIgnoreCase)));

    public Task<PagedResult<TransferOrder>> SearchAsync(TransferSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _entriesById.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(x => string.Equals(x.Status.ToString(), criteria.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Channel))
        {
            query = query.Where(x => string.Equals(x.Channel.ToString(), criteria.Channel, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Reference))
        {
            query = query.Where(x => x.IdempotencyKey.Contains(criteria.Reference, StringComparison.OrdinalIgnoreCase)
                || x.DestinationDetails.Contains(criteria.Reference, StringComparison.OrdinalIgnoreCase)
                || (x.PartnerReference?.Contains(criteria.Reference, StringComparison.OrdinalIgnoreCase) ?? false)
                || x.Id.ToString().Contains(criteria.Reference, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var totalCount = query.Count();
        var items = query.OrderByDescending(x => x.CreatedAtUtc).Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize).ToArray();
        return Task.FromResult(new PagedResult<TransferOrder>(items, criteria.Page, criteria.PageSize, totalCount));
    }
}

public sealed class InMemoryWalletProjectionRepository : IWalletProjectionRepository
{
    private readonly Dictionary<Guid, WalletBalanceProjection> _balances = new();

    public InMemoryWalletProjectionRepository()
    {
        var seededWalletId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var counterpartyWalletId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var usdWalletId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        _balances[seededWalletId] = new WalletBalanceProjection(seededWalletId, 5000m, 0m, "GHS", "Active", Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"));
        _balances[counterpartyWalletId] = new WalletBalanceProjection(counterpartyWalletId, 1200m, 0m, "GHS", "Active", Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"));
        _balances[usdWalletId] = new WalletBalanceProjection(usdWalletId, 2500m, 0m, "USD", "Active", Guid.Parse("cccccccc-5555-5555-5555-555555555555"));
    }

    public Task<decimal> GetAvailableBalanceAsync(Guid walletId, CancellationToken cancellationToken)
        => Task.FromResult(_balances.TryGetValue(walletId, out var balance) ? balance.AvailableBalance : 0m);

    public Task<WalletBalanceProjection?> GetProjectionAsync(Guid walletId, CancellationToken cancellationToken)
        => Task.FromResult(_balances.TryGetValue(walletId, out var balance) ? balance : null);

    public Task SaveProjectionAsync(WalletBalanceProjection projection, CancellationToken cancellationToken)
    {
        _balances[projection.WalletId] = projection;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryTransactionManager : ITransactionManager
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        => action(cancellationToken);
}

public sealed class InMemoryAuditEventRepository : IAuditEventRepository
{
    private readonly List<AuditEventRecord> _items = new();

    public Task SaveAsync(AuditEventRecord auditEventRecord, CancellationToken cancellationToken)
    {
        _items.Add(auditEventRecord);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AuditEventRecord>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyCollection<AuditEventRecord>)_items.Where(x => x.EntityType == entityType && x.EntityId == entityId).OrderByDescending(x => x.CreatedAtUtc).ToArray());

    public Task<PagedResult<AuditEventRecord>> SearchAsync(AuditEventSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            query = query.Where(x => string.Equals(x.EntityType, criteria.EntityType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityId))
        {
            query = query.Where(x => string.Equals(x.EntityId, criteria.EntityId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action))
        {
            query = query.Where(x => x.Action.Contains(criteria.Action, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var totalCount = query.Count();
        var items = query.OrderByDescending(x => x.CreatedAtUtc).Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize).ToArray();
        return Task.FromResult(new PagedResult<AuditEventRecord>(items, criteria.Page, criteria.PageSize, totalCount));
    }
}

public sealed class InMemoryAlertRepository : IAlertRepository
{
    private readonly Dictionary<Guid, AlertRecord> _alerts = new();

    public Task SaveAsync(AlertRecord alertRecord, CancellationToken cancellationToken)
    {
        _alerts[alertRecord.Id] = alertRecord;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AlertRecord>> GetOpenAlertsAsync(CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyCollection<AlertRecord>)_alerts.Values.Where(x => x.Status == "Open").ToArray());

    public Task<PagedResult<AlertRecord>> SearchAsync(AlertSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _alerts.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(x => string.Equals(x.Status, criteria.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Severity))
        {
            query = query.Where(x => string.Equals(x.Severity, criteria.Severity, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.AlertCode))
        {
            query = query.Where(x => x.AlertCode.Contains(criteria.AlertCode, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var totalCount = query.Count();
        var items = query.OrderByDescending(x => x.CreatedAtUtc).Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize).ToArray();
        return Task.FromResult(new PagedResult<AlertRecord>(items, criteria.Page, criteria.PageSize, totalCount));
    }
}

public sealed class InMemoryApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly Dictionary<Guid, ApprovalRequest> _requests = new();

    public Task SaveAsync(ApprovalRequest approvalRequest, CancellationToken cancellationToken)
    {
        _requests[approvalRequest.Id] = approvalRequest;
        return Task.CompletedTask;
    }

    public Task<ApprovalRequest?> GetByIdAsync(Guid approvalRequestId, CancellationToken cancellationToken)
        => Task.FromResult(_requests.TryGetValue(approvalRequestId, out var request) ? request : null);

    public Task<IReadOnlyCollection<ApprovalRequest>> GetPendingAsync(CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyCollection<ApprovalRequest>)_requests.Values.Where(x => x.Status == "Pending").OrderByDescending(x => x.CreatedAtUtc).ToArray());

    public Task<PagedResult<ApprovalRequest>> SearchAsync(ApprovalSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _requests.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(x => string.Equals(x.Status, criteria.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ActionCode))
        {
            query = query.Where(x => x.ActionCode.Contains(criteria.ActionCode, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.TransferOrderId.HasValue)
        {
            query = query.Where(x => x.TransferOrderId == criteria.TransferOrderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.RequestedBy))
        {
            query = query.Where(x => x.RequestedBy.Contains(criteria.RequestedBy, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var totalCount = query.Count();
        var items = query.OrderByDescending(x => x.CreatedAtUtc).Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize).ToArray();
        return Task.FromResult(new PagedResult<ApprovalRequest>(items, criteria.Page, criteria.PageSize, totalCount));
    }
}

public sealed class InMemoryReconciliationRepository : IReconciliationRepository
{
    private readonly Dictionary<Guid, ReconciliationItem> _items = new();

    public InMemoryReconciliationRepository()
    {
        var seed = new ReconciliationItem(Guid.NewGuid(), "MobileMoneySettlement", "MOMO-SETTLEMENT-001", "TXN-BATCH-001", 2500m, "GHS", "Open", "Provider callback amount mismatch pending finance review.", DateTimeOffset.UtcNow.AddHours(-4));
        _items[seed.Id] = seed;
    }

    public Task SaveAsync(ReconciliationItem reconciliationItem, CancellationToken cancellationToken)
    {
        _items[reconciliationItem.Id] = reconciliationItem;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ReconciliationItem>> GetOpenItemsAsync(CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyCollection<ReconciliationItem>)_items.Values.Where(x => x.Status == "Open").OrderByDescending(x => x.DetectedAtUtc).ToArray());
}

public sealed class EfTransactionManager : ITransactionManager
{
    private readonly HybridTransferDbContext _dbContext;

    public EfTransactionManager(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}



public sealed class InMemoryWebhookReceiptRepository : IWebhookReceiptRepository
{
    private readonly HashSet<string> _receipts = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> ExistsAsync(string providerCode, string providerReference, string payloadHash, CancellationToken cancellationToken)
        => Task.FromResult(_receipts.Contains(Key(providerCode, providerReference, payloadHash)));

    public Task SaveAsync(WebhookReceiptRecord receipt, CancellationToken cancellationToken)
    {
        _receipts.Add(Key(receipt.ProviderCode, receipt.ProviderReference, receipt.PayloadHash));
        return Task.CompletedTask;
    }

    private static string Key(string providerCode, string providerReference, string payloadHash)
        => $"{providerCode}|{providerReference}|{payloadHash}";
}
