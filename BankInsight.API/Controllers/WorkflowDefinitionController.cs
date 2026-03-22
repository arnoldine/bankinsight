using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowDefinitionController : ControllerBase
{
    private readonly ProcessDefinitionService _definitionService;
    private readonly Security.ICurrentUserContext _currentUser;
    private readonly ApplicationDbContext _context;

    public WorkflowDefinitionController(
        ProcessDefinitionService definitionService,
        Security.ICurrentUserContext currentUser,
        ApplicationDbContext context)
    {
        _definitionService = definitionService;
        _currentUser = currentUser;
        _context = context;
    }

    [HttpGet]
    [HasPermission("processes.view")]
    public async Task<IActionResult> GetDefinitions()
    {
        var result = await _definitionService.GetAllDefinitionsAsync();
        return Ok(result);
    }

    [HttpGet("{definitionId:guid}")]
    [HasPermission("processes.view")]
    public async Task<IActionResult> GetDefinition(Guid definitionId)
    {
        var definition = await _context.ProcessDefinitions
            .Include(p => p.Versions)
                .ThenInclude(v => v.Steps)
            .Include(p => p.Versions)
                .ThenInclude(v => v.Transitions)
            .FirstOrDefaultAsync(p => p.Id == definitionId);

        if (definition == null)
        {
            return NotFound(new { message = "Workflow definition not found." });
        }

        var result = new
        {
            id = definition.Id,
            code = definition.Code,
            name = definition.Name,
            module = definition.Module,
            entityType = definition.EntityType,
            triggerType = definition.TriggerType,
            triggerEventType = definition.TriggerEventType,
            isSystemProcess = definition.IsSystemProcess,
            isActive = definition.IsActive,
            versions = definition.Versions
                .OrderByDescending(v => v.VersionNo)
                .Select(v => new
                {
                    id = v.Id,
                    processDefinitionId = v.ProcessDefinitionId,
                    versionNo = v.VersionNo,
                    status = v.Status,
                    isPublished = v.IsPublished,
                    notes = v.Notes,
                    createdAtUtc = v.CreatedAtUtc,
                    publishedAtUtc = v.PublishedAtUtc,
                    steps = v.Steps
                        .OrderBy(s => s.OrderNo)
                        .ThenBy(s => s.StepName)
                        .Select(s => new
                        {
                            id = s.Id,
                            processDefinitionVersionId = s.ProcessDefinitionVersionId,
                            stepCode = s.StepCode,
                            stepName = s.StepName,
                            stepType = s.StepType,
                            orderNo = s.OrderNo,
                            isStartStep = s.IsStartStep,
                            isEndStep = s.IsEndStep,
                            assignmentType = s.AssignmentType,
                            assignedRoleCode = s.AssignedRoleCode,
                            assignedPermissionCode = s.AssignedPermissionCode,
                            assignedUserFieldPath = s.AssignedUserFieldPath,
                            slaHours = s.SlaHours,
                            requireMakerCheckerSeparation = s.RequireMakerCheckerSeparation,
                        }),
                    transitions = v.Transitions
                        .OrderBy(t => t.TransitionName)
                        .Select(t => new
                        {
                            id = t.Id,
                            processDefinitionVersionId = t.ProcessDefinitionVersionId,
                            fromStepId = t.FromStepId,
                            toStepId = t.ToStepId,
                            transitionName = t.TransitionName,
                            conditionRuleCode = t.ConditionRuleCode,
                            requiredOutcome = t.RequiredOutcome,
                            isDefault = t.IsDefault,
                        }),
                }),
        };

        return Ok(result);
    }

    [HttpPost]
    [HasPermission("processes.manage")]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateProcessDefinitionRequest request)
    {
        var result = await _definitionService.CreateProcessDefinitionAsync(request, _currentUser.UserId);
        return CreatedAtAction(nameof(GetDefinitions), new { id = result.Id }, result);
    }

    [HttpPost("{definitionId:guid}/versions")]
    [HasPermission("processes.manage")]
    public async Task<IActionResult> CreateDraftVersion(Guid definitionId)
    {
        var definition = await _context.ProcessDefinitions
            .Include(p => p.Versions)
                .ThenInclude(v => v.Steps)
            .Include(p => p.Versions)
                .ThenInclude(v => v.Transitions)
            .FirstOrDefaultAsync(p => p.Id == definitionId);

        if (definition == null)
        {
            return NotFound(new { message = "Workflow definition not found." });
        }

        var sourceVersion = definition.Versions
            .OrderByDescending(v => v.VersionNo)
            .FirstOrDefault();

        if (sourceVersion == null)
        {
            return BadRequest(new { message = "Workflow definition has no source version to clone." });
        }

        var nextVersion = new ProcessDefinitionVersion
        {
            Id = Guid.NewGuid(),
            ProcessDefinitionId = definition.Id,
            VersionNo = sourceVersion.VersionNo + 1,
            Status = "Draft",
            IsPublished = false,
            Notes = $"Draft cloned from version {sourceVersion.VersionNo}",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
        };

        var stepMap = new Dictionary<Guid, Guid>();
        var clonedSteps = sourceVersion.Steps.Select(step =>
        {
            var nextId = Guid.NewGuid();
            stepMap[step.Id] = nextId;
            return new ProcessStepDefinition
            {
                Id = nextId,
                ProcessDefinitionVersionId = nextVersion.Id,
                StepCode = step.StepCode,
                StepName = step.StepName,
                StepType = step.StepType,
                OrderNo = step.OrderNo,
                IsStartStep = step.IsStartStep,
                IsEndStep = step.IsEndStep,
                AssignmentType = step.AssignmentType,
                AssignedRoleCode = step.AssignedRoleCode,
                AssignedPermissionCode = step.AssignedPermissionCode,
                AssignedUserFieldPath = step.AssignedUserFieldPath,
                SlaHours = step.SlaHours,
                RequireMakerCheckerSeparation = step.RequireMakerCheckerSeparation,
                AutoActionConfigJson = step.AutoActionConfigJson,
            };
        }).ToList();

        var clonedTransitions = sourceVersion.Transitions.Select(transition => new ProcessTransitionDefinition
        {
            Id = Guid.NewGuid(),
            ProcessDefinitionVersionId = nextVersion.Id,
            FromStepId = stepMap[transition.FromStepId],
            ToStepId = stepMap[transition.ToStepId],
            TransitionName = transition.TransitionName,
            ConditionRuleCode = transition.ConditionRuleCode,
            RequiredOutcome = transition.RequiredOutcome,
            IsDefault = transition.IsDefault,
        }).ToList();

        _context.ProcessDefinitionVersions.Add(nextVersion);
        _context.ProcessStepDefinitions.AddRange(clonedSteps);
        _context.ProcessTransitionDefinitions.AddRange(clonedTransitions);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = nextVersion.Id,
            processDefinitionId = nextVersion.ProcessDefinitionId,
            versionNo = nextVersion.VersionNo,
            status = nextVersion.Status,
            isPublished = nextVersion.IsPublished,
            createdAtUtc = nextVersion.CreatedAtUtc,
        });
    }

    [HttpPost("versions/{versionId}/publish")]
    [HasPermission("processes.publish")]
    public async Task<IActionResult> PublishVersion(Guid versionId)
    {
        await _definitionService.PublishVersionAsync(versionId, _currentUser.UserId);
        return Ok(new { message = "Workflow version published successfully." });
    }

    [HttpPost("versions/{versionId}/steps")]
    [HasPermission("processes.manage")]
    public async Task<IActionResult> AddStep(Guid versionId, [FromBody] CreateProcessStepRequest request)
    {
        var result = await _definitionService.AddStepAsync(versionId, request);
        return Ok(result);
    }

    [HttpPost("versions/{versionId}/transitions")]
    [HasPermission("processes.manage")]
    public async Task<IActionResult> AddTransition(Guid versionId, [FromBody] CreateProcessTransitionRequest request)
    {
        var result = await _definitionService.AddTransitionAsync(versionId, request);
        return Ok(result);
    }

    [HttpGet("versions/{versionId}/validate")]
    [HasPermission("processes.manage")]
    public async Task<IActionResult> ValidateVersion(Guid versionId)
    {
        var result = await _definitionService.ValidateVersionAsync(versionId);
        return Ok(result);
    }
}
