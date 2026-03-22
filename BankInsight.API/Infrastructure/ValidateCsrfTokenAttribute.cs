using System;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankInsight.API.Infrastructure;

/// <summary>
/// Custom attribute to validate CSRF tokens for API endpoints.
/// Apply this attribute to POST, PUT, DELETE endpoints to ensure CSRF protection.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ValidateCsrfTokenAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip CSRF validation for GET, HEAD, OPTIONS, and TRACE requests
        if (context.HttpContext.Request.Method == "GET" ||
            context.HttpContext.Request.Method == "HEAD" ||
            context.HttpContext.Request.Method == "OPTIONS" ||
            context.HttpContext.Request.Method == "TRACE")
        {
            return;
        }

        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                new { error = "CSRF token validation failed" });
        }
    }
}
