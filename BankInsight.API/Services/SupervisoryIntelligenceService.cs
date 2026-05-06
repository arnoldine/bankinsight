using System.Security.Claims;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class SupervisoryIntelligenceService
{
    private readonly ApplicationDbContext _context;
    private readonly IAnalyticsService _analyticsService;
    private readonly IOrassService _orassService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SupervisoryIntelligenceService(
        ApplicationDbContext context,
        IAnalyticsService analyticsService,
        IOrassService orassService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _analyticsService = analyticsService;
        _orassService = orassService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RelationshipBankingSummaryDto> GetRelationshipBankingSummaryAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Select(customer => new
            {
                customer.Id,
                customer.Name
            })
            .ToListAsync(cancellationToken);

        var accountAggregates = await _context.Accounts
            .AsNoTracking()
            .GroupBy(account => account.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key ?? string.Empty,
                ActiveAccountCount = group.Count(account => account.Status == "ACTIVE" && account.Type != "FIXED_DEPOSIT"),
                ActiveInvestmentCount = group.Count(account => account.Status == "ACTIVE" && account.Type == "FIXED_DEPOSIT"),
                DepositBalance = group.Where(account => account.Type != "FIXED_DEPOSIT").Sum(account => account.Balance),
                InvestmentBalance = group.Where(account => account.Type == "FIXED_DEPOSIT").Sum(account => account.Balance)
            })
            .ToDictionaryAsync(item => item.CustomerId, cancellationToken);

        var accountOwners = await _context.Accounts
            .AsNoTracking()
            .Include(account => account.OwnerStaff)
            .Where(account => account.OwnerStaffId != null && account.CustomerId != null)
            .Select(account => new
            {
                CustomerId = account.CustomerId!,
                OwnerName = account.OwnerStaff != null ? account.OwnerStaff.Name : null
            })
            .ToListAsync(cancellationToken);

        var loanAggregates = await _context.Loans
            .AsNoTracking()
            .GroupBy(loan => loan.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key ?? string.Empty,
                ActiveLoanCount = group.Count(loan => loan.Status != "CLOSED"),
                LoanExposure = group.Sum(loan => loan.OutstandingBalance ?? loan.Principal)
            })
            .ToDictionaryAsync(item => item.CustomerId, cancellationToken);

        var loanOwners = await _context.Loans
            .AsNoTracking()
            .Include(loan => loan.OwnerStaff)
            .Where(loan => loan.OwnerStaffId != null && loan.CustomerId != null)
            .Select(loan => new
            {
                CustomerId = loan.CustomerId!,
                OwnerName = loan.OwnerStaff != null ? loan.OwnerStaff.Name : null
            })
            .ToListAsync(cancellationToken);

        var investmentOwners = await _context.Investments
            .AsNoTracking()
            .Include(investment => investment.OwnerStaff)
            .Where(investment => investment.OwnerStaffId != null && investment.SettlementAccount != null)
            .Join(
                _context.Accounts.AsNoTracking(),
                investment => investment.SettlementAccount,
                account => account.Id,
                (investment, account) => new
                {
                    CustomerId = account.CustomerId ?? string.Empty,
                    OwnerName = investment.OwnerStaff != null ? investment.OwnerStaff.Name : null
                })
            .Where(item => item.CustomerId != string.Empty)
            .ToListAsync(cancellationToken);

        var lastEngagementByCustomer = await _context.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Account)
            .Where(transaction => transaction.Account != null && transaction.Account.CustomerId != null)
            .GroupBy(transaction => transaction.Account!.CustomerId!)
            .Select(group => new
            {
                CustomerId = group.Key,
                LastEngagementAt = group.Max(transaction => transaction.Date)
            })
            .ToDictionaryAsync(item => item.CustomerId, item => (DateTime?)item.LastEngagementAt, cancellationToken);

        var complaintCounts = await _context.ClientComplaints
            .AsNoTracking()
            .Where(item => item.Status != "CLOSED")
            .GroupBy(item => item.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.CustomerId, item => item.Count, cancellationToken);

        var groupLinkCounts = await _context.GroupMembers
            .AsNoTracking()
            .GroupBy(item => item.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.CustomerId, item => item.Count, cancellationToken);

        var relationshipAssignments = await _context.RelationshipOwnershipAssignments
            .AsNoTracking()
            .ToDictionaryAsync(item => item.CustomerId, cancellationToken);

        var topRelationships = customers
            .Select(customer =>
            {
                accountAggregates.TryGetValue(customer.Id, out var accountData);
                loanAggregates.TryGetValue(customer.Id, out var loanData);
                complaintCounts.TryGetValue(customer.Id, out var openComplaints);
                groupLinkCounts.TryGetValue(customer.Id, out var groupLinks);

                var depositBalance = accountData?.DepositBalance ?? 0m;
                var investmentBalance = accountData?.InvestmentBalance ?? 0m;
                var loanExposure = loanData?.LoanExposure ?? 0m;
                var estimatedAnnualRevenue = EstimateRelationshipRevenue(depositBalance, loanExposure, investmentBalance, (accountData?.ActiveAccountCount ?? 0) + (loanData?.ActiveLoanCount ?? 0));
                var relationshipValue = Math.Round((depositBalance * 0.015m) + (investmentBalance * 0.03m) - (loanExposure * 0.005m) + estimatedAnnualRevenue, 2);
                var assignment = relationshipAssignments.GetValueOrDefault(customer.Id);
                var owner = assignment?.OwnerName ?? ResolveRelationshipOwner(
                    accountOwners.Where(item => item.CustomerId == customer.Id).Select(item => item.OwnerName),
                    loanOwners.Where(item => item.CustomerId == customer.Id).Select(item => item.OwnerName),
                    investmentOwners.Where(item => item.CustomerId == customer.Id).Select(item => item.OwnerName));

                return new RelationshipCustomerItemDto
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    Segment = ResolveRelationshipSegment(depositBalance + investmentBalance),
                    ActiveAccountCount = accountData?.ActiveAccountCount ?? 0,
                    ActiveLoanCount = loanData?.ActiveLoanCount ?? 0,
                    ActiveInvestmentCount = accountData?.ActiveInvestmentCount ?? 0,
                    DepositBalance = depositBalance,
                    LoanExposure = loanExposure,
                    InvestmentBalance = investmentBalance,
                    EstimatedRelationshipValue = relationshipValue,
                    EstimatedAnnualRevenue = estimatedAnnualRevenue,
                    HouseholdOrGroupLinks = groupLinks,
                    OpenComplaintCount = openComplaints,
                    RiskSummary = ResolveRelationshipRisk(openComplaints, loanExposure, depositBalance),
                    RelationshipOwnerUserId = assignment?.OwnerUserId,
                    RelationshipOwner = owner,
                    LastEngagementAt = lastEngagementByCustomer.GetValueOrDefault(customer.Id)
                };
            })
            .OrderByDescending(item => item.EstimatedRelationshipValue)
            .ThenByDescending(item => item.DepositBalance + item.InvestmentBalance)
            .Take(12)
            .ToList();

        var recentTransactionEngagements = await _context.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Account)
            .ThenInclude(account => account!.Customer)
            .OrderByDescending(transaction => transaction.Date)
            .Take(10)
            .Select(transaction => new RelationshipEngagementItemDto
            {
                CustomerId = transaction.Account != null ? transaction.Account.CustomerId ?? string.Empty : string.Empty,
                CustomerName = transaction.Account != null && transaction.Account.Customer != null ? transaction.Account.Customer.Name : "Unknown customer",
                Source = "TRANSACTION",
                Title = transaction.Type,
                Detail = $"{transaction.Amount:N2} {(transaction.Account != null ? transaction.Account.Currency : "GHS")} on {transaction.AccountId}",
                Severity = transaction.Amount >= 100000m ? "HIGH" : "INFO",
                OccurredAt = transaction.Date
            })
            .ToListAsync(cancellationToken);

        var complaintEngagements = await _context.ClientComplaints
            .AsNoTracking()
            .Include(item => item.Customer)
            .OrderByDescending(item => item.UpdatedAt)
            .Take(6)
            .Select(item => new RelationshipEngagementItemDto
            {
                CustomerId = item.CustomerId,
                CustomerName = item.Customer != null ? item.Customer.Name : item.CustomerId,
                Source = "COMPLAINT",
                Title = item.Category,
                Detail = item.Summary,
                Severity = item.Status == "ESCALATED" ? "HIGH" : "WARN",
                OccurredAt = item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var metrics = new List<SupervisoryMetricDto>
        {
            new() { Key = "customers", Label = "Managed relationships", Value = customers.Count.ToString("N0"), Severity = "INFO" },
            new() { Key = "relationship-value", Label = "Estimated relationship value", Value = topRelationships.Sum(item => item.EstimatedRelationshipValue).ToString("N2"), Severity = "SUCCESS", Subtitle = "Top portfolio slice" },
            new() { Key = "annual-revenue", Label = "Estimated annual revenue", Value = topRelationships.Sum(item => item.EstimatedAnnualRevenue).ToString("N2"), Severity = "INFO", Subtitle = "Modeled profitability across balances and activity" },
            new() { Key = "open-complaints", Label = "Open complaints", Value = complaintCounts.Values.Sum().ToString("N0"), Severity = complaintCounts.Values.Sum() > 10 ? "WARN" : "INFO" },
            new() { Key = "group-links", Label = "Household / group links", Value = groupLinkCounts.Values.Sum().ToString("N0"), Severity = "INFO" }
        };

        return new RelationshipBankingSummaryDto
        {
            Metrics = metrics,
            TopRelationships = topRelationships,
            ManagerPerformance = topRelationships
                .GroupBy(item => item.RelationshipOwner)
                .Select(group => new RelationshipManagerPerformanceItemDto
                {
                    RelationshipOwner = group.Key,
                    CustomerCount = group.Count(),
                    DepositBalance = group.Sum(item => item.DepositBalance),
                    LoanExposure = group.Sum(item => item.LoanExposure),
                    EstimatedAnnualRevenue = group.Sum(item => item.EstimatedAnnualRevenue),
                    HighRiskRelationships = group.Count(item => string.Equals(item.RiskSummary, "HIGH", StringComparison.OrdinalIgnoreCase)),
                    OpenComplaintCount = group.Sum(item => item.OpenComplaintCount)
                })
                .OrderByDescending(item => item.EstimatedAnnualRevenue)
                .ToList(),
            RecentEngagements = recentTransactionEngagements.Concat(complaintEngagements)
                .OrderByDescending(item => item.OccurredAt)
                .Take(12)
                .ToList(),
            AssignableStaff = await GetAssignableStaffAsync(cancellationToken)
        };
    }

    public async Task<RelationshipPortfolioDetailDto?> GetRelationshipPortfolioDetailAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(item => item.Id == customerId)
            .Select(item => new { item.Id, item.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(item => item.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        var accountIds = accounts.Select(item => item.Id).ToList();

        var loans = await _context.Loans
            .AsNoTracking()
            .Where(item => item.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        var investments = await _context.Investments
            .AsNoTracking()
            .Where(item => item.SettlementAccount != null && accountIds.Contains(item.SettlementAccount))
            .ToListAsync(cancellationToken);

        var complaints = await _context.ClientComplaints
            .AsNoTracking()
            .Where(item => item.CustomerId == customerId && item.Status != "CLOSED")
            .ToListAsync(cancellationToken);

        var groupLinks = await _context.GroupMembers
            .AsNoTracking()
            .CountAsync(item => item.CustomerId == customerId, cancellationToken);

        var recentTransactions = await _context.Transactions
            .AsNoTracking()
            .Where(item => item.AccountId != null && accountIds.Contains(item.AccountId))
            .OrderByDescending(item => item.Date)
            .Take(8)
            .ToListAsync(cancellationToken);

        var assignment = await _context.RelationshipOwnershipAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CustomerId == customerId, cancellationToken);

        var depositBalance = accounts.Where(item => item.Type != "FIXED_DEPOSIT").Sum(item => item.Balance);
        var investmentBalance = investments.Sum(item => item.PrincipalAmount) + accounts.Where(item => item.Type == "FIXED_DEPOSIT").Sum(item => item.Balance);
        var loanExposure = loans.Sum(item => item.OutstandingBalance ?? item.Principal);
        var estimatedAnnualRevenue = EstimateRelationshipRevenue(depositBalance, loanExposure, investmentBalance, accounts.Count + loans.Count + recentTransactions.Count);
        var riskSummary = ResolveRelationshipRisk(complaints.Count, loanExposure, depositBalance);
        var lastEngagement = recentTransactions.FirstOrDefault()?.Date;

        return new RelationshipPortfolioDetailDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Segment = ResolveRelationshipSegment(depositBalance + investmentBalance),
            RiskSummary = riskSummary,
            RelationshipOwnerUserId = assignment?.OwnerUserId,
            RelationshipOwner = assignment?.OwnerName ?? "Unassigned",
            DepositBalance = depositBalance,
            InvestmentBalance = investmentBalance,
            LoanExposure = loanExposure,
            EstimatedAnnualRevenue = estimatedAnnualRevenue,
            EstimatedRelationshipValue = Math.Round((depositBalance * 0.015m) + (investmentBalance * 0.03m) - (loanExposure * 0.005m) + estimatedAnnualRevenue, 2),
            LastEngagementAt = lastEngagement,
            OpenComplaintCount = complaints.Count,
            HouseholdOrGroupLinks = groupLinks,
            ProductBreakdown = new List<RelationshipPortfolioBreakdownItemDto>
            {
                new()
                {
                    Category = "Accounts",
                    Count = accounts.Count(item => item.Type != "FIXED_DEPOSIT"),
                    Balance = depositBalance,
                    Contribution = Math.Round(depositBalance * 0.006m, 2)
                },
                new()
                {
                    Category = "Investments",
                    Count = investments.Count + accounts.Count(item => item.Type == "FIXED_DEPOSIT"),
                    Balance = investmentBalance,
                    Contribution = Math.Round(investmentBalance * 0.018m, 2)
                },
                new()
                {
                    Category = "Loans",
                    Count = loans.Count,
                    Balance = loanExposure,
                    Contribution = Math.Round(loanExposure * 0.085m, 2)
                }
            },
            RecentEngagements = recentTransactions.Select(item => new RelationshipEngagementItemDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Source = "TRANSACTION",
                Title = item.Type,
                Detail = $"{item.Amount:N2} on {(item.AccountId ?? "N/A")}",
                Severity = item.Amount >= 100000m ? "HIGH" : "INFO",
                OccurredAt = item.Date
            }).Concat(complaints.Select(item => new RelationshipEngagementItemDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Source = "COMPLAINT",
                Title = item.Category,
                Detail = item.Summary,
                Severity = item.Status == "ESCALATED" ? "HIGH" : "WARN",
                OccurredAt = item.UpdatedAt
            }))
            .OrderByDescending(item => item.OccurredAt)
            .Take(10)
            .ToList()
        };
    }

    public async Task<RelationshipCustomerItemDto> AssignRelationshipOwnerAsync(AssignRelationshipOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(item => item.Id == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");

        Staff? owner = null;
        if (!string.IsNullOrWhiteSpace(request.OwnerUserId))
        {
            owner = await _context.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.OwnerUserId, cancellationToken)
                ?? throw new InvalidOperationException("Selected relationship owner was not found.");
        }

        var assignment = await _context.RelationshipOwnershipAssignments
            .FirstOrDefaultAsync(item => item.CustomerId == request.CustomerId, cancellationToken);

        if (assignment is null)
        {
            assignment = new RelationshipOwnershipAssignment
            {
                CustomerId = request.CustomerId
            };
            _context.RelationshipOwnershipAssignments.Add(assignment);
        }

        assignment.OwnerUserId = owner?.Id;
        assignment.OwnerName = owner?.Name ?? "Unassigned";
        assignment.AssignedByUserId = GetCurrentUserId();
        assignment.AssignedByName = GetCurrentUserName();
        assignment.AssignmentNote = string.IsNullOrWhiteSpace(request.AssignmentNote) ? "Relationship owner updated from supervisory workbench." : request.AssignmentNote.Trim();
        assignment.AssignedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var detail = await GetRelationshipPortfolioDetailAsync(request.CustomerId, cancellationToken)
                     ?? throw new InvalidOperationException("Relationship detail could not be reloaded.");

        return new RelationshipCustomerItemDto
        {
            CustomerId = detail.CustomerId,
            CustomerName = detail.CustomerName,
            Segment = detail.Segment,
            ActiveAccountCount = detail.ProductBreakdown.FirstOrDefault(item => item.Category == "Accounts")?.Count ?? 0,
            ActiveLoanCount = detail.ProductBreakdown.FirstOrDefault(item => item.Category == "Loans")?.Count ?? 0,
            ActiveInvestmentCount = detail.ProductBreakdown.FirstOrDefault(item => item.Category == "Investments")?.Count ?? 0,
            DepositBalance = detail.DepositBalance,
            LoanExposure = detail.LoanExposure,
            InvestmentBalance = detail.InvestmentBalance,
            EstimatedRelationshipValue = detail.EstimatedRelationshipValue,
            EstimatedAnnualRevenue = detail.EstimatedAnnualRevenue,
            HouseholdOrGroupLinks = detail.HouseholdOrGroupLinks,
            OpenComplaintCount = detail.OpenComplaintCount,
            RiskSummary = detail.RiskSummary,
            RelationshipOwnerUserId = detail.RelationshipOwnerUserId,
            RelationshipOwner = detail.RelationshipOwner,
            LastEngagementAt = detail.LastEngagementAt
        };
    }

    public async Task<List<AssignableStaffItemDto>> GetAssignableStaffAsync(CancellationToken cancellationToken = default)
        => await _context.Staff
            .AsNoTracking()
            .Where(item => item.Status == "Active")
            .OrderBy(item => item.Name)
            .Select(item => new AssignableStaffItemDto
            {
                UserId = item.Id,
                Name = item.Name,
                Email = item.Email,
                BranchId = item.BranchId,
                Status = item.Status
            })
            .ToListAsync(cancellationToken);

    public async Task<DigitalChannelOperationsSummaryDto> GetDigitalChannelOperationsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var periodStart = now.AddDays(-30);
        var channelAnalytics = await _analyticsService.GetChannelAnalyticsAsync(periodStart, now);

        var activeSessions = await _context.ClientChannelSessions
            .AsNoTracking()
            .Include(item => item.Customer)
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.LastActivity)
            .Take(12)
            .ToListAsync(cancellationToken);

        var complaintQueue = await _context.ClientComplaints
            .AsNoTracking()
            .Include(item => item.Customer)
            .Where(item => item.Status != "CLOSED")
            .OrderBy(item => item.SlaDueAt)
            .Take(12)
            .ToListAsync(cancellationToken);

        var kycQueue = await _context.ClientKycCases
            .AsNoTracking()
            .Include(item => item.Customer)
            .Where(item => item.Status == "SUBMITTED" || item.Status == "UNDER_REVIEW")
            .OrderBy(item => item.SubmittedAt)
            .Take(12)
            .ToListAsync(cancellationToken);

        var digitalTransferCount = await _context.Transactions
            .AsNoTracking()
            .CountAsync(item => item.Date >= now.AddHours(-24) && (item.Type.Contains("TRANSFER") || item.Type.Contains("PAYMENT")), cancellationToken);

        var activeClientSessions = await _context.ClientChannelSessions.AsNoTracking().CountAsync(item => item.IsActive, cancellationToken);
        var openComplaints = await _context.ClientComplaints.AsNoTracking().CountAsync(item => item.Status != "CLOSED", cancellationToken);
        var pendingKycCases = await _context.ClientKycCases.AsNoTracking().CountAsync(item => item.Status == "SUBMITTED" || item.Status == "UNDER_REVIEW", cancellationToken);
        var merchantProfiles = await _context.ClientMerchantProfiles.AsNoTracking().CountAsync(item => item.Status == "ACTIVE", cancellationToken);

        return new DigitalChannelOperationsSummaryDto
        {
            Metrics = new List<SupervisoryMetricDto>
            {
                new() { Key = "sessions", Label = "Active client sessions", Value = activeClientSessions.ToString("N0"), Severity = activeClientSessions > 100 ? "SUCCESS" : "INFO" },
                new() { Key = "transfers24h", Label = "Digital payments / transfers 24h", Value = digitalTransferCount.ToString("N0"), Severity = "INFO" },
                new() { Key = "complaints", Label = "Open digital complaints", Value = openComplaints.ToString("N0"), Severity = openComplaints > 8 ? "WARN" : "INFO" },
                new() { Key = "kyc", Label = "Pending KYC refresh cases", Value = pendingKycCases.ToString("N0"), Severity = pendingKycCases > 5 ? "WARN" : "INFO", Subtitle = $"{merchantProfiles:N0} active merchant profiles" }
            },
            ChannelMetrics = channelAnalytics.ChannelMetrics.Select(item => new DigitalChannelMetricDto
            {
                ChannelName = item.ChannelName,
                TransactionCount = item.TransactionCount > int.MaxValue ? int.MaxValue : (int)item.TransactionCount,
                TransactionVolume = item.TransactionVolume,
                PercentageOfTotal = item.PercentageOfTotal
            }).ToList(),
            SessionRiskItems = activeSessions.Select(item => new DigitalSessionRiskItemDto
            {
                SessionId = item.Id,
                CustomerId = item.CustomerId,
                CustomerName = item.Customer != null ? item.Customer.Name : item.CustomerId,
                IpAddress = item.IpAddress,
                UserAgent = item.UserAgent,
                LastActivity = item.LastActivity,
                ExpiresAt = item.ExpiresAt,
                IsActive = item.IsActive,
                RiskLabel = ResolveSessionRisk(item.LastActivity, item.ExpiresAt)
            }).ToList(),
            ComplaintQueue = complaintQueue.Select(item => new DigitalComplaintItemDto
            {
                ComplaintId = item.Id,
                Reference = item.Reference,
                CustomerId = item.CustomerId,
                CustomerName = item.Customer != null ? item.Customer.Name : item.CustomerId,
                Category = item.Category,
                Status = item.Status,
                OwnerTeam = item.OwnerTeam,
                SlaDueAt = item.SlaDueAt,
                Summary = item.Summary
            }).ToList(),
            KycQueue = kycQueue.Select(item => new DigitalKycItemDto
            {
                KycCaseId = item.Id,
                Reference = item.Reference,
                CustomerId = item.CustomerId,
                CustomerName = item.Customer != null ? item.Customer.Name : item.CustomerId,
                Status = item.Status,
                Reason = item.Reason,
                SubmittedAt = item.SubmittedAt,
                ReviewerName = item.ReviewerName
            }).ToList()
        };
    }

    public async Task<RegulatoryIntelligenceSummaryDto> GetRegulatoryIntelligenceSummaryAsync(CancellationToken cancellationToken = default)
    {
        var readiness = await _orassService.GetReadinessAsync();
        var queue = (await _orassService.GetQueueAsync()).ToList();
        var history = (await _orassService.GetHistoryAsync(12)).ToList();
        var resolutions = await _context.RegulatoryVarianceResolutions.AsNoTracking().ToListAsync(cancellationToken);
        var events = await _context.RegulatoryVarianceEvents
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var variances = new List<RegulatoryVarianceItemDto>();

        variances.AddRange(queue
            .Where(item => !item.IsReadyForSubmission || item.ValidationMessages.Length > 0)
            .Select(item => ApplyResolution(new RegulatoryVarianceItemDto
            {
                Reference = $"QUEUE-{item.Id}",
                ReturnType = item.ReturnType,
                Severity = item.ValidationMessages.Length > 0 ? "WARN" : "INFO",
                Title = item.IsReadyForSubmission ? "Validation attention required" : "Return not ready for submission",
                Detail = item.ValidationMessages.FirstOrDefault() ?? $"{item.ReturnType} is waiting for readiness clearance.",
                ActionHint = item.IsReadyForSubmission ? "Review queue validation messages." : "Clear readiness blockers and seek approval."
            }, resolutions, events)));

        variances.AddRange(history
            .Where(item => string.Equals(item.AcknowledgementStatus, "REJECTED", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(item.SubmissionStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            .Select(item => ApplyResolution(new RegulatoryVarianceItemDto
            {
                Reference = $"SUBMISSION-{item.Id}",
                ReturnType = item.ReturnType,
                Severity = "HIGH",
                Title = "Rejected regulatory submission",
                Detail = item.TransportMessage ?? item.ValidationMessages.FirstOrDefault() ?? "The regulator returned this submission with a rejection status.",
                ActionHint = "Review evidence, correct the return, and resubmit through ORASS."
            }, resolutions, events)));

        return new RegulatoryIntelligenceSummaryDto
        {
            Metrics = new List<SupervisoryMetricDto>
            {
                new() { Key = "profile", Label = "Profile configured", Value = readiness.ProfileConfigured ? "Yes" : "No", Severity = readiness.ProfileConfigured ? "SUCCESS" : "ERROR" },
                new() { Key = "ready", Label = "Returns ready", Value = readiness.ReturnsReadyForSubmission.ToString("N0"), Severity = readiness.ReturnsReadyForSubmission > 0 ? "SUCCESS" : "WARN" },
                new() { Key = "pending", Label = "Pending returns", Value = readiness.PendingReturns.ToString("N0"), Severity = readiness.PendingReturns > readiness.ReturnsReadyForSubmission ? "WARN" : "INFO" },
                new() { Key = "variances", Label = "Open regulatory variances", Value = variances.Count.ToString("N0"), Severity = variances.Any(item => item.Severity == "HIGH") ? "ERROR" : "INFO" }
            },
            Readiness = readiness,
            Queue = queue,
            History = history,
            Variances = variances
        };
    }

    public async Task<RegulatoryVarianceItemDto> ResolveRegulatoryVarianceAsync(ResolveRegulatoryVarianceRequest request, CancellationToken cancellationToken = default)
        => await UpsertVarianceResolutionAsync(request, "RESOLVED", cancellationToken);

    public async Task<RegulatoryVarianceItemDto> ReopenRegulatoryVarianceAsync(ResolveRegulatoryVarianceRequest request, CancellationToken cancellationToken = default)
        => await UpsertVarianceResolutionAsync(request, "OPEN", cancellationToken);

    public async Task<RegulatoryVarianceItemDto> AssignRegulatoryVarianceAsync(AssignRegulatoryVarianceRequest request, CancellationToken cancellationToken = default)
    {
        Staff? owner = null;
        if (!string.IsNullOrWhiteSpace(request.OwnerUserId))
        {
            owner = await _context.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.OwnerUserId, cancellationToken)
                ?? throw new InvalidOperationException("Selected variance owner was not found.");
        }

        var resolution = await _context.RegulatoryVarianceResolutions
            .FirstOrDefaultAsync(item => item.Reference == request.Reference && item.ReturnType == request.ReturnType, cancellationToken);

        if (resolution is null)
        {
            resolution = new RegulatoryVarianceResolution
            {
                Reference = request.Reference,
                ReturnType = request.ReturnType,
                ResolutionStatus = "OPEN"
            };
            _context.RegulatoryVarianceResolutions.Add(resolution);
        }

        resolution.OwnerUserId = owner?.Id;
        resolution.OwnerName = owner?.Name;
        resolution.AssignedByUserId = GetCurrentUserId();
        resolution.AssignedByName = GetCurrentUserName();
        resolution.AssignedAt = DateTime.UtcNow;
        resolution.UpdatedAt = DateTime.UtcNow;

        _context.RegulatoryVarianceEvents.Add(new RegulatoryVarianceEvent
        {
            Reference = request.Reference,
            ReturnType = request.ReturnType,
            EventType = "ASSIGNED",
            PerformedByUserId = GetCurrentUserId(),
            PerformedByName = GetCurrentUserName(),
            Detail = string.IsNullOrWhiteSpace(request.AssignmentNote)
                ? $"Assigned to {owner?.Name ?? "Unassigned"}."
                : request.AssignmentNote.Trim()
        });

        await _context.SaveChangesAsync(cancellationToken);

        var latestResolution = await _context.RegulatoryVarianceResolutions.AsNoTracking()
            .Where(item => item.Reference == request.Reference && item.ReturnType == request.ReturnType)
            .FirstAsync(cancellationToken);
        var latestEvents = await _context.RegulatoryVarianceEvents.AsNoTracking()
            .Where(item => item.Reference == request.Reference && item.ReturnType == request.ReturnType)
            .OrderByDescending(item => item.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        return ApplyResolution(new RegulatoryVarianceItemDto
        {
            Reference = request.Reference,
            ReturnType = request.ReturnType,
            Severity = "INFO",
            Title = "Assigned regulatory variance",
            Detail = latestResolution.ResolutionNote ?? string.Empty,
            ActionHint = "Assignment updated",
            ResolutionStatus = latestResolution.ResolutionStatus
        }, new List<RegulatoryVarianceResolution> { latestResolution }, latestEvents);
    }

    private async Task<RegulatoryVarianceItemDto> UpsertVarianceResolutionAsync(ResolveRegulatoryVarianceRequest request, string status, CancellationToken cancellationToken)
    {
        var resolution = await _context.RegulatoryVarianceResolutions
            .FirstOrDefaultAsync(item => item.Reference == request.Reference && item.ReturnType == request.ReturnType, cancellationToken);

        if (resolution is null)
        {
            resolution = new RegulatoryVarianceResolution
            {
                Reference = request.Reference,
                ReturnType = request.ReturnType
            };
            _context.RegulatoryVarianceResolutions.Add(resolution);
        }

        resolution.ResolutionStatus = status;
        resolution.OwnerUserId = GetCurrentUserId();
        resolution.OwnerName = GetCurrentUserName();
        resolution.ResolutionNote = string.IsNullOrWhiteSpace(request.ResolutionNote)
            ? (status == "RESOLVED" ? "Resolved from regulatory intelligence workbench." : "Reopened for further review.")
            : request.ResolutionNote.Trim();
        resolution.ResolvedAt = status == "RESOLVED" ? DateTime.UtcNow : null;
        resolution.UpdatedAt = DateTime.UtcNow;

        _context.RegulatoryVarianceEvents.Add(new RegulatoryVarianceEvent
        {
            Reference = request.Reference,
            ReturnType = request.ReturnType,
            EventType = status == "RESOLVED" ? "RESOLVED" : "REOPENED",
            PerformedByUserId = GetCurrentUserId(),
            PerformedByName = GetCurrentUserName(),
            Detail = resolution.ResolutionNote ?? string.Empty
        });

        await _context.SaveChangesAsync(cancellationToken);

        var events = await _context.RegulatoryVarianceEvents
            .AsNoTracking()
            .Where(item => item.Reference == request.Reference && item.ReturnType == request.ReturnType)
            .OrderByDescending(item => item.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new RegulatoryVarianceItemDto
        {
            Reference = resolution.Reference,
            ReturnType = resolution.ReturnType,
            Severity = status == "RESOLVED" ? "INFO" : "WARN",
            Title = status == "RESOLVED" ? "Resolved variance" : "Reopened variance",
            Detail = resolution.ResolutionNote ?? string.Empty,
            ActionHint = status == "RESOLVED" ? "Resolved" : "Open",
            ResolutionStatus = resolution.ResolutionStatus,
            OwnerUserId = resolution.OwnerUserId,
            OwnerName = resolution.OwnerName,
            AssignedByName = resolution.AssignedByName,
            AssignedAt = resolution.AssignedAt,
            ResolutionNote = resolution.ResolutionNote,
            ResolvedAt = resolution.ResolvedAt,
            UpdatedAt = resolution.UpdatedAt,
            Events = events.Select(MapVarianceEvent).ToList()
        };
    }

    private static string ResolveRelationshipSegment(decimal balance)
        => balance switch
        {
            >= 1000000m => "Strategic",
            >= 100000m => "Commercial",
            >= 10000m => "Retail Plus",
            _ => "Retail"
        };

    private static string ResolveRelationshipRisk(int openComplaints, decimal loanExposure, decimal depositBalance)
    {
        if (openComplaints >= 3 || (loanExposure > 0m && depositBalance < loanExposure * 0.10m))
        {
            return "HIGH";
        }

        if (openComplaints > 0 || loanExposure > depositBalance)
        {
            return "MEDIUM";
        }

        return "LOW";
    }

    private static string ResolveSessionRisk(DateTime lastActivity, DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow || lastActivity <= DateTime.UtcNow.AddHours(-12))
        {
            return "HIGH";
        }

        if (lastActivity <= DateTime.UtcNow.AddHours(-4))
        {
            return "MEDIUM";
        }

        return "LOW";
    }

    private static decimal EstimateRelationshipRevenue(decimal deposits, decimal loans, decimal investments, int activityUnits)
        => Math.Round((deposits * 0.006m) + (loans * 0.085m) + (investments * 0.018m) + (activityUnits * 18m), 2);

    private static string ResolveRelationshipOwner(params IEnumerable<string?>[] sources)
        => sources
            .SelectMany(source => source)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault()
           ?? "Unassigned";

    private static RegulatoryVarianceItemDto ApplyResolution(RegulatoryVarianceItemDto item, List<RegulatoryVarianceResolution> resolutions, List<RegulatoryVarianceEvent> events)
    {
        var resolution = resolutions
            .Where(entry => entry.Reference == item.Reference && entry.ReturnType == item.ReturnType)
            .OrderByDescending(entry => entry.UpdatedAt)
            .FirstOrDefault();

        if (resolution is null)
        {
            item.ResolutionStatus = "OPEN";
            return item;
        }

        item.ResolutionStatus = resolution.ResolutionStatus;
        item.OwnerUserId = resolution.OwnerUserId;
        item.OwnerName = resolution.OwnerName;
        item.AssignedByName = resolution.AssignedByName;
        item.AssignedAt = resolution.AssignedAt;
        item.ResolutionNote = resolution.ResolutionNote;
        item.ResolvedAt = resolution.ResolvedAt;
        item.UpdatedAt = resolution.UpdatedAt;
        item.Events = events
            .Where(entry => entry.Reference == item.Reference && entry.ReturnType == item.ReturnType)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(10)
            .Select(MapVarianceEvent)
            .ToList();
        return item;
    }

    private static RegulatoryVarianceEventDto MapVarianceEvent(RegulatoryVarianceEvent entry)
        => new()
        {
            EventType = entry.EventType,
            PerformedByUserId = entry.PerformedByUserId,
            PerformedByName = entry.PerformedByName,
            Detail = entry.Detail,
            CreatedAt = entry.CreatedAt
        };

    private string GetCurrentUserId()
        => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
           ?? "system";

    private string GetCurrentUserName()
        => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value
           ?? _httpContextAccessor.HttpContext?.User.Identity?.Name
           ?? "system";
}
