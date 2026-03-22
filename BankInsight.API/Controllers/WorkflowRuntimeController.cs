using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowRuntimeController : ControllerBase
{
    private readonly ProcessRuntimeService _runtimeService;
    private readonly ProcessTaskService _taskService;
    private readonly Security.ICurrentUserContext _currentUser;

    public WorkflowRuntimeController(
        ProcessRuntimeService runtimeService, 
        ProcessTaskService taskService, 
        Security.ICurrentUserContext currentUser)
    {
        _runtimeService = runtimeService;
        _taskService = taskService;
        _currentUser = currentUser;
    }

    [HttpPost("start")]
    [HasPermission("processes.start")]
    public async Task<IActionResult> StartProcess([FromBody] StartProcessRequest request, [FromQuery] string? processCode)
    {
        var instance = await _runtimeService.StartProcessAsync(request, _currentUser.UserId, processCode);
        return Ok(new { instanceId = instance.Id, status = instance.Status });
    }

    [HttpGet("tasks/my")]
    public async Task<IActionResult> GetMyTasks()
    {
        var tasks = await _taskService.GetMyTasksAsync(_currentUser.UserId);
        return Ok(tasks);
    }

    [HttpGet("tasks/claimable")]
    [HasPermission("tasks.claim")]
    public async Task<IActionResult> GetClaimableTasks()
    {
        var roles = new List<string>(); // In this RBAC, permissions dictate access, roles are grouped permissions.
        var perms = _currentUser.Permissions.ToList();
        
        var tasks = await _taskService.GetClaimableTasksAsync(roles, perms);
        return Ok(tasks);
    }

    [HttpPost("tasks/{taskId}/claim")]
    [HasPermission("tasks.claim")]
    public async Task<IActionResult> ClaimTask(Guid taskId)
    {
        await _taskService.ClaimTaskAsync(taskId, _currentUser.UserId);
        return Ok(new { message = "Task claimed successfully." });
    }

    [HttpPost("tasks/{taskId}/complete")]
    public async Task<IActionResult> CompleteTask(Guid taskId, [FromBody] CompleteTaskRequest request)
    {
        await _taskService.CompleteTaskAsync(taskId, _currentUser.UserId, request);
        return Ok(new { message = "Task completed successfully." });
    }

    [HttpPost("tasks/{taskId}/reject")]
    public async Task<IActionResult> RejectTask(Guid taskId, [FromBody] CompleteTaskRequest request)
    {
        await _taskService.RejectTaskAsync(taskId, _currentUser.UserId, request);
        return Ok(new { message = "Task rejected successfully." });
    }
}
