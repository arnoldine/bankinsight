using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IBranchLimitService
{
    Task<BranchLimitDto> CreateLimitAsync(CreateBranchLimitRequest request);
    Task<BranchLimitDto> UpdateLimitAsync(int id, CreateBranchLimitRequest request);
    Task<BranchLimitDto?> GetLimitAsync(int id);
    Task<List<BranchLimitDto>> GetBranchLimitsAsync(string branchId);
    Task<List<BranchLimitDto>> GetAllLimitsAsync();
    Task<bool> DeleteLimitAsync(int id);
    Task<bool> ValidateTransactionAgainstLimitsAsync(string branchId, string transactionType, decimal amount, string currency);
}

public class BranchLimitService : IBranchLimitService
{
    private readonly ApplicationDbContext _context;

    public BranchLimitService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BranchLimitDto> CreateLimitAsync(CreateBranchLimitRequest request)
    {
        var branch = await _context.Branches.FindAsync(request.BranchId);
        if (branch == null)
        {
            throw new Exception("Branch not found");
        }

        var limit = new BranchLimit
        {
            BranchId = request.BranchId,
            LimitType = request.LimitType,
            TransactionType = request.TransactionType,
            Currency = request.Currency,
            SingleTransactionLimit = request.SingleTransactionLimit,
            DailyLimit = request.DailyLimit,
            MonthlyLimit = request.MonthlyLimit,
            RequiresApproval = request.RequiresApproval,
            ApprovalThreshold = request.ApprovalThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BranchLimits.Add(limit);
        await _context.SaveChangesAsync();

        return await GetLimitDtoAsync(limit.Id);
    }

    public async Task<BranchLimitDto> UpdateLimitAsync(int id, CreateBranchLimitRequest request)
    {
        var limit = await _context.BranchLimits.FindAsync(id);
        if (limit == null)
        {
            throw new Exception("Limit not found");
        }

        limit.LimitType = request.LimitType;
        limit.TransactionType = request.TransactionType;
        limit.Currency = request.Currency;
        limit.SingleTransactionLimit = request.SingleTransactionLimit;
        limit.DailyLimit = request.DailyLimit;
        limit.MonthlyLimit = request.MonthlyLimit;
        limit.RequiresApproval = request.RequiresApproval;
        limit.ApprovalThreshold = request.ApprovalThreshold;
        limit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetLimitDtoAsync(limit.Id);
    }

    public async Task<BranchLimitDto?> GetLimitAsync(int id)
    {
        var limit = await _context.BranchLimits
            .Include(l => l.Branch)
            .FirstOrDefaultAsync(l => l.Id == id);

        return limit == null ? null : MapToDto(limit);
    }

    public async Task<List<BranchLimitDto>> GetBranchLimitsAsync(string branchId)
    {
        var limits = await _context.BranchLimits
            .Include(l => l.Branch)
            .Where(l => l.BranchId == branchId)
            .ToListAsync();

        return limits.Select(MapToDto).ToList();
    }

    public async Task<List<BranchLimitDto>> GetAllLimitsAsync()
    {
        var limits = await _context.BranchLimits
            .Include(l => l.Branch)
            .ToListAsync();

        return limits.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteLimitAsync(int id)
    {
        var limit = await _context.BranchLimits.FindAsync(id);
        if (limit == null)
        {
            return false;
        }

        _context.BranchLimits.Remove(limit);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ValidateTransactionAgainstLimitsAsync(string branchId, string transactionType, decimal amount, string currency)
    {
        var limits = await _context.BranchLimits
            .Where(l => l.BranchId == branchId 
                && l.Currency == currency
                && (l.TransactionType == transactionType || l.TransactionType == null))
            .ToListAsync();

        foreach (var limit in limits)
        {
            // Check single transaction limit
            if (limit.SingleTransactionLimit.HasValue && amount > limit.SingleTransactionLimit.Value)
            {
                return false;
            }

            // Check daily limit
            if (limit.DailyLimit.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                var todayTotal = await GetBranchTransactionTotalAsync(branchId, transactionType, currency, today, today.AddDays(1));
                
                if (todayTotal + amount > limit.DailyLimit.Value)
                {
                    return false;
                }
            }

            // Check monthly limit
            if (limit.MonthlyLimit.HasValue)
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                var monthTotal = await GetBranchTransactionTotalAsync(branchId, transactionType, currency, monthStart, monthEnd);
                
                if (monthTotal + amount > limit.MonthlyLimit.Value)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<decimal> GetBranchTransactionTotalAsync(string branchId, string transactionType, string currency, DateTime startDate, DateTime endDate)
    {
        // This would typically query the transactions table
        // For now, return 0 as a placeholder
        return await Task.FromResult(0m);
    }

    private async Task<BranchLimitDto> GetLimitDtoAsync(int id)
    {
        var limit = await _context.BranchLimits
            .Include(l => l.Branch)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (limit == null)
        {
            throw new Exception("Limit not found");
        }

        return MapToDto(limit);
    }

    private BranchLimitDto MapToDto(BranchLimit limit)
    {
        return new BranchLimitDto
        {
            Id = limit.Id,
            BranchId = limit.BranchId,
            BranchCode = limit.Branch?.Code ?? "",
            BranchName = limit.Branch?.Name ?? "",
            LimitType = limit.LimitType,
            TransactionType = limit.TransactionType,
            Currency = limit.Currency,
            SingleTransactionLimit = limit.SingleTransactionLimit,
            DailyLimit = limit.DailyLimit,
            MonthlyLimit = limit.MonthlyLimit,
            RequiresApproval = limit.RequiresApproval,
            ApprovalThreshold = limit.ApprovalThreshold
        };
    }
}
