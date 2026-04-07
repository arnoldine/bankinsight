using HybridTransfer.Api.Contracts;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Application.Services;
using HybridTransfer.Domain.Common;
using Microsoft.Extensions.Options;

namespace BankInsight.API.Services;

public sealed class FintechLedgerOptions
{
    public bool EnableAutoPostPendingExternalPayout { get; set; } = true;
    public bool EnableAutoPostBankSettlement { get; set; } = true;
    public bool EnableAutoPostBankReversal { get; set; } = true;
    public Guid PendingPayoutLiabilityAccountId { get; set; } = Guid.Parse("cccccccc-3333-3333-3333-333333333333");
    public Guid BankSettlementAssetAccountId { get; set; } = Guid.Parse("dddddddd-4444-4444-4444-444444444444");
}

public sealed class BankTransferLifecycleService
{
    private readonly PayoutOrchestrator _payoutOrchestrator;
    private readonly ProviderTransferStatusService _providerTransferStatusService;
    private readonly LedgerApplicationService _ledgerApplicationService;
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly IWalletProjectionRepository _walletProjectionRepository;
    private readonly IJournalRepository _journalRepository;
    private readonly ReconciliationService _reconciliationService;
    private readonly AuditTrailService _auditTrailService;
    private readonly FintechLedgerOptions _options;

    public BankTransferLifecycleService(
        PayoutOrchestrator payoutOrchestrator,
        ProviderTransferStatusService providerTransferStatusService,
        LedgerApplicationService ledgerApplicationService,
        ITransferOrderRepository transferOrderRepository,
        IWalletProjectionRepository walletProjectionRepository,
        IJournalRepository journalRepository,
        ReconciliationService reconciliationService,
        AuditTrailService auditTrailService,
        IOptions<FintechLedgerOptions> options)
    {
        _payoutOrchestrator = payoutOrchestrator;
        _providerTransferStatusService = providerTransferStatusService;
        _ledgerApplicationService = ledgerApplicationService;
        _transferOrderRepository = transferOrderRepository;
        _walletProjectionRepository = walletProjectionRepository;
        _journalRepository = journalRepository;
        _reconciliationService = reconciliationService;
        _auditTrailService = auditTrailService;
        _options = options.Value;
    }

