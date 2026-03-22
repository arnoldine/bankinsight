using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/clerk")]
public class ClerkAuthController : ControllerBase
{
    private readonly IClerkUserSyncService _clerkUserSyncService;
    private readonly ApplicationDbContext _context;

    public ClerkAuthController(
        IClerkUserSyncService clerkUserSyncService,
        ApplicationDbContext context)
    {
        _clerkUserSyncService = clerkUserSyncService;
        _context = context;
    }

    /// <summary>
    /// Get current authenticated user's staff profile
    /// </summary>
    [Authorize(AuthenticationSchemes = "Clerk")]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var clerkUserId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(clerkUserId))
        {
            return Unauthorized(new { message = "Invalid or missing Clerk user ID" });
        }

        // Sync/get the user from local database
        var staff = await _clerkUserSyncService.GetOrCreateStaffFromClerkAsync(clerkUserId, email ?? "unknown@example.com");

        if (staff == null)
        {
            return NotFound(new { message = "User not found in system" });
        }

        var userDto = new
        {
            id = staff.Id,
            name = staff.Name,
            email = staff.Email,
            clerkUserId = staff.ClerkUserId,
            roleId = staff.UserRoles?.FirstOrDefault()?.RoleId,
            branchId = staff.BranchId,
            status = staff.Status,
            phone = staff.Phone,
            role = staff.UserRoles?.FirstOrDefault()?.Role?.Name,
            permissions = Array.Empty<string>()
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Sync Clerk user with local Staff table
    /// </summary>
    [Authorize(AuthenticationSchemes = "Clerk")]
    [HttpPost("sync")]
    public async Task<IActionResult> SyncClerkUser([FromBody] ClerkSyncRequest request)
    {
        if (string.IsNullOrEmpty(request.ClerkUserId) || string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { message = "ClerkUserId and Email are required" });
        }

        var staff = await _clerkUserSyncService.SyncClerkUserAsync(
            request.ClerkUserId,
            request.Email,
            request.FirstName,
            request.LastName);

        if (staff == null)
        {
            return StatusCode(500, new { message = "Failed to sync user" });
        }

        return Ok(new
        {
            id = staff.Id,
            name = staff.Name,
            email = staff.Email,
            clerkUserId = staff.ClerkUserId
        });
    }

    /// <summary>
    /// Webhook endpoint for Clerk user events (user.created, user.updated, user.deleted)
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleClerkWebhook([FromBody] Dictionary<string, object> payload)
    {
        // Verify webhook signature in production using Clerk's webhook secret
        // For now, we'll process the event directly

        if (payload == null || !payload.ContainsKey("data"))
        {
            return BadRequest(new { message = "Invalid webhook payload" });
        }

        // TODO: Add signature verification using SVIX or Clerk's webhook secret
        
        return Ok(new { success = true });
    }
}

public class ClerkSyncRequest
{
    public string ClerkUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
