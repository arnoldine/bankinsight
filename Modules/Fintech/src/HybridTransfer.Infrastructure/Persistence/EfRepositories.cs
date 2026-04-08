using System.Text.Json;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Domain.Common;
using HybridTransfer.Domain.Ledger;
using HybridTransfer.Domain.Transfers;
using HybridTransfer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HybridTransfer.Infrastructure.Persistence;

public sealed class EfJournalRepository : IJournalRepository
{
    private readonly HybridTransferDbContext _dbContext;

    public EfJournalRepository(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken)
        => _dbContext.JournalEntries.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<bool> ExistsReversalAsync(Guid reversedJournalEntryId, CancellationToken cancellationToken)
        => _dbContext.JournalEntries.AnyAsync(x => x.ReversalOfJournalEntryId == reversedJournalEntryId, cancellationToken);

    public async Task SaveAsync(JournalEntry entry, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.JournalEntries.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == entry.Id, cancellationToken);
        if (entity is null)
        {
            entity = new JournalEntryEntity { Id = entry.Id, CreatedAtUtc = DateTimeOffset.UtcNow, Version = 1 };
            await _dbContext.JournalEntries.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Version += 1;
        }

        entity.Reference = entry.Reference;
        entity.ValueDate = entry.ValueDate;
        entity.BookingDate = entry.BookingDate;
        entity.Status = entry.Status.ToString();
        entity.SourceModule = entry.SourceModule;
        entity.ExternalReference = entry.ExternalReference;
        entity.IdempotencyKey = entry.IdempotencyKey;
        entity.ReversalOfJournalEntryId = entry.ReversalOfJournalEntryId;
        entity.TransferOrderId = entry.TransferOrderId;

        _dbContext.JournalLines.RemoveRange(entity.Lines);
        entity.Lines = entry.Lines.Select(line => new JournalLineEntity
        {
            Id = line.Id,
            JournalEntryId = entry.Id,
            LedgerAccountId = line.LedgerAccountId,
            Debit = line.Debit,
            Credit = line.Credit,
            Currency = line.Currency,
            ExchangeRate = line.ExchangeRate,
            Narrative = line.Narrative
        }).ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<JournalEntry?> GetByIdAsync(Guid journalEntryId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.JournalEntries.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == journalEntryId, cancellationToken);
        return entity is null ? null : Hydrate(entity);
    }

    public async Task<IReadOnlyCollection<JournalEntry>> GetByTransferOrderIdAsync(Guid transferOrderId, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.JournalEntries.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.TransferOrderId == transferOrderId)
            .OrderByDescending(x => x.BookingDate)
            .ToListAsync(cancellationToken);

        return entries.Select(Hydrate).ToArray();
    }

    public async Task<PagedResult<JournalEntry>> SearchAsync(JournalSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _dbContext.JournalEntries.AsNoTracking().Include(x => x.Lines).AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(x => x.Status == criteria.Status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.SourceModule))
        {
            query = query.Where(x => x.SourceModule == criteria.SourceModule);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Reference))
        {
            query = query.Where(x => EF.Functions.ILike(x.Reference, $"%{criteria.Reference}%") || EF.Functions.ILike(x.IdempotencyKey, $"%{criteria.Reference}%"));
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

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.BookingDate)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<JournalEntry>(items.Select(Hydrate).ToArray(), criteria.Page, criteria.PageSize, totalCount);
    }

    private static JournalEntry Hydrate(JournalEntryEntity entity)
    {
        var entry = new JournalEntry(entity.Reference, entity.ValueDate, entity.SourceModule, entity.IdempotencyKey, entity.ExternalReference, entity.ReversalOfJournalEntryId, entity.TransferOrderId);
        foreach (var line in entity.Lines.OrderBy(x => x.Id))
        {
            entry.AddLine(line.LedgerAccountId, line.Debit, line.Credit, line.Currency, line.Narrative, line.ExchangeRate);
        }

        if (entity.Status == nameof(JournalEntryStatus.Posted))
        {
            entry.Post();
        }

        return entry;
    }
}

