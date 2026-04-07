using HybridTransfer.Application.Abstractions;

namespace HybridTransfer.Infrastructure.Providers;

public sealed class SandboxMobileMoneyProvider : IMobileMoneyProvider
{
    public Task<ProviderTransferResult> InitiatePayoutAsync(MobileMoneyPayoutInstruction instruction, CancellationToken cancellationToken)
        => Task.FromResult(new ProviderTransferResult(true, $"MOMO-{instruction.TransferId:N}", "submitted"));
}

public sealed class SandboxBankTransferProvider : IBankTransferProvider
{
    public Task<ProviderTransferResult> InitiatePayoutAsync(BankPayoutInstruction instruction, CancellationToken cancellationToken)
        => Task.FromResult(new ProviderTransferResult(true, $"BANK-{instruction.TransferId:N}", "submitted"));
}

public sealed class SandboxCryptoCustodyProvider : ICryptoCustodyProvider
{
    public Task<DepositAddressResult> CreateDepositAddressAsync(Guid walletId, string asset, string network, CancellationToken cancellationToken)
        => Task.FromResult(new DepositAddressResult($"sandbox-{asset.ToLowerInvariant()}-{walletId:N}"[0..Math.Min(42, $"sandbox-{asset.ToLowerInvariant()}-{walletId:N}".Length)], asset, network, 12));

    public Task<WithdrawalBroadcastResult> BroadcastWithdrawalAsync(CryptoWithdrawalInstruction instruction, CancellationToken cancellationToken)
        => Task.FromResult(new WithdrawalBroadcastResult($"0xsandbox{instruction.WithdrawalId:N}", 1.25m));
}

public sealed class BasicWebhookSecurityService : IWebhookSecurityService
{
    public bool VerifySignature(string providerCode, string payload, string? signatureHeader)
        => !string.IsNullOrWhiteSpace(providerCode) && !string.IsNullOrWhiteSpace(payload) && !string.IsNullOrWhiteSpace(signatureHeader);
}
