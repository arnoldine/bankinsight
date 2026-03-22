using System;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/fees")]
public class FeeController : ControllerBase
{
    private readonly IFeeService _feeService;
    private readonly ILogger<FeeController> _logger;

    public FeeController(IFeeService feeService, ILogger<FeeController> logger)
    {
        _feeService = feeService;
        _logger = logger;
    }

    [HttpPost]
    [RequirePermission("POST_TRANSACTIONS")]
    public async Task<IActionResult> AssessAccountFee([FromBody] AssessAccountFeeRequest request)
    {
        try
        {
            var fee = await _feeService.AssessAccountFeeAsync(request);
            return StatusCode(201, fee);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected fee assessment failure for account {AccountId}", request.AccountId);
            return StatusCode(500, new { message = "Unexpected error while assessing fee" });
        }
    }
}
