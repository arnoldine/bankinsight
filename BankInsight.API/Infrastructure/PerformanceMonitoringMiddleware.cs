using System.Diagnostics;

namespace BankInsight.API.Infrastructure;

public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        try
        {
            // Add response header before the response starts
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Response-Time-Ms"))
                {
                    context.Response.Headers.Append("X-Response-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;

            // Log slow requests (>500ms)
            if (elapsedMs > 500)
            {
                _logger.LogWarning(
                    "Slow Request: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    requestMethod, requestPath, elapsedMs, statusCode);
            }
            else
            {
                _logger.LogInformation(
                    "Request: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    requestMethod, requestPath, elapsedMs, statusCode);
            }
        }
    }
}

public static class PerformanceMonitoringMiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
    }
}
