using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/deposits")]
[Authorize]
public class DepositController : ControllerBase
{
    private const string FixedDepositAccountType = "FIXED_DEPOSIT";
    private readonly ApplicationDbContext _context;

    public DepositController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all fixed deposits
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FixedDepositDto>>> GetFixedDeposits()
    {
        try
        {
            var deposits = await _context.Accounts
                .Where(a => a.Type == FixedDepositAccountType)
                .Select(a => new FixedDepositDto
                {
                    Id = a.Id,
                    CustomerId = a.CustomerId ?? string.Empty,
                    AccountId = a.Id,
                    Principal = a.Balance,
                    Rate = 0,
                    Tenure = 90, // Default tenure in days (hardcoded for demo)
                    StartDate = a.CreatedAt.ToString("yyyy-MM-dd"),
                    MaturityDate = a.CreatedAt.AddDays(90).ToString("yyyy-MM-dd"),
                    Currency = a.Currency ?? string.Empty,
                    InterestPaymentFrequency = "AT_MATURITY",
                    Status = a.Status == "ACTIVE" ? "ACTIVE" : "CLOSED",
                    AccruedInterest = 0,
                    MaturityValue = a.Balance
                })
                .ToListAsync();

            return Ok(deposits);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error retrieving deposits: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get a specific fixed deposit
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FixedDepositDto>> GetFixedDeposit(string id)
    {
        try
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.Type == FixedDepositAccountType);
            if (account == null)
            {
                return NotFound(new { message = "Fixed deposit not found" });
            }

            var deposit = new FixedDepositDto
            {
                Id = account.Id,
                CustomerId = account.CustomerId ?? string.Empty,
                AccountId = account.Id,
                Principal = account.Balance,
                Rate = 0,
                Tenure = 90,
                StartDate = account.CreatedAt.ToString("yyyy-MM-dd"),
                MaturityDate = account.CreatedAt.AddDays(90).ToString("yyyy-MM-dd"),
                Currency = account.Currency ?? string.Empty,
                InterestPaymentFrequency = "AT_MATURITY",
                Status = account.Status == "ACTIVE" ? "ACTIVE" : "CLOSED",
                AccruedInterest = 0,
                MaturityValue = account.Balance
            };

            return Ok(deposit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error retrieving deposit: {ex.Message}" });
        }
    }

    /// <summary>
    /// Create a new fixed deposit
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FixedDepositDto>> CreateFixedDeposit([FromBody] CreateFixedDepositRequest request)
    {
        try
        {
            // Validate customer exists
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId);
            if (customer == null)
            {
                return BadRequest(new { message = "Customer not found" });
            }

            // Create new fixed deposit account
            var account = new Account
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = request.CustomerId,
                Type = FixedDepositAccountType,
                ProductCode = request.AccountId,
                Currency = request.Currency,
                Balance = request.Principal,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var maturityDate = DateTime.UtcNow.AddDays(request.Tenure);
            var deposit = new FixedDepositDto
            {
                Id = account.Id,
                CustomerId = account.CustomerId ?? string.Empty,
                AccountId = account.Id,
                Principal = account.Balance,
                Rate = request.Rate,
                Tenure = request.Tenure,
                StartDate = account.CreatedAt.ToString("yyyy-MM-dd"),
                MaturityDate = maturityDate.ToString("yyyy-MM-dd"),
                Currency = account.Currency ?? string.Empty,
                InterestPaymentFrequency = request.InterestPaymentFrequency,
                Status = "ACTIVE",
                AccruedInterest = 0,
                MaturityValue = request.Principal + (request.Principal * request.Rate / 100 * (request.Tenure / 365m))
            };

            return CreatedAtAction(nameof(GetFixedDeposit), new { id = account.Id }, deposit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error creating deposit: {ex.Message}" });
        }
    }

    /// <summary>
    /// Renew a fixed deposit
    /// </summary>
    [HttpPost("{id}/renew")]
    public async Task<ActionResult<FixedDepositDto>> RenewFixedDeposit(string id, [FromBody] RenewDepositRequest request)
    {
        try
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.Type == FixedDepositAccountType);
            if (account == null)
            {
                return NotFound(new { message = "Fixed deposit not found" });
            }

            // Update the account with new principal and tenure
            account.Balance = request.Principal > 0 ? request.Principal : account.Balance;

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();

            var maturityDate = DateTime.UtcNow.AddDays(request.Tenure);
            var deposit = new FixedDepositDto
            {
                Id = account.Id,
                CustomerId = account.CustomerId ?? string.Empty,
                AccountId = account.Id,
                Principal = account.Balance,
                Rate = 0,
                Tenure = request.Tenure,
                StartDate = account.CreatedAt.ToString("yyyy-MM-dd"),
                MaturityDate = maturityDate.ToString("yyyy-MM-dd"),
                Currency = account.Currency ?? string.Empty,
                InterestPaymentFrequency = "AT_MATURITY",
                Status = "ACTIVE",
                AccruedInterest = 0,
                MaturityValue = account.Balance
            };

            return Ok(deposit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error renewing deposit: {ex.Message}" });
        }
    }

    /// <summary>
    /// Close a fixed deposit
    /// </summary>
    [HttpPost("{id}/close")]
    public async Task<ActionResult<CloseDepositResponse>> CloseFixedDeposit(string id)
    {
        try
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.Type == FixedDepositAccountType);
            if (account == null)
            {
                return NotFound(new { message = "Fixed deposit not found" });
            }

            var priorBalance = account.Balance;
            var accruedInterest = 0m;
            var finalAmount = priorBalance + accruedInterest;

            // Mark as closed
            account.Status = "CLOSED";

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();

            return Ok(new CloseDepositResponse
            {
                Message = "Fixed deposit closed successfully",
                FinalAmount = finalAmount
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error closing deposit: {ex.Message}" });
        }
    }
}

public class FixedDepositDto
{
    public string Id { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string AccountId { get; set; } = null!;
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public int Tenure { get; set; } // Days
    public string StartDate { get; set; } = null!;
    public string MaturityDate { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public string InterestPaymentFrequency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal AccruedInterest { get; set; }
    public decimal MaturityValue { get; set; }
}

public class CreateFixedDepositRequest
{
    public string CustomerId { get; set; } = null!;
    public string AccountId { get; set; } = null!;
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public int Tenure { get; set; }
    public string Currency { get; set; } = null!;
    public string InterestPaymentFrequency { get; set; } = null!;
}

public class RenewDepositRequest
{
    public decimal Principal { get; set; }
    public int Tenure { get; set; }
}

public class CloseDepositResponse
{
    public string Message { get; set; } = null!;
    public decimal FinalAmount { get; set; }
}
