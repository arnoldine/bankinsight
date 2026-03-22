using System;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IKycService
{
    Task<decimal> GetTransactionLimitAsync(string customerId);
    Task<string> GetKycLevelAsync(string customerId);
    Task<bool> ValidateTransactionAmountAsync(string customerId, decimal amount);
    Task<KycLimitInfo> GetKycLimitInfoAsync(string customerId);
}

public class KycLimitInfo
{
    public string KycLevel { get; set; } = string.Empty;
    public decimal TransactionLimit { get; set; }
    public decimal DailyLimit { get; set; }
    public bool IsUnlimited { get; set; }
}

public class KycService : IKycService
{
    private readonly ApplicationDbContext _context;

    // Define KYC limits as per Bank of Ghana requirements
    private const decimal TIER1_LIMIT = 1000m; // GHS 1,000
    private const decimal TIER2_LIMIT = 10000m; // GHS 10,000
    private const decimal TIER3_LIMIT = decimal.MaxValue; // Unlimited

    public KycService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetKycLevelAsync(string customerId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        return NormalizeKycLevel(customer.KycLevel);
    }

    public async Task<decimal> GetTransactionLimitAsync(string customerId)
    {
        var kycLevel = await GetKycLevelAsync(customerId);

        return kycLevel switch
        {
            "TIER1" => TIER1_LIMIT,
            "TIER2" => TIER2_LIMIT,
            "TIER3" => TIER3_LIMIT,
            _ => TIER1_LIMIT
        };
    }

    public async Task<bool> ValidateTransactionAmountAsync(string customerId, decimal amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        var limit = await GetTransactionLimitAsync(customerId);
        return amount <= limit;
    }

    public async Task<KycLimitInfo> GetKycLimitInfoAsync(string customerId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        var kycLevel = NormalizeKycLevel(customer.KycLevel);
        var (transactionLimit, dailyLimit, isUnlimited) = kycLevel switch
        {
            "TIER1" => (TIER1_LIMIT, TIER1_LIMIT * 5, false),
            "TIER2" => (TIER2_LIMIT, TIER2_LIMIT * 5, false),
            "TIER3" => (TIER3_LIMIT, TIER3_LIMIT, true),
            _ => (TIER1_LIMIT, TIER1_LIMIT * 5, false)
        };

        return new KycLimitInfo
        {
            KycLevel = kycLevel,
            TransactionLimit = transactionLimit,
            DailyLimit = dailyLimit,
            IsUnlimited = isUnlimited
        };
    }

    private static string NormalizeKycLevel(string? rawLevel)
    {
        if (string.IsNullOrWhiteSpace(rawLevel))
        {
            return "TIER1";
        }

        var normalized = rawLevel.Trim().ToUpperInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
        return normalized switch
        {
            "TIER1" => "TIER1",
            "TIER2" => "TIER2",
            "TIER3" => "TIER3",
            _ => "TIER1"
        };
    }
}
