namespace HybridTransfer.Infrastructure.Persistence.Entities;

public sealed class TransferOrderEntity
{
    public Guid Id { get; set; }
    public string TransferType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string FundingSource { get; set; } = string.Empty;
    public Guid? BeneficiaryId { get; set; }
    public Guid SourceWalletId { get; set; }
    public string DestinationDetails { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal? FxRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RiskStatus { get; set; } = string.Empty;
    public string ComplianceStatus { get; set; } = string.Empty;
    public string? PartnerReference { get; set; }
    public string? FailureReason { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int Version { get; set; }
}

public sealed class ApprovalRequestEntity
{
    public Guid Id { get; set; }
    public Guid TransferOrderId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class AlertEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string AlertCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ReconciliationItemEntity
{
    public Guid Id { get; set; }
    public string ReconciliationType { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string InternalReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset DetectedAtUtc { get; set; }
}

public sealed class WebhookReceiptEntity
{
    public Guid Id { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string ProviderReference { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAtUtc { get; set; }
}

public sealed class WalletEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string WalletType { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? LiabilityLedgerAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int Version { get; set; }
}

public sealed class JournalEntryEntity
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateOnly ValueDate { get; set; }
    public DateTimeOffset BookingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SourceModule { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? ReversalOfJournalEntryId { get; set; }
    public Guid? TransferOrderId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int Version { get; set; }
    public ICollection<JournalLineEntity> Lines { get; set; } = new List<JournalLineEntity>();
}

public sealed class JournalLineEntity
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid LedgerAccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? ExchangeRate { get; set; }
    public string Narrative { get; set; } = string.Empty;
    public JournalEntryEntity? JournalEntry { get; set; }
}

public sealed class AuditEventEntity
{
    public Guid Id { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceId { get; set; }
    public string? TraceId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
