using System;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using BankInsight.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly CustomerService _customerService;
    private readonly ApplicationDbContext _context;
    private readonly IKycService _kycService;

    public CustomerController(CustomerService customerService, ApplicationDbContext context, IKycService kycService)
    {
        _customerService = customerService;
        _context = context;
        _kycService = kycService;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Customers.View)]
    public async Task<IActionResult> GetCustomers()
    {
        var customers = await _customerService.GetCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Customers.View)]
    public async Task<IActionResult> GetCustomerById(string id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null) return NotFound(new { message = "Customer not found" });
        return Ok(customer);
    }

    [HttpGet("{id}/profile")]
    [HasPermission(AppPermissions.Customers.View)]
    public async Task<IActionResult> GetCustomerProfile(string id)
    {
        var profile = await _customerService.GetCustomerProfileAsync(id);
        if (profile == null) return NotFound(new { message = "Customer not found" });
        return Ok(profile);
    }

    [HttpGet("{id}/kyc")]
    [HasPermission(AppPermissions.Customers.View)]
    public async Task<IActionResult> GetCustomerKyc(string id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found" });
        }

        var limits = await _kycService.GetKycLimitInfoAsync(id);
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var todayPostedTotal = await _context.Transactions
            .Where(t => t.Status == "POSTED"
                        && t.Date >= todayStart
                        && t.Date < todayEnd
                        && t.Account != null
                        && t.Account.CustomerId == id)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return Ok(new CustomerKycStatusResponse
        {
            CustomerId = id,
            KycLevel = limits.KycLevel,
            TransactionLimit = limits.TransactionLimit,
            DailyLimit = limits.DailyLimit,
            RemainingDailyLimit = limits.IsUnlimited ? limits.DailyLimit : Math.Max(0m, limits.DailyLimit - todayPostedTotal),
            IsUnlimited = limits.IsUnlimited,
            GhanaCardMatchesProfile = !string.IsNullOrWhiteSpace(customer.GhanaCard),
            TodayPostedTotal = todayPostedTotal
        });
    }

    [HttpPost]
    [HasPermission(AppPermissions.Customers.Create)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var customer = await _customerService.CreateCustomerAsync(request);
        return StatusCode(201, customer);
    }

    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Customers.Edit)]
    public async Task<IActionResult> UpdateCustomer(string id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _customerService.UpdateCustomerAsync(id, request);
        if (customer == null) return NotFound(new { message = "Customer not found" });
        return Ok(customer);
    }

    [HttpPost("{id}/notes")]
    [HasPermission(AppPermissions.Customers.Edit)]
    public async Task<IActionResult> AddCustomerNote(string id, [FromBody] CreateCustomerNoteRequest request)
    {
        var note = await _customerService.AddCustomerNoteAsync(id, request);
        if (note == null) return NotFound(new { message = "Customer not found" });
        return Ok(note);
    }

    [HttpPost("{id}/documents")]
    [HasPermission(AppPermissions.Customers.Edit)]
    public async Task<IActionResult> AddCustomerDocument(string id, [FromBody] CreateCustomerDocumentRequest request)
    {
        var document = await _customerService.AddCustomerDocumentAsync(id, request);
        if (document == null) return NotFound(new { message = "Customer not found" });
        return Ok(document);
    }
}
