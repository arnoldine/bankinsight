using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CashIncidentDto
{
    public string Id { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string StoreType { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string IncidentType { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "OPEN";
    public string? Reference { get; set; }
    public string? Narration { get; set; }
    public string? ReportedBy { get; set; }
    public string? ReportedByName { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class CreateCashIncidentRequest
{
    [Required]
    public string BranchId { get; set; } = string.Empty;

    [Required]
    public string StoreType { get; set; } = string.Empty;

    [Required]
    public string StoreId { get; set; } = string.Empty;

    [Required]
    public string IncidentType { get; set; } = string.Empty;

    public string Currency { get; set; } = "GHS";

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    public string? Reference { get; set; }
    public string? Narration { get; set; }
}

public class ResolveCashIncidentRequest
{
    [Required]
    public string ResolutionNote { get; set; } = string.Empty;
}