public sealed class EfWalletProjectionRepository : IWalletProjectionRepository
{
    private readonly HybridTransferDbContext _dbContext;

    public EfWalletProjectionRepository(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetAvailableBalanceAsync(Guid walletId, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == walletId, cancellationToken);
        return wallet?.AvailableBalance ?? 0m;
    }

    public async Task<WalletBalanceProjection?> GetProjectionAsync(Guid walletId, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == walletId, cancellationToken);
        return wallet is null
            ? null
            : new WalletBalanceProjection(wallet.Id, wallet.AvailableBalance, wallet.ReservedBalance, wallet.Currency, wallet.Status, wallet.LiabilityLedgerAccountId);
    }

    public async Task SaveProjectionAsync(WalletBalanceProjection projection, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.Id == projection.WalletId, cancellationToken);
        if (entity is null)
        {
            entity = new WalletEntity
            {
                Id = projection.WalletId,
                CustomerId = Guid.Empty,
                WalletType = "Fiat",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Version = 1
            };
            await _dbContext.Wallets.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Version += 1;
        }

        entity.Currency = projection.Currency;
        entity.AvailableBalance = projection.AvailableBalance;
        entity.ReservedBalance = projection.ReservedBalance;
        entity.Status = projection.Status;
        entity.LiabilityLedgerAccountId = projection.LiabilityLedgerAccountId;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EfTransferOrderRepository : ITransferOrderRepository
{
    private readonly HybridTransferDbContext _dbContext;

    public EfTransferOrderRepository(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken)
        => _dbContext.TransferOrders.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task SaveAsync(TransferOrder transferOrder, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.TransferOrders.FirstOrDefaultAsync(x => x.Id == transferOrder.Id, cancellationToken);
        if (entity is null)
        {
            entity = new TransferOrderEntity { Id = transferOrder.Id, CreatedAtUtc = transferOrder.CreatedAtUtc, Version = 1 };
            await _dbContext.TransferOrders.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Version += 1;
        }

        entity.TransferType = transferOrder.Type.ToString();
        entity.Channel = transferOrder.Channel.ToString();
        entity.FundingSource = transferOrder.FundingSource;
        entity.BeneficiaryId = transferOrder.BeneficiaryId;
        entity.SourceWalletId = transferOrder.SourceWalletId;
        entity.DestinationDetails = transferOrder.DestinationDetails;
        entity.Currency = transferOrder.Currency;
        entity.DestinationCountryCode = transferOrder.DestinationCountryCode;
        entity.Amount = transferOrder.Amount;
        entity.Fee = transferOrder.Fee;
        entity.FxRate = transferOrder.FxRate;
        entity.Status = transferOrder.Status.ToString();
        entity.RiskStatus = transferOrder.RiskStatus.ToString();
        entity.ComplianceStatus = transferOrder.ComplianceStatus.ToString();
        entity.PartnerReference = transferOrder.PartnerReference;
        entity.FailureReason = transferOrder.FailureReason;
        entity.CreatedBy = transferOrder.CreatedBy;
        entity.ApprovedBy = transferOrder.ApprovedBy;
        entity.IdempotencyKey = transferOrder.IdempotencyKey;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TransferOrder?> GetByIdAsync(Guid transferOrderId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.TransferOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == transferOrderId, cancellationToken);
        return entity is null ? null : Hydrate(entity);
    }

    public async Task<TransferOrder?> GetByPartnerReferenceAsync(string partnerReference, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.TransferOrders.AsNoTracking().FirstOrDefaultAsync(x => x.PartnerReference == partnerReference, cancellationToken);
        return entity is null ? null : Hydrate(entity);
    }

    public async Task<PagedResult<TransferOrder>> SearchAsync(TransferSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _dbContext.TransferOrders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(x => x.Status == criteria.Status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Channel))
        {
            query = query.Where(x => x.Channel == criteria.Channel);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Reference))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.IdempotencyKey, $"%{criteria.Reference}%") ||
                EF.Functions.ILike(x.DestinationDetails, $"%{criteria.Reference}%") ||
                (x.PartnerReference != null && EF.Functions.ILike(x.PartnerReference, $"%{criteria.Reference}%")) ||
                x.Id.ToString().Contains(criteria.Reference));
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TransferOrder>(items.Select(Hydrate).ToArray(), criteria.Page, criteria.PageSize, totalCount);
    }

    private static TransferOrder Hydrate(TransferOrderEntity entity)
    {
        var transfer = new TransferOrder(
            Enum.Parse<TransferType>(entity.TransferType),
            Enum.Parse<TransferChannel>(entity.Channel),
            entity.FundingSource,
            entity.SourceWalletId,
            entity.DestinationDetails,
            entity.Currency,
            entity.DestinationCountryCode,
            entity.Amount,
            entity.CreatedBy,
            entity.IdempotencyKey,
            entity.BeneficiaryId);

        transfer.ApplyPricing(entity.Fee, entity.FxRate);
        transfer.ApplyRisk(Enum.Parse<RiskStatus>(entity.RiskStatus), Enum.Parse<ComplianceStatus>(entity.ComplianceStatus));

        if (entity.Status == nameof(TransferStatus.AwaitingApproval))
        {
            transfer.MarkAwaitingApproval();
        }
        else if (entity.Status == nameof(TransferStatus.Authorized) || entity.Status == nameof(TransferStatus.Submitted) || entity.Status == nameof(TransferStatus.PendingSettlement) || entity.Status == nameof(TransferStatus.Posted) || entity.Status == nameof(TransferStatus.Failed) || entity.Status == nameof(TransferStatus.Reversed))
        {
            transfer.Authorize(entity.ApprovedBy);
            if (entity.Status == nameof(TransferStatus.Submitted) || entity.Status == nameof(TransferStatus.PendingSettlement) || entity.Status == nameof(TransferStatus.Posted) || entity.Status == nameof(TransferStatus.Reversed))
            {
                transfer.Submit(entity.PartnerReference ?? "RESTORED-PROVIDER-REF");
            }
            if (entity.Status == nameof(TransferStatus.PendingSettlement))
            {
                transfer.MarkPendingSettlement();
            }
            if (entity.Status == nameof(TransferStatus.Failed))
            {
                transfer.SetOutcome(TransferStatus.Failed, entity.FailureReason);
            }
            if (entity.Status == nameof(TransferStatus.Posted))
            {
                transfer.MarkSettled();
            }
            if (entity.Status == nameof(TransferStatus.Reversed))
            {
                transfer.MarkReversed(entity.FailureReason);
            }
        }

        return transfer;
    }
}

