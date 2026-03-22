using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("accounts")]
public class Account
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("customer_id")]
    [MaxLength(50)]
    public string? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("branch_id")]
    [MaxLength(50)]
    public string? BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("product_code")]
    [MaxLength(50)]
    public string? ProductCode { get; set; }

    [ForeignKey(nameof(ProductCode))]
    public Product? Product { get; set; }

    [Required]
    [Column("type")]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("balance")]
    public decimal Balance { get; set; } = 0;

    [Column("lien_amount")]
    public decimal LienAmount { get; set; } = 0;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("last_trans_date")]
    public DateTime? LastTransDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
