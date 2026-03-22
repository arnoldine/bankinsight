using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class ProcessTaskService
{
    private readonly ApplicationDbContext _context;
    private readonly ProcessRuntimeService _runtimeService;

    public ProcessTaskService(ApplicationDbContext context, ProcessRuntimeService runtimeService)
    {
        _context = context;
        _runtimeService = runtimeService;
    }

    public async Task<List<ProcessTask>> GetMyTasksAsync(string userId)
    {
        return await _context.ProcessTasks
            .Include(t => t.ProcessInstance)
                .ThenInclude(i => i.ProcessDefinitionVersion)
                    .ThenInclude(v => v.ProcessDefinition)
            .Include(t => t.ProcessStepDefinition)
            .Where(t => t.AssignedUserId == userId && (t.Status == "Pending" || t.Status == "Claimed" || t.Status == "Overdue"))
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<List<ProcessTask>> GetClaimableTasksAsync(List<string> userRoles, List<string> userPermissions)
    {
        return await _context.ProcessTasks
            .Include(t => t.ProcessInstance)
                .ThenInclude(i => i.ProcessDefinitionVersion)
                    .ThenInclude(v => v.ProcessDefinition)
            .Include(t => t.ProcessStepDefinition)
            .Where(t => t.Status == "Claimable" && 
                 ((t.AssignedRoleCode != null && userRoles.Contains(t.AssignedRoleCode)) ||
                  (t.AssignedPermissionCode != null && userPermissions.Contains(t.AssignedPermissionCode))))
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<ProcessTask?> GetAccessibleTaskAsync(Guid taskId, string userId, List<string> userRoles, List<string> userPermissions)
    {
        return await _context.ProcessTasks
            .Include(t => t.ProcessInstance)
                .ThenInclude(i => i.ProcessDefinitionVersion)
                    .ThenInclude(v => v.ProcessDefinition)
            .Include(t => t.ProcessInstance)
                .ThenInclude(i => i.History)
            .Include(t => t.ProcessStepDefinition)
            .FirstOrDefaultAsync(t =>
                t.Id == taskId &&
                (
                    t.AssignedUserId == userId ||
                    (
                        t.Status == "Claimable" &&
                        (
                            (t.AssignedRoleCode != null && userRoles.Contains(t.AssignedRoleCode)) ||
                            (t.AssignedPermissionCode != null && userPermissions.Contains(t.AssignedPermissionCode))
                        )
                    )
                ));
    }

    public async Task ClaimTaskAsync(Guid taskId, string userId)
    {
        var task = await _context.ProcessTasks.FindAsync(taskId);
        if (task == null || task.Status != "Claimable") throw new InvalidOperationException("Task is not claimable.");

        task.Status = "Claimed";
        task.ClaimedByUserId = userId;
        task.ClaimedAtUtc = DateTime.UtcNow;
        task.AssignedUserId = userId; // explicitly assign to caller

        _context.ProcessInstanceHistories.Add(new ProcessInstanceHistory
        {
            Id = Guid.NewGuid(),
            ProcessInstanceId = task.ProcessInstanceId,
            ActionType = "TaskClaimed",
            ActionByUserId = userId,
            ActionAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task CompleteTaskAsync(Guid taskId, string userId, CompleteTaskRequest request)
    {
        var task = await _context.ProcessTasks
            .Include(t => t.ProcessStepDefinition)
            .Include(t => t.ProcessInstance)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null || (task.Status != "Pending" && task.Status != "Claimed" && task.Status != "Overdue"))
        {
            throw new InvalidOperationException("Task is not active or cannot be completed.");
        }

        if (task.AssignedUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not assigned to this task.");
        }

        // Maker-Checker enforcement
        if (task.ProcessStepDefinition.RequireMakerCheckerSeparation)
        {
            if (task.ProcessInstance.StartedByUserId == userId)
            {
                throw new InvalidOperationException("Maker-checker violation: Initiator cannot approve their own request.");
            }
            
            var priorActions = await _context.ProcessInstanceHistories
                .Where(h => h.ProcessInstanceId == task.ProcessInstanceId && h.ActionByUserId == userId && h.ActionType == "TaskCompleted")
                .AnyAsync();
            if (priorActions)
            {
                throw new InvalidOperationException("Maker-checker violation: User has already participated in this process instance.");
            }
        }

        task.Status = "Completed";
        task.CompletedAtUtc = DateTime.UtcNow;
        task.CompletedByUserId = userId;
        task.Outcome = request.Outcome;
        task.Remarks = request.Remarks;

        _context.ProcessInstanceHistories.Add(new ProcessInstanceHistory
        {
            Id = Guid.NewGuid(),
            ProcessInstanceId = task.ProcessInstanceId,
            ActionType = "TaskCompleted",
            ActionByUserId = userId,
            Outcome = request.Outcome,
            Remarks = request.Remarks,
            PayloadJson = request.PayloadJson,
            ActionAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        
        // Progress workflow
        await _runtimeService.MoveToNextStepAsync(task.ProcessInstance, task.ProcessStepDefinition, request.Outcome, request.PayloadJson);
    }

    public async Task RejectTaskAsync(Guid taskId, string userId, CompleteTaskRequest request)
    {
        request.Outcome = "Reject";
        await CompleteTaskAsync(taskId, userId, request);
    }
}
