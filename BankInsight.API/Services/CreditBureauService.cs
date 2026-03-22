using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BankInsight.API.Services;

public class CreditBureauResult
{
    public int Score { get; set; }
    public string RiskBand { get; set; } = "UNKNOWN";
    public string RiskGrade { get; set; } = "UNKNOWN";
    public string Decision { get; set; } = "REVIEW";
    public string Recommendation { get; set; } = "Manual review";
    public string BureauName { get; set; } = "DefaultCRB";
    public string ProviderName { get; set; } = "fallback";
    public string InquiryReference { get; set; } = string.Empty;
    public string RequestPayload { get; set; } = "{}";
    public string RawResponse { get; set; } = "{}";
    public bool IsTimeout { get; set; }
    public int RetryCount { get; set; }
    public string Status { get; set; } = "SUCCESS";
}

public interface ICreditBureauProvider
{
    string ProviderName { get; }
    bool CanHandle(string providerName);
    Task<CreditBureauResult> CheckCreditAsync(string customerId, int timeoutSeconds);
}

public class HttpCreditBureauProvider : ICreditBureauProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public HttpCreditBureauProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public string ProviderName => "http";

    public bool CanHandle(string providerName)
    {
        return string.Equals(providerName, ProviderName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerName, "default", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CreditBureauResult> CheckCreditAsync(string customerId, int timeoutSeconds)
    {
        var baseUrl = _configuration["CreditBureau:BaseUrl"];
        var scorePath = _configuration["CreditBureau:ScorePath"] ?? "/score/check";
        var apiKey = _configuration["CreditBureau:ApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Credit bureau base URL is not configured.");
        }

        var requestPayload = JsonSerializer.Serialize(new { customerId });
        using var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        using var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(scorePath, content);
        var raw = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Credit bureau provider returned {(int)response.StatusCode}");
        }

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;

        var score = root.TryGetProperty("score", out var scoreValue) && scoreValue.TryGetInt32(out var parsedScore)
            ? parsedScore
            : ComputeDeterministicScore(customerId);

        var riskBand = root.TryGetProperty("riskBand", out var riskBandValue)
            ? riskBandValue.GetString() ?? ComputeRiskBand(score)
            : ComputeRiskBand(score);

        var decision = root.TryGetProperty("decision", out var decisionValue)
            ? decisionValue.GetString() ?? ComputeDecision(score)
            : ComputeDecision(score);

        return new CreditBureauResult
        {
            Score = score,
            RiskBand = riskBand,
            RiskGrade = ComputeRiskGrade(score),
            Decision = decision,
            Recommendation = BuildRecommendation(decision),
            BureauName = _configuration["CreditBureau:Name"] ?? "DefaultCRB",
            ProviderName = ProviderName,
            InquiryReference = $"CRB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            RequestPayload = requestPayload,
            RawResponse = string.IsNullOrWhiteSpace(raw) ? "{}" : raw,
            IsTimeout = false,
            RetryCount = 0,
            Status = "SUCCESS"
        };
    }

    private static int ComputeDeterministicScore(string customerId)
    {
        var hash = Math.Abs(customerId.GetHashCode());
        return 500 + (hash % 351);
    }

    private static string ComputeRiskBand(int score) => score switch
    {
        >= 750 => "LOW",
        >= 650 => "MEDIUM",
        _ => "HIGH"
    };

    private static string ComputeRiskGrade(int score) => score switch
    {
        >= 800 => "A",
        >= 700 => "B",
        >= 620 => "C",
        _ => "D"
    };

    private static string ComputeDecision(int score) => score switch
    {
        >= 700 => "PASS",
        >= 620 => "REVIEW",
        _ => "FAIL"
    };

    private static string BuildRecommendation(string decision) => decision switch
    {
        "PASS" => "Proceed with approval",
        "REVIEW" => "Escalate for manual review",
        _ => "Decline or request stronger collateral"
    };
}

public class MockCreditBureauProvider : ICreditBureauProvider
{
    public string ProviderName => "mock";

    public bool CanHandle(string providerName)
    {
        return string.Equals(providerName, ProviderName, StringComparison.OrdinalIgnoreCase);
    }

    public Task<CreditBureauResult> CheckCreditAsync(string customerId, int timeoutSeconds)
    {
        var score = 500 + (Math.Abs(customerId.GetHashCode()) % 351);
        var decision = score >= 700 ? "PASS" : score >= 620 ? "REVIEW" : "FAIL";

        return Task.FromResult(new CreditBureauResult
        {
            Score = score,
            RiskBand = score >= 750 ? "LOW" : score >= 650 ? "MEDIUM" : "HIGH",
            RiskGrade = score >= 800 ? "A" : score >= 700 ? "B" : score >= 620 ? "C" : "D",
            Decision = decision,
            Recommendation = decision == "PASS" ? "Proceed with approval" : decision == "REVIEW" ? "Escalate for manual review" : "Decline or request stronger collateral",
            BureauName = "Mock Ghana CRB",
            ProviderName = ProviderName,
            InquiryReference = $"MOCK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            RequestPayload = JsonSerializer.Serialize(new { customerId, timeoutSeconds }),
            RawResponse = JsonSerializer.Serialize(new { source = "mock", score, decision }),
            Status = "SUCCESS"
        });
    }
}

