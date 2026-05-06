using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/operations-control")]
public class OperationsControlCenterController : ControllerBase
{
    private readonly OperationsControlCenterService _operationsControlCenterService;

    public OperationsControlCenterController(OperationsControlCenterService operationsControlCenterService)
    {
        _operationsControlCenterService = operationsControlCenterService;
    }

    [HttpGet("summary")]
    [RequirePermission("accounts.view")]
    public async Task<ActionResult<OperationsControlCenterDto>> GetSummary()
    {
        return Ok(await _operationsControlCenterService.GetSummaryAsync());
    }
}
