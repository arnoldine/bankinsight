using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("collector_portfolio_assignments")]
public class CollectorPortfolioAssignment
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Required]
    [Column("account_id")]
    [MaxLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("collector_staff_id")]
    [MaxLength(50)]
    public string? CollectorStaffId { get; set; }

    [ForeignKey(nameof(CollectorStaffId))]
    public Staff? CollectorStaff { get; set; }

    [Column("loan_product_id")]
    [MaxLength(50)]
    public string? LoanProductId { get; set; }

    [ForeignKey(nameof(LoanProductId))]
    public LoanProduct? LoanProduct { get; set; }

    [Column("collection_type")]
    [MaxLength(30)]
    public string CollectionType { get; set; } = "SUSU_SAVINGS";

    [Column("frequency")]
    [MaxLength(20)]
    public string Frequency { get; set; } = "DAILY";

    [Column("target_amount")]
    public decimal TargetAmount { get; set; }

    [Column("minimum_contribution_amount")]
    public decimal? MinimumContributionAmount { get; set; }

    [Column("route_name")]
    [MaxLength(120)]
    public string? RouteName { get; set; }

    [Column("meeting_day")]
    [MaxLength(20)]
    public string? MeetingDay { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("next_collection_date")]
    public DateOnly? NextCollectionDate { get; set; }

    [Column("last_collection_at")]
    public DateTime? LastCollectionAt { get; set; }

    [Column("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("field_collection_batches")]
public class FieldCollectionBatch
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("collector_staff_id")]
    [MaxLength(50)]
    public string? CollectorStaffId { get; set; }

    [ForeignKey(nameof(CollectorStaffId))]
    public Staff? CollectorStaff { get; set; }

    [Column("branch_id")]
    [MaxLength(50)]
    public string? BranchId { get; set; }

    [Column("batch_date")]
    public DateOnly BatchDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Column("route_name")]
    [MaxLength(120)]
    public string? RouteName { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "OPEN";

    [Column("expected_amount")]
    public decimal ExpectedAmount { get; set; }

    [Column("collected_amount")]
    public decimal CollectedAmount { get; set; }

    [Column("settled_amount")]
    public decimal SettledAmount { get; set; }

    [Column("variance_amount")]
    public decimal VarianceAmount { get; set; }

    [Column("opening_float")]
    public decimal OpeningFloat { get; set; }

    [Column("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [Column("settled_at")]
    public DateTime? SettledAt { get; set; }

    public ICollection<FieldCollectionBatchLine> Lines { get; set; } = new List<FieldCollectionBatchLine>();
}

[Table("field_collection_batch_lines")]
public class FieldCollectionBatchLine
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("batch_id")]
    [MaxLength(50)]
    public string BatchId { get; set; } = string.Empty;

    [ForeignKey(nameof(BatchId))]
    public FieldCollectionBatch? Batch { get; set; }

    [Column("assignment_id")]
    [MaxLength(50)]
    public string? AssignmentId { get; set; }

    [ForeignKey(nameof(AssignmentId))]
    public CollectorPortfolioAssignment? Assignment { get; set; }

    [Required]
    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Required]
    [Column("account_id")]
    [MaxLength(50)]
    public string AccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("loan_id")]
    [MaxLength(50)]
    public string? LoanId { get; set; }

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("transaction_type")]
    [MaxLength(30)]
    public string TransactionType { get; set; } = "SUSU_SAVINGS";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "POSTED";

    [Column("narration")]
    [MaxLength(500)]
    public string Narration { get; set; } = string.Empty;

    [Column("posted_transaction_id")]
    [MaxLength(100)]
    public string? PostedTransactionId { get; set; }

    [Column("due_amount")]
    public decimal? DueAmount { get; set; }

    [Column("was_missed")]
    public bool WasMissed { get; set; }

    [Column("collected_at")]
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
