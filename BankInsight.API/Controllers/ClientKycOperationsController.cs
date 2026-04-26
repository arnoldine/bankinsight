using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/client-kyc-ops")]
[Authorize]
public class ClientKycOperationsController : ControllerBase
{
    private readonly ClientChannelService _clientChannelService;

    public ClientKycOperationsController(ClientChannelService clientChannelService)
    {
        _clientChannelService = clientChannelService;
    }

    [HttpGet("queue")]
    [RequirePermission("customers.view")]
    public async Task<IActionResult> GetQueue([FromQuery] string? status)
    {
        return Ok(await _clientChannelService.GetKycCaseQueueAsync(status));
    }

    [HttpPost("{kycCaseId}/review")]
    [RequirePermission("customers.edit")]
    public async Task<IActionResult> ReviewCase(string kycCaseId, [FromBody] ReviewClientKycCaseRequest request)
    {
        try
        {
            var kycCase = await _clientChannelService.ReviewKycCaseAsync(kycCaseId, request);
            return kycCase == null
                ? NotFound(new { message = "KYC case not found." })
                : Ok(kycCase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
