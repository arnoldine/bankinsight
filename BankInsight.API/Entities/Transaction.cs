using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("transactions")]
public class Transaction
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("account_id")]
    [MaxLength(50)]
    public string? AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Required]
    [Column("type")]
    [MaxLength(30)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("narration")]
    [MaxLength(255)]
    public string? Narration { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("reference")]
    [MaxLength(100)]
    public string? Reference { get; set; }

    [Column("teller_id")]
    [MaxLength(50)]
    public string? TellerId { get; set; }

    [ForeignKey(nameof(TellerId))]
    public Staff? Teller { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "POSTED";
}
