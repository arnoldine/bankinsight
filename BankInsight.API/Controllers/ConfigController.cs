using System.Collections.Generic;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly ConfigService _configService;

    public ConfigController(ConfigService configService)
    {
        _configService = configService;
    }

    [HttpGet]
    [RequirePermission("VIEW_CONFIG")]
    public async Task<IActionResult> GetConfig()
    {
        var config = await _configService.GetConfigAsync();
        return Ok(config);
    }

    [HttpPost]
    [RequirePermission("MANAGE_CONFIG")]
    public async Task<IActionResult> UpdateConfig([FromBody] List<ConfigItemDto> items)
    {
        await _configService.UpdateConfigAsync(items);
        return Ok(new { message = "Configuration updated successfully" });
    }
}
