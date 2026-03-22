using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("branch_vaults")]
public class BranchVault
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = string.Empty;

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("cash_on_hand")]
    public decimal CashOnHand { get; set; } = 0;

    [Column("vault_limit")]
    public decimal? VaultLimit { get; set; }

    [Column("min_balance")]
    public decimal? MinBalance { get; set; }

    [Column("last_count_date")]
    public DateTime? LastCountDate { get; set; }

    [Column("last_count_by")]
    [MaxLength(50)]
    public string? LastCountBy { get; set; }

    [ForeignKey(nameof(LastCountBy))]
    public Staff? LastCountByStaff { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
