using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface ILoginAttemptService
{
    Task LogAttemptAsync(string email, bool success, string? failureReason, string ipAddress, string? userAgent, string? staffId = null);
    Task<List<LoginAttemptDto>> GetRecentAttemptsAsync(int limit = 100);
    Task<List<LoginAttemptDto>> GetFailedAttemptsAsync(string email, DateTime since);
    Task<bool> IsAccountLockedAsync(string email);
}

public class LoginAttemptService : ILoginAttemptService
{
    private readonly ApplicationDbContext _context;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    public LoginAttemptService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAttemptAsync(string email, bool success, string? failureReason, string ipAddress, string? userAgent, string? staffId = null)
    {
        var attempt = new LoginAttempt
        {
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            FailureReason = failureReason,
            StaffId = staffId,
            AttemptedAt = DateTime.UtcNow
        };

        _context.LoginAttempts.Add(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task<List<LoginAttemptDto>> GetRecentAttemptsAsync(int limit = 100)
    {
        return await _context.LoginAttempts
            .OrderByDescending(a => a.AttemptedAt)
            .Take(limit)
            .Select(a => new LoginAttemptDto
            {
                Id = a.Id,
                Email = a.Email,
                IpAddress = a.IpAddress,
                Success = a.Success,
                FailureReason = a.FailureReason,
                AttemptedAt = a.AttemptedAt
            })
            .ToListAsync();
    }

    public async Task<List<LoginAttemptDto>> GetFailedAttemptsAsync(string email, DateTime since)
    {
        return await _context.LoginAttempts
            .Where(a => a.Email == email && !a.Success && a.AttemptedAt >= since)
            .OrderByDescending(a => a.AttemptedAt)
            .Select(a => new LoginAttemptDto
            {
                Id = a.Id,
                Email = a.Email,
                IpAddress = a.IpAddress,
                Success = a.Success,
                FailureReason = a.FailureReason,
                AttemptedAt = a.AttemptedAt
            })
            .ToListAsync();
    }

    public async Task<bool> IsAccountLockedAsync(string email)
    {
        var lockoutStart = DateTime.UtcNow.Subtract(LockoutDuration);
        
        var failedAttempts = await _context.LoginAttempts
            .Where(a => a.Email == email && !a.Success && a.AttemptedAt >= lockoutStart)
            .CountAsync();

        return failedAttempts >= MaxFailedAttempts;
    }
}
