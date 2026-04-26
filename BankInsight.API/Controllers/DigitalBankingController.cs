using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/digital-banking")]
public class DigitalBankingController : ControllerBase
{
    private readonly DigitalBankingService _service;

    public DigitalBankingController(DigitalBankingService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _service.GetDashboardAsync(cancellationToken));

    [HttpGet("savings/products")]
    public async Task<IActionResult> GetSavingsProducts(CancellationToken cancellationToken)
        => Ok(await _service.GetSavingsProductsAsync(cancellationToken));

    [HttpGet("savings/accounts/{customerId}")]
    public async Task<IActionResult> GetSavingsAccounts(string customerId, CancellationToken cancellationToken)
        => Ok(await _service.GetCustomerSavingsAccountsAsync(customerId, cancellationToken));

    [HttpPost("savings/accounts")]
    public async Task<IActionResult> OpenSavingsAccount([FromBody] OpenDigitalSavingsAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return StatusCode(201, await _service.OpenSavingsAccountAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("savings/accounts/{accountId}/fund")]
    public async Task<IActionResult> FundSavingsAccount(string accountId, [FromBody] DigitalSavingsTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.FundSavingsAccountAsync(accountId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("savings/accounts/{accountId}/withdraw")]
    public async Task<IActionResult> WithdrawSavingsAccount(string accountId, [FromBody] DigitalSavingsTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.WithdrawSavingsAccountAsync(accountId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("investments/portfolio")]
    public async Task<IActionResult> GetInvestmentPortfolio([FromQuery] string? customerId, CancellationToken cancellationToken)
        => Ok(await _service.GetInvestmentPortfolioAsync(customerId, cancellationToken));

    [HttpPost("investments")]
    public async Task<IActionResult> CreateInvestment([FromBody] CreateDigitalInvestmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return StatusCode(201, await _service.CreateInvestmentAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("investments/{profileId}/top-up")]
    public async Task<IActionResult> TopUpInvestment(string profileId, [FromBody] DigitalInvestmentActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.TopUpInvestmentAsync(profileId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("investments/{profileId}/rollover")]
    public async Task<IActionResult> RolloverInvestment(string profileId, [FromBody] DigitalInvestmentActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RolloverInvestmentAsync(profileId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("investments/{profileId}/liquidate")]
    public async Task<IActionResult> LiquidateInvestment(string profileId, [FromBody] DigitalInvestmentActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.LiquidateInvestmentAsync(profileId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("loans/eligibility")]
    public async Task<IActionResult> CheckLoanEligibility([FromBody] CheckDigitalLoanEligibilityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.CheckLoanEligibilityAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("loans/apply")]
    public async Task<IActionResult> ApplyLoan([FromBody] CreateDigitalLoanApplicationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return StatusCode(201, await _service.ApplyLoanAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("loans/{loanId}/repay")]
    public async Task<IActionResult> RepayLoan(string loanId, [FromBody] LoanRepayRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RepayLoanAsync(loanId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("loans/restructure")]
    public async Task<IActionResult> RestructureLoan([FromBody] LoanRestructureRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RestructureLoanAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("loans/{loanId}/statement")]
    public async Task<IActionResult> GetLoanStatement(string loanId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetLoanStatementAsync(loanId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("loans/{loanId}/schedule")]
    public async Task<IActionResult> GetLoanSchedule(string loanId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetLoanScheduleAsync(loanId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
