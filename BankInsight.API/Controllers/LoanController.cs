using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using BankInsight.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/loans")]
public class LoanController : ControllerBase
{
    private readonly LoanService _loanService;
    private readonly ILogger<LoanController> _logger;

    public LoanController(LoanService loanService, ILogger<LoanController> logger)
    {
        _loanService = loanService;
        _logger = logger;
    }

    [HttpGet]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public async Task<IActionResult> GetLoans()
    {
        var loans = await _loanService.GetLoansAsync();
        return Ok(loans);
    }

    [HttpPost("disburse")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Disburse)]
    public async Task<IActionResult> DisburseLoan([FromBody] DisburseLoanRequest request)
    {
        try
        {
            var loan = !string.IsNullOrWhiteSpace(request.LoanId)
                ? await _loanService.DisburseApprovedLoanAsync(new DisburseApprovedLoanRequest
                {
                    LoanId = request.LoanId,
                    ClientReference = request.ClientReference
                })
                : await _loanService.DisburseLoanAsync(request);
            return StatusCode(201, loan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected loan disbursement failure for customer {Cif}", request.Cif);
            return StatusCode(500, new { message = "Unexpected error while disbursing loan" });
        }
    }

    [HttpPost("apply")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Disburse)]
    public async Task<IActionResult> ApplyLoan([FromBody] ApplyLoanRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            var loan = await _loanService.ApplyLoanAsync(request, userId);
            return StatusCode(201, loan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected loan application failure for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, new { message = "Unexpected error while applying for loan" });
        }
    }

    [HttpPost("approve")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Approve)]
    public async Task<IActionResult> ApproveLoan([FromBody] ApproveLoanRequest request)
    {
        try
        {
            var checkerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            var loan = await _loanService.ApproveLoanAsync(request, checkerId);
            return Ok(loan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected loan approval failure for loan {LoanId}", request.LoanId);
            return StatusCode(500, new { message = "Unexpected error while approving loan" });
        }
    }

    [HttpPost("repay")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Transactions.Post)]
    public async Task<IActionResult> RepayLoan([FromBody] RepayLoanRequest request)
    {
        try
        {
            var loan = await _loanService.RepayLoanByBodyAsync(request);
            return Ok(loan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected loan repayment failure for loan {LoanId}", request.LoanId);
            return StatusCode(500, new { message = "Unexpected error while processing repayment" });
        }
    }

    [HttpPost("check-credit")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public async Task<IActionResult> CheckCredit([FromBody] CheckCreditRequest request)
    {
        try
        {
            var result = await _loanService.CheckCreditAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected credit check failure for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, new { message = "Unexpected error while checking credit" });
        }
    }

    [HttpPost("generate-schedule")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public async Task<IActionResult> GenerateSchedule([FromBody] GenerateLoanScheduleRequest request)
    {
        try
        {
            var schedule = await _loanService.GenerateScheduleAsync(request);
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected schedule generation failure");
            return StatusCode(500, new { message = "Unexpected error while generating schedule" });
        }
    }

    [HttpPost("products/configure")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Roles.Manage)]
    public async Task<IActionResult> ConfigureLoanProduct([FromBody] ConfigureLoanProductRequest request)
    {
        var result = await _loanService.ConfigureLoanProductAsync(request);
        return Ok(result);
    }

    [HttpGet("products")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public async Task<IActionResult> GetLoanProducts()
    {
        var result = await _loanService.GetLoanProductsAsync();
        return Ok(result);
    }

    [HttpPost("accounting-profiles/configure")]
    [HasPermission(BankInsight.API.Security.AppPermissions.GeneralLedger.Post)]
    public async Task<IActionResult> ConfigureLoanAccountingProfile([FromBody] ConfigureLoanAccountingProfileRequest request)
    {
        var result = await _loanService.ConfigureLoanAccountingProfileAsync(request);
        return Ok(result);
    }

    [HttpPost("appraise")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Approve)]
    public async Task<IActionResult> AppraiseLoan([FromBody] AppraiseLoanRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var result = await _loanService.AppraiseLoanAsync(request, userId);
        return Ok(result);
    }

    [HttpPost("restructure")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Edit)]
    public async Task<IActionResult> RestructureLoan([FromBody] LoanRestructureRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var result = await _loanService.RestructureLoanAsync(request, userId);
        return Ok(result);
    }

    [HttpPost("repay/reverse")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Transactions.Post)]
    public async Task<IActionResult> ReverseRepayment([FromBody] LoanRepaymentReversalRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var result = await _loanService.ReverseRepaymentAsync(request, userId);
        return Ok(result);
    }

    [HttpPost("accruals/process")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Edit)]
    public async Task<IActionResult> ProcessAccrualBatch([FromBody] LoanAccrualBatchRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var result = await _loanService.ProcessAccrualBatchAsync(request, userId);
        return Ok(result);
    }

    [HttpPost("writeoff")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Edit)]
    public async Task<IActionResult> WriteOffLoan([FromBody] LoanWriteOffRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var result = await _loanService.WriteOffLoanAsync(request, userId);
        return Ok(result);
    }

    [HttpPost("recover")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Edit)]
    public async Task<IActionResult> RecoverLoan([FromBody] LoanRecoveryRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var result = await _loanService.RecoverWrittenOffLoanAsync(request, userId);
        return Ok(result);
    }

    [HttpGet("{id}/statement")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public async Task<IActionResult> GetLoanStatement(string id)
    {
        var result = await _loanService.GetLoanStatementAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/gl-postings")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Audit.View)]
    public async Task<IActionResult> GetLoanGlPostings(string id)
    {
        var result = await _loanService.GetLoanGlPostingsAsync(id);
        return Ok(result);
    }

    [HttpGet("dashboards/delinquency")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public async Task<IActionResult> GetDelinquencyDashboard()
    {
        var result = await _loanService.GetDelinquencyDashboardAsync();
        return Ok(result);
    }

    [HttpGet("reports/profitability")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Reports.View)]
    public async Task<IActionResult> GetProfitabilityReport([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var from = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _loanService.GetLoanProfitabilityReportAsync(from, to);
        return Ok(result);
    }

    [HttpGet("reports/balance-sheet")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Reports.View)]
    public async Task<IActionResult> GetLoanBalanceSheetReport([FromQuery] DateOnly? asOfDate)
    {
        var result = await _loanService.GetLoanBalanceSheetReportAsync(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow));
        return Ok(result);
    }

    [HttpGet("credit-bureau/providers")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public IActionResult GetCreditBureauProviders()
    {
        var result = _loanService.GetCreditBureauProviders();
        return Ok(result);
    }

    [HttpGet("{id}/schedule")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public async Task<IActionResult> GetLoanSchedule(string id)
    {
        var schedule = await _loanService.GetLoanScheduleAsync(id);
        return Ok(schedule);
    }

    [HttpGet("{id}/accrual")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]
    public async Task<IActionResult> GetLoanAccrual(string id)
    {
        try
        {
            var accrual = await _loanService.GetLoanAccrualSnapshotAsync(id);
            return Ok(accrual);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected accrual lookup failure for loan {LoanId}", id);
            return StatusCode(500, new { message = "Unexpected error while fetching accrual snapshot" });
        }
    }

    [HttpPost("{id}/repay")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Transactions.Post)]
    public async Task<IActionResult> RepayLoan(string id, [FromBody] LoanRepayRequest request)
    {
        try
        {
            var loan = await _loanService.RepayLoanAsync(id, request);
            return Ok(loan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected loan repayment failure for loan {LoanId}", id);
            return StatusCode(500, new { message = "Unexpected error while processing loan repayment" });
        }
    }

    [HttpPost("{id}/penalty")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Edit)]
    public async Task<IActionResult> AssessPenalty(string id, [FromBody] AssessLoanPenaltyRequest request)
    {
        try
        {
            var penalty = await _loanService.AssessPenaltyAsync(id, request);
            return StatusCode(201, penalty);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected penalty assessment failure for loan {LoanId}", id);
            return StatusCode(500, new { message = "Unexpected error while assessing penalty" });
        }
    }

    [HttpPost("{id}/classify")]
    [HasPermission(BankInsight.API.Security.AppPermissions.Loans.Edit)]
    public async Task<IActionResult> ClassifyAndProvision(string id)
    {
        try
        {
            var classification = await _loanService.ClassifyAndProvisionLoanAsync(id);
            return StatusCode(201, classification);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected NPL classification failure for loan {LoanId}", id);
            return StatusCode(500, new { message = "Unexpected error while classifying loan" });
        }
    }
}
