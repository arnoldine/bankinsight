using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class UserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Staff>> GetUsersAsync()
    {
        return await _context.Staff.ToListAsync();
    }

    public async Task<Staff?> GetUserByIdAsync(string id)
    {
        return await _context.Staff.FindAsync(id);
    }

    public async Task<Staff> CreateUserAsync(CreateUserRequest request)
    {
        var id = $"STF{(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000).ToString().PadLeft(4, '0')}";

        var user = new Staff
        {
            Id = id,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            BranchId = string.IsNullOrEmpty(request.BranchId) ? null : request.BranchId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = "Active",
            AvatarInitials = request.AvatarInitials ?? string.Empty
        };

        if (!string.IsNullOrEmpty(request.RoleId))
        {
            user.UserRoles.Add(new UserRole { RoleId = request.RoleId });
        }

        _context.Staff.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<Staff?> UpdateUserAsync(string id, UpdateUserRequest request)
    {
        var user = await _context.Staff.Include(s => s.UserRoles).FirstOrDefaultAsync(s => s.Id == id);
        if (user == null) return null;

        if (request.Name != null) user.Name = request.Name;
        if (request.Email != null) user.Email = request.Email;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.RoleId != null) 
        {
            user.UserRoles.Clear();
            if (!string.IsNullOrEmpty(request.RoleId))
            {
                user.UserRoles.Add(new UserRole { RoleId = request.RoleId });
            }
        }
        if (request.BranchId != null) user.BranchId = request.BranchId;
        if (request.Status != null) user.Status = request.Status;
        if (request.Password != null) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await _context.Staff.FindAsync(id);
        if (user == null) return false;

        _context.Staff.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
