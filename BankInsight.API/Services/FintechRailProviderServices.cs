using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using HybridTransfer.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace BankInsight.API.Services;

public sealed class BankInsightMobileMoneyProvider : IMobileMoneyProvider
{
    private readonly RailProviderOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BankInsightMobileMoneyProvider> _logger;

    public BankInsightMobileMoneyProvider(IOptions<FintechProviderOptions> options, IHttpClientFactory httpClientFactory, ILogger<BankInsightMobileMoneyProvider> logger)
    {
        _options = options.Value.MobileMoney;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ProviderTransferResult> InitiatePayoutAsync(MobileMoneyPayoutInstruction instruction, CancellationToken cancellationToken)
    {
        if (FintechRailProviderUtility.IsMock(_options.Mode))
        {
            var providerReference = FintechRailProviderUtility.BuildMockReference(_options.ReferencePrefix, "MOMO", instruction.TransferId);
            return new ProviderTransferResult(true, providerReference, "Submitted");
        }

        FintechRailProviderUtility.EnsureLiveConfigured(_options, "MobileMoney");
        var client = FintechRailProviderUtility.CreateClient(_httpClientFactory, _options);
        var payload = new MobileMoneyPayoutRequest(
            instruction.TransferId,
            instruction.MomoNumber,
            instruction.Network,
            instruction.Amount,
            instruction.Currency,
            instruction.Narrative,
            _options.ProviderCode);

        var endpoint = FintechRailProviderUtility.BuildUri(_options.BaseUrl!, _options.PayoutPath ?? "/payouts/mobile-money");
        _logger.LogInformation("Submitting fintech mobile money payout {TransferId} to {Endpoint}", instruction.TransferId, endpoint);
        var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ProviderTransferEnvelope>(cancellationToken: cancellationToken);
        return new ProviderTransferResult(true, body?.ProviderReference ?? FintechRailProviderUtility.BuildMockReference(_options.ReferencePrefix, "MOMO", instruction.TransferId), body?.Status ?? "Submitted");
    }
}

public sealed class BankInsightBankTransferProvider : IBankTransferProvider, IProviderTransferStatusProvider
{
    private readonly RailProviderOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BankInsightBankTransferProvider> _logger;

