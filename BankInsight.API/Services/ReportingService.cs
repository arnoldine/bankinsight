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
    public interface IReportingService
    {
        Task<ReportDefinitionDTO> CreateReportDefinitionAsync(CreateReportDefinitionRequest request, string userId);
        Task<ReportDefinitionDTO> GetReportDefinitionAsync(int reportId);
        Task<List<ReportDefinitionDTO>> GetReportCatalogAsync(string? reportType = null);
        Task<ReportRunDTO> GenerateReportAsync(int reportId, Dictionary<string, object> parameters, string userId, string format = "JSON");
        Task<List<ReportRunDTO>> GetReportHistoryAsync(int reportId, int pageSize = 20);
        Task<ReportRunDTO> GetReportRunAsync(int runId);
        Task DeleteReportDefinitionAsync(int reportId);
    }

    public class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRegulatoryReportService _regulatoryReportService;
        private readonly IFinancialReportService _financialReportService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(
            ApplicationDbContext context,
            IRegulatoryReportService regulatoryReportService,
            IFinancialReportService financialReportService,
            IAnalyticsService analyticsService,
            ILogger<ReportingService> logger)
        {
            _context = context;
            _regulatoryReportService = regulatoryReportService;
            _financialReportService = financialReportService;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        public async Task<ReportDefinitionDTO> CreateReportDefinitionAsync(CreateReportDefinitionRequest request, string userId)
        {
            var reportDef = new Entities.ReportDefinition
            {
                ReportCode = $"RPT-{DateTime.UtcNow:yyyyMMddHHmmss}",
                ReportName = request.ReportName,
                Description = request.Description,
                ReportType = request.ReportType,
                DataSource = request.DataSource,
                Frequency = request.Frequency,
                TemplateFormat = request.TemplateFormat,
                TemplateContent = request.TemplateContent,
                IsActive = true,
                RequiresApproval = request.RequiresApproval,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ReportDefinitions.Add(reportDef);
            await _context.SaveChangesAsync();

            return MapToDTO(reportDef);
        }

        public async Task<ReportDefinitionDTO> GetReportDefinitionAsync(int reportId)
        {
            var report = await _context.ReportDefinitions
                .Include(r => r.Parameters)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
            {
                throw new KeyNotFoundException($"Report {reportId} not found");
            }

            return MapToDTO(report);
        }

        public async Task<List<ReportDefinitionDTO>> GetReportCatalogAsync(string? reportType = null)
        {
            IQueryable<Entities.ReportDefinition> query = _context.ReportDefinitions
                .Include(r => r.Parameters)
                .Where(r => r.IsActive);

            if (!string.IsNullOrEmpty(reportType))
            {
                query = query.Where(r => r.ReportType == reportType);
            }

            var reports = await query.OrderBy(r => r.ReportName).ToListAsync();
            return reports.Select(MapToDTO).ToList();
        }

        public async Task<ReportRunDTO> GenerateReportAsync(int reportId, Dictionary<string, object> parameters, string userId, string format = "JSON")
        {
            var reportDef = await _context.ReportDefinitions
                .Include(r => r.Runs)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (reportDef == null)
            {
                throw new KeyNotFoundException($"Report {reportId} not found");
            }

            var run = new Entities.ReportRun
            {
                ReportDefinitionId = reportId,
                RunBy = userId,
                StartedAt = DateTime.UtcNow,
                Status = "Running",
                Format = format
            };

            _context.ReportRuns.Add(run);
            await _context.SaveChangesAsync();

            try
            {
                var reportData = await GenerateReportDataAsync(reportDef, parameters);
                await PersistRegulatoryArtifactsAsync(reportDef, reportData, format, userId);

                run.RowCount = reportData.Count;
                run.Status = "Completed";
                run.CompletedAt = DateTime.UtcNow;
                run.ExecutionTimeMs = (long)(DateTime.UtcNow - run.StartedAt).TotalMilliseconds;
                run.FileName = $"{reportDef.ReportCode}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{GetFileExtension(format)}";
                run.FileSizeBytes = EstimateFileSize(reportData);

                _logger.LogInformation("Report {ReportId} generated successfully with {RowCount} records", reportId, reportData.Count);
            }
            catch (Exception ex)
            {
                run.Status = "Failed";
                run.ErrorMessage = ex.Message;
                run.CompletedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Report generation failed for {ReportId}", reportId);
            }

            _context.ReportRuns.Update(run);
            await _context.SaveChangesAsync();

            return MapRunToDTO(run);
        }

        public async Task<List<ReportRunDTO>> GetReportHistoryAsync(int reportId, int pageSize = 20)
        {
            var runs = await _context.ReportRuns
                .Where(r => r.ReportDefinitionId == reportId)
                .OrderByDescending(r => r.StartedAt)
                .Take(pageSize)
                .ToListAsync();

            return runs.Select(MapRunToDTO).ToList();
        }

        public async Task<ReportRunDTO> GetReportRunAsync(int runId)
        {
            var run = await _context.ReportRuns.FirstOrDefaultAsync(r => r.Id == runId);
            if (run == null)
            {
                throw new KeyNotFoundException($"Report run {runId} not found");
            }

            return MapRunToDTO(run);
        }

        public async Task DeleteReportDefinitionAsync(int reportId)
        {
            var report = await _context.ReportDefinitions.FindAsync(reportId);
            if (report != null)
            {
                _context.ReportDefinitions.Remove(report);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<List<Dictionary<string, object>>> GenerateReportDataAsync(Entities.ReportDefinition reportDef, Dictionary<string, object> parameters)
        {
            return reportDef.DataSource?.ToUpperInvariant() switch
            {
                "REGULATORY_DAILY_POSITION" => await GenerateDailyPositionRowsAsync(parameters),
                "REGULATORY_MONTHLY_RETURN_1" => await GenerateMonthlyReturnRowsAsync(1, parameters),
                "REGULATORY_MONTHLY_RETURN_2" => await GenerateMonthlyReturnRowsAsync(2, parameters),
                "REGULATORY_MONTHLY_RETURN_3" => await GenerateMonthlyReturnRowsAsync(3, parameters),
                "REGULATORY_PRUDENTIAL_RETURN" => await GeneratePrudentialRowsAsync(parameters),
                "REGULATORY_LARGE_EXPOSURE" => await GenerateLargeExposureRowsAsync(parameters),
                "CRB_CONSUMER_CREDIT" => await GenerateCrbCreditRowsAsync(includeBusinesses: false),
                "CRB_BUSINESS_CREDIT" => await GenerateCrbCreditRowsAsync(includeBusinesses: true),
                "CRB_CONSUMER_DISHONOURED_CHEQUE" => await GenerateDishonouredChequeRowsAsync(includeBusinesses: false),
                "CRB_BUSINESS_DISHONOURED_CHEQUE" => await GenerateDishonouredChequeRowsAsync(includeBusinesses: true),
                "CRB_CONSUMER_JUDGMENT" => await GenerateJudgmentRowsAsync(includeBusinesses: false),
                "CRB_BUSINESS_JUDGMENT" => await GenerateJudgmentRowsAsync(includeBusinesses: true),
                "ORASS_SUBMISSION_QUEUE" => await GenerateOrassSubmissionQueueRowsAsync(),
                "ORASS_VALIDATION_EXCEPTIONS" => await GenerateOrassValidationExceptionRowsAsync(),
                "FINANCIAL_BALANCE_SHEET" => await GenerateBalanceSheetRowsAsync(parameters),
                "FINANCIAL_INCOME_STATEMENT" => await GenerateIncomeStatementRowsAsync(parameters),
                "ANALYTICS_CUSTOMER_SEGMENTATION" => await GenerateCustomerSegmentationRowsAsync(parameters),
                "ANALYTICS_PRODUCT_ANALYTICS" => await GenerateProductAnalyticsRowsAsync(parameters),
                _ => new List<Dictionary<string, object>>()
            };
        }

        private async Task PersistRegulatoryArtifactsAsync(
            Entities.ReportDefinition reportDef,
            List<Dictionary<string, object>> reportData,
            string format,
            string userId)
        {
            if (!reportDef.ReportType.StartsWith("Regulatory", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var returnDate = now.Date;
            var reportingPeriodStart = new DateTime(now.Year, now.Month, 1);
            var fileName = $"{reportDef.ReportCode}_{now:yyyyMMdd_HHmmss}.{GetFileExtension(format)}";

            if (reportDef.ReportType.Contains("CRB", StringComparison.OrdinalIgnoreCase))
            {
                _context.DataExtracts.Add(new Entities.DataExtract
                {
                    ExtractName = reportDef.ReportName,
                    ExtractType = "CRB",
                    ExtractDate = returnDate,
                    RecordCount = reportData.Count,
                    FilePath = fileName,
                    FileFormat = format.ToUpperInvariant(),
                    CreatedBy = userId,
                    CreatedAt = now,
                });
            }

            var validationMessages = ValidateRegulatoryArtifact(reportDef, reportData);

            _context.RegulatoryReturns.Add(new Entities.RegulatoryReturn
            {
                ReturnType = reportDef.ReportCode,
                ReturnDate = returnDate,
                ReportingPeriodStart = reportingPeriodStart,
                ReportingPeriodEnd = returnDate,
                SubmissionStatus = reportDef.RequiresApproval ? "PendingApproval" : "Approved",
                TotalRecords = reportData.Count,
                FilePath = fileName,
                FileFormat = format.ToUpperInvariant(),
                ValidationErrors = JsonSerializer.Serialize(validationMessages),
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        private static List<string> ValidateRegulatoryArtifact(Entities.ReportDefinition reportDef, List<Dictionary<string, object>> reportData)
        {
            var issues = new List<string>();
            var source = reportDef.DataSource?.ToUpperInvariant() ?? string.Empty;

            if (source is "REGULATORY_DAILY_POSITION" or "REGULATORY_MONTHLY_RETURN_1" or "REGULATORY_MONTHLY_RETURN_2" or "REGULATORY_MONTHLY_RETURN_3" or "REGULATORY_PRUDENTIAL_RETURN")
            {
                if (reportData.Count == 0)
                {
                    issues.Add("ERROR: No data returned for a mandatory supervisory return.");
                }
            }

            if (source == "REGULATORY_DAILY_POSITION" && reportData.Count > 0 && !reportData.Any(row => string.Equals(row.GetValueOrDefault("currency")?.ToString(), "GHS", StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add("ERROR: Daily Bank Return does not contain a GHS position line.");
            }

            if (source.StartsWith("CRB_", StringComparison.OrdinalIgnoreCase))
            {
                var missingIdentifiers = reportData.Count(row => string.IsNullOrWhiteSpace(row.GetValueOrDefault("ghanaCardOrRegNo")?.ToString()));
                var missingPhones = reportData.Count(row => string.IsNullOrWhiteSpace(row.GetValueOrDefault("phone")?.ToString()));

                if (missingIdentifiers > 0)
                {
                    issues.Add($"ERROR: {missingIdentifiers} CRB rows are missing borrower identifiers.");
                }

                if (missingPhones > 0)
                {
                    issues.Add($"WARN: {missingPhones} CRB rows are missing phone numbers.");
                }

                if (reportData.Count == 0)
                {
                    issues.Add("WARN: No CRB rows were produced for this run.");
                }
            }

            if (source.StartsWith("ORASS_", StringComparison.OrdinalIgnoreCase) && reportData.Count == 0)
            {
                issues.Add("WARN: ORASS control report returned no records.");
            }

            if (source == "REGULATORY_LARGE_EXPOSURE" && reportData.Count == 0)
            {
                issues.Add("WARN: No large exposures breached the configured threshold for this run.");
            }

            return issues;
        }
        private async Task<List<Dictionary<string, object>>> GenerateDailyPositionRowsAsync(Dictionary<string, object> parameters)
        {
            var reportDate = GetDateParameter(parameters, "reportDate", DateTime.UtcNow.Date);
            var report = await _regulatoryReportService.GenerateDailyPositionReportAsync(reportDate);

            return report.Positions.Select(position => new Dictionary<string, object>
            {
                ["reportDate"] = report.ReportDate,
                ["currency"] = position.Currency,
                ["closingBalance"] = position.ClosingBalance,
                ["totalDeposits"] = position.TotalDeposits,
                ["totalWithdrawals"] = position.TotalWithdrawals,
                ["lastReconciliationDate"] = position.LastReconciliationDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                ["totalPositionGhs"] = report.TotalPositionGHS,
                ["totalPositionUsd"] = report.TotalPositionUSD,
            }).ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateMonthlyReturnRowsAsync(int returnNumber, Dictionary<string, object> parameters)
        {
            var month = GetIntParameter(parameters, "month", DateTime.UtcNow.Month);
            var year = GetIntParameter(parameters, "year", DateTime.UtcNow.Year);

            var report = returnNumber switch
            {
                1 => await _regulatoryReportService.GenerateMonthlyReturn1Async(month, year),
                2 => await _regulatoryReportService.GenerateMonthlyReturn2Async(month, year),
                _ => await _regulatoryReportService.GenerateMonthlyReturn3Async(month, year),
            };

            return report.ReturnDetails.Select(detail => new Dictionary<string, object>
            {
                ["returnType"] = report.ReturnType,
                ["reportingPeriod"] = report.ReportingPeriod,
                ["category"] = detail.Category,
                ["count"] = detail.Count,
                ["balance"] = detail.Balance,
                ["interestRate"] = detail.InterestRate,
                ["totalAccounts"] = report.TotalAccounts,
                ["totalBalance"] = report.TotalDepositBalance,
            }).ToList();
        }

        private async Task<List<Dictionary<string, object>>> GeneratePrudentialRowsAsync(Dictionary<string, object> parameters)
        {
            var asOfDate = GetDateParameter(parameters, "asOfDate", DateTime.UtcNow.Date);
            var report = await _regulatoryReportService.GeneratePrudentialReturnAsync(asOfDate);

            return new List<Dictionary<string, object>>
            {
                new()
                {
                    ["asOfDate"] = report.AsOfDate,
                    ["tier1Capital"] = report.CapitalMetrics.Tier1Capital,
                    ["tier2Capital"] = report.CapitalMetrics.Tier2Capital,
                    ["totalCapital"] = report.CapitalMetrics.TotalCapital,
                    ["riskWeightedAssets"] = report.CapitalMetrics.RiskWeightedAssets,
                    ["capitalAdequacyRatio"] = report.CapitalMetrics.CapitalAdequacyRatio,
                    ["tier1Ratio"] = report.CapitalMetrics.Tier1Ratio,
                    ["valueAtRisk"] = report.RiskMetrics.ValueAtRisk,
                    ["liquidityCoverageRatio"] = report.RiskMetrics.LiquidityCoverageRatio,
                    ["averageCurrencyExposure"] = report.RiskMetrics.AverageCurrencyExposure,
                }
            };
        }

        private async Task<List<Dictionary<string, object>>> GenerateLargeExposureRowsAsync(Dictionary<string, object> parameters)
        {
            var asOfDate = GetDateParameter(parameters, "asOfDate", DateTime.UtcNow.Date);
            var report = await _regulatoryReportService.GenerateLargeExposureReportAsync(asOfDate);

            return report.LargeExposures.Select(exposure => new Dictionary<string, object>
            {
                ["reportDate"] = report.ReportDate,
                ["customerId"] = exposure.CustomerId,
                ["customerName"] = exposure.CustomerName,
                ["customerType"] = exposure.CustomerType,
                ["totalExposure"] = exposure.TotalExposure,
                ["percentageOfCapital"] = exposure.PercentageOfCapital,
                ["exposureCategory"] = exposure.ExposureCategory,
                ["portfolioLargeExposureTotal"] = report.TotalLargeExposures,
            }).ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateCrbCreditRowsAsync(bool includeBusinesses)
        {
            var loanRows = await _context.Loans
                .Include(loan => loan.Customer)
                .Where(loan => loan.CustomerId != null && loan.Customer != null)
                .ToListAsync();

            return loanRows
                .Where(loan => IsBusinessCustomer(loan.Customer!, includeBusinesses))
                .Select(loan => new Dictionary<string, object>
                {
                    ["customerId"] = loan.CustomerId ?? string.Empty,
                    ["customerName"] = loan.Customer?.Name ?? string.Empty,
                    ["customerType"] = loan.Customer?.Type ?? string.Empty,
                    ["ghanaCardOrRegNo"] = loan.Customer?.GhanaCard ?? loan.Customer?.BusinessRegNo ?? string.Empty,
                    ["phone"] = loan.Customer?.Phone ?? string.Empty,
                    ["branchId"] = loan.BranchId,
                    ["loanId"] = loan.Id,
                    ["productCode"] = loan.ProductCode ?? loan.LoanProductId ?? string.Empty,
                    ["status"] = loan.Status,
                    ["principal"] = loan.Principal,
                    ["outstandingBalance"] = loan.OutstandingBalance ?? loan.Principal,
                    ["rate"] = loan.Rate,
                    ["termMonths"] = loan.TermMonths,
                    ["repaymentFrequency"] = loan.RepaymentFrequency,
                    ["disbursementDate"] = loan.DisbursementDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    ["parBucket"] = loan.ParBucket,
                })
                .ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateDishonouredChequeRowsAsync(bool includeBusinesses)
        {
            var rows = await _context.Transactions
                .Include(transaction => transaction.Account)
                .ThenInclude(account => account!.Customer)
                .Where(transaction =>
                    transaction.Narration != null &&
                    transaction.Narration.ToLower().Contains("dishonour"))
                .ToListAsync();

            return rows
                .Where(transaction => transaction.Account?.Customer != null && IsBusinessCustomer(transaction.Account.Customer, includeBusinesses))
                .Select(transaction => new Dictionary<string, object>
                {
                    ["transactionId"] = transaction.Id,
                    ["customerId"] = transaction.Account?.CustomerId ?? string.Empty,
                    ["customerName"] = transaction.Account?.Customer?.Name ?? string.Empty,
                    ["accountId"] = transaction.AccountId ?? string.Empty,
                    ["amount"] = transaction.Amount,
                    ["reference"] = transaction.Reference ?? string.Empty,
                    ["narration"] = transaction.Narration ?? string.Empty,
                    ["date"] = transaction.Date,
                    ["branchId"] = transaction.Account?.BranchId ?? string.Empty,
                })
                .ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateJudgmentRowsAsync(bool includeBusinesses)
        {
            var rows = await _context.Loans
                .Include(loan => loan.Customer)
                .Where(loan => loan.Customer != null && (loan.ParBucket == "90+" || loan.Status == "WRITTEN_OFF"))
                .ToListAsync();

            return rows
                .Where(loan => loan.Customer != null && IsBusinessCustomer(loan.Customer, includeBusinesses))
                .Select(loan => new Dictionary<string, object>
                {
                    ["customerId"] = loan.CustomerId ?? string.Empty,
                    ["customerName"] = loan.Customer?.Name ?? string.Empty,
                    ["customerType"] = loan.Customer?.Type ?? string.Empty,
                    ["loanId"] = loan.Id,
                    ["status"] = loan.Status,
                    ["outstandingBalance"] = loan.OutstandingBalance ?? loan.Principal,
                    ["parBucket"] = loan.ParBucket,
                    ["classificationReason"] = "Severe delinquency / judgment-equivalent watchlist",
                })
                .ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateOrassSubmissionQueueRowsAsync()
        {
            var items = await _context.RegulatoryReturns
                .OrderByDescending(item => item.ReturnDate)
                .ToListAsync();

            return items.Select(item => new Dictionary<string, object>
            {
                ["returnId"] = item.Id,
                ["returnType"] = item.ReturnType,
                ["returnDate"] = item.ReturnDate,
                ["submissionStatus"] = item.SubmissionStatus,
                ["totalRecords"] = item.TotalRecords,
                ["fileFormat"] = item.FileFormat ?? string.Empty,
                ["filePath"] = item.FilePath ?? string.Empty,
                ["bogReferenceNumber"] = item.BogReferenceNumber ?? string.Empty,
            }).ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateOrassValidationExceptionRowsAsync()
        {
            var items = await _context.RegulatoryReturns
                .Where(item =>
                    item.SubmissionStatus == "Rejected" ||
                    (item.ValidationErrors != null && item.ValidationErrors != "[]"))
                .OrderByDescending(item => item.ReturnDate)
                .ToListAsync();

            return items.Select(item => new Dictionary<string, object>
            {
                ["returnId"] = item.Id,
                ["returnType"] = item.ReturnType,
                ["returnDate"] = item.ReturnDate,
                ["submissionStatus"] = item.SubmissionStatus,
                ["validationErrors"] = item.ValidationErrors ?? "[]",
                ["submittedBy"] = item.SubmittedBy ?? string.Empty,
                ["submissionDate"] = item.SubmissionDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            }).ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateBalanceSheetRowsAsync(Dictionary<string, object> parameters)
        {
            var asOfDate = GetDateParameter(parameters, "asOfDate", DateTime.UtcNow.Date);
            var report = await _financialReportService.GenerateBalanceSheetAsync(asOfDate);

            return report.Assets.Select(item => new Dictionary<string, object>
            {
                ["section"] = "Asset",
                ["lineItem"] = item.LineItem,
                ["amount"] = item.Amount,
                ["percentage"] = item.Percentage,
                ["asOfDate"] = report.AsOfDate,
            }).Concat(report.Liabilities.Select(item => new Dictionary<string, object>
            {
                ["section"] = "Liability",
                ["lineItem"] = item.LineItem,
                ["amount"] = item.Amount,
                ["percentage"] = item.Percentage,
                ["asOfDate"] = report.AsOfDate,
            })).Concat(report.Equity.Select(item => new Dictionary<string, object>
            {
                ["section"] = "Equity",
                ["lineItem"] = item.LineItem,
                ["amount"] = item.Amount,
                ["percentage"] = item.Percentage,
                ["asOfDate"] = report.AsOfDate,
            })).ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateIncomeStatementRowsAsync(Dictionary<string, object> parameters)
        {
            var periodStart = GetDateParameter(parameters, "periodStart", new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1));
            var periodEnd = GetDateParameter(parameters, "periodEnd", DateTime.UtcNow.Date);
            var report = await _financialReportService.GenerateIncomeStatementAsync(periodStart, periodEnd);

            return report.RevenueItems.Select(item => new Dictionary<string, object>
            {
                ["section"] = "Revenue",
                ["lineItem"] = item.LineItem,
                ["amount"] = item.Amount,
                ["periodStart"] = report.PeriodStart,
                ["periodEnd"] = report.PeriodEnd,
            }).Concat(report.ExpenseItems.Select(item => new Dictionary<string, object>
            {
                ["section"] = "Expense",
                ["lineItem"] = item.LineItem,
                ["amount"] = item.Amount,
                ["periodStart"] = report.PeriodStart,
                ["periodEnd"] = report.PeriodEnd,
            })).ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateCustomerSegmentationRowsAsync(Dictionary<string, object> parameters)
        {
            var asOfDate = GetDateParameter(parameters, "asOfDate", DateTime.UtcNow.Date);
            var report = await _analyticsService.GetCustomerSegmentationAsync(asOfDate);

            return report.Segments.Select(segment => new Dictionary<string, object>
            {
                ["segmentName"] = segment.SegmentName,
                ["customerCount"] = segment.CustomerCount,
                ["totalBalance"] = segment.TotalBalance,
                ["averageBalance"] = segment.AverageBalance,
                ["averageAge"] = segment.AverageAge,
                ["churnRate"] = segment.ChurnRate,
                ["asOfDate"] = report.AsOfDate,
            }).ToList();
        }

        private async Task<List<Dictionary<string, object>>> GenerateProductAnalyticsRowsAsync(Dictionary<string, object> parameters)
        {
            var asOfDate = GetDateParameter(parameters, "asOfDate", DateTime.UtcNow.Date);
            var report = await _analyticsService.GetProductAnalyticsAsync(asOfDate);

            return report.ProductMetrics.Select(metric => new Dictionary<string, object>
            {
                ["productId"] = metric.ProductId,
                ["productName"] = metric.ProductName,
                ["productType"] = metric.ProductType,
                ["accountCount"] = metric.AccountCount,
                ["totalBalance"] = metric.TotalBalance,
                ["averageBalance"] = metric.AverageBalance,
                ["interestRate"] = metric.InterestRate,
                ["revenueContribution"] = metric.RevenueContribution,
                ["asOfDate"] = report.AsOfDate,
            }).ToList();
        }

        private long EstimateFileSize(List<Dictionary<string, object>> data)
        {
            return Math.Max(1024, data.Count * 200);
        }

        private string GetFileExtension(string format)
        {
            return format.ToLower() switch
            {
                "excel" => "xlsx",
                "pdf" => "pdf",
                "csv" => "csv",
                "json" => "json",
                _ => "txt"
            };
        }

        private ReportDefinitionDTO MapToDTO(Entities.ReportDefinition report)
        {
            return new ReportDefinitionDTO
            {
                Id = report.Id,
                ReportCode = report.ReportCode,
                ReportName = report.ReportName,
                Description = report.Description,
                ReportType = report.ReportType,
                Frequency = report.Frequency,
                TemplateFormat = report.TemplateFormat,
                IsActive = report.IsActive,
                RequiresApproval = report.RequiresApproval,
                CreatedAt = report.CreatedAt
            };
        }

        private ReportRunDTO MapRunToDTO(Entities.ReportRun run)
        {
            return new ReportRunDTO
            {
                Id = run.Id,
                ReportDefinitionId = run.ReportDefinitionId,
                Status = run.Status,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                FileName = run.FileName,
                RowCount = run.RowCount,
                Format = run.Format,
                ExecutionTimeMs = run.ExecutionTimeMs
            };
        }

        private static bool IsBusinessCustomer(Entities.Customer customer, bool includeBusinesses)
        {
            var customerType = customer.Type?.ToUpperInvariant() ?? string.Empty;
            var isBusiness = customerType.Contains("BUSINESS") || customerType.Contains("CORPORATE") || customerType.Contains("COMPANY");
            return includeBusinesses ? isBusiness : !isBusiness;
        }

        private static DateTime GetDateParameter(Dictionary<string, object> parameters, string key, DateTime fallback)
        {
            if (parameters.TryGetValue(key, out var raw) && DateTime.TryParse(raw?.ToString(), out var parsed))
            {
                return parsed.Date;
            }

            return fallback.Date;
        }

        private static int GetIntParameter(Dictionary<string, object> parameters, string key, int fallback)
        {
            if (parameters.TryGetValue(key, out var raw) && int.TryParse(raw?.ToString(), out var parsed))
            {
                return parsed;
            }

            return fallback;
        }
    }
}