public sealed class EfAlertRepository : IAlertRepository
{
    private readonly HybridTransferDbContext _dbContext;

    public EfAlertRepository(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(AlertRecord alertRecord, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Alerts.FirstOrDefaultAsync(x => x.Id == alertRecord.Id, cancellationToken);
        if (entity is null)
        {
            entity = new AlertEntity { Id = alertRecord.Id };
            await _dbContext.Alerts.AddAsync(entity, cancellationToken);
        }

        entity.CustomerId = alertRecord.CustomerId;
        entity.AlertCode = alertRecord.AlertCode;
        entity.Severity = alertRecord.Severity;
        entity.Score = alertRecord.Score;
        entity.Status = alertRecord.Status;
        entity.PayloadJson = alertRecord.PayloadJson;
        entity.CreatedAtUtc = alertRecord.CreatedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AlertRecord>> GetOpenAlertsAsync(CancellationToken cancellationToken)
        => await _dbContext.Alerts.AsNoTracking()
            .Where(x => x.Status == "Open")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AlertRecord(x.Id, x.CustomerId, x.AlertCode, x.Severity, x.Score, x.Status, x.PayloadJson, x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

    public async Task<PagedResult<AlertRecord>> SearchAsync(AlertSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _dbContext.Alerts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(x => x.Status == criteria.Status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Severity))
        {
            query = query.Where(x => x.Severity == criteria.Severity);
        }

        if (criteria.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.AlertCode))
        {
            query = query.Where(x => EF.Functions.ILike(x.AlertCode, $"%{criteria.AlertCode}%"));
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(x => new AlertRecord(x.Id, x.CustomerId, x.AlertCode, x.Severity, x.Score, x.Status, x.PayloadJson, x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AlertRecord>(items, criteria.Page, criteria.PageSize, totalCount);
    }
}

public sealed class EfApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly HybridTransferDbContext _dbContext;

    public EfApprovalRequestRepository(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(ApprovalRequest approvalRequest, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ApprovalRequests.FirstOrDefaultAsync(x => x.Id == approvalRequest.Id, cancellationToken);
        if (entity is null)
        {
            entity = new ApprovalRequestEntity { Id = approvalRequest.Id };
            await _dbContext.ApprovalRequests.AddAsync(entity, cancellationToken);
        }

        entity.TransferOrderId = approvalRequest.TransferOrderId;
        entity.ActionCode = approvalRequest.ActionCode;
        entity.Status = approvalRequest.Status;
        entity.RequestedBy = approvalRequest.RequestedBy;
        entity.ApprovedBy = approvalRequest.ApprovedBy;
        entity.Reason = approvalRequest.Reason;
        entity.CreatedAtUtc = approvalRequest.CreatedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApprovalRequest?> GetByIdAsync(Guid approvalRequestId, CancellationToken cancellationToken)
        => await _dbContext.ApprovalRequests.AsNoTracking()
            .Where(x => x.Id == approvalRequestId)
            .Select(x => new ApprovalRequest(x.Id, x.TransferOrderId, x.ActionCode, x.Status, x.RequestedBy, x.ApprovedBy, x.Reason, x.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ApprovalRequest>> GetPendingAsync(CancellationToken cancellationToken)
        => await _dbContext.ApprovalRequests.AsNoTracking()
            .Where(x => x.Status == "Pending")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ApprovalRequest(x.Id, x.TransferOrderId, x.ActionCode, x.Status, x.RequestedBy, x.ApprovedBy, x.Reason, x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

    public async Task<PagedResult<ApprovalRequest>> SearchAsync(ApprovalSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _dbContext.ApprovalRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(x => x.Status == criteria.Status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ActionCode))
        {
            query = query.Where(x => EF.Functions.ILike(x.ActionCode, $"%{criteria.ActionCode}%"));
        }

        if (criteria.TransferOrderId.HasValue)
        {
            query = query.Where(x => x.TransferOrderId == criteria.TransferOrderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.RequestedBy))
        {
            query = query.Where(x => EF.Functions.ILike(x.RequestedBy, $"%{criteria.RequestedBy}%"));
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(x => new ApprovalRequest(x.Id, x.TransferOrderId, x.ActionCode, x.Status, x.RequestedBy, x.ApprovedBy, x.Reason, x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<ApprovalRequest>(items, criteria.Page, criteria.PageSize, totalCount);
    }
}

public sealed class EfReconciliationRepository : IReconciliationRepository
{
    private readonly HybridTransferDbContext _dbContext;

    public EfReconciliationRepository(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(ReconciliationItem reconciliationItem, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ReconciliationItems.FirstOrDefaultAsync(x => x.Id == reconciliationItem.Id, cancellationToken);
        if (entity is null)
        {
            entity = new ReconciliationItemEntity { Id = reconciliationItem.Id };
            await _dbContext.ReconciliationItems.AddAsync(entity, cancellationToken);
        }

        entity.ReconciliationType = reconciliationItem.ReconciliationType;
        entity.ExternalReference = reconciliationItem.ExternalReference;
        entity.InternalReference = reconciliationItem.InternalReference;
        entity.Amount = reconciliationItem.Amount;
        entity.Currency = reconciliationItem.Currency;
        entity.Status = reconciliationItem.Status;
        entity.Notes = reconciliationItem.Notes;
        entity.DetectedAtUtc = reconciliationItem.DetectedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReconciliationItem>> GetOpenItemsAsync(CancellationToken cancellationToken)
        => await _dbContext.ReconciliationItems.AsNoTracking()
            .Where(x => x.Status == "Open")
            .OrderByDescending(x => x.DetectedAtUtc)
            .Select(x => new ReconciliationItem(x.Id, x.ReconciliationType, x.ExternalReference, x.InternalReference, x.Amount, x.Currency, x.Status, x.Notes, x.DetectedAtUtc))
            .ToArrayAsync(cancellationToken);
}

public sealed class EfAuditEventRepository : IAuditEventRepository
{
    private readonly HybridTransferDbContext _dbContext;

    public EfAuditEventRepository(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(AuditEventRecord auditEventRecord, CancellationToken cancellationToken)
    {
        var entity = new AuditEventEntity
        {
            Id = auditEventRecord.Id,
            ActorId = auditEventRecord.ActorId,
            ActorType = auditEventRecord.ActorType,
            Action = auditEventRecord.Action,
            EntityType = auditEventRecord.EntityType,
            EntityId = auditEventRecord.EntityId,
            BeforeJson = auditEventRecord.BeforeJson,
            AfterJson = auditEventRecord.AfterJson,
            IpAddress = auditEventRecord.IpAddress,
            DeviceId = auditEventRecord.DeviceId,
            TraceId = auditEventRecord.TraceId,
            CreatedAtUtc = auditEventRecord.CreatedAtUtc
        };

        await _dbContext.AuditEvents.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditEventRecord>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken)
        => await _dbContext.AuditEvents.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AuditEventRecord(x.Id, x.ActorId, x.ActorType, x.Action, x.EntityType, x.EntityId, x.BeforeJson, x.AfterJson, x.IpAddress, x.DeviceId, x.TraceId, x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

    public async Task<PagedResult<AuditEventRecord>> SearchAsync(AuditEventSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            query = query.Where(x => x.EntityType == criteria.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityId))
        {
            query = query.Where(x => x.EntityId == criteria.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action))
        {
            query = query.Where(x => EF.Functions.ILike(x.Action, $"%{criteria.Action}%"));
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(x => new AuditEventRecord(x.Id, x.ActorId, x.ActorType, x.Action, x.EntityType, x.EntityId, x.BeforeJson, x.AfterJson, x.IpAddress, x.DeviceId, x.TraceId, x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AuditEventRecord>(items, criteria.Page, criteria.PageSize, totalCount);
    }

    private static AuditEventRecord MapAuditRecord(AuditEventEntity x)
        => new(x.Id, x.ActorId, x.ActorType, x.Action, x.EntityType, x.EntityId, x.BeforeJson, x.AfterJson, x.IpAddress, x.DeviceId, x.TraceId, x.CreatedAtUtc);
}





public sealed class EfWebhookReceiptRepository : IWebhookReceiptRepository
{
    private readonly HybridTransferDbContext _dbContext;

    public EfWebhookReceiptRepository(HybridTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(string providerCode, string providerReference, string payloadHash, CancellationToken cancellationToken)
        => _dbContext.WebhookReceipts.AnyAsync(x => x.ProviderCode == providerCode && x.ProviderReference == providerReference && x.PayloadHash == payloadHash, cancellationToken);

    public async Task SaveAsync(WebhookReceiptRecord receipt, CancellationToken cancellationToken)
    {
        var entity = new WebhookReceiptEntity
        {
            Id = receipt.Id,
            ProviderCode = receipt.ProviderCode,
            ProviderReference = receipt.ProviderReference,
            PayloadHash = receipt.PayloadHash,
            EventType = receipt.EventType,
            ProcessedAtUtc = receipt.ProcessedAtUtc
        };

        await _dbContext.WebhookReceipts.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
