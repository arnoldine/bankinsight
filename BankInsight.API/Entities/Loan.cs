using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("loans")]
public class Loan
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("customer_id")]
    [MaxLength(50)]
    public string? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("group_id")]
    [MaxLength(50)]
    public string? GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    [Column("product_code")]
    [MaxLength(50)]
    public string? ProductCode { get; set; }

    [ForeignKey(nameof(ProductCode))]
    public Product? Product { get; set; }

    [Column("loan_product_id")]
    [MaxLength(50)]
    public string? LoanProductId { get; set; }

    [ForeignKey(nameof(LoanProductId))]
    public LoanProduct? LoanProduct { get; set; }

    [Required]
    [Column("principal")]
    public decimal Principal { get; set; }

    [Required]
    [Column("rate")]
    public decimal Rate { get; set; }

    [Required]
    [Column("term_months")]
    public int TermMonths { get; set; }

    [Column("interest_method")]
    [MaxLength(30)]
    public string InterestMethod { get; set; } = "Flat";

    [Column("repayment_frequency")]
    [MaxLength(20)]
    public string RepaymentFrequency { get; set; } = "Monthly";

    [Column("schedule_type")]
    [MaxLength(20)]
    public string ScheduleType { get; set; } = "Monthly";

    [Column("disbursement_date")]
    public DateOnly? DisbursementDate { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("application_date")]
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("approved_by")]
    [MaxLength(50)]
    public string? ApprovedBy { get; set; }

    [Column("maker_id")]
    [MaxLength(50)]
    public string? MakerId { get; set; }

    [Column("checker_id")]
    [MaxLength(50)]
    public string? CheckerId { get; set; }

    [Column("disbursed_at")]
    public DateTime? DisbursedAt { get; set; }

    [Column("outstanding_balance")]
    public decimal? OutstandingBalance { get; set; }

    [Column("collateral_type")]
    [MaxLength(50)]
    public string? CollateralType { get; set; }

    [Column("collateral_value")]
    public decimal? CollateralValue { get; set; }

    [Column("par_bucket")]
    [MaxLength(20)]
    public string ParBucket { get; set; } = "0";

    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = "BR001";

    public ICollection<LoanSchedule> Schedules { get; set; } = new List<LoanSchedule>();
    public ICollection<LoanRepayment> Repayments { get; set; } = new List<LoanRepayment>();
}

[Table("loan_schedules")]
public class LoanSchedule
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("loan_id")]
    [MaxLength(50)]
    public string? LoanId { get; set; }

    [ForeignKey(nameof(LoanId))]
    public Loan? Loan { get; set; }

    [Column("period")]
    public int? Period { get; set; }

    [Column("due_date")]
    public DateOnly? DueDate { get; set; }

    [Column("principal")]
    public decimal? Principal { get; set; }

    [Column("interest")]
    public decimal? Interest { get; set; }

    [Column("total")]
    public decimal? Total { get; set; }

    [Column("balance")]
    public decimal? Balance { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string? Status { get; set; }

    [Column("paid_amount")]
    public decimal? PaidAmount { get; set; }

    [Column("paid_date")]
    public DateOnly? PaidDate { get; set; }
}
