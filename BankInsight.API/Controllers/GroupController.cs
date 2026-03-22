using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupController : ControllerBase
{
    private readonly GroupService _groupService;

    public GroupController(GroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet]
    [RequirePermission("VIEW_GROUPS")]
    public async Task<IActionResult> GetGroups()
    {
        var groups = await _groupService.GetGroupsAsync();
        return Ok(groups);
    }

    [HttpPost]
    [RequirePermission("CREATE_GROUPS")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var group = await _groupService.CreateGroupAsync(request);
        return StatusCode(201, group);
    }

    [HttpPost("{id}/members")]
    [RequirePermission("MANAGE_GROUPS")]
    public async Task<IActionResult> AddMember(string id, [FromBody] AddMemberRequest request)
    {
        var success = await _groupService.AddMemberAsync(id, request.CustomerId);
        if (!success) return BadRequest(new { message = "Member already exists in this group or group not found" });

        return Ok(new { message = "Member added successfully" });
    }

    [HttpDelete("{id}/members/{customerId}")]
    [RequirePermission("MANAGE_GROUPS")]
    public async Task<IActionResult> RemoveMember(string id, string customerId)
    {
        await _groupService.RemoveMemberAsync(id, customerId);
        return Ok(new { message = "Member removed successfully" });
    }
}
