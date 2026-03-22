using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class RoleService
{
    private readonly ApplicationDbContext _context;

    public RoleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleSummaryDto>> GetRolesAsync()
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .OrderBy(r => r.Name)
            .Select(role => MapRoleSummary(role))
            .ToListAsync();
    }

    public async Task<RoleSummaryDto> CreateRoleAsync(CreateRoleRequest request)
    {
        var role = new Role
        {
            Id = $"ROL{(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000).ToString().PadLeft(4, '0')}",
            Name = request.Name,
            Description = request.Description
        };

        var matchedPermissions = await _context.Permissions.Where(p => request.Permissions.Contains(p.Code)).ToListAsync();
        foreach (var p in matchedPermissions)
        {
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
        }

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return await GetRoleSummaryAsync(role.Id)
            ?? throw new InvalidOperationException("Created role could not be reloaded.");
    }

    public async Task<RoleSummaryDto?> UpdateRoleAsync(string id, UpdateRoleRequest request)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return null;

        role.Name = request.Name;
        role.Description = request.Description;

        _context.RolePermissions.RemoveRange(role.RolePermissions);

        var matchedPermissions = await _context.Permissions.Where(p => request.Permissions.Contains(p.Code)).ToListAsync();
        foreach (var p in matchedPermissions)
        {
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
        }

        await _context.SaveChangesAsync();
        return await GetRoleSummaryAsync(id);
    }

    private async Task<RoleSummaryDto?> GetRoleSummaryAsync(string id)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .Where(r => r.Id == id)
            .Select(role => MapRoleSummary(role))
            .FirstOrDefaultAsync();
    }

    private async Task EnsureUniqueRoleNameAsync(string roleName, string? excludingRoleId = null)
    {
        var normalizedRoleName = roleName.Trim();
        var duplicateExists = await _context.Roles.AnyAsync(role =>
            role.Name.ToLower() == normalizedRoleName.ToLower() &&
            (excludingRoleId == null || role.Id != excludingRoleId));

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A role named '{normalizedRoleName}' already exists.");
        }
    }

    private static RoleSummaryDto MapRoleSummary(Role role)
    {
        return new RoleSummaryDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive,
            UserCount = role.UserRoles.Count,
            Permissions = role.RolePermissions
                .Select(rp => rp.Permission.Code)
                .OrderBy(code => code)
                .ToList()
        };
    }
}
