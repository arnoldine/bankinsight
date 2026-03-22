using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("journal_entries")]
public class JournalEntry
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("date")]
    public DateOnly? Date { get; set; }

    [Column("reference")]
    [MaxLength(100)]
    public string? Reference { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("posted_by")]
    [MaxLength(50)]
    public string? PostedBy { get; set; }

    [ForeignKey(nameof(PostedBy))]
    public Staff? PostedByStaff { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "POSTED";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}

[Table("journal_lines")]
public class JournalLine
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("journal_id")]
    [MaxLength(50)]
    public string? JournalId { get; set; }

    [ForeignKey(nameof(JournalId))]
    public JournalEntry? Journal { get; set; }

    [Column("account_code")]
    [MaxLength(20)]
    public string? AccountCode { get; set; }

    [ForeignKey(nameof(AccountCode))]
    public GlAccount? Account { get; set; }

    [Column("debit")]
    public decimal Debit { get; set; } = 0;

    [Column("credit")]
    public decimal Credit { get; set; } = 0;
}
