using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IFeeService
{
    Task<AccountFeeDto> AssessAccountFeeAsync(AssessAccountFeeRequest request);
}

public class FeeService : IFeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ISuspiciousActivityService _suspiciousActivityService;

    public FeeService(
        ApplicationDbContext context,
        IAuditLoggingService auditLoggingService,
        ISuspiciousActivityService suspiciousActivityService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _suspiciousActivityService = suspiciousActivityService;
    }

    public async Task<AccountFeeDto> AssessAccountFeeAsync(AssessAccountFeeRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            if (account.Status != "ACTIVE" && account.Status != "Active")
            {
                throw new InvalidOperationException("Account must be ACTIVE to assess fees");
            }

            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Fee amount must be greater than zero");
            }

            var feeReference = string.IsNullOrWhiteSpace(request.ClientReference)
                ? $"FEE-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}"
                : $"FEE-{request.ClientReference.Trim()}";

            if (!string.IsNullOrWhiteSpace(request.ClientReference))
            {
                var existingTxn = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Reference == feeReference && t.Type == "FEE");

                if (existingTxn != null)
                {
                    return new AccountFeeDto
                    {
                        TransactionId = existingTxn.Id,
                        AccountId = existingTxn.AccountId ?? string.Empty,
                        FeeCode = request.FeeCode,
                        Amount = existingTxn.Amount,
                        Narration = existingTxn.Narration ?? string.Empty,
                        PostedAt = existingTxn.Date
                    };
                }
            }

            var availableBalance = account.Balance - account.LienAmount;
            if (availableBalance < request.Amount)
            {
                throw new InvalidOperationException($"Insufficient available balance. Account has {availableBalance:N2} available (Balance: {account.Balance:N2}, Lien: {account.LienAmount:N2})");
            }

            var narration = string.IsNullOrWhiteSpace(request.Narration)
                ? $"Fee: {request.FeeCode}"
                : request.Narration.Trim();

            var txn = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = request.AccountId,
                Type = "FEE",
                Amount = request.Amount,
                Narration = narration,
                Date = DateTime.UtcNow,
                Status = "POSTED",
                Reference = feeReference,
                TellerId = null
            };

            account.Balance = Math.Round(account.Balance - request.Amount, 2, MidpointRounding.AwayFromZero);
            account.LastTransDate = DateTime.UtcNow;

            _context.Transactions.Add(txn);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _auditLoggingService.LogActionAsync(
                action: "FEE_ASSESSED",
                entityType: "ACCOUNT",
                entityId: request.AccountId,
                userId: null,
                description: $"Fee {request.FeeCode} assessed on account {request.AccountId}",
                status: "SUCCESS",
                newValues: new { txn.Id, request.FeeCode, request.Amount, account.Balance, feeReference });

            await _suspiciousActivityService.HandleLargeTransactionAsync(request.AccountId, request.Amount, "FEE", null);

            return new AccountFeeDto
            {
                TransactionId = txn.Id,
                AccountId = request.AccountId,
                FeeCode = request.FeeCode,
                Amount = request.Amount,
                Narration = narration,
                PostedAt = txn.Date
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            await _auditLoggingService.LogActionAsync(
                action: "FEE_ASSESSMENT_FAILED",
                entityType: "ACCOUNT",
                entityId: request.AccountId,
                userId: null,
                description: $"Failed to assess fee on account {request.AccountId}",
                status: "FAILED",
                errorMessage: ex.Message,
                newValues: new { request.AccountId, request.FeeCode, request.Amount, request.ClientReference });

            throw;
        }
    }
}
