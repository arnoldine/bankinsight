using System.Security.Claims;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class WorkspacePreferencesService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkspacePreferencesService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkspacePreferencesSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var staffId = GetStaffId();

        var favorites = await _context.WorkspacePreferences
            .AsNoTracking()
            .Where(x => x.StaffId == staffId && x.IsFavorite)
            .OrderByDescending(x => x.IsPinned)
            .ThenBy(x => x.WorkspaceKey)
            .Select(x => new WorkspaceFavoriteDto
            {
                WorkspaceKey = x.WorkspaceKey,
                Route = x.Route,
                IsPinned = x.IsPinned
            })
            .ToListAsync(cancellationToken);

        var savedViews = await _context.WorkspacePreferences
            .AsNoTracking()
            .Where(x => x.StaffId == staffId && x.ViewName != null)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAt)
            .Select(x => new WorkspaceSavedViewDto
            {
                Id = x.Id,
                WorkspaceKey = x.WorkspaceKey,
                ViewName = x.ViewName ?? x.WorkspaceKey,
                Route = x.Route,
                FilterJson = x.FilterJson,
                IsDefault = x.IsDefault,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new WorkspacePreferencesSummaryDto
        {
            Favorites = favorites,
            SavedViews = savedViews
        };
    }

    public async Task UpsertFavoriteAsync(string workspaceKey, UpsertWorkspaceFavoriteRequest request, CancellationToken cancellationToken = default)
    {
        var staffId = GetStaffId();
        var existing = await _context.WorkspacePreferences
            .FirstOrDefaultAsync(x => x.StaffId == staffId && x.WorkspaceKey == workspaceKey && x.ViewName == null, cancellationToken);

        if (existing is null)
        {
            existing = new WorkspacePreference
            {
                StaffId = staffId,
                WorkspaceKey = workspaceKey
            };
            _context.WorkspacePreferences.Add(existing);
        }

        existing.Route = request.Route;
        existing.IsFavorite = true;
        existing.IsPinned = request.IsPinned;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFavoriteAsync(string workspaceKey, CancellationToken cancellationToken = default)
    {
        var staffId = GetStaffId();
        var existing = await _context.WorkspacePreferences
            .FirstOrDefaultAsync(x => x.StaffId == staffId && x.WorkspaceKey == workspaceKey && x.ViewName == null, cancellationToken);

        if (existing is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(existing.ViewName))
        {
            existing.IsFavorite = false;
            existing.IsPinned = false;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.WorkspacePreferences.Remove(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkspaceSavedViewDto> SaveViewAsync(SaveWorkspaceViewRequest request, CancellationToken cancellationToken = default)
    {
        var staffId = GetStaffId();
        if (request.IsDefault)
        {
            var existingDefaults = await _context.WorkspacePreferences
                .Where(x => x.StaffId == staffId && x.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

        var entity = new WorkspacePreference
        {
            StaffId = staffId,
            WorkspaceKey = request.WorkspaceKey,
            ViewName = request.ViewName,
            Route = request.Route,
            FilterJson = request.FilterJson,
            IsDefault = request.IsDefault,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkspacePreferences.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkspaceSavedViewDto
        {
            Id = entity.Id,
            WorkspaceKey = entity.WorkspaceKey,
            ViewName = entity.ViewName ?? entity.WorkspaceKey,
            Route = entity.Route,
            FilterJson = entity.FilterJson,
            IsDefault = entity.IsDefault,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task DeleteViewAsync(string id, CancellationToken cancellationToken = default)
    {
        var staffId = GetStaffId();
        var existing = await _context.WorkspacePreferences
            .FirstOrDefaultAsync(x => x.StaffId == staffId && x.Id == id, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _context.WorkspacePreferences.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private string GetStaffId()
        => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? "system";
}
