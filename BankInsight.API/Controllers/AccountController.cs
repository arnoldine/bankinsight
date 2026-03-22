using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    [RequirePermission("VIEW_ACCOUNTS")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _accountService.GetAccountsAsync();
        return Ok(accounts);
    }

    [HttpGet("{id}")]
    [RequirePermission("VIEW_ACCOUNTS")]
    public async Task<IActionResult> GetAccountById(string id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);
        if (account == null) return NotFound(new { message = "Account not found" });
        return Ok(account);
    }

    [HttpGet("customer/{cif}")]
    [RequirePermission("VIEW_ACCOUNTS")]
    public async Task<IActionResult> GetAccountsByCustomerId(string cif)
    {
        var accounts = await _accountService.GetAccountsByCustomerIdAsync(cif);
        return Ok(accounts);
    }

    [HttpPost]
    [RequirePermission("CREATE_ACCOUNTS")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var account = await _accountService.CreateAccountAsync(request);
        return StatusCode(201, account);
    }
}
