using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IUserActivityService
{
    Task LogActivityAsync(string staffId, CreateActivityRequest request, string? ipAddress, string? userAgent, string? sessionId);
    Task<List<UserActivityDto>> GetUserActivitiesAsync(string staffId, int limit = 100);
    Task<List<UserActivityDto>> GetRecentActivitiesAsync(int limit = 100);
    Task<List<UserActivityDto>> GetActivitiesByEntityAsync(string entityType, string entityId, int limit = 100);
    Task<UserActivityReportDto> GetUserActivityReportAsync(string staffId, DateTime? fromDate, DateTime? toDate);
}

public class UserActivityService : IUserActivityService
{
    private readonly ApplicationDbContext _context;

    public UserActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogActivityAsync(string staffId, CreateActivityRequest request, string? ipAddress, string? userAgent, string? sessionId)
    {
        var activity = new UserActivity
        {
            StaffId = staffId,
            Action = request.Action,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            BeforeValue = request.BeforeValue,
            AfterValue = request.AfterValue,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserActivities.Add(activity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserActivityDto>> GetUserActivitiesAsync(string staffId, int limit = 100)
    {
        return await _context.UserActivities
            .Include(a => a.Staff)
            .Where(a => a.StaffId == staffId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new UserActivityDto
            {
                Id = a.Id,
                StaffId = a.StaffId,
                StaffName = a.Staff != null ? a.Staff.Name : "",
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<UserActivityDto>> GetRecentActivitiesAsync(int limit = 100)
    {
        return await _context.UserActivities
            .Include(a => a.Staff)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new UserActivityDto
            {
                Id = a.Id,
                StaffId = a.StaffId,
                StaffName = a.Staff != null ? a.Staff.Name : "",
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<UserActivityDto>> GetActivitiesByEntityAsync(string entityType, string entityId, int limit = 100)
    {
        return await _context.UserActivities
            .Include(a => a.Staff)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new UserActivityDto
            {
                Id = a.Id,
                StaffId = a.StaffId,
                StaffName = a.Staff != null ? a.Staff.Name : "",
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserActivityReportDto> GetUserActivityReportAsync(string staffId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;

        var activities = await _context.UserActivities
            .Include(a => a.Staff)
            .Where(a => a.StaffId == staffId && a.CreatedAt >= from && a.CreatedAt <= to)
            .ToListAsync();

        if (!activities.Any())
        {
            return new UserActivityReportDto
            {
                StaffId = staffId,
                StaffName = "",
                TotalActions = 0,
                ActionCounts = new Dictionary<string, int>(),
                FirstActivity = DateTime.MinValue,
                LastActivity = DateTime.MinValue
            };
        }

        var actionCounts = activities
            .GroupBy(a => a.Action)
            .ToDictionary(g => g.Key, g => g.Count());

        return new UserActivityReportDto
        {
            StaffId = staffId,
            StaffName = activities.First().Staff?.Name ?? "",
            TotalActions = activities.Count,
            ActionCounts = actionCounts,
            FirstActivity = activities.Min(a => a.CreatedAt),
            LastActivity = activities.Max(a => a.CreatedAt)
        };
    }
}
