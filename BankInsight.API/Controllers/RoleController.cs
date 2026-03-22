using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/roles")]
public class RoleController : ControllerBase
{
    private readonly RoleService _roleService;

    public RoleController(RoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [RequirePermission("VIEW_ROLES")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleService.GetRolesAsync();
        return Ok(roles);
    }

    [HttpPost]
    [RequirePermission("MANAGE_ROLES")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var role = await _roleService.CreateRoleAsync(request);
        return StatusCode(201, role);
    }

    [HttpPut("{id}")]
    [RequirePermission("MANAGE_ROLES")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleService.UpdateRoleAsync(id, request);
        if (role == null) return NotFound(new { message = "Role not found" });
        return Ok(role);
    }
}
