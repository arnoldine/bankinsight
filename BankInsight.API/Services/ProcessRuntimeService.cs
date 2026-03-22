using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class ProcessRuntimeService
{
    private readonly ApplicationDbContext _context;
    private readonly ProcessAssignmentService _assignmentService;

    public ProcessRuntimeService(ApplicationDbContext context, ProcessAssignmentService assignmentService)
    {
        _context = context;
        _assignmentService = assignmentService;
    }

    public async Task<ProcessInstance> StartProcessAsync(StartProcessRequest request, string userId, string? processCode = null)
    {
        var publishedVersion = await GetPublishedVersionAsync(processCode, request.EntityType);
        if (publishedVersion == null) throw new InvalidOperationException("No published process found.");

        var startStep = publishedVersion.Steps.FirstOrDefault(s => s.IsStartStep);
        if (startStep == null) throw new InvalidOperationException("Process definition missing start step.");

        var instance = new ProcessInstance
        {
            Id = Guid.NewGuid(),
            ProcessDefinitionVersionId = publishedVersion.Id,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            CurrentStepId = startStep.Id,
            Status = "Running",
            CorrelationId = request.CorrelationId,
            StartedByUserId = userId,
            StartedAtUtc = DateTime.UtcNow
        };

        _context.ProcessInstances.Add(instance);

        _context.ProcessInstanceHistories.Add(new ProcessInstanceHistory
        {
            Id = Guid.NewGuid(),
            ProcessInstanceId = instance.Id,
            ActionType = "Started",
            ActionByUserId = userId,
            ActionAtUtc = DateTime.UtcNow,
            PayloadJson = request.PayloadJson
        });

        await _context.SaveChangesAsync();

        // Immediately attempt to progress from the start node
        await MoveToNextStepAsync(instance, startStep, null, request.PayloadJson);

        return instance;
    }

    public async Task MoveToNextStepAsync(ProcessInstance instance, ProcessStepDefinition currentStep, string? outcome, string? payloadJson)
    {
        var version = await _context.ProcessDefinitionVersions
            .Include(v => v.Steps)
            .Include(v => v.Transitions)
            .FirstOrDefaultAsync(v => v.Id == instance.ProcessDefinitionVersionId);

        var nextTransition = version!.Transitions.FirstOrDefault(t => 
            t.FromStepId == currentStep.Id && 
            (string.IsNullOrEmpty(t.RequiredOutcome) || t.RequiredOutcome == outcome)
            && !t.IsDefault);

        if (nextTransition == null)
        {
            nextTransition = version.Transitions.FirstOrDefault(t => t.FromStepId == currentStep.Id && t.IsDefault);
        }

        if (nextTransition == null && currentStep.IsEndStep)
        {
            instance.Status = outcome == "Reject" ? "Rejected" : "Completed";
            instance.CompletedAtUtc = DateTime.UtcNow;

            _context.ProcessInstanceHistories.Add(new ProcessInstanceHistory
            {
                Id = Guid.NewGuid(),
                ProcessInstanceId = instance.Id,
                ActionType = "Completed",
                ToStepCode = currentStep.StepCode,
                ActionAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return;
        }

        if (nextTransition == null) throw new InvalidOperationException($"No valid transition found from step {currentStep.StepCode} with outcome {outcome}");

        var nextStep = version.Steps.First(s => s.Id == nextTransition.ToStepId);
        instance.CurrentStepId = nextStep.Id;

        _context.ProcessInstanceHistories.Add(new ProcessInstanceHistory
        {
            Id = Guid.NewGuid(),
            ProcessInstanceId = instance.Id,
            ActionType = "Transitioned",
            FromStepCode = currentStep.StepCode,
            ToStepCode = nextStep.StepCode,
            Outcome = outcome,
            ActionAtUtc = DateTime.UtcNow
        });

        if (nextStep.StepType == "SystemTask" || nextStep.StepType == "Decision")
        {
            // Auto progress for now
            await _context.SaveChangesAsync();
            await MoveToNextStepAsync(instance, nextStep, "Complete", payloadJson);
            return;
        }
        else if (nextStep.StepType == "UserTask" || nextStep.StepType == "ApprovalTask")
        {
            var assignedUserId = await _assignmentService.ResolveAssigneeUserAsync(
                nextStep.AssignmentType, nextStep.AssignedUserFieldPath, payloadJson);

            var task = new ProcessTask
            {
                Id = Guid.NewGuid(),
                ProcessInstanceId = instance.Id,
                ProcessStepDefinitionId = nextStep.Id,
                AssignedUserId = assignedUserId,
                AssignedRoleCode = nextStep.AssignedRoleCode,
                AssignedPermissionCode = nextStep.AssignedPermissionCode,
                Status = assignedUserId != null ? "Pending" : "Claimable",
                CreatedAtUtc = DateTime.UtcNow,
                DueAtUtc = nextStep.SlaHours.HasValue ? DateTime.UtcNow.AddHours(nextStep.SlaHours.Value) : null
            };

            _context.ProcessTasks.Add(task);
            
            _context.ProcessInstanceHistories.Add(new ProcessInstanceHistory
            {
                Id = Guid.NewGuid(),
                ProcessInstanceId = instance.Id,
                ActionType = "TaskCreated",
                ToStepCode = nextStep.StepCode,
                ActionAtUtc = DateTime.UtcNow
            });
        }
        else if (nextStep.IsEndStep)
        {
            instance.Status = outcome == "Reject" ? "Rejected" : "Completed";
            instance.CompletedAtUtc = DateTime.UtcNow;
            
            _context.ProcessInstanceHistories.Add(new ProcessInstanceHistory
            {
                Id = Guid.NewGuid(),
                ProcessInstanceId = instance.Id,
                ActionType = "Completed",
                ToStepCode = nextStep.StepCode,
                ActionAtUtc = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task<ProcessDefinitionVersion?> GetPublishedVersionAsync(string? processCode, string? entityType)
    {
        var query = _context.ProcessDefinitionVersions
            .Include(v => v.ProcessDefinition)
            .Include(v => v.Steps)
            .Where(v => v.IsPublished);

        if (!string.IsNullOrEmpty(processCode))
        {
            query = query.Where(v => v.ProcessDefinition.Code == processCode);
        }
        else if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(v => v.ProcessDefinition.EntityType == entityType);
        }
        
        return await query.FirstOrDefaultAsync();
    }
}
