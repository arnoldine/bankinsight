using HybridTransfer.Domain.Common;

namespace HybridTransfer.Domain.Wallets;

public sealed class Wallet
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CustomerId { get; init; }
    public WalletType WalletType { get; init; }
    public string Currency { get; init; }
    public decimal AvailableBalance { get; private set; }
    public decimal ReservedBalance { get; private set; }
    public WalletStatus Status { get; private set; }
    public Guid LiabilityLedgerAccountId { get; init; }

    public Wallet(Guid customerId, WalletType walletType, string currency, Guid liabilityLedgerAccountId)
    {
        CustomerId = customerId;
        WalletType = walletType;
        Currency = currency;
        LiabilityLedgerAccountId = liabilityLedgerAccountId;
        Status = WalletStatus.Active;
    }

    public void ProjectBalance(decimal availableBalance, decimal reservedBalance)
    {
        if (availableBalance < 0 || reservedBalance < 0) throw new InvalidOperationException("Projected balances cannot be negative.");
        AvailableBalance = availableBalance;
        ReservedBalance = reservedBalance;
    }

    public void Freeze() => Status = WalletStatus.Frozen;
}
