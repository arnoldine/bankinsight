using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CollectionCaseDto
{
    public string Id { get; set; } = string.Empty;
    public string LoanId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string RecoveryStage { get; set; } = string.Empty;
    public int DelinquencyDays { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal AmountInArrears { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? NextActionDate { get; set; }
    public DateTime? PromiseToPayDate { get; set; }
    public decimal? PromiseToPayAmount { get; set; }
    public DateTime? LastContactAt { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public DateTime? NextEscalationDate { get; set; }
    public string? Notes { get; set; }
    public string? RecoveryStrategy { get; set; }
    public string? LegalStatus { get; set; }
    public decimal? SettlementAmount { get; set; }
    public DateTime? SettlementExpiryDate { get; set; }
    public string? AssignedAgency { get; set; }
    public string? RepossessionStatus { get; set; }
    public string? ApprovalStatus { get; set; }
    public decimal? WriteOffRecommendedAmount { get; set; }
    public string? WriteOffReason { get; set; }
    public List<CollectionCaseEventDto> Events { get; set; } = new();
}

public class CollectionCaseEventDto
{
    public string EventType { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateCollectionCaseRequest
{
    [StringLength(20)]
    public string? Status { get; set; }

    [StringLength(20)]
    public string? Priority { get; set; }

    [StringLength(30)]
    public string? RecoveryStage { get; set; }

    [StringLength(50)]
    public string? AssignedTo { get; set; }

    public DateTime? NextActionDate { get; set; }
    public DateTime? PromiseToPayDate { get; set; }
    public decimal? PromiseToPayAmount { get; set; }
    public decimal? SettlementAmount { get; set; }
    public DateTime? SettlementExpiryDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? RecoveryStrategy { get; set; }

    [StringLength(30)]
    public string? LegalStatus { get; set; }

    [StringLength(120)]
    public string? AssignedAgency { get; set; }

    [StringLength(30)]
    public string? RepossessionStatus { get; set; }

    [StringLength(30)]
    public string? ApprovalStatus { get; set; }

    public decimal? WriteOffRecommendedAmount { get; set; }

    [StringLength(500)]
    public string? WriteOffReason { get; set; }

    [Required]
    [StringLength(30)]
    public string EventType { get; set; } = "NOTE";

    [Required]
    [StringLength(2000)]
    public string Detail { get; set; } = string.Empty;
}

public class ExecuteCollectionActionRequest
{
    [Required]
    [StringLength(40)]
    public string ActionType { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Detail { get; set; }

    public DateTime? PromiseToPayDate { get; set; }
    public decimal? PromiseToPayAmount { get; set; }
    public decimal? SettlementAmount { get; set; }
    public DateTime? SettlementExpiryDate { get; set; }
    public DateTime? NextActionDate { get; set; }
    public string? AssignedAgency { get; set; }
    public string? WriteOffReason { get; set; }
}
