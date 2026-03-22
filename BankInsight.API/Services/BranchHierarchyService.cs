using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IBranchHierarchyService
{
    Task<BranchHierarchyDto> CreateHierarchyAsync(CreateBranchHierarchyRequest request);
    Task<BranchHierarchyDto?> GetBranchHierarchyAsync(string branchId);
    Task<List<BranchHierarchyDto>> GetAllHierarchiesAsync();
    Task<List<BranchHierarchyDto>> GetBranchTreeAsync();
    Task<List<BranchHierarchyDto>> GetChildBranchesAsync(string branchId);
    Task<bool> DeleteHierarchyAsync(int id);
}

public class BranchHierarchyService : IBranchHierarchyService
{
    private readonly ApplicationDbContext _context;

    public BranchHierarchyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BranchHierarchyDto> CreateHierarchyAsync(CreateBranchHierarchyRequest request)
    {
        var branch = await _context.Branches.FindAsync(request.BranchId);
        if (branch == null)
        {
            throw new Exception("Branch not found");
        }

        int level = 0;
        string path = request.BranchId;

        if (!string.IsNullOrEmpty(request.ParentBranchId))
        {
            var parentHierarchy = await _context.BranchHierarchies
                .FirstOrDefaultAsync(h => h.BranchId == request.ParentBranchId);

            if (parentHierarchy != null)
            {
                level = parentHierarchy.Level + 1;
                path = $"{parentHierarchy.Path}/{request.BranchId}";
            }
        }

        var hierarchy = new BranchHierarchy
        {
            BranchId = request.BranchId,
            ParentBranchId = request.ParentBranchId,
            Level = level,
            Path = path,
            CreatedAt = DateTime.UtcNow
        };

        _context.BranchHierarchies.Add(hierarchy);
        await _context.SaveChangesAsync();

        return await GetBranchHierarchyDtoAsync(hierarchy.Id);
    }

    public async Task<BranchHierarchyDto?> GetBranchHierarchyAsync(string branchId)
    {
        var hierarchy = await _context.BranchHierarchies
            .Include(h => h.Branch)
            .Include(h => h.ParentBranch)
            .FirstOrDefaultAsync(h => h.BranchId == branchId);

        if (hierarchy == null)
        {
            return null;
        }

        return new BranchHierarchyDto
        {
            Id = hierarchy.Id,
            BranchId = hierarchy.BranchId,
            BranchCode = hierarchy.Branch?.Code ?? "",
            BranchName = hierarchy.Branch?.Name ?? "",
            ParentBranchId = hierarchy.ParentBranchId,
            ParentBranchCode = hierarchy.ParentBranch?.Code,
            ParentBranchName = hierarchy.ParentBranch?.Name,
            Level = hierarchy.Level,
            Path = hierarchy.Path
        };
    }

    public async Task<List<BranchHierarchyDto>> GetAllHierarchiesAsync()
    {
        return await _context.BranchHierarchies
            .Include(h => h.Branch)
            .Include(h => h.ParentBranch)
            .Select(h => new BranchHierarchyDto
            {
                Id = h.Id,
                BranchId = h.BranchId,
                BranchCode = h.Branch != null ? h.Branch.Code : "",
                BranchName = h.Branch != null ? h.Branch.Name : "",
                ParentBranchId = h.ParentBranchId,
                ParentBranchCode = h.ParentBranch != null ? h.ParentBranch.Code : null,
                ParentBranchName = h.ParentBranch != null ? h.ParentBranch.Name : null,
                Level = h.Level,
                Path = h.Path
            })
            .OrderBy(h => h.Level)
            .ThenBy(h => h.BranchCode)
            .ToListAsync();
    }

    public async Task<List<BranchHierarchyDto>> GetBranchTreeAsync()
    {
        var allHierarchies = await GetAllHierarchiesAsync();
        var rootNodes = allHierarchies.Where(h => h.ParentBranchId == null).ToList();

        foreach (var root in rootNodes)
        {
            await PopulateChildrenAsync(root, allHierarchies);
        }

        return rootNodes;
    }

    public async Task<List<BranchHierarchyDto>> GetChildBranchesAsync(string branchId)
    {
        return await _context.BranchHierarchies
            .Include(h => h.Branch)
            .Include(h => h.ParentBranch)
            .Where(h => h.ParentBranchId == branchId)
            .Select(h => new BranchHierarchyDto
            {
                Id = h.Id,
                BranchId = h.BranchId,
                BranchCode = h.Branch != null ? h.Branch.Code : "",
                BranchName = h.Branch != null ? h.Branch.Name : "",
                ParentBranchId = h.ParentBranchId,
                ParentBranchCode = h.ParentBranch != null ? h.ParentBranch.Code : null,
                ParentBranchName = h.ParentBranch != null ? h.ParentBranch.Name : null,
                Level = h.Level,
                Path = h.Path
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteHierarchyAsync(int id)
    {
        var hierarchy = await _context.BranchHierarchies.FindAsync(id);
        if (hierarchy == null)
        {
            return false;
        }

        _context.BranchHierarchies.Remove(hierarchy);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<BranchHierarchyDto> GetBranchHierarchyDtoAsync(int id)
    {
        var hierarchy = await _context.BranchHierarchies
            .Include(h => h.Branch)
            .Include(h => h.ParentBranch)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hierarchy == null)
        {
            throw new Exception("Hierarchy not found");
        }

        return new BranchHierarchyDto
        {
            Id = hierarchy.Id,
            BranchId = hierarchy.BranchId,
            BranchCode = hierarchy.Branch?.Code ?? "",
            BranchName = hierarchy.Branch?.Name ?? "",
            ParentBranchId = hierarchy.ParentBranchId,
            ParentBranchCode = hierarchy.ParentBranch?.Code,
            ParentBranchName = hierarchy.ParentBranch?.Name,
            Level = hierarchy.Level,
            Path = hierarchy.Path
        };
    }

    private async Task PopulateChildrenAsync(BranchHierarchyDto parent, List<BranchHierarchyDto> allHierarchies)
    {
        parent.Children = allHierarchies.Where(h => h.ParentBranchId == parent.BranchId).ToList();
        
        foreach (var child in parent.Children)
        {
            await PopulateChildrenAsync(child, allHierarchies);
        }
    }
}
