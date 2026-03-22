using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class ConfigService
{
    private readonly ApplicationDbContext _context;

    public ConfigService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ConfigItemDto>> GetConfigAsync()
    {
        return await _context.SystemConfigs
            .Select(c => new ConfigItemDto
            {
                Key = c.Key,
                Value = c.Value
            })
            .ToListAsync();
    }

    public async Task UpdateConfigAsync(List<ConfigItemDto> configItems)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var item in configItems)
            {
                var existingConfig = await _context.SystemConfigs
                    .FirstOrDefaultAsync(c => c.Key == item.Key);

                if (existingConfig != null)
                {
                    existingConfig.Value = item.Value;
                    existingConfig.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var id = $"CFG{(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000).ToString().PadLeft(4, '0')}{new Random().Next(10, 99)}";
                    _context.SystemConfigs.Add(new SystemConfig
                    {
                        Id = id,
                        Key = item.Key,
                        Value = item.Value,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
