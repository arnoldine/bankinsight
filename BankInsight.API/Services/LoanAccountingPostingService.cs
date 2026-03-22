using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public enum LoanAccountingEventType
{
    Disbursement,
    InterestAccrual,
    PenaltyAccrual,
    Repayment,
    Impairment,
    WriteOff,
    Recovery,
    ProcessingFee
}

public class LoanPostingResult
{
    public string JournalId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public LoanAccountingEventType EventType { get; set; }
    public decimal Amount { get; set; }
}

public interface ILoanAccountingPostingService
{
    Task<LoanPostingResult> PostEventAsync(Loan loan, LoanAccountingEventType eventType, decimal amount, string? userId = null, string? description = null);
    Task<List<JournalEntry>> GetLoanPostingsAsync(string loanId);
}

public class LoanAccountingPostingService : ILoanAccountingPostingService
{
    private readonly ApplicationDbContext _context;

    public LoanAccountingPostingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LoanPostingResult> PostEventAsync(Loan loan, LoanAccountingEventType eventType, decimal amount, string? userId = null, string? description = null)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Posting amount must be greater than zero.");
        }

        var profile = await ResolveProfileAsync(loan);
        var reference = $"LN-{loan.Id}-{eventType}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var journalId = $"JE{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var (debitCode, creditCode) = ResolveGlPair(profile, eventType);

        var debit = await _context.GlAccounts.FirstOrDefaultAsync(a => a.Code == debitCode)
            ?? throw new InvalidOperationException($"GL account not found: {debitCode}");

        var credit = await _context.GlAccounts.FirstOrDefaultAsync(a => a.Code == creditCode)
            ?? throw new InvalidOperationException($"GL account not found: {creditCode}");

        var journal = new JournalEntry
        {
            Id = journalId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Reference = reference,
            Description = description ?? $"Loan {eventType} posting for {loan.Id}",
            PostedBy = await ResolvePostedByAsync(userId),
            Status = "POSTED",
            CreatedAt = DateTime.UtcNow
        };

        var lines = new List<JournalLine>
        {
            new()
            {
                JournalId = journalId,
                AccountCode = debitCode,
                Debit = amount,
                Credit = 0m
            },
            new()
            {
                JournalId = journalId,
                AccountCode = creditCode,
                Debit = 0m,
                Credit = amount
            }
        };

        debit.Balance += amount;
        credit.Balance -= amount;

        _context.JournalEntries.Add(journal);
        _context.JournalLines.AddRange(lines);
        await _context.SaveChangesAsync();

        return new LoanPostingResult
        {
            JournalId = journalId,
            Reference = reference,
            EventType = eventType,
            Amount = amount
        };
    }

    public async Task<List<JournalEntry>> GetLoanPostingsAsync(string loanId)
    {
        return await _context.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.Reference != null && j.Reference.Contains($"LN-{loanId}"))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    private async Task<LoanAccountingProfile> ResolveProfileAsync(Loan loan)
    {
        if (!string.IsNullOrWhiteSpace(loan.LoanProductId))
        {
            var byProduct = await _context.LoanAccountingProfiles
                .FirstOrDefaultAsync(p => p.LoanProductId == loan.LoanProductId && p.IsActive);

            if (byProduct != null)
            {
                return byProduct;
            }
        }

        var fallback = await _context.LoanAccountingProfiles.FirstOrDefaultAsync(p => p.IsActive);
        if (fallback == null)
        {
            throw new InvalidOperationException("No active loan accounting profile is configured.");
        }

        return fallback;
    }

    private async Task<string?> ResolvePostedByAsync(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var trimmed = candidate.Trim();
        if (trimmed.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var exists = await _context.Staff.AnyAsync(staff => staff.Id == trimmed);
        return exists ? trimmed : null;
    }

    private static (string debitCode, string creditCode) ResolveGlPair(LoanAccountingProfile profile, LoanAccountingEventType eventType)
    {
        return eventType switch
        {
            LoanAccountingEventType.Disbursement => (profile.LoanPortfolioGl, profile.DisbursementFundingGl),
            LoanAccountingEventType.InterestAccrual => (profile.InterestReceivableGl, profile.InterestIncomeGl),
            LoanAccountingEventType.PenaltyAccrual => (profile.PenaltyReceivableGl, profile.PenaltyIncomeGl),
            LoanAccountingEventType.Repayment => (profile.DisbursementFundingGl, profile.LoanPortfolioGl),
            LoanAccountingEventType.Impairment => (profile.ImpairmentExpenseGl, profile.ImpairmentAllowanceGl),
            LoanAccountingEventType.WriteOff => (profile.ImpairmentAllowanceGl, profile.LoanPortfolioGl),
            LoanAccountingEventType.Recovery => (profile.DisbursementFundingGl, profile.RecoveryIncomeGl),
            LoanAccountingEventType.ProcessingFee => (profile.DisbursementFundingGl, profile.ProcessingFeeIncomeGl),
            _ => throw new InvalidOperationException("Unsupported loan accounting event")
        };
    }
}
