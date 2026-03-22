using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/workflows")]
public class WorkflowController : ControllerBase
{
    private readonly WorkflowService _workflowService;

    public WorkflowController(WorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet]
    [RequirePermission("VIEW_WORKFLOWS")]
    public async Task<IActionResult> GetWorkflows()
    {
        var workflows = await _workflowService.GetWorkflowsAsync();
        return Ok(workflows);
    }

    [HttpPost]
    [RequirePermission("MANAGE_WORKFLOWS")]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request)
    {
        var workflow = await _workflowService.CreateWorkflowAsync(request);
        return StatusCode(201, workflow);
    }

    [HttpPut("{id}")]
    [RequirePermission("MANAGE_WORKFLOWS")]
    public async Task<IActionResult> UpdateWorkflow(string id, [FromBody] UpdateWorkflowRequest request)
    {
        var workflow = await _workflowService.UpdateWorkflowAsync(id, request);
        if (workflow == null) return NotFound(new { message = "Workflow not found" });
        return Ok(workflow);
    }
}
