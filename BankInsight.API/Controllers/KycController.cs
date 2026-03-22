using System;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/kyc")]
[Authorize]
public class KycController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IKycService _kycService;

    public KycController(ApplicationDbContext context, IKycService kycService)
    {
        _context = context;
        _kycService = kycService;
    }

    [HttpGet("limits/{customerId}")]
    [HasPermission(AppPermissions.Customers.View)]
    public async Task<ActionResult<CustomerKycStatusResponse>> GetKycLimits(string customerId)
    {
        var response = await BuildCustomerKycResponseAsync(customerId);
        return Ok(response);
    }

    [HttpGet("daily-limit/{customerId}")]
    [HasPermission(AppPermissions.Customers.View)]
    public async Task<IActionResult> GetDailyLimit(string customerId)
    {
        var response = await BuildCustomerKycResponseAsync(customerId);
        return Ok(new { dailyLimit = response.DailyLimit });
    }

    [HttpPost("validate-ghana-card")]
    [HasPermission(AppPermissions.Customers.View)]
    public async Task<ActionResult<ValidateGhanaCardResponse>> ValidateGhanaCard([FromBody] ValidateGhanaCardRequest request)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId);
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found" });
        }

        var normalizedRequest = NormalizeIdentifier(request.GhanaCardNumber);
        var normalizedProfile = NormalizeIdentifier(customer.GhanaCard);

        return Ok(new ValidateGhanaCardResponse
        {
            IsValid = !string.IsNullOrWhiteSpace(normalizedProfile)
                && string.Equals(normalizedRequest, normalizedProfile, StringComparison.OrdinalIgnoreCase)
        });
    }

    internal async Task<CustomerKycStatusResponse> BuildCustomerKycResponseAsync(string customerId)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        var limits = await _kycService.GetKycLimitInfoAsync(customerId);
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var todayPostedTotal = await _context.Transactions
            .Where(t => t.Status == "POSTED"
                        && t.Date >= todayStart
                        && t.Date < todayEnd
                        && t.Account != null
                        && t.Account.CustomerId == customerId)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var remainingDailyLimit = limits.IsUnlimited
            ? limits.DailyLimit
            : Math.Max(0m, limits.DailyLimit - todayPostedTotal);

        return new CustomerKycStatusResponse
        {
            CustomerId = customerId,
            KycLevel = limits.KycLevel,
            TransactionLimit = limits.TransactionLimit,
            DailyLimit = limits.DailyLimit,
            RemainingDailyLimit = remainingDailyLimit,
            IsUnlimited = limits.IsUnlimited,
            GhanaCardMatchesProfile = !string.IsNullOrWhiteSpace(customer.GhanaCard),
            TodayPostedTotal = todayPostedTotal
        };
    }

    private static string NormalizeIdentifier(string? value)
    {
        return (value ?? string.Empty).Trim().Replace(" ", string.Empty).ToUpperInvariant();
    }
}
