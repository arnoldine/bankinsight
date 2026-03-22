using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class ProcessDefinitionService
{
    private readonly ApplicationDbContext _context;

    public ProcessDefinitionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProcessDefinitionDto> CreateProcessDefinitionAsync(CreateProcessDefinitionRequest request, string userId)
    {
        var exists = await _context.ProcessDefinitions.AnyAsync(p => p.Code == request.Code);
        if (exists) throw new InvalidOperationException($"Process Definition with code {request.Code} already exists.");

        var id = Guid.NewGuid();
        var def = new ProcessDefinition
        {
            Id = id,
            Code = request.Code,
            Name = request.Name,
            Module = request.Module,
            EntityType = request.EntityType,
            TriggerType = request.TriggerType,
            TriggerEventType = request.TriggerEventType,
            IsSystemProcess = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        var initialVersion = new ProcessDefinitionVersion
        {
            Id = Guid.NewGuid(),
            ProcessDefinitionId = id,
            VersionNo = 1,
            Status = "Draft",
            IsPublished = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.ProcessDefinitions.Add(def);
        _context.ProcessDefinitionVersions.Add(initialVersion);
        await _context.SaveChangesAsync();

        return MapDto(def);
    }

    public async Task<List<ProcessDefinitionDto>> GetAllDefinitionsAsync()
    {
        return await _context.ProcessDefinitions
            .OrderBy(p => p.Name)
            .Select(p => new ProcessDefinitionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                EntityType = p.EntityType,
                TriggerType = p.TriggerType,
                TriggerEventType = p.TriggerEventType,
                IsSystemProcess = p.IsSystemProcess,
                IsActive = p.IsActive
            })
            .ToListAsync();
    }

    public async Task<ProcessValidationResultDto> ValidateVersionAsync(Guid versionId)
    {
        var version = await _context.ProcessDefinitionVersions
            .Include(v => v.Steps)
            .Include(v => v.Transitions)
            .FirstOrDefaultAsync(v => v.Id == versionId);

        var result = new ProcessValidationResultDto { IsValid = true };
        
        if (version == null) 
        {
            result.IsValid = false;
            result.Errors.Add("Version not found.");
            return result;
        }

        var startSteps = version.Steps.Where(s => s.IsStartStep).ToList();
        if (startSteps.Count != 1)
        {
            result.IsValid = false;
            result.Errors.Add($"Expected exactly 1 start step, found {startSteps.Count}.");
        }

        var endSteps = version.Steps.Where(s => s.IsEndStep).ToList();
        if (!endSteps.Any())
        {
            result.IsValid = false;
            result.Errors.Add("At least 1 end step is required.");
        }

        foreach (var step in version.Steps)
        {
            if (!step.IsEndStep && !version.Transitions.Any(t => t.FromStepId == step.Id))
            {
                result.IsValid = false;
                result.Errors.Add($"Step '{step.StepName}' must have at least one outgoing transition.");
            }

            if (!step.IsStartStep && !version.Transitions.Any(t => t.ToStepId == step.Id))
            {
                result.IsValid = false;
                result.Errors.Add($"Step '{step.StepName}' is orphaned (no incoming transitions).");
            }

            if (step.StepType == "ApprovalTask" || step.StepType == "UserTask")
            {
                if (string.IsNullOrWhiteSpace(step.AssignmentType))
                {
                    result.IsValid = false;
                    result.Errors.Add($"User/Approval task '{step.StepName}' is missing valid assignment type.");
                }
            }
        }

        foreach(var t in version.Transitions)
        {
            if(!version.Steps.Any(s => s.Id == t.FromStepId) || !version.Steps.Any(s => s.Id == t.ToStepId))
            {
                result.IsValid = false;
                result.Errors.Add($"Transition '{t.TransitionName}' references invalid steps.");
            }
        }

        return result;
    }

    public async Task PublishVersionAsync(Guid versionId, string userId)
    {
        var validation = await ValidateVersionAsync(versionId);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("Process version failed validation: " + string.Join("; ", validation.Errors));
        }

        var version = await _context.ProcessDefinitionVersions.FindAsync(versionId);
        if (version == null || version.Status != "Draft")
        {
            throw new InvalidOperationException("Only draft versions can be published.");
        }

        // Archive previously published version
        var publishedVersions = await _context.ProcessDefinitionVersions
            .Where(v => v.ProcessDefinitionId == version.ProcessDefinitionId && v.IsPublished)
            .ToListAsync();
            
        foreach(var old in publishedVersions)
        {
            old.IsPublished = false;
            old.Status = "Archived";
        }

        version.IsPublished = true;
        version.Status = "Published";
        version.PublishedAtUtc = DateTime.UtcNow;
        version.PublishedByUserId = userId;

        await _context.SaveChangesAsync();
    }

    // Example methods for adding steps and transitions
    public async Task<ProcessStepDto> AddStepAsync(Guid versionId, CreateProcessStepRequest request)
    {
        var step = new ProcessStepDefinition
        {
            Id = Guid.NewGuid(),
            ProcessDefinitionVersionId = versionId,
            StepCode = request.StepCode,
            StepName = request.StepName,
            StepType = request.StepType,
            OrderNo = request.OrderNo,
            IsStartStep = request.IsStartStep,
            IsEndStep = request.IsEndStep,
            AssignmentType = request.AssignmentType,
            AssignedRoleCode = request.AssignedRoleCode,
            AssignedPermissionCode = request.AssignedPermissionCode,
            AssignedUserFieldPath = request.AssignedUserFieldPath,
            SlaHours = request.SlaHours,
            RequireMakerCheckerSeparation = request.RequireMakerCheckerSeparation
        };

        _context.ProcessStepDefinitions.Add(step);
        await _context.SaveChangesAsync();
        
        return new ProcessStepDto 
        {
            Id = step.Id,
            ProcessDefinitionVersionId = step.ProcessDefinitionVersionId,
            StepCode = step.StepCode,
            StepName = step.StepName,
            StepType = step.StepType
        };
    }

    public async Task<ProcessTransitionDto> AddTransitionAsync(Guid versionId, CreateProcessTransitionRequest request)
    {
        var transition = new ProcessTransitionDefinition
        {
            Id = Guid.NewGuid(),
            ProcessDefinitionVersionId = versionId,
            FromStepId = request.FromStepId,
            ToStepId = request.ToStepId,
            TransitionName = request.TransitionName,
            ConditionRuleCode = request.ConditionRuleCode,
            RequiredOutcome = request.RequiredOutcome,
            IsDefault = request.IsDefault
        };

        _context.ProcessTransitionDefinitions.Add(transition);
        await _context.SaveChangesAsync();

        return new ProcessTransitionDto 
        {
            Id = transition.Id,
            ProcessDefinitionVersionId = transition.ProcessDefinitionVersionId,
            FromStepId = transition.FromStepId,
            ToStepId = transition.ToStepId,
            TransitionName = transition.TransitionName
        };
    }

    private static ProcessDefinitionDto MapDto(ProcessDefinition p)
    {
        return new ProcessDefinitionDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Module = p.Module,
            EntityType = p.EntityType,
            TriggerType = p.TriggerType,
            TriggerEventType = p.TriggerEventType,
            IsSystemProcess = p.IsSystemProcess,
            IsActive = p.IsActive
        };
    }
}
