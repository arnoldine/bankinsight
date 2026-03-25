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
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Account name is required.");
        }

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

        var duplicateExists = await _context.GlAccounts.AnyAsync(account => account.Code == code);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"GL account code {code} already exists.");
        }

        var glAccount = new GlAccount
        {
            Code = code,
            Name = name,
            Category = categoryName,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "GHS" : request.Currency.Trim().ToUpperInvariant(),
            Balance = 0,
            IsHeader = request.IsHeader
        };

        _context.GlAccounts.Add(glAccount);
        await _context.SaveChangesAsync();

        return glAccount;
    }

    public async Task<List<JournalEntryResponseDto>> GetJournalEntriesAsync()
    {
        return await _context.JournalEntries
            .AsNoTracking()
            .Include(j => j.Lines)
            .ThenInclude(line => line.Account)
            .OrderByDescending(j => j.CreatedAt)
            .Take(100)
            .Select(journal => new JournalEntryResponseDto
            {
                Id = journal.Id,
                Date = journal.Date ?? default,
                Reference = journal.Reference,
                Description = journal.Description,
                PostedBy = journal.PostedBy,
                Status = journal.Status,
                CreatedAt = journal.CreatedAt,
                TotalDebit = journal.Lines.Sum(line => line.Debit),
                TotalCredit = journal.Lines.Sum(line => line.Credit),
                Lines = journal.Lines
                    .OrderBy(line => line.Id)
                    .Select(line => new JournalLineResponseDto
                    {
                        Id = line.Id,
                        JournalId = line.JournalId ?? string.Empty,
                        AccountCode = line.AccountCode ?? string.Empty,
                        AccountName = line.Account != null ? line.Account.Name : string.Empty,
                        Debit = line.Debit,
                        Credit = line.Credit,
                    })
                    .ToList(),
            })
            .ToListAsync();
    }

    public async Task<JournalEntryResponseDto> PostJournalEntryAsync(PostJournalEntryRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new InvalidOperationException("Journal description is required.");
            }

            if (request.Lines.Count < 2)
            {
                throw new InvalidOperationException("Journal entries must contain at least two lines.");
            }

            if (request.Lines.Any(line => string.IsNullOrWhiteSpace(line.AccountCode)))
            {
                throw new InvalidOperationException("Every journal line must include an account code.");
            }

            if (request.Lines.Any(line => line.Debit < 0 || line.Credit < 0))
            {
                throw new InvalidOperationException("Debit and credit amounts cannot be negative.");
            }

            if (request.Lines.Any(line => (line.Debit > 0 && line.Credit > 0) || (line.Debit <= 0 && line.Credit <= 0)))
            {
                throw new InvalidOperationException("Each journal line must contain either a debit or a credit amount.");
            }

            decimal totalDebit = request.Lines.Sum(l => l.Debit);
            decimal totalCredit = request.Lines.Sum(l => l.Credit);

            if (totalDebit != totalCredit)
            {
                throw new InvalidOperationException("Debits and Credits must balance.");
            }

            var requestedCodes = request.Lines
                .Select(line => line.AccountCode.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();
            var accountLookup = await _context.GlAccounts
                .Where(account => requestedCodes.Contains(account.Code))
                .ToDictionaryAsync(account => account.Code, account => account);

            var missingCodes = requestedCodes.Where(code => !accountLookup.ContainsKey(code)).ToList();
            if (missingCodes.Count > 0)
            {
                throw new InvalidOperationException($"Unknown GL account code(s): {string.Join(", ", missingCodes)}.");
            }

            var headerCodes = requestedCodes.Where(code => accountLookup[code].IsHeader).ToList();
            if (headerCodes.Count > 0)
            {
                throw new InvalidOperationException($"Header GL account(s) cannot be posted to directly: {string.Join(", ", headerCodes)}.");
            }

            var journalId = $"JRN-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var journalEntry = new JournalEntry
            {
                Id = journalId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
                Description = request.Description.Trim(),
                PostedBy = string.IsNullOrWhiteSpace(request.PostedBy) ? null : request.PostedBy.Trim(),
                Status = "POSTED",
                CreatedAt = DateTime.UtcNow
            };

            _context.JournalEntries.Add(journalEntry);

            foreach (var line in request.Lines)
            {
                var normalizedAccountCode = line.AccountCode.Trim().ToUpperInvariant();
                var journalLine = new JournalLine
                {
                    JournalId = journalId,
                    AccountCode = normalizedAccountCode,
                    Debit = line.Debit,
                    Credit = line.Credit
                };

                _context.JournalLines.Add(journalLine);

                var glAccount = accountLookup[normalizedAccountCode];
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

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new JournalEntryResponseDto
            {
                Id = journalEntry.Id,
                Date = journalEntry.Date ?? default,
                Reference = journalEntry.Reference,
                Description = journalEntry.Description,
                PostedBy = journalEntry.PostedBy,
                Status = journalEntry.Status,
                CreatedAt = journalEntry.CreatedAt,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                Lines = request.Lines.Select((line, index) =>
                {
                    var account = accountLookup[line.AccountCode.Trim().ToUpperInvariant()];
                    return new JournalLineResponseDto
                    {
                        Id = index + 1,
                        JournalId = journalEntry.Id,
                        AccountCode = account.Code,
                        AccountName = account.Name,
                        Debit = line.Debit,
                        Credit = line.Credit,
                    };
                }).ToList(),
            };
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
