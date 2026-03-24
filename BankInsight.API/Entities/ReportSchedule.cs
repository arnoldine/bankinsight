using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities
{
    [Table("report_schedules")]
    public class ReportSchedule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("report_definition_id")]
        [Required]
        public int ReportDefinitionId { get; set; }

        [Column("schedule_name")]
        [StringLength(255)]
        [Required]
        public string ScheduleName { get; set; } = string.Empty;

        [Column("cron_expression")]
        [StringLength(100)]
        [Required]
        public string CronExpression { get; set; } = string.Empty; // Hangfire CRON format

        [Column("frequency")]
        [StringLength(50)]
        public string Frequency { get; set; } = string.Empty; // "Daily", "Weekly", "Monthly", "Custom"

        [Column("day_of_week")]
        public int? DayOfWeek { get; set; } // 0=Sunday, 1=Monday, etc. (for Weekly)

        [Column("day_of_month")]
        public int? DayOfMonth { get; set; } // 1-31 (for Monthly)

        [Column("time_of_day")]
        public TimeSpan TimeOfDay { get; set; } = new TimeSpan(8, 0, 0); // Default 8:00 AM

        [Column("is_enabled")]
        public bool IsEnabled { get; set; } = true;

        [Column("last_run_at")]
        public DateTime? LastRunAt { get; set; }

        [Column("next_run_at")]
        public DateTime? NextRunAt { get; set; }

        [Column("hangfire_job_id")]
        [StringLength(50)]
        public string HangfireJobId { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ReportDefinitionId))]
        public virtual ReportDefinition ReportDefinition { get; set; } = null!;
    }

    [Table("report_runs")]
    public class ReportRun
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("report_definition_id")]
        [Required]
        public int ReportDefinitionId { get; set; }

        [Column("schedule_id")]
        public int? ScheduleId { get; set; }

        [Column("run_by")]
        [StringLength(50)]
        public string RunBy { get; set; } = string.Empty;

        [Column("started_at")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("status")]
        [StringLength(50)]
        [Required]
        public string Status { get; set; } = "Pending"; // "Pending", "Running", "Completed", "Failed"

        [Column("file_name")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Column("file_path")]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Column("file_size_bytes")]
        public long? FileSizeBytes { get; set; }

        [Column("format")]
        [StringLength(50)]
        public string Format { get; set; } = string.Empty; // "Excel", "PDF", "CSV", "JSON"

        [Column("row_count")]
        public int RowCount { get; set; }

        [Column("error_message")]
        public string ErrorMessage { get; set; } = string.Empty;

        [Column("execution_time_ms")]
        public long? ExecutionTimeMs { get; set; }

        // Navigation
        [ForeignKey(nameof(ReportDefinitionId))]
        public virtual ReportDefinition ReportDefinition { get; set; } = null!;
    }

    [Table("report_subscriptions")]
    public class ReportSubscription
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("report_definition_id")]
        [Required]
        public int ReportDefinitionId { get; set; }

        [Column("staff_id")]
        [StringLength(50)]
        [Required]
        public string StaffId { get; set; } = string.Empty;

        [Column("email_address")]
        [StringLength(255)]
        [Required]
        public string EmailAddress { get; set; } = string.Empty;

        [Column("delivery_frequency")]
        [StringLength(50)]
        [Required]
        public string DeliveryFrequency { get; set; } = string.Empty; // "Daily", "Weekly", "Monthly",  "OnCompletion"

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ReportDefinitionId))]
        public virtual ReportDefinition ReportDefinition { get; set; } = null!;
    }
}
