using System.Net;
using System.Text.Json;

namespace BankInsight.API.Infrastructure;

public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalErrorHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                response.ErrorCode = "UNAUTHORIZED";
                break;

            case ArgumentNullException:
            case ArgumentException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                response.ErrorCode = "INVALID_ARGUMENT";
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Resource not found";
                response.ErrorCode = "NOT_FOUND";
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                response.ErrorCode = "INVALID_OPERATION";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = _env.IsDevelopment() 
                    ? exception.Message 
                    : "An internal server error occurred";
                response.ErrorCode = "INTERNAL_ERROR";
                break;
        }

        // Include stack trace in development
        if (_env.IsDevelopment())
        {
            response.Details = exception.StackTrace;
            response.InnerException = exception.InnerException?.Message;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? InnerException { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class GlobalErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalErrorHandlingMiddleware>();
    }
}