    public BankInsightBankTransferProvider(IOptions<FintechProviderOptions> options, IHttpClientFactory httpClientFactory, ILogger<BankInsightBankTransferProvider> logger)
    {
        _options = options.Value.BankTransfer;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ProviderTransferResult> InitiatePayoutAsync(BankPayoutInstruction instruction, CancellationToken cancellationToken)
    {
        if (FintechRailProviderUtility.IsMock(_options.Mode))
        {
            var providerReference = FintechRailProviderUtility.BuildMockReference(_options.ReferencePrefix, "BANK", instruction.TransferId);
            return new ProviderTransferResult(true, providerReference, "Submitted");
        }

        if (FintechRailProviderUtility.IsPaystack(_options.ProviderCode))
        {
            return await InitiatePaystackPayoutAsync(instruction, cancellationToken);
        }

        FintechRailProviderUtility.EnsureLiveConfigured(_options, "BankTransfer");
        var client = FintechRailProviderUtility.CreateClient(_httpClientFactory, _options);
        var payload = new BankTransferPayoutRequest(
            instruction.TransferId,
            instruction.BankCode,
            instruction.AccountNumber,
            instruction.Amount,
            instruction.Currency,
            instruction.Narrative,
            _options.ProviderCode);

        var endpoint = FintechRailProviderUtility.BuildUri(_options.BaseUrl!, _options.PayoutPath ?? "/payouts/bank");
        _logger.LogInformation("Submitting fintech bank payout {TransferId} to {Endpoint}", instruction.TransferId, endpoint);
        var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ProviderTransferEnvelope>(cancellationToken: cancellationToken);
        return new ProviderTransferResult(true, body?.ProviderReference ?? FintechRailProviderUtility.BuildMockReference(_options.ReferencePrefix, "BANK", instruction.TransferId), body?.Status ?? "Submitted");
    }

    public async Task<ProviderTransferStatusResult> GetBankTransferStatusAsync(string providerReference, CancellationToken cancellationToken)
    {
        if (FintechRailProviderUtility.IsMock(_options.Mode))
        {
            return new ProviderTransferStatusResult(true, providerReference, "success", null);
        }

        if (!FintechRailProviderUtility.IsPaystack(_options.ProviderCode))
        {
            throw new InvalidOperationException("Provider status verification is currently implemented for Paystack bank transfers only.");
        }

        FintechRailProviderUtility.EnsureLiveConfigured(_options, "BankTransfer");
        var client = FintechRailProviderUtility.CreateClient(_httpClientFactory, _options);
        var statusTemplate = _options.StatusPath ?? "/transfer/verify/{reference}";
        var endpoint = FintechRailProviderUtility.BuildTemplatedUri(_options.BaseUrl!, statusTemplate, new Dictionary<string, string>
        {
            ["reference"] = providerReference
        });

        _logger.LogInformation("Verifying Paystack bank payout {ProviderReference} using {Endpoint}", providerReference, endpoint);
        var response = await client.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<PaystackTransferStatusEnvelope>(cancellationToken: cancellationToken);
        if (body is null || !body.Status)
        {
            return new ProviderTransferStatusResult(false, providerReference, "unknown", body?.Message);
        }

        var resolvedReference = body.Data?.Transfer_Code ?? body.Data?.Reference ?? providerReference;
        return new ProviderTransferStatusResult(true, resolvedReference, body.Data?.Status ?? "unknown", body.Data?.Reason);
    }

    private async Task<ProviderTransferResult> InitiatePaystackPayoutAsync(BankPayoutInstruction instruction, CancellationToken cancellationToken)
    {
        FintechRailProviderUtility.EnsureLiveConfigured(_options, "BankTransfer");
        var client = FintechRailProviderUtility.CreateClient(_httpClientFactory, _options);

        var resolvedAccountName = await ResolvePaystackAccountNameAsync(client, instruction, cancellationToken);
        var recipientCode = await CreatePaystackRecipientAsync(client, instruction, resolvedAccountName, cancellationToken);
        var transferReference = FintechRailProviderUtility.BuildProviderReference(_options.ReferencePrefix, "bank", instruction.TransferId);
        var payload = new PaystackTransferRequest(
            _options.SourceAccount ?? "balance",
            FintechRailProviderUtility.ToSubunitAmount(instruction.Amount),
            recipientCode,
            transferReference,
            instruction.Narrative,
            instruction.Currency);

        var endpoint = FintechRailProviderUtility.BuildUri(_options.BaseUrl!, _options.PayoutPath ?? "/transfer");
        _logger.LogInformation("Submitting Paystack bank payout {TransferId} to {Endpoint}", instruction.TransferId, endpoint);
        var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<PaystackTransferEnvelope>(cancellationToken: cancellationToken);
        if (body is null || !body.Status)
        {
            throw new InvalidOperationException(body?.Message ?? "Paystack transfer request failed.");
        }

        var providerReference = body.Data?.Transfer_Code ?? body.Data?.Reference ?? transferReference;
        var providerStatus = body.Data?.Status ?? "pending";
        return new ProviderTransferResult(true, providerReference, providerStatus);
    }

    private async Task<string> ResolvePaystackAccountNameAsync(HttpClient client, BankPayoutInstruction instruction, CancellationToken cancellationToken)
    {
        var resolveTemplate = _options.ResolvePath ?? "/bank/resolve?account_number={accountNumber}&bank_code={bankCode}";
        var endpoint = FintechRailProviderUtility.BuildTemplatedUri(_options.BaseUrl!, resolveTemplate, new Dictionary<string, string>
        {
            ["accountNumber"] = instruction.AccountNumber,
            ["bankCode"] = instruction.BankCode
        });

        _logger.LogInformation("Resolving Paystack beneficiary account {AccountNumber} using {Endpoint}", instruction.AccountNumber, endpoint);
        var response = await client.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<PaystackResolveAccountEnvelope>(cancellationToken: cancellationToken);
        var accountName = body?.Data?.Account_Name;
        if (body is null || !body.Status || string.IsNullOrWhiteSpace(accountName))
        {
            throw new InvalidOperationException(body?.Message ?? "Paystack account resolution failed.");
        }

        return accountName;
    }

    private async Task<string> CreatePaystackRecipientAsync(HttpClient client, BankPayoutInstruction instruction, string accountName, CancellationToken cancellationToken)
    {
        var currency = instruction.Currency.Trim().ToUpperInvariant();
        var recipientType = currency == "GHS" ? "ghipss" : "nuban";
        var payload = new PaystackTransferRecipientRequest(
            recipientType,
            accountName,
            instruction.AccountNumber,
            instruction.BankCode,
            currency,
            instruction.Narrative,
            new { transferId = instruction.TransferId, provider = _options.ProviderCode });

        var endpoint = FintechRailProviderUtility.BuildUri(_options.BaseUrl!, _options.RecipientPath ?? "/transferrecipient");
        _logger.LogInformation("Creating Paystack recipient for transfer {TransferId} via {Endpoint}", instruction.TransferId, endpoint);
        var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<PaystackTransferRecipientEnvelope>(cancellationToken: cancellationToken);
        var recipientCode = body?.Data?.Recipient_Code;
        if (body is null || !body.Status || string.IsNullOrWhiteSpace(recipientCode))
        {
            throw new InvalidOperationException(body?.Message ?? "Paystack transfer recipient creation failed.");
        }

        return recipientCode;
    }
}

public sealed class BankInsightCryptoCustodyProvider : ICryptoCustodyProvider
{
    private readonly RailProviderOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BankInsightCryptoCustodyProvider> _logger;

