using HybridTransfer.Domain.Ledger;
using HybridTransfer.Domain.Transfers;

namespace HybridTransfer.Application.Abstractions;

public interface IJournalRepository
{
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task SaveAsync(JournalEntry entry, CancellationToken cancellationToken);
    Task<JournalEntry?> GetByIdAsync(Guid journalEntryId, CancellationToken cancellationToken);
    Task<bool> ExistsReversalAsync(Guid reversedJournalEntryId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JournalEntry>> GetByTransferOrderIdAsync(Guid transferOrderId, CancellationToken cancellationToken);
    Task<PagedResult<JournalEntry>> SearchAsync(JournalSearchCriteria criteria, CancellationToken cancellationToken);
}

public interface ITransferOrderRepository
{
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task SaveAsync(TransferOrder transferOrder, CancellationToken cancellationToken);
    Task<TransferOrder?> GetByIdAsync(Guid transferOrderId, CancellationToken cancellationToken);
    Task<TransferOrder?> GetByPartnerReferenceAsync(string partnerReference, CancellationToken cancellationToken);
    Task<PagedResult<TransferOrder>> SearchAsync(TransferSearchCriteria criteria, CancellationToken cancellationToken);
}

public interface IWalletProjectionRepository
{
    Task<decimal> GetAvailableBalanceAsync(Guid walletId, CancellationToken cancellationToken);
    Task<WalletBalanceProjection?> GetProjectionAsync(Guid walletId, CancellationToken cancellationToken);
    Task SaveProjectionAsync(WalletBalanceProjection projection, CancellationToken cancellationToken);
}

public interface ITransactionManager
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}

public interface IAuditEventRepository
{
    Task SaveAsync(AuditEventRecord auditEventRecord, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditEventRecord>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken);
    Task<PagedResult<AuditEventRecord>> SearchAsync(AuditEventSearchCriteria criteria, CancellationToken cancellationToken);
}

public interface IWebhookReceiptRepository
{
    Task<bool> ExistsAsync(string providerCode, string providerReference, string payloadHash, CancellationToken cancellationToken);
    Task SaveAsync(WebhookReceiptRecord receipt, CancellationToken cancellationToken);
}

public interface IMobileMoneyProvider
{
    Task<ProviderTransferResult> InitiatePayoutAsync(MobileMoneyPayoutInstruction instruction, CancellationToken cancellationToken);
}

public interface IBankTransferProvider
{
    Task<ProviderTransferResult> InitiatePayoutAsync(BankPayoutInstruction instruction, CancellationToken cancellationToken);
}

public interface IProviderTransferStatusProvider
{
    Task<ProviderTransferStatusResult> GetBankTransferStatusAsync(string providerReference, CancellationToken cancellationToken);
}

public interface ICryptoCustodyProvider
{
    Task<DepositAddressResult> CreateDepositAddressAsync(Guid walletId, string asset, string network, CancellationToken cancellationToken);
    Task<WithdrawalBroadcastResult> BroadcastWithdrawalAsync(CryptoWithdrawalInstruction instruction, CancellationToken cancellationToken);
}

public interface IWebhookSecurityService
{
    bool VerifySignature(string providerCode, string payload, string? signatureHeader);
}

public interface IAlertRepository
{
    Task SaveAsync(AlertRecord alertRecord, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AlertRecord>> GetOpenAlertsAsync(CancellationToken cancellationToken);
    Task<PagedResult<AlertRecord>> SearchAsync(AlertSearchCriteria criteria, CancellationToken cancellationToken);
}

public interface IApprovalRequestRepository
{
    Task SaveAsync(ApprovalRequest approvalRequest, CancellationToken cancellationToken);
    Task<ApprovalRequest?> GetByIdAsync(Guid approvalRequestId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ApprovalRequest>> GetPendingAsync(CancellationToken cancellationToken);
    Task<PagedResult<ApprovalRequest>> SearchAsync(ApprovalSearchCriteria criteria, CancellationToken cancellationToken);
}

public interface IReconciliationRepository
{
    Task SaveAsync(ReconciliationItem reconciliationItem, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ReconciliationItem>> GetOpenItemsAsync(CancellationToken cancellationToken);
}

public sealed record ProviderTransferResult(bool Accepted, string ProviderReference, string RawStatus);
public sealed record ProviderTransferStatusResult(bool Found, string ProviderReference, string RawStatus, string? FailureReason);
public sealed record DepositAddressResult(string WalletAddress, string Asset, string Network, int RequiredConfirmations);
public sealed record WithdrawalBroadcastResult(string TxHash, decimal NetworkFee);
public sealed record MobileMoneyPayoutInstruction(Guid TransferId, string MomoNumber, string Network, decimal Amount, string Currency, string Narrative);
public sealed record BankPayoutInstruction(Guid TransferId, string BankCode, string AccountNumber, decimal Amount, string Currency, string Narrative);
public sealed record CryptoWithdrawalInstruction(Guid WithdrawalId, string Asset, string Network, string DestinationAddress, decimal Amount);
public sealed record AlertRecord(Guid Id, Guid CustomerId, string AlertCode, string Severity, int Score, string Status, string PayloadJson, DateTimeOffset CreatedAtUtc);
public sealed record ApprovalRequest(Guid Id, Guid TransferOrderId, string ActionCode, string Status, string RequestedBy, string? ApprovedBy, string Reason, DateTimeOffset CreatedAtUtc);
public sealed record ReconciliationItem(Guid Id, string ReconciliationType, string ExternalReference, string InternalReference, decimal Amount, string Currency, string Status, string Notes, DateTimeOffset DetectedAtUtc);
public sealed record WebhookReceiptRecord(Guid Id, string ProviderCode, string ProviderReference, string PayloadHash, string EventType, DateTimeOffset ProcessedAtUtc);
public sealed record WalletBalanceProjection(Guid WalletId, decimal AvailableBalance, decimal ReservedBalance, string Currency, string Status, Guid? LiabilityLedgerAccountId);
public sealed record AuditEventRecord(Guid Id, string ActorId, string ActorType, string Action, string EntityType, string EntityId, string? BeforeJson, string? AfterJson, string? IpAddress, string? DeviceId, string? TraceId, DateTimeOffset CreatedAtUtc);
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
public sealed record TransferSearchCriteria(string? Status, string? Channel, string? Reference, DateTimeOffset? CreatedFromUtc, DateTimeOffset? CreatedToUtc, int Page, int PageSize);
public sealed record JournalSearchCriteria(string? Status, string? SourceModule, string? Reference, Guid? TransferOrderId, DateTimeOffset? BookingFromUtc, DateTimeOffset? BookingToUtc, int Page, int PageSize);
public sealed record AuditEventSearchCriteria(string? EntityType, string? EntityId, string? Action, DateTimeOffset? CreatedFromUtc, DateTimeOffset? CreatedToUtc, int Page, int PageSize);
public sealed record ApprovalSearchCriteria(string? Status, string? ActionCode, Guid? TransferOrderId, string? RequestedBy, DateTimeOffset? CreatedFromUtc, DateTimeOffset? CreatedToUtc, int Page, int PageSize);
public sealed record AlertSearchCriteria(string? Status, string? Severity, Guid? CustomerId, string? AlertCode, DateTimeOffset? CreatedFromUtc, DateTimeOffset? CreatedToUtc, int Page, int PageSize);
