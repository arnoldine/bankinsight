using System;
using System.Collections.Generic;

namespace BankInsight.API.DTOs
{
    public class ReportParameterSchemaDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public bool Required { get; set; }
        public string? DefaultValue { get; set; }
        public string? Placeholder { get; set; }
        public List<string> Options { get; set; } = new();
    }

    public class ReportCatalogEntryDTO
    {
        public string ReportCode { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ApplicableInstitutionTypes { get; set; } = new();
        public List<string> RequiredPermissions { get; set; } = new();
        public string DataSource { get; set; } = string.Empty;
        public List<ReportParameterSchemaDTO> ParameterSchema { get; set; } = new();
        public string? DefaultSort { get; set; }
        public List<string> DefaultColumns { get; set; } = new();
        public List<string> ExportFormats { get; set; } = new();
        public bool IsRegulatory { get; set; }
        public bool RequiresApprovalBeforeFinalExport { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string Version { get; set; } = "1.0";
        public bool IsActive { get; set; } = true;
        public bool IsFavorite { get; set; }
        public bool SupportsBranchScope { get; set; }
        public bool SupportsHeadOfficeScope { get; set; }
    }

    public class ReportExecutionRequestDTO
    {
        public Dictionary<string, string> Parameters { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "desc";
    }

    public class ReportSummaryMetricDTO
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Helper { get; set; }
    }

    public class ReportExecutionResponseDTO
    {
        public string ReportCode { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public int? RunId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public int TotalRows { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<ReportSummaryMetricDTO> Summary { get; set; } = new();
        public Dictionary<string, string> AppliedFilters { get; set; } = new();
        public List<string> ValidationMessages { get; set; } = new();
        public bool IsMasked { get; set; }
    }

    public class ReportHistoryItemDTO
    {
        public int? RunId { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public int RowCount { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? ExecutionTimeMs { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public string ActionType { get; set; } = "RUN";
    }

    public class ReportFavoriteDTO
    {
        public string ReportCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ReportFilterPresetDTO
    {
        public Guid Id { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public string PresetName { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SaveReportFilterPresetRequestDTO
    {
        public string PresetName { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    public class CrbDataQualityDashboardDTO
    {
        public int TotalChecks { get; set; }
        public int FailedChecks { get; set; }
        public int MissingMandatoryConsumerFields { get; set; }
        public int MissingMandatoryBusinessFields { get; set; }
        public int PendingSubmissionReadinessItems { get; set; }
        public int RejectedRecords { get; set; }
        public int ResubmissionCandidates { get; set; }
        public List<ReportSummaryMetricDTO> Breakdown { get; set; } = new();
    }
}
