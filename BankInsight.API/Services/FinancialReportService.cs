using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services
{
    public interface IFinancialReportService
    {
        Task<BalanceSheetDTO> GenerateBalanceSheetAsync(DateTime asOfDate);
        Task<IncomeStatementDTO> GenerateIncomeStatementAsync(DateTime periodStart, DateTime periodEnd);
        Task<CashFlowStatementDTO> GenerateCashFlowStatementAsync(DateTime periodStart, DateTime periodEnd);
        Task<TrialBalanceDTO> GenerateTrialBalanceAsync(DateTime asOfDate);
    }

    public class FinancialReportService : IFinancialReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FinancialReportService> _logger;

        public FinancialReportService(ApplicationDbContext context, ILogger<FinancialReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BalanceSheetDTO> GenerateBalanceSheetAsync(DateTime asOfDate)
        {
            var balanceSheet = new BalanceSheetDTO
            {
                AsOfDate = asOfDate,
                GeneratedDate = DateTime.UtcNow,
                Assets = new List<BalanceSheetLineItemDTO>(),
                Liabilities = new List<BalanceSheetLineItemDTO>(),
                Equity = new List<BalanceSheetLineItemDTO>()
            };

            // Assets
            var cashAssets = await _context.TreasuryPositions
                .Where(p => p.PositionDate == asOfDate.Date)
                .GroupBy(p => p.Currency)
                .Select(g => new { Currency = g.Key, Balance = g.Sum(p => p.ClosingBalance) })
                .ToListAsync();

            balanceSheet.Assets.Add(new BalanceSheetLineItemDTO
            {
                LineItem = "Cash and Cash Equivalents",
                Amount = cashAssets.Sum(c => c.Balance),
                Percentage = 0
            });

            var accountAssets = await _context.Accounts
                .SumAsync(a => a.Balance);

            balanceSheet.Assets.Add(new BalanceSheetLineItemDTO
            {
                LineItem = "Customer Deposits",
                Amount = accountAssets,
                Percentage = 0
            });

            var investmentAssets = await _context.Investments
                .SumAsync(i => i.PrincipalAmount);

            balanceSheet.Assets.Add(new BalanceSheetLineItemDTO
            {
                LineItem = "Investment Securities",
                Amount = investmentAssets,
                Percentage = 0
            });

            var loanAssets = await _context.Loans
                .Where(l => l.Status == "ACTIVE")
                .SumAsync(l => l.Principal);

            balanceSheet.Assets.Add(new BalanceSheetLineItemDTO
            {
                LineItem = "Loans and Advances",
                Amount = loanAssets,
                Percentage = 0
            });

            var totalAssets = balanceSheet.Assets.Sum(a => a.Amount);
            balanceSheet.TotalAssets = totalAssets;

            foreach (var asset in balanceSheet.Assets)
            {
                asset.Percentage = totalAssets > 0 ? (asset.Amount / totalAssets) * 100 : 0;
            }

            // Liabilities
            var customerDeposits = accountAssets;
            balanceSheet.Liabilities.Add(new BalanceSheetLineItemDTO
            {
                LineItem = "Customer Deposits",
                Amount = customerDeposits,
                Percentage = 0
            });

            var totalLiabilities = balanceSheet.Liabilities.Sum(l => l.Amount);
            balanceSheet.TotalLiabilities = totalLiabilities;

            // Equity
            var equity = totalAssets - totalLiabilities;
            balanceSheet.Equity.Add(new BalanceSheetLineItemDTO
            {
                LineItem = "Share Capital",
                Amount = 50_000_000,
                Percentage = 0
            });

            balanceSheet.Equity.Add(new BalanceSheetLineItemDTO
            {
                LineItem = "Retained Earnings",
                Amount = Math.Max(0, equity - 50_000_000),
                Percentage = 0
            });

            var totalEquity = balanceSheet.Equity.Sum(e => e.Amount);
            balanceSheet.TotalEquity = totalEquity;

            _logger.LogInformation($"Balance sheet generated as of {asOfDate:yyyy-MM-dd}");
            return balanceSheet;
        }

        public async Task<IncomeStatementDTO> GenerateIncomeStatementAsync(DateTime periodStart, DateTime periodEnd)
        {
            var incomeStatement = new IncomeStatementDTO
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GeneratedDate = DateTime.UtcNow,
                RevenueItems = new List<IncomeStatementLineItemDTO>(),
                ExpenseItems = new List<IncomeStatementLineItemDTO>()
            };

            var periodStartDate = new DateOnly(periodStart.Year, periodStart.Month, periodStart.Day);
            var periodEndDate = new DateOnly(periodEnd.Year, periodEnd.Month, periodEnd.Day);
            
            var interestIncome = await _context.Loans
                .Where(l => l.DisbursementDate >= periodStartDate && l.DisbursementDate <= periodEndDate)
                .SumAsync(l => l.Rate * l.Principal / 100);

            incomeStatement.RevenueItems.Add(new IncomeStatementLineItemDTO
            {
                LineItem = "Interest Income",
                Amount = interestIncome
            });

            var investmentIncome = await _context.Investments
                .Where(i => i.PlacementDate >= periodStart && i.PlacementDate <= periodEnd)
                .SumAsync(i => i.InterestAmount ?? 0);

            incomeStatement.RevenueItems.Add(new IncomeStatementLineItemDTO
            {
                LineItem = "Investment Income",
                Amount = investmentIncome
            });

            incomeStatement.TotalRevenue = incomeStatement.RevenueItems.Sum(r => r.Amount);

            var staffCost = 2_000_000;
            incomeStatement.ExpenseItems.Add(new IncomeStatementLineItemDTO
            {
                LineItem = "Staff Costs",
                Amount = staffCost
            });

            var operatingCosts = 500_000;
            incomeStatement.ExpenseItems.Add(new IncomeStatementLineItemDTO
            {
                LineItem = "Operating Expenses",
                Amount = operatingCosts
            });

            incomeStatement.TotalExpenses = incomeStatement.ExpenseItems.Sum(e => e.Amount);
            incomeStatement.NetProfit = incomeStatement.TotalRevenue - incomeStatement.TotalExpenses;

            _logger.LogInformation($"Income statement generated for {periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd}");
            return incomeStatement;
        }

        public async Task<CashFlowStatementDTO> GenerateCashFlowStatementAsync(DateTime periodStart, DateTime periodEnd)
        {
            var cashFlow = new CashFlowStatementDTO
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GeneratedDate = DateTime.UtcNow,
                OperatingActivities = new List<CashFlowLineItemDTO>(),
                InvestingActivities = new List<CashFlowLineItemDTO>(),
                FinancingActivities = new List<CashFlowLineItemDTO>()
            };

            var depositInflows = await _context.Transactions
                .Where(t => t.Date >= periodStart && t.Date <= periodEnd && t.Type == "Deposit")
                .SumAsync(t => t.Amount);

            cashFlow.OperatingActivities.Add(new CashFlowLineItemDTO
            {
                Activity = "Customer Deposits",
                Amount = depositInflows,
                Category = "Inflow"
            });

            var withdrawalOutflows = await _context.Transactions
                .Where(t => t.Date >= periodStart && t.Date <= periodEnd && t.Type == "Withdrawal")
                .SumAsync(t => t.Amount);

            cashFlow.OperatingActivities.Add(new CashFlowLineItemDTO
            {
                Activity = "Customer Withdrawals",
                Amount = -withdrawalOutflows,
                Category = "Outflow"
            });

            cashFlow.NetOperatingCashFlow = cashFlow.OperatingActivities.Sum(a => a.Amount);

            var periodStartDate = new DateOnly(periodStart.Year, periodStart.Month, periodStart.Day);
            var periodEndDate = new DateOnly(periodEnd.Year, periodEnd.Month, periodEnd.Day);
            
            var investmentOutflows = await _context.Investments
                .Where(i => i.PlacementDate >= periodStart && i.PlacementDate <= periodEnd)
                .SumAsync(i => i.PrincipalAmount);

            if (investmentOutflows > 0)
            {
                cashFlow.InvestingActivities.Add(new CashFlowLineItemDTO
                {
                    Activity = "Investment Purchases",
                    Amount = -investmentOutflows,
                    Category = "Outflow"
                });
            }

            cashFlow.NetInvestingCashFlow = cashFlow.InvestingActivities.Sum(a => a.Amount);

            var loanDisbursal = await _context.Loans
                .Where(l => l.DisbursementDate.HasValue && l.DisbursementDate >= periodStartDate && l.DisbursementDate <= periodEndDate)
                .SumAsync(l => l.Principal);

            cashFlow.FinancingActivities.Add(new CashFlowLineItemDTO
            {
                Activity = "Loan Disbursals",
                Amount = -loanDisbursal,
                Category = "Outflow"
            });

            cashFlow.NetFinancingCashFlow = cashFlow.FinancingActivities.Sum(a => a.Amount);
            cashFlow.NetChangeInCash = cashFlow.NetOperatingCashFlow + cashFlow.NetInvestingCashFlow + cashFlow.NetFinancingCashFlow;

            _logger.LogInformation($"Cash flow statement generated for {periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd}");
            return cashFlow;
        }

        public async Task<TrialBalanceDTO> GenerateTrialBalanceAsync(DateTime asOfDate)
        {
            var trialBalance = new TrialBalanceDTO
            {
                AsOfDate = asOfDate,
                GeneratedDate = DateTime.UtcNow,
                Accounts = new List<TrialBalanceAccountDTO>()
            };

            var glAccounts = await _context.GlAccounts.ToListAsync();

            foreach (var glAccount in glAccounts)
            {
                var balance = await _context.JournalLines
                    .Where(t => t.AccountCode == glAccount.Code)
                    .SumAsync(t => t.Debit - t.Credit);

                if (balance != 0)
                {
                    trialBalance.Accounts.Add(new TrialBalanceAccountDTO
                    {
                        AccountNumber = glAccount.Code,
                        AccountName = glAccount.Name,
                        Balance = balance,
                        DebitBalance = balance > 0 ? balance : 0,
                        CreditBalance = balance < 0 ? Math.Abs(balance) : 0
                    });
                }
            }

            trialBalance.TotalDebits = trialBalance.Accounts.Sum(a => a.DebitBalance);
            trialBalance.TotalCredits = trialBalance.Accounts.Sum(a => a.CreditBalance);
            trialBalance.IsBalanced = Math.Abs(trialBalance.TotalDebits - trialBalance.TotalCredits) < 0.01m;

            _logger.LogInformation($"Trial balance generated as of {asOfDate:yyyy-MM-dd}");
            return trialBalance;
        }
    }
}
