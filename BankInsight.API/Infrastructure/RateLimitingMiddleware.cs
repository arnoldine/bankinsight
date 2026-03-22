using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Hosting;

namespace BankInsight.API.Infrastructure;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private static readonly ConcurrentDictionary<string, RequestCounter> _requestCounts = new();

    private const int MaxRequestsPerMinute = 60;
    private const int CleanupIntervalSeconds = 60;
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldBypassRateLimit(context))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        PerformCleanup();

        var counter = _requestCounts.GetOrAdd(clientId, _ => new RequestCounter());

        if (counter.IsRateLimitExceeded(MaxRequestsPerMinute))
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Append("Retry-After", "60");
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = $"Maximum {MaxRequestsPerMinute} requests per minute allowed",
                retryAfter = 60
            });
            return;
        }

        counter.IncrementRequest();
        await _next(context);
    }

    private bool ShouldBypassRateLimit(HttpContext context)
    {
        if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
        {
            return true;
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp == null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(remoteIp))
        {
            return true;
        }

        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        if (remoteIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = remoteIp.GetAddressBytes();
            if (bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 127))
            {
                return true;
            }
        }

        return false;
    }

    private string GetClientIdentifier(HttpContext context)
    {
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    private void PerformCleanup()
    {
        if ((DateTime.UtcNow - _lastCleanup).TotalSeconds > CleanupIntervalSeconds)
        {
            _lastCleanup = DateTime.UtcNow;
            var keysToRemove = _requestCounts
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _requestCounts.TryRemove(key, out _);
            }
        }
    }
}

public class RequestCounter
{
    private readonly Queue<DateTime> _requestTimes = new();
    private readonly object _lock = new();
    private DateTime _lastRequest = DateTime.UtcNow;

    public void IncrementRequest()
    {
        lock (_lock)
        {
            _requestTimes.Enqueue(DateTime.UtcNow);
            _lastRequest = DateTime.UtcNow;
        }
    }

    public bool IsRateLimitExceeded(int maxRequests)
    {
        lock (_lock)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);

            while (_requestTimes.Count > 0 && _requestTimes.Peek() < oneMinuteAgo)
            {
                _requestTimes.Dequeue();
            }

            return _requestTimes.Count >= maxRequests;
        }
    }

    public bool IsExpired()
    {
        return (DateTime.UtcNow - _lastRequest).TotalMinutes > 10;
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}