    public BankInsightCryptoCustodyProvider(IOptions<FintechProviderOptions> options, IHttpClientFactory httpClientFactory, ILogger<BankInsightCryptoCustodyProvider> logger)
    {
        _options = options.Value.CryptoCustody;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DepositAddressResult> CreateDepositAddressAsync(Guid walletId, string asset, string network, CancellationToken cancellationToken)
    {
        var normalizedAsset = asset.Trim().ToUpperInvariant();
        var normalizedNetwork = network.Trim().ToUpperInvariant();

        if (FintechRailProviderUtility.IsMock(_options.Mode))
        {
            var address = $"{_options.ReferencePrefix}-{normalizedAsset}-{normalizedNetwork}-{walletId:N}";
            return new DepositAddressResult(address, normalizedAsset, normalizedNetwork, 3);
        }

        FintechRailProviderUtility.EnsureLiveConfigured(_options, "CryptoCustody");
        var client = FintechRailProviderUtility.CreateClient(_httpClientFactory, _options);
        var endpoint = FintechRailProviderUtility.BuildUri(_options.BaseUrl!, _options.AddressPath ?? "/custody/addresses");
        var payload = new CryptoDepositAddressRequest(walletId, normalizedAsset, normalizedNetwork, _options.ProviderCode);
        var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DepositAddressEnvelope>(cancellationToken: cancellationToken);
        return new DepositAddressResult(body?.WalletAddress ?? $"LIVE-{normalizedAsset}-{walletId:N}", normalizedAsset, normalizedNetwork, body?.RequiredConfirmations ?? 3);
    }

    public async Task<WithdrawalBroadcastResult> BroadcastWithdrawalAsync(CryptoWithdrawalInstruction instruction, CancellationToken cancellationToken)
    {
        if (FintechRailProviderUtility.IsMock(_options.Mode))
        {
            var txHash = $"0x{_options.ReferencePrefix.ToLowerInvariant()}{instruction.WithdrawalId:N}";
            return new WithdrawalBroadcastResult(txHash, 1.25m);
        }

        FintechRailProviderUtility.EnsureLiveConfigured(_options, "CryptoCustody");
        var client = FintechRailProviderUtility.CreateClient(_httpClientFactory, _options);
        var endpoint = FintechRailProviderUtility.BuildUri(_options.BaseUrl!, _options.WithdrawalPath ?? "/custody/withdrawals");
        _logger.LogInformation("Broadcasting fintech crypto withdrawal {WithdrawalId} to {Endpoint}", instruction.WithdrawalId, endpoint);
        var payload = new CryptoWithdrawalBroadcastRequest(
            instruction.WithdrawalId,
            instruction.Asset,
            instruction.Network,
            instruction.DestinationAddress,
            instruction.Amount,
            _options.ProviderCode);
        var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<WithdrawalBroadcastEnvelope>(cancellationToken: cancellationToken);
        return new WithdrawalBroadcastResult(body?.TxHash ?? $"0xlive{instruction.WithdrawalId:N}", body?.NetworkFee ?? 1.25m);
    }
}

public sealed class BankInsightWebhookSecurityService : IWebhookSecurityService
{
    private readonly FintechProviderOptions _options;

