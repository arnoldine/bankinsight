namespace BankInsight.API.DTOs;

public class OperationsControlCenterDto
{
    public string BusinessDate { get; set; } = string.Empty;
    public string PlatformStatus { get; set; } = "HEALTHY";
    public List<OperationsMetricDto> Metrics { get; set; } = new();
    public List<OperationsWorkItemDto> WorkItems { get; set; } = new();
}

public class OperationsMetricDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public string? Subtitle { get; set; }
}

public class OperationsWorkItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? RouteHint { get; set; }
    public decimal? Amount { get; set; }
    public int? Count { get; set; }
    public string? Reference { get; set; }
}
