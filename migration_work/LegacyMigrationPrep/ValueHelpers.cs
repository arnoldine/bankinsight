using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LegacyMigrationPrep;

internal static class ValueHelpers
{
    public static Dictionary<string, string> NewRecord(params (string Key, string Value)[] values)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            row[key] = value ?? string.Empty;
        }

        return row;
    }

    public static string Get(IReadOnlyDictionary<string, string> row, string key)
        => row.TryGetValue(key, out var value) ? value?.Trim() ?? string.Empty : string.Empty;

    public static string? FirstNonEmpty(IEnumerable<string?> values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    public static string JoinName(params string?[] parts)
        => string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));

    public static string CombineAddress(string? home, string? postal)
    {
        var items = new[] { home, postal }
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return string.Join(" | ", items);
    }

    public static string DistinctSecondaryPhone(string? primary, string? secondary)
    {
        if (string.IsNullOrWhiteSpace(secondary))
        {
            return string.Empty;
        }

        if (string.Equals(primary?.Trim(), secondary.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return secondary.Trim();
    }

    public static string NormalizeCustomerType(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "INDIVIDUAL" => "Individual",
            "CORPORATE" => "Corporate",
            "BUSINESS" => "Corporate",
            _ => string.IsNullOrWhiteSpace(value) ? "Individual" : value.Trim()
        };
    }

    public static string NormalizeAccountType(string accountType, string productName)
    {
        var combined = $"{accountType} {productName}".ToUpperInvariant();
        if (combined.Contains("FIXED")) return "FIXED_DEPOSIT";
        if (combined.Contains("CURRENT")) return "CURRENT";
        if (combined.Contains("SAV")) return "SAVINGS";
        return "SAVINGS";
    }

    public static string NormalizeAccountStatus(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ACTIVE" => "ACTIVE",
            "DORMANT" => "DORMANT",
            "CLOSED" => "CLOSED",
            "FROZEN" => "FROZEN",
            _ => string.IsNullOrWhiteSpace(value) ? "ACTIVE" : normalized
        };
    }

    public static string NormalizeLoanStatus(string contractStatusId, string outstandingBalance, string disbursementDate)
    {
        if (!string.IsNullOrWhiteSpace(outstandingBalance) &&
            decimal.TryParse(outstandingBalance, NumberStyles.Any, CultureInfo.InvariantCulture, out var balance) &&
            balance <= 0)
        {
            return "CLOSED";
        }

        if (!string.IsNullOrWhiteSpace(disbursementDate)) return "ACTIVE";
        return contractStatusId == "4" ? "ACTIVE" : "PENDING";
    }

    public static string NormalizeRepaymentFrequency(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "MONTHLY" => "Monthly",
            "WEEKLY" => "Weekly",
            "QUARTERLY" => "Quarterly",
            "DAILY" => "Daily",
            _ => string.IsNullOrWhiteSpace(value) ? "Monthly" : value.Trim()
        };
    }

    public static string NormalizeInterestMethod(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "FIXED" => "Flat",
            "FLAT" => "Flat",
            "REDUCING" => "Reducing",
            _ => string.IsNullOrWhiteSpace(value) ? "Flat" : value.Trim()
        };
    }

    public static string NormalizeLoanTermMonths(string payments, string frequency)
    {
        if (!int.TryParse(NormalizeInteger(payments), out var count) || count <= 0) return "12";
        return NormalizeRepaymentFrequency(frequency) switch
        {
            "Monthly" => count.ToString(CultureInfo.InvariantCulture),
            "Quarterly" => (count * 3).ToString(CultureInfo.InvariantCulture),
            "Weekly" => Math.Max(1, (int)Math.Ceiling(count / 4m)).ToString(CultureInfo.InvariantCulture),
            "Daily" => Math.Max(1, (int)Math.Ceiling(count / 30m)).ToString(CultureInfo.InvariantCulture),
            _ => count.ToString(CultureInfo.InvariantCulture)
        };
    }

    public static string NormalizeBranch(string? branch)
    {
        var normalized = branch?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized is "01" or "1" or "MAKOLA" or "AVENOR" or "HEAD OFFICE" or "HO") return "BR001";
        if (normalized is "02" or "2" or "KUMASI" or "KUMASI BRANCH" or "KSI") return "BR002";
        return "BR001";
    }

    public static string NormalizeCurrency(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch { "GHC" => "GHS", "" => "GHS", _ => normalized };
    }

    public static string NormalizeRiskLevel(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized switch { "HIGH" => "High", "MEDIUM" => "Medium", "LOW" => "Low", _ => "Low" };
    }

    public static bool IsGhanaCard(string? idType)
        => !string.IsNullOrWhiteSpace(idType) && idType.Contains("GHANA", StringComparison.OrdinalIgnoreCase);

    public static string NormalizeGender(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized switch { "M" => "Male", "F" => "Female", "MALE" => "Male", "FEMALE" => "Female", _ => value?.Trim() ?? string.Empty };
    }

    public static string NormalizeYesNo(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized switch { "YES" => "YES", "NO" => "NO", "TRUE" => "YES", "FALSE" => "NO", _ => string.Empty };
    }

    public static string NormalizeParBucket(string? daysInArrears)
        => int.TryParse(NormalizeInteger(daysInArrears), out var days) ? days.ToString(CultureInfo.InvariantCulture) : "0";

    public static string NormalizeInteger(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var cleaned = value.Trim().Replace(".00", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (int.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)) return result.ToString(CultureInfo.InvariantCulture);
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalResult)) return decimal.Truncate(decimalResult).ToString(CultureInfo.InvariantCulture);
        return string.Empty;
    }

    public static string NormalizeDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "NULL", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        return decimal.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }

    public static string NormalizeDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "NULL", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        return DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    public static string NormalizeDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "NULL", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        return DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    public static string SafeTruncate(string? value, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    public static string BuildStableCode(string prefix, string source)
    {
        var normalized = new string(source.ToUpperInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (normalized.Contains("--", StringComparison.Ordinal)) normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        normalized = normalized.Trim('-');
        var body = normalized.Length > 30 ? normalized[..30].Trim('-') : normalized;
        using var sha1 = SHA1.Create();
        var hash = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(source))).Substring(0, 8);
        return $"{prefix}-{body}-{hash}";
    }

    public static bool LooksValidId(string? value, int minLength)
        => !string.IsNullOrWhiteSpace(value) && System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), $"^[0-9A-Za-z-]{{{minLength},}}$");
}
