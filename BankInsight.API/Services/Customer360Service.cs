using BankInsight.API.Data;
using BankInsight.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class Customer360Service
{
    private readonly ApplicationDbContext _context;
    private readonly CustomerService _customerService;

    public Customer360Service(ApplicationDbContext context, CustomerService customerService)
    {
        _context = context;
        _customerService = customerService;
    }

    public async Task<Customer360Response?> GetCustomer360Async(string customerId)
    {
        var profile = await _customerService.GetCustomerProfileAsync(customerId);
        if (profile == null)
        {
            return null;
        }

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Include(account => account.OwnerStaff)
            .Where(account => account.CustomerId == customerId)
            .OrderByDescending(account => account.CreatedAt)
            .ToListAsync();

        var accountIds = accounts.Select(account => account.Id).ToList();

        var loans = await _context.Loans
            .AsNoTracking()
            .Include(loan => loan.OwnerStaff)
            .Where(loan => loan.CustomerId == customerId)
            .OrderByDescending(loan => loan.ApplicationDate)
            .ToListAsync();

        var recentTransactions = await _context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId != null && accountIds.Contains(transaction.AccountId))
            .OrderByDescending(transaction => transaction.Date)
            .Take(12)
            .ToListAsync();

        var digitalInvestments = await _context.DigitalInvestmentProfiles
            .AsNoTracking()
            .Include(profileItem => profileItem.Account)
            .Where(profileItem => profileItem.CustomerId == customerId)
            .OrderByDescending(profileItem => profileItem.CreatedAt)
            .ToListAsync();

        var investmentSettlementMatches = await _context.Investments
            .AsNoTracking()
            .Include(investment => investment.OwnerStaff)
            .Where(investment => investment.SettlementAccount != null && accountIds.Contains(investment.SettlementAccount))
            .OrderByDescending(investment => investment.CreatedAt)
            .Take(8)
            .ToListAsync();

        var relationshipAssignment = await _context.RelationshipOwnershipAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CustomerId == customerId);

        var relationshipOwner = relationshipAssignment?.OwnerName
            ?? ResolveRelationshipOwner(accounts.Select(item => item.OwnerStaff?.Name), loans.Select(item => item.OwnerStaff?.Name), investmentSettlementMatches.Select(item => item.OwnerStaff?.Name));

        var complaintEvents = await _context.ClientComplaints
            .AsNoTracking()
            .Where(complaint => complaint.CustomerId == customerId)
            .OrderByDescending(complaint => complaint.CreatedAt)
            .Take(5)
            .Select(complaint => new Customer360EngagementItemDto
            {
                Type = "COMPLAINT",
                Title = $"Complaint {complaint.Status}",
                Detail = complaint.Summary,
                Severity = complaint.Status == "OPEN" ? "MEDIUM" : "INFO",
                At = complaint.CreatedAt
            })
            .ToListAsync();

        var kycEvents = await _context.ClientKycCases
            .AsNoTracking()
            .Where(kycCase => kycCase.CustomerId == customerId)
            .OrderByDescending(kycCase => kycCase.CreatedAt)
            .Take(5)
            .Select(kycCase => new Customer360EngagementItemDto
            {
                Type = "KYC",
                Title = $"KYC {kycCase.Status}",
                Detail = kycCase.Summary,
                Severity = kycCase.Status == "PENDING_REVIEW" ? "MEDIUM" : "INFO",
                At = kycCase.CreatedAt
            })
            .ToListAsync();

        var accountDtos = accounts.Select(account => new Customer360AccountDto
        {
            Id = account.Id,
            ProductCode = account.ProductCode,
            Status = account.Status,
            Currency = account.Currency,
            Balance = account.Balance,
            OpenDate = account.CreatedAt
        }).ToList();

        var loanDtos = loans.Select(loan => new Customer360LoanDto
        {
            Id = loan.Id,
            ProductCode = loan.ProductCode ?? loan.LoanProductId,
            Status = loan.Status,
            Principal = loan.Principal,
            OutstandingBalance = loan.OutstandingBalance ?? 0m,
            ParBucket = loan.ParBucket,
            RepaymentFrequency = loan.RepaymentFrequency
        }).ToList();

        var investmentDtos = digitalInvestments.Select(profileItem => new Customer360InvestmentDto
        {
            Id = profileItem.Id,
            ProductCode = profileItem.ProductCode,
            Status = profileItem.Status,
            Principal = profileItem.Account?.Balance ?? 0m,
            Rate = profileItem.Rate,
            MaturityDate = profileItem.MaturityDate
        }).ToList();

        investmentDtos.AddRange(investmentSettlementMatches.Select(investment => new Customer360InvestmentDto
        {
            Id = investment.InvestmentNumber,
            ProductCode = investment.Instrument,
            Status = investment.Status,
            Principal = investment.PrincipalAmount,
            Rate = investment.InterestRate,
            MaturityDate = investment.MaturityDate
        }));

        var transactionDtos = recentTransactions.Select(transaction => new Customer360TransactionDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId ?? string.Empty,
            Type = transaction.Type,
            Status = transaction.Status,
            Currency = accounts.FirstOrDefault(account => account.Id == transaction.AccountId)?.Currency ?? "GHS",
            Amount = transaction.Amount,
            Date = transaction.Date,
            Description = transaction.Narration
        }).ToList();

        var deposits90 = recentTransactions
            .Where(transaction => transaction.Type.Contains("DEPOSIT", StringComparison.OrdinalIgnoreCase) && transaction.Date >= DateTime.UtcNow.AddDays(-90))
            .Sum(transaction => transaction.Amount);
        var withdrawals90 = recentTransactions
            .Where(transaction => transaction.Type.Contains("WITHDRAW", StringComparison.OrdinalIgnoreCase) && transaction.Date >= DateTime.UtcNow.AddDays(-90))
            .Sum(transaction => transaction.Amount);
        var estimatedAnnualRevenue = EstimateAnnualRevenue(accounts.Sum(account => account.Balance), loans.Sum(loan => loan.OutstandingBalance ?? 0m), investmentDtos.Sum(item => item.Principal), recentTransactions.Count);

        var timeline = profile.Notes
            .Take(5)
            .Select(note => new Customer360EngagementItemDto
            {
                Type = "NOTE",
                Title = note.Category,
                Detail = note.Text,
                Severity = "INFO",
                At = DateTime.TryParse(note.Date, out var parsed) ? parsed : DateTime.UtcNow
            })
            .Concat(complaintEvents)
            .Concat(kycEvents)
            .OrderByDescending(item => item.At)
            .Take(12)
            .ToList();

        return new Customer360Response
        {
            Profile = profile,
            FinancialSummary = new Customer360FinancialSummaryDto
            {
                ActiveAccountCount = accounts.Count(account => string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)),
                ActiveLoanCount = loans.Count(loan => string.Equals(loan.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) || string.Equals(loan.Status, "APPROVED", StringComparison.OrdinalIgnoreCase)),
                ActiveInvestmentCount = investmentDtos.Count(item => string.Equals(item.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)),
                TotalDeposits90Days = deposits90,
                TotalWithdrawals90Days = withdrawals90,
                TotalBalances = accounts.Sum(account => account.Balance),
                TotalOutstandingLoans = loans.Sum(loan => loan.OutstandingBalance ?? 0m),
                TotalInvestmentBook = investmentDtos.Sum(item => item.Principal),
                PrimaryCurrency = accounts.FirstOrDefault()?.Currency ?? "GHS",
                EstimatedAnnualRevenue = estimatedAnnualRevenue,
                RelationshipOwner = relationshipOwner
            },
            Accounts = accountDtos,
            Loans = loanDtos,
            Investments = investmentDtos,
            RecentTransactions = transactionDtos,
            EngagementTimeline = timeline
        };
    }

    private static decimal EstimateAnnualRevenue(decimal deposits, decimal loans, decimal investments, int transactionCount)
        => Math.Round((deposits * 0.006m) + (loans * 0.085m) + (investments * 0.018m) + (transactionCount * 1.50m), 2);

    private static string ResolveRelationshipOwner(params IEnumerable<string?>[] sources)
        => sources
            .SelectMany(source => source)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault()
           ?? "Unassigned";
}
