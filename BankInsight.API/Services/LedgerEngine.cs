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

/// <summary>
/// Ledger Engine: Double-entry bookkeeping system for all banking transactions
/// Ensures BOG compliance including customer ID verification, KYC limits, and margin validation
/// </summary>
public interface ILedgerEngine
{
    Task<LedgerPostingResult> PostDepositAsync(DepositRequest request);
    Task<LedgerPostingResult> PostWithdrawalAsync(WithdrawalRequest request);
    Task<LedgerPostingResult> PostTransferAsync(TransferRequest request);
    Task<LedgerPostingResult> PostChequeAsync(ChequeRequest request);
    Task<List<LedgerEntry>> GetLedgerEntriesAsync(string accountId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<decimal> GetAvailableMarginAsync(string customerId);
    Task<LedgerBalance> GetAccountBalanceAsync(string accountId);
}

// --- DATA TRANSFER OBJECTS ---

public class DepositRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string DepositMethod { get; set; } = "CASH"; // CASH, CHEQUE
    public string ChequeNumber { get; set; } = string.Empty;
    public string ChequeBank { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public string TellerId { get; set; } = string.Empty;
    public string CustomerGhanaCard { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
}

public class WithdrawalRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string WithdrawalMethod { get; set; } = "CASH"; // CASH, CHEQUE
    public string Narration { get; set; } = string.Empty;
    public string TellerId { get; set; } = string.Empty;
    public string CustomerGhanaCard { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string? ChequeNumber { get; set; }
}

public class TransferRequest
{
    public string FromAccountId { get; set; } = string.Empty;
    public string ToAccountId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Narration { get; set; } = string.Empty;
    public string TellerId { get; set; } = string.Empty;
    public string CustomerGhanaCard { get; set; } = string.Empty;
}

public class ChequeRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ChequeNumber { get; set; } = string.Empty;
    public string ChequeBank { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Narration { get; set; } = string.Empty;
    public string TransactionType { get; set; } = "DEPOSIT"; // DEPOSIT or WITHDRAWAL
    public string TellerId { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
}

public class LedgerPostingResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AppliedFees { get; set; }
    public decimal NetAmount { get; set; }
    public decimal NewBalance { get; set; }
    public decimal AvailableMargin { get; set; }
    public string Status { get; set; } = "POSTED"; // POSTED, PENDING, REJECTED
    public string Message { get; set; } = string.Empty;
    public List<LedgerEntry> JournalLines { get; set; } = new();
}

public class LedgerEntry
{
    public string Id { get; set; } = string.Empty;
    public string JournalId { get; set; } = string.Empty;
    public string GLCode { get; set; } = string.Empty;
    public string GLName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Narration { get; set; } = string.Empty;
    public DateTime PostedDate { get; set; }
}

public class LedgerBalance
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal LienAmount { get; set; }
    public decimal AvailableBalance => Balance - LienAmount;
    public decimal DailyDebitTotal { get; set; }
    public decimal DailyCreditTotal { get; set; }
}

// --- LEDGER ENGINE IMPLEMENTATION ---

public class LedgerEngine : ILedgerEngine
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IKycService _kycService;
    private readonly ISuspiciousActivityService _suspiciousActivityService;
    private readonly IFeeService _feeService;
    private readonly IPostingEngine _postingEngine;

    // BOG Compliance: KYC-based daily transaction limits
    private const decimal TIER1_DAILY_LIMIT = 5000m;    // GHS 5,000
    private const decimal TIER2_DAILY_LIMIT = 50000m;   // GHS 50,000
    private const decimal TIER3_DAILY_LIMIT = decimal.MaxValue; // Unlimited

    // GL Account Codes for double-entry bookkeeping
    private const string GL_CASH_ON_HAND = "10100"; // Asset: Cash
    private const string GL_BANK_CLEARING = "10110"; // Asset: Cheques Clearing
    private const string GL_CUSTOMER_DEPOSITS = "20100"; // Liability: Savings Deposits
    private const string GL_CURRENT_DEPOSITS = "20101"; // Liability: Current Deposits
    private const string GL_FEES_EARNED = "40500"; // Income: Fees
    private const string GL_CHEQUE_CLEARING = "50500"; // Expense: Clearing Fees

