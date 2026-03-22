using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/operations/eod")]
public class OperationsController : ControllerBase
{
    private readonly OperationsService _operationsService;
    private readonly ICurrentUserContext _currentUserContext;

    public OperationsController(OperationsService operationsService, ICurrentUserContext currentUserContext)
    {
        _operationsService = operationsService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet("status")]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _operationsService.GetEodStatusAsync();
        return Ok(result);
    }

    [HttpPost("run-step")]
    [HasPermission(AppPermissions.GeneralLedger.Post)]
    public async Task<IActionResult> RunStep([FromBody] RunEodStepRequest request)
    {
        var result = await _operationsService.RunStepAsync(request, _currentUserContext.UserId);
        return Ok(result);
    }
}
