using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class ReconciliationHubService
{
    private readonly ApplicationDbContext _context;

    public ReconciliationHubService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReconciliationHubSummaryDto> GetSummaryAsync()
    {
        await SynchronizeExceptionsAsync();

        var items = await _context.ReconciliationExceptions
            .AsNoTracking()
            .OrderByDescending(item => item.DetectedAt)
            .ToListAsync();

        var instructions = await _context.SettlementInstructions
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        var openCount = items.Count(item => item.Status == "OPEN");
        var overdueCount = items.Count(item => item.DueAt != null && item.DueAt < DateTime.UtcNow && item.Status != "RESOLVED");
        var highSeverity = items.Count(item => item.Severity == "HIGH");
        var resolvedToday = items.Count(item => item.ResolvedAt != null && item.ResolvedAt.Value.Date == DateTime.UtcNow.Date);

        return new ReconciliationHubSummaryDto
        {
            Metrics = new List<ReconciliationMetricDto>
            {
                new() { Key = "open", Label = "Open Exceptions", Value = openCount.ToString(), Severity = openCount > 0 ? "MEDIUM" : "INFO" },
                new() { Key = "overdue", Label = "Overdue Breaks", Value = overdueCount.ToString(), Severity = overdueCount > 0 ? "HIGH" : "INFO" },
                new() { Key = "high", Label = "High Severity", Value = highSeverity.ToString(), Severity = highSeverity > 0 ? "HIGH" : "INFO" },
                new() { Key = "resolved", Label = "Resolved Today", Value = resolvedToday.ToString(), Severity = "INFO" },
                new() { Key = "settlements", Label = "Pending Settlements", Value = instructions.Count(item => item.Status != "COMPLETED").ToString(), Severity = instructions.Any(item => item.Status == "FAILED") ? "HIGH" : "INFO" }
            },
            Exceptions = items.Select(Map).ToList(),
            SettlementInstructions = instructions.Select(MapInstruction).ToList()
        };
    }

    public async Task<ReconciliationExceptionDto?> UpdateExceptionAsync(string id, UpdateReconciliationExceptionRequest request)
    {
        var item = await _context.ReconciliationExceptions.FirstOrDefaultAsync(entry => entry.Id == id);
        if (item == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            item.Status = request.Status.Trim().ToUpperInvariant();
            if (item.Status == "RESOLVED")
            {
                item.ResolvedAt = DateTime.UtcNow;
            }
        }

        item.OwnerUserId = string.IsNullOrWhiteSpace(request.OwnerUserId) ? item.OwnerUserId : request.OwnerUserId.Trim();
        item.Detail = string.IsNullOrWhiteSpace(request.Detail) ? item.Detail : request.Detail.Trim();
        item.WorkflowStage = string.IsNullOrWhiteSpace(request.WorkflowStage) ? item.WorkflowStage : request.WorkflowStage.Trim().ToUpperInvariant();
        item.ResolutionCode = string.IsNullOrWhiteSpace(request.ResolutionCode) ? item.ResolutionCode : request.ResolutionCode.Trim().ToUpperInvariant();
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Map(item);
    }

    public async Task<ReconciliationExceptionDto?> RetryExceptionAsync(string id, RetryReconciliationExceptionRequest request)
    {
        var item = await _context.ReconciliationExceptions.FirstOrDefaultAsync(entry => entry.Id == id);
        if (item == null)
        {
            return null;
        }

        item.RetryCount += 1;
        item.LastAttemptAt = DateTime.UtcNow;
        item.Status = "RETRYING";
        item.WorkflowStage = "RETRY";
        item.Detail = string.IsNullOrWhiteSpace(request.Detail) ? $"{item.Detail} Retry initiated." : request.Detail.Trim();
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Map(item);
    }

    public async Task<SettlementInstructionDto> CreateSettlementInstructionAsync(CreateSettlementInstructionRequest request)
    {
        var instruction = new SettlementInstruction
        {
            Id = $"SETTLE-{Guid.NewGuid():N}"[..20],
            ReconciliationExceptionId = request.ReconciliationExceptionId,
            InstructionType = request.InstructionType.Trim().ToUpperInvariant(),
            Status = "PENDING",
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "GHS" : request.Currency.Trim().ToUpperInvariant(),
            Amount = request.Amount,
            SettlementAccount = request.SettlementAccount?.Trim(),
            Counterparty = request.Counterparty?.Trim(),
            DueAt = request.DueAt ?? DateTime.UtcNow.Date.AddDays(1),
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SettlementInstructions.Add(instruction);

        var exception = await _context.ReconciliationExceptions.FirstOrDefaultAsync(item => item.Id == request.ReconciliationExceptionId);
        if (exception != null)
        {
            exception.WorkflowStage = "SETTLEMENT_PENDING";
            exception.Status = "IN_PROGRESS";
            exception.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return MapInstruction(instruction);
    }

    private async Task SynchronizeExceptionsAsync()
    {
        var unreconciledTreasury = await _context.TreasuryPositions
            .AsNoTracking()
            .Where(position => position.PositionStatus != "Reconciled" && position.PositionStatus != "Closed")
            .ToListAsync();

        foreach (var position in unreconciledTreasury)
        {
            await UpsertAsync(
                $"RECON-TREASURY-{position.Id}",
                "TREASURY",
                "TREASURY",
                $"TREASURY-{position.Id}",
                position.Currency,
                position.ClosingBalance,
                "MEDIUM",
                $"Treasury position {position.Currency} is not reconciled",
                $"Position dated {position.PositionDate:yyyy-MM-dd} remains in status {position.PositionStatus}.",
                DateTime.UtcNow.Date.AddDays(1));
        }

        var openTransfers = await _context.InterBranchTransfers
            .AsNoTracking()
            .Where(transfer => transfer.Status != "Completed" && transfer.Status != "Rejected")
            .ToListAsync();

        foreach (var transfer in openTransfers)
        {
            await UpsertAsync(
                $"RECON-TRANSFER-{transfer.Id}",
                "INTER_BRANCH",
                "BRANCH",
                transfer.Reference ?? transfer.Id,
                transfer.Currency,
                transfer.Amount,
                transfer.Status == "Pending" ? "HIGH" : "MEDIUM",
                $"Inter-branch transfer {transfer.Id} remains open",
                $"Transfer from {transfer.FromBranchId} to {transfer.ToBranchId} is in status {transfer.Status}.",
                DateTime.UtcNow.Date.AddDays(1));
        }

        var openCashIncidents = await _context.CashIncidents
            .AsNoTracking()
            .Where(incident => incident.Status == "OPEN")
            .ToListAsync();

        foreach (var incident in openCashIncidents)
        {
            await UpsertAsync(
                $"RECON-CASH-{incident.Id}",
                "CASH_CONTROL",
                "VAULT",
                incident.Reference ?? incident.Id,
                incident.Currency,
                incident.Amount,
                "HIGH",
                $"Cash incident {incident.IncidentType} remains unresolved",
                incident.Narration ?? $"Cash incident in {incident.StoreType} {incident.StoreId} is still open.",
                DateTime.UtcNow.Date);
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpsertAsync(string id, string category, string sourceSystem, string reference, string currency, decimal amount, string severity, string summary, string detail, DateTime dueAt)
    {
        var existing = await _context.ReconciliationExceptions.FirstOrDefaultAsync(item => item.Id == id);
        if (existing == null)
        {
            _context.ReconciliationExceptions.Add(new ReconciliationException
            {
                Id = id,
                Category = category,
                SourceSystem = sourceSystem,
                Reference = reference,
                Currency = currency,
                Amount = amount,
                Severity = severity,
                Summary = summary,
                Detail = detail,
                DueAt = dueAt,
                WorkflowStage = "OPEN",
                DetectedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            return;
        }

        existing.Category = category;
        existing.SourceSystem = sourceSystem;
        existing.Reference = reference;
        existing.Currency = currency;
        existing.Amount = amount;
        existing.Severity = severity;
        existing.Summary = summary;
        existing.Detail = detail;
        existing.DueAt = dueAt;
        existing.WorkflowStage ??= "OPEN";
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private static ReconciliationExceptionDto Map(ReconciliationException item)
    {
        return new ReconciliationExceptionDto
        {
            Id = item.Id,
            Category = item.Category,
            SourceSystem = item.SourceSystem,
            Reference = item.Reference,
            Status = item.Status,
            Severity = item.Severity,
            Currency = item.Currency,
            Amount = item.Amount,
            OwnerUserId = item.OwnerUserId,
            Summary = item.Summary,
            Detail = item.Detail,
            DetectedAt = item.DetectedAt,
            DueAt = item.DueAt,
            ResolvedAt = item.ResolvedAt,
            RetryCount = item.RetryCount,
            LastAttemptAt = item.LastAttemptAt,
            WorkflowStage = item.WorkflowStage,
            ResolutionCode = item.ResolutionCode
        };
    }

    private static SettlementInstructionDto MapInstruction(SettlementInstruction item)
    {
        return new SettlementInstructionDto
        {
            Id = item.Id,
            ReconciliationExceptionId = item.ReconciliationExceptionId,
            InstructionType = item.InstructionType,
            Status = item.Status,
            Currency = item.Currency,
            Amount = item.Amount,
            SettlementAccount = item.SettlementAccount,
            Counterparty = item.Counterparty,
            DueAt = item.DueAt,
            CompletedAt = item.CompletedAt,
            Notes = item.Notes
        };
    }
}
