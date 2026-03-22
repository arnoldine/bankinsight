using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class AccountService
{
    private readonly ApplicationDbContext _context;
    private readonly ISequenceGeneratorService _sequenceService;

    public AccountService(ApplicationDbContext context, ISequenceGeneratorService sequenceService)
    {
        _context = context;
        _sequenceService = sequenceService;
    }

    public async Task<List<Account>> GetAccountsAsync()
    {
        return await _context.Accounts.ToListAsync();
    }

    public async Task<Account?> GetAccountByIdAsync(string id)
    {
        return await _context.Accounts.FindAsync(id);
    }

    public async Task<List<Account>> GetAccountsByCustomerIdAsync(string customerId)
    {
        return await _context.Accounts
            .Where(a => a.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<Account> CreateAccountAsync(CreateAccountRequest request)
    {
        var normalizedBranchId = NormalizeBranchId(request.BranchId);
        var branchCode = ExtractBranchCode(normalizedBranchId);
        var productCode = ExtractProductPrefix(request.ProductCode, request.Type);

        var prefix = $"{branchCode}{productCode}";
        var sequenceNumber = await _sequenceService.GetNextSequenceAsync($"CASA-{prefix}");

        var baseNumber = $"{prefix}{sequenceNumber:D6}";
        var checkDigit = _sequenceService.CalculateLuhnCheckDigit(baseNumber);
        var id = $"{baseNumber}{checkDigit}";

        var account = new Account
        {
            Id = id,
            CustomerId = request.CustomerId,
            BranchId = normalizedBranchId,
            Type = request.Type,
            Currency = string.IsNullOrEmpty(request.Currency) ? "GHS" : request.Currency,
            Balance = 0,
            LienAmount = 0,
            Status = "ACTIVE",
            ProductCode = request.ProductCode,
            LastTransDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return account;
    }

    private static string NormalizeBranchId(string? branchId)
    {
        if (string.IsNullOrWhiteSpace(branchId))
        {
            return "BR001";
        }

        var trimmed = branchId.Trim().ToUpperInvariant();
        if (trimmed.StartsWith("BR", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits))
        {
            return "BR001";
        }

        return $"BR{digits.PadLeft(3, '0')}";
    }

    private static string ExtractBranchCode(string branchId)
    {
        var digits = new string(branchId.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits))
        {
            return "001";
        }

        return digits.Length > 3 ? digits.Substring(0, 3) : digits.PadLeft(3, '0');
    }

    private static string ExtractProductPrefix(string? productCode, string? accountType)
    {
        var digits = string.IsNullOrWhiteSpace(productCode)
            ? string.Empty
            : new string(productCode.Where(char.IsDigit).ToArray());

        if (digits.Length >= 2)
        {
            return digits.Substring(0, 2);
        }

        if (digits.Length == 1)
        {
            return digits.PadLeft(2, '0');
        }

        return accountType?.Trim().ToUpperInvariant() switch
        {
            "CURRENT" => "20",
            "FIXED_DEPOSIT" => "30",
            _ => "10"
        };
    }
}
