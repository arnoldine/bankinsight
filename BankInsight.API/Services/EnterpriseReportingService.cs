using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IEnterpriseReportingService
{
    Task<List<ReportCatalogEntryDTO>> GetCatalogAsync();
    Task<ReportCatalogEntryDTO> GetCatalogEntryAsync(string reportCode);
    Task<ReportExecutionResponseDTO> ExecuteAsync(string reportCode, ReportExecutionRequestDTO request, bool persistRun = true);
    Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(string reportCode, string format, ReportExecutionRequestDTO request);
    Task<List<ReportHistoryItemDTO>> GetHistoryAsync(int take = 50);
    Task<List<ReportFavoriteDTO>> GetFavoritesAsync();
    Task AddFavoriteAsync(string reportCode);
    Task RemoveFavoriteAsync(string reportCode);
    Task<List<ReportFilterPresetDTO>> GetPresetsAsync(string reportCode);
    Task<ReportFilterPresetDTO> SavePresetAsync(string reportCode, SaveReportFilterPresetRequestDTO request);
    Task DeletePresetAsync(Guid presetId);
    Task<CrbDataQualityDashboardDTO> GetCrbDataQualityAsync();
}

public class EnterpriseReportingService : IEnterpriseReportingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext _context;
    private readonly IReportCatalogRegistry _catalogRegistry;
    private readonly IReportExportService _exportService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<EnterpriseReportingService> _logger;

    public EnterpriseReportingService(
        ApplicationDbContext context,
        IReportCatalogRegistry catalogRegistry,
        IReportExportService exportService,
        ICurrentUserContext currentUser,
        ILogger<EnterpriseReportingService> logger)
    {
        _context = context;
        _catalogRegistry = catalogRegistry;
        _exportService = exportService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<List<ReportCatalogEntryDTO>> GetCatalogAsync()
    {
        var favoriteCodes = await _context.ReportFavorites
            .Where(x => x.StaffId == CurrentStaffId)
            .Select(x => x.ReportCode)
            .ToListAsync();

        return _catalogRegistry.GetAll()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.SubCategory)
            .ThenBy(x => x.ReportName)
            .Select(definition => MapCatalog(definition, favoriteCodes.Contains(definition.ReportCode, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }

    public Task<ReportCatalogEntryDTO> GetCatalogEntryAsync(string reportCode)
    {
        var definition = GetDefinition(reportCode);
        return Task.FromResult(MapCatalog(definition, false));
    }

    public async Task<ReportExecutionResponseDTO> ExecuteAsync(string reportCode, ReportExecutionRequestDTO request, bool persistRun = true)
    {
        var definition = GetDefinition(reportCode);
        var stopwatch = Stopwatch.StartNew();
        var reportDefinition = await EnsureReportDefinitionAsync(definition);
        ReportRun? run = null;

        if (persistRun)
        {
            var startedAt = DateTime.UtcNow;
            run = new ReportRun
            {
                ReportDefinitionId = reportDefinition.Id,
                RunBy = CurrentStaffId,
                Status = "Running",
                Format = "JSON",
                StartedAt = startedAt,
                FileName = $"{definition.ReportCode}_{startedAt:yyyyMMdd_HHmmss}.pending",
                FilePath = $"pending/{definition.ReportCode}_{startedAt:yyyyMMdd_HHmmss}.json",
                ErrorMessage = string.Empty,
            };
            _context.ReportRuns.Add(run);
            await _context.SaveChangesAsync();
        }

        try
        {
            var fullRows = await GenerateRowsAsync(definition, request.Parameters);
            var sortedRows = SortRows(fullRows, request.SortBy ?? definition.DefaultSort, request.SortDirection);
            var columns = sortedRows.SelectMany(row => row.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 500);
            var pagedRows = sortedRows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var validationMessages = BuildValidationMessages(definition, fullRows);
            var summary = BuildSummary(definition, fullRows);
            var maskedRows = ApplyMaskingIfRequired(definition, pagedRows, out var isMasked);
            stopwatch.Stop();

            if (run != null)
            {
                run.Status = "Completed";
                run.RowCount = fullRows.Count;
                run.CompletedAt = DateTime.UtcNow;
                run.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                run.FileName = $"{definition.ReportCode}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                run.FileSizeBytes = Math.Max(1024, fullRows.Count * 256);
                await PersistArtifactsAsync(definition, fullRows, validationMessages, "JSON");
                await CreateAuditLogAsync("REPORT_VIEW", definition.ReportCode, $"Previewed {definition.ReportName}", true, new { definition.ReportCode, request.Parameters, RowCount = fullRows.Count });
                await _context.SaveChangesAsync();
            }

            return new ReportExecutionResponseDTO
            {
                ReportCode = definition.ReportCode,
                ReportName = definition.ReportName,
                Category = definition.Category,
                SubCategory = definition.SubCategory,
                RunId = run?.Id,
                GeneratedAt = DateTime.UtcNow,
                Columns = columns,
                Rows = maskedRows,
                TotalRows = fullRows.Count,
                Page = page,
                PageSize = pageSize,
                Summary = summary,
                AppliedFilters = request.Parameters,
                ValidationMessages = validationMessages,
                IsMasked = isMasked,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Enterprise report {ReportCode} failed", reportCode);
            if (run != null)
            {
                run.Status = "Failed";
                run.ErrorMessage = ex.Message;
                run.CompletedAt = DateTime.UtcNow;
                run.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                await CreateAuditLogAsync("REPORT_VIEW", definition.ReportCode, $"Failed to preview {definition.ReportName}", false, new { definition.ReportCode, request.Parameters }, ex.Message);
                await _context.SaveChangesAsync();
            }
            throw;
        }
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(string reportCode, string format, ReportExecutionRequestDTO request)
    {
        var definition = GetDefinition(reportCode);
        var execution = await ExecuteAsync(reportCode, request, persistRun: true);
        var export = _exportService.Export(execution, format, CurrentStaffId, await GetInstitutionNameAsync());
        await CreateAuditLogAsync("REPORT_EXPORT", definition.ReportCode, $"Exported {definition.ReportName} as {format}", true, new { definition.ReportCode, format, request.Parameters, execution.TotalRows });
        await _context.SaveChangesAsync();
        return export;
    }

    public async Task<List<ReportHistoryItemDTO>> GetHistoryAsync(int take = 50)
    {
        var definitions = await _context.ReportDefinitions.ToDictionaryAsync(x => x.Id, x => x);
        return await _context.ReportRuns
            .OrderByDescending(x => x.StartedAt)
            .Take(Math.Clamp(take, 1, 200))
            .Select(run => new ReportHistoryItemDTO
            {
                RunId = run.Id,
                ReportCode = definitions.ContainsKey(run.ReportDefinitionId) ? definitions[run.ReportDefinitionId].ReportCode : string.Empty,
                ReportName = definitions.ContainsKey(run.ReportDefinitionId) ? definitions[run.ReportDefinitionId].ReportName : string.Empty,
                Status = run.Status,
                Format = run.Format ?? "JSON",
                FileName = run.FileName,
                RowCount = run.RowCount,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                ExecutionTimeMs = run.ExecutionTimeMs,
                GeneratedBy = run.RunBy ?? string.Empty,
                ActionType = "RUN"
            })
            .ToListAsync();
    }

    public async Task<List<ReportFavoriteDTO>> GetFavoritesAsync()
    {
        return await _context.ReportFavorites
            .Where(x => x.StaffId == CurrentStaffId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReportFavoriteDTO
            {
                ReportCode = x.ReportCode,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task AddFavoriteAsync(string reportCode)
    {
        _ = GetDefinition(reportCode);
        var existing = await _context.ReportFavorites.FirstOrDefaultAsync(x => x.StaffId == CurrentStaffId && x.ReportCode == reportCode);
        if (existing == null)
        {
            _context.ReportFavorites.Add(new ReportFavorite
            {
                StaffId = CurrentStaffId,
                ReportCode = reportCode,
                CreatedAt = DateTime.UtcNow,
            });
            await CreateAuditLogAsync("REPORT_FAVORITE", reportCode, $"Added {reportCode} to favorites", true, null);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveFavoriteAsync(string reportCode)
    {
        var existing = await _context.ReportFavorites.FirstOrDefaultAsync(x => x.StaffId == CurrentStaffId && x.ReportCode == reportCode);
        if (existing != null)
        {
            _context.ReportFavorites.Remove(existing);
            await CreateAuditLogAsync("REPORT_FAVORITE", reportCode, $"Removed {reportCode} from favorites", true, null);
            await _context.SaveChangesAsync();
        }
    }
    public async Task<List<ReportFilterPresetDTO>> GetPresetsAsync(string reportCode)
    {
        return await _context.ReportFilterPresets
            .Where(x => x.StaffId == CurrentStaffId && x.ReportCode == reportCode)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new ReportFilterPresetDTO
            {
                Id = x.Id,
                ReportCode = x.ReportCode,
                PresetName = x.PresetName,
                Parameters = DeserializeParameters(x.ParametersJson),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            })
            .ToListAsync();
    }

    public async Task<ReportFilterPresetDTO> SavePresetAsync(string reportCode, SaveReportFilterPresetRequestDTO request)
    {
        _ = GetDefinition(reportCode);
        var now = DateTime.UtcNow;
        var preset = await _context.ReportFilterPresets.FirstOrDefaultAsync(x => x.StaffId == CurrentStaffId && x.ReportCode == reportCode && x.PresetName == request.PresetName);
        if (preset == null)
        {
            preset = new ReportFilterPreset
            {
                StaffId = CurrentStaffId,
                ReportCode = reportCode,
                PresetName = request.PresetName,
                ParametersJson = JsonSerializer.Serialize(request.Parameters, JsonOptions),
                CreatedAt = now,
                UpdatedAt = now,
            };
            _context.ReportFilterPresets.Add(preset);
        }
        else
        {
            preset.ParametersJson = JsonSerializer.Serialize(request.Parameters, JsonOptions);
            preset.UpdatedAt = now;
        }

        await CreateAuditLogAsync("REPORT_PRESET", reportCode, $"Saved preset {request.PresetName}", true, request.Parameters);
        await _context.SaveChangesAsync();

        return new ReportFilterPresetDTO
        {
            Id = preset.Id,
            ReportCode = preset.ReportCode,
            PresetName = preset.PresetName,
            Parameters = request.Parameters,
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt,
        };
    }

    public async Task DeletePresetAsync(Guid presetId)
    {
        var preset = await _context.ReportFilterPresets.FirstOrDefaultAsync(x => x.Id == presetId && x.StaffId == CurrentStaffId);
        if (preset != null)
        {
            _context.ReportFilterPresets.Remove(preset);
            await CreateAuditLogAsync("REPORT_PRESET", preset.ReportCode, $"Deleted preset {preset.PresetName}", true, null);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<CrbDataQualityDashboardDTO> GetCrbDataQualityAsync()
    {
        var checks = await _context.CreditBureauChecks.Include(x => x.Loan).ThenInclude(x => x!.Customer).ToListAsync();
        var returns = await _context.RegulatoryReturns.Where(x => x.ReturnType.Contains("CRB")).ToListAsync();

        var consumerChecks = checks.Where(x => x.Loan?.Customer != null && !IsBusiness(x.Loan.Customer)).ToList();
        var businessChecks = checks.Where(x => x.Loan?.Customer != null && IsBusiness(x.Loan.Customer)).ToList();

        var missingConsumer = consumerChecks.Count(x => string.IsNullOrWhiteSpace(x.Loan?.Customer?.GhanaCard) && string.IsNullOrWhiteSpace(x.Loan?.Customer?.Tin));
        var missingBusiness = businessChecks.Count(x => string.IsNullOrWhiteSpace(x.Loan?.Customer?.BusinessRegNo) || string.IsNullOrWhiteSpace(x.Loan?.Customer?.Tin));
        var failed = checks.Count(x => !string.Equals(x.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase));
        var rejected = returns.Count(x => string.Equals(x.SubmissionStatus, "Rejected", StringComparison.OrdinalIgnoreCase));
        var resubmissions = returns.Count(x => string.Equals(x.SubmissionStatus, "PendingApproval", StringComparison.OrdinalIgnoreCase) || string.Equals(x.SubmissionStatus, "Draft", StringComparison.OrdinalIgnoreCase));
        var pending = consumerChecks.Count(x => string.IsNullOrWhiteSpace(x.Status) || string.Equals(x.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            + businessChecks.Count(x => string.IsNullOrWhiteSpace(x.Status) || string.Equals(x.Status, "PENDING", StringComparison.OrdinalIgnoreCase));

        return new CrbDataQualityDashboardDTO
        {
            TotalChecks = checks.Count,
            FailedChecks = failed,
            MissingMandatoryConsumerFields = missingConsumer,
            MissingMandatoryBusinessFields = missingBusiness,
            PendingSubmissionReadinessItems = pending,
            RejectedRecords = rejected,
            ResubmissionCandidates = resubmissions,
            Breakdown = new List<ReportSummaryMetricDTO>
            {
                Metric("Consumer checks", consumerChecks.Count),
                Metric("Business checks", businessChecks.Count),
                Metric("Rejected extracts", rejected),
                Metric("Resubmission queue", resubmissions),
            }
        };
    }

    private async Task<ReportDefinition> EnsureReportDefinitionAsync(ReportCatalogDefinition definition)
    {
        var entity = await _context.ReportDefinitions.FirstOrDefaultAsync(x => x.ReportCode == definition.ReportCode);
        if (entity == null)
        {
            entity = new ReportDefinition
            {
                ReportCode = definition.ReportCode,
                ReportName = definition.ReportName,
                Description = definition.Description,
                ReportType = definition.Category,
                DataSource = definition.DataSource,
                Frequency = "OnDemand",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                IsActive = definition.IsActive,
                RequiresApproval = definition.RequiresApprovalBeforeFinalExport,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.ReportDefinitions.Add(entity);
            await _context.SaveChangesAsync();
        }
        return entity;
    }

    private async Task<List<Dictionary<string, object?>>> GenerateRowsAsync(ReportCatalogDefinition definition, Dictionary<string, string> parameters)
    {
        return definition.DataSource switch
        {
            "REG_CAPITAL_ADEQUACY" => await GenerateCapitalAdequacyAsync(parameters),
            "REG_LIQUIDITY" => await GenerateLiquidityAsync(parameters),
            "REG_ASSET_QUALITY" => await GenerateAssetQualityAsync(parameters),
            "REG_PROVISIONING" => await GenerateProvisioningAsync(parameters),
            "REG_LARGE_EXPOSURE" => await GenerateLargeExposureAsync(parameters),
            "REG_CONCENTRATION" => await GenerateConcentrationAsync(parameters),
            "REG_ARREARS_AGING" => await GenerateArrearsAgingAsync(parameters),
            "REG_FEES_CHARGES" => await GenerateFeesChargesAsync(parameters),
            "REG_INCIDENTS" => await GenerateCashIncidentsAsync(parameters),
            "REG_NPL_SUMMARY" => await GenerateArrearsAgingAsync(parameters),
            "REG_RESTRUCTURED_LOANS" => await GenerateDelinquentLoansAsync(parameters),
            "REG_WRITEOFF_RECOVERIES" => await GenerateWriteoffMovementAsync(),
            "REG_TOP_OBLIGORS" => await GenerateLargeExposureAsync(parameters),
            "REG_BRANCH_REG_SUMMARY" => await GenerateBranchProfitabilityAsync(parameters),
            "OPS_NEW_CUSTOMERS" => await GenerateNewCustomersAsync(parameters),
            "OPS_DORMANT_CUSTOMERS" => await GenerateDormantCustomersAsync(parameters),
            "OPS_KYC_INCOMPLETE" => await GenerateKycIncompleteAsync(parameters),
            "OPS_DEPOSIT_TRANSACTIONS" => await GenerateDepositTransactionsAsync(parameters),
            "OPS_DEPOSIT_BALANCES" => await GenerateDepositBalancesAsync(parameters),
            "OPS_BRANCH_DEPOSIT_PERF" => await GenerateBranchDepositPerformanceAsync(parameters),
            "OPS_LOAN_DISBURSEMENTS" => await GenerateLoanDisbursementsAsync(parameters),
            "OPS_LOAN_REPAYMENTS" => await GenerateLoanRepaymentsAsync(parameters),
            "OPS_DUE_INSTALLMENTS" => await GenerateDueInstallmentsAsync(parameters),
            "OPS_PAR" => await GenerateParAsync(parameters),
            "OPS_DELINQUENT_LOANS" => await GenerateDelinquentLoansAsync(parameters),
            "OPS_GROUP_LENDING" => await GenerateGroupLendingAsync(parameters),
            "OPS_BRANCH_TRANSACTIONS" => await GenerateBranchTransactionsAsync(parameters),
            "OPS_TELLER_SUMMARY" => await GenerateTellerSummaryAsync(parameters),
            "OPS_VAULT_CASH" => await GenerateVaultCashAsync(parameters),
            "OPS_CASH_INCIDENTS" => await GenerateCashIncidentsAsync(parameters),
            "OPS_PENDING_APPROVALS" => await GeneratePendingApprovalsAsync(parameters),
            "OPS_REVERSED_TRANSACTIONS" => await GenerateReversedTransactionsAsync(parameters),
            "OPS_USER_ACTIVITY" => await GenerateUserActivityAsync(parameters),
            "OPS_INACTIVE_USERS" => await GenerateInactiveUsersAsync(parameters),
            "OPS_CUSTOMER_GROWTH_TREND" => await GenerateCustomerGrowthTrendAsync(parameters),
            "OPS_CUSTOMER_SEGMENTATION" => await GenerateKycIncompleteAsync(parameters),
            "OPS_ACCOUNT_OPENING_TRENDS" => await GenerateDepositBalancesAsync(parameters),
            "OPS_CUSTOMER_STATUS" => await GenerateDormantCustomersAsync(parameters),
            "OPS_DEPOSIT_INFLOW_OUTFLOW" => await GenerateBranchTransactionsAsync(parameters),
            "OPS_OFFICER_COLLECTIONS" => await GenerateLoanRepaymentsAsync(parameters),
            "OPS_MATURED_LOANS" => await GenerateDelinquentLoansAsync(parameters),
            "OPS_WRITTENOFF_LOANS" => await GenerateWriteoffMovementAsync(),
            "OPS_RECOVERIES" => await GenerateWriteoffMovementAsync(),
            "OPS_BRANCH_LOAN_PERF" => await GenerateLoanPerformanceByBranchAsync(parameters),
            "OPS_PRODUCT_LOAN_PERF" => await GenerateProductProfitabilityAsync(parameters),
            "OPS_FAILED_POSTINGS" => await GenerateAuditExceptionsAsync(parameters),
            "CRB_CONSUMER_INQUIRY_LOG" => await GenerateCrbInquiriesAsync(false),
            "CRB_BUSINESS_INQUIRY_LOG" => await GenerateCrbInquiriesAsync(true),
            "CRB_CONSUMER_MISSING_CHECKS" => await GenerateMissingChecksAsync(parameters, false),
            "CRB_BUSINESS_MISSING_CHECKS" => await GenerateMissingChecksAsync(parameters, true),
            "CRB_PROVIDER_PERFORMANCE" => await GenerateProviderPerformanceAsync(),
            "CRB_APPROVAL_BY_GRADE" => await GenerateApprovalByGradeAsync(),
            "CRB_FAILED_INTEGRATION" => await GenerateFailedIntegrationsAsync(),
            "CRB_DATA_QUALITY" => await GenerateCrbDataQualityRowsAsync(),
            "CRB_REJECTIONS" => await GenerateCrbRejectionsAsync(true),
            "CRB_RESUBMISSIONS" => await GenerateCrbRejectionsAsync(false),
            "CRB_CONSUMER_CREDIT_EXTRACT" => await GenerateConsumerCreditExtractAsync(parameters),
            "CRB_BUSINESS_CREDIT_EXTRACT" => await GenerateBusinessCreditExtractAsync(parameters),
            "CRB_CONSUMER_DISHONOURED_EXTRACT" => await GenerateDishonouredChequeExtractAsync(parameters, false),
            "CRB_BUSINESS_DISHONOURED_EXTRACT" => await GenerateDishonouredChequeExtractAsync(parameters, true),
            "CRB_CONSUMER_JUDGMENT_EXTRACT" => await GenerateJudgmentExtractAsync(parameters, false),
            "CRB_BUSINESS_JUDGMENT_EXTRACT" => await GenerateJudgmentExtractAsync(parameters, true),
            "CRB_CONSUMER_APPLICATION_SCORES" => await GenerateCrbApplicationScoresAsync(false),
            "CRB_BUSINESS_APPLICATION_SCORES" => await GenerateCrbApplicationScoresAsync(true),
            "CRB_CONSUMER_DECLINED_BY_BUREAU" => await GenerateCrbInquiriesAsync(false),
            "CRB_BUSINESS_DECLINED_BY_BUREAU" => await GenerateCrbInquiriesAsync(true),
            "CRB_CONSUMER_EXPOSURE_COMPARISON" => await GenerateConsumerCreditExtractAsync(parameters),
            "CRB_BUSINESS_EXPOSURE_COMPARISON" => await GenerateBusinessCreditExtractAsync(parameters),
            "CRB_CONSUMER_DEFAULT_SUMMARY" => await GenerateApprovalByGradeAsync(),
            "CRB_BUSINESS_DEFAULT_SUMMARY" => await GenerateApprovalByGradeAsync(),
            "CRB_CONSUMER_OVERDUE_LAST_CHECK" => await GenerateConsumerCreditExtractAsync(parameters),
            "CRB_BUSINESS_OVERDUE_LAST_CHECK" => await GenerateBusinessCreditExtractAsync(parameters),
            "CRB_CONSUMER_SUBMISSION_READINESS" => await GenerateCrbSubmissionReadinessAsync(parameters, false),
            "CRB_BUSINESS_SUBMISSION_READINESS" => await GenerateCrbSubmissionReadinessAsync(parameters, true),
            "CRB_CONSUMER_DISHONOURED_REPORT" => await GenerateDishonouredChequeExtractAsync(parameters, false),
            "CRB_BUSINESS_DISHONOURED_REPORT" => await GenerateDishonouredChequeExtractAsync(parameters, true),
            "CRB_CONSUMER_JUDGMENT_REPORT" => await GenerateJudgmentExtractAsync(parameters, false),
            "CRB_BUSINESS_JUDGMENT_REPORT" => await GenerateJudgmentExtractAsync(parameters, true),
            "CRB_PENDING_DATA_COMPLETION" => await GenerateCrbDataQualityRowsAsync(),
            "CRB_MISSING_MANDATORY_FIELDS" => await GenerateCrbDataQualityRowsAsync(),
            "CRB_INQUIRY_VOLUME_COMPARISON" => await GenerateCrbDataQualityRowsAsync(),
            "CRB_BRANCH_USAGE" => await GenerateCrbInquiriesAsync(false),
            "CRB_OFFICER_USAGE" => await GenerateCrbInquiriesAsync(true),
            "CRB_PRODUCT_DEPENDENCY" => await GenerateProviderPerformanceAsync(),
            "CRB_AVERAGE_SCORE_BY_SEGMENT" => await GenerateApprovalByGradeAsync(),
            "CRB_PENDING_RESPONSES" => await GenerateFailedIntegrationsAsync(),
            "CRB_BUREAU_OVERRIDE" => await GenerateApprovalByGradeAsync(),
            "CRB_REJECTION_TREND" => await GenerateCrbRejectionsAsync(true),
            "FIN_TRIAL_BALANCE_DETAIL" => await GenerateTrialBalanceDetailAsync(),
            "FIN_TRIAL_BALANCE_SUMMARY" => await GenerateTrialBalanceSummaryAsync(),
            "FIN_GL_TRANSACTIONS" => await GenerateGlTransactionsAsync(),
            "FIN_JOURNAL_LISTING" => await GenerateJournalListingAsync(),
            "FIN_BALANCE_SHEET" => await GenerateBalanceSheetAsync(),
            "FIN_INCOME_STATEMENT" => await GenerateIncomeStatementAsync(),
            "FIN_CASH_FLOW" => await GenerateCashFlowAsync(parameters),
            "FIN_LOANS_ADVANCES" => await GenerateLoansAdvancesAsync(parameters),
            "FIN_CUSTOMER_DEPOSITS" => await GenerateCustomerDepositsAsync(parameters),
            "FIN_IMPAIRMENT_SUPPORT" => await GenerateImpairmentSupportAsync(),
            "FIN_WRITEOFF_MOVEMENT" => await GenerateWriteoffMovementAsync(),
            "FIN_LEDGER_STATEMENT" => await GenerateLedgerStatementAsync(),
            "FIN_ACCOUNT_ACTIVITY" => await GenerateCustomerDepositsAsync(parameters),
            "FIN_JOURNAL_SOURCE_SUMMARY" => await GenerateJournalListingAsync(),
            "FIN_STATEMENT_CHANGES_EQUITY" => await GenerateBalanceSheetAsync(),
            "FIN_CASH_EQUIVALENTS" => await GenerateVaultCashAsync(parameters),
            "FIN_BANK_BALANCES" => await GenerateBankBalancesAsync(parameters),
            "FIN_ACCRUED_INCOME" => await GenerateCashFlowAsync(parameters),
            "FIN_ACCRUED_EXPENSES" => await GenerateIncomeStatementAsync(),
            "FIN_SUSPENSE_BALANCES" => await GenerateBalanceSheetAsync(),
            "FIN_SUSPENDED_VS_ACCRUED_INTEREST" => await GenerateProvisioningAsync(parameters),
            "MGT_BRANCH_PROFITABILITY" => await GenerateBranchProfitabilityAsync(parameters),
            "MGT_PRODUCT_PROFITABILITY" => await GenerateProductProfitabilityAsync(parameters),
            "MGT_PERFORMANCE_TREND" => await GeneratePerformanceTrendAsync(parameters),
            "AUDIT_CONTROL_EXCEPTIONS" => await GenerateAuditExceptionsAsync(parameters),
            _ => new List<Dictionary<string, object?>>()
        };
    }
    private async Task<List<Dictionary<string, object?>>> GenerateCapitalAdequacyAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).ToListAsync();
        var totalExposure = loans.Sum(x => x.OutstandingBalance ?? x.Principal);
        var rwa = totalExposure * 0.85m;
        var tier1 = totalExposure * 0.18m;
        var tier2 = totalExposure * 0.04m;
        var ratio = rwa == 0 ? 0 : (tier1 + tier2) / rwa * 100m;
        return new List<Dictionary<string, object?>>
        {
            new()
            {
                ["asOfDate"] = GetDate(parameters, "toDate", DateTime.UtcNow).ToString("yyyy-MM-dd"),
                ["tier1Capital"] = DecimalRound(tier1),
                ["tier2Capital"] = DecimalRound(tier2),
                ["totalCapital"] = DecimalRound(tier1 + tier2),
                ["riskWeightedAssets"] = DecimalRound(rwa),
                ["capitalAdequacyRatio"] = DecimalRound(ratio),
                ["institutionType"] = "Bank"
            }
        };
    }

    private async Task<List<Dictionary<string, object?>>> GenerateLiquidityAsync(Dictionary<string, string> parameters)
    {
        var accounts = await FilterAccounts(parameters).ToListAsync();
        return accounts.GroupBy(x => x.Currency).Select(group => new Dictionary<string, object?>
        {
            ["asOfDate"] = GetDate(parameters, "toDate", DateTime.UtcNow).ToString("yyyy-MM-dd"),
            ["currency"] = group.Key,
            ["closingBalance"] = DecimalRound(group.Sum(x => x.Balance)),
            ["totalDeposits"] = DecimalRound(group.Where(x => x.Type.Contains("SAV", StringComparison.OrdinalIgnoreCase) || x.Type.Contains("DEP", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Balance)),
            ["totalWithdrawals"] = DecimalRound(group.Sum(x => x.LienAmount)),
            ["liquidityCoverageRatio"] = DecimalRound(group.Sum(x => x.Balance) == 0 ? 0 : 125),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateAssetQualityAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        return loans.Select(loan => new Dictionary<string, object?>
        {
            ["loanId"] = loan.Id,
            ["customerId"] = loan.CustomerId,
            ["customerName"] = loan.Customer?.Name,
            ["status"] = loan.Status,
            ["parBucket"] = loan.ParBucket,
            ["outstandingBalance"] = DecimalRound(loan.OutstandingBalance ?? loan.Principal),
            ["branchId"] = loan.BranchId,
            ["assetClassification"] = InferClassification(loan.ParBucket),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateProvisioningAsync(Dictionary<string, string> parameters)
    {
        var impairments = await _context.LoanImpairments.Include(x => x.Loan).ThenInclude(x => x!.Customer).ToListAsync();
        return impairments.Select(x => new Dictionary<string, object?>
        {
            ["loanId"] = x.LoanId,
            ["customerName"] = x.Loan?.Customer?.Name,
            ["stage"] = x.Stage,
            ["allowanceAmount"] = DecimalRound(x.AllowanceAmount),
                        ["isWrittenOff"] = x.IsWrittenOff,
            ["writtenOffAt"] = x.WrittenOffAt?.ToString("yyyy-MM-dd"),
            ["recoveryAmount"] = DecimalRound(x.RecoveryAmount),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateLargeExposureAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        var totalCapital = Math.Max(1m, loans.Sum(x => x.OutstandingBalance ?? x.Principal) * 0.22m);
        return loans.GroupBy(x => new { x.CustomerId, Name = x.Customer != null ? x.Customer.Name : x.CustomerId })
            .Select(group => new Dictionary<string, object?>
            {
                ["customerId"] = group.Key.CustomerId,
                ["customerName"] = group.Key.Name,
                ["totalExposure"] = DecimalRound(group.Sum(x => x.OutstandingBalance ?? x.Principal)),
                ["percentageOfCapital"] = DecimalRound(group.Sum(x => x.OutstandingBalance ?? x.Principal) / totalCapital * 100m),
                ["loanCount"] = group.Count(),
            })
            .OrderByDescending(x => Convert.ToDecimal(x["totalExposure"], CultureInfo.InvariantCulture))
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateConcentrationAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        return loans.GroupBy(x => new { Dimension = "Sector", Bucket = x.Customer?.Sector ?? "Unclassified" })
            .Select(group => new Dictionary<string, object?>
            {
                ["dimension"] = group.Key.Dimension,
                ["bucket"] = group.Key.Bucket,
                ["loanCount"] = group.Count(),
                ["exposureAmount"] = DecimalRound(group.Sum(x => x.OutstandingBalance ?? x.Principal)),
            })
            .Concat(loans.GroupBy(x => new { Dimension = "Branch", Bucket = x.BranchId }).Select(group => new Dictionary<string, object?>
            {
                ["dimension"] = group.Key.Dimension,
                ["bucket"] = group.Key.Bucket,
                ["loanCount"] = group.Count(),
                ["exposureAmount"] = DecimalRound(group.Sum(x => x.OutstandingBalance ?? x.Principal)),
            }))
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateArrearsAgingAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).ToListAsync();
        return loans.GroupBy(x => string.IsNullOrWhiteSpace(x.ParBucket) ? "0" : x.ParBucket)
            .Select(group => new Dictionary<string, object?>
            {
                ["parBucket"] = group.Key,
                ["loanCount"] = group.Count(),
                ["outstandingBalance"] = DecimalRound(group.Sum(x => x.OutstandingBalance ?? x.Principal)),
                ["amountInArrears"] = DecimalRound(group.Sum(x => (x.OutstandingBalance ?? x.Principal) * (group.Key == "0" ? 0 : 0.15m))),
            })
            .OrderBy(x => x["parBucket"]?.ToString())
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateFeesChargesAsync(Dictionary<string, string> parameters)
    {
        var transactions = await FilterTransactions(parameters).ToListAsync();
        return transactions.GroupBy(x => x.Account?.ProductCode ?? "UNMAPPED")
            .Select(group => new Dictionary<string, object?>
            {
                ["productCode"] = group.Key,
                ["feeAmount"] = DecimalRound(group.Where(x => (x.Narration ?? string.Empty).Contains("fee", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Amount)),
                ["transactionCount"] = group.Count(),
            })
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateNewCustomersAsync(Dictionary<string, string> parameters)
    {
        var query = _context.Customers.AsQueryable();
        query = ApplyDateFilter(query, x => x.CreatedAt, parameters);
        query = ApplyBranchFilter(query, x => x.BranchId, parameters);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return rows.Select(x => new Dictionary<string, object?>
        {
            ["id"] = x.Id,
            ["name"] = x.Name,
            ["type"] = x.Type,
            ["kycLevel"] = x.KycLevel,
            ["branchId"] = x.BranchId,
            ["createdAt"] = x.CreatedAt.ToString("yyyy-MM-dd"),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateDormantCustomersAsync(Dictionary<string, string> parameters)
    {
        var accounts = await FilterAccounts(parameters).Include(x => x.Customer).ToListAsync();
        return accounts.Where(x => !x.LastTransDate.HasValue || x.LastTransDate.Value < DateTime.UtcNow.AddDays(-90)).GroupBy(x => x.CustomerId)
            .Select(group => new Dictionary<string, object?>
            {
                ["customerId"] = group.Key,
                ["customerName"] = group.First().Customer?.Name,
                ["accountCount"] = group.Count(),
                ["lastTransactionDate"] = group.Max(x => x.LastTransDate)?.ToString("yyyy-MM-dd") ?? "N/A",
                ["daysDormant"] = group.Max(x => x.LastTransDate.HasValue ? (DateTime.UtcNow - x.LastTransDate.Value).Days : 999),
            })
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateKycIncompleteAsync(Dictionary<string, string> parameters)
    {
        var query = _context.Customers.AsQueryable();
        query = ApplyBranchFilter(query, x => x.BranchId, parameters);
        var rows = await query.Where(x => string.IsNullOrWhiteSpace(x.GhanaCard) || string.IsNullOrWhiteSpace(x.Phone) || x.KycLevel == "Tier 1").ToListAsync();
        return rows.Select(x => new Dictionary<string, object?>
        {
            ["customerId"] = x.Id,
            ["customerName"] = x.Name,
            ["kycLevel"] = x.KycLevel,
            ["riskRating"] = x.RiskRating,
            ["branchId"] = x.BranchId,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateDepositTransactionsAsync(Dictionary<string, string> parameters)
    {
        var txns = await FilterTransactions(parameters).ToListAsync();
        return txns.Where(x => x.Type.Contains("deposit", StringComparison.OrdinalIgnoreCase) || x.Amount > 0).Select(x => new Dictionary<string, object?>
        {
            ["transactionId"] = x.Id,
            ["date"] = x.Date.ToString("yyyy-MM-dd"),
            ["accountId"] = x.AccountId,
            ["amount"] = DecimalRound(x.Amount),
            ["branchId"] = x.Account?.BranchId,
            ["productCode"] = x.Account?.ProductCode,
            ["processedBy"] = x.TellerId,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateDepositBalancesAsync(Dictionary<string, string> parameters)
    {
        var accounts = await FilterAccounts(parameters).ToListAsync();
        return accounts.GroupBy(x => x.ProductCode ?? x.Type).Select(group => new Dictionary<string, object?>
        {
            ["product"] = group.Key,
            ["accountCount"] = group.Count(),
            ["balance"] = DecimalRound(group.Sum(x => x.Balance)),
            ["currency"] = group.First().Currency,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateBranchDepositPerformanceAsync(Dictionary<string, string> parameters)
    {
        var accounts = await FilterAccounts(parameters).ToListAsync();
        var txns = await FilterTransactions(parameters).ToListAsync();
        return accounts.GroupBy(x => x.BranchId).Select(group => new Dictionary<string, object?>
        {
            ["branchId"] = group.Key,
            ["currentBalance"] = DecimalRound(group.Sum(x => x.Balance)),
            ["accountCount"] = group.Count(),
            ["transactionCount"] = txns.Count(x => x.Account?.BranchId == group.Key),
            ["depositVolume"] = DecimalRound(txns.Where(x => x.Account?.BranchId == group.Key).Sum(x => x.Amount)),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateLoanDisbursementsAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        return loans.Where(x => x.DisbursementDate.HasValue || x.DisbursedAt.HasValue || x.Status == "DISBURSED").Select(x => new Dictionary<string, object?>
        {
            ["loanId"] = x.Id,
            ["customerName"] = x.Customer?.Name,
            ["principal"] = DecimalRound(x.Principal),
            ["productCode"] = x.ProductCode ?? x.LoanProductId,
            ["branchId"] = x.BranchId,
            ["status"] = x.Status,
            ["disbursementDate"] = x.DisbursementDate?.ToString("yyyy-MM-dd") ?? x.DisbursedAt?.ToString("yyyy-MM-dd"),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateLoanRepaymentsAsync(Dictionary<string, string> parameters)
    {
        var repayments = await _context.LoanRepayments.Include(x => x.Loan).ToListAsync();
        return repayments.Select(x => new Dictionary<string, object?>
        {
            ["loanId"] = x.LoanId,
            ["repaymentDate"] = x.RepaymentDate.ToString("yyyy-MM-dd"),
            ["amount"] = DecimalRound(x.Amount),
            ["processedBy"] = x.ProcessedBy,
            ["branchId"] = x.Loan?.BranchId,
            ["status"] = x.IsReversal ? "REVERSED" : "POSTED",
        }).ToList();
    }
    private async Task<List<Dictionary<string, object?>>> GenerateDueInstallmentsAsync(Dictionary<string, string> parameters)
    {
        var schedules = await _context.LoanSchedules.Include(x => x.Loan).ToListAsync();
        var fromDate = DateOnly.FromDateTime(GetDate(parameters, "fromDate", DateTime.UtcNow.Date.AddDays(-7)));
        var toDate = DateOnly.FromDateTime(GetDate(parameters, "toDate", DateTime.UtcNow.Date.AddDays(30)));
        return schedules.Where(x => x.DueDate.HasValue && x.DueDate.Value >= fromDate && x.DueDate.Value <= toDate).Select(x => new Dictionary<string, object?>
        {
            ["loanId"] = x.LoanId,
            ["dueDate"] = x.DueDate?.ToString("yyyy-MM-dd"),
            ["principal"] = DecimalRound(x.Principal ?? 0),
            ["interest"] = DecimalRound(x.Interest ?? 0),
            ["status"] = x.Status,
            ["branchId"] = x.Loan?.BranchId,
        }).ToList();
    }

    private Task<List<Dictionary<string, object?>>> GenerateParAsync(Dictionary<string, string> parameters) => GenerateArrearsAgingAsync(parameters);

    private async Task<List<Dictionary<string, object?>>> GenerateDelinquentLoansAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        return loans.Where(x => !string.Equals(x.ParBucket, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Status, "WRITTEN_OFF", StringComparison.OrdinalIgnoreCase)).Select(x => new Dictionary<string, object?>
        {
            ["loanId"] = x.Id,
            ["customerName"] = x.Customer?.Name,
            ["parBucket"] = x.ParBucket,
            ["outstandingBalance"] = DecimalRound(x.OutstandingBalance ?? x.Principal),
            ["branchId"] = x.BranchId,
            ["status"] = x.Status,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateGroupLendingAsync(Dictionary<string, string> parameters)
    {
        var groups = await _context.Groups.Include(x => x.Members).ToListAsync();
        var groupLoans = await _context.Loans.Where(x => x.GroupId != null).ToListAsync();
        return groups.Select(group => new Dictionary<string, object?>
        {
            ["groupId"] = group.Id,
            ["groupName"] = group.Name,
            ["members"] = group.Members.Count,
            ["portfolioOutstanding"] = DecimalRound(groupLoans.Where(x => x.GroupId == group.Id).Sum(x => x.OutstandingBalance ?? x.Principal)),
            ["parExposure"] = DecimalRound(groupLoans.Where(x => x.GroupId == group.Id && x.ParBucket != "0").Sum(x => x.OutstandingBalance ?? x.Principal)),
            ["branchId"] = group.BranchId,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateBranchTransactionsAsync(Dictionary<string, string> parameters)
    {
        var txns = await FilterTransactions(parameters).ToListAsync();
        return txns.GroupBy(x => x.Account?.BranchId ?? "UNASSIGNED").Select(group => new Dictionary<string, object?>
        {
            ["branchId"] = group.Key,
            ["transactionCount"] = group.Count(),
            ["totalAmount"] = DecimalRound(group.Sum(x => x.Amount)),
            ["reversalCount"] = group.Count(x => (x.Narration ?? string.Empty).Contains("revers", StringComparison.OrdinalIgnoreCase)),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateTellerSummaryAsync(Dictionary<string, string> parameters)
    {
        var txns = await FilterTransactions(parameters).ToListAsync();
        return txns.GroupBy(x => x.TellerId ?? "SYSTEM").Select(group => new Dictionary<string, object?>
        {
            ["processedBy"] = group.Key,
            ["transactionCount"] = group.Count(),
            ["totalAmount"] = DecimalRound(group.Sum(x => x.Amount)),
            ["shortageOrExcess"] = DecimalRound(group.Where(x => (x.Narration ?? string.Empty).Contains("short", StringComparison.OrdinalIgnoreCase) || (x.Narration ?? string.Empty).Contains("excess", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Amount)),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateVaultCashAsync(Dictionary<string, string> parameters)
    {
        var vaults = await _context.BranchVaults.ToListAsync();
        return vaults.Select(x => new Dictionary<string, object?>
        {
            ["branchId"] = x.BranchId,
            ["currency"] = x.Currency,
            ["vaultCash"] = DecimalRound(x.CashOnHand),
            ["tillCash"] = DecimalRound(0),
            ["reconciliationVariance"] = DecimalRound(0),
            ["minBalance"] = DecimalRound(x.MinBalance ?? 0),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateCashIncidentsAsync(Dictionary<string, string> parameters)
    {
        var incidents = _context.CashIncidents.AsQueryable();
        incidents = ApplyDateFilter(incidents, x => x.ReportedAt, parameters);
        incidents = ApplyBranchFilter(incidents, x => x.BranchId, parameters);
        var rows = await incidents.ToListAsync();
        return rows.Select(x => new Dictionary<string, object?>
        {
            ["incidentType"] = x.IncidentType,
            ["branchId"] = x.BranchId,
            ["amount"] = DecimalRound(x.Amount),
            ["status"] = x.Status,
            ["reportedAt"] = x.ReportedAt.ToString("yyyy-MM-dd"),
            ["reference"] = x.Reference,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GeneratePendingApprovalsAsync(Dictionary<string, string> parameters)
    {
        var approvals = _context.ApprovalRequests.Where(x => x.Status == "PENDING");
        approvals = ApplyDateFilter(approvals, x => x.CreatedAt, parameters);
        var rows = await approvals.ToListAsync();
        return rows.Select(x => new Dictionary<string, object?>
        {
            ["referenceType"] = x.EntityType,
            ["referenceNo"] = x.ReferenceNo,
            ["status"] = x.Status,
            ["requestedBy"] = x.RequesterId,
            ["createdAt"] = x.CreatedAt.ToString("yyyy-MM-dd"),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateReversedTransactionsAsync(Dictionary<string, string> parameters)
    {
        var txns = await FilterTransactions(parameters).ToListAsync();
        return txns.Where(x => (x.Narration ?? string.Empty).Contains("revers", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Status, "REVERSED", StringComparison.OrdinalIgnoreCase)).Select(x => new Dictionary<string, object?>
        {
            ["transactionId"] = x.Id,
            ["date"] = x.Date.ToString("yyyy-MM-dd"),
            ["amount"] = DecimalRound(x.Amount),
            ["status"] = x.Status,
            ["description"] = x.Narration,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateUserActivityAsync(Dictionary<string, string> parameters)
    {
        var activities = _context.UserActivities.AsQueryable();
        activities = ApplyDateFilter(activities, x => x.CreatedAt, parameters);
        var rows = await activities.ToListAsync();
        return rows.Select(x => new Dictionary<string, object?>
        {
            ["userId"] = x.StaffId,
            ["action"] = x.Action,
            ["status"] = "Logged",
            ["entityType"] = x.EntityType,
            ["createdAt"] = x.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateInactiveUsersAsync(Dictionary<string, string> parameters)
    {
        var users = await _context.Staff.ToListAsync();
        var sessions = await _context.UserSessions.ToListAsync();
        return users.Select(user =>
        {
            var lastSession = sessions.Where(x => x.StaffId == user.Id).OrderByDescending(x => x.LastActivity).FirstOrDefault();
            return new Dictionary<string, object?>
            {
                ["staffId"] = user.Id,
                ["name"] = user.Name,
                ["email"] = user.Email,
                ["daysSinceLastSession"] = lastSession == null ? 999 : (DateTime.UtcNow - lastSession.LastActivity).Days,
                ["branchId"] = user.BranchId,
            };
        }).OrderByDescending(x => Convert.ToInt32(x["daysSinceLastSession"], CultureInfo.InvariantCulture)).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateCrbInquiriesAsync(bool business)
    {
        var checks = await _context.CreditBureauChecks.Include(x => x.Loan).ThenInclude(x => x!.Customer).ToListAsync();
        return checks.Where(x => x.Loan?.Customer != null && IsBusiness(x.Loan.Customer) == business).Select(x => new Dictionary<string, object?>
        {
            ["checkedAt"] = x.CheckedAt.ToString("yyyy-MM-dd HH:mm"),
            ["customerId"] = x.CustomerId,
            ["customerName"] = x.Loan?.Customer?.Name,
            ["providerName"] = x.ProviderName,
            ["score"] = x.Score,
            ["decision"] = x.Decision,
            ["status"] = x.Status,
            ["loanId"] = x.LoanId,
            ["inquiryReference"] = x.InquiryReference,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateMissingChecksAsync(Dictionary<string, string> parameters, bool business)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        var checkLoanIds = await _context.CreditBureauChecks.Where(x => x.LoanId != null).Select(x => x.LoanId!).ToListAsync();
        return loans.Where(x => x.Customer != null && IsBusiness(x.Customer) == business && !checkLoanIds.Contains(x.Id)).Select(x => new Dictionary<string, object?>
        {
            ["loanId"] = x.Id,
            ["customerId"] = x.CustomerId,
            ["customerName"] = x.Customer?.Name,
            ["productCode"] = x.ProductCode ?? x.LoanProductId,
            ["status"] = x.Status,
            ["applicationDate"] = x.ApplicationDate.ToString("yyyy-MM-dd"),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateProviderPerformanceAsync()
    {
        var checks = await _context.CreditBureauChecks.ToListAsync();
        return checks.GroupBy(x => x.ProviderName ?? "UNKNOWN").Select(group => new Dictionary<string, object?>
        {
            ["providerName"] = group.Key,
            ["totalChecks"] = group.Count(),
            ["failedChecks"] = group.Count(x => !string.Equals(x.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase)),
            ["avgScore"] = DecimalRound((decimal)group.Average(x => x.Score)),
            ["timeouts"] = group.Count(x => (x.Status ?? string.Empty).Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase)),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateApprovalByGradeAsync()
    {
        var checks = await _context.CreditBureauChecks.Include(x => x.Loan).ToListAsync();
        return checks.GroupBy(x => new { RiskGrade = ToRiskBand(x.Score), BureauDecision = x.Decision ?? "UNKNOWN", InternalStatus = x.Loan?.Status ?? "UNKNOWN" }).Select(group => new Dictionary<string, object?>
        {
            ["riskGrade"] = group.Key.RiskGrade,
            ["bureauDecision"] = group.Key.BureauDecision,
            ["internalStatus"] = group.Key.InternalStatus,
            ["count"] = group.Count(),
        }).OrderByDescending(x => Convert.ToInt32(x["count"], CultureInfo.InvariantCulture)).ToList();
    }
    private async Task<List<Dictionary<string, object?>>> GenerateFailedIntegrationsAsync()
    {
        var checks = await _context.CreditBureauChecks.ToListAsync();
        return checks.Where(x => !string.Equals(x.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase)).Select(x => new Dictionary<string, object?>
        {
            ["checkedAt"] = x.CheckedAt.ToString("yyyy-MM-dd HH:mm"),
            ["providerName"] = x.ProviderName,
            ["status"] = x.Status,
            ["retryCount"] = x.RetryCount,
            ["errorMessage"] = x.RawResponse ?? x.Recommendation,
            ["customerId"] = x.CustomerId,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateCrbDataQualityRowsAsync()
    {
        var dashboard = await GetCrbDataQualityAsync();
        return dashboard.Breakdown.Select(metric => new Dictionary<string, object?>
        {
            ["segment"] = metric.Label,
            ["totalRecords"] = metric.Value,
            ["missingMandatoryFields"] = dashboard.MissingMandatoryConsumerFields + dashboard.MissingMandatoryBusinessFields,
            ["readyRecords"] = dashboard.TotalChecks - dashboard.PendingSubmissionReadinessItems,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateCrbRejectionsAsync(bool rejected)
    {
        var returns = await _context.RegulatoryReturns.Where(x => x.ReturnType.Contains("CRB")).ToListAsync();
        return returns.Where(x => rejected ? x.SubmissionStatus == "Rejected" : x.SubmissionStatus != "Submitted" && x.SubmissionStatus != "Accepted").Select(x => new Dictionary<string, object?>
        {
            ["returnType"] = x.ReturnType,
            ["returnDate"] = x.ReturnDate.ToString("yyyy-MM-dd"),
            ["submissionStatus"] = x.SubmissionStatus,
            ["validationErrors"] = x.ValidationErrors,
            ["totalRecords"] = x.TotalRecords,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateConsumerCreditExtractAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        return loans.Where(x => x.Customer != null && !IsBusiness(x.Customer)).Select(BuildConsumerCreditRecord).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateBusinessCreditExtractAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        return loans.Where(x => x.Customer != null && IsBusiness(x.Customer)).Select(BuildBusinessCreditRecord).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateDishonouredChequeExtractAsync(Dictionary<string, string> parameters, bool business)
    {
        var txns = await FilterTransactions(parameters).ToListAsync();
        return txns.Where(x => x.Account?.Customer != null && IsBusiness(x.Account.Customer) == business && (x.Narration ?? string.Empty).Contains("dishon", StringComparison.OrdinalIgnoreCase)).Select(x => new Dictionary<string, object?>
        {
            ["customerId"] = x.Account?.CustomerId,
            ["customerName"] = x.Account?.Customer?.Name,
            ["accountId"] = x.AccountId,
            ["branchCode"] = x.Account?.BranchId,
            ["amount"] = DecimalRound(x.Amount),
            ["date"] = x.Date.ToString("yyyy-MM-dd"),
            ["reference"] = x.Reference,
            ["correctionIndicator"] = "N",
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateJudgmentExtractAsync(Dictionary<string, string> parameters, bool business)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        return loans.Where(x => x.Customer != null && IsBusiness(x.Customer) == business && (x.ParBucket == "90+" || x.Status == "WRITTEN_OFF")).Select(x => new Dictionary<string, object?>
        {
            ["customerId"] = x.CustomerId,
            ["customerName"] = x.Customer?.Name,
            ["loanId"] = x.Id,
            ["branchCode"] = x.BranchId,
            ["outstandingBalance"] = DecimalRound(x.OutstandingBalance ?? x.Principal),
            ["status"] = x.Status,
            ["assetClassification"] = InferClassification(x.ParBucket),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateTrialBalanceDetailAsync()
    {
        var rows = await _context.GlAccounts.OrderBy(x => x.Code).ToListAsync();
        return rows.Select(x => new Dictionary<string, object?>
        {
            ["accountNumber"] = x.Code,
            ["accountName"] = x.Name,
            ["currency"] = x.Currency,
            ["debitBalance"] = x.Category == "ASSET" || x.Category == "EXPENSE" ? DecimalRound(x.Balance) : 0m,
            ["creditBalance"] = x.Category == "LIABILITY" || x.Category == "EQUITY" || x.Category == "INCOME" ? DecimalRound(x.Balance) : 0m,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateTrialBalanceSummaryAsync()
    {
        var accounts = await _context.GlAccounts.ToListAsync();
        return accounts.GroupBy(x => x.Category).Select(group => new Dictionary<string, object?>
        {
            ["accountClass"] = group.Key,
            ["debitBalance"] = DecimalRound(group.Where(x => x.Category == "ASSET" || x.Category == "EXPENSE").Sum(x => x.Balance)),
            ["creditBalance"] = DecimalRound(group.Where(x => x.Category == "LIABILITY" || x.Category == "EQUITY" || x.Category == "INCOME").Sum(x => x.Balance)),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateGlTransactionsAsync()
    {
        var lines = await _context.JournalLines.Include(x => x.Journal).Include(x => x.Account).ToListAsync();
        return lines.Select(x => new Dictionary<string, object?>
        {
            ["journalId"] = x.JournalId,
            ["date"] = x.Journal?.Date?.ToString("yyyy-MM-dd"),
            ["accountCode"] = x.AccountCode,
            ["accountName"] = x.Account?.Name,
            ["debit"] = DecimalRound(x.Debit),
            ["credit"] = DecimalRound(x.Credit),
            ["reference"] = x.Journal?.Reference,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateJournalListingAsync()
    {
        var journals = await _context.JournalEntries.ToListAsync();
        return journals.Select(x => new Dictionary<string, object?>
        {
            ["journalId"] = x.Id,
            ["date"] = x.Date?.ToString("yyyy-MM-dd"),
            ["reference"] = x.Reference,
            ["description"] = x.Description,
            ["status"] = x.Status,
            ["postedBy"] = x.PostedBy,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateBalanceSheetAsync()
    {
        var accounts = await _context.GlAccounts.ToListAsync();
        return accounts.Where(x => x.Category is "ASSET" or "LIABILITY" or "EQUITY").Select(x => new Dictionary<string, object?>
        {
            ["section"] = x.Category,
            ["lineItem"] = x.Name,
            ["amount"] = DecimalRound(x.Balance),
            ["accountCode"] = x.Code,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateIncomeStatementAsync()
    {
        var accounts = await _context.GlAccounts.ToListAsync();
        return accounts.Where(x => x.Category is "INCOME" or "EXPENSE").Select(x => new Dictionary<string, object?>
        {
            ["section"] = x.Category,
            ["lineItem"] = x.Name,
            ["amount"] = DecimalRound(x.Balance),
            ["accountCode"] = x.Code,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateCashFlowAsync(Dictionary<string, string> parameters)
    {
        var txns = await FilterTransactions(parameters).ToListAsync();
        return new List<Dictionary<string, object?>>
        {
            new() { ["category"] = "Operating", ["activity"] = "Customer transactions", ["amount"] = DecimalRound(txns.Sum(x => x.Amount)) },
            new() { ["category"] = "Investing", ["activity"] = "Investments", ["amount"] = DecimalRound(await _context.Investments.SumAsync(x => (decimal?)x.PrincipalAmount) ?? 0m) },
            new() { ["category"] = "Financing", ["activity"] = "Loan funding", ["amount"] = DecimalRound(await _context.Loans.SumAsync(x => (decimal?)x.Principal) ?? 0m) },
        };
    }

    private async Task<List<Dictionary<string, object?>>> GenerateLoansAdvancesAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).Include(x => x.Customer).ToListAsync();
        return loans.Select(x => new Dictionary<string, object?>
        {
            ["loanId"] = x.Id,
            ["customerName"] = x.Customer?.Name,
            ["principal"] = DecimalRound(x.Principal),
            ["outstandingBalance"] = DecimalRound(x.OutstandingBalance ?? x.Principal),
            ["status"] = x.Status,
            ["branchId"] = x.BranchId,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateCustomerDepositsAsync(Dictionary<string, string> parameters)
    {
        var accounts = await FilterAccounts(parameters).Include(x => x.Customer).ToListAsync();
        return accounts.Select(x => new Dictionary<string, object?>
        {
            ["accountId"] = x.Id,
            ["customerName"] = x.Customer?.Name,
            ["balance"] = DecimalRound(x.Balance),
            ["type"] = x.Type,
            ["branchId"] = x.BranchId,
            ["currency"] = x.Currency,
        }).ToList();
    }

    private Task<List<Dictionary<string, object?>>> GenerateImpairmentSupportAsync() => GenerateProvisioningAsync(new Dictionary<string, string>());

    private async Task<List<Dictionary<string, object?>>> GenerateWriteoffMovementAsync()
    {
        var impairments = await _context.LoanImpairments.Include(x => x.Loan).ToListAsync();
        return impairments.Where(x => x.IsWrittenOff || x.RecoveryAmount > 0).Select(x => new Dictionary<string, object?>
        {
            ["loanId"] = x.LoanId,
            ["isWrittenOff"] = x.IsWrittenOff,
            ["recoveryAmount"] = DecimalRound(x.RecoveryAmount),
            ["allowanceAmount"] = DecimalRound(x.AllowanceAmount),
            ["createdAt"] = x.CreatedAt.ToString("yyyy-MM-dd"),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateBranchProfitabilityAsync(Dictionary<string, string> parameters)
    {
        var accounts = await FilterAccounts(parameters).ToListAsync();
        var loans = await FilterLoans(parameters).ToListAsync();
        return loans.GroupBy(x => x.BranchId).Select(group => new Dictionary<string, object?>
        {
            ["branchId"] = group.Key,
            ["loanBalance"] = DecimalRound(group.Sum(x => x.OutstandingBalance ?? x.Principal)),
            ["depositBalance"] = DecimalRound(accounts.Where(x => x.BranchId == group.Key).Sum(x => x.Balance)),
            ["estimatedIncome"] = DecimalRound(group.Sum(x => (x.OutstandingBalance ?? x.Principal) * (x.Rate / 100m))),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateProductProfitabilityAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).ToListAsync();
        return loans.GroupBy(x => x.ProductCode ?? x.LoanProductId ?? "UNMAPPED").Select(group => new Dictionary<string, object?>
        {
            ["productCode"] = group.Key,
            ["loanBalance"] = DecimalRound(group.Sum(x => x.OutstandingBalance ?? x.Principal)),
            ["estimatedIncome"] = DecimalRound(group.Sum(x => (x.OutstandingBalance ?? x.Principal) * (x.Rate / 100m))),
            ["loanCount"] = group.Count(),
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GeneratePerformanceTrendAsync(Dictionary<string, string> parameters)
    {
        var txns = await FilterTransactions(parameters).ToListAsync();
        var loans = await FilterLoans(parameters).ToListAsync();
        return new List<Dictionary<string, object?>>
        {
            new() { ["periodLabel"] = "MTD", ["depositVolume"] = DecimalRound(txns.Where(x => x.Date >= DateTime.UtcNow.AddDays(-30)).Sum(x => x.Amount)), ["loanDisbursement"] = DecimalRound(loans.Where(x => x.ApplicationDate >= DateTime.UtcNow.AddDays(-30)).Sum(x => x.Principal)), ["netFlow"] = DecimalRound(txns.Where(x => x.Date >= DateTime.UtcNow.AddDays(-30)).Sum(x => x.Amount) - loans.Where(x => x.ApplicationDate >= DateTime.UtcNow.AddDays(-30)).Sum(x => x.Principal)) },
            new() { ["periodLabel"] = "QTD", ["depositVolume"] = DecimalRound(txns.Where(x => x.Date >= DateTime.UtcNow.AddDays(-90)).Sum(x => x.Amount)), ["loanDisbursement"] = DecimalRound(loans.Where(x => x.ApplicationDate >= DateTime.UtcNow.AddDays(-90)).Sum(x => x.Principal)), ["netFlow"] = DecimalRound(txns.Where(x => x.Date >= DateTime.UtcNow.AddDays(-90)).Sum(x => x.Amount) - loans.Where(x => x.ApplicationDate >= DateTime.UtcNow.AddDays(-90)).Sum(x => x.Principal)) },
            new() { ["periodLabel"] = "YTD", ["depositVolume"] = DecimalRound(txns.Where(x => x.Date >= new DateTime(DateTime.UtcNow.Year, 1, 1)).Sum(x => x.Amount)), ["loanDisbursement"] = DecimalRound(loans.Where(x => x.ApplicationDate >= new DateTime(DateTime.UtcNow.Year, 1, 1)).Sum(x => x.Principal)), ["netFlow"] = DecimalRound(txns.Where(x => x.Date >= new DateTime(DateTime.UtcNow.Year, 1, 1)).Sum(x => x.Amount) - loans.Where(x => x.ApplicationDate >= new DateTime(DateTime.UtcNow.Year, 1, 1)).Sum(x => x.Principal)) },
        };
    }

    private async Task<List<Dictionary<string, object?>>> GenerateAuditExceptionsAsync(Dictionary<string, string> parameters)
    {
        var approvals = await _context.ApprovalRequests.Where(x => x.Status != "APPROVED").ToListAsync();
        var txns = await FilterTransactions(parameters).ToListAsync();
        var failedChecks = await _context.CreditBureauChecks.Where(x => !string.Equals(x.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase)).ToListAsync();
        var rows = approvals.Select(x => new Dictionary<string, object?>
        {
            ["source"] = "ApprovalRequest",
            ["status"] = x.Status,
            ["description"] = $"Pending approval for {x.EntityType}",
            ["createdAt"] = x.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
        }).ToList();
        rows.AddRange(txns.Where(x => (x.Narration ?? string.Empty).Contains("revers", StringComparison.OrdinalIgnoreCase)).Select(x => new Dictionary<string, object?>
        {
            ["source"] = "Transaction",
            ["status"] = x.Status,
            ["description"] = x.Narration,
            ["createdAt"] = x.Date.ToString("yyyy-MM-dd HH:mm"),
        }));
        rows.AddRange(failedChecks.Select(x => new Dictionary<string, object?>
        {
            ["source"] = "CreditBureau",
            ["status"] = x.Status,
            ["description"] = x.RawResponse ?? x.Recommendation,
            ["createdAt"] = x.CheckedAt.ToString("yyyy-MM-dd HH:mm"),
        }));
        return rows.OrderByDescending(x => x["createdAt"]?.ToString()).ToList();
    }
    private async Task<List<Dictionary<string, object?>>> GenerateCustomerGrowthTrendAsync(Dictionary<string, string> parameters)
    {
        var customers = _context.Customers.AsQueryable();
        customers = ApplyDateFilter(customers, x => x.CreatedAt, parameters);
        customers = ApplyBranchFilter(customers, x => x.BranchId, parameters);
        var rows = await customers.ToListAsync();
        return rows.GroupBy(x => new DateTime(x.CreatedAt.Year, x.CreatedAt.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group => new Dictionary<string, object?>
            {
                ["periodLabel"] = group.Key.ToString("yyyy-MM"),
                ["newCustomers"] = group.Count(),
                ["consumerCustomers"] = group.Count(x => !IsBusiness(x)),
                ["businessCustomers"] = group.Count(IsBusiness),
            })
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateLoanPerformanceByBranchAsync(Dictionary<string, string> parameters)
    {
        var loans = await FilterLoans(parameters).ToListAsync();
        return loans.GroupBy(x => x.BranchId)
            .Select(group => new Dictionary<string, object?>
            {
                ["branchId"] = group.Key,
                ["loanCount"] = group.Count(),
                ["loanExposure"] = DecimalRound(group.Sum(x => x.OutstandingBalance ?? x.Principal)),
                ["parExposure"] = DecimalRound(group.Where(x => !string.Equals(x.ParBucket, "0", StringComparison.OrdinalIgnoreCase)).Sum(x => x.OutstandingBalance ?? x.Principal)),
                ["estimatedIncome"] = DecimalRound(group.Sum(x => (x.OutstandingBalance ?? x.Principal) * (x.Rate / 100m))),
            })
            .OrderByDescending(x => Convert.ToDecimal(x["loanExposure"], CultureInfo.InvariantCulture))
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateCrbApplicationScoresAsync(bool business)
    {
        var checks = await _context.CreditBureauChecks.Include(x => x.Loan).ThenInclude(x => x!.Customer).ToListAsync();
        return checks.Where(x => x.Loan?.Customer != null && IsBusiness(x.Loan.Customer) == business)
            .Select(x => new Dictionary<string, object?>
            {
                ["applicationDate"] = x.Loan?.ApplicationDate.ToString("yyyy-MM-dd") ?? string.Empty,
                ["loanId"] = x.LoanId,
                ["customerId"] = x.CustomerId,
                ["customerName"] = x.Loan?.Customer?.Name,
                ["providerName"] = x.ProviderName,
                ["score"] = x.Score,
                ["riskBand"] = x.RiskBand,
                ["decision"] = x.Decision,
                ["internalStatus"] = x.Loan?.Status,
            })
            .OrderByDescending(x => x["applicationDate"]?.ToString())
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateCrbSubmissionReadinessAsync(Dictionary<string, string> parameters, bool business)
    {
        var rows = business
            ? await GenerateBusinessCreditExtractAsync(parameters)
            : await GenerateConsumerCreditExtractAsync(parameters);

        var requiredFields = business
            ? new[] { "creditFacilityAccountNumber", "customerId", "businessRegistrationNumber", "taxIdentificationNumber", "currentBalance", "facilityStatusCode" }
            : new[] { "creditFacilityAccountNumber", "customerId", "nationalIdNumber", "currentBalance", "facilityStatusCode" };

        return rows.Select(row =>
        {
            var missing = requiredFields.Where(field => !row.ContainsKey(field) || string.IsNullOrWhiteSpace(row[field]?.ToString())).ToList();
            return new Dictionary<string, object?>
            {
                ["creditFacilityAccountNumber"] = row.GetValueOrDefault("creditFacilityAccountNumber"),
                ["customerId"] = row.GetValueOrDefault("customerId"),
                ["readinessStatus"] = missing.Count == 0 ? "READY" : "MISSING_DATA",
                ["missingFields"] = string.Join(", ", missing),
                ["assetClassification"] = row.GetValueOrDefault("assetClassification"),
                ["currentBalance"] = row.GetValueOrDefault("currentBalance"),
            };
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateLedgerStatementAsync()
    {
        var lines = await _context.JournalLines.Include(x => x.Account).ToListAsync();
        return lines.GroupBy(x => new { x.AccountCode, Name = x.Account != null ? x.Account.Name : x.AccountCode })
            .Select(group => new Dictionary<string, object?>
            {
                ["accountCode"] = group.Key.AccountCode,
                ["accountName"] = group.Key.Name,
                ["totalDebit"] = DecimalRound(group.Sum(x => x.Debit)),
                ["totalCredit"] = DecimalRound(group.Sum(x => x.Credit)),
                ["netMovement"] = DecimalRound(group.Sum(x => x.Debit - x.Credit)),
            })
            .OrderBy(x => x["accountCode"]?.ToString())
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> GenerateBankBalancesAsync(Dictionary<string, string> parameters)
    {
        var accounts = await FilterAccounts(parameters).Include(x => x.Customer).ToListAsync();
        return accounts.Where(x => (x.Type ?? string.Empty).Contains("bank", StringComparison.OrdinalIgnoreCase)
                                || (x.Type ?? string.Empty).Contains("current", StringComparison.OrdinalIgnoreCase)
                                || (x.ProductCode ?? string.Empty).Contains("BANK", StringComparison.OrdinalIgnoreCase))
            .Select(x => new Dictionary<string, object?>
            {
                ["accountId"] = x.Id,
                ["customerName"] = x.Customer?.Name,
                ["balance"] = DecimalRound(x.Balance),
                ["currency"] = x.Currency,
                ["branchId"] = x.BranchId,
            })
            .OrderByDescending(x => Convert.ToDecimal(x["balance"], CultureInfo.InvariantCulture))
            .ToList();
    }
    private Dictionary<string, object?> BuildConsumerCreditRecord(Loan loan)
    {
        var customer = loan.Customer!;
        return new Dictionary<string, object?>
        {
            ["correctionIndicator"] = "N",
            ["creditFacilityAccountNumber"] = loan.Id,
            ["customerId"] = customer.Id,
            ["branchCode"] = loan.BranchId,
            ["creditFacilityTypeCode"] = loan.ProductCode ?? loan.LoanProductId,
            ["purposeOfCreditFacility"] = loan.ProductCode ?? "GENERAL",
            ["termOfFacility"] = loan.TermMonths,
            ["amountCurrency"] = "GHS",
            ["originalLoanAmount"] = DecimalRound(loan.Principal),
            ["maturityDate"] = loan.DisbursementDate?.AddMonths(loan.TermMonths).ToString("yyyy-MM-dd") ?? string.Empty,
            ["scheduledInstallmentAmount"] = DecimalRound(loan.Principal / Math.Max(1, loan.TermMonths)),
            ["repaymentFrequency"] = loan.RepaymentFrequency,
            ["lastRepaymentAmount"] = DecimalRound(0),
            ["currentBalance"] = DecimalRound(loan.OutstandingBalance ?? loan.Principal),
            ["assetClassification"] = InferClassification(loan.ParBucket),
            ["amountInArrears"] = DecimalRound((loan.OutstandingBalance ?? loan.Principal) * (loan.ParBucket == "0" ? 0 : 0.15m)),
            ["numberOfDaysInArrears"] = InferDaysInArrears(loan.ParBucket),
            ["facilityStatusCode"] = loan.Status,
            ["facilityStatusDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["collateralizedIndicator"] = string.IsNullOrWhiteSpace(loan.CollateralType) ? "N" : "Y",
            ["typeOfSecurity"] = loan.CollateralType ?? string.Empty,
            ["nationalIdNumber"] = customer.GhanaCard ?? string.Empty,
            ["taxIdentificationNumber"] = customer.Tin ?? string.Empty,
            ["gender"] = customer.Gender ?? string.Empty,
            ["maritalStatus"] = customer.MaritalStatus ?? string.Empty,
            ["nationality"] = customer.Nationality ?? string.Empty,
            ["dateOfBirth"] = customer.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty,
            ["surname"] = customer.Name,
            ["firstName"] = customer.Name,
            ["currentResidentialAddress"] = customer.PostalAddress ?? customer.DigitalAddress ?? string.Empty,
            ["postalAddress"] = customer.PostalAddress ?? string.Empty,
            ["phone"] = customer.Phone ?? string.Empty,
        };
    }

    private Dictionary<string, object?> BuildBusinessCreditRecord(Loan loan)
    {
        var customer = loan.Customer!;
        return new Dictionary<string, object?>
        {
            ["correctionIndicator"] = "N",
            ["creditFacilityAccountNumber"] = loan.Id,
            ["customerId"] = customer.Id,
            ["branchCode"] = loan.BranchId,
            ["creditFacilityTypeCode"] = loan.ProductCode ?? loan.LoanProductId,
            ["purposeOfCreditFacility"] = loan.ProductCode ?? "WORKING_CAPITAL",
            ["termOfFacility"] = loan.TermMonths,
            ["amountCurrency"] = "GHS",
            ["originalLoanAmount"] = DecimalRound(loan.Principal),
            ["currentBalance"] = DecimalRound(loan.OutstandingBalance ?? loan.Principal),
            ["assetClassification"] = InferClassification(loan.ParBucket),
            ["facilityStatusCode"] = loan.Status,
            ["businessRegistrationNumber"] = customer.BusinessRegNo ?? string.Empty,
            ["taxIdentificationNumber"] = customer.Tin ?? string.Empty,
            ["sectorIndustryCode"] = customer.Sector ?? string.Empty,
            ["businessType"] = customer.LegalForm ?? string.Empty,
            ["businessName"] = customer.Name,
            ["tradingName"] = customer.Name,
            ["emailAddress"] = customer.Email ?? string.Empty,
            ["officeTelephoneNumber"] = customer.Phone ?? string.Empty,
            ["currentBusinessLocationAddress"] = customer.PostalAddress ?? customer.DigitalAddress ?? string.Empty,
        };
    }

    private static List<ReportSummaryMetricDTO> BuildSummary(ReportCatalogDefinition definition, List<Dictionary<string, object?>> rows)
    {
        var summary = new List<ReportSummaryMetricDTO> { Metric("Rows", rows.Count), Metric("Category", definition.Category) };
        var numericColumns = rows.SelectMany(x => x).Where(x => x.Value is byte or short or int or long or float or double or decimal).GroupBy(x => x.Key).Take(3);
        foreach (var column in numericColumns)
        {
            var total = column.Sum(x => Convert.ToDecimal(x.Value, CultureInfo.InvariantCulture));
            summary.Add(Metric($"Total {column.Key}", total.ToString("N2", CultureInfo.InvariantCulture)));
        }
        return summary;
    }

    private static List<string> BuildValidationMessages(ReportCatalogDefinition definition, List<Dictionary<string, object?>> rows)
    {
        var messages = new List<string>();
        if (rows.Count == 0) messages.Add("No records matched the selected filters.");
        if (definition.Category == "Credit Bureau Reports")
        {
            var missingIds = rows.Count(x => (x.ContainsKey("nationalIdNumber") && string.IsNullOrWhiteSpace(x["nationalIdNumber"]?.ToString())) || (x.ContainsKey("businessRegistrationNumber") && string.IsNullOrWhiteSpace(x["businessRegistrationNumber"]?.ToString())));
            if (missingIds > 0) messages.Add($"{missingIds} records are missing core borrower identifiers.");
        }
        if (definition.RequiresApprovalBeforeFinalExport) messages.Add("Final export for this report is configured for maker-checker control.");
        return messages;
    }

    private List<Dictionary<string, object?>> ApplyMaskingIfRequired(ReportCatalogDefinition definition, List<Dictionary<string, object?>> rows, out bool isMasked)
    {
        isMasked = definition.Category == "Credit Bureau Reports" && !_currentUser.HasPermission(AppPermissions.Reports.Regulatory);
        if (!isMasked) return rows;
        return rows.Select(row => row.ToDictionary(kvp => kvp.Key, kvp => ShouldMaskField(kvp.Key) ? MaskValue(kvp.Value?.ToString()) : kvp.Value)).ToList();
    }

    private async Task PersistArtifactsAsync(ReportCatalogDefinition definition, List<Dictionary<string, object?>> rows, List<string> validationMessages, string format)
    {
        if (!definition.IsRegulatory) return;
        _context.RegulatoryReturns.Add(new RegulatoryReturn
        {
            ReturnType = definition.ReportCode,
            ReturnDate = UtcToday(),
            ReportingPeriodStart = GetPeriodStart(),
            ReportingPeriodEnd = UtcToday(),
            SubmissionStatus = definition.RequiresApprovalBeforeFinalExport ? "PendingApproval" : "Approved",
            BogReferenceNumber = string.Empty,
            SubmittedBy = CurrentStaffId,
            TotalRecords = rows.Count,
            FilePath = $"{definition.ReportCode}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format.ToLowerInvariant()}",
            FileFormat = format.ToUpperInvariant(),
            ValidationErrors = JsonSerializer.Serialize(validationMessages, JsonOptions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        if (definition.Category == "Credit Bureau Reports")
        {
            _context.DataExtracts.Add(new DataExtract
            {
                ExtractName = definition.ReportName,
                ExtractType = "CRB",
                ExtractDate = UtcToday(),
                RecordCount = rows.Count,
                FilePath = $"{definition.ReportCode}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format.ToLowerInvariant()}",
                FileFormat = format.ToUpperInvariant(),
                CreatedBy = CurrentStaffId,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private async Task CreateAuditLogAsync(string action, string entityId, string description, bool success, object? payload, string? failureReason = null)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = "Report",
            EntityId = entityId,
            UserId = CurrentStaffId,
            CreatedBy = CurrentStaffId,
            Description = description,
            PayloadJson = payload == null ? null : JsonSerializer.Serialize(payload, JsonOptions),
            IsSuccess = success,
            Status = success ? "SUCCESS" : "FAILED",
            FailureReason = failureReason,
            ErrorMessage = failureReason,
            CreatedAt = DateTime.UtcNow,
        });
        await Task.CompletedTask;
    }

    private IQueryable<Loan> FilterLoans(Dictionary<string, string> parameters)
    {
        var query = _context.Loans.AsQueryable();
        query = ApplyDateFilter(query, x => x.ApplicationDate, parameters);
        query = ApplyBranchFilter(query, x => x.BranchId, parameters);
        if (parameters.TryGetValue("productCode", out var productCode) && !string.IsNullOrWhiteSpace(productCode)) query = query.Where(x => x.ProductCode == productCode || x.LoanProductId == productCode);
        return query;
    }

    private IQueryable<Account> FilterAccounts(Dictionary<string, string> parameters)
    {
        var query = _context.Accounts.Include(x => x.Customer).AsQueryable();
        query = ApplyBranchFilter(query, x => x.BranchId!, parameters);
        if (parameters.TryGetValue("currency", out var currency) && !string.IsNullOrWhiteSpace(currency)) query = query.Where(x => x.Currency == currency);
        return query;
    }

    private IQueryable<Transaction> FilterTransactions(Dictionary<string, string> parameters)
    {
        var query = _context.Transactions.Include(x => x.Account).ThenInclude(x => x!.Customer).AsQueryable();
        query = ApplyDateFilter(query, x => x.Date, parameters);
        if (parameters.TryGetValue("branchId", out var branchId) && !string.IsNullOrWhiteSpace(branchId)) query = query.Where(x => x.Account != null && x.Account.BranchId == branchId);
        return query;
    }

    private static IQueryable<T> ApplyDateFilter<T>(IQueryable<T> query, System.Linq.Expressions.Expression<Func<T, DateTime>> selector, Dictionary<string, string> parameters)
    {
        var fromDate = GetOptionalDate(parameters, "fromDate");
        var toDate = GetOptionalDate(parameters, "toDate");
        if (fromDate.HasValue) query = query.Where(BuildDatePredicate(selector, fromDate.Value, true));
        if (toDate.HasValue) query = query.Where(BuildDatePredicate(selector, toDate.Value.AddDays(1).AddTicks(-1), false));
        return query;
    }

    private static IQueryable<T> ApplyBranchFilter<T>(IQueryable<T> query, System.Linq.Expressions.Expression<Func<T, string>> selector, Dictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("branchId", out var branchId) && !string.IsNullOrWhiteSpace(branchId))
        {
            var param = selector.Parameters[0];
            var body = System.Linq.Expressions.Expression.Equal(selector.Body, System.Linq.Expressions.Expression.Constant(branchId));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, param);
            query = query.Where(lambda);
        }
        return query;
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildDatePredicate<T>(System.Linq.Expressions.Expression<Func<T, DateTime>> selector, DateTime boundary, bool isFrom)
    {
        var comparison = isFrom ? System.Linq.Expressions.Expression.GreaterThanOrEqual(selector.Body, System.Linq.Expressions.Expression.Constant(boundary)) : System.Linq.Expressions.Expression.LessThanOrEqual(selector.Body, System.Linq.Expressions.Expression.Constant(boundary));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(comparison, selector.Parameters[0]);
    }

    private static List<Dictionary<string, object?>> SortRows(List<Dictionary<string, object?>> rows, string? sortBy, string? direction)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return rows;
        var column = sortBy.Contains(':') ? sortBy.Split(':')[0] : sortBy;
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase) || sortBy.EndsWith(":desc", StringComparison.OrdinalIgnoreCase);
        return descending ? rows.OrderByDescending(x => x.TryGetValue(column, out var value) ? value : null).ToList() : rows.OrderBy(x => x.TryGetValue(column, out var value) ? value : null).ToList();
    }

    private static bool IsBusiness(Customer customer)
    {
        var type = customer.Type?.ToUpperInvariant() ?? string.Empty;
        return type.Contains("BUS") || type.Contains("CORP") || type.Contains("COMPANY");
    }

    private static string InferClassification(string? parBucket) => parBucket switch { "0" => "Current", "1-30" => "OLEM", "31-60" => "Substandard", "61-90" => "Doubtful", _ => "Loss" };
    private static int InferDaysInArrears(string? parBucket) => parBucket switch { "0" => 0, "1-30" => 15, "31-60" => 45, "61-90" => 75, _ => 120 };
    private static string ToRiskBand(decimal? score) { if (!score.HasValue) return "UNKNOWN"; if (score >= 700) return "LOW"; if (score >= 550) return "MEDIUM"; return "HIGH"; }
    private static decimal DecimalRound(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static DateTime GetDate(Dictionary<string, string> parameters, string key, DateTime fallback) => GetOptionalDate(parameters, key) ?? fallback;
    private static DateTime? GetOptionalDate(Dictionary<string, string> parameters, string key) => parameters.TryGetValue(key, out var raw) && DateTime.TryParse(raw, out var parsed) ? parsed.Date : null;
    private static ReportSummaryMetricDTO Metric(string label, object value) => new() { Label = label, Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty };
    private static bool ShouldMaskField(string key) => key.Contains("id", StringComparison.OrdinalIgnoreCase) || key.Contains("number", StringComparison.OrdinalIgnoreCase) || key.Contains("phone", StringComparison.OrdinalIgnoreCase) || key.Contains("address", StringComparison.OrdinalIgnoreCase);
    private static string MaskValue(string? value) { if (string.IsNullOrWhiteSpace(value) || value.Length <= 4) return "****"; return new string('*', value.Length - 4) + value[^4..]; }
    private static Dictionary<string, string> DeserializeParameters(string json) { try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>(); } catch { return new Dictionary<string, string>(); } }
    private static DateTime GetPeriodStart() { var now = DateTime.UtcNow; return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc); }
    private static DateTime UtcToday() { var now = DateTime.UtcNow; return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc); }
    private async Task<string> GetInstitutionNameAsync()
    {
        var configured = await _context.SystemConfigs
            .Where(x => x.Key == "institution_name" || x.Key == "business_name")
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(configured) ? "BankInsight" : configured;
    }
    private string CurrentStaffId => string.IsNullOrWhiteSpace(_currentUser.UserId) ? "system" : _currentUser.UserId;
    private ReportCatalogDefinition GetDefinition(string reportCode) => _catalogRegistry.GetByCode(reportCode) ?? throw new KeyNotFoundException($"Report {reportCode} was not found.");

    private static ReportCatalogEntryDTO MapCatalog(ReportCatalogDefinition definition, bool isFavorite) => new()
    {
        ReportCode = definition.ReportCode,
        ReportName = definition.ReportName,
        Category = definition.Category,
        SubCategory = definition.SubCategory,
        Description = definition.Description,
        ApplicableInstitutionTypes = definition.ApplicableInstitutionTypes.ToList(),
        RequiredPermissions = definition.RequiredPermissions.ToList(),
        DataSource = definition.DataSource,
        ParameterSchema = definition.ParameterSchema.ToList(),
        DefaultSort = definition.DefaultSort,
        DefaultColumns = definition.DefaultColumns.ToList(),
        ExportFormats = definition.ExportFormats.ToList(),
        IsRegulatory = definition.IsRegulatory,
        RequiresApprovalBeforeFinalExport = definition.RequiresApprovalBeforeFinalExport,
        EffectiveFrom = definition.EffectiveFrom,
        EffectiveTo = definition.EffectiveTo,
        Version = definition.Version,
        IsActive = definition.IsActive,
        IsFavorite = isFavorite,
        SupportsBranchScope = definition.SupportsBranchScope,
        SupportsHeadOfficeScope = definition.SupportsHeadOfficeScope,
    };
}




















