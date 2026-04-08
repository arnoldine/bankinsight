using HybridTransfer.Domain.Common;

namespace HybridTransfer.Application.Services;

public sealed class TransferRoutingPolicyService
{
    public const string HomeCountryCode = "GH";

    public string NormalizeCountryCode(string? countryCode)
        => string.IsNullOrWhiteSpace(countryCode) ? HomeCountryCode : countryCode.Trim().ToUpperInvariant();

    public void EnsureFiatRailAllowed(TransferChannel channel, string? destinationCountryCode)
    {
        var normalizedCountryCode = NormalizeCountryCode(destinationCountryCode);
        if (string.Equals(normalizedCountryCode, HomeCountryCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException($"{channel} transfers to '{normalizedCountryCode}' are not permitted on fiat rails. Cross-border transfers must use blockchain settlement.");
    }
}
