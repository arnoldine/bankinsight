using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankInsight.API.DTOs;
using BankInsight.API.Services;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchHierarchyController : ControllerBase
{
    private readonly IBranchHierarchyService _hierarchyService;

    public BranchHierarchyController(IBranchHierarchyService hierarchyService)
    {
        _hierarchyService = hierarchyService;
    }

    [HttpPost]
    public async Task<ActionResult<BranchHierarchyDto>> CreateHierarchy([FromBody] CreateBranchHierarchyRequest request)
    {
        try
        {
            var hierarchy = await _hierarchyService.CreateHierarchyAsync(request);
            return Ok(hierarchy);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{branchId}")]
    public async Task<ActionResult<BranchHierarchyDto>> GetBranchHierarchy(string branchId)
    {
        var hierarchy = await _hierarchyService.GetBranchHierarchyAsync(branchId);
        
        if (hierarchy == null)
        {
            return NotFound(new { message = "Branch hierarchy not found" });
        }

        return Ok(hierarchy);
    }

    [HttpGet]
    public async Task<ActionResult<List<BranchHierarchyDto>>> GetAllHierarchies()
    {
        var hierarchies = await _hierarchyService.GetAllHierarchiesAsync();
        return Ok(hierarchies);
    }

    [HttpGet("tree")]
    public async Task<ActionResult<List<BranchHierarchyDto>>> GetBranchTree()
    {
        var tree = await _hierarchyService.GetBranchTreeAsync();
        return Ok(tree);
    }

    [HttpGet("{branchId}/children")]
    public async Task<ActionResult<List<BranchHierarchyDto>>> GetChildBranches(string branchId)
    {
        var children = await _hierarchyService.GetChildBranchesAsync(branchId);
        return Ok(children);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHierarchy(int id)
    {
        var result = await _hierarchyService.DeleteHierarchyAsync(id);
        
        if (!result)
        {
            return NotFound(new { message = "Hierarchy not found" });
        }

        return Ok(new { message = "Hierarchy deleted successfully" });
    }
}
