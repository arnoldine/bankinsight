using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IBranchConfigService
{
    Task<BranchConfigDto> GetOrCreateConfigAsync(string branchId, string configKey);
    Task<BranchConfigDto> UpdateConfigAsync(UpdateBranchConfigRequest request);
    Task<List<BranchConfigDto>> GetBranchConfigsAsync(string branchId);
    Task<List<BranchConfigDto>> GetAllConfigsAsync();
    Task<string?> GetConfigValueAsync(string branchId, string configKey);
    Task<bool> DeleteConfigAsync(int id);
}

public class BranchConfigService : IBranchConfigService
{
    private readonly ApplicationDbContext _context;

    public BranchConfigService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BranchConfigDto> GetOrCreateConfigAsync(string branchId, string configKey)
    {
        var config = await _context.BranchConfigs
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.BranchId == branchId && c.ConfigKey == configKey);

        if (config == null)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null)
            {
                throw new Exception("Branch not found");
            }

            config = new BranchConfig
            {
                BranchId = branchId,
                ConfigKey = configKey,
                DataType = "string",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BranchConfigs.Add(config);
            await _context.SaveChangesAsync();

            config = await _context.BranchConfigs
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.Id == config.Id);
        }

        return MapToDto(config!);
    }

    public async Task<BranchConfigDto> UpdateConfigAsync(UpdateBranchConfigRequest request)
    {
        var config = await _context.BranchConfigs
            .FirstOrDefaultAsync(c => c.BranchId == request.BranchId && c.ConfigKey == request.ConfigKey);

        if (config == null)
        {
            var branch = await _context.Branches.FindAsync(request.BranchId);
            if (branch == null)
            {
                throw new Exception("Branch not found");
            }

            config = new BranchConfig
            {
                BranchId = request.BranchId,
                ConfigKey = request.ConfigKey,
                ConfigValue = request.ConfigValue,
                DataType = request.DataType,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BranchConfigs.Add(config);
        }
        else
        {
            config.ConfigValue = request.ConfigValue;
            config.DataType = request.DataType;
            config.Description = request.Description;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await GetConfigDtoAsync(config.Id);
    }

    public async Task<List<BranchConfigDto>> GetBranchConfigsAsync(string branchId)
    {
        var configs = await _context.BranchConfigs
            .Include(c => c.Branch)
            .Where(c => c.BranchId == branchId)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    public async Task<List<BranchConfigDto>> GetAllConfigsAsync()
    {
        var configs = await _context.BranchConfigs
            .Include(c => c.Branch)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    public async Task<string?> GetConfigValueAsync(string branchId, string configKey)
    {
        var config = await _context.BranchConfigs
            .FirstOrDefaultAsync(c => c.BranchId == branchId && c.ConfigKey == configKey);

        return config?.ConfigValue;
    }

    public async Task<bool> DeleteConfigAsync(int id)
    {
        var config = await _context.BranchConfigs.FindAsync(id);
        if (config == null)
        {
            return false;
        }

        _context.BranchConfigs.Remove(config);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<BranchConfigDto> GetConfigDtoAsync(int id)
    {
        var config = await _context.BranchConfigs
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (config == null)
        {
            throw new Exception("Config not found");
        }

        return MapToDto(config);
    }

    private BranchConfigDto MapToDto(BranchConfig config)
    {
        return new BranchConfigDto
        {
            Id = config.Id,
            BranchId = config.BranchId,
            BranchCode = config.Branch?.Code ?? "",
            BranchName = config.Branch?.Name ?? "",
            ConfigKey = config.ConfigKey,
            ConfigValue = config.ConfigValue,
            DataType = config.DataType,
            Description = config.Description
        };
    }
}
