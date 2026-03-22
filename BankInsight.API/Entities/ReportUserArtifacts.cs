using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities
{
    [Table("report_favorites")]
    public class ReportFavorite
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("staff_id")]
        [MaxLength(50)]
        [Required]
        public string StaffId { get; set; } = string.Empty;

        [Column("report_code")]
        [MaxLength(100)]
        [Required]
        public string ReportCode { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("report_filter_presets")]
    public class ReportFilterPreset
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("staff_id")]
        [MaxLength(50)]
        [Required]
        public string StaffId { get; set; } = string.Empty;

        [Column("report_code")]
        [MaxLength(100)]
        [Required]
        public string ReportCode { get; set; } = string.Empty;

        [Column("preset_name")]
        [MaxLength(150)]
        [Required]
        public string PresetName { get; set; } = string.Empty;

        [Column("parameters_json", TypeName = "jsonb")]
        [Required]
        public string ParametersJson { get; set; } = "{}";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
