using HybridTransfer.Application.Abstractions;
using HybridTransfer.Domain.Common;

namespace HybridTransfer.Application.Services;

public sealed class LedgerApplicationService
{
    private readonly PostingEngine _postingEngine;
    private readonly IJournalRepository _journalRepository;
    private readonly IWalletProjectionRepository _walletProjectionRepository;
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly TransferPostingPolicyService _transferPostingPolicyService;
    private readonly AuditTrailService _auditTrailService;

    public LedgerApplicationService(
        PostingEngine postingEngine,
        IJournalRepository journalRepository,
        IWalletProjectionRepository walletProjectionRepository,
        ITransferOrderRepository transferOrderRepository,
        ITransactionManager transactionManager,
        TransferPostingPolicyService transferPostingPolicyService,
        AuditTrailService auditTrailService)
    {
        _postingEngine = postingEngine;
        _journalRepository = journalRepository;
        _walletProjectionRepository = walletProjectionRepository;
        _transferOrderRepository = transferOrderRepository;
        _transactionManager = transactionManager;
        _transferPostingPolicyService = transferPostingPolicyService;
        _auditTrailService = auditTrailService;
    }

    public Task<Guid> PostInternalTransferAsync(
        Guid sourceWalletId,
        Guid destinationWalletId,
        Guid sourceLiabilityAccountId,
        Guid destinationLiabilityAccountId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        CancellationToken cancellationToken)
        => _transactionManager.ExecuteAsync(async ct =>
        {
            if (await _journalRepository.ExistsAsync(idempotencyKey, ct))
            {
                throw new InvalidOperationException("A journal entry with the same idempotency key already exists.");
            }

            var entry = _postingEngine.CreateInternalTransfer(sourceLiabilityAccountId, destinationLiabilityAccountId, amount, currency, reference, idempotencyKey);
            await _journalRepository.SaveAsync(entry, ct);

            var sourceBalance = await _walletProjectionRepository.GetAvailableBalanceAsync(sourceWalletId, ct);
            var destinationBalance = await _walletProjectionRepository.GetAvailableBalanceAsync(destinationWalletId, ct);

            await _walletProjectionRepository.SaveProjectionAsync(new WalletBalanceProjection(sourceWalletId, sourceBalance - amount, 0m, currency, "Active", sourceLiabilityAccountId), ct);
            await _walletProjectionRepository.SaveProjectionAsync(new WalletBalanceProjection(destinationWalletId, destinationBalance + amount, 0m, currency, "Active", destinationLiabilityAccountId), ct);
            await _auditTrailService.RecordAsync("system", "Service", "Ledger.InternalTransferPosted", "JournalEntry", entry.Id.ToString(), null, new { entry.Id, sourceWalletId, destinationWalletId, amount, currency }, ct);

            return entry.Id;
        }, cancellationToken);

    public Task<Guid> PostPendingExternalPayoutAsync(
        Guid transferOrderId,
        Guid customerWalletId,
        Guid customerLiabilityAccountId,
        Guid pendingPayoutLiabilityAccountId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        CancellationToken cancellationToken)
        => _transactionManager.ExecuteAsync(async ct =>
        {
            if (await _journalRepository.ExistsAsync(idempotencyKey, ct))
            {
                throw new InvalidOperationException("A journal entry with the same idempotency key already exists.");
            }

            var transfer = await _transferOrderRepository.GetByIdAsync(transferOrderId, ct)
                ?? throw new InvalidOperationException("Transfer order was not found for pending payout posting.");
            var before = new { transfer.Id, Status = transfer.Status.ToString() };
            _transferPostingPolicyService.EnsurePendingPayoutAllowed(transfer);

            var entry = _postingEngine.CreatePendingExternalPayout(customerLiabilityAccountId, pendingPayoutLiabilityAccountId, amount, currency, reference, idempotencyKey, transferOrderId);
            await _journalRepository.SaveAsync(entry, ct);

            var customerProjection = await _walletProjectionRepository.GetProjectionAsync(customerWalletId, ct)
                ?? new WalletBalanceProjection(customerWalletId, 0m, 0m, currency, "Active", customerLiabilityAccountId);

            await _walletProjectionRepository.SaveProjectionAsync(
                customerProjection with { AvailableBalance = customerProjection.AvailableBalance - amount, Currency = currency, LiabilityLedgerAccountId = customerLiabilityAccountId },
                ct);

            transfer.MarkPendingSettlement();
            await _transferOrderRepository.SaveAsync(transfer, ct);
            await _auditTrailService.RecordAsync("system", "Service", "Ledger.PendingExternalPayoutPosted", "TransferOrder", transfer.Id.ToString(), before, new { transfer.Id, Status = transfer.Status.ToString(), JournalEntryId = entry.Id }, ct);
            await _auditTrailService.RecordAsync("system", "Service", "Ledger.JournalLinkedToTransfer", "JournalEntry", entry.Id.ToString(), null, new { entry.Id, TransferOrderId = transfer.Id }, ct);
            return entry.Id;
        }, cancellationToken);

