using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("collateral_records")]
public class CollateralRecord
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("loan_id")]
    [MaxLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("collateral_type")]
    [MaxLength(50)]
    public string CollateralType { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column("registered_value")]
    public decimal RegisteredValue { get; set; }

    [Column("current_valuation")]
    public decimal CurrentValuation { get; set; }

    [Column("valuation_date")]
    public DateTime? ValuationDate { get; set; }

    [Column("valuation_expiry_date")]
    public DateTime? ValuationExpiryDate { get; set; }

    [Column("perfection_status")]
    [MaxLength(30)]
    public string PerfectionStatus { get; set; } = "PENDING";

    [Column("document_reference")]
    [MaxLength(100)]
    public string? DocumentReference { get; set; }

    [Column("custody_location")]
    [MaxLength(100)]
    public string? CustodyLocation { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("covenant_records")]
public class CovenantRecord
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("loan_id")]
    [MaxLength(50)]
    public string LoanId { get; set; } = string.Empty;

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("name")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("covenant_type")]
    [MaxLength(30)]
    public string CovenantType { get; set; } = "REPORTING";

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("last_reviewed_at")]
    public DateTime? LastReviewedAt { get; set; }

    [Column("detail")]
    [MaxLength(1000)]
    public string Detail { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
