using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("client_merchant_profiles")]
public class ClientMerchantProfile
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Required]
    [Column("settlement_account_id")]
    [MaxLength(50)]
    public string SettlementAccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(SettlementAccountId))]
    public Account? SettlementAccount { get; set; }

    [Required]
    [Column("merchant_code")]
    [MaxLength(50)]
    public string MerchantCode { get; set; } = string.Empty;

    [Required]
    [Column("display_name")]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Column("category")]
    [MaxLength(100)]
    public string Category { get; set; } = "General";

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("qr_scheme")]
    [MaxLength(30)]
    public string QrScheme { get; set; } = "BANKINSIGHT_QR";

    [Column("qr_payload")]
    public string QrPayload { get; set; } = string.Empty;

    [Column("ghqr_ready")]
    public bool GhQrReady { get; set; }

    [Column("accepts_app_payments")]
    public bool AcceptsAppPayments { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_payment_at")]
    public DateTime? LastPaymentAt { get; set; }
}
