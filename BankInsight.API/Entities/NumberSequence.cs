using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

public class NumberSequence
{
    [Key]
    [Required]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty; // e.g., "CIF-2603", "CASA-00120", "LNO-0014026", "GL-101"

    [Required]
    public int NextValue { get; set; } = 1;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
