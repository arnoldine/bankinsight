using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("customer_credentials")]
public class CustomerCredential
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
    [Column("login_email")]
    [MaxLength(100)]
    public string LoginEmail { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("mfa_enabled")]
    public bool MfaEnabled { get; set; } = true;

    [Column("transaction_pin_hash")]
    [MaxLength(255)]
    public string? TransactionPinHash { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
