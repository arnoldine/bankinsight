namespace HybridTransfer.Application.Services;

public sealed class CurrencyPolicyService
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "GHS",
        "USD",
        "EUR",
        "GBP",
        "USDT",
        "USDC",
        "BTC",
        "ETH"
    };

    public string NormalizeSupportedCurrency(string currency)
    {
        var normalized = currency?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !SupportedCurrencies.Contains(normalized))
        {
            throw new InvalidOperationException($"Currency '{currency}' is not supported by the fintech platform.");
        }

        return normalized;
    }

    public void EnsureWalletCurrencyMatches(string walletCurrency, string requestCurrency)
    {
        var normalizedWalletCurrency = NormalizeSupportedCurrency(walletCurrency);
        var normalizedRequestCurrency = NormalizeSupportedCurrency(requestCurrency);

        if (!string.Equals(normalizedWalletCurrency, normalizedRequestCurrency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Wallet currency '{normalizedWalletCurrency}' does not match requested settlement currency '{normalizedRequestCurrency}'. Convert funds before transfer.");
        }
    }
}
