using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class WorkflowService
{
    private readonly ApplicationDbContext _context;

    public WorkflowService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Workflow>> GetWorkflowsAsync()
    {
        return await _context.Workflows.ToListAsync();
    }

    public async Task<Workflow> CreateWorkflowAsync(CreateWorkflowRequest request)
    {
        var id = $"WF{(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000).ToString().PadLeft(4, '0')}";

        var workflow = new Workflow
        {
            Id = id,
            Name = request.Name,
            TriggerType = request.TriggerType,
            Steps = JsonSerializer.Serialize(request.Steps),
            Status = string.IsNullOrEmpty(request.Status) ? "ACTIVE" : request.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        return workflow;
    }

    public async Task<Workflow?> UpdateWorkflowAsync(string id, UpdateWorkflowRequest request)
    {
        var workflow = await _context.Workflows.FindAsync(id);
        if (workflow == null) return null;

        workflow.Name = request.Name;
        workflow.TriggerType = request.TriggerType;
        workflow.Steps = JsonSerializer.Serialize(request.Steps);
        workflow.Status = request.Status ?? workflow.Status;

        await _context.SaveChangesAsync();
        return workflow;
    }
}
