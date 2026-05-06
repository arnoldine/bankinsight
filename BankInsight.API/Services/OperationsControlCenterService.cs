using BankInsight.API.Data;
using BankInsight.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class OperationsControlCenterService
{
    private readonly ApplicationDbContext _context;
    private readonly OperationsService _operationsService;

    public OperationsControlCenterService(ApplicationDbContext context, OperationsService operationsService)
    {
        _context = context;
        _operationsService = operationsService;
    }

    public async Task<OperationsControlCenterDto> GetSummaryAsync()
    {
        var eodStatus = await _operationsService.GetEodStatusAsync();
        var since24Hours = DateTime.UtcNow.AddHours(-24);
        var since7Days = DateTime.UtcNow.AddDays(-7);

        var pendingApprovals = await _context.ApprovalRequests.CountAsync(request => request.Status == "PENDING");
        var pendingTransactions = await _context.Transactions.CountAsync(transaction => transaction.Status == "PENDING");
        var openComplaints = await _context.ClientComplaints.CountAsync(complaint => complaint.Status == "OPEN");
        var openKycCases = await _context.ClientKycCases.CountAsync(kycCase => kycCase.Status == "OPEN" || kycCase.Status == "PENDING_REVIEW");
        var failedLogins = await _context.LoginAttempts.CountAsync(attempt => !attempt.Success && attempt.AttemptedAt >= since24Hours);
        var openCashIncidents = await _context.CashIncidents.CountAsync(incident => incident.Status == "OPEN");
        var pendingChequeItems = await _context.ChequeClearingItems.CountAsync(item => item.Status == "LODGED" || item.Status == "PENDING");
        var openCollectionCases = await _context.CollectionCases.CountAsync(collectionCase => collectionCase.Status == "OPEN" || collectionCase.Status == "PROMISE_TO_PAY");
        var staleCollections = await _context.CollectionCases.CountAsync(collectionCase => collectionCase.NextActionDate != null && collectionCase.NextActionDate < DateTime.UtcNow && collectionCase.Status != "RESOLVED");
        var wafBlocks = await _context.UserActivities.CountAsync(activity => activity.Action == "WAF_BLOCK" && activity.CreatedAt >= since7Days);

        var workItems = new List<OperationsWorkItemDto>();

        if (pendingApprovals > 0)
        {
            workItems.Add(new OperationsWorkItemDto
            {
                Id = "pending-approvals",
                Category = "APPROVALS",
                Severity = pendingApprovals > 10 ? "HIGH" : "MEDIUM",
                Title = $"{pendingApprovals} approval request(s) awaiting action",
                Detail = "Credit, operations, or workflow approvals are pending maker-checker review.",
                RouteHint = "/approvals",
                Count = pendingApprovals
            });
        }

        if (pendingTransactions > 0)
        {
            workItems.Add(new OperationsWorkItemDto
            {
                Id = "pending-transactions",
                Category = "TRANSACTIONS",
                Severity = pendingTransactions > 25 ? "HIGH" : "MEDIUM",
                Title = $"{pendingTransactions} transaction(s) still pending posting",
                Detail = "Pending postings should be reviewed before close of business.",
                RouteHint = "/transactions",
                Count = pendingTransactions
            });
        }

        if (openCollectionCases > 0)
        {
            workItems.Add(new OperationsWorkItemDto
            {
                Id = "collections",
                Category = "COLLECTIONS",
                Severity = staleCollections > 0 ? "HIGH" : "MEDIUM",
                Title = $"{openCollectionCases} collection case(s) need follow-up",
                Detail = staleCollections > 0
                    ? $"{staleCollections} case(s) have overdue next-action dates."
                    : "Delinquent facilities are open in collections and recoveries.",
                RouteHint = "/collections-recoveries",
                Count = openCollectionCases
            });
        }

        if (openComplaints > 0)
        {
            workItems.Add(new OperationsWorkItemDto
            {
                Id = "complaints",
                Category = "CLIENT_SERVICE",
                Severity = openComplaints > 5 ? "MEDIUM" : "LOW",
                Title = $"{openComplaints} open complaint case(s)",
                Detail = "Customer complaints remain unresolved and should be reviewed in operations.",
                RouteHint = "/security-ops",
                Count = openComplaints
            });
        }

        if (openKycCases > 0)
        {
            workItems.Add(new OperationsWorkItemDto
            {
                Id = "kyc-cases",
                Category = "KYC",
                Severity = openKycCases > 10 ? "MEDIUM" : "LOW",
                Title = $"{openKycCases} KYC case(s) awaiting review",
                Detail = "Customer onboarding or remediation KYC cases remain open.",
                RouteHint = "/clients",
                Count = openKycCases
            });
        }

        if (openCashIncidents > 0)
        {
            workItems.Add(new OperationsWorkItemDto
            {
                Id = "cash-incidents",
                Category = "CASH_CONTROL",
                Severity = "HIGH",
                Title = $"{openCashIncidents} open cash incident(s)",
                Detail = "Vault or till incidents remain unresolved and need immediate attention.",
                RouteHint = "/vault",
                Count = openCashIncidents
            });
        }

        if (pendingChequeItems > 0)
        {
            workItems.Add(new OperationsWorkItemDto
            {
                Id = "cheque-clearing",
                Category = "CHEQUE_CLEARING",
                Severity = "MEDIUM",
                Title = $"{pendingChequeItems} cheque item(s) pending clearing outcome",
                Detail = "Cheque clearing queue contains lodged or pending items awaiting settlement.",
                RouteHint = "/transactions",
                Count = pendingChequeItems
            });
        }

        if (failedLogins > 0 || wafBlocks > 0)
        {
            workItems.Add(new OperationsWorkItemDto
            {
                Id = "security-events",
                Category = "SECURITY",
                Severity = failedLogins > 15 || wafBlocks > 0 ? "HIGH" : "MEDIUM",
                Title = $"{failedLogins} failed login(s) and {wafBlocks} WAF block(s) detected",
                Detail = "Security operations should review authentication spikes and blocked requests.",
                RouteHint = "/security-ops",
                Count = failedLogins + wafBlocks
            });
        }

        var metrics = new List<OperationsMetricDto>
        {
            new() { Key = "business-date", Label = "Business Date", Value = eodStatus.BusinessDate, Severity = "INFO", Subtitle = eodStatus.Status },
            new() { Key = "pending-approvals", Label = "Pending Approvals", Value = pendingApprovals.ToString(), Severity = pendingApprovals > 10 ? "HIGH" : "INFO" },
            new() { Key = "collection-cases", Label = "Open Collections", Value = openCollectionCases.ToString(), Severity = staleCollections > 0 ? "HIGH" : "INFO" },
            new() { Key = "failed-logins", Label = "Failed Logins (24h)", Value = failedLogins.ToString(), Severity = failedLogins > 15 ? "HIGH" : "INFO" },
            new() { Key = "cash-incidents", Label = "Open Cash Incidents", Value = openCashIncidents.ToString(), Severity = openCashIncidents > 0 ? "HIGH" : "INFO" },
            new() { Key = "cheque-queue", Label = "Cheque Queue", Value = pendingChequeItems.ToString(), Severity = pendingChequeItems > 0 ? "MEDIUM" : "INFO" }
        };

        var platformStatus = (openCashIncidents > 0 || staleCollections > 0 || failedLogins > 15)
            ? "ATTENTION_REQUIRED"
            : "HEALTHY";

        return new OperationsControlCenterDto
        {
            BusinessDate = eodStatus.BusinessDate,
            PlatformStatus = platformStatus,
            Metrics = metrics,
            WorkItems = workItems
                .OrderByDescending(item => item.Severity == "HIGH")
                .ThenByDescending(item => item.Count ?? 0)
                .ToList()
        };
    }
}
