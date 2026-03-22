using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("branch_limits")]
public class BranchLimit
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = string.Empty;

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Required]
    [Column("limit_type")]
    [MaxLength(50)]
    public string LimitType { get; set; } = string.Empty;

    [Column("transaction_type")]
    [MaxLength(50)]
    public string? TransactionType { get; set; }

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("single_transaction_limit")]
    public decimal? SingleTransactionLimit { get; set; }

    [Column("daily_limit")]
    public decimal? DailyLimit { get; set; }

    [Column("monthly_limit")]
    public decimal? MonthlyLimit { get; set; }

    [Column("requires_approval")]
    public bool RequiresApproval { get; set; } = false;

    [Column("approval_threshold")]
    public decimal? ApprovalThreshold { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
