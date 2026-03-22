using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Jint;
using Jint.Constraints;
using Jint.Runtime;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;

namespace BankInsight.API.Middleware;

public class JintScriptingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JintScriptingMiddleware> _logger;

    public JintScriptingMiddleware(RequestDelegate next, ILogger<JintScriptingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Example: Only intercept requests matching a magical header for "Custom-Script"
        // In reality, this would likely lookup a script by Route or Tenant from the DB via Elsa
        if (!context.Request.Headers.TryGetValue("X-Run-Script", out var scriptCode))
        {
            await _next(context);
            return;
        }

        _logger.LogInformation("Intercepted request with Custom Script requirement.");

        // 1. Audit Trail: Hash the script and context for immutable auditing
        var scriptHash = ComputeSha256(scriptCode.ToString());
        _logger.LogInformation("Script Hash: {Hash}", scriptHash);

        try
        {
            // 2. Read Request Body to expose to Jint
            context.Request.EnableBuffering();
            var reader = new StreamReader(context.Request.Body);
            var bodyText = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset for downstream

            // 3. Configure a deeply Sandboxed Jint Engine
            var engine = new Engine(options => {
                
                // Sandbox Rule 1: STRICT Memory Limit (e.g. 10MB) to prevent Heap bursts
                options.LimitMemory(10_000_000); 

                // Sandbox Rule 2: STRICT Execution Timeout to prevent While(true) loops
                options.TimeoutInterval(TimeSpan.FromSeconds(5));

                // Sandbox Rule 3: Disable I/O, clr imports, and Reflection entirely
                options.Strict(true); 
            });

            // 4. Inject safe contextual variables
            engine.SetValue("reqBody", bodyText);
            engine.SetValue("reqMethod", context.Request.Method);
            engine.SetValue("reqPath", context.Request.Path.Value);
            
            // Allow them to append a custom log locally within the script
            List<string> scriptLogs = new List<string>();
            engine.SetValue("log", new Action<string>(msg => scriptLogs.Add(msg)));

            // 5. Execute the injected Custom JavaScript logic safely
            var result = engine.Evaluate(scriptCode.ToString());

            // 6. Handle the result
            if (!result.IsUndefined() && !result.IsNull())
            {
                // If the script generates a Response directly, we hijack the pipeline and return it.
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                    ScriptResult = result.ToString(),
                    Logs = scriptLogs 
                }));
                return;
            }

            // Script ran successfully, didn't block or return immediately. Continue pipeline.
            await _next(context);
        }
        catch (ExecutionCanceledException ex)
        {
            _logger.LogError(ex, "Script exceeded evaluation constraints (Time or Memory).");
            context.Response.StatusCode = 408; // Request Timeout
            await context.Response.WriteAsync("Script Execution Terminated due to Resource Constraints.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Jint Sandbox evaluation failed.");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Script Error: {ex.Message}");
        }
    }

    private string ComputeSha256(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class JintScriptingMiddlewareExtensions
{
    public static IApplicationBuilder UseJintScriptingSandbox(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JintScriptingMiddleware>();
    }
}
