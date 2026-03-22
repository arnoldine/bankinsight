using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [RequirePermission("VIEW_USERS")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [RequirePermission("VIEW_USERS")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    [HttpPost]
    [RequirePermission("MANAGE_USERS")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        return StatusCode(201, user);
    }

    [HttpPut("{id}")]
    [RequirePermission("MANAGE_USERS")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateUserAsync(id, request);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    [HttpDelete("{id}")]
    [RequirePermission("MANAGE_USERS")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success) return NotFound(new { message = "User not found" });
        return Ok(new { message = "User deleted" });
    }
}
