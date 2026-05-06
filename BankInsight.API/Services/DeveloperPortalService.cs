using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class DeveloperPortalService
{
    private static readonly IReadOnlyList<(string EventName, string Category, string Description)> EventCatalog =
    [
        ("customer.created", "CUSTOMERS", "Raised when a customer profile is created and approved."),
        ("account.opened", "ACCOUNTS", "Raised when a deposit account becomes active."),
        ("loan.approved", "LOANS", "Raised when a loan application passes approval."),
        ("loan.disbursed", "LOANS", "Raised when a loan disbursement is posted."),
        ("payment.returned", "PAYMENTS", "Raised when a cheque or bulk payment is returned."),
        ("kyc.verified", "COMPLIANCE", "Raised when a customer KYC package is verified.")
    ];

    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public DeveloperPortalService(ApplicationDbContext context, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
    }

    public async Task<DeveloperPortalSummaryDto> GetSummaryAsync()
    {
        await EnsureSeededAsync();

        var products = await _context.ApiProductDefinitions
            .AsNoTracking()
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name)
            .ToListAsync();

        var partnerApps = await _context.PartnerApplications
            .AsNoTracking()
            .OrderBy(item => item.PartnerName)
            .ThenBy(item => item.Name)
            .ToListAsync();

        var webhookSubscriptions = await _context.WebhookSubscriptions
            .AsNoTracking()
            .Include(item => item.PartnerApplication)
            .OrderBy(item => item.EventName)
            .ThenBy(item => item.TargetUrl)
            .ToListAsync();

        var deliveryLogs = await _context.WebhookDeliveryLogs
            .AsNoTracking()
            .OrderByDescending(item => item.DeliveredAt)
            .Take(25)
            .ToListAsync();

        return new DeveloperPortalSummaryDto
        {
            Products = products.Select(MapProduct).ToList(),
            PartnerApplications = partnerApps.Select(MapPartnerApplication).ToList(),
            WebhookSubscriptions = webhookSubscriptions.Select(MapWebhook).ToList(),
            DeliveryLogs = deliveryLogs.Select(MapDeliveryLog).ToList(),
            EventCatalog = EventCatalog.Select(item => new WebhookEventCatalogItemDto
            {
                EventName = item.EventName,
                Category = item.Category,
                Description = item.Description
            }).ToList(),
            Metrics =
            [
                new() { Key = "products", Label = "Published API Products", Value = products.Count(item => item.Status == "PUBLISHED").ToString(), Severity = "INFO" },
                new() { Key = "partnerApps", Label = "Partner Applications", Value = partnerApps.Count.ToString(), Severity = "INFO" },
                new() { Key = "productionApps", Label = "Production Apps", Value = partnerApps.Count(item => item.Environment == "PRODUCTION").ToString(), Severity = "SUCCESS" },
                new() { Key = "webhooks", Label = "Active Webhooks", Value = webhookSubscriptions.Count(item => item.Status == "ACTIVE").ToString(), Severity = "INFO" },
                new() { Key = "deliveries", Label = "Failed Deliveries (7d)", Value = deliveryLogs.Count(item => item.DeliveryStatus == "FAILED" && item.DeliveredAt >= DateTime.UtcNow.AddDays(-7)).ToString(), Severity = deliveryLogs.Any(item => item.DeliveryStatus == "FAILED") ? "HIGH" : "INFO" }
            ]
        };
    }

    public async Task<PartnerApplicationDto> CreatePartnerApplicationAsync(CreatePartnerApplicationRequest request, string? userId)
    {
        await EnsureSeededAsync();

        var entity = new PartnerApplication
        {
            Id = $"PARTNER-{Guid.NewGuid():N}"[..20],
            Name = request.Name.Trim(),
            PartnerName = request.PartnerName.Trim(),
            CallbackUrl = request.CallbackUrl.Trim(),
            ContactEmail = request.ContactEmail.Trim(),
            ApiProductIdsJson = JsonSerializer.Serialize(request.ApiProductIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList()),
            SandboxKey = GenerateSecret("sbx"),
            Status = "SANDBOX",
            Environment = "SANDBOX",
            LastKeyRotatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PartnerApplications.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync("PARTNER_APPLICATION_CREATED", "PARTNER_APPLICATION", entity.Id, userId, $"Partner application {entity.Name} created.");
        return MapPartnerApplication(entity);
    }

    public async Task<PartnerApplicationDto?> UpdatePartnerApplicationAsync(string id, UpdatePartnerApplicationRequest request, string? userId)
    {
        var entity = await _context.PartnerApplications.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            entity.Name = request.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.PartnerName))
        {
            entity.PartnerName = request.PartnerName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            entity.Status = request.Status.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(request.Environment))
        {
            entity.Environment = request.Environment.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            entity.CallbackUrl = request.CallbackUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ContactEmail))
        {
            entity.ContactEmail = request.ContactEmail.Trim();
        }

        if (request.ApiProductIds is not null)
        {
            entity.ApiProductIdsJson = JsonSerializer.Serialize(request.ApiProductIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync("PARTNER_APPLICATION_UPDATED", "PARTNER_APPLICATION", entity.Id, userId, $"Partner application {entity.Name} updated.");
        return MapPartnerApplication(entity);
    }

    public async Task<PartnerApplicationDto?> RotateSandboxKeyAsync(string id, string? userId)
    {
        var entity = await _context.PartnerApplications.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return null;
        }

        entity.SandboxKey = GenerateSecret("sbx");
        entity.LastKeyRotatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync("PARTNER_APPLICATION_KEY_ROTATED", "PARTNER_APPLICATION", entity.Id, userId, $"Sandbox key rotated for {entity.Name}.");
        return MapPartnerApplication(entity);
    }

    public async Task<PartnerApplicationDto?> PromotePartnerApplicationAsync(string id, PromotePartnerApplicationRequest request, string? userId)
    {
        var entity = await _context.PartnerApplications.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return null;
        }

        entity.Environment = string.IsNullOrWhiteSpace(request.Environment) ? "PRODUCTION" : request.Environment.Trim().ToUpperInvariant();
        entity.Status = "ACTIVE";
        entity.ProductionKey = GenerateSecret("prd");
        entity.ProductionKeyActivatedAt = DateTime.UtcNow;
        entity.LastKeyRotatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync("PARTNER_APPLICATION_PROMOTED", "PARTNER_APPLICATION", entity.Id, userId, $"Partner application {entity.Name} promoted to {entity.Environment}.");
        return MapPartnerApplication(entity);
    }

    public async Task<WebhookSubscriptionDto> CreateWebhookSubscriptionAsync(CreateWebhookSubscriptionRequest request, string? userId)
    {
        var entity = new WebhookSubscription
        {
            Id = $"WH-{Guid.NewGuid():N}"[..20],
            PartnerApplicationId = request.PartnerApplicationId,
            EventName = request.EventName.Trim(),
            TargetUrl = request.TargetUrl.Trim(),
            Status = "ACTIVE",
            SigningSecret = GenerateSecret("whsec"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WebhookSubscriptions.Add(entity);
        await _context.SaveChangesAsync();

        var webhook = await _context.WebhookSubscriptions
            .AsNoTracking()
            .Include(item => item.PartnerApplication)
            .FirstAsync(item => item.Id == entity.Id);

        await _auditLoggingService.LogActionAsync("WEBHOOK_SUBSCRIPTION_CREATED", "WEBHOOK_SUBSCRIPTION", entity.Id, userId, $"Webhook subscription created for {request.EventName}.");
        return MapWebhook(webhook);
    }

    public async Task<WebhookDeliveryLogDto?> ReplayWebhookAsync(ReplayWebhookRequest request, string? userId)
    {
        var subscription = await _context.WebhookSubscriptions.FirstOrDefaultAsync(item => item.Id == request.WebhookSubscriptionId);
        if (subscription == null)
        {
            return null;
        }

        var priorAttempts = await _context.WebhookDeliveryLogs.CountAsync(item => item.WebhookSubscriptionId == subscription.Id && item.EventName == request.EventName);
        var log = new WebhookDeliveryLog
        {
            Id = $"WDL-{Guid.NewGuid():N}"[..20],
            WebhookSubscriptionId = subscription.Id,
            EventName = request.EventName.Trim(),
            DeliveryStatus = "DELIVERED",
            ResponseCode = 202,
            AttemptNumber = priorAttempts + 1,
            DeliveredAt = DateTime.UtcNow
        };

        _context.WebhookDeliveryLogs.Add(log);
        subscription.LastDeliveryAt = log.DeliveredAt;
        subscription.LastDeliveryStatus = log.DeliveryStatus;
        subscription.UpdatedAt = DateTime.UtcNow;

        var partner = await _context.PartnerApplications.FirstOrDefaultAsync(item => item.Id == subscription.PartnerApplicationId);
        if (partner != null)
        {
            partner.LastActivityAt = DateTime.UtcNow;
            partner.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _auditLoggingService.LogActionAsync("WEBHOOK_REPLAY_TRIGGERED", "WEBHOOK_SUBSCRIPTION", subscription.Id, userId, $"Replay triggered for event {request.EventName}.");
        return MapDeliveryLog(log);
    }

    private async Task EnsureSeededAsync()
    {
        if (await _context.ApiProductDefinitions.AnyAsync())
        {
            return;
        }

        var definitions = new[]
        {
            new ApiProductDefinition
            {
                Id = "API-SAVINGS-V1",
                Name = "Digital Savings API",
                Slug = "digital-savings",
                Category = "DEPOSITS",
                Audience = "PARTNER",
                Status = "PUBLISHED",
                Version = "v1",
                AuthModel = "BEARER_TOKEN",
                BasePath = "/api/digital-banking/savings",
                DocumentationPath = "/docs/BANKINSIGHT_PUBLIC_API_INTEGRATION_GUIDE.md",
                RateLimitPerMinute = 180,
                SupportsWebhooks = true,
                SupportsSandbox = true,
                ScopeSummary = "Open, fund, withdraw, and query digital savings accounts.",
                LastPublishedAt = DateTime.UtcNow.Date.AddDays(-14)
            },
            new ApiProductDefinition
            {
                Id = "API-LENDING-V1",
                Name = "Digital Lending API",
                Slug = "digital-lending",
                Category = "LENDING",
                Audience = "PARTNER",
                Status = "PUBLISHED",
                Version = "v1",
                AuthModel = "BEARER_TOKEN",
                BasePath = "/api/digital-banking/loans",
                DocumentationPath = "/docs/BANKINSIGHT_PUBLIC_API_INTEGRATION_GUIDE.md",
                RateLimitPerMinute = 120,
                SupportsWebhooks = true,
                SupportsSandbox = true,
                ScopeSummary = "Check eligibility, apply for loans, repay, and retrieve loan statements.",
                LastPublishedAt = DateTime.UtcNow.Date.AddDays(-10)
            },
            new ApiProductDefinition
            {
                Id = "API-PAYMENTS-V1",
                Name = "Payments and Cheques API",
                Slug = "payments-cheques",
                Category = "PAYMENTS",
                Audience = "PARTNER",
                Status = "PUBLISHED",
                Version = "v1",
                AuthModel = "BEARER_TOKEN",
                BasePath = "/api/payments",
                DocumentationPath = "/docs/BANKINSIGHT_PUBLIC_API_INTEGRATION_GUIDE.md",
                RateLimitPerMinute = 90,
                SupportsWebhooks = true,
                SupportsSandbox = false,
                ScopeSummary = "Initiate bulk payments, issue cheque books, and handle cheque returns.",
                LastPublishedAt = DateTime.UtcNow.Date.AddDays(-7)
            }
        };

        _context.ApiProductDefinitions.AddRange(definitions);
        await _context.SaveChangesAsync();
    }

    private static ApiProductDefinitionDto MapProduct(ApiProductDefinition item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Slug = item.Slug,
        Category = item.Category,
        Audience = item.Audience,
        Status = item.Status,
        Version = item.Version,
        AuthModel = item.AuthModel,
        BasePath = item.BasePath,
        DocumentationPath = item.DocumentationPath,
        RateLimitPerMinute = item.RateLimitPerMinute,
        SupportsWebhooks = item.SupportsWebhooks,
        SupportsSandbox = item.SupportsSandbox,
        ScopeSummary = item.ScopeSummary,
        LastPublishedAt = item.LastPublishedAt
    };

    private static PartnerApplicationDto MapPartnerApplication(PartnerApplication item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        PartnerName = item.PartnerName,
        Status = item.Status,
        Environment = item.Environment,
        CallbackUrl = item.CallbackUrl,
        ContactEmail = item.ContactEmail,
        ApiProductIds = SafeDeserialize(item.ApiProductIdsJson),
        SandboxKeyPreview = MaskSecret(item.SandboxKey),
        ProductionKeyPreview = string.IsNullOrWhiteSpace(item.ProductionKey) ? null : MaskSecret(item.ProductionKey),
        LastKeyRotatedAt = item.LastKeyRotatedAt,
        LastActivityAt = item.LastActivityAt,
        ProductionKeyActivatedAt = item.ProductionKeyActivatedAt
    };

    private static WebhookSubscriptionDto MapWebhook(WebhookSubscription item) => new()
    {
        Id = item.Id,
        PartnerApplicationId = item.PartnerApplicationId,
        PartnerApplicationName = item.PartnerApplication?.Name ?? item.PartnerApplicationId,
        EventName = item.EventName,
        TargetUrl = item.TargetUrl,
        Status = item.Status,
        SigningSecretPreview = MaskSecret(item.SigningSecret),
        LastDeliveryAt = item.LastDeliveryAt,
        LastDeliveryStatus = item.LastDeliveryStatus
    };

    private static WebhookDeliveryLogDto MapDeliveryLog(WebhookDeliveryLog item) => new()
    {
        Id = item.Id,
        WebhookSubscriptionId = item.WebhookSubscriptionId,
        EventName = item.EventName,
        DeliveryStatus = item.DeliveryStatus,
        ResponseCode = item.ResponseCode,
        AttemptNumber = item.AttemptNumber,
        FailureReason = item.FailureReason,
        DeliveredAt = item.DeliveredAt
    };

    private static List<string> SafeDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static string GenerateSecret(string prefix)
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return $"{prefix}_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    private static string MaskSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 8
            ? new string('*', value.Length)
            : $"{value[..4]}...{value[^4..]}";
    }
}
