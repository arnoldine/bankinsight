using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IAuditLoggingService
{
    Task<AuditLog> LogActionAsync(
        string action,
        string entityType,
        string? entityId,
        string? userId,
        string? description = null,
        string? ipAddress = null,
        string? userAgent = null,
        string status = "SUCCESS",
        string? errorMessage = null,
        object? oldValues = null,
        object? newValues = null);

    Task<List<AuditLog>> GetAuditLogsAsync(int limit = 100, int offset = 0);
    Task<List<AuditLog>> GetAuditLogsByEntityAsync(string entityType, string entityId);
    Task<List<AuditLog>> GetAuditLogsByUserAsync(string userId);
}

public class AuditLoggingService : IAuditLoggingService
{
    private const int DefaultColumnLimit = 500;
    private readonly ApplicationDbContext _context;

    public AuditLoggingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> LogActionAsync(
        string action,
        string entityType,
        string? entityId,
        string? userId,
        string? description = null,
        string? ipAddress = null,
        string? userAgent = null,
        string status = "SUCCESS",
        string? errorMessage = null,
        object? oldValues = null,
        object? newValues = null)
    {
        var serializedOldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null;
        var serializedNewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null;
        var normalizedUserId = await ResolveExistingStaffIdAsync(userId);

        var auditLog = new AuditLog
        {
            Action = Truncate(action, 100) ?? string.Empty,
            EntityType = Truncate(entityType, 100) ?? string.Empty,
            EntityId = Truncate(entityId, 50),
            UserId = normalizedUserId,
            Description = Truncate(description, DefaultColumnLimit),
            IpAddress = Truncate(ipAddress, 50),
            UserAgent = Truncate(userAgent, DefaultColumnLimit),
            Status = Truncate(status, 20) ?? "SUCCESS",
            ErrorMessage = Truncate(errorMessage, DefaultColumnLimit),
            OldValues = Truncate(serializedOldValues, 2000),
            NewValues = Truncate(serializedNewValues, 2000),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = normalizedUserId
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        return auditLog;
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(int limit = 100, int offset = 0)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetAuditLogsByEntityAsync(string entityType, string entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetAuditLogsByUserAsync(string userId)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    private async Task<string?> ResolveExistingStaffIdAsync(string? userId)
    {
        var trimmed = Truncate(userId, 50);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        var exists = await _context.Staff.AnyAsync(staff => staff.Id == trimmed);
        return exists ? trimmed : null;
    }

    private static string? Truncate(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
        {
            return input;
        }

        return input[..maxLength];
    }
}

