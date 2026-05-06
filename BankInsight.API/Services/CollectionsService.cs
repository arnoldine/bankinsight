using System.Text.Json;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class CollectionsService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public CollectionsService(ApplicationDbContext context, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
    }

    public async Task<List<CollectionCaseDto>> GetCasesAsync()
    {
        await SynchronizeCasesAsync();

        var cases = await _context.CollectionCases
            .AsNoTracking()
            .Include(collectionCase => collectionCase.Customer)
            .Include(collectionCase => collectionCase.Events)
            .OrderByDescending(collectionCase => collectionCase.DelinquencyDays)
            .ThenBy(collectionCase => collectionCase.NextActionDate)
            .ToListAsync();

        return cases.Select(MapCase).ToList();
    }

    public async Task<CollectionCaseDto?> UpdateCaseAsync(string caseId, UpdateCollectionCaseRequest request, string? userId)
    {
        await SynchronizeCasesAsync();

        var collectionCase = await _context.CollectionCases
            .Include(item => item.Customer)
            .Include(item => item.Events)
            .FirstOrDefaultAsync(item => item.Id == caseId);

        if (collectionCase == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            collectionCase.Status = request.Status.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            collectionCase.Priority = request.Priority.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(request.RecoveryStage))
        {
            collectionCase.RecoveryStage = request.RecoveryStage.Trim().ToUpperInvariant();
        }

        collectionCase.AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? collectionCase.AssignedTo : request.AssignedTo.Trim();
        collectionCase.NextActionDate = request.NextActionDate ?? collectionCase.NextActionDate;
        collectionCase.PromiseToPayDate = request.PromiseToPayDate ?? collectionCase.PromiseToPayDate;
        collectionCase.PromiseToPayAmount = request.PromiseToPayAmount ?? collectionCase.PromiseToPayAmount;
        collectionCase.SettlementAmount = request.SettlementAmount ?? collectionCase.SettlementAmount;
        collectionCase.SettlementExpiryDate = request.SettlementExpiryDate ?? collectionCase.SettlementExpiryDate;
        collectionCase.RecoveryStrategy = string.IsNullOrWhiteSpace(request.RecoveryStrategy) ? collectionCase.RecoveryStrategy : request.RecoveryStrategy.Trim();
        collectionCase.LegalStatus = string.IsNullOrWhiteSpace(request.LegalStatus) ? collectionCase.LegalStatus : request.LegalStatus.Trim().ToUpperInvariant();
        collectionCase.AssignedAgency = string.IsNullOrWhiteSpace(request.AssignedAgency) ? collectionCase.AssignedAgency : request.AssignedAgency.Trim();
        collectionCase.RepossessionStatus = string.IsNullOrWhiteSpace(request.RepossessionStatus) ? collectionCase.RepossessionStatus : request.RepossessionStatus.Trim().ToUpperInvariant();
        collectionCase.ApprovalStatus = string.IsNullOrWhiteSpace(request.ApprovalStatus) ? collectionCase.ApprovalStatus : request.ApprovalStatus.Trim().ToUpperInvariant();
        collectionCase.WriteOffRecommendedAmount = request.WriteOffRecommendedAmount ?? collectionCase.WriteOffRecommendedAmount;
        collectionCase.WriteOffReason = string.IsNullOrWhiteSpace(request.WriteOffReason) ? collectionCase.WriteOffReason : request.WriteOffReason.Trim();
        collectionCase.Notes = string.IsNullOrWhiteSpace(request.Notes) ? collectionCase.Notes : request.Notes.Trim();
        collectionCase.LastContactAt = DateTime.UtcNow;
        collectionCase.UpdatedAt = DateTime.UtcNow;

        var caseEvent = new CollectionCaseEvent
        {
            CaseId = collectionCase.Id,
            EventType = request.EventType.Trim().ToUpperInvariant(),
            PerformedBy = userId,
            Detail = request.Detail.Trim(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                request.Status,
                request.Priority,
                request.RecoveryStage,
                request.AssignedTo,
                request.NextActionDate,
                request.PromiseToPayDate,
                request.PromiseToPayAmount,
                request.SettlementAmount,
                request.SettlementExpiryDate,
                request.RecoveryStrategy,
                request.LegalStatus,
                request.AssignedAgency,
                request.RepossessionStatus,
                request.ApprovalStatus,
                request.WriteOffRecommendedAmount,
                request.WriteOffReason
            }),
            CreatedAt = DateTime.UtcNow
        };

        _context.CollectionCaseEvents.Add(caseEvent);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "COLLECTION_CASE_UPDATED",
            "COLLECTION_CASE",
            collectionCase.Id,
            userId,
            $"Collection case {collectionCase.Id} updated with event {caseEvent.EventType}",
            status: "SUCCESS",
            newValues: new
            {
                collectionCase.Status,
                collectionCase.Priority,
                collectionCase.RecoveryStage,
                collectionCase.AssignedTo,
                collectionCase.NextActionDate,
                collectionCase.PromiseToPayDate,
                collectionCase.PromiseToPayAmount,
                collectionCase.SettlementAmount,
                collectionCase.SettlementExpiryDate,
                collectionCase.RecoveryStrategy,
                collectionCase.LegalStatus,
                collectionCase.AssignedAgency,
                collectionCase.RepossessionStatus,
                collectionCase.ApprovalStatus,
                collectionCase.WriteOffRecommendedAmount,
                collectionCase.WriteOffReason
            });

        collectionCase.Events = collectionCase.Events
            .OrderByDescending(item => item.CreatedAt)
            .ToList();
        collectionCase.Events.Add(caseEvent);

        return MapCase(collectionCase);
    }

    public async Task<CollectionCaseDto?> ExecuteCaseActionAsync(string caseId, ExecuteCollectionActionRequest request, string? userId)
    {
        await SynchronizeCasesAsync();

        var collectionCase = await _context.CollectionCases
            .Include(item => item.Customer)
            .Include(item => item.Events)
            .FirstOrDefaultAsync(item => item.Id == caseId);

        if (collectionCase == null)
        {
            return null;
        }

        var actionType = request.ActionType.Trim().ToUpperInvariant();
        var detail = string.IsNullOrWhiteSpace(request.Detail)
            ? $"Collection action {actionType} processed."
            : request.Detail.Trim();

        switch (actionType)
        {
            case "PROMISE_TO_PAY":
                collectionCase.PromiseToPayDate = request.PromiseToPayDate ?? collectionCase.PromiseToPayDate ?? DateTime.UtcNow.Date.AddDays(7);
                collectionCase.PromiseToPayAmount = request.PromiseToPayAmount ?? collectionCase.PromiseToPayAmount ?? collectionCase.AmountInArrears;
                collectionCase.RecoveryStage = "PROMISE_TO_PAY";
                collectionCase.Status = "MONITORING";
                break;
            case "SETTLEMENT_OFFER":
                collectionCase.SettlementAmount = request.SettlementAmount ?? collectionCase.SettlementAmount ?? collectionCase.AmountInArrears * 0.85m;
                collectionCase.SettlementExpiryDate = request.SettlementExpiryDate ?? DateTime.UtcNow.Date.AddDays(14);
                collectionCase.RecoveryStage = "SETTLEMENT_NEGOTIATION";
                collectionCase.RecoveryStrategy = "NEGOTIATED_SETTLEMENT";
                break;
            case "LEGAL_REFERRAL":
                collectionCase.RecoveryStage = "LEGAL_REVIEW";
                collectionCase.LegalStatus = "REFERRED";
                collectionCase.NextEscalationDate = request.NextActionDate ?? DateTime.UtcNow.Date.AddDays(5);
                collectionCase.Priority = "HIGH";
                collectionCase.ApprovalStatus = await CreateOrReuseApprovalAsync(
                    "COLLECTION_LEGAL_REFERRAL",
                    collectionCase,
                    userId,
                    new
                    {
                        CaseId = collectionCase.Id,
                        ActionType = actionType,
                        collectionCase.LegalStatus,
                        collectionCase.NextEscalationDate,
                        Detail = detail
                    });
                break;
            case "REPOSSESSION_REVIEW":
                collectionCase.RecoveryStage = "SECURED_ENFORCEMENT";
                collectionCase.RecoveryStrategy = "COLLATERAL_ENFORCEMENT";
                collectionCase.NextEscalationDate = request.NextActionDate ?? DateTime.UtcNow.Date.AddDays(7);
                collectionCase.RepossessionStatus = "UNDER_REVIEW";
                collectionCase.ApprovalStatus = await CreateOrReuseApprovalAsync(
                    "COLLECTION_REPOSSESSION",
                    collectionCase,
                    userId,
                    new
                    {
                        CaseId = collectionCase.Id,
                        ActionType = actionType,
                        collectionCase.RepossessionStatus,
                        collectionCase.NextEscalationDate,
                        Detail = detail
                    });
                break;
            case "WRITE_OFF_RECOMMENDATION":
                collectionCase.RecoveryStage = "WRITE_OFF_REVIEW";
                collectionCase.Status = "ESCALATED";
                collectionCase.NextEscalationDate = request.NextActionDate ?? DateTime.UtcNow.Date.AddDays(3);
                collectionCase.WriteOffRecommendedAmount = request.SettlementAmount ?? request.PromiseToPayAmount ?? collectionCase.AmountInArrears;
                collectionCase.WriteOffReason = string.IsNullOrWhiteSpace(request.WriteOffReason) ? detail : request.WriteOffReason.Trim();
                collectionCase.ApprovalStatus = await CreateOrReuseApprovalAsync(
                    "COLLECTION_WRITE_OFF",
                    collectionCase,
                    userId,
                    new
                    {
                        CaseId = collectionCase.Id,
                        ActionType = actionType,
                        collectionCase.WriteOffRecommendedAmount,
                        collectionCase.WriteOffReason,
                        collectionCase.NextEscalationDate
                    });
                break;
            case "ASSIGN_AGENCY":
                collectionCase.RecoveryStage = "OUTSOURCED_COLLECTIONS";
                collectionCase.AssignedAgency = string.IsNullOrWhiteSpace(request.AssignedAgency) ? collectionCase.AssignedAgency ?? "Unspecified Agency" : request.AssignedAgency.Trim();
                collectionCase.RecoveryStrategy = "AGENCY_COLLECTION";
                collectionCase.Status = "ASSIGNED";
                break;
            default:
                throw new InvalidOperationException($"Unsupported collection action '{request.ActionType}'.");
        }

        collectionCase.LastContactAt = DateTime.UtcNow;
        collectionCase.UpdatedAt = DateTime.UtcNow;

        var caseEvent = new CollectionCaseEvent
        {
            CaseId = collectionCase.Id,
            EventType = actionType,
            PerformedBy = userId,
            Detail = detail,
            MetadataJson = JsonSerializer.Serialize(request),
            CreatedAt = DateTime.UtcNow
        };

        _context.CollectionCaseEvents.Add(caseEvent);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync("COLLECTION_CASE_ACTION", "COLLECTION_CASE", collectionCase.Id, userId, $"Executed collection action {actionType} for case {collectionCase.Id}");

        collectionCase.Events = collectionCase.Events.OrderByDescending(item => item.CreatedAt).ToList();
        collectionCase.Events.Add(caseEvent);
        return MapCase(collectionCase);
    }

    private async Task<string> CreateOrReuseApprovalAsync(string entityType, CollectionCase collectionCase, string? userId, object payload)
    {
        var existing = await _context.ApprovalRequests
            .FirstOrDefaultAsync(item => item.EntityType == entityType && item.EntityId == collectionCase.Id && item.Status == "PENDING");

        if (existing != null)
        {
            return "PENDING_APPROVAL";
        }

        _context.ApprovalRequests.Add(new ApprovalRequest
        {
            Id = $"APP{Guid.NewGuid():N}"[..16],
            EntityType = entityType,
            EntityId = collectionCase.Id,
            RequesterId = userId,
            Status = "PENDING",
            CurrentStep = 0,
            PayloadJson = JsonSerializer.Serialize(payload),
            Remarks = $"Approval requested for {entityType} on collection case {collectionCase.Id}.",
            ReferenceNo = collectionCase.LoanId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        return "PENDING_APPROVAL";
    }

    private async Task SynchronizeCasesAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeLoans = await _context.Loans
            .AsNoTracking()
            .Where(loan => loan.Status == "ACTIVE" || loan.Status == "APPROVED")
            .ToListAsync();

        var activeLoanIds = activeLoans.Select(loan => loan.Id).ToList();
        var pastDueSchedules = await _context.LoanSchedules
            .AsNoTracking()
            .Where(schedule => schedule.LoanId != null
                && activeLoanIds.Contains(schedule.LoanId)
                && schedule.DueDate != null
                && schedule.DueDate < today
                && (schedule.Status == null || schedule.Status != "PAID"))
            .ToListAsync();

        var customerIds = activeLoans
            .Where(loan => !string.IsNullOrWhiteSpace(loan.CustomerId))
            .Select(loan => loan.CustomerId!)
            .Distinct()
            .ToList();

        var customerNames = await _context.Customers
            .AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, customer => customer.Name);

        foreach (var loan in activeLoans)
        {
            var loanSchedules = pastDueSchedules.Where(schedule => schedule.LoanId == loan.Id).ToList();
            if (loanSchedules.Count == 0)
            {
                continue;
            }

            var earliestDueDate = loanSchedules
                .Where(schedule => schedule.DueDate != null)
                .Min(schedule => schedule.DueDate!.Value);

            var delinquencyDays = Math.Max(1, today.DayNumber - earliestDueDate.DayNumber);
            var amountInArrears = loanSchedules.Sum(schedule => Math.Max(0m, (schedule.Total ?? 0m) - (schedule.PaidAmount ?? 0m)));

            var collectionCase = await _context.CollectionCases.FirstOrDefaultAsync(item => item.LoanId == loan.Id);
            if (collectionCase == null)
            {
                collectionCase = new CollectionCase
                {
                    Id = $"COL-{loan.Id}",
                    LoanId = loan.Id,
                    CustomerId = loan.CustomerId ?? string.Empty,
                    Status = "OPEN",
                    Priority = delinquencyDays >= 60 ? "HIGH" : delinquencyDays >= 30 ? "MEDIUM" : "LOW",
                    RecoveryStage = delinquencyDays >= 90 ? "LEGAL_REVIEW" : delinquencyDays >= 30 ? "INTENSIVE_FOLLOW_UP" : "EARLY_ARREARS",
                    DelinquencyDays = delinquencyDays,
                    OutstandingBalance = loan.OutstandingBalance ?? loan.Principal,
                    AmountInArrears = amountInArrears,
                    NextActionDate = DateTime.UtcNow.Date.AddDays(1),
                    NextEscalationDate = delinquencyDays >= 90 ? DateTime.UtcNow.Date.AddDays(2) : DateTime.UtcNow.Date.AddDays(7),
                    RecoveryStrategy = delinquencyDays >= 90 ? "LEGAL_ESCALATION" : delinquencyDays >= 30 ? "INTENSIVE_CONTACT" : "EARLY_CONTACT",
                    LegalStatus = delinquencyDays >= 90 ? "PENDING_REVIEW" : "NOT_STARTED",
                    ApprovalStatus = "NOT_REQUIRED",
                    RepossessionStatus = "NOT_STARTED",
                    Notes = $"Auto-opened from delinquency monitor for {(loan.CustomerId != null && customerNames.TryGetValue(loan.CustomerId, out var customerName) ? customerName : loan.CustomerId)}.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CollectionCases.Add(collectionCase);
            }
            else
            {
                collectionCase.DelinquencyDays = delinquencyDays;
                collectionCase.AmountInArrears = amountInArrears;
                collectionCase.OutstandingBalance = loan.OutstandingBalance ?? loan.Principal;
                collectionCase.Priority = delinquencyDays >= 60 ? "HIGH" : delinquencyDays >= 30 ? "MEDIUM" : "LOW";
                collectionCase.RecoveryStage = delinquencyDays >= 90 ? "LEGAL_REVIEW" : delinquencyDays >= 30 ? "INTENSIVE_FOLLOW_UP" : "EARLY_ARREARS";
                collectionCase.NextEscalationDate = delinquencyDays >= 90 ? DateTime.UtcNow.Date.AddDays(2) : DateTime.UtcNow.Date.AddDays(7);
                collectionCase.RecoveryStrategy = delinquencyDays >= 90 ? "LEGAL_ESCALATION" : delinquencyDays >= 30 ? "INTENSIVE_CONTACT" : "EARLY_CONTACT";
                collectionCase.LegalStatus = delinquencyDays >= 90 ? "PENDING_REVIEW" : "NOT_STARTED";
                collectionCase.ApprovalStatus ??= "NOT_REQUIRED";
                collectionCase.RepossessionStatus ??= "NOT_STARTED";
                collectionCase.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    private static CollectionCaseDto MapCase(CollectionCase collectionCase)
    {
        return new CollectionCaseDto
        {
            Id = collectionCase.Id,
            LoanId = collectionCase.LoanId,
            CustomerId = collectionCase.CustomerId,
            CustomerName = collectionCase.Customer?.Name ?? collectionCase.CustomerId,
            Status = collectionCase.Status,
            Priority = collectionCase.Priority,
            RecoveryStage = collectionCase.RecoveryStage,
            DelinquencyDays = collectionCase.DelinquencyDays,
            OutstandingBalance = collectionCase.OutstandingBalance,
            AmountInArrears = collectionCase.AmountInArrears,
            AssignedTo = collectionCase.AssignedTo,
            NextActionDate = collectionCase.NextActionDate,
            PromiseToPayDate = collectionCase.PromiseToPayDate,
            PromiseToPayAmount = collectionCase.PromiseToPayAmount,
            LastContactAt = collectionCase.LastContactAt,
            LastPaymentAt = collectionCase.LastPaymentAt,
            NextEscalationDate = collectionCase.NextEscalationDate,
            Notes = collectionCase.Notes,
            RecoveryStrategy = collectionCase.RecoveryStrategy,
            LegalStatus = collectionCase.LegalStatus,
            SettlementAmount = collectionCase.SettlementAmount,
            SettlementExpiryDate = collectionCase.SettlementExpiryDate,
            AssignedAgency = collectionCase.AssignedAgency,
            RepossessionStatus = collectionCase.RepossessionStatus,
            ApprovalStatus = collectionCase.ApprovalStatus,
            WriteOffRecommendedAmount = collectionCase.WriteOffRecommendedAmount,
            WriteOffReason = collectionCase.WriteOffReason,
            Events = collectionCase.Events
                .OrderByDescending(item => item.CreatedAt)
                .Take(10)
                .Select(item => new CollectionCaseEventDto
                {
                    EventType = item.EventType,
                    PerformedBy = item.PerformedBy,
                    Detail = item.Detail,
                    MetadataJson = item.MetadataJson,
                    CreatedAt = item.CreatedAt
                })
                .ToList()
        };
    }
}
