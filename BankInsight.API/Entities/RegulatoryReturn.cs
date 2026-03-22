using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities
{
    public enum ReturnType
    {
        DailyPosition,
        MonthlyReturn1,
        MonthlyReturn2,
        MonthlyReturn3,
        PrudentialReturn,
        LargeExposure,
        SuspiciousTransaction,
        AMLReturn
    }

    [Table("regulatory_returns")]
    public class RegulatoryReturn
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("return_type")]
        [StringLength(50)]
        [Required]
        public string ReturnType { get; set; } // ReturnType enum as string

        [Column("return_date")]
        [Required]
        public DateTime ReturnDate { get; set; }

        [Column("reporting_period_start")]
        public DateTime ReportingPeriodStart { get; set; }

        [Column("reporting_period_end")]
        public DateTime ReportingPeriodEnd { get; set; }

        [Column("submission_status")]
        [StringLength(50)]
        public string SubmissionStatus { get; set; } = "Draft"; // "Draft", "Submitted", "Accepted", "Rejected"

        [Column("submission_date")]
        public DateTime? SubmissionDate { get; set; }

        [Column("bog_reference_number")]
        [StringLength(100)]
        public string BogReferenceNumber { get; set; }

        [Column("submitted_by")]
        [StringLength(50)]
        public string SubmittedBy { get; set; }

        [Column("total_records")]
        public int TotalRecords { get; set; }

        [Column("file_path")]
        [StringLength(500)]
        public string FilePath { get; set; }

        [Column("file_format")]
        [StringLength(50)]
        public string FileFormat { get; set; } // "Excel", "PDF", "XML"

        [Column("validation_errors")]
        public string ValidationErrors { get; set; } // JSON array of errors

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("data_extracts")]
    public class DataExtract
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("extract_name")]
        [StringLength(255)]
        [Required]
        public string ExtractName { get; set; }

        [Column("extract_type")]
        [StringLength(100)]
        [Required]
        public string ExtractType { get; set; } // "CustomerData", "TransactionData", "LoanData", "PositionData"

        [Column("extract_date")]
        [Required]
        public DateTime ExtractDate { get; set; }

        [Column("record_count")]
        public int RecordCount { get; set; }

        [Column("file_path")]
        [StringLength(500)]
        public string FilePath { get; set; }

        [Column("file_format")]
        [StringLength(50)]
        public string FileFormat { get; set; } // "CSV", "Excel", "XML", "JSON"

        [Column("created_by")]
        [StringLength(50)]
        public string CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_archived")]
        public bool IsArchived { get; set; } = false;
    }
}
