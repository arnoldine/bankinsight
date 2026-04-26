using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/client-channel")]
[Authorize(Policy = "ClientCustomer")]
public class ClientChannelController : ControllerBase
{
    private readonly ClientChannelService _clientChannelService;

    public ClientChannelController(ClientChannelService clientChannelService)
    {
        _clientChannelService = clientChannelService;
    }

    [HttpGet("bootstrap")]
    public async Task<IActionResult> GetBootstrap()
    {
        return Ok(await _clientChannelService.GetBootstrapAsync());
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _clientChannelService.GetLinkedCustomerProfileAsync();
        if (profile == null)
        {
            return NotFound(new
            {
                message = "No linked customer profile was found for the signed-in identity."
            });
        }

        return Ok(profile);
    }

    [HttpGet("kyc")]
    public async Task<IActionResult> GetKycOverview()
    {
        var overview = await _clientChannelService.GetKycOverviewAsync();
        return overview == null
            ? NotFound(new { message = "No linked customer profile was found for the signed-in identity." })
            : Ok(overview);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateClientProfileRequest request)
    {
        try
        {
            var profile = await _clientChannelService.UpdateLinkedProfileAsync(request);
            return profile == null
                ? NotFound(new { message = "No linked customer profile was found for the signed-in identity." })
                : Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("profile/media")]
    public async Task<IActionResult> UploadProfileMedia([FromBody] UploadClientProfileMediaRequest request)
    {
        try
        {
            var media = await _clientChannelService.UploadLinkedProfileMediaAsync(request);
            return media == null
                ? NotFound(new { message = "No linked customer profile was found for the signed-in identity." })
                : Ok(media);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("kyc/refresh")]
    public async Task<IActionResult> SubmitKycRefresh([FromBody] SubmitClientKycRefreshRequest request)
    {
        try
        {
            var kycCase = await _clientChannelService.SubmitKycRefreshCaseAsync(request);
            return kycCase == null
                ? NotFound(new { message = "No linked customer profile was found for the signed-in identity." })
                : StatusCode(201, kycCase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        return Ok(await _clientChannelService.GetLinkedAccountsAsync());
    }

    [HttpGet("banking/overview")]
    public async Task<IActionResult> GetBankingOverview()
    {
        return Ok(await _clientChannelService.GetBankingOverviewAsync());
    }

    [HttpGet("banking/merchants")]
    public async Task<IActionResult> GetMerchants()
    {
        return Ok(await _clientChannelService.GetMerchantCatalogAsync());
    }

    [HttpGet("banking/merchant-acceptance/eligibility")]
    public async Task<IActionResult> GetMerchantAcceptanceEligibility()
    {
        return Ok(await _clientChannelService.GetMerchantAcceptanceEligibilityAsync());
    }

    [HttpGet("banking/merchant-acceptance/profiles")]
    public async Task<IActionResult> GetMerchantProfiles()
    {
        return Ok(await _clientChannelService.GetMerchantProfilesAsync());
    }

    [HttpPost("banking/merchant-acceptance/profiles")]
    public async Task<IActionResult> CreateMerchantProfile([FromBody] CreateClientMerchantProfileRequest request)
    {
        try
        {
            return StatusCode(201, await _clientChannelService.CreateMerchantProfileAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("banking/transfers/internal")]
    public async Task<IActionResult> CreateInternalTransfer([FromBody] CreateClientInternalTransferRequest request)
    {
        try
        {
            return Ok(await _clientChannelService.CreateInternalTransferAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("banking/payments/merchants")]
    public async Task<IActionResult> CreateMerchantPayment([FromBody] CreateClientMerchantPaymentRequest request)
    {
        try
        {
            return Ok(await _clientChannelService.CreateMerchantPaymentAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("banking/payments/qr/resolve")]
    public async Task<IActionResult> ResolveQrPayment([FromBody] ResolveClientQrPaymentRequest request)
    {
        try
        {
            return Ok(await _clientChannelService.ResolveQrPaymentAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("banking/payments/qr")]
    public async Task<IActionResult> CreateQrPayment([FromBody] CreateClientQrPaymentRequest request)
    {
        try
        {
            return Ok(await _clientChannelService.CreateQrPaymentAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("banking/standing-orders")]
    public async Task<IActionResult> GetStandingOrders()
    {
        return Ok(await _clientChannelService.GetStandingOrdersAsync());
    }

    [HttpPost("banking/standing-orders")]
    public async Task<IActionResult> CreateStandingOrder([FromBody] CreateClientStandingOrderRequest request)
    {
        try
        {
            return StatusCode(201, await _clientChannelService.CreateStandingOrderAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("banking/standing-orders/{standingOrderId}/status")]
    public async Task<IActionResult> UpdateStandingOrderStatus(string standingOrderId, [FromBody] UpdateClientStandingOrderStatusRequest request)
    {
        var standingOrder = await _clientChannelService.UpdateStandingOrderStatusAsync(standingOrderId, request.Status);
        return standingOrder == null
            ? NotFound(new { message = "Standing order not found." })
            : Ok(standingOrder);
    }

    [HttpGet("banking/investments")]
    public async Task<IActionResult> GetFixedDeposits()
    {
        return Ok(await _clientChannelService.GetFixedDepositsAsync());
    }

    [HttpPost("banking/investments")]
    public async Task<IActionResult> CreateFixedDeposit([FromBody] CreateClientFixedDepositRequest request)
    {
        try
        {
            return StatusCode(201, await _clientChannelService.CreateFixedDepositAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("banking/loans")]
    public async Task<IActionResult> GetLoans()
    {
        return Ok(await _clientChannelService.GetClientLoansAsync());
    }

    [HttpGet("banking/loan-products")]
    public async Task<IActionResult> GetLoanProducts()
    {
        return Ok(await _clientChannelService.GetClientLoanProductsAsync());
    }

    [HttpPost("banking/loans/apply")]
    public async Task<IActionResult> ApplyForLoan([FromBody] CreateClientLoanApplicationRequest request)
    {
        try
        {
            return StatusCode(201, await _clientChannelService.ApplyForLoanAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("banking/loans/{loanId}/schedule")]
    public async Task<IActionResult> GetLoanSchedule(string loanId)
    {
        try
        {
            return Ok(await _clientChannelService.GetClientLoanScheduleAsync(loanId));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("banking/loans/{loanId}/statement")]
    public async Task<IActionResult> GetLoanStatement(string loanId)
    {
        try
        {
            return Ok(await _clientChannelService.GetClientLoanStatementAsync(loanId));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        return Ok(await _clientChannelService.GetMySessionsAsync());
    }

    [HttpGet("statements")]
    public async Task<IActionResult> GetStatements()
    {
        return Ok(await _clientChannelService.GetStatementSummariesAsync());
    }

    [HttpGet("statements/{accountId}")]
    public async Task<IActionResult> GetStatementDetail(string accountId, [FromQuery] int year, [FromQuery] int month)
    {
        var statement = await _clientChannelService.GetStatementDetailAsync(accountId, year, month);
        return statement == null
            ? NotFound(new { message = "Statement not found." })
            : Ok(statement);
    }

    [HttpGet("statements/{accountId}/export")]
    public async Task<IActionResult> ExportStatement(string accountId, [FromQuery] int year, [FromQuery] int month, [FromQuery] string? format)
    {
        try
        {
            var export = await _clientChannelService.ExportStatementAsync(accountId, year, month, format);
            return export == null
                ? NotFound(new { message = "Statement not found." })
                : Ok(export);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("complaints")]
    public async Task<IActionResult> GetComplaints()
    {
        return Ok(await _clientChannelService.GetComplaintsAsync());
    }

    [HttpGet("complaints/{complaintId}")]
    public async Task<IActionResult> GetComplaint(string complaintId)
    {
        var complaint = await _clientChannelService.GetComplaintAsync(complaintId);
        if (complaint == null)
        {
            return NotFound(new { message = "Complaint not found." });
        }

        return Ok(complaint);
    }

    [HttpPost("complaints/{complaintId}/reopen")]
    public async Task<IActionResult> ReopenComplaint(string complaintId, [FromBody] ReopenClientComplaintRequest request)
    {
        var complaint = await _clientChannelService.ReopenComplaintAsync(complaintId, request);
        return complaint == null
            ? NotFound(new { message = "Complaint not found." })
            : Ok(complaint);
    }

    [HttpPost("complaints/{complaintId}/attachments")]
    public async Task<IActionResult> UploadComplaintAttachment(string complaintId, [FromBody] UploadClientComplaintAttachmentRequest request)
    {
        var attachment = await _clientChannelService.AddComplaintAttachmentAsync(complaintId, request);
        return attachment == null
            ? NotFound(new { message = "Complaint not found." })
            : Ok(attachment);
    }

    [HttpPost("complaints")]
    public async Task<IActionResult> CreateComplaint([FromBody] CreateClientComplaintRequest request)
    {
        var complaint = await _clientChannelService.CreateComplaintAsync(request);
        if (complaint == null)
        {
            return NotFound(new
            {
                message = "No linked customer record was found for the signed-in identity."
            });
        }

        return StatusCode(201, complaint);
    }
}
