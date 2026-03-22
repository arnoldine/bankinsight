using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BankInsight.API.Services;

public interface IPrivilegeLeaseService
{
    Task<PrivilegeLeaseDto> CreateLeaseAsync(CreatePrivilegeLeaseRequest request);
    Task<bool> RevokeLeaseAsync(string leaseId, string revokedBy);
    Task<List<PrivilegeLeaseDto>> GetActiveLeasesForStaffAsync(string staffId);
    Task<List<string>> GetActiveLeasedPermissionsAsync(string staffId);
    Task<bool> HasActivePermissionLeaseAsync(string staffId, string permission);
}

public class PrivilegeLeaseService : IPrivilegeLeaseService
{
    private static readonly TimeSpan MaxLeaseDuration = TimeSpan.FromHours(24);

    private readonly ApplicationDbContext _context;

    public PrivilegeLeaseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PrivilegeLeaseDto> CreateLeaseAsync(CreatePrivilegeLeaseRequest request)
    {
        try
        {
            var staff = await _context.Staff
                .Include(s => s.UserRoles)
                .FirstOrDefaultAsync(s => s.Id == request.StaffId);
            if (staff == null)
            {
                throw new InvalidOperationException("Staff not found");
            }

            var startsAt = request.StartsAt ?? DateTime.UtcNow;
            if (request.ExpiresAt <= startsAt)
            {
                throw new InvalidOperationException("Lease expiry must be after lease start");
            }

            if (request.ExpiresAt - startsAt > MaxLeaseDuration)
            {
                throw new InvalidOperationException("Lease duration cannot exceed 24 hours");
            }

            var normalizedPermission = request.Permission.Trim().ToUpperInvariant();

            var duplicateActiveLease = await _context.PrivilegeLeases.AnyAsync(p =>
                p.StaffId == request.StaffId &&
                p.Permission == normalizedPermission &&
                !p.IsRevoked &&
                p.ExpiresAt > DateTime.UtcNow);

            if (duplicateActiveLease)
            {
                throw new InvalidOperationException("An active lease already exists for this permission");
            }

            var lease = new PrivilegeLease
            {
                Id = $"PL-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                StaffId = request.StaffId,
                Permission = normalizedPermission,
                Reason = request.Reason.Trim(),
                ApprovedBy = request.ApprovedBy,
                ApprovedAt = DateTime.UtcNow,
                StartsAt = startsAt,
                ExpiresAt = request.ExpiresAt,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.PrivilegeLeases.Add(lease);
            await _context.SaveChangesAsync();

            return MapToDto(lease);
        }
        catch (Exception ex) when (IsPrivilegeLeaseTableMissing(ex))
        {
            throw new InvalidOperationException("Privilege lease storage is not initialized. Apply database migration for table 'privilege_leases'.");
        }
    }

    public async Task<bool> RevokeLeaseAsync(string leaseId, string revokedBy)
    {
        try
        {
            var lease = await _context.PrivilegeLeases.FirstOrDefaultAsync(p => p.Id == leaseId);
            if (lease == null)
            {
                return false;
            }

            if (lease.IsRevoked)
            {
                return true;
            }

            lease.IsRevoked = true;
            lease.RevokedBy = revokedBy;
            lease.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) when (IsPrivilegeLeaseTableMissing(ex))
        {
            throw new InvalidOperationException("Privilege lease storage is not initialized. Apply database migration for table 'privilege_leases'.");
        }
    }

    public async Task<List<PrivilegeLeaseDto>> GetActiveLeasesForStaffAsync(string staffId)
    {
        try
        {
            var now = DateTime.UtcNow;

            var leases = await _context.PrivilegeLeases
                .Where(p => p.StaffId == staffId && !p.IsRevoked && p.StartsAt <= now && p.ExpiresAt > now)
                .OrderByDescending(p => p.ExpiresAt)
                .ToListAsync();

            return leases.Select(MapToDto).ToList();
        }
        catch (Exception ex) when (IsPrivilegeLeaseTableMissing(ex))
        {
            return new List<PrivilegeLeaseDto>();
        }
    }

    public async Task<List<string>> GetActiveLeasedPermissionsAsync(string staffId)
    {
        try
        {
            var now = DateTime.UtcNow;

            return await _context.PrivilegeLeases
                .Where(p => p.StaffId == staffId && !p.IsRevoked && p.StartsAt <= now && p.ExpiresAt > now)
                .Select(p => p.Permission)
                .Distinct()
                .ToListAsync();
        }
        catch (Exception ex) when (IsPrivilegeLeaseTableMissing(ex))
        {
            return new List<string>();
        }
    }

    public async Task<bool> HasActivePermissionLeaseAsync(string staffId, string permission)
    {
        try
        {
            var now = DateTime.UtcNow;
            var normalizedPermission = permission.Trim().ToUpperInvariant();

            return await _context.PrivilegeLeases.AnyAsync(p =>
                p.StaffId == staffId &&
                p.Permission == normalizedPermission &&
                !p.IsRevoked &&
                p.StartsAt <= now &&
                p.ExpiresAt > now);
        }
        catch (Exception ex) when (IsPrivilegeLeaseTableMissing(ex))
        {
            return false;
        }
    }

    private static bool IsPrivilegeLeaseTableMissing(Exception ex)
    {
        if (ex is PostgresException pg && pg.SqlState == "42P01")
        {
            return string.Equals(pg.TableName, "privilege_leases", StringComparison.OrdinalIgnoreCase)
                || pg.MessageText.Contains("privilege_leases", StringComparison.OrdinalIgnoreCase);
        }

        return ex.Message.Contains("privilege_leases", StringComparison.OrdinalIgnoreCase);
    }

    private static PrivilegeLeaseDto MapToDto(PrivilegeLease lease)
    {
        var now = DateTime.UtcNow;

        return new PrivilegeLeaseDto
        {
            Id = lease.Id,
            StaffId = lease.StaffId,
            Permission = lease.Permission,
            Reason = lease.Reason,
            ApprovedBy = lease.ApprovedBy,
            ApprovedAt = lease.ApprovedAt,
            StartsAt = lease.StartsAt,
            ExpiresAt = lease.ExpiresAt,
            IsRevoked = lease.IsRevoked,
            IsActive = !lease.IsRevoked && lease.StartsAt <= now && lease.ExpiresAt > now
        };
    }
}
