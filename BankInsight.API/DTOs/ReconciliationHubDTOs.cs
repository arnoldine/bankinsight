using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class ReconciliationHubSummaryDto
{
    public List<ReconciliationMetricDto> Metrics { get; set; } = new();
    public List<ReconciliationExceptionDto> Exceptions { get; set; } = new();
    public List<SettlementInstructionDto> SettlementInstructions { get; set; } = new();
}

public class ReconciliationMetricDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
}

public class ReconciliationExceptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public string? OwnerUserId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? WorkflowStage { get; set; }
    public string? ResolutionCode { get; set; }
}

public class UpdateReconciliationExceptionRequest
{
    [StringLength(20)]
    public string? Status { get; set; }

    [StringLength(50)]
    public string? OwnerUserId { get; set; }

    [StringLength(2000)]
    public string? Detail { get; set; }

    [StringLength(40)]
    public string? WorkflowStage { get; set; }

    [StringLength(40)]
    public string? ResolutionCode { get; set; }
}

public class SettlementInstructionDto
{
    public string Id { get; set; } = string.Empty;
    public string ReconciliationExceptionId { get; set; } = string.Empty;
    public string InstructionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "GHS";
    public decimal Amount { get; set; }
    public string? SettlementAccount { get; set; }
    public string? Counterparty { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateSettlementInstructionRequest
{
    [Required]
    [StringLength(50)]
    public string ReconciliationExceptionId { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string InstructionType { get; set; } = string.Empty;

    [StringLength(10)]
    public string Currency { get; set; } = "GHS";

    public decimal Amount { get; set; }

    [StringLength(80)]
    public string? SettlementAccount { get; set; }

    [StringLength(120)]
    public string? Counterparty { get; set; }

    public DateTime? DueAt { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class RetryReconciliationExceptionRequest
{
    [StringLength(2000)]
    public string? Detail { get; set; }
}
