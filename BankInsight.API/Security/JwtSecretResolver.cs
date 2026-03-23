using System.Text;
using Microsoft.Extensions.Configuration;

namespace BankInsight.API.Security;

public static class JwtSecretResolver
{
    private const int MinSecretBytes = 16; // HMAC-SHA256 requires at least 128 bits.

    public static string Resolve(IConfiguration configuration)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET");

        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = configuration["JwtSettings:Secret"];
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "JWT secret key is not configured. Set JWT_SECRET environment variable or JwtSettings:Secret.");
        }

        if (LooksLikePlaceholder(secret))
        {
            throw new InvalidOperationException(
                "JWT secret key is still a placeholder (${JWT_SECRET}). Set a real JWT_SECRET value in environment variables.");
        }

        var byteCount = Encoding.UTF8.GetByteCount(secret);
        if (byteCount < MinSecretBytes)
        {
            throw new InvalidOperationException(
                $"JWT secret key is too short ({byteCount * 8} bits). It must be at least {MinSecretBytes * 8} bits (16+ UTF-8 bytes).");
        }

        return secret;
    }

    public static byte[] ResolveBytes(IConfiguration configuration)
    {
        return Encoding.UTF8.GetBytes(Resolve(configuration));
    }

    private static bool LooksLikePlaceholder(string value)
    {
        var trimmed = value.Trim();
        return string.Equals(trimmed, "${JWT_SECRET}", StringComparison.OrdinalIgnoreCase)
            || (trimmed.StartsWith("${", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal));
    }
}