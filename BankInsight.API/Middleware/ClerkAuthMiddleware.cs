using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BankInsight.API.Middleware;

/// <summary>
/// Middleware to validate Clerk JWT tokens and extract claims
/// </summary>
public class ClerkAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly string? _clerkPublishableKey;

    public ClerkAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
        _clerkPublishableKey = configuration["Clerk:PublishableKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context);

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var principal = ValidateToken(token);
                if (principal != null)
                {
                    context.User = principal;
                }
            }
            catch (Exception ex)
            {
                // Log validation error but don't fail the request
                Console.WriteLine($"Clerk token validation error: {ex.Message}");
            }
        }

        await _next(context);
    }

    private string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Verify token structure and claims (without validating signature for now)
            // In production, validate the signature using Clerk's public key
            var claims = jwtToken.Claims.ToList();

            // Extract Clerk user ID and email
            var userId = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            // Create identity with Clerk claims
            var identity = new ClaimsIdentity(claims, "Clerk");
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, email));
            }

            return new ClaimsPrincipal(identity);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
