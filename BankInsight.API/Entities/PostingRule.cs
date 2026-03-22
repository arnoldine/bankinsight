using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("posting_rules")]
public class PostingRule
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("event_type")]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Column("debit_account_code")]
    [MaxLength(50)]
    public string DebitAccountCode { get; set; } = string.Empty;

    [Required]
    [Column("credit_account_code")]
    [MaxLength(50)]
    public string CreditAccountCode { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("priority")]
    public int Priority { get; set; } = 0; // For fallback rules if needed

    [Column("description")]
    [MaxLength(255)]
    public string? Description { get; set; }
    
    // Optional script definition to resolve dynamic GL codes based on product/branch
    [Column("gl_resolution_script")]
    public string? GlResolutionScript { get; set; }
}
