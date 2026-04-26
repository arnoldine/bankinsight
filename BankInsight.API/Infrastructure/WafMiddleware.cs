using System.Net;
using BankInsight.API.Services;

namespace BankInsight.API.Infrastructure;

public class WafMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WafMiddleware> _logger;

    public WafMiddleware(RequestDelegate next, ILogger<WafMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IWafService wafService)
    {
        var evaluation = await wafService.EvaluateRequestAsync(context);
        if (evaluation is null)
        {
            await _next(context);
            return;
        }

        await wafService.RecordIncidentAsync(context, evaluation);
        _logger.LogWarning("WAF {Outcome} rule {RuleCode} for {Method} {Path}", evaluation.ShouldBlock ? "blocked" : "detected", evaluation.RuleCode, context.Request.Method, evaluation.RequestPath);

        if (!evaluation.ShouldBlock)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Request blocked by WAF",
            rule = evaluation.RuleCode,
            mode = evaluation.Mode,
            message = evaluation.Description,
        });
    }
}

public static class WafMiddlewareExtensions
{
    public static IApplicationBuilder UseWaf(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WafMiddleware>();
    }
}
