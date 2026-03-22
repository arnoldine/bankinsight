using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class TransactionService
{
    private static readonly HashSet<string> SupportedTransactionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DEPOSIT",
        "WITHDRAWAL",
        "TRANSFER",
        "LOAN_REPAYMENT"
    };

    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IKycService _kycService;
    private readonly ISuspiciousActivityService _suspiciousActivityService;
    private readonly Security.ICurrentUserContext _currentUser;
    private readonly IPostingEngine _postingEngine;

    public TransactionService(
        ApplicationDbContext context, 
        IAuditLoggingService auditLoggingService,
        IKycService kycService,
        ISuspiciousActivityService suspiciousActivityService,
        Security.ICurrentUserContext currentUser,
        IPostingEngine postingEngine)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _kycService = kycService;
        _suspiciousActivityService = suspiciousActivityService;
        _currentUser = currentUser;
        _postingEngine = postingEngine;
    }

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        var query = _context.Transactions.AsQueryable();
        if (_currentUser.ScopeType == AccessScopeType.BranchOnly && !string.IsNullOrEmpty(_currentUser.BranchId))
        {
            query = query.Where(t => t.Account != null && t.Account.Customer != null && t.Account.Customer.BranchId == _currentUser.BranchId);
        }

        return await query
            .OrderByDescending(t => t.Date)
            .Take(100)
            .ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(string id)
    {
        return await _context.Transactions.FindAsync(id);
    }

    public async Task<Transaction> PostTransactionAsync(CreateTransactionRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        Transaction? newTransaction = null;
        string? txnId = null;
        string? refNum = null;
        decimal oldBalance = 0m;
        decimal availableBalance = 0m;
        string normalizedType = NormalizeTransactionType(request.Type);

        try
        {
            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account Not Found");
            }

            if (!string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Account status {account.Status} does not allow transactions");
            }

            if (string.IsNullOrWhiteSpace(request.TellerId))
            {
                throw new InvalidOperationException("TellerId is required");
            }

            var teller = await _context.Staff.FirstOrDefaultAsync(s => s.Id == request.TellerId);
            if (teller == null || !string.Equals(teller.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid or inactive teller");
            }

            if (string.IsNullOrWhiteSpace(account.CustomerId))
            {
                throw new InvalidOperationException("Account is missing CustomerId");
            }

            var isValidAmount = await _kycService.ValidateTransactionAmountAsync(account.CustomerId, request.Amount);
            if (!isValidAmount)
            {
                var kycInfo = await _kycService.GetKycLimitInfoAsync(account.CustomerId);
                throw new InvalidOperationException(
                    $"Transaction amount {request.Amount:C} exceeds KYC {kycInfo.KycLevel} limit of {kycInfo.TransactionLimit:C}");
            }

            var kycLimits = await _kycService.GetKycLimitInfoAsync(account.CustomerId);
            if (!kycLimits.IsUnlimited)
            {
                var utcDayStart = DateTime.UtcNow.Date;
                var utcDayEnd = utcDayStart.AddDays(1);

                var todayTotal = await _context.Transactions
                    .Where(t => t.AccountId == request.AccountId
                                && t.Status == "POSTED"
                                && t.Date >= utcDayStart
                                && t.Date < utcDayEnd)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

                if (todayTotal + request.Amount > kycLimits.DailyLimit)
                {
                    throw new InvalidOperationException(
                        $"Daily transaction limit exceeded. Daily limit: {kycLimits.DailyLimit:C}, Today's total: {todayTotal:C}");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.ClientReference))
            {
                var duplicate = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Reference == request.ClientReference && t.AccountId == request.AccountId);
                if (duplicate != null)
                {
                    return duplicate;
                }
            }

            availableBalance = Math.Max(0m, account.Balance - account.LienAmount);

            if ((normalizedType == "WITHDRAWAL" || normalizedType == "TRANSFER") && availableBalance < request.Amount)
            {
                throw new InvalidOperationException("Insufficient Funds");
            }

            txnId = $"TXN{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            refNum = !string.IsNullOrWhiteSpace(request.ClientReference)
                ? request.ClientReference.Trim()
                : GenerateSecureReference();

            newTransaction = new Transaction
            {
                Id = txnId,
                AccountId = request.AccountId,
                Type = normalizedType,
                Amount = request.Amount,
                Narration = request.Narration,
                TellerId = request.TellerId,
                Status = "POSTED",
                Reference = refNum,
                Date = DateTime.UtcNow
            };

            _context.Transactions.Add(newTransaction);

            oldBalance = account.Balance;
            string eventType = EventTypes.DepositPosted;
            if (normalizedType == "WITHDRAWAL") eventType = EventTypes.WithdrawalPosted;
            else if (normalizedType == "TRANSFER") eventType = EventTypes.TransferCompleted;
            else if (normalizedType == "LOAN_REPAYMENT") eventType = EventTypes.LoanRepaymentReceived;
            
            var financialEvent = new FinancialEvent
            {
                EventType = eventType,
                EntityType = "ACCOUNT",
                EntityId = account.Id,
                Amount = request.Amount,
                Currency = account.Currency ?? "GHS",
                BranchId = request.TellerId,
                Reference = refNum,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { Method = "Cash/Direct" }),
                CreatedBy = request.TellerId
            };
            
            var postResult = await _postingEngine.ProcessEventAsync(financialEvent);
            if (!postResult.Success)
            {
                throw new InvalidOperationException($"Posting Engine Failed: {postResult.ErrorMessage}");
            }

            // Let posting engine implicitly handle balance changes via ledger rebuilds
            // For now, in a phased architecture, if we STILL need to mutate balance for legacy reads:
            if (normalizedType == "WITHDRAWAL" || normalizedType == "TRANSFER" || normalizedType == "LOAN_REPAYMENT")
            {
                account.Balance -= request.Amount;
            }
            else
            {
                account.Balance += request.Amount;
            }

            account.LastTransDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            try
            {
                await _auditLoggingService.LogActionAsync(
                    action: "POST_TRANSACTION_FAILED",
                    entityType: "TRANSACTION",
                    entityId: null,
                    userId: request.TellerId,
                    description: $"Failed to post {request.Type} transaction of {request.Amount:C}",
                    status: "FAILED",
                    errorMessage: ex.Message
                );
            }
            catch
            {
            }

            throw;
        }

        try
        {
            await _auditLoggingService.LogActionAsync(
                action: "POST_TRANSACTION",
                entityType: "TRANSACTION",
                entityId: txnId,
                userId: request.TellerId,
                description: $"Posted {normalizedType} transaction of {request.Amount:C} to account {request.AccountId}",
                status: "SUCCESS",
                newValues: new
                {
                    TransactionId = txnId,
                    AccountId = request.AccountId,
                    Type = normalizedType,
                    Amount = request.Amount,
                    Reference = refNum,
                    Narration = request.Narration
                },
                oldValues: new { AccountBalance = oldBalance, AvailableBalance = availableBalance }
            );

            await _suspiciousActivityService.HandleLargeTransactionAsync(
                request.AccountId,
                request.Amount,
                normalizedType,
                request.TellerId);
        }
        catch
        {
        }

        return newTransaction!;
    }

    private static string NormalizeTransactionType(string type)
    {
        var normalized = type?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!SupportedTransactionTypes.Contains(normalized))
        {
            throw new InvalidOperationException($"Unsupported transaction type: {type}");
        }

        return normalized;
    }

    private static string GenerateSecureReference()
    {
        Span<byte> randomBytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(randomBytes);
        var token = Convert.ToHexString(randomBytes);
        return $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{token}";
    }
}
