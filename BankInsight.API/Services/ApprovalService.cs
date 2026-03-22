using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class ApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly ICashIncidentService _cashIncidentService;
    private readonly IVaultManagementService _vaultManagementService;

    public ApprovalService(ApplicationDbContext context, ICashIncidentService cashIncidentService, IVaultManagementService vaultManagementService)
    {
        _context = context;
        _cashIncidentService = cashIncidentService;
        _vaultManagementService = vaultManagementService;
    }

    public async Task<List<ApprovalRequestDto>> GetApprovalsAsync()
    {
        var approvals = await _context.ApprovalRequests
            .Include(a => a.Workflow)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var loanIds = approvals
            .Where(a => string.Equals(a.EntityType, "LOAN", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.EntityId)
            .Distinct()
            .ToList();

        var loanDetails = await _context.Loans
            .Where(l => loanIds.Contains(l.Id))
            .Include(l => l.Customer)
            .Include(l => l.LoanProduct)
            .ToDictionaryAsync(
                loan => loan.Id,
                loan => new LoanApprovalDetailsDto
                {
                    LoanId = loan.Id,
                    CustomerId = loan.CustomerId ?? string.Empty,
                    CustomerName = loan.Customer?.Name,
                    ProductCode = loan.ProductCode,
                    ProductName = loan.LoanProduct != null ? loan.LoanProduct.Name : loan.ProductCode,
                    Principal = loan.Principal,
                    OutstandingBalance = loan.OutstandingBalance,
                    Rate = loan.Rate,
                    TermMonths = loan.TermMonths,
                    CollateralType = loan.CollateralType,
                    CollateralValue = loan.CollateralValue,
                    ParBucket = loan.ParBucket,
                    Status = loan.Status,
                    AppliedAt = loan.ApplicationDate,
                });

        return approvals
            .Select(a => new ApprovalRequestDto
            {
                Id = a.Id,
                WorkflowId = a.WorkflowId,
                WorkflowName = a.Workflow != null ? a.Workflow.Name : null,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                RequesterId = a.RequesterId,
                Status = a.Status,
                CurrentStep = a.CurrentStep,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                Remarks = a.Remarks,
                ReferenceNo = a.ReferenceNo,
                PayloadJson = a.PayloadJson,
                LoanDetails = loanDetails.GetValueOrDefault(a.EntityId)
            })
            .ToList();
    }

    public async Task<ApprovalRequest> CreateApprovalAsync(CreateApprovalRequest request)
    {
        var id = $"APP{(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000).ToString().PadLeft(4, '0')}";

        var approval = new ApprovalRequest
        {
            Id = id,
            WorkflowId = request.WorkflowId,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            RequesterId = request.RequesterId,
            Status = "PENDING",
            CurrentStep = 0,
            PayloadJson = request.PayloadJson,
            Remarks = request.Remarks,
            ReferenceNo = request.ReferenceNo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ApprovalRequests.Add(approval);
        await _context.SaveChangesAsync();

        return approval;
    }

    public async Task<ApprovalRequest?> UpdateApprovalAsync(string id, UpdateApprovalRequest request, string? actingUserId = null)
    {
        var approval = await _context.ApprovalRequests.FindAsync(id);
        if (approval == null) return null;

        var normalizedStatus = request.Status.ToUpperInvariant();
        if (normalizedStatus == "APPROVED")
        {
            await ExecuteApprovalActionAsync(approval, actingUserId);
        }

        approval.Status = request.Status;
        approval.CurrentStep = request.CurrentStep;
        if (!string.IsNullOrWhiteSpace(request.Remarks))
        {
            approval.Remarks = request.Remarks;
        }
        approval.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return approval;
    }

    private async Task ExecuteApprovalActionAsync(ApprovalRequest approval, string? actingUserId)
    {
        if (string.Equals(approval.EntityType, "CASH_INCIDENT_RESOLUTION", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(approval.PayloadJson))
            {
                throw new InvalidOperationException("Approval payload is missing for cash incident resolution.");
            }

            var payload = JsonSerializer.Deserialize<CashIncidentResolutionApprovalPayload>(approval.PayloadJson)
                ?? throw new InvalidOperationException("Cash incident resolution payload is invalid.");

            await _cashIncidentService.ResolveIncidentAsync(
                payload.IncidentId,
                actingUserId ?? payload.RequestedBy ?? "SYSTEM",
                payload.ResolutionNote);
            return;
        }

        if (string.Equals(approval.EntityType, "TILL_CASH_MOVEMENT_APPROVAL", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(approval.PayloadJson))
            {
                throw new InvalidOperationException("Approval payload is missing for till cash movement.");
            }

            var payload = JsonSerializer.Deserialize<TillCashMovementApprovalPayload>(approval.PayloadJson)
                ?? throw new InvalidOperationException("Till cash movement payload is invalid.");

            if (string.Equals(payload.Direction, "RETURN", StringComparison.OrdinalIgnoreCase))
            {
                await _vaultManagementService.ReturnTillCashAsync(payload.Request, actingUserId ?? payload.RequestedBy ?? "SYSTEM");
            }
            else
            {
                await _vaultManagementService.AllocateTillCashAsync(payload.Request, actingUserId ?? payload.RequestedBy ?? "SYSTEM");
            }
        }
    }
}

internal sealed class CashIncidentResolutionApprovalPayload
{
    public string IncidentId { get; set; } = string.Empty;
    public string ResolutionNote { get; set; } = string.Empty;
    public string? RequestedBy { get; set; }
}

internal sealed class TillCashMovementApprovalPayload
{
    public string Direction { get; set; } = "ALLOCATE";
    public TillCashTransferRequest Request { get; set; } = new();
    public string? RequestedBy { get; set; }
}



