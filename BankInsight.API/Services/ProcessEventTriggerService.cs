using System;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class ProcessEventTriggerService
{
    private readonly ApplicationDbContext _context;
    private readonly ProcessRuntimeService _runtimeService;

    public ProcessEventTriggerService(ApplicationDbContext context, ProcessRuntimeService runtimeService)
    {
        _context = context;
        _runtimeService = runtimeService;
    }

    public async Task TriggerEventAsync(string eventType, string entityType, string entityId, string userId, string? payloadJson)
    {
        // Finding standard event-triggered processes
        var eventDefinitions = await _context.ProcessDefinitions
            .Where(d => d.IsActive && d.TriggerType == "Event" && d.TriggerEventType == eventType)
            .ToListAsync();

        foreach (var def in eventDefinitions)
        {
            var req = new StartProcessRequest
            {
                EntityType = entityType,
                EntityId = entityId,
                CorrelationId = $"EVT-{eventType}-{DateTime.UtcNow.Ticks}",
                PayloadJson = payloadJson
            };

            await _runtimeService.StartProcessAsync(req, userId, def.Code);
        }
        
        // Finding explicitly subscribed triggers
        var activeSubscriptions = await _context.ProcessEventSubscriptions
            .Include(s => s.ProcessDefinition)
            .Where(s => s.EventType == eventType && s.IsActive)
            .ToListAsync();

        foreach (var sub in activeSubscriptions)
        {
            if (!sub.ProcessDefinition.IsActive || eventDefinitions.Any(d => d.Id == sub.ProcessDefinitionId)) continue; // avoid double firing

            var req = new StartProcessRequest
            {
                EntityType = entityType,
                EntityId = entityId,
                CorrelationId = $"SUB-{eventType}-{DateTime.UtcNow.Ticks}",
                PayloadJson = payloadJson
            };

            await _runtimeService.StartProcessAsync(req, userId, sub.ProcessDefinition.Code);
        }
    }
}