    public BankInsightWebhookSecurityService(IOptions<FintechProviderOptions> options)
    {
        _options = options.Value;
    }

    public bool VerifySignature(string providerCode, string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(providerCode) || string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        if (FintechRailProviderUtility.IsPaystack(providerCode))
        {
            var secret = _options.BankTransfer.ApiKey;
            if (string.IsNullOrWhiteSpace(secret))
            {
                return false;
            }

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            var provided = signatureHeader.Trim().ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(provided));
        }

        return string.Equals(signatureHeader, _options.Webhook.SharedSecret, StringComparison.Ordinal);
    }
}

internal static class FintechRailProviderUtility
{
    public static HttpClient CreateClient(IHttpClientFactory httpClientFactory, RailProviderOptions options)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds <= 0 ? 30 : options.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            var headerName = string.IsNullOrWhiteSpace(options.ApiKeyHeaderName) ? "X-API-Key" : options.ApiKeyHeaderName;
            var headerValue = string.IsNullOrWhiteSpace(options.ApiKeyPrefix) ? options.ApiKey : $"{options.ApiKeyPrefix} {options.ApiKey}";
            client.DefaultRequestHeaders.Remove(headerName);
            client.DefaultRequestHeaders.Add(headerName, headerValue);
        }

        return client;
    }

    public static string BuildUri(string baseUrl, string path)
    {
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    public static string BuildTemplatedUri(string baseUrl, string template, IReadOnlyDictionary<string, string> replacements)
    {
        var resolved = template;
        foreach (var pair in replacements)
        {
            resolved = resolved.Replace($"{{{pair.Key}}}", Uri.EscapeDataString(pair.Value), StringComparison.Ordinal);
        }

        if (resolved.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || resolved.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return resolved;
        }

        return BuildUri(baseUrl, resolved);
    }

    public static bool IsMock(string? mode) => string.IsNullOrWhiteSpace(mode) || mode.Equals("Mock", StringComparison.OrdinalIgnoreCase);
    public static bool IsPaystack(string? providerCode) => !string.IsNullOrWhiteSpace(providerCode) && providerCode.Contains("paystack", StringComparison.OrdinalIgnoreCase);
    public static string BuildMockReference(string prefix, string channel, Guid id) => $"{prefix}-{channel}-{id:N}";
    public static string BuildProviderReference(string prefix, string channel, Guid id) => $"{prefix.ToLowerInvariant()}-{channel.ToLowerInvariant()}-{id:N}";
    public static long ToSubunitAmount(decimal amount) => decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    public static void EnsureLiveConfigured(RailProviderOptions options, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException($"FintechProviders:{sectionName}:BaseUrl must be configured when Mode is Live.");
        }
    }
}
