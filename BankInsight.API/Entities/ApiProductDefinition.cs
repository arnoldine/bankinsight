using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("api_product_definitions")]
public class ApiProductDefinition
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Column("slug")]
    [MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [Column("category")]
    [MaxLength(40)]
    public string Category { get; set; } = string.Empty;

    [Column("audience")]
    [MaxLength(40)]
    public string Audience { get; set; } = "PARTNER";

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PUBLISHED";

    [Column("version")]
    [MaxLength(20)]
    public string Version { get; set; } = "v1";

    [Column("auth_model")]
    [MaxLength(40)]
    public string AuthModel { get; set; } = "BEARER_TOKEN";

    [Column("base_path")]
    [MaxLength(120)]
    public string BasePath { get; set; } = string.Empty;

    [Column("documentation_path")]
    [MaxLength(255)]
    public string DocumentationPath { get; set; } = string.Empty;

    [Column("rate_limit_per_minute")]
    public int RateLimitPerMinute { get; set; } = 120;

    [Column("supports_webhooks")]
    public bool SupportsWebhooks { get; set; }

    [Column("supports_sandbox")]
    public bool SupportsSandbox { get; set; } = true;

    [Column("scope_summary")]
    [MaxLength(1000)]
    public string ScopeSummary { get; set; } = string.Empty;

    [Column("last_published_at")]
    public DateTime? LastPublishedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("partner_applications")]
public class PartnerApplication
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Column("partner_name")]
    [MaxLength(120)]
    public string PartnerName { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "SANDBOX";

    [Column("environment")]
    [MaxLength(20)]
    public string Environment { get; set; } = "SANDBOX";

    [Column("callback_url")]
    [MaxLength(255)]
    public string CallbackUrl { get; set; } = string.Empty;

    [Column("contact_email")]
    [MaxLength(120)]
    public string ContactEmail { get; set; } = string.Empty;

    [Column("api_product_ids_json")]
    public string ApiProductIdsJson { get; set; } = "[]";

    [Column("sandbox_key")]
    [MaxLength(120)]
    public string SandboxKey { get; set; } = string.Empty;

    [Column("production_key")]
    [MaxLength(120)]
    public string? ProductionKey { get; set; }

    [Column("production_key_activated_at")]
    public DateTime? ProductionKeyActivatedAt { get; set; }

    [Column("last_key_rotated_at")]
    public DateTime? LastKeyRotatedAt { get; set; }

    [Column("last_activity_at")]
    public DateTime? LastActivityAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("webhook_subscriptions")]
public class WebhookSubscription
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("partner_application_id")]
    [MaxLength(50)]
    public string PartnerApplicationId { get; set; } = string.Empty;

    [ForeignKey(nameof(PartnerApplicationId))]
    public PartnerApplication? PartnerApplication { get; set; }

    [Column("event_name")]
    [MaxLength(80)]
    public string EventName { get; set; } = string.Empty;

    [Column("target_url")]
    [MaxLength(255)]
    public string TargetUrl { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("signing_secret")]
    [MaxLength(120)]
    public string SigningSecret { get; set; } = string.Empty;

    [Column("last_delivery_at")]
    public DateTime? LastDeliveryAt { get; set; }

    [Column("last_delivery_status")]
    [MaxLength(20)]
    public string? LastDeliveryStatus { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
