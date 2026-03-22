using System;

namespace BankInsight.API.DTOs;

public class LoanApprovalDetailsDto
{
    public string LoanId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Principal { get; set; }
    public decimal? OutstandingBalance { get; set; }
    public decimal Rate { get; set; }
    public int TermMonths { get; set; }
    public string? CollateralType { get; set; }
    public decimal? CollateralValue { get; set; }
    public string? ParBucket { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }
}

public class ApprovalRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string? WorkflowId { get; set; }
    public string? WorkflowName { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? RequesterId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Remarks { get; set; }
    public string? ReferenceNo { get; set; }
    public string? PayloadJson { get; set; }
    public LoanApprovalDetailsDto? LoanDetails { get; set; }
}

public class CreateApprovalRequest
{
    public string? WorkflowId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? RequesterId { get; set; }
    public string? PayloadJson { get; set; }
    public string? Remarks { get; set; }
    public string? ReferenceNo { get; set; }
}

public class UpdateApprovalRequest
{
    public string Status { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public string? Remarks { get; set; }
}

