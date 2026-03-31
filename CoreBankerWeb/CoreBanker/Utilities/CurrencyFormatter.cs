using System.Globalization;

namespace CoreBanker.Utilities;

public static class CurrencyFormatter
{
    public static string Format(decimal amount, string? currency = "GHS")
    {
        return Format(amount, currency, includeCode: false);
    }

    public static string Format(decimal amount, string? currency, bool includeCode)
    {
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? "GHS"
            : currency.Trim().ToUpperInvariant();

        var culture = normalizedCurrency switch
        {
            "USD" => CultureInfo.GetCultureInfo("en-US"),
            "EUR" => CultureInfo.GetCultureInfo("fr-FR"),
            "GBP" => CultureInfo.GetCultureInfo("en-GB"),
            _ => CultureInfo.GetCultureInfo("en-GH")
        };

        var formatted = string.Format(culture, "{0:C}", amount);
        return includeCode ? $"{formatted} ({normalizedCurrency})" : formatted;
    }
}
