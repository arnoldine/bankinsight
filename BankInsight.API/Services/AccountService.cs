using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class AccountService
{
    private readonly ApplicationDbContext _context;
    private readonly ISequenceGeneratorService _sequenceService;
    private readonly ICurrentUserContext _currentUser;
    private readonly CustomerService _customerService;

    public AccountService(ApplicationDbContext context, ISequenceGeneratorService sequenceService, ICurrentUserContext currentUser, CustomerService customerService)
    {
        _context = context;
        _sequenceService = sequenceService;
        _currentUser = currentUser;
        _customerService = customerService;
    }

    public async Task<List<Account>> GetAccountsAsync()
    {
        return await ApplyVisibilityScope(_context.Accounts.AsQueryable())
            .AsNoTracking()
            .OrderBy(a => a.Id)
            .ToListAsync();
    }

    public async Task<PagedResultDto<AccountListItemDto>> GetAccountsPageAsync(int pageNumber, int pageSize, string? search, string? type)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);

        var query = ApplyVisibilityScope(_context.Accounts.AsQueryable())
            .AsNoTracking()
            .Include(a => a.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.Id.Contains(term) ||
                (a.CustomerId != null && a.CustomerId.Contains(term)) ||
                (a.BranchId != null && a.BranchId.Contains(term)) ||
                (a.ProductCode != null && a.ProductCode.Contains(term)) ||
                (a.Customer != null && a.Customer.Name.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToUpperInvariant();
            query = query.Where(a => a.Type.ToUpper() == normalizedType);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Id)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(a => new AccountListItemDto
            {
                Id = a.Id,
                CustomerId = a.CustomerId ?? string.Empty,
                CustomerName = a.Customer != null ? a.Customer.Name : (a.CustomerId ?? string.Empty),
                BranchId = a.BranchId ?? "BR001",
                Type = a.Type,
                Currency = a.Currency,
                Balance = a.Balance,
                LienAmount = a.LienAmount,
                Status = a.Status,
                ProductCode = a.ProductCode,
                LastTransDate = a.LastTransDate.HasValue ? a.LastTransDate.Value.ToString("O") : null,
                CreatedAt = a.CreatedAt.ToString("O")
            })
            .ToListAsync();

        return new PagedResultDto<AccountListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize
        };
    }

    public async Task<Account?> GetAccountByIdAsync(string id)
    {
        return await ApplyVisibilityScope(_context.Accounts.AsQueryable())
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Account>> GetAccountsByCustomerIdAsync(string customerId)
    {
        return await ApplyVisibilityScope(_context.Accounts.AsQueryable())
            .Where(a => a.CustomerId == customerId)
            .OrderBy(a => a.Id)
            .ToListAsync();
    }

    public async Task<Account> CreateAccountAsync(CreateAccountRequest request)
    {
        var readiness = await _customerService.GetCustomerKycReadinessAsync(request.CustomerId);
        if (readiness == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        if (!readiness.IsReadyForAccountOpening)
        {
            throw new InvalidOperationException($"Customer is not KYC-ready for account opening. Missing: {string.Join(", ", readiness.MissingRequirements)}");
        }

        var normalizedBranchId = NormalizeBranchId(request.BranchId);
        var branchCode = ExtractBranchCode(normalizedBranchId);
        var productCode = ExtractProductPrefix(request.ProductCode, request.Type);

        var prefix = $"{branchCode}{productCode}";
        var sequenceNumber = await _sequenceService.GetNextSequenceAsync($"CASA-{prefix}");

        var baseNumber = $"{prefix}{sequenceNumber:D6}";
        var checkDigit = _sequenceService.CalculateLuhnCheckDigit(baseNumber);
        var id = $"{baseNumber}{checkDigit}";
        var ownerStaffId = await ResolveOwnerStaffIdAsync(request.OwnerStaffId, request.IsConfidential);

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
            IsConfidential = request.IsConfidential,
            OwnerStaffId = ownerStaffId,
            LastTransDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return account;
    }

    private IQueryable<Account> ApplyVisibilityScope(IQueryable<Account> query)
    {
        if (_currentUser.ScopeType == AccessScopeType.BranchOnly && !string.IsNullOrWhiteSpace(_currentUser.BranchId))
        {
            query = query.Where(a => a.BranchId == _currentUser.BranchId);
        }

        if (!CanViewConfidentialRecords() && !string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            var userId = _currentUser.UserId;
            query = query.Where(a => !a.IsConfidential || a.OwnerStaffId == userId);
        }

        return query;
    }

    private bool CanViewConfidentialRecords()
    {
        return _currentUser.HasPermission(AppPermissions.Accounts.ViewConfidential)
            || _currentUser.HasPermission(AppPermissions.Users.Manage);
    }

    private async Task<string?> ResolveOwnerStaffIdAsync(string? requestedOwnerStaffId, bool isConfidential)
    {
        if (!isConfidential)
        {
            return null;
        }

        var candidate = string.IsNullOrWhiteSpace(requestedOwnerStaffId)
            ? _currentUser.UserId
            : requestedOwnerStaffId.Trim();

        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException("Confidential accounts must have an owning staff member.");
        }

        var resolved = await _context.Staff
            .Where(s => s.Id == candidate || s.Email == candidate)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException("The specified confidential account owner was not found.");
        }

        return resolved;
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
