using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/client-complaint-ops")]
[Authorize]
public class ClientComplaintOperationsController : ControllerBase
{
    private readonly ClientChannelService _clientChannelService;

    public ClientComplaintOperationsController(ClientChannelService clientChannelService)
    {
        _clientChannelService = clientChannelService;
    }

    [HttpGet("queue")]
    [RequirePermission("customers.view")]
    public async Task<IActionResult> GetQueue([FromQuery] string? status)
    {
        return Ok(await _clientChannelService.GetComplaintQueueAsync(status));
    }

    [HttpGet("queue/summary")]
    [RequirePermission("customers.view")]
    public async Task<IActionResult> GetQueueSummary()
    {
        return Ok(await _clientChannelService.GetComplaintQueueSummaryAsync());
    }

    [HttpGet("{complaintId}")]
    [RequirePermission("customers.view")]
    public async Task<IActionResult> GetComplaint(string complaintId)
    {
        var complaint = await _clientChannelService.GetComplaintForOperationsAsync(complaintId);
        return complaint == null
            ? NotFound(new { message = "Complaint not found." })
            : Ok(complaint);
    }

    [HttpPost("{complaintId}/triage")]
    [RequirePermission("customers.edit")]
    public async Task<IActionResult> TriageComplaint(string complaintId, [FromBody] TriageClientComplaintRequest request)
    {
        var complaint = await _clientChannelService.TriageComplaintAsync(complaintId, request);
        return complaint == null
            ? NotFound(new { message = "Complaint not found." })
            : Ok(complaint);
    }

    [HttpPost("{complaintId}/escalate")]
    [RequirePermission("customers.edit")]
    public async Task<IActionResult> EscalateComplaint(string complaintId, [FromBody] EscalateClientComplaintRequest request)
    {
        var complaint = await _clientChannelService.EscalateComplaintAsync(complaintId, request);
        return complaint == null
            ? NotFound(new { message = "Complaint not found." })
            : Ok(complaint);
    }

    [HttpPost("{complaintId}/close")]
    [RequirePermission("customers.edit")]
    public async Task<IActionResult> CloseComplaint(string complaintId, [FromBody] CloseClientComplaintRequest request)
    {
        var complaint = await _clientChannelService.CloseComplaintAsync(complaintId, request);
        return complaint == null
            ? NotFound(new { message = "Complaint not found." })
            : Ok(complaint);
    }

    [HttpPost("process-sla")]
    [RequirePermission("customers.edit")]
    public async Task<IActionResult> ProcessSlaBreaches()
    {
        return Ok(await _clientChannelService.ProcessComplaintSlaBreachesAsync());
    }
}
