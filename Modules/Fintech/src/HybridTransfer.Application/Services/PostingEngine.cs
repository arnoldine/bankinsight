using HybridTransfer.Domain.Common;
using HybridTransfer.Domain.Ledger;

namespace HybridTransfer.Application.Services;

public sealed class PostingEngine
{
    public JournalEntry CreateInternalTransfer(
        Guid sourceLiabilityAccountId,
        Guid destinationLiabilityAccountId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        Guid? transferOrderId = null)
    {
        var entry = new JournalEntry(reference, DateOnly.FromDateTime(DateTime.UtcNow), "Transfers", idempotencyKey, transferOrderId: transferOrderId);
        entry.AddLine(sourceLiabilityAccountId, amount, 0m, currency, "Debit source customer wallet liability");
        entry.AddLine(destinationLiabilityAccountId, 0m, amount, currency, "Credit destination customer wallet liability");
        entry.Post();
        return entry;
    }

    public JournalEntry CreatePendingExternalPayout(
        Guid customerLiabilityAccountId,
        Guid pendingPayoutLiabilityAccountId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        Guid? transferOrderId = null)
    {
        var entry = new JournalEntry(reference, DateOnly.FromDateTime(DateTime.UtcNow), "Transfers", idempotencyKey, transferOrderId: transferOrderId);
        entry.AddLine(customerLiabilityAccountId, amount, 0m, currency, "Reduce customer wallet liability for payout");
        entry.AddLine(pendingPayoutLiabilityAccountId, 0m, amount, currency, "Move funds to pending payouts");
        entry.Post();
        return entry;
    }

    public JournalEntry CreatePayoutSettlement(
        Guid pendingPayoutLiabilityAccountId,
        Guid settlementAssetAccountId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        Guid? transferOrderId = null)
    {
        var entry = new JournalEntry(reference, DateOnly.FromDateTime(DateTime.UtcNow), "Settlement", idempotencyKey, transferOrderId: transferOrderId);
        entry.AddLine(pendingPayoutLiabilityAccountId, amount, 0m, currency, "Release pending payout liability");
        entry.AddLine(settlementAssetAccountId, 0m, amount, currency, "Reduce partner settlement asset");
        entry.Post();
        return entry;
    }

    public JournalEntry CreateReversal(
        Guid pendingPayoutLiabilityAccountId,
        Guid customerLiabilityAccountId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        Guid reversedJournalEntryId,
        Guid? transferOrderId = null)
    {
        var entry = new JournalEntry(reference, DateOnly.FromDateTime(DateTime.UtcNow), "Reversal", idempotencyKey, reversalOfJournalEntryId: reversedJournalEntryId, transferOrderId: transferOrderId);
        entry.AddLine(pendingPayoutLiabilityAccountId, amount, 0m, currency, "Reverse pending payout liability");
        entry.AddLine(customerLiabilityAccountId, 0m, amount, currency, "Restore customer wallet liability");
        entry.Post();
        return entry;
    }
}