    public LedgerEngine(
        ApplicationDbContext context,
        IAuditLoggingService auditLoggingService,
        IKycService kycService,
        ISuspiciousActivityService suspiciousActivityService,
        IFeeService feeService,
        IPostingEngine postingEngine)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _kycService = kycService;
        _suspiciousActivityService = suspiciousActivityService;
        _feeService = feeService;
        _postingEngine = postingEngine;
    }

    /// <summary>
    /// Posts a cash or cheque deposit with full BOG compliance
    /// </summary>
    public async Task<LedgerPostingResult> PostDepositAsync(DepositRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        var result = new LedgerPostingResult();
        var journalLines = new List<JournalLine>();

        try
        {
            // Step 1: Validate customer and account existence
            var customer = await _context.Customers.FindAsync(request.CustomerId);
            if (customer == null)
                throw new InvalidOperationException("Customer not found");

            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null)
                throw new InvalidOperationException("Account not found");

            // Step 2: BOG Customer ID Verification
            await ValidateCustomerIdAsync(request.CustomerId, request.CustomerGhanaCard);

            // Step 3: BOG KYC Limit Validation
            var kycLimit = await _kycService.GetKycLimitInfoAsync(request.CustomerId);
            await ValidateDailyLimitAsync(request.AccountId, request.Amount, kycLimit.DailyLimit);

            // Step 4: Calculate fees based on deposit method
            var fees = CalculateDepositFees(request.DepositMethod, request.Amount);

            // Step 5: Create transaction record
            var txnId = GenerateTransactionId();
            var refNum = GenerateSecureReference();

            var newTransaction = new Transaction
            {
                Id = txnId,
                AccountId = request.AccountId,
                Type = "DEPOSIT",
                Amount = request.Amount,
                Narration = $"{request.DepositMethod} Deposit: {request.Narration}",
                TellerId = request.TellerId,
                Status = "POSTED",
                Reference = refNum,
                Date = DateTime.UtcNow
            };

            _context.Transactions.Add(newTransaction);

            // Step 6: Update account balance
            account.Balance += request.Amount;
            account.LastTransDate = DateTime.UtcNow;

            // Step 7: Create Financial Event instead of direct double entry log
            var depositEvent = new FinancialEvent
            {
                EventType = EventTypes.DepositPosted,
                EntityType = "ACCOUNT",
                EntityId = account.Id,
                Amount = request.Amount,
                Currency = account.Currency ?? "GHS",
                BranchId = request.BranchId,
                Reference = refNum,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { Method = request.DepositMethod }),
                CreatedBy = request.TellerId
            };

            var postResult = await _postingEngine.ProcessEventAsync(depositEvent);
            if (!postResult.Success)
            {
                throw new InvalidOperationException($"Posting Engine Failed: {postResult.ErrorMessage}");
            }
            
            // Step 8: Optionally handle fees mapping
            if (fees > 0)
            {
                account.Balance -= fees;
                var feeEvent = new FinancialEvent
                {
                    EventType = EventTypes.ChargeApplied,
                    EntityType = "ACCOUNT",
                    EntityId = account.Id,
                    Amount = fees,
                    Currency = account.Currency ?? "GHS",
                    Reference = $"FEE-{refNum}",
                    PayloadJson = "{\"Reason\": \"Deposit Fee\"}",
                    CreatedBy = "SYSTEM"
                };
                await _postingEngine.ProcessEventAsync(feeEvent);
            }

            // Step 8: Save and commit transaction
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Step 9: Audit logging
            await _auditLoggingService.LogActionAsync(
                action: "DEPOSIT_POSTED",
                entityType: "TRANSACTION",
                entityId: txnId,
                userId: request.TellerId,
                description: $"Deposit of {request.Amount:C} via {request.DepositMethod} to account {request.AccountId}",
                status: "SUCCESS",
                newValues: new { Amount = request.Amount, Balance = account.Balance, Fees = fees }
            );

            // Step 10: Check for suspicious activity (large transactions)
            if (request.Amount > 10000)
            {
                await _suspiciousActivityService.HandleLargeTransactionAsync(
                    request.AccountId,
                    request.Amount,
                    "DEPOSIT",
                    request.TellerId
                );
            }

            // Build result
            result.Success = true;
            result.TransactionId = txnId;
            result.Reference = refNum;
            result.Narration = newTransaction.Narration;
            result.Amount = request.Amount;
            result.AppliedFees = fees;
            result.NetAmount = request.Amount - fees;
            result.NewBalance = account.Balance;
            result.Status = "POSTED";
            result.Message = $"Deposit successfully posted. Fees: GHS {fees:F2}";
            result.JournalLines = journalLines.Select(jl => new LedgerEntry
            {
                JournalId = jl.JournalId,
                GLCode = jl.AccountCode,
                Debit = jl.Debit,
                Credit = jl.Credit
            }).ToList();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.Status = "REJECTED";
            result.Message = ex.Message;
            
            // Audit failure
            try
            {
                await _auditLoggingService.LogActionAsync(
                    action: "DEPOSIT_FAILED",
                    entityType: "TRANSACTION",
                    entityId: null,
                    userId: request.TellerId,
                    description: $"Failed to post deposit of {request.Amount:C}",
                    status: "FAILED",
                    errorMessage: ex.Message
                );
            }
            catch { }
        }

        return result;
    }

    /// <summary>
    /// Posts a withdrawal with BOG compliance checks
    /// </summary>
    public async Task<LedgerPostingResult> PostWithdrawalAsync(WithdrawalRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        var result = new LedgerPostingResult();
        var journalLines = new List<JournalLine>();

        try
        {
            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null)
                throw new InvalidOperationException("Account not found");

            var customer = await _context.Customers.FindAsync(request.CustomerId);
            if (customer == null)
                throw new InvalidOperationException("Customer not found");

            // BOG Customer ID Verification
            await ValidateCustomerIdAsync(request.CustomerId, request.CustomerGhanaCard);

            // Check available balance
            var availableBalance = account.Balance - account.LienAmount;
            if (availableBalance < request.Amount)
                throw new InvalidOperationException($"Insufficient funds. Available: {availableBalance:C}");

            // KYC daily limit
            var kycLimit = await _kycService.GetKycLimitInfoAsync(request.CustomerId);
            await ValidateDailyLimitAsync(request.AccountId, request.Amount, kycLimit.DailyLimit);

            var txnId = GenerateTransactionId();
            var refNum = GenerateSecureReference();
            var fees = CalculateWithdrawalFees(request.WithdrawalMethod, request.Amount);

            var newTransaction = new Transaction
            {
                Id = txnId,
                AccountId = request.AccountId,
                Type = "WITHDRAWAL",
                Amount = request.Amount,
                Narration = $"{request.WithdrawalMethod} Withdrawal: {request.Narration}",
                TellerId = request.TellerId,
                Status = "POSTED",
                Reference = refNum,
                Date = DateTime.UtcNow
            };

            _context.Transactions.Add(newTransaction);

            account.Balance -= (request.Amount + fees);
            account.LastTransDate = DateTime.UtcNow;

            var withdrawalEvent = new FinancialEvent
            {
                EventType = EventTypes.WithdrawalPosted,
                EntityType = "ACCOUNT",
                EntityId = account.Id,
                Amount = request.Amount,
                Currency = account.Currency ?? "GHS",
                BranchId = request.BranchId,
                Reference = refNum,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { Method = request.WithdrawalMethod }),
                CreatedBy = request.TellerId
            };

            var postResult = await _postingEngine.ProcessEventAsync(withdrawalEvent);
            if (!postResult.Success)
            {
                throw new InvalidOperationException($"Posting Engine Failed: {postResult.ErrorMessage}");
            }

            if (fees > 0)
            {
                account.Balance -= fees;
                var feeEvent = new FinancialEvent
                {
                    EventType = EventTypes.ChargeApplied,
                    EntityType = "ACCOUNT",
                    EntityId = account.Id,
                    Amount = fees,
                    Currency = account.Currency ?? "GHS",
                    Reference = $"FEE-{refNum}",
                    PayloadJson = "{\"Reason\": \"Withdrawal Fee\"}",
                    CreatedBy = "SYSTEM"
                };
                await _postingEngine.ProcessEventAsync(feeEvent);
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLoggingService.LogActionAsync(
                action: "WITHDRAWAL_POSTED",
                entityType: "TRANSACTION",
                entityId: txnId,
                userId: request.TellerId,
                description: $"Withdrawal of {request.Amount:C} via {request.WithdrawalMethod}",
                status: "SUCCESS"
            );

            if (request.Amount > 10000)
            {
                await _suspiciousActivityService.HandleLargeTransactionAsync(
                    request.AccountId,
                    request.Amount,
                    "WITHDRAWAL",
                    request.TellerId
                );
            }

            result.Success = true;
            result.TransactionId = txnId;
            result.Reference = refNum;
            result.Amount = request.Amount;
            result.AppliedFees = fees;
            result.NetAmount = request.Amount + fees;
            result.NewBalance = account.Balance;
            result.Status = "POSTED";
            result.Message = $"Withdrawal successfully posted. Fees: GHS {fees:F2}";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.Status = "REJECTED";
            result.Message = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Posts an inter-account transfer
    /// </summary>
    public async Task<LedgerPostingResult> PostTransferAsync(TransferRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        var result = new LedgerPostingResult();
        var journalLines = new List<JournalLine>();

        try
        {
            var fromAccount = await _context.Accounts.FindAsync(request.FromAccountId);
            var toAccount = await _context.Accounts.FindAsync(request.ToAccountId);

            if (fromAccount == null || toAccount == null)
                throw new InvalidOperationException("One or both accounts not found");

            await ValidateCustomerIdAsync(request.CustomerId, request.CustomerGhanaCard);

            var availableBalance = fromAccount.Balance - fromAccount.LienAmount;
            if (availableBalance < request.Amount)
                throw new InvalidOperationException("Insufficient funds");

            var kycLimit = await _kycService.GetKycLimitInfoAsync(request.CustomerId);
            await ValidateDailyLimitAsync(request.FromAccountId, request.Amount, kycLimit.DailyLimit);

            var txnId = GenerateTransactionId();
            var refNum = GenerateSecureReference();
            var fees = CalculateTransferFees(request.Amount);

            var newTransaction = new Transaction
            {
                Id = txnId,
                AccountId = request.FromAccountId,
                Type = "TRANSFER",
                Amount = request.Amount,
                Narration = $"Transfer to {request.ToAccountId}: {request.Narration}",
                TellerId = request.TellerId,
                Status = "POSTED",
                Reference = refNum,
                Date = DateTime.UtcNow
            };

            _context.Transactions.Add(newTransaction);

            fromAccount.Balance -= (request.Amount + fees);
            toAccount.Balance += request.Amount;
            fromAccount.LastTransDate = DateTime.UtcNow;
            toAccount.LastTransDate = DateTime.UtcNow;

            var transferEvent = new FinancialEvent
            {
                EventType = EventTypes.TransferCompleted,
                EntityType = "ACCOUNT",
                EntityId = fromAccount.Id,
                Amount = request.Amount,
                Currency = fromAccount.Currency ?? "GHS",
                BranchId = request.TellerId, // Contextual branch info
                Reference = refNum,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { ToAccount = toAccount.Id }),
                CreatedBy = request.TellerId
            };

            var postResult = await _postingEngine.ProcessEventAsync(transferEvent);
            if (!postResult.Success)
            {
                throw new InvalidOperationException($"Posting Engine Failed: {postResult.ErrorMessage}");
            }

            if (fees > 0)
            {
                fromAccount.Balance -= fees;
                var feeEvent = new FinancialEvent
                {
                    EventType = EventTypes.ChargeApplied,
                    EntityType = "ACCOUNT",
                    EntityId = fromAccount.Id,
                    Amount = fees,
                    Currency = fromAccount.Currency ?? "GHS",
                    Reference = $"FEE-{refNum}",
                    PayloadJson = "{\"Reason\": \"Transfer Fee\"}",
                    CreatedBy = "SYSTEM"
                };
                await _postingEngine.ProcessEventAsync(feeEvent);
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.TransactionId = txnId;
            result.Reference = refNum;
            result.Amount = request.Amount;
            result.AppliedFees = fees;
            result.NewBalance = fromAccount.Balance;
            result.Status = "POSTED";
            result.Message = $"Transfer successfully posted to {request.ToAccountId}";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.Status = "REJECTED";
            result.Message = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Posts a cheque transaction (clearing)
    /// </summary>
    public async Task<LedgerPostingResult> PostChequeAsync(ChequeRequest request)
    {
        if (request.TransactionType == "DEPOSIT")
            return await PostDepositAsync(new DepositRequest
            {
                AccountId = request.AccountId,
                CustomerId = request.CustomerId,
                Amount = request.Amount,
                DepositMethod = "CHEQUE",
                ChequeNumber = request.ChequeNumber,
                ChequeBank = request.ChequeBank,
                Narration = $"Cheque #{request.ChequeNumber} from {request.ChequeBank}",
                TellerId = request.TellerId,
                BranchId = request.BranchId
            });
        else
            return await PostWithdrawalAsync(new WithdrawalRequest
            {
                AccountId = request.AccountId,
                CustomerId = request.CustomerId,
                Amount = request.Amount,
                WithdrawalMethod = "CHEQUE",
                ChequeNumber = request.ChequeNumber,
                Narration = $"Cheque #{request.ChequeNumber}",
                TellerId = request.TellerId,
                BranchId = request.BranchId
            });
    }

    /// <summary>
    /// Retrieves ledger entries for an account
    /// </summary>
    public async Task<List<LedgerEntry>> GetLedgerEntriesAsync(string accountId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = from t in _context.Transactions
                    join jl in _context.JournalLines on t.Id equals jl.JournalId into jLoin
                    from jl in jLoin.DefaultIfEmpty()
                    where t.AccountId == accountId
                    select new { t, jl };

        if (fromDate.HasValue)
            query = query.Where(x => x.t.Date >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(x => x.t.Date <= toDate.Value);

        var entries = await query
            .OrderByDescending(x => x.t.Date)
            .Select(x => new LedgerEntry
            {
                Id = x.t.Id,
                JournalId = x.jl.JournalId,
                GLCode = x.jl.AccountCode,
                Debit = x.jl.Debit,
                Credit = x.jl.Credit,
                Narration = x.t.Narration,
                PostedDate = x.t.Date
            })
            .ToListAsync();

        return entries;
    }

    /// <summary>
    /// Gets available credit margin for a customer (for lending decisions)
    /// </summary>
    public async Task<decimal> GetAvailableMarginAsync(string customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            return 0m;

        // Base margin based on KYC level (in GHS)
        var baseMargin = customer.KycLevel switch
        {
            "TIER1" => 10000m,
            "TIER2" => 50000m,
            "TIER3" => 500000m,
            _ => 5000m
        };

        // Adjust based on risk rating
        var riskAdjustment = customer.RiskRating switch
        {
            "Low" => 1.0m,
            "Medium" => 0.75m,
            "High" => 0.5m,
            _ => 0.75m
        };

        // Get existing loan exposure
        var existingLoans = await _context.Loans
            .Where(l => l.CustomerId == customerId && l.Status == "ACTIVE")
            .SumAsync(l => l.OutstandingBalance ?? 0m);

        var margin = (baseMargin * riskAdjustment) - existingLoans;
        return Math.Max(0m, margin);
    }

    /// <summary>
    /// Gets account balance and daily transaction totals
    /// </summary>
    public async Task<LedgerBalance> GetAccountBalanceAsync(string accountId)
    {
        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null)
            throw new InvalidOperationException("Account not found");

        var today = DateTime.UtcNow.Date;
        var dailyTransactions = await _context.Transactions
            .Where(t => t.AccountId == accountId && t.Date.Date == today)
            .ToListAsync();

        return new LedgerBalance
        {
            AccountId = accountId,
            Balance = account.Balance,
            LienAmount = account.LienAmount,
            DailyDebitTotal = dailyTransactions
                .Where(t => t.Type == "WITHDRAWAL" || t.Type == "TRANSFER")
                .Sum(t => t.Amount),
            DailyCreditTotal = dailyTransactions
                .Where(t => t.Type == "DEPOSIT")
                .Sum(t => t.Amount)
        };
    }

    // --- PRIVATE HELPER METHODS ---

    private async Task ValidateCustomerIdAsync(string customerId, string ghanaCardNumber)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException("Customer ID not found");

        // BOG requires customer ID verification for all transactions
        if (customer.GhanaCard != ghanaCardNumber)
            throw new InvalidOperationException("Ghana Card number does not match customer records. Transaction blocked for compliance.");
    }

    private async Task ValidateDailyLimitAsync(string accountId, decimal amount, decimal dailyLimit)
    {
        if (dailyLimit == decimal.MaxValue)
            return; // Tier 3: Unlimited

        var today = DateTime.UtcNow.Date;
        var todayTotal = await _context.Transactions
            .Where(t => t.AccountId == accountId
                     && t.Date.Date == today
                     && (t.Type == "DEPOSIT" || t.Type == "WITHDRAWAL"))
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        if (todayTotal + amount > dailyLimit)
            throw new InvalidOperationException($"Daily transaction limit exceeded. Limit: {dailyLimit:C}, Today's total: {todayTotal:C}");
    }

    private decimal CalculateDepositFees(string method, decimal amount)
    {
        return method switch
        {
            "CHEQUE" => Math.Max(50m, amount * 0.001m), // Min GHS 50 or 0.1%, whichever is greater
            _ => 0m // Cash deposits are free
        };
    }

    private decimal CalculateWithdrawalFees(string method, decimal amount)
    {
        return method switch
        {
            "CHEQUE" => 100m, // Fixed cheque issuance fee
            _ => amount >= 5000m ? amount * 0.005m : 0m // Large cash withdrawals: 0.5%
        };
    }

    private decimal CalculateTransferFees(decimal amount)
    {
        return amount switch
        {
            >= 100000m => amount * 0.008m,   // 0.8% for large transfers
            >= 10000m => amount * 0.005m,   // 0.5% for medium transfers
            _ => 0m                         // Free for small transfers
        };
    }

    private static string GenerateTransactionId() => $"TXN{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    private static string GenerateSecureReference()
    {
        Span<byte> randomBytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(randomBytes);
        var token = Convert.ToHexString(randomBytes);
        return $"LEG-{DateTime.UtcNow:yyyyMMddHHmmss}-{token}";
    }
}
