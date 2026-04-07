using HybridTransfer.Domain.Common;

namespace HybridTransfer.Domain.Crypto;

public sealed class CryptoDeposit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid WalletId { get; init; }
    public string WalletAddress { get; init; }
    public string Asset { get; init; }
    public string Blockchain { get; init; }
    public string TxHash { get; init; }
    public decimal Amount { get; init; }
    public int Confirmations { get; private set; }
    public int RequiredConfirmations { get; init; }
    public CryptoTransactionStatus Status { get; private set; }

    public CryptoDeposit(Guid walletId, string walletAddress, string asset, string blockchain, string txHash, decimal amount, int requiredConfirmations)
    {
        WalletId = walletId;
        WalletAddress = walletAddress;
        Asset = asset;
        Blockchain = blockchain;
        TxHash = txHash;
        Amount = amount;
        RequiredConfirmations = requiredConfirmations;
        Status = CryptoTransactionStatus.Detected;
    }

    public void UpdateConfirmations(int confirmations)
    {
        Confirmations = confirmations;
        Status = confirmations >= RequiredConfirmations ? CryptoTransactionStatus.Credited : CryptoTransactionStatus.AwaitingConfirmations;
    }
}

public sealed class CryptoWithdrawal
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SourceWalletId { get; init; }
    public string DestinationAddress { get; init; }
    public string Asset { get; init; }
    public string Network { get; init; }
    public decimal Amount { get; init; }
    public decimal Fee { get; private set; }
    public string? TxHash { get; private set; }
    public ApprovalStatus ApprovalStatus { get; private set; } = ApprovalStatus.Pending;
    public RiskStatus RiskStatus { get; private set; } = RiskStatus.Monitor;
    public TransferStatus Status { get; private set; } = TransferStatus.AwaitingApproval;
    public string IdempotencyKey { get; init; }

    public CryptoWithdrawal(Guid sourceWalletId, string destinationAddress, string asset, string network, decimal amount, string idempotencyKey)
    {
        SourceWalletId = sourceWalletId;
        DestinationAddress = destinationAddress;
        Asset = asset;
        Network = network;
        Amount = amount;
        IdempotencyKey = idempotencyKey;
    }

    public void Approve() => ApprovalStatus = ApprovalStatus.Approved;
    public void SetRisk(RiskStatus riskStatus) => RiskStatus = riskStatus;
    public void Broadcast(string txHash, decimal fee)
    {
        TxHash = txHash;
        Fee = fee;
        Status = TransferStatus.Submitted;
    }
}
