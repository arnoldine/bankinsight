namespace HybridTransfer.Api.Contracts;

public sealed record BankTransferRequest(Guid SourceWalletId, string BankCode, string AccountNumber, decimal Amount, string Currency, string AccountName, string DestinationCountryCode, string? Narrative);
