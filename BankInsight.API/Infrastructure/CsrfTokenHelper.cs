using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Infrastructure;

/// <summary>
/// Helper service for CSRF token management in API endpoints.
/// </summary>
public static class CsrfTokenHelper
{
    /// <summary>
    /// Gets the CSRF token from the HTTP context for the current request.
    /// </summary>
    public static string GetCsrfToken(this HttpContext httpContext)
    {
        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
        var token = antiforgery.GetAndStoreTokens(httpContext);
        return token.RequestToken ?? string.Empty;
    }

    /// <summary>
    /// Validates the CSRF token from the current request.
    /// Throws AntiforgeryValidationException if validation fails.
    /// </summary>
    public static async Task ValidateCsrfTokenAsync(this HttpContext httpContext)
    {
        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
        await antiforgery.ValidateRequestAsync(httpContext);
    }

    /// <summary>
    /// Gets the CSRF token from the request header.
    /// Header name is "X-CSRF-TOKEN" by default.
    /// </summary>
    public static string? GetCsrfTokenFromHeader(this HttpRequest request)
    {
        return request.Headers["X-CSRF-TOKEN"].FirstOrDefault() 
            ?? request.Headers["x-csrf-token"].FirstOrDefault();
    }

    /// <summary>
    /// Gets the CSRF token from the request form.
    /// Form field name is "_csrf_token" by default.
    /// </summary>
    public static string? GetCsrfTokenFromForm(this HttpRequest request)
    {
        return request.Form["_csrf_token"].FirstOrDefault() 
            ?? request.Form["_token"].FirstOrDefault();
    }
}
