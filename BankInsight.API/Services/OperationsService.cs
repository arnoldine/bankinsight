using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class OperationsService
{
    private const string BusinessDateKey = "business_date";
    private const string SchedulerEnabledKey = "eod_scheduler_enabled";
    private const string SchedulerTimeKey = "eod_scheduler_time_utc";
    private const string SchedulerLastRunKey = "eod_scheduler_last_run_date";

    private static readonly string[] ManualOnlySteps = ["BACKUP_DB"];
    private static readonly string[] ScheduledSteps = ["PRE_VALIDATION", "SAVINGS_ACCRUAL", "LOAN_AGING", "GL_POSTING", "CLOSE_DATE"];

    private readonly ApplicationDbContext _context;
    private readonly IDepositEngine _depositEngine;
    private readonly LoanService _loanService;
    private readonly IAuditLoggingService _auditLoggingService;

    public OperationsService(ApplicationDbContext context, IDepositEngine depositEngine, LoanService loanService, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _depositEngine = depositEngine;
        _loanService = loanService;
        _auditLoggingService = auditLoggingService;
    }
    public async Task<EodStatusDto> GetEodStatusAsync()
    {
        var businessDate = await GetBusinessDateAsync();
        var schedulerEnabled = await GetBoolConfigAsync(SchedulerEnabledKey, false);
        var schedulerTimeUtc = await GetStringConfigAsync(SchedulerTimeKey, "23:00") ?? "23:00";
        var lastSchedulerRunDate = await GetStringConfigAsync(SchedulerLastRunKey, null);

        return new EodStatusDto
        {
            BusinessDate = businessDate.ToString("yyyy-MM-dd"),
            Mode = "LIVE",
            Status = "OPEN",
            PendingTransactions = await _context.Transactions.CountAsync(t => t.Status == "PENDING"),
            DraftJournalEntries = await _context.JournalEntries.CountAsync(j => j.Status != "POSTED"),
            ActiveLoans = await _context.Loans.CountAsync(l => l.Status != null && l.Status.ToUpper() == "ACTIVE"),
            ManualSteps = ManualOnlySteps.ToList(),
            SchedulerEnabled = schedulerEnabled,
            SchedulerTimeUtc = schedulerTimeUtc,
            LastSchedulerRunDate = lastSchedulerRunDate,
            NextScheduledRunUtc = CalculateNextScheduledRunUtc(businessDate, schedulerTimeUtc, schedulerEnabled, lastSchedulerRunDate)
        };
    }

    public async Task<EodStepResultDto> RunStepAsync(RunEodStepRequest request, string? userId)
    {
        var businessDate = request.BusinessDate ?? await GetBusinessDateAsync();
        var stepId = request.StepId.Trim().ToUpperInvariant();

        return stepId switch
        {
            "PRE_VALIDATION" => await RunPreValidationAsync(businessDate),
            "SAVINGS_ACCRUAL" => await RunSavingsAccrualAsync(businessDate),
            "LOAN_AGING" => await RunLoanAgingAsync(businessDate, userId),
            "GL_POSTING" => await RunGlPostingCheckAsync(businessDate),
            "BACKUP_DB" => CreateManualStepResult(stepId, businessDate, "Database snapshot remains a manual infrastructure operation in this environment."),
            "CLOSE_DATE" => await RunCloseDateAsync(businessDate, userId),
            _ => throw new InvalidOperationException($"Unsupported EOD step '{request.StepId}'.")
        };
    }

    public async Task ExecuteScheduledBatchAsync()
    {
        var status = await GetEodStatusAsync();
        if (!status.SchedulerEnabled)
        {
            return;
        }

        var businessDate = DateOnly.ParseExact(status.BusinessDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var now = DateTime.UtcNow;
        var scheduledTime = ParseSchedulerTime(status.SchedulerTimeUtc);
        var lastRun = ParseOptionalDate(status.LastSchedulerRunDate);

        if (lastRun == businessDate || now.TimeOfDay < scheduledTime.ToTimeSpan())
        {
            return;
        }

        foreach (var stepId in ScheduledSteps)
        {
            var result = await RunStepAsync(new RunEodStepRequest { StepId = stepId, BusinessDate = businessDate }, null);
            if (result.Status == "ERROR")
            {
                await _auditLoggingService.LogActionAsync(
                    "EOD_SCHEDULER_FAILED",
                    "SYSTEM",
                    stepId,
                    null,
                    result.Message,
                    status: "FAILED",
                    errorMessage: result.Message,
                    newValues: new { stepId, businessDate = businessDate.ToString("yyyy-MM-dd") });
                return;
            }
        }

        await UpsertConfigAsync(SchedulerLastRunKey, businessDate.ToString("yyyy-MM-dd"), "Last business date processed by the EOD scheduler");
        await _auditLoggingService.LogActionAsync(
            "EOD_SCHEDULER_COMPLETED",
            "SYSTEM",
            businessDate.ToString("yyyy-MM-dd"),
            null,
            $"Scheduled EOD batch completed for {businessDate:yyyy-MM-dd}",
            status: "SUCCESS",
            newValues: new { businessDate = businessDate.ToString("yyyy-MM-dd"), scheduledAtUtc = now });
    }

    private async Task<EodStepResultDto> RunPreValidationAsync(DateOnly businessDate)
    {
        var pendingTransactions = await _context.Transactions.CountAsync(t => t.Status == "PENDING");
        var draftJournals = await _context.JournalEntries.CountAsync(j => j.Status != "POSTED");

        if (pendingTransactions > 0 || draftJournals > 0)
        {
            return new EodStepResultDto
            {
                StepId = "PRE_VALIDATION",
                Status = "WARNING",
                BusinessDate = businessDate.ToString("yyyy-MM-dd"),
                Message = "Pre-close validation found unresolved work items.",
                Details = new { pendingTransactions, draftJournals }
            };
        }

        return new EodStepResultDto
        {
            StepId = "PRE_VALIDATION",
            Status = "SUCCESS",
            BusinessDate = businessDate.ToString("yyyy-MM-dd"),
            Message = "Pre-close validation passed.",
            Details = new { pendingTransactions, draftJournals }
        };
    }

    private async Task<EodStepResultDto> RunSavingsAccrualAsync(DateOnly businessDate)
    {
        var eligibleAccounts = await _context.Accounts.CountAsync(account => account.Balance > 0 && account.Status == "ACTIVE");
        await _depositEngine.RunDailyAccrualJobAsync();

        return new EodStepResultDto
        {
            StepId = "SAVINGS_ACCRUAL",
            Status = "SUCCESS",
            BusinessDate = businessDate.ToString("yyyy-MM-dd"),
            Message = $"Savings accrual batch processed for {eligibleAccounts} active deposit accounts.",
            Details = new { eligibleAccounts }
        };
    }

    private async Task<EodStepResultDto> RunLoanAgingAsync(DateOnly businessDate, string? userId)
    {
        var result = await _loanService.ProcessAccrualBatchAsync(new LoanAccrualBatchRequest
        {
            AsOfDate = businessDate
        }, userId);

        return new EodStepResultDto
        {
            StepId = "LOAN_AGING",
            Status = "SUCCESS",
            BusinessDate = businessDate.ToString("yyyy-MM-dd"),
            Message = $"Loan accrual batch processed for {result.LoansProcessed} loans.",
            Details = new
            {
                result.LoansProcessed,
                result.TotalInterestAccrued,
                result.TotalPenaltyAccrued,
                result.JournalIds
            }
        };
    }

    private async Task<EodStepResultDto> RunGlPostingCheckAsync(DateOnly businessDate)
    {
        var draftJournals = await _context.JournalEntries.CountAsync(j => j.Status != "POSTED");
        if (draftJournals > 0)
        {
            return new EodStepResultDto
            {
                StepId = "GL_POSTING",
                Status = "WARNING",
                BusinessDate = businessDate.ToString("yyyy-MM-dd"),
                Message = $"{draftJournals} draft journal entries still require posting or approval.",
                Details = new { draftJournals }
            };
        }

        return new EodStepResultDto
        {
            StepId = "GL_POSTING",
            Status = "SUCCESS",
            BusinessDate = businessDate.ToString("yyyy-MM-dd"),
            Message = "No draft journal entries are blocking close.",
            Details = new { draftJournals }
        };
    }

    private async Task<EodStepResultDto> RunCloseDateAsync(DateOnly businessDate, string? userId)
    {
        var validation = await RunPreValidationAsync(businessDate);
        if (validation.Status == "WARNING")
        {
            return new EodStepResultDto
            {
                StepId = "CLOSE_DATE",
                Status = "WARNING",
                BusinessDate = businessDate.ToString("yyyy-MM-dd"),
                Message = "Business date rollover blocked until pre-close validation issues are resolved.",
                Details = validation.Details
            };
        }

        var nextBusinessDate = businessDate.AddDays(1);
        var businessDateConfig = await _context.SystemConfigs.FirstOrDefaultAsync(config => config.Key == BusinessDateKey)
            ?? throw new InvalidOperationException("business_date system configuration is missing.");

        var previousValue = businessDateConfig.Value;
        businessDateConfig.Value = nextBusinessDate.ToString("yyyy-MM-dd");
        businessDateConfig.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "EOD_CLOSE_DATE",
            "SYSTEM",
            BusinessDateKey,
            userId,
            $"Business date rolled from {businessDate:yyyy-MM-dd} to {nextBusinessDate:yyyy-MM-dd}",
            status: "SUCCESS",
            oldValues: new { businessDate = previousValue },
            newValues: new { businessDate = businessDateConfig.Value });

        return new EodStepResultDto
        {
            StepId = "CLOSE_DATE",
            Status = "SUCCESS",
            BusinessDate = nextBusinessDate.ToString("yyyy-MM-dd"),
            Message = $"Business date advanced to {nextBusinessDate:yyyy-MM-dd}.",
            Details = new { previousBusinessDate = businessDate.ToString("yyyy-MM-dd"), nextBusinessDate = nextBusinessDate.ToString("yyyy-MM-dd") }
        };
    }

    private static EodStepResultDto CreateManualStepResult(string stepId, DateOnly businessDate, string message)
    {
        return new EodStepResultDto
        {
            StepId = stepId,
            Status = "WARNING",
            BusinessDate = businessDate.ToString("yyyy-MM-dd"),
            Message = message
        };
    }

    private async Task<DateOnly> GetBusinessDateAsync()
    {
        var value = await GetStringConfigAsync(BusinessDateKey, null);
        if (!string.IsNullOrWhiteSpace(value) && DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var businessDate))
        {
            return businessDate;
        }

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private async Task<bool> GetBoolConfigAsync(string key, bool defaultValue)
    {
        var value = await GetStringConfigAsync(key, null);
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private async Task<string?> GetStringConfigAsync(string key, string? defaultValue)
    {
        var value = await _context.SystemConfigs
            .Where(config => config.Key == key)
            .Select(config => config.Value)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private async Task UpsertConfigAsync(string key, string value, string description)
    {
        var existing = await _context.SystemConfigs.FirstOrDefaultAsync(config => config.Key == key);
        if (existing == null)
        {
            _context.SystemConfigs.Add(new Entities.SystemConfig
            {
                Id = $"CFG_{key.ToUpperInvariant()}",
                Key = key,
                Value = value,
                Description = description,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Value = value;
            existing.Description ??= description;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private static TimeOnly ParseSchedulerTime(string? configuredTime)
    {
        return TimeOnly.TryParseExact(configuredTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : new TimeOnly(23, 0);
    }

    private static DateOnly? ParseOptionalDate(string? value)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    private static string? CalculateNextScheduledRunUtc(DateOnly businessDate, string schedulerTimeUtc, bool schedulerEnabled, string? lastSchedulerRunDate)
    {
        if (!schedulerEnabled)
        {
            return null;
        }

        var scheduledTime = ParseSchedulerTime(schedulerTimeUtc);
        var lastRun = ParseOptionalDate(lastSchedulerRunDate);
        var targetBusinessDate = lastRun == businessDate ? businessDate.AddDays(1) : businessDate;
        var nextRun = targetBusinessDate.ToDateTime(scheduledTime, DateTimeKind.Utc);
        return nextRun.ToString("O");
    }
}
