using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("client_standing_orders")]
public class ClientStandingOrder
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("source_account_id")]
    [MaxLength(50)]
    public string SourceAccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(SourceAccountId))]
    public Account? SourceAccount { get; set; }

    [Column("instruction_type")]
    [MaxLength(30)]
    public string InstructionType { get; set; } = "INTERNAL_TRANSFER";

    [Column("merchant_code")]
    [MaxLength(50)]
    public string? MerchantCode { get; set; }

    [Column("merchant_name")]
    [MaxLength(200)]
    public string? MerchantName { get; set; }

    [Column("destination_account_id")]
    [MaxLength(50)]
    public string? DestinationAccountId { get; set; }

    [ForeignKey(nameof(DestinationAccountId))]
    public Account? DestinationAccount { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("frequency")]
    [MaxLength(20)]
    public string Frequency { get; set; } = "MONTHLY";

    [Column("narration")]
    [MaxLength(500)]
    public string Narration { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("next_run_at")]
    public DateTime NextRunAt { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("last_run_at")]
    public DateTime? LastRunAt { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
