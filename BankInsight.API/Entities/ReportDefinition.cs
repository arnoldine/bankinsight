using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities
{
    [Table("report_definitions")]
    public class ReportDefinition
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("report_code")]
        [StringLength(50)]
        [Required]
        public string ReportCode { get; set; }

        [Column("report_name")]
        [StringLength(255)]
        [Required]
        public string ReportName { get; set; }

        [Column("description")]
        [StringLength(1000)]
        public string Description { get; set; }

        [Column("report_type")]
        [StringLength(50)]
        [Required]
        public string ReportType { get; set; } // "Regulatory", "Financial", "Analytics", "Operational"

        [Column("data_source")]
        [StringLength(255)]
        public string DataSource { get; set; } // Table names or stored procedure

        [Column("frequency")]
        [StringLength(50)]
        public string Frequency { get; set; } // "Daily", "Weekly", "Monthly", "Quarterly", "Annual", "OnDemand"

        [Column("template_format")]
        [StringLength(50)]
        public string TemplateFormat { get; set; } // "Excel", "PDF", "CSV", "JSON"

        [Column("template_content")]
        public string TemplateContent { get; set; } // Liquid template

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("requires_approval")]
        public bool RequiresApproval { get; set; } = false;

        [Column("created_by")]
        [StringLength(50)]
        public string CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<ReportParameter> Parameters { get; set; } = new List<ReportParameter>();
        public virtual ICollection<ReportSchedule> Schedules { get; set; } = new List<ReportSchedule>();
        public virtual ICollection<ReportRun> Runs { get; set; } = new List<ReportRun>();
        public virtual ICollection<ReportSubscription> Subscriptions { get; set; } = new List<ReportSubscription>();
    }

    [Table("report_parameters")]
    public class ReportParameter
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("report_definition_id")]
        [Required]
        public int ReportDefinitionId { get; set; }

        [Column("parameter_name")]
        [StringLength(50)]
        [Required]
        public string ParameterName { get; set; }

        [Column("parameter_type")]
        [StringLength(50)]
        [Required]
        public string ParameterType { get; set; } // "String", "Date", "DateRange", "Number", "Currency", "Dropdown"

        [Column("default_value")]
        public string DefaultValue { get; set; }

        [Column("is_required")]
        public bool IsRequired { get; set; }

        [Column("display_label")]
        [StringLength(100)]
        public string DisplayLabel { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        // Navigation
        [ForeignKey(nameof(ReportDefinitionId))]
        public virtual ReportDefinition ReportDefinition { get; set; }
    }
}
