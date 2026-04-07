using HybridTransfer.Domain.Common;

namespace HybridTransfer.Domain.Ledger;

public sealed class LedgerAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string AccountCode { get; init; }
    public string AccountName { get; init; }
    public LedgerAccountCategory Category { get; init; }
    public string Currency { get; init; }
    public Guid? ParentAccountId { get; init; }
    public bool IsControlAccount { get; init; }

    public LedgerAccount(string accountCode, string accountName, LedgerAccountCategory category, string currency, bool isControlAccount = false)
    {
        AccountCode = accountCode;
        AccountName = accountName;
        Category = category;
        Currency = currency;
        IsControlAccount = isControlAccount;
    }
}

public sealed class JournalEntry
{
    private readonly List<JournalLine> _lines = new();

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Reference { get; init; }
    public DateOnly ValueDate { get; init; }
    public DateTimeOffset BookingDate { get; init; }
    public JournalEntryStatus Status { get; private set; }
    public string SourceModule { get; init; }
    public string? ExternalReference { get; init; }
    public string IdempotencyKey { get; init; }
    public Guid? ReversalOfJournalEntryId { get; init; }
    public Guid? TransferOrderId { get; init; }
    public IReadOnlyCollection<JournalLine> Lines => _lines;

    public JournalEntry(string reference, DateOnly valueDate, string sourceModule, string idempotencyKey, string? externalReference = null, Guid? reversalOfJournalEntryId = null, Guid? transferOrderId = null)
    {
        Reference = reference;
        ValueDate = valueDate;
        BookingDate = DateTimeOffset.UtcNow;
        SourceModule = sourceModule;
        IdempotencyKey = idempotencyKey;
        ExternalReference = externalReference;
        ReversalOfJournalEntryId = reversalOfJournalEntryId;
        TransferOrderId = transferOrderId;
        Status = JournalEntryStatus.Pending;
    }

    public void AddLine(Guid ledgerAccountId, decimal debit, decimal credit, string currency, string narrative, decimal? exchangeRate = null)
    {
        if (debit < 0 || credit < 0 || (debit == 0 && credit == 0) || (debit > 0 && credit > 0))
        {
            throw new InvalidOperationException("Journal line must contain either a debit or a credit.");
        }

        _lines.Add(new JournalLine(Guid.NewGuid(), Id, ledgerAccountId, debit, credit, currency, narrative, exchangeRate));
    }

    public void Post()
    {
        var totalDebit = _lines.Sum(x => x.Debit);
        var totalCredit = _lines.Sum(x => x.Credit);
        if (totalDebit != totalCredit)
        {
            throw new InvalidOperationException("Journal entry is not balanced.");
        }

        Status = JournalEntryStatus.Posted;
    }
}

public sealed record JournalLine(Guid Id, Guid JournalEntryId, Guid LedgerAccountId, decimal Debit, decimal Credit, string Currency, string Narrative, decimal? ExchangeRate);
