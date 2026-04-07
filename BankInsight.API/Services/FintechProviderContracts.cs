namespace BankInsight.API.Services;

public sealed class FintechProviderOptions
{
    public RailProviderOptions MobileMoney { get; set; } = new();
    public RailProviderOptions BankTransfer { get; set; } = new();
    public RailProviderOptions CryptoCustody { get; set; } = new();
    public WebhookProviderOptions Webhook { get; set; } = new();
}

public sealed class RailProviderOptions
{
    public string Mode { get; set; } = "Mock";
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
    public string? ApiKeyPrefix { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public string? PayoutPath { get; set; }
    public string? StatusPath { get; set; }
    public string? ValidationPath { get; set; }
    public string? RecipientPath { get; set; }
    public string? ResolvePath { get; set; }
    public string? AddressPath { get; set; }
    public string? WithdrawalPath { get; set; }
    public string? SourceAccount { get; set; }
    public string ProviderCode { get; set; } = "bankinsight";
    public string ReferencePrefix { get; set; } = "BI";
}

public sealed class WebhookProviderOptions
{
    public string SharedSecret { get; set; } = "bankinsight-dev-webhook-secret";
    public string SignatureHeaderName { get; set; } = "X-Signature";
}

internal sealed record MobileMoneyPayoutRequest(
    Guid TransferId,
    string MomoNumber,
    string Network,
    decimal Amount,
    string Currency,
    string Narrative,
    string ProviderCode);

internal sealed record BankTransferPayoutRequest(
    Guid TransferId,
    string BankCode,
    string AccountNumber,
    decimal Amount,
    string Currency,
    string Narrative,
    string ProviderCode);

internal sealed record PaystackResolveAccountEnvelope(bool Status, string? Message, PaystackResolveAccountData? Data);
internal sealed record PaystackResolveAccountData(string? Account_Number, string? Account_Name);
internal sealed record PaystackTransferRecipientRequest(string Type, string Name, string Account_Number, string Bank_Code, string Currency, string Description, object Metadata);
internal sealed record PaystackTransferRecipientEnvelope(bool Status, string? Message, PaystackTransferRecipientData? Data);
internal sealed record PaystackTransferRecipientData(string? Recipient_Code, PaystackRecipientDetails? Details);
internal sealed record PaystackRecipientDetails(string? Account_Number, string? Account_Name, string? Bank_Code, string? Bank_Name);
internal sealed record PaystackTransferRequest(string Source, long Amount, string Recipient, string Reference, string Reason, string Currency);
internal sealed record PaystackTransferEnvelope(bool Status, string? Message, PaystackTransferData? Data);
internal sealed record PaystackTransferData(string? Reference, string? Status, string? Transfer_Code);
internal sealed record PaystackTransferStatusEnvelope(bool Status, string? Message, PaystackTransferStatusData? Data);
internal sealed record PaystackTransferStatusData(string? Reference, string? Status, string? Transfer_Code, string? Reason);

internal sealed record CryptoDepositAddressRequest(
    Guid WalletId,
    string Asset,
    string Network,
    string ProviderCode);

internal sealed record CryptoWithdrawalBroadcastRequest(
    Guid WithdrawalId,
    string Asset,
    string Network,
    string DestinationAddress,
    decimal Amount,
    string ProviderCode);

internal sealed record ProviderTransferEnvelope(string? ProviderReference, string? Status);
internal sealed record DepositAddressEnvelope(string? WalletAddress, int? RequiredConfirmations);
internal sealed record WithdrawalBroadcastEnvelope(string? TxHash, decimal? NetworkFee);
