using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IInterBranchTransferService
{
    Task<InterBranchTransferDto> InitiateTransferAsync(CreateInterBranchTransferRequest request, string initiatedBy);
    Task<InterBranchTransferDto> ApproveTransferAsync(ApproveInterBranchTransferRequest request, string approvedBy);
    Task<InterBranchTransferDto> DispatchTransferAsync(DispatchInterBranchTransferRequest request, string sentBy);
    Task<InterBranchTransferDto> ReceiveTransferAsync(ReceiveInterBranchTransferRequest request, string receivedBy);
    Task<InterBranchTransferDto?> GetTransferAsync(string transferId);
    Task<List<InterBranchTransferDto>> GetTransfersAsync();
    Task<List<InterBranchTransferDto>> GetBranchTransfersAsync(string branchId);
    Task<List<InterBranchTransferDto>> GetPendingTransfersAsync();
}

public class InterBranchTransferService : IInterBranchTransferService
{
    private readonly ApplicationDbContext _context;
    private readonly IVaultManagementService _vaultService;

    public InterBranchTransferService(ApplicationDbContext context, IVaultManagementService vaultService)
    {
        _context = context;
        _vaultService = vaultService;
    }

    public async Task<InterBranchTransferDto> InitiateTransferAsync(CreateInterBranchTransferRequest request, string initiatedBy)
    {
        var fromBranch = await _context.Branches.FindAsync(request.FromBranchId);
        var toBranch = await _context.Branches.FindAsync(request.ToBranchId);

        if (fromBranch == null || toBranch == null)
        {
            throw new Exception("One or both branches not found");
        }

        var vault = await _vaultService.GetVaultAsync(request.FromBranchId, request.Currency);
        if (vault == null || vault.CashOnHand < request.Amount)
        {
            throw new Exception("Insufficient funds in source branch vault");
        }

        var transfer = new InterBranchTransfer
        {
            Id = $"IBT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            FromBranchId = request.FromBranchId,
            ToBranchId = request.ToBranchId,
            Currency = request.Currency,
            Amount = request.Amount,
            Reference = request.Reference,
            Narration = request.Narration,
            Status = "Pending",
            InitiatedBy = initiatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.InterBranchTransfers.Add(transfer);
        await _context.SaveChangesAsync();

        return await GetTransferDtoAsync(transfer.Id);
    }

    public async Task<InterBranchTransferDto> ApproveTransferAsync(ApproveInterBranchTransferRequest request, string approvedBy)
    {
        var transfer = await LoadTransferForUpdateAsync(request.TransferId);

        if (!transfer.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Transfer is already {transfer.Status}");
        }

        transfer.ApprovedBy = approvedBy;
        transfer.ApprovedAt = DateTime.UtcNow;

        if (request.Approved)
        {
            transfer.Status = "Approved";
        }
        else
        {
            transfer.Status = "Rejected";
            transfer.RejectionReason = request.RejectionReason;
            transfer.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return await GetTransferDtoAsync(transfer.Id);
    }

    public async Task<InterBranchTransferDto> DispatchTransferAsync(DispatchInterBranchTransferRequest request, string sentBy)
    {
        var transfer = await LoadTransferForUpdateAsync(request.TransferId);

        if (!transfer.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Only approved transfers can be dispatched.");
        }

        await _vaultService.ProcessVaultTransactionAsync(new VaultTransactionRequest
        {
            BranchId = transfer.FromBranchId,
            Currency = transfer.Currency,
            Amount = transfer.Amount,
            Type = "Withdrawal",
            Reference = transfer.Id,
            Narration = $"Inter-branch transfer dispatch to {transfer.ToBranch?.Name}"
        }, sentBy);

        transfer.Status = "InTransit";
        transfer.SentBy = sentBy;
        transfer.DispatchedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetTransferDtoAsync(transfer.Id);
    }

    public async Task<InterBranchTransferDto> ReceiveTransferAsync(ReceiveInterBranchTransferRequest request, string receivedBy)
    {
        var transfer = await LoadTransferForUpdateAsync(request.TransferId);

        if (!transfer.Status.Equals("InTransit", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Only in-transit transfers can be received.");
        }

        await _vaultService.ProcessVaultTransactionAsync(new VaultTransactionRequest
        {
            BranchId = transfer.ToBranchId,
            Currency = transfer.Currency,
            Amount = transfer.Amount,
            Type = "Deposit",
            Reference = transfer.Id,
            Narration = $"Inter-branch transfer receipt from {transfer.FromBranch?.Name}"
        }, receivedBy);

        transfer.Status = "Received";
        transfer.ReceivedBy = receivedBy;
        transfer.ReceivedAt = DateTime.UtcNow;
        transfer.CompletedAt = transfer.ReceivedAt;

        await _context.SaveChangesAsync();
        return await GetTransferDtoAsync(transfer.Id);
    }

    public async Task<InterBranchTransferDto?> GetTransferAsync(string transferId)
    {
        var transfer = await QueryTransfers().FirstOrDefaultAsync(t => t.Id == transferId);
        return transfer == null ? null : MapToDto(transfer);
    }

    public async Task<List<InterBranchTransferDto>> GetTransfersAsync()
    {
        var transfers = await QueryTransfers().OrderByDescending(t => t.CreatedAt).ToListAsync();
        return transfers.Select(MapToDto).ToList();
    }

    public async Task<List<InterBranchTransferDto>> GetBranchTransfersAsync(string branchId)
    {
        var transfers = await QueryTransfers()
            .Where(t => t.FromBranchId == branchId || t.ToBranchId == branchId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transfers.Select(MapToDto).ToList();
    }

    public async Task<List<InterBranchTransferDto>> GetPendingTransfersAsync()
    {
        var transfers = await QueryTransfers()
            .Where(t => t.Status == "Pending" || t.Status == "Approved" || t.Status == "InTransit")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return transfers.Select(MapToDto).ToList();
    }

    private IQueryable<InterBranchTransfer> QueryTransfers()
    {
        return _context.InterBranchTransfers
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .Include(t => t.Initiator)
            .Include(t => t.Approver)
            .Include(t => t.Sender)
            .Include(t => t.Receiver);
    }

    private async Task<InterBranchTransfer> LoadTransferForUpdateAsync(string transferId)
    {
        var transfer = await QueryTransfers().FirstOrDefaultAsync(t => t.Id == transferId);
        if (transfer == null)
        {
            throw new Exception("Transfer not found");
        }

        return transfer;
    }

    private async Task<InterBranchTransferDto> GetTransferDtoAsync(string transferId)
    {
        var transfer = await QueryTransfers().FirstOrDefaultAsync(t => t.Id == transferId);
        if (transfer == null)
        {
            throw new Exception("Transfer not found");
        }

        return MapToDto(transfer);
    }

    private static string ResolveTransitStage(InterBranchTransfer transfer)
    {
        if (transfer.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return "REJECTED";
        }

        if (transfer.Status.Equals("Received", StringComparison.OrdinalIgnoreCase))
        {
            return "RECEIVED";
        }

        if (transfer.Status.Equals("InTransit", StringComparison.OrdinalIgnoreCase))
        {
            return "IN_TRANSIT";
        }

        if (transfer.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
        {
            return "READY_FOR_DISPATCH";
        }

        return "AWAITING_APPROVAL";
    }

    private InterBranchTransferDto MapToDto(InterBranchTransfer transfer)
    {
        return new InterBranchTransferDto
        {
            Id = transfer.Id,
            FromBranchId = transfer.FromBranchId,
            FromBranchCode = transfer.FromBranch?.Code ?? "",
            FromBranchName = transfer.FromBranch?.Name ?? "",
            ToBranchId = transfer.ToBranchId,
            ToBranchCode = transfer.ToBranch?.Code ?? "",
            ToBranchName = transfer.ToBranch?.Name ?? "",
            Currency = transfer.Currency,
            Amount = transfer.Amount,
            Reference = transfer.Reference,
            Narration = transfer.Narration,
            Status = transfer.Status,
            TransitStage = ResolveTransitStage(transfer),
            InitiatedBy = transfer.InitiatedBy,
            InitiatedByName = transfer.Initiator?.Name ?? "",
            ApprovedBy = transfer.ApprovedBy,
            ApprovedByName = transfer.Approver?.Name,
            SentBy = transfer.SentBy,
            SentByName = transfer.Sender?.Name,
            ReceivedBy = transfer.ReceivedBy,
            ReceivedByName = transfer.Receiver?.Name,
            CreatedAt = transfer.CreatedAt,
            ApprovedAt = transfer.ApprovedAt,
            DispatchedAt = transfer.DispatchedAt,
            ReceivedAt = transfer.ReceivedAt,
            CompletedAt = transfer.CompletedAt,
            RejectionReason = transfer.RejectionReason
        };
    }
}
