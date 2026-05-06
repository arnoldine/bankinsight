using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class DeveloperPortalSummaryDto
{
    public List<ApiProductDefinitionDto> Products { get; set; } = new();
    public List<PartnerApplicationDto> PartnerApplications { get; set; } = new();
    public List<WebhookSubscriptionDto> WebhookSubscriptions { get; set; } = new();
    public List<WebhookDeliveryLogDto> DeliveryLogs { get; set; } = new();
    public List<WebhookEventCatalogItemDto> EventCatalog { get; set; } = new();
    public List<DeveloperPortalMetricDto> Metrics { get; set; } = new();
}

public class DeveloperPortalMetricDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
}

public class ApiProductDefinitionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string AuthModel { get; set; } = string.Empty;
    public string BasePath { get; set; } = string.Empty;
    public string DocumentationPath { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; }
    public bool SupportsWebhooks { get; set; }
    public bool SupportsSandbox { get; set; }
    public string ScopeSummary { get; set; } = string.Empty;
    public DateTime? LastPublishedAt { get; set; }
}

public class PartnerApplicationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public List<string> ApiProductIds { get; set; } = new();
    public string SandboxKeyPreview { get; set; } = string.Empty;
    public string? ProductionKeyPreview { get; set; }
    public DateTime? LastKeyRotatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? ProductionKeyActivatedAt { get; set; }
}

public class WebhookSubscriptionDto
{
    public string Id { get; set; } = string.Empty;
    public string PartnerApplicationId { get; set; } = string.Empty;
    public string PartnerApplicationName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SigningSecretPreview { get; set; } = string.Empty;
    public DateTime? LastDeliveryAt { get; set; }
    public string? LastDeliveryStatus { get; set; }
}

public class WebhookDeliveryLogDto
{
    public string Id { get; set; } = string.Empty;
    public string WebhookSubscriptionId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public int? ResponseCode { get; set; }
    public int AttemptNumber { get; set; }
    public string? FailureReason { get; set; }
    public DateTime DeliveredAt { get; set; }
}

public class WebhookEventCatalogItemDto
{
    public string EventName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreatePartnerApplicationRequest
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string PartnerName { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string CallbackUrl { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContactEmail { get; set; } = string.Empty;

    public List<string> ApiProductIds { get; set; } = new();
}

public class UpdatePartnerApplicationRequest
{
    [StringLength(120)]
    public string? Name { get; set; }

    [StringLength(120)]
    public string? PartnerName { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [StringLength(20)]
    public string? Environment { get; set; }

    [StringLength(255)]
    public string? CallbackUrl { get; set; }

    [StringLength(120)]
    public string? ContactEmail { get; set; }

    public List<string>? ApiProductIds { get; set; }
}

public class PromotePartnerApplicationRequest
{
    [Required]
    [StringLength(20)]
    public string Environment { get; set; } = "PRODUCTION";
}

public class CreateWebhookSubscriptionRequest
{
    [Required, StringLength(50)]
    public string PartnerApplicationId { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string EventName { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string TargetUrl { get; set; } = string.Empty;
}

public class ReplayWebhookRequest
{
    [Required, StringLength(50)]
    public string WebhookSubscriptionId { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string EventName { get; set; } = string.Empty;
}
