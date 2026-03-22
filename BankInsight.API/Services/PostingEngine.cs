using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankInsight.API.Services;

public class PostingEngine : IPostingEngine
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostingEngine> _logger;

    public PostingEngine(ApplicationDbContext context, ILogger<PostingEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PostingResult> ProcessEventAsync(FinancialEvent financialEvent)
    {
        try
        {
            var rules = await _context.PostingRules
                .Where(r => r.EventType == financialEvent.EventType && r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ToListAsync();

            if (!rules.Any())
            {
                return new PostingResult
                {
                    Success = false,
                    ErrorMessage = $"No active posting rule found for EventType: {financialEvent.EventType}"
                };
            }

            var rule = rules.First();
            var journalId = $"JRN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var postedBy = await ResolvePostedByAsync(financialEvent.CreatedBy);

            var journalEntry = new JournalEntry
            {
                Id = journalId,
                Date = DateOnly.FromDateTime(financialEvent.CreatedAt),
                Reference = financialEvent.Reference ?? financialEvent.Id.ToString(),
                Description = $"System Post: {rule.Description ?? financialEvent.EventType}",
                PostedBy = postedBy,
                Status = "POSTED",
                CreatedAt = DateTime.UtcNow
            };

            var lines = new List<JournalLine>
            {
                new JournalLine
                {
                    JournalId = journalId,
                    AccountCode = ResolveGLCode(rule.DebitAccountCode, financialEvent),
                    Debit = financialEvent.Amount,
                    Credit = 0
                },
                new JournalLine
                {
                    JournalId = journalId,
                    AccountCode = ResolveGLCode(rule.CreditAccountCode, financialEvent),
                    Debit = 0,
                    Credit = financialEvent.Amount
                }
            };

            _context.FinancialEvents.Add(financialEvent);
            _context.JournalEntries.Add(journalEntry);
            _context.JournalLines.AddRange(lines);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Event {EventType} processed successfully into Journal {JournalId}", financialEvent.EventType, journalId);

            return new PostingResult { Success = true, JournalEntryId = journalId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FinancialEvent {EventId}", financialEvent.Id);
            return new PostingResult { Success = false, ErrorMessage = ex.Message };
        }
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

    private string ResolveGLCode(string codeTemplate, FinancialEvent financialEvent)
    {
        return codeTemplate;
    }
}