public interface ICreditBureauService
{
    Task<CreditBureauResult> CheckCreditAsync(string customerId, string? providerName = null);
    IReadOnlyList<string> GetAvailableProviders();
}

public class CreditBureauService : ICreditBureauService
{
    private readonly IEnumerable<ICreditBureauProvider> _providers;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreditBureauService> _logger;
    private readonly IHostEnvironment _environment;

    public CreditBureauService(
        IEnumerable<ICreditBureauProvider> providers,
        IConfiguration configuration,
        ILogger<CreditBureauService> logger,
        IHostEnvironment environment)
    {
        _providers = providers;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    public async Task<CreditBureauResult> CheckCreditAsync(string customerId, string? providerName = null)
    {
        var configuredProvider = providerName
            ?? _configuration["CreditBureau:ActiveProvider"]
            ?? "mock";

        if (IsStrictProductionMode() && string.Equals(configuredProvider, "mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Mock credit bureau provider is not allowed outside development or testing.");
        }

        var timeoutSeconds = int.TryParse(_configuration["CreditBureau:TimeoutSeconds"], out var timeout)
            ? Math.Max(2, timeout)
            : 10;

        var maxRetries = int.TryParse(_configuration["CreditBureau:MaxRetries"], out var retries)
            ? Math.Max(0, retries)
            : 2;

        var provider = _providers.FirstOrDefault(p => p.CanHandle(configuredProvider));
        if (provider == null)
        {
            if (IsStrictProductionMode())
            {
                throw new InvalidOperationException($"Credit bureau provider '{configuredProvider}' is not configured.");
            }

            return BuildFallback(customerId, configuredProvider, "Provider not found", 0, false);
        }

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await provider.CheckCreditAsync(customerId, timeoutSeconds);
                result.RetryCount = attempt;
                result.ProviderName = provider.ProviderName;
                return result;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Credit bureau timeout from provider {Provider} for customer {CustomerId} attempt {Attempt}", provider.ProviderName, customerId, attempt + 1);
                if (attempt == maxRetries)
                {
                    if (IsStrictProductionMode())
                    {
                        throw new InvalidOperationException($"Credit bureau provider timeout after {attempt + 1} attempts.");
                    }
                    return BuildFallback(customerId, provider.ProviderName, ex.Message, attempt, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Credit bureau failure from provider {Provider} for customer {CustomerId} attempt {Attempt}", provider.ProviderName, customerId, attempt + 1);
                if (attempt == maxRetries)
                {
                    if (IsStrictProductionMode())
                    {
                        throw new InvalidOperationException($"Credit bureau provider failure after {attempt + 1} attempts: {ex.Message}");
                    }
                    return BuildFallback(customerId, provider.ProviderName, ex.Message, attempt, false);
                }
            }
        }

        if (IsStrictProductionMode())
        {
            throw new InvalidOperationException("Credit bureau check could not be completed.");
        }

        return BuildFallback(customerId, provider.ProviderName, "Unknown provider failure", maxRetries, false);
    }

    public IReadOnlyList<string> GetAvailableProviders()
    {
        return _providers.Select(p => p.ProviderName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p).ToList();
    }

    private static CreditBureauResult BuildFallback(string customerId, string providerName, string reason, int retryCount, bool isTimeout)
    {
        var score = 500 + (Math.Abs(customerId.GetHashCode()) % 351);
        var decision = score >= 700 ? "PASS" : score >= 620 ? "REVIEW" : "FAIL";

        return new CreditBureauResult
        {
            Score = score,
            RiskBand = score >= 750 ? "LOW" : score >= 650 ? "MEDIUM" : "HIGH",
            RiskGrade = score >= 800 ? "A" : score >= 700 ? "B" : score >= 620 ? "C" : "D",
            Decision = decision,
            Recommendation = "Fallback recommendation due to provider error",
            BureauName = "Fallback Bureau",
            ProviderName = providerName,
            InquiryReference = $"FBK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            RequestPayload = JsonSerializer.Serialize(new { customerId }),
            RawResponse = JsonSerializer.Serialize(new { mode = "fallback", reason }),
            IsTimeout = isTimeout,
            RetryCount = retryCount,
            Status = "FALLBACK"
        };
    }

    private bool IsStrictProductionMode()
    {
        return !_environment.IsDevelopment() && !_environment.IsEnvironment("Testing");
    }
}
