using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankInsight.API.DTOs;
using BankInsight.API.Services;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchConfigController : ControllerBase
{
    private readonly IBranchConfigService _configService;

    public BranchConfigController(IBranchConfigService configService)
    {
        _configService = configService;
    }

    [HttpPost]
    public async Task<ActionResult<BranchConfigDto>> UpdateConfig([FromBody] UpdateBranchConfigRequest request)
    {
        try
        {
            var config = await _configService.UpdateConfigAsync(request);
            return Ok(config);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{branchId}/{configKey}")]
    public async Task<ActionResult<BranchConfigDto>> GetConfig(string branchId, string configKey)
    {
        try
        {
            var config = await _configService.GetOrCreateConfigAsync(branchId, configKey);
            return Ok(config);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<List<BranchConfigDto>>> GetBranchConfigs(string branchId)
    {
        var configs = await _configService.GetBranchConfigsAsync(branchId);
        return Ok(configs);
    }

    [HttpGet]
    public async Task<ActionResult<List<BranchConfigDto>>> GetAllConfigs()
    {
        var configs = await _configService.GetAllConfigsAsync();
        return Ok(configs);
    }

    [HttpGet("{branchId}/{configKey}/value")]
    public async Task<ActionResult<string>> GetConfigValue(string branchId, string configKey)
    {
        var value = await _configService.GetConfigValueAsync(branchId, configKey);
        
        if (value == null)
        {
            return NotFound(new { message = "Config not found" });
        }

        return Ok(new { value });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteConfig(int id)
    {
        var result = await _configService.DeleteConfigAsync(id);
        
        if (!result)
        {
            return NotFound(new { message = "Config not found" });
        }

        return Ok(new { message = "Config deleted successfully" });
    }
}
