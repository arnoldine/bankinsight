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
        
        // This job would sum up all un-capitalized InterestAccrued events for the month
        // and issue an InterestPosted event to modify the underlying Customer Balance directly.
        // Left as an architectural stub based on the requirements diagram.
        await Task.CompletedTask;
    }
}
