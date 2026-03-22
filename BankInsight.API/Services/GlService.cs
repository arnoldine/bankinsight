using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class GlService
{
    private readonly ApplicationDbContext _context;
    private readonly ISequenceGeneratorService _sequenceService;

    public GlService(ApplicationDbContext context, ISequenceGeneratorService sequenceService)
    {
        _context = context;
        _sequenceService = sequenceService;
    }

    public async Task<List<GlAccount>> GetGlAccountsAsync()
    {
        return await _context.GlAccounts
            .Select(g => new GlAccount
            {
                Code = g.Code,
                Name = g.Name,
                Category = g.Category,
                Currency = g.Currency,
                Balance = g.Balance,
                IsHeader = g.IsHeader
            })
            .OrderBy(g => g.Code)
            .ToListAsync();
    }

    public async Task<SeedChartOfAccountsResponse> SeedRegulatoryChartOfAccountsAsync(string regionCode = RegulatoryChartOfAccountsCatalog.GhanaRegionCode)
    {
        var chartAccounts = RegulatoryChartOfAccountsCatalog.GetAccounts(regionCode);
        if (!chartAccounts.Any())
        {
            throw new InvalidOperationException($"No regulatory chart of accounts is configured for region '{regionCode}'.");
        }

        var existingCodes = await _context.GlAccounts.Select(a => a.Code).ToListAsync();
        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var toInsert = chartAccounts.Where(account => !existingSet.Contains(account.Code)).ToList();

        if (toInsert.Any())
        {
            _context.GlAccounts.AddRange(toInsert);
            await _context.SaveChangesAsync();
        }

        return new SeedChartOfAccountsResponse
        {
            RegionCode = regionCode.ToUpperInvariant(),
            StandardName = RegulatoryChartOfAccountsCatalog.GetStandardName(regionCode),
            InsertedCount = toInsert.Count,
            ExistingCount = chartAccounts.Count - toInsert.Count,
            TotalStandardAccounts = chartAccounts.Count,
            InsertedCodes = toInsert.Select(account => account.Code).OrderBy(code => code).ToList(),
        };
    }

    public async Task<GlAccount> CreateGlAccountAsync(CreateGlAccountRequest request)
    {
        var categoryName = string.IsNullOrEmpty(request.Category) ? "ASSET" : request.Category.ToUpper();
        string classDigit = categoryName switch
        {
            "ASSET" => "1",
            "LIABILITY" => "2",
            "EQUITY" => "3",
            "REVENUE" => "4",
            "INCOME" => "4",
            "EXPENSE" => "5",
            _ => "1"
        };
        var categoryCode = "01";
        var prefix = $"{classDigit}-{categoryCode}";

        var seq = await _sequenceService.GetNextSequenceAsync($"GL-{prefix}");
        var code = string.IsNullOrWhiteSpace(request.Code) ? $"{prefix}-{seq:D4}" : request.Code.Trim().ToUpperInvariant();

        var glAccount = new GlAccount
        {
            Code = code,
            Name = request.Name,
            Category = categoryName,
            Currency = string.IsNullOrEmpty(request.Currency) ? "GHS" : request.Currency,
            Balance = 0,
            IsHeader = request.IsHeader
        };

        _context.GlAccounts.Add(glAccount);
        await _context.SaveChangesAsync();

        return glAccount;
    }

    public async Task<List<JournalEntry>> GetJournalEntriesAsync()
    {
        return await _context.JournalEntries
            .OrderByDescending(j => j.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<JournalEntry> PostJournalEntryAsync(PostJournalEntryRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            decimal totalDebit = request.Lines.Sum(l => l.Debit);
            decimal totalCredit = request.Lines.Sum(l => l.Credit);

            if (totalDebit != totalCredit)
            {
                throw new InvalidOperationException("Debits and Credits must balance.");
            }

            var journalId = $"JRN-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var journalEntry = new JournalEntry
            {
                Id = journalId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Reference = request.Reference,
                Description = request.Description,
                PostedBy = request.PostedBy,
                Status = "POSTED",
                CreatedAt = DateTime.UtcNow
            };

            _context.JournalEntries.Add(journalEntry);

            foreach (var line in request.Lines)
            {
                var journalLine = new JournalLine
                {
                    JournalId = journalId,
                    AccountCode = line.AccountCode,
                    Debit = line.Debit,
                    Credit = line.Credit
                };

                _context.JournalLines.Add(journalLine);

                var glAccount = await _context.GlAccounts.FindAsync(line.AccountCode);
                if (glAccount != null)
                {
                    decimal balanceChange = 0;
                    if (glAccount.Category == "ASSET" || glAccount.Category == "EXPENSE")
                    {
                        balanceChange = line.Debit - line.Credit;
                    }
                    else
                    {
                        balanceChange = line.Credit - line.Debit;
                    }

                    glAccount.Balance += balanceChange;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return journalEntry;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
