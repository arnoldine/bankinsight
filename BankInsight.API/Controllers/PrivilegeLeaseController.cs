using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/privilege-leases")]
public class PrivilegeLeaseController : ControllerBase
{
    private readonly IPrivilegeLeaseService _privilegeLeaseService;

    public PrivilegeLeaseController(IPrivilegeLeaseService privilegeLeaseService)
    {
        _privilegeLeaseService = privilegeLeaseService;
    }

    [HttpPost]
    [RequirePermission("MANAGE_ROLES")]
    public async Task<IActionResult> CreateLease([FromBody] CreatePrivilegeLeaseRequest request)
    {
        try
        {
            var lease = await _privilegeLeaseService.CreateLeaseAsync(request);
            return StatusCode(201, lease);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{leaseId}/revoke")]
    [RequirePermission("MANAGE_ROLES")]
    public async Task<IActionResult> RevokeLease(string leaseId, [FromBody] RevokePrivilegeLeaseRequest request)
    {
        var revoked = await _privilegeLeaseService.RevokeLeaseAsync(leaseId, request.RevokedBy);
        if (!revoked)
        {
            return NotFound(new { message = "Privilege lease not found" });
        }

        return Ok(new { message = "Privilege lease revoked" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyLeases()
    {
        var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(staffId))
        {
            return Unauthorized(new { message = "Invalid session" });
        }

        var leases = await _privilegeLeaseService.GetActiveLeasesForStaffAsync(staffId);
        return Ok(leases);
    }

    [HttpGet("{staffId}")]
    [RequirePermission("VIEW_ROLES")]
    public async Task<IActionResult> GetStaffLeases(string staffId)
    {
        var leases = await _privilegeLeaseService.GetActiveLeasesForStaffAsync(staffId);
        return Ok(leases);
    }
}