    public Task<Guid> PostPayoutSettlementAsync(
        Guid transferOrderId,
        Guid pendingPayoutLiabilityAccountId,
        Guid settlementAssetAccountId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        CancellationToken cancellationToken)
        => _transactionManager.ExecuteAsync(async ct =>
        {
            if (await _journalRepository.ExistsAsync(idempotencyKey, ct))
            {
                throw new InvalidOperationException("A journal entry with the same idempotency key already exists.");
            }

            var transfer = await _transferOrderRepository.GetByIdAsync(transferOrderId, ct)
                ?? throw new InvalidOperationException("Transfer order was not found for settlement posting.");
            var before = new { transfer.Id, Status = transfer.Status.ToString() };
            _transferPostingPolicyService.EnsureSettlementAllowed(transfer);

            var entry = _postingEngine.CreatePayoutSettlement(pendingPayoutLiabilityAccountId, settlementAssetAccountId, amount, currency, reference, idempotencyKey, transferOrderId);
            await _journalRepository.SaveAsync(entry, ct);

            transfer.MarkSettled();
            await _transferOrderRepository.SaveAsync(transfer, ct);
            await _auditTrailService.RecordAsync("system", "Service", "Ledger.PayoutSettled", "TransferOrder", transfer.Id.ToString(), before, new { transfer.Id, Status = transfer.Status.ToString(), JournalEntryId = entry.Id }, ct);
            await _auditTrailService.RecordAsync("system", "Service", "Ledger.JournalLinkedToTransfer", "JournalEntry", entry.Id.ToString(), null, new { entry.Id, TransferOrderId = transfer.Id }, ct);
            return entry.Id;
        }, cancellationToken);

    public Task<Guid> PostPayoutReversalAsync(
        Guid transferOrderId,
        Guid customerWalletId,
        Guid pendingPayoutLiabilityAccountId,
        Guid customerLiabilityAccountId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        Guid reversedJournalEntryId,
        CancellationToken cancellationToken)
        => _transactionManager.ExecuteAsync(async ct =>
        {
            if (await _journalRepository.ExistsAsync(idempotencyKey, ct))
            {
                throw new InvalidOperationException("A journal entry with the same idempotency key already exists.");
            }

            var transfer = await _transferOrderRepository.GetByIdAsync(transferOrderId, ct)
                ?? throw new InvalidOperationException("Transfer order was not found for reversal posting.");
            var before = new { transfer.Id, Status = transfer.Status.ToString() };
            _transferPostingPolicyService.EnsureReversalAllowed(transfer);

            var reversedEntry = await _journalRepository.GetByIdAsync(reversedJournalEntryId, ct);
            if (reversedEntry is null || reversedEntry.Status != JournalEntryStatus.Posted)
            {
                throw new InvalidOperationException("Referenced journal entry must exist and be posted before reversal.");
            }

            if (await _journalRepository.ExistsReversalAsync(reversedJournalEntryId, ct))
            {
                throw new InvalidOperationException("A reversal has already been posted for the referenced journal entry.");
            }

            var entry = _postingEngine.CreateReversal(pendingPayoutLiabilityAccountId, customerLiabilityAccountId, amount, currency, reference, idempotencyKey, reversedJournalEntryId, transferOrderId);
            await _journalRepository.SaveAsync(entry, ct);

            var customerProjection = await _walletProjectionRepository.GetProjectionAsync(customerWalletId, ct)
                ?? new WalletBalanceProjection(customerWalletId, 0m, 0m, currency, "Active", customerLiabilityAccountId);

            await _walletProjectionRepository.SaveProjectionAsync(
                customerProjection with { AvailableBalance = customerProjection.AvailableBalance + amount, Currency = currency, LiabilityLedgerAccountId = customerLiabilityAccountId },
                ct);

            transfer.MarkReversed("Ledger reversal posted");
            await _transferOrderRepository.SaveAsync(transfer, ct);
            await _auditTrailService.RecordAsync("system", "Service", "Ledger.PayoutReversed", "TransferOrder", transfer.Id.ToString(), before, new { transfer.Id, Status = transfer.Status.ToString(), JournalEntryId = entry.Id }, ct);
            await _auditTrailService.RecordAsync("system", "Service", "Ledger.JournalLinkedToTransfer", "JournalEntry", entry.Id.ToString(), null, new { entry.Id, TransferOrderId = transfer.Id, ReversedJournalEntryId = reversedJournalEntryId }, ct);
            return entry.Id;
        }, cancellationToken);
}
