using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services
{
    public interface IRegulatoryReportService
    {
        Task<DailyPositionReportDTO> GenerateDailyPositionReportAsync(DateTime reportDate);
        Task<MonthlyReturnDTO> GenerateMonthlyReturn1Async(int month, int year);
        Task<MonthlyReturnDTO> GenerateMonthlyReturn2Async(int month, int year);
        Task<MonthlyReturnDTO> GenerateMonthlyReturn3Async(int month, int year);
        Task<PrudentialReturnDTO> GeneratePrudentialReturnAsync(DateTime asOfDate);
        Task<LargeExposureReportDTO> GenerateLargeExposureReportAsync(DateTime asOfDate);
        Task<RegulatoryReturnDTO> ApproveReturnAsync(int returnId, string approvedBy);
        Task<RegulatoryReturnDTO> RejectReturnAsync(int returnId, string rejectedBy, string? reason = null);
        Task<RegulatoryReturnDTO> SubmitReturnToBogAsync(int readyReturnId, string submittedBy);
        Task<List<RegulatoryReturnDTO>> GetRegulatoryReturnsAsync(string? returnType = null);
    }

    public class RegulatoryReportService : IRegulatoryReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegulatoryReportService> _logger;
        private const string PrudentialFormulaVersion = "BOG-PRUDENTIAL-2026.03";
        private const decimal Tier2CapitalCapRatio = 0.50m;
        private const decimal ReportingLargeExposureRatio = 0.10m;
        private const decimal MaterialLargeExposureRatio = 0.25m;

        public RegulatoryReportService(ApplicationDbContext context, ILogger<RegulatoryReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DailyPositionReportDTO> GenerateDailyPositionReportAsync(DateTime reportDate)
        {
            var report = new DailyPositionReportDTO
            {
                ReportDate = reportDate,
                GeneratedAt = DateTime.UtcNow,
                Positions = new List<DailyPositionDetailDTO>()
            };

            // Get treasury positions
            var positions = await _context.TreasuryPositions
                .Where(p => p.PositionDate == reportDate.Date)
                .GroupBy(p => p.Currency)
                .Select(g => new DailyPositionDetailDTO
                {
                    Currency = g.Key,
                    ClosingBalance = g.Sum(p => p.ClosingBalance),
                    TotalDeposits = g.Sum(p => p.Deposits),
                    TotalWithdrawals = g.Sum(p => p.Withdrawals),
                    LastReconciliationDate = g.Max(p => p.ReconciledAt)
                })
                .ToListAsync();

            report.Positions = positions;
            report.TotalPositionGHS = positions.Where(p => p.Currency == "GHS").Sum(p => p.ClosingBalance);
            report.TotalPositionUSD = positions.Where(p => p.Currency == "USD").Sum(p => p.ClosingBalance);

            _logger.LogInformation($"Daily position report generated for {reportDate:yyyy-MM-dd}");
            return report;
        }

        public async Task<MonthlyReturnDTO> GenerateMonthlyReturn1Async(int month, int year)
        {
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var report = new MonthlyReturnDTO
            {
                ReturnType = "Monthly Return 1",
                ReportingPeriod = $"{month:00}/{year}",
                GeneratedDate = DateTime.UtcNow,
                ReturnDetails = new List<MonthlyReturnDetailDTO>()
            };

            // Get account data for BoG Monthly Return 1
            var accountData = await _context.Accounts
                .Where(a => a.Status != "CLOSED")
                .GroupBy(a => a.Type)
                .Select(g => new MonthlyReturnDetailDTO
                {
                    Category = $"Account Type: {g.Key}",
                    Count = g.Count(),
                    Balance = g.Sum(a => a.Balance),
                    InterestRate = 0 // Not available in Account entity
                })
                .ToListAsync();

            report.ReturnDetails = accountData;
            report.TotalAccounts = accountData.Sum(d => d.Count);
            report.TotalDepositBalance = accountData.Sum(d => d.Balance);

            _logger.LogInformation($"Monthly Return 1 generated for {month}/{year}");
            return report;
        }

        public async Task<MonthlyReturnDTO> GenerateMonthlyReturn2Async(int month, int year)
        {
            // Monthly Return 2: Loan Portfolio
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var report = new MonthlyReturnDTO
            {
                ReturnType = "Monthly Return 2",
                ReportingPeriod = $"{month:00}/{year}",
                GeneratedDate = DateTime.UtcNow,
                ReturnDetails = new List<MonthlyReturnDetailDTO>()
            };

            var loanData = await _context.Loans
                .Where(l => l.Status != "CANCELLED")
                .GroupBy(l => l.ProductCode)
                .Select(g => new MonthlyReturnDetailDTO
                {
                    Category = "Loan Product",
                    Count = g.Count(),
                    Balance = g.Sum(l => l.Principal),
                    InterestRate = g.Average(l => l.Rate)
                })
                .ToListAsync();

            report.ReturnDetails = loanData;
            report.TotalAccounts = loanData.Sum(d => d.Count);
            report.TotalDepositBalance = loanData.Sum(d => d.Balance);

            _logger.LogInformation($"Monthly Return 2 generated for {month}/{year}");
            return report;
        }

        public async Task<MonthlyReturnDTO> GenerateMonthlyReturn3Async(int month, int year)
        {
            // Monthly Return 3: Off-balance sheet items, derivatives, contingent liabilities
            var report = new MonthlyReturnDTO
            {
                ReturnType = "Monthly Return 3",
                ReportingPeriod = $"{month:00}/{year}",
                GeneratedDate = DateTime.UtcNow,
                ReturnDetails = new List<MonthlyReturnDetailDTO>
                {
                    new MonthlyReturnDetailDTO
                    {
                        Category = "FX Forward Contracts",
                        Count = await _context.FxTrades.CountAsync(),
                        Balance = await _context.FxTrades.SumAsync(t => t.BaseAmount),
                        InterestRate = 0
                    },
                    new MonthlyReturnDetailDTO
                    {
                        Category = "Investment Securities",
                        Count = await _context.Investments.CountAsync(),
                        Balance = await _context.Investments.SumAsync(i => i.PrincipalAmount),
                        InterestRate = 0
                    }
                }
            };

            report.TotalAccounts = report.ReturnDetails.Sum(d => d.Count);
            report.TotalDepositBalance = report.ReturnDetails.Sum(d => d.Balance);

            _logger.LogInformation($"Monthly Return 3 generated for {month}/{year}");
            return report;
        }

        public async Task<PrudentialReturnDTO> GeneratePrudentialReturnAsync(DateTime asOfDate)
        {
            var sourceBalances = new List<RegulatorySourceBalanceDTO>();
            var validationFindings = new List<string>();

            var report = new PrudentialReturnDTO
            {
                AsOfDate = asOfDate,
                GeneratedDate = DateTime.UtcNow,
                CapitalMetrics = new CapitalMetricsDTO(),
                RiskMetrics = new RiskMetricsDetailDTO(),
                FormulaVersion = PrudentialFormulaVersion,
                ApprovalStatus = "CALCULATED",
                ValidationFindings = validationFindings,
                SourceBalances = sourceBalances
            };

            var glAccounts = await _context.GlAccounts
                .Where(account => account.Currency == "GHS" && !account.IsHeader)
                .ToListAsync();

            var activeAccounts = await _context.Accounts
                .Where(account => account.CreatedAt <= asOfDate && !string.Equals(account.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
                .ToListAsync();

            var activeInvestments = await _context.Investments
                .Where(investment => investment.PlacementDate <= asOfDate && !string.Equals(investment.Status, "Liquidated", StringComparison.OrdinalIgnoreCase))
                .ToListAsync();

            var activeLoans = await _context.Loans
                .Include(loan => loan.Customer)
                .Where(loan =>
                    loan.ApplicationDate <= asOfDate &&
                    !string.Equals(loan.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(loan.Status, "REJECTED", StringComparison.OrdinalIgnoreCase))
                .ToListAsync();

            var retainedEarnings = glAccounts
                .Where(account => account.Code == "30200")
                .Sum(account => account.Balance);

            var currentPeriodEarnings = glAccounts
                .Where(account => string.Equals(account.Category, "INCOME", StringComparison.OrdinalIgnoreCase))
                .Sum(account => account.Balance)
                - glAccounts
                    .Where(account => string.Equals(account.Category, "EXPENSE", StringComparison.OrdinalIgnoreCase))
                    .Sum(account => account.Balance);

            var statedCapital = glAccounts
                .Where(account => account.Code == "30100")
                .Sum(account => account.Balance);

            var reserveAccounts = glAccounts
                .Where(account =>
                    string.Equals(account.Category, "EQUITY", StringComparison.OrdinalIgnoreCase) &&
                    account.Code != "30100" &&
                    account.Code != "30200")
                .Sum(account => account.Balance);

            var tier1Capital = statedCapital + retainedEarnings + currentPeriodEarnings + reserveAccounts;
            var generalLoanLossReserve = glAccounts
                .Where(account => account.Code == "15900")
                .Sum(account => Math.Abs(account.Balance));
            var tier2Capital = Math.Min(generalLoanLossReserve, Math.Max(tier1Capital, 0) * Tier2CapitalCapRatio);
            var totalCapital = Math.Max(tier1Capital, 0) + tier2Capital;

            sourceBalances.AddRange(new[]
            {
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "GL",
                    SourceCode = "30100",
                    Description = "Stated capital",
                    Amount = statedCapital
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "GL",
                    SourceCode = "30200",
                    Description = "Retained earnings",
                    Amount = retainedEarnings
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "GL",
                    SourceCode = "15900",
                    Description = "Loan loss reserve eligible for tier 2 capital",
                    Amount = tier2Capital
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "GL",
                    SourceCode = "CURRENT_PERIOD",
                    Description = "Current period earnings from GL income less expense balances",
                    Amount = currentPeriodEarnings
                }
            });

            var cashAndBankAssets = glAccounts
                .Where(account => account.Code is "10100" or "10200" or "10300")
                .Sum(account => Math.Abs(account.Balance));
            var treasuryBills = glAccounts
                .Where(account => account.Code == "11100")
                .Sum(account => Math.Abs(account.Balance));
            var placements = glAccounts
                .Where(account => account.Code == "11200")
                .Sum(account => Math.Abs(account.Balance));
            var grossLoans = activeLoans.Sum(loan => loan.OutstandingBalance ?? loan.Principal);
            var performingLoans = activeLoans
                .Where(loan => loan.ParBucket is "0" or "CURRENT")
                .Sum(loan => loan.OutstandingBalance ?? loan.Principal);
            var watchLoans = activeLoans
                .Where(loan => loan.ParBucket is "1-30" or "31-60")
                .Sum(loan => loan.OutstandingBalance ?? loan.Principal);
            var nonPerformingLoans = activeLoans
                .Where(loan => loan.ParBucket is not ("0" or "CURRENT" or "1-30" or "31-60"))
                .Sum(loan => loan.OutstandingBalance ?? loan.Principal);
            var otherAssets = glAccounts
                .Where(account =>
                    string.Equals(account.Category, "ASSET", StringComparison.OrdinalIgnoreCase) &&
                    account.Code is not ("10100" or "10200" or "10300" or "11100" or "11200" or "15100" or "15150" or "15160"))
                .Sum(account => Math.Abs(account.Balance));

            var riskWeightedAssets =
                (cashAndBankAssets * 0m) +
                (treasuryBills * 0m) +
                (placements * 0.20m) +
                (performingLoans * 1.00m) +
                (watchLoans * 1.00m) +
                (nonPerformingLoans * 1.50m) +
                (otherAssets * 1.00m);
            var customerDeposits = activeAccounts
                .Where(account => string.Equals(account.Type, "SAVINGS", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(account.Type, "CURRENT", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(account.Type, "FIXED", StringComparison.OrdinalIgnoreCase))
                .Sum(account => account.Balance);
            var investmentBook = activeInvestments.Sum(investment => investment.PrincipalAmount);

            sourceBalances.AddRange(new[]
            {
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "GL",
                    SourceCode = "10100-10300",
                    Description = "Cash and bank balances",
                    Amount = cashAndBankAssets
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "GL",
                    SourceCode = "11100",
                    Description = "Treasury bills",
                    Amount = treasuryBills
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "GL",
                    SourceCode = "11200",
                    Description = "Interbank placements and fixed deposits placed",
                    Amount = placements
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "LOAN",
                    SourceCode = "PERFORMING",
                    Description = "Performing loan book",
                    Amount = performingLoans
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "LOAN",
                    SourceCode = "WATCH",
                    Description = "Watch-list loan book",
                    Amount = watchLoans
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "LOAN",
                    SourceCode = "NON_PERFORMING",
                    Description = "Non-performing loan book",
                    Amount = nonPerformingLoans
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "GL",
                    SourceCode = "OTHER_ASSETS",
                    Description = "Other balance-sheet assets not risk-weighted separately",
                    Amount = otherAssets
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "ACCOUNT",
                    SourceCode = "DEPOSITS",
                    Description = "Customer deposit book used for prudential sense-checking",
                    Amount = customerDeposits
                },
                new RegulatorySourceBalanceDTO
                {
                    SourceType = "INVESTMENT",
                    SourceCode = "ACTIVE_BOOK",
                    Description = "Active investment book by principal amount",
                    Amount = investmentBook
                }
            });

            if (tier1Capital <= 0)
            {
                validationFindings.Add("ERROR: Tier 1 capital is non-positive based on current GL equity balances.");
            }

            if (riskWeightedAssets <= 0)
            {
                validationFindings.Add("WARN: Risk-weighted assets computed as zero; validate GL balances and active exposures.");
            }

            if (grossLoans <= 0)
            {
                validationFindings.Add("WARN: Loan portfolio is zero in prudential computation. Confirm lending balances before submission.");
            }

            if (!sourceBalances.Any(balance => balance.Amount != 0))
            {
                validationFindings.Add("ERROR: No prudential source balances were found for the requested date.");
            }

            report.CapitalMetrics = new CapitalMetricsDTO
            {
                Tier1Capital = Math.Round(Math.Max(tier1Capital, 0), 2),
                Tier2Capital = Math.Round(tier2Capital, 2),
                TotalCapital = Math.Round(totalCapital, 2),
                RiskWeightedAssets = Math.Round(riskWeightedAssets, 2),
                CapitalAdequacyRatio = riskWeightedAssets > 0 ? Math.Round((totalCapital / riskWeightedAssets) * 100, 4) : 0,
                Tier1Ratio = riskWeightedAssets > 0 ? Math.Round((Math.Max(tier1Capital, 0) / riskWeightedAssets) * 100, 4) : 0
            };

            // Risk metrics from database
            var latestRiskMetrics = await _context.RiskMetrics
                .Where(r => r.MetricDate <= asOfDate)
                .OrderByDescending(r => r.MetricDate)
                .FirstOrDefaultAsync();

            if (latestRiskMetrics != null)
            {
                // Risk metrics are stored with different MetricType values
                var varMetric = await _context.RiskMetrics
                    .Where(r => r.MetricDate <= asOfDate && r.MetricType == "VaR")
                    .OrderByDescending(r => r.MetricDate)
                    .FirstOrDefaultAsync();
                var lcrMetric = await _context.RiskMetrics
                    .Where(r => r.MetricDate <= asOfDate && r.MetricType == "LCR")
                    .OrderByDescending(r => r.MetricDate)
                    .FirstOrDefaultAsync();
                var ceMetric = await _context.RiskMetrics
                    .Where(r => r.MetricDate <= asOfDate && r.MetricType == "CurrencyExposure")
                    .OrderByDescending(r => r.MetricDate)
                    .FirstOrDefaultAsync();

                report.RiskMetrics = new RiskMetricsDetailDTO
                {
                    ValueAtRisk = varMetric?.MetricValue ?? 0,
                    LiquidityCoverageRatio = lcrMetric?.MetricValue ?? 0,
                    AverageCurrencyExposure = ceMetric?.MetricValue ?? 0
                };
            }
            else
            {
                validationFindings.Add("WARN: No reviewed risk metrics were available for the requested prudential date.");
            }

            _logger.LogInformation($"Prudential return generated as of {asOfDate:yyyy-MM-dd}");
            return report;
        }

        public async Task<LargeExposureReportDTO> GenerateLargeExposureReportAsync(DateTime asOfDate)
        {
            var sourceBalances = new List<RegulatorySourceBalanceDTO>();
            var validationFindings = new List<string>();
            var capitalBase = await CalculateCapitalBaseAsync();
            var reportingThreshold = capitalBase > 0
                ? Math.Round(capitalBase * ReportingLargeExposureRatio, 2)
                : 0;

            var report = new LargeExposureReportDTO
            {
                ReportDate = asOfDate,
                GeneratedDate = DateTime.UtcNow,
                LargeExposures = new List<LargeExposureDetailDTO>(),
                CapitalBase = capitalBase,
                ReportingThreshold = reportingThreshold,
                FormulaVersion = PrudentialFormulaVersion,
                ValidationFindings = validationFindings,
                SourceBalances = sourceBalances
            };

            var exposureCandidates = await _context.Loans
                .Include(l => l.Customer)
                .Where(l =>
                    l.ApplicationDate <= asOfDate &&
                    !string.Equals(l.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(l.Status, "REJECTED", StringComparison.OrdinalIgnoreCase))
                .GroupBy(l => new
                {
                    CustomerId = l.CustomerId ?? string.Empty,
                    CustomerName = l.Customer != null ? l.Customer.Name : "Unknown customer",
                    CustomerType = l.Customer != null ? l.Customer.Type : "Unknown"
                })
                .Select(group => new LargeExposureDetailDTO
                {
                    CustomerId = group.Key.CustomerId,
                    CustomerName = group.Key.CustomerName,
                    CustomerType = group.Key.CustomerType,
                    TotalExposure = group.Sum(loan => loan.OutstandingBalance ?? loan.Principal),
                    PercentageOfCapital = 0,
                    ExposureCategory = "Monitor",
                    BreachesReportingThreshold = false,
                    ActiveFacilityCount = group.Count()
                })
                .ToListAsync();

            foreach (var exposure in exposureCandidates)
            {
                exposure.PercentageOfCapital = capitalBase > 0
                    ? Math.Round((exposure.TotalExposure / capitalBase) * 100, 4)
                    : 0;
                exposure.BreachesReportingThreshold = reportingThreshold > 0 && exposure.TotalExposure >= reportingThreshold;
                exposure.ExposureCategory = exposure.PercentageOfCapital >= MaterialLargeExposureRatio * 100
                    ? "Material"
                    : exposure.BreachesReportingThreshold
                        ? "Reportable"
                        : "Monitor";
            }

            report.LargeExposures = exposureCandidates
                .Where(exposure => exposure.BreachesReportingThreshold)
                .OrderByDescending(exposure => exposure.TotalExposure)
                .ToList();
            report.TotalLargeExposures = report.LargeExposures.Sum(exposure => exposure.TotalExposure);

            sourceBalances.Add(new RegulatorySourceBalanceDTO
            {
                SourceType = "CAPITAL",
                SourceCode = "CAPITAL_BASE",
                Description = "Capital base used for large exposure threshold computation",
                Amount = capitalBase
            });

            if (capitalBase <= 0)
            {
                validationFindings.Add("ERROR: Capital base is non-positive. Large exposure ratios are not regulator-ready.");
            }

            if (!report.LargeExposures.Any())
            {
                validationFindings.Add("WARN: No customer exposure breaches the reporting threshold for the selected date.");
            }

            _logger.LogInformation($"Large exposure report generated with {report.LargeExposures.Count} exposures");
            return report;
        }

        public async Task<RegulatoryReturnDTO> ApproveReturnAsync(int returnId, string approvedBy)
        {
            var regulatoryReturn = await _context.RegulatoryReturns.FindAsync(returnId);
            if (regulatoryReturn == null)
                throw new KeyNotFoundException($"Return {returnId} not found");

            if (regulatoryReturn.SubmissionStatus == "Submitted")
                throw new InvalidOperationException("Submitted returns cannot be re-approved");

            regulatoryReturn.SubmissionStatus = "Approved";
            regulatoryReturn.UpdatedAt = DateTime.UtcNow;
            regulatoryReturn.ValidationErrors = MergeReviewNote(regulatoryReturn.ValidationErrors, $"Approved by {approvedBy} at {DateTime.UtcNow:O}");

            _context.RegulatoryReturns.Update(regulatoryReturn);
            await _context.SaveChangesAsync();

            return MapToDTO(regulatoryReturn);
        }

        public async Task<RegulatoryReturnDTO> RejectReturnAsync(int returnId, string rejectedBy, string? reason = null)
        {
            var regulatoryReturn = await _context.RegulatoryReturns.FindAsync(returnId);
            if (regulatoryReturn == null)
                throw new KeyNotFoundException($"Return {returnId} not found");

            if (regulatoryReturn.SubmissionStatus == "Submitted")
                throw new InvalidOperationException("Submitted returns cannot be rejected");

            regulatoryReturn.SubmissionStatus = "Rejected";
            regulatoryReturn.UpdatedAt = DateTime.UtcNow;
            regulatoryReturn.ValidationErrors = MergeReviewNote(regulatoryReturn.ValidationErrors, $"Rejected by {rejectedBy} at {DateTime.UtcNow:O}. {reason ?? "Awaiting correction."}");

            _context.RegulatoryReturns.Update(regulatoryReturn);
            await _context.SaveChangesAsync();

            return MapToDTO(regulatoryReturn);
        }
        public async Task<RegulatoryReturnDTO> SubmitReturnToBogAsync(int returnId, string submittedBy)
        {
            var regulatoryReturn = await _context.RegulatoryReturns.FindAsync(returnId);
            if (regulatoryReturn == null)
                throw new KeyNotFoundException($"Return {returnId} not found");

            var validationStatus = DetermineValidationStatus(regulatoryReturn.ValidationErrors);
            if (validationStatus == "ERROR")
                throw new InvalidOperationException("Regulatory return has blocking validation errors and cannot be submitted.");

            var definition = await GetReportDefinitionAsync(regulatoryReturn.ReturnType);
            if (definition?.RequiresApproval == true && regulatoryReturn.SubmissionStatus != "Approved")
                throw new InvalidOperationException("Regulatory return must be approved before submission.");

            regulatoryReturn.SubmissionStatus = "Submitted";
            regulatoryReturn.SubmissionDate = DateTime.UtcNow;
            regulatoryReturn.SubmittedBy = submittedBy;
            regulatoryReturn.BogReferenceNumber = $"BOG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            _context.RegulatoryReturns.Update(regulatoryReturn);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Return {returnId} submitted to BoG with reference {regulatoryReturn.BogReferenceNumber}");

            return MapToDTO(regulatoryReturn);
        }

        public async Task<List<RegulatoryReturnDTO>> GetRegulatoryReturnsAsync(string? returnType = null)
        {
            var query = _context.RegulatoryReturns.AsQueryable();

            if (!string.IsNullOrEmpty(returnType))
                query = query.Where(r => r.ReturnType == returnType);

            var returns = await query.OrderByDescending(r => r.ReturnDate).ToListAsync();
            return returns.Select(MapToDTO).ToList();
        }


        private async Task<Entities.ReportDefinition?> GetReportDefinitionAsync(string reportCode)
        {
            return await _context.ReportDefinitions.FirstOrDefaultAsync(definition => definition.ReportCode == reportCode);
        }

        private static List<string> ParseValidationErrors(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue) || rawValue == "[]")
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(rawValue) ?? new List<string>();
            }
            catch
            {
                return new List<string> { rawValue };
            }
        }

        private static string DetermineValidationStatus(string? rawValue)
        {
            var messages = ParseValidationErrors(rawValue);
            if (messages.Any(message => message.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)))
            {
                return "ERROR";
            }

            if (messages.Any(message => message.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)))
            {
                return "WARNING";
            }

            return "VALID";
        }
        private static string MergeReviewNote(string? existingValue, string note)
        {
            if (string.IsNullOrWhiteSpace(existingValue) || existingValue == "[]")
            {
                return $"[\"{note.Replace("\"", "'")}\"]";
            }

            return existingValue.TrimEnd(']') + $",\"{note.Replace("\"", "'")}\"]";
        }
        private RegulatoryReturnDTO MapToDTO(Entities.RegulatoryReturn r)
        {
            return new RegulatoryReturnDTO
            {
                Id = r.Id,
                ReturnType = r.ReturnType,
                ReturnDate = r.ReturnDate,
                SubmissionStatus = r.SubmissionStatus,
                SubmissionDate = r.SubmissionDate,
                BogReferenceNumber = r.BogReferenceNumber,
                TotalRecords = r.TotalRecords,
                CreatedAt = r.CreatedAt
            };
        }

        private async Task<decimal> CalculateCapitalBaseAsync()
        {
            var glAccounts = await _context.GlAccounts
                .Where(account => account.Currency == "GHS" && !account.IsHeader)
                .ToListAsync();

            var statedCapital = glAccounts
                .Where(account => account.Code == "30100")
                .Sum(account => account.Balance);
            var retainedEarnings = glAccounts
                .Where(account => account.Code == "30200")
                .Sum(account => account.Balance);
            var reserveAccounts = glAccounts
                .Where(account =>
                    string.Equals(account.Category, "EQUITY", StringComparison.OrdinalIgnoreCase) &&
                    account.Code != "30100" &&
                    account.Code != "30200")
                .Sum(account => account.Balance);
            var currentPeriodEarnings = glAccounts
                .Where(account => string.Equals(account.Category, "INCOME", StringComparison.OrdinalIgnoreCase))
                .Sum(account => account.Balance)
                - glAccounts
                    .Where(account => string.Equals(account.Category, "EXPENSE", StringComparison.OrdinalIgnoreCase))
                    .Sum(account => account.Balance);
            var loanLossReserve = glAccounts
                .Where(account => account.Code == "15900")
                .Sum(account => Math.Abs(account.Balance));

            var tier1Capital = statedCapital + retainedEarnings + reserveAccounts + currentPeriodEarnings;
            var tier2Capital = Math.Min(loanLossReserve, Math.Max(tier1Capital, 0) * Tier2CapitalCapRatio);
            return Math.Round(Math.Max(tier1Capital, 0) + tier2Capital, 2);
        }
    }
}
















