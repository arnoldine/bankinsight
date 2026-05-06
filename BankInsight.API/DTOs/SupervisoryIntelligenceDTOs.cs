namespace BankInsight.API.DTOs;

public class SupervisoryMetricDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public string? Subtitle { get; set; }
}

public class RelationshipBankingSummaryDto
{
    public List<SupervisoryMetricDto> Metrics { get; set; } = new();
    public List<RelationshipCustomerItemDto> TopRelationships { get; set; } = new();
    public List<RelationshipManagerPerformanceItemDto> ManagerPerformance { get; set; } = new();
    public List<RelationshipEngagementItemDto> RecentEngagements { get; set; } = new();
    public List<AssignableStaffItemDto> AssignableStaff { get; set; } = new();
}

public class RelationshipCustomerItemDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Segment { get; set; } = "Retail";
    public int ActiveAccountCount { get; set; }
    public int ActiveLoanCount { get; set; }
    public int ActiveInvestmentCount { get; set; }
    public decimal DepositBalance { get; set; }
    public decimal LoanExposure { get; set; }
    public decimal InvestmentBalance { get; set; }
    public decimal EstimatedRelationshipValue { get; set; }
    public decimal EstimatedAnnualRevenue { get; set; }
    public int HouseholdOrGroupLinks { get; set; }
    public int OpenComplaintCount { get; set; }
    public string RiskSummary { get; set; } = "LOW";
    public string? RelationshipOwnerUserId { get; set; }
    public string RelationshipOwner { get; set; } = "Unassigned";
    public DateTime? LastEngagementAt { get; set; }
}

public class RelationshipPortfolioDetailDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Segment { get; set; } = "Retail";
    public string RiskSummary { get; set; } = "LOW";
    public string? RelationshipOwnerUserId { get; set; }
    public string RelationshipOwner { get; set; } = "Unassigned";
    public decimal DepositBalance { get; set; }
    public decimal InvestmentBalance { get; set; }
    public decimal LoanExposure { get; set; }
    public decimal EstimatedAnnualRevenue { get; set; }
    public decimal EstimatedRelationshipValue { get; set; }
    public DateTime? LastEngagementAt { get; set; }
    public int OpenComplaintCount { get; set; }
    public int HouseholdOrGroupLinks { get; set; }
    public List<RelationshipPortfolioBreakdownItemDto> ProductBreakdown { get; set; } = new();
    public List<RelationshipEngagementItemDto> RecentEngagements { get; set; } = new();
}

public class RelationshipPortfolioBreakdownItemDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Balance { get; set; }
    public decimal Contribution { get; set; }
}

public class RelationshipEngagementItemDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public DateTime OccurredAt { get; set; }
}

public class RelationshipManagerPerformanceItemDto
{
    public string RelationshipOwner { get; set; } = "Unassigned";
    public int CustomerCount { get; set; }
    public decimal DepositBalance { get; set; }
    public decimal LoanExposure { get; set; }
    public decimal EstimatedAnnualRevenue { get; set; }
    public int HighRiskRelationships { get; set; }
    public int OpenComplaintCount { get; set; }
}

public class DigitalChannelOperationsSummaryDto
{
    public List<SupervisoryMetricDto> Metrics { get; set; } = new();
    public List<DigitalChannelMetricDto> ChannelMetrics { get; set; } = new();
    public List<DigitalSessionRiskItemDto> SessionRiskItems { get; set; } = new();
    public List<DigitalComplaintItemDto> ComplaintQueue { get; set; } = new();
    public List<DigitalKycItemDto> KycQueue { get; set; } = new();
}

public class DigitalChannelMetricDto
{
    public string ChannelName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TransactionVolume { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class DigitalSessionRiskItemDto
{
    public string SessionId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string RiskLabel { get; set; } = "LOW";
}

public class DigitalComplaintItemDto
{
    public string ComplaintId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerTeam { get; set; } = string.Empty;
    public DateTime SlaDueAt { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class DigitalKycItemDto
{
    public string KycCaseId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? ReviewerName { get; set; }
}

public class RegulatoryIntelligenceSummaryDto
{
    public List<SupervisoryMetricDto> Metrics { get; set; } = new();
    public OrassReadinessDto Readiness { get; set; } = new();
    public List<OrassQueueItemDto> Queue { get; set; } = new();
    public List<OrassSubmissionHistoryItemDto> History { get; set; } = new();
    public List<RegulatoryVarianceItemDto> Variances { get; set; } = new();
}

public class RegulatoryVarianceItemDto
{
    public string Reference { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string ActionHint { get; set; } = string.Empty;
    public string ResolutionStatus { get; set; } = "OPEN";
    public string? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public string? AssignedByName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RegulatoryVarianceEventDto> Events { get; set; } = new();
}

public class ResolveRegulatoryVarianceRequest
{
    public string Reference { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
}

public class AssignRelationshipOwnerRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public string? AssignmentNote { get; set; }
}

public class AssignRegulatoryVarianceRequest
{
    public string Reference { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public string? AssignmentNote { get; set; }
}

public class RegulatoryVarianceEventDto
{
    public string EventType { get; set; } = string.Empty;
    public string? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
    public string Detail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AssignableStaffItemDto
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BranchId { get; set; }
    public string Status { get; set; } = string.Empty;
}
