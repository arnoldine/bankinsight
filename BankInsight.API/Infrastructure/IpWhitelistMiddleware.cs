using System.Net;

namespace BankInsight.API.Infrastructure;

public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpWhitelistMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public IpWhitelistMiddleware(
        RequestDelegate next,
        ILogger<IpWhitelistMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var enabled = _configuration.GetValue<bool>("Security:IpWhitelist:Enabled");
        if (!enabled)
        {
            await _next(context);
            return;
        }

        var allowedIps = _configuration
            .GetSection("Security:IpWhitelist:AllowedIps")
            .Get<string[]>() ?? Array.Empty<string>();

        if (allowedIps.Length == 0)
        {
            _logger.LogWarning("IP whitelist is enabled but no allowed IPs are configured.");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Access denied",
                message = "No whitelisted IP addresses configured"
            });
            return;
        }

        var remoteIp = GetClientIp(context);
        if (string.IsNullOrWhiteSpace(remoteIp))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Access denied",
                message = "Unable to determine client IP"
            });
            return;
        }

        var isAllowed = allowedIps.Any(ip => string.Equals(ip, remoteIp, StringComparison.OrdinalIgnoreCase));
        if (!isAllowed)
        {
            _logger.LogWarning("Blocked request from non-whitelisted IP: {RemoteIp}", remoteIp);
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Access denied",
                message = "Your IP address is not allowed"
            });
            return;
        }

        await _next(context);
    }

    private static string? GetClientIp(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}

public static class IpWhitelistMiddlewareExtensions
{
    public static IApplicationBuilder UseIpWhitelist(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IpWhitelistMiddleware>();
    }
}
