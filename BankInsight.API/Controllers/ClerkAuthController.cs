using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/clerk")]
public class ClerkAuthController : ControllerBase
{
    private readonly IClerkUserSyncService _clerkUserSyncService;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public ClerkAuthController(
        IClerkUserSyncService clerkUserSyncService,
        ApplicationDbContext context,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        _clerkUserSyncService = clerkUserSyncService;
        _context = context;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
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
    public async Task<IActionResult> HandleClerkWebhook()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return BadRequest(new { message = "Webhook payload is required" });
        }

        var secret = _configuration["Clerk:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            if (!TryValidateSvixSignature(body, secret, Request.Headers["svix-id"], Request.Headers["svix-timestamp"], Request.Headers["svix-signature"]))
            {
                return Unauthorized(new { message = "Invalid Clerk webhook signature" });
            }
        }
        else if (_hostEnvironment.IsProduction())
        {
            return StatusCode(500, new { message = "Clerk webhook secret is not configured" });
        }

        Dictionary<string, object>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "Invalid webhook payload" });
        }

        if (payload == null || !payload.ContainsKey("data"))
        {
            return BadRequest(new { message = "Invalid webhook payload" });
        }
        
        return Ok(new { success = true });
    }

    private static bool TryValidateSvixSignature(string payload, string secret, string? messageId, string? timestamp, string? signaturesHeader)
    {
        if (string.IsNullOrWhiteSpace(messageId) ||
            string.IsNullOrWhiteSpace(timestamp) ||
            string.IsNullOrWhiteSpace(signaturesHeader))
        {
            return false;
        }

        if (!long.TryParse(timestamp, out var unixTimestamp))
        {
            return false;
        }

        var timestampUtc = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        if (DateTimeOffset.UtcNow - timestampUtc > TimeSpan.FromMinutes(5))
        {
            return false;
        }

        var normalizedSecret = secret.StartsWith("whsec_", StringComparison.OrdinalIgnoreCase)
            ? secret["whsec_".Length..]
            : secret;

        byte[] secretBytes;
        try
        {
            secretBytes = Convert.FromBase64String(normalizedSecret);
        }
        catch (FormatException)
        {
            return false;
        }

        var signedPayload = $"{messageId}.{timestamp}.{payload}";
        using var hmac = new HMACSHA256(secretBytes);
        var computedSignature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload)));

        var expectedBytes = Encoding.UTF8.GetBytes(computedSignature);
        var signatures = signaturesHeader
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(entry => entry.StartsWith("v1,", StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry[3..]);

        foreach (var signature in signatures)
        {
            var providedBytes = Encoding.UTF8.GetBytes(signature);
            if (providedBytes.Length == expectedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
            {
                return true;
            }
        }

        return false;
    }
}

public class ClerkSyncRequest
{
    public string ClerkUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
