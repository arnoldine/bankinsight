using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using BankInsight.API.Security;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/transactions")]
public class TransactionController : ControllerBase
{
    private readonly TransactionService _transactionService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(TransactionService transactionService, ILogger<TransactionController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    [HttpGet]
    [HasPermission(BankInsight.API.Security.AppPermissions.Transactions.View)]
    public async Task<IActionResult> GetTransactions()
    {
        var transactions = await _transactionService.GetTransactionsAsync();
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Transactions.View)]
    public async Task<IActionResult> GetTransactionById(string id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound(new { message = "Transaction not found" });
        return Ok(transaction);
    }

    [HttpPost]
    [HasPermission(BankInsight.API.Security.AppPermissions.Transactions.Post)]
    public async Task<IActionResult> PostTransaction([FromBody] CreateTransactionRequest request)
    {
        try
        {
            var transaction = await _transactionService.PostTransactionAsync(request);
            return StatusCode(201, transaction);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while posting transaction for account {AccountId}", request.AccountId);
            return StatusCode(500, new { message = "Unexpected error while posting transaction" });
        }
    }
}