    public async Task<TransferResponse> SubmitBankPayoutAsync(BankTransferRequest request, string actor, string idempotencyKey, CancellationToken cancellationToken)
    {
        var response = await _payoutOrchestrator.CreateBankPayoutAsync(
            new BankPayoutInstruction(Guid.NewGuid(), request.BankCode, request.AccountNumber, request.Amount, request.Currency, request.Narrative ?? "HybridTransfer bank payout"),
            request.SourceWalletId,
            actor,
            idempotencyKey,
            cancellationToken);

        if (!_options.EnableAutoPostPendingExternalPayout || !string.Equals(response.Status, TransferStatus.Submitted.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return response;
        }

        var transfer = await _transferOrderRepository.GetByIdAsync(response.TransferId, cancellationToken)
            ?? throw new InvalidOperationException("Transfer order was not found after submission.");
        var wallet = await _walletProjectionRepository.GetProjectionAsync(transfer.SourceWalletId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet projection was not found for pending payout posting.");
        if (!wallet.LiabilityLedgerAccountId.HasValue)
        {
            throw new InvalidOperationException("Wallet liability ledger account is required for pending payout posting.");
        }

        await _ledgerApplicationService.PostPendingExternalPayoutAsync(
            transfer.Id,
            transfer.SourceWalletId,
            wallet.LiabilityLedgerAccountId.Value,
            _options.PendingPayoutLiabilityAccountId,
            transfer.Amount,
            wallet.Currency,
            $"BANK-PENDING-{transfer.Id:N}",
            $"ledger-pending:{idempotencyKey}",
            cancellationToken);

        var updated = await _transferOrderRepository.GetByIdAsync(transfer.Id, cancellationToken) ?? transfer;
        return new TransferResponse(updated.Id, updated.Status.ToString(), updated.RiskStatus.ToString(), updated.ComplianceStatus.ToString(), updated.PartnerReference);
    }

    public async Task<TransferStatusSyncResult> VerifyBankTransferAsync(string providerReference, string actorId, CancellationToken cancellationToken)
    {
        var sync = await _providerTransferStatusService.VerifyBankTransferAsync(providerReference, actorId, cancellationToken);
        await ApplyAccountingAsync(sync.TransferOrderId, sync.ProviderReference, sync.ProviderStatus, actorId, cancellationToken);
        var updated = await _transferOrderRepository.GetByIdAsync(sync.TransferOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Transfer order was not found after provider status sync.");
        return new TransferStatusSyncResult(updated.Id, sync.ProviderReference, updated.Status.ToString(), sync.ProviderStatus, updated.FailureReason);
    }

    public async Task<TransferStatusSyncResult> ApplyBankTransferCallbackAsync(string providerReference, string providerStatus, string? failureReason, string actorId, CancellationToken cancellationToken)
    {
        var sync = await _providerTransferStatusService.ApplyBankTransferCallbackAsync(providerReference, providerStatus, failureReason, actorId, cancellationToken);
        await ApplyAccountingAsync(sync.TransferOrderId, sync.ProviderReference, sync.ProviderStatus, actorId, cancellationToken);
        var updated = await _transferOrderRepository.GetByIdAsync(sync.TransferOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Transfer order was not found after provider callback sync.");
        return new TransferStatusSyncResult(updated.Id, sync.ProviderReference, updated.Status.ToString(), sync.ProviderStatus, updated.FailureReason);
    }

    private async Task ApplyAccountingAsync(Guid transferOrderId, string providerReference, string providerStatus, string actorId, CancellationToken cancellationToken)
    {
        var transfer = await _transferOrderRepository.GetByIdAsync(transferOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Transfer order was not found for accounting sync.");
        var normalized = providerStatus.Trim().ToLowerInvariant();

        if ((normalized is "success" or "successful") && _options.EnableAutoPostBankSettlement && transfer.Status == TransferStatus.PendingSettlement)
        {
            var walletForSettlement = await _walletProjectionRepository.GetProjectionAsync(transfer.SourceWalletId, cancellationToken)
                ?? throw new InvalidOperationException("Wallet projection was not found for payout settlement.");
            await _ledgerApplicationService.PostPayoutSettlementAsync(
                transfer.Id,
                _options.PendingPayoutLiabilityAccountId,
                _options.BankSettlementAssetAccountId,
                transfer.Amount,
                walletForSettlement.Currency,
                $"BANK-SETTLEMENT-{transfer.Id:N}",
                $"ledger-settlement:{providerReference}",
                cancellationToken);

            var afterSettlement = await _transferOrderRepository.GetByIdAsync(transfer.Id, cancellationToken) ?? transfer;
            if (afterSettlement.Status != TransferStatus.Posted)
            {
                await RegisterDivergenceAsync(afterSettlement, providerReference, providerStatus, "Provider reported success but ledger settlement did not finalize the transfer.", cancellationToken);
            }
            return;
        }

        if ((normalized is "failed" or "reversed" or "abandoned") && _options.EnableAutoPostBankReversal)
        {
            var pendingEntry = (await _journalRepository.GetByTransferOrderIdAsync(transfer.Id, cancellationToken))
                .Where(x => x.SourceModule == "Transfers" && x.ReversalOfJournalEntryId is null)
                .OrderByDescending(x => x.BookingDate)
                .FirstOrDefault();

            if (pendingEntry is null)
            {
                await RegisterDivergenceAsync(transfer, providerReference, providerStatus, "Provider reported failure but no pending payout journal was found to reverse.", cancellationToken);
                return;
            }

            if (await _journalRepository.ExistsReversalAsync(pendingEntry.Id, cancellationToken))
            {
                return;
            }

            var wallet = await _walletProjectionRepository.GetProjectionAsync(transfer.SourceWalletId, cancellationToken)
                ?? throw new InvalidOperationException("Wallet projection was not found for payout reversal.");
            if (!wallet.LiabilityLedgerAccountId.HasValue)
            {
                throw new InvalidOperationException("Wallet liability ledger account is required for payout reversal.");
            }

            await _ledgerApplicationService.PostPayoutReversalAsync(
                transfer.Id,
                transfer.SourceWalletId,
                _options.PendingPayoutLiabilityAccountId,
                wallet.LiabilityLedgerAccountId.Value,
                transfer.Amount,
                wallet.Currency,
                $"BANK-REVERSAL-{transfer.Id:N}",
                $"ledger-reversal:{providerReference}",
                pendingEntry.Id,
                cancellationToken);
            return;
        }

        if ((normalized is "success" or "successful") && transfer.Status != TransferStatus.Posted)
        {
            await RegisterDivergenceAsync(transfer, providerReference, providerStatus, "Provider reported success but transfer is not posted in the ledger.", cancellationToken);
        }
    }

    private async Task RegisterDivergenceAsync(HybridTransfer.Domain.Transfers.TransferOrder transfer, string providerReference, string providerStatus, string notes, CancellationToken cancellationToken)
    {
        var wallet = await _walletProjectionRepository.GetProjectionAsync(transfer.SourceWalletId, cancellationToken);
        var currency = wallet?.Currency ?? "GHS";
        var detail = $"{notes} Current transfer status: {transfer.Status}. Provider status: {providerStatus}.";

        await _reconciliationService.RegisterSystemBreakAsync(
            "ProviderLedgerDivergence",
            providerReference,
            transfer.Id.ToString(),
            transfer.Amount,
            currency,
            detail,
            cancellationToken);

        await _auditTrailService.RecordAsync(
            "provider-sync",
            "System",
            "ProviderLedgerDivergenceDetected",
            "TransferOrder",
            transfer.Id.ToString(),
            null,
            new
            {
                providerReference,
                providerStatus,
                transferStatus = transfer.Status.ToString(),
                notes = detail
            },
            cancellationToken);
    }
}
