using System.Security.Claims;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/cash-incidents")]
[Authorize]
public class CashIncidentController : ControllerBase
{
    private readonly ICashIncidentService _cashIncidentService;

    public CashIncidentController(ICashIncidentService cashIncidentService)
    {
        _cashIncidentService = cashIncidentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CashIncidentDto>>> GetIncidents([FromQuery] string? branchId, [FromQuery] string? status)
    {
        var incidents = await _cashIncidentService.GetIncidentsAsync(branchId, status);
        return Ok(incidents);
    }

    [HttpPost]
    public async Task<ActionResult<CashIncidentDto>> CreateIncident([FromBody] CreateCashIncidentRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffId();
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return Unauthorized();
            }

            var incident = await _cashIncidentService.CreateIncidentAsync(request, staffId);
            return Ok(incident);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{incidentId}/resolve")]
    public async Task<ActionResult<CashIncidentDto>> ResolveIncident(string incidentId, [FromBody] ResolveCashIncidentRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffId();
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return Unauthorized();
            }

            var incident = await _cashIncidentService.ResolveIncidentAsync(incidentId, staffId, request.ResolutionNote);
            return Ok(incident);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? GetCurrentStaffId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
