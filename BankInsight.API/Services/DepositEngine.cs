using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankInsight.API.Services;

public interface IDepositEngine
{
    Task RunDailyAccrualJobAsync();
    Task RunMonthlyCapitalizationJobAsync();
}

public class DepositEngine : IDepositEngine
{
    private readonly ApplicationDbContext _context;
    private readonly IPostingEngine _postingEngine;
    private readonly ILogger<DepositEngine> _logger;

    public DepositEngine(ApplicationDbContext context, IPostingEngine postingEngine, ILogger<DepositEngine> logger)
    {
        _context = context;
        _postingEngine = postingEngine;
        _logger = logger;
    }

    public async Task RunDailyAccrualJobAsync()
    {
        _logger.LogInformation("Starting Daily Deposit Accrual Job");

        // Simple Implementation: Process accounts with > 0 balance
        // Ideally, filtering by Product settings (Interest Bearing = true)
        var accounts = await _context.Accounts
            .Where(a => a.Balance > 0 && a.Status == "ACTIVE")
            .ToListAsync();

        foreach (var account in accounts)
        {
            // Dummy logic: Base rate 5% APY
            decimal annualRate = 0.05m;
            decimal dailyAccrual = account.Balance * annualRate / 365m;

            if (dailyAccrual <= 0) continue;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var accrualEvent = new FinancialEvent
                {
                    EventType = EventTypes.InterestAccrued,
                    EntityType = "Account",
                    EntityId = account.Id,
                    Amount = dailyAccrual,
                    Currency = account.Currency,
                    Reference = $"ACCRUAL-{DateTime.UtcNow:yyyyMMdd}-{account.Id}",
                    PayloadJson = JsonSerializer.Serialize(new { Rate = annualRate, SourceBalance = account.Balance }),
                    CreatedBy = "SYSTEM"
                };

                // The Posting Engine will convert this event into Dr/Cr Journal Lines
                var result = await _postingEngine.ProcessEventAsync(accrualEvent);

                if (result.Success)
                {
                    // Increment the unposted accrual tracking on the account record (New field needed ideally, substituting Balance for now)
                    // If we strictly follow the new architecture, the accrued interest is strictly derived from the GL
                    await transaction.CommitAsync();
                }
                else
                {
                    _logger.LogWarning("Failed to post accrual for Account {AccountId}: {Error}", account.Id, result.ErrorMessage);
                    await transaction.RollbackAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception running daily accrual for {AccountId}", account.Id);
                await transaction.RollbackAsync();
            }
        }

        _logger.LogInformation("Finished Daily Deposit Accrual Job");
    }

    public async Task RunMonthlyCapitalizationJobAsync()
    {
        _logger.LogInformation("Starting Monthly Deposit Capitalization Job");

        var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var targetMonthStart = currentMonthStart.AddMonths(-1);
        var targetMonthEnd = currentMonthStart;

        var accruals = await _context.FinancialEvents
            .AsNoTracking()
            .Where(e =>
                e.EventType == EventTypes.InterestAccrued &&
                e.EntityType == "Account" &&
                e.CreatedAt >= targetMonthStart &&
                e.CreatedAt < targetMonthEnd)
            .GroupBy(e => new { e.EntityId, e.Currency, e.BranchId })
            .Select(g => new
            {
                AccountId = g.Key.EntityId,
                Currency = g.Key.Currency,
                BranchId = g.Key.BranchId,
                Amount = g.Sum(e => e.Amount)
            })
            .ToListAsync();

        foreach (var accrual in accruals.Where(a => a.Amount > 0))
        {
            var capitalizationReference = $"CAPITALIZE-{targetMonthStart:yyyyMM}-{accrual.AccountId}";

            var alreadyCapitalized = await _context.FinancialEvents
                .AnyAsync(e =>
                    e.EventType == EventTypes.InterestPosted &&
                    e.EntityType == "Account" &&
                    e.EntityId == accrual.AccountId &&
                    e.Reference == capitalizationReference);

            if (alreadyCapitalized)
            {
                continue;
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accrual.AccountId);
            if (account == null || account.Status != "ACTIVE")
            {
                continue;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                account.Balance += accrual.Amount;
                account.LastTransDate = DateTime.UtcNow;

                _context.Transactions.Add(new Transaction
                {
                    Id = $"TXN-CAP-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
                    AccountId = account.Id,
                    Type = "INTEREST_POSTED",
                    Amount = accrual.Amount,
                    Narration = $"Monthly interest capitalization for {targetMonthStart:MMMM yyyy}",
                    Date = DateTime.UtcNow,
                    Reference = capitalizationReference,
                    Status = "POSTED"
                });

                _context.FinancialEvents.Add(new FinancialEvent
                {
                    EventType = EventTypes.InterestPosted,
                    EntityType = "Account",
                    EntityId = account.Id,
                    Amount = accrual.Amount,
                    Currency = account.Currency,
                    BranchId = account.BranchId ?? accrual.BranchId,
                    Reference = capitalizationReference,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        Month = targetMonthStart.ToString("yyyy-MM"),
                        AccruedAmount = accrual.Amount
                    }),
                    CreatedBy = "SYSTEM"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capitalize deposit interest for account {AccountId}", accrual.AccountId);
                await transaction.RollbackAsync();
            }
        }

        _logger.LogInformation("Finished Monthly Deposit Capitalization Job");
    }
}
