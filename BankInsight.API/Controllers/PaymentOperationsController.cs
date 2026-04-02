using System;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/payments")]
public class PaymentOperationsController : ControllerBase
{
    private readonly PaymentOperationsService _paymentOperationsService;

    public PaymentOperationsController(PaymentOperationsService paymentOperationsService)
    {
        _paymentOperationsService = paymentOperationsService;
    }

    [HttpGet("bulk")]
    [HasPermission(AppPermissions.Transactions.View)]
    public async Task<IActionResult> GetBulkBatches()
    {
        return Ok(await _paymentOperationsService.GetBulkPaymentBatchesAsync());
    }

    [HttpGet("bulk/{batchId}")]
    [HasPermission(AppPermissions.Transactions.View)]
    public async Task<IActionResult> GetBulkBatch(string batchId)
    {
        var batch = await _paymentOperationsService.GetBulkPaymentBatchAsync(batchId);
        return batch == null ? NotFound(new { message = "Bulk payment batch not found" }) : Ok(batch);
    }

    [HttpPost("bulk")]
    [HasPermission(AppPermissions.Transactions.Post)]
    public async Task<IActionResult> CreateBulkBatch([FromBody] CreateBulkPaymentBatchRequest request)
    {
        try
        {
            var batch = await _paymentOperationsService.CreateBulkPaymentBatchAsync(request);
            return StatusCode(201, batch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("cheques")]
    [HasPermission(AppPermissions.Transactions.View)]
    public async Task<IActionResult> GetCheques()
    {
        return Ok(await _paymentOperationsService.GetChequeItemsAsync());
    }

    [HttpGet("cheques/{itemId}")]
    [HasPermission(AppPermissions.Transactions.View)]
    public async Task<IActionResult> GetCheque(string itemId)
    {
        var cheque = await _paymentOperationsService.GetChequeItemAsync(itemId);
        return cheque == null ? NotFound(new { message = "Cheque item not found" }) : Ok(cheque);
    }

    [HttpPost("cheques/deposits")]
    [HasPermission(AppPermissions.Transactions.Post)]
    public async Task<IActionResult> LodgeChequeDeposit([FromBody] LodgeChequeDepositRequest request)
    {
        try
        {
            var item = await _paymentOperationsService.LodgeChequeDepositAsync(request);
            return StatusCode(201, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cheques/withdrawals")]
    [HasPermission(AppPermissions.Transactions.Post)]
    public async Task<IActionResult> ProcessChequeWithdrawal([FromBody] ChequeWithdrawalRequest request)
    {
        try
        {
            var item = await _paymentOperationsService.ProcessChequeWithdrawalAsync(request);
            return StatusCode(201, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cheques/{itemId}/return")]
    [HasPermission(AppPermissions.Transactions.Approve)]
    public async Task<IActionResult> ReturnCheque(string itemId, [FromBody] ReturnChequeRequest request)
    {
        try
        {
            var item = await _paymentOperationsService.ReturnChequeAsync(itemId, request.Reason, User?.Identity?.Name);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("cheque-books")]
    [HasPermission(AppPermissions.Transactions.View)]
    public async Task<IActionResult> GetChequeBooks([FromQuery] string? accountId = null)
    {
        return Ok(await _paymentOperationsService.GetChequeBooksAsync(accountId));
    }

    [HttpGet("cheque-books/{bookId}")]
    [HasPermission(AppPermissions.Transactions.View)]
    public async Task<IActionResult> GetChequeBook(string bookId)
    {
        var book = await _paymentOperationsService.GetChequeBookAsync(bookId);
        return book == null ? NotFound(new { message = "Cheque book not found" }) : Ok(book);
    }

    [HttpPost("cheque-books/stock")]
    [HasPermission(AppPermissions.Transactions.Post)]
    public async Task<IActionResult> CreateChequeBookStock([FromBody] CreateChequeBookStockRequest request)
    {
        try
        {
            var book = await _paymentOperationsService.CreateChequeBookStockAsync(request);
            return StatusCode(201, book);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cheque-books/{bookId}/issue")]
    [HasPermission(AppPermissions.Transactions.Post)]
    public async Task<IActionResult> IssueChequeBook(string bookId, [FromBody] IssueChequeBookRequest request)
    {
        try
        {
            var book = await _paymentOperationsService.IssueChequeBookAsync(bookId, request);
            return Ok(book);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cheque-books/leaves/{leafId}/cancel")]
    [HasPermission(AppPermissions.Transactions.Post)]
    public async Task<IActionResult> CancelChequeLeaf(string leafId, [FromBody] CancelChequeLeafRequest request)
    {
        try
        {
            var book = await _paymentOperationsService.CancelChequeLeafAsync(leafId, request.Reason, User?.Identity?.Name);
            return Ok(book);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cheque-books/leaves/use-history")]
    [HasPermission(AppPermissions.Transactions.Post)]
    public async Task<IActionResult> MarkChequeLeafUsed([FromBody] MarkChequeLeafUsedRequest request)
    {
        try
        {
            var book = await _paymentOperationsService.MarkChequeLeafUsedHistoricallyAsync(request, User?.Identity?.Name);
            return Ok(book);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
