using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

/// <summary>
/// Ledger Controller: RESTful API endpoints for Teller transaction posting
/// All endpoints require BOG-compliant customer ID verification
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LedgerController : ControllerBase
{
    private readonly ILedgerEngine _ledgerEngine;
    private readonly ILogger<LedgerController> _logger;

    public LedgerController(ILedgerEngine ledgerEngine, ILogger<LedgerController> logger)
    {
        _ledgerEngine = ledgerEngine;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/ledger/deposits - Post a cash or cheque deposit
    /// </summary>
    [HttpPost("deposits")]
    [ProducesResponseType(typeof(LedgerPostingResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LedgerPostingResult>> PostDeposit([FromBody] DepositRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Deposit request: Account={AccountId}, Amount={Amount}, Method={Method}",
                request.AccountId, request.Amount, request.DepositMethod);

            var result = await _ledgerEngine.PostDepositAsync(request);

            if (!result.Success)
                return BadRequest(new ErrorResponse { Message = result.Message });

            return CreatedAtAction(nameof(PostDeposit), result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Deposit validation failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing the deposit" });
        }
    }

    /// <summary>
    /// POST /api/ledger/withdrawals - Post a cash or cheque withdrawal
    /// </summary>
    [HttpPost("withdrawals")]
    [ProducesResponseType(typeof(LedgerPostingResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LedgerPostingResult>> PostWithdrawal([FromBody] WithdrawalRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Withdrawal request: Account={AccountId}, Amount={Amount}, Method={Method}",
                request.AccountId, request.Amount, request.WithdrawalMethod);

            var result = await _ledgerEngine.PostWithdrawalAsync(request);

            if (!result.Success)
                return BadRequest(new ErrorResponse { Message = result.Message });

            return CreatedAtAction(nameof(PostWithdrawal), result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Withdrawal validation failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing withdrawal");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing the withdrawal" });
        }
    }

    /// <summary>
    /// POST /api/ledger/transfers - Post an inter-account transfer
    /// </summary>
    [HttpPost("transfers")]
    [ProducesResponseType(typeof(LedgerPostingResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LedgerPostingResult>> PostTransfer([FromBody] TransferRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Transfer request: From={FromAccount}, To={ToAccount}, Amount={Amount}",
                request.FromAccountId, request.ToAccountId, request.Amount);

            var result = await _ledgerEngine.PostTransferAsync(request);

            if (!result.Success)
                return BadRequest(new ErrorResponse { Message = result.Message });

            return CreatedAtAction(nameof(PostTransfer), result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Transfer validation failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transfer");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing the transfer" });
        }
    }

    /// <summary>
    /// POST /api/ledger/cheques - Post a cheque transaction
    /// </summary>
    [HttpPost("cheques")]
    [ProducesResponseType(typeof(LedgerPostingResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LedgerPostingResult>> PostCheque([FromBody] ChequeRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Cheque transaction: Account={AccountId}, Cheque={ChequeNumber}, Amount={Amount}",
                request.AccountId, request.ChequeNumber, request.Amount);

            var result = await _ledgerEngine.PostChequeAsync(request);

            if (!result.Success)
                return BadRequest(new ErrorResponse { Message = result.Message });

            return CreatedAtAction(nameof(PostCheque), result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cheque validation failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cheque");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing the cheque" });
        }
    }

    /// <summary>
    /// GET /api/ledger/ledger - Get ledger entries for an account
    /// </summary>
    [HttpGet("ledger")]
    [ProducesResponseType(typeof(List<LedgerEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<LedgerEntry>>> GetLedgerEntries(
        [FromQuery] string accountId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return BadRequest(new ErrorResponse { Message = "accountId is required" });

            var entries = await _ledgerEngine.GetLedgerEntriesAsync(accountId, fromDate, toDate);
            return Ok(entries);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Ledger fetch failed: {Message}", ex.Message);
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ledger entries");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while fetching ledger entries" });
        }
    }

    /// <summary>
    /// GET /api/ledger/balance/{accountId} - Get account balance and daily totals
    /// </summary>
    [HttpGet("balance/{accountId}")]
    [ProducesResponseType(typeof(LedgerBalance), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerBalance>> GetAccountBalance(string accountId)
    {
        try
        {
            var balance = await _ledgerEngine.GetAccountBalanceAsync(accountId);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Balance fetch failed: {Message}", ex.Message);
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching account balance");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while fetching account balance" });
        }
    }

    /// <summary>
    /// GET /api/ledger/margins/{customerId} - Get available credit margin for a customer
    /// </summary>
    [HttpGet("margins/{customerId}")]
    [ProducesResponseType(typeof(AvailableMarginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailableMarginResponse>> GetAvailableMargin(string customerId)
    {
        try
        {
            var margin = await _ledgerEngine.GetAvailableMarginAsync(customerId);
            return Ok(new AvailableMarginResponse { AvailableMargin = margin });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available margin");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while fetching available margin" });
        }
    }

    /// <summary>
    /// Helper response models
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
    }

    public class AvailableMarginResponse
    {
        public decimal AvailableMargin { get; set; }
    }
}
