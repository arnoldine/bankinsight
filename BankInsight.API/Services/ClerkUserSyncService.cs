using System;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

/// <summary>
/// Service to sync Clerk users with local Staff/User table
/// </summary>
public interface IClerkUserSyncService
{
    Task<Staff?> SyncClerkUserAsync(string clerkUserId, string? email, string? firstName, string? lastName);
    Task<Staff?> GetOrCreateStaffFromClerkAsync(string clerkUserId, string email);
}

public class ClerkUserSyncService : IClerkUserSyncService
{
    private readonly ApplicationDbContext _context;

    public ClerkUserSyncService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Sync a Clerk user with the Staff table. Creates or updates the Staff record.
    /// </summary>
    public async Task<Staff?> SyncClerkUserAsync(string clerkUserId, string? email, string? firstName, string? lastName)
    {
        if (string.IsNullOrEmpty(clerkUserId))
        {
            throw new ArgumentNullException(nameof(clerkUserId));
        }

        // Look for existing staff with this Clerk ID
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.ClerkUserId == clerkUserId);

        if (staff == null && !string.IsNullOrEmpty(email))
        {
            // Try to find by email
            staff = await _context.Staff
                .FirstOrDefaultAsync(s => s.Email == email);

            if (staff != null)
            {
                // Link existing staff to Clerk user
                staff.ClerkUserId = clerkUserId;
            }
        }

        if (staff == null)
        {
            // Create new staff record for Clerk user
            staff = new Staff
            {
                Id = Guid.NewGuid().ToString(),
                Email = email ?? $"user_{clerkUserId}@bankinsight.local",
                Name = $"{firstName} {lastName}".Trim(),
                Status = "Active",
                ClerkUserId = clerkUserId,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Dummy hash, not used with Clerk
                BranchId = null // Will be set during onboarding
            };

            _context.Staff.Add(staff);
        }
        else
        {
            // Update existing staff
            if (!string.IsNullOrEmpty(email))
            {
                staff.Email = email;
            }

            if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
            {
                staff.Name = $"{firstName} {lastName}".Trim();
            }

            if (string.IsNullOrEmpty(staff.ClerkUserId))
            {
                staff.ClerkUserId = clerkUserId;
            }

            _context.Staff.Update(staff);
        }

        await _context.SaveChangesAsync();
        return staff;
    }

    /// <summary>
    /// Get or create a Staff record from Clerk user identifier
    /// </summary>
    public async Task<Staff?> GetOrCreateStaffFromClerkAsync(string clerkUserId, string email)
    {
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.ClerkUserId == clerkUserId);

        if (staff == null)
        {
            return await SyncClerkUserAsync(clerkUserId, email, null, null);
        }

        return staff;
    }
}
