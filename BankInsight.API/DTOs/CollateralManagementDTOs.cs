using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CollateralManagementSummaryDto
{
    public List<CollateralRecordDto> CollateralItems { get; set; } = new();
    public List<CovenantRecordDto> Covenants { get; set; } = new();
    public int ExpiringValuationsCount { get; set; }
    public int OverdueCovenantsCount { get; set; }
}

public class CollateralRecordDto
{
    public string Id { get; set; } = string.Empty;
    public string LoanId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CollateralType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal RegisteredValue { get; set; }
    public decimal CurrentValuation { get; set; }
    public DateTime? ValuationDate { get; set; }
    public DateTime? ValuationExpiryDate { get; set; }
    public string PerfectionStatus { get; set; } = string.Empty;
    public string? DocumentReference { get; set; }
    public string? CustodyLocation { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CovenantRecordDto
{
    public string Id { get; set; } = string.Empty;
    public string LoanId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CovenantType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public string Detail { get; set; } = string.Empty;
}

public class UpdateCollateralRecordRequest
{
    [Range(0, 999999999.99)]
    public decimal? CurrentValuation { get; set; }

    public DateTime? ValuationDate { get; set; }
    public DateTime? ValuationExpiryDate { get; set; }

    [StringLength(30)]
    public string? PerfectionStatus { get; set; }

    [StringLength(100)]
    public string? DocumentReference { get; set; }

    [StringLength(100)]
    public string? CustodyLocation { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }
}

public class UpdateCovenantRecordRequest
{
    [StringLength(20)]
    public string? Status { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime? LastReviewedAt { get; set; }

    [StringLength(1000)]
    public string? Detail { get; set; }
}
