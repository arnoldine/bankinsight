using BankInsight.API.DTOs;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class EnterpriseReportsController : ControllerBase
{
    private readonly IEnterpriseReportingService _service;

    public EnterpriseReportsController(IEnterpriseReportingService service)
    {
        _service = service;
    }

    [HttpGet("catalog")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<List<ReportCatalogEntryDTO>>> GetCatalog()
    {
        return Ok(await _service.GetCatalogAsync());
    }

    [HttpGet("catalog/{code}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<ReportCatalogEntryDTO>> GetCatalogEntry(string code)
    {
        return Ok(await _service.GetCatalogEntryAsync(code));
    }

    [HttpPost("execute/{code}")]
    [HasPermission(AppPermissions.Reports.Generate)]
    public async Task<ActionResult<ReportExecutionResponseDTO>> Execute(string code, [FromBody] ReportExecutionRequestDTO request)
    {
        return Ok(await _service.ExecuteAsync(code, request));
    }

    [HttpPost("export/{code}/{format}")]
    [HasPermission(AppPermissions.Reports.Generate)]
    public async Task<IActionResult> Export(string code, string format, [FromBody] ReportExecutionRequestDTO request)
    {
        var result = await _service.ExportAsync(code, format, request);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("history")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<List<ReportHistoryItemDTO>>> History([FromQuery] int take = 50)
    {
        return Ok(await _service.GetHistoryAsync(take));
    }

    [HttpGet("favorites")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<List<ReportFavoriteDTO>>> Favorites()
    {
        return Ok(await _service.GetFavoritesAsync());
    }

    [HttpPost("favorites/{code}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<IActionResult> AddFavorite(string code)
    {
        await _service.AddFavoriteAsync(code);
        return NoContent();
    }

    [HttpDelete("favorites/{code}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<IActionResult> RemoveFavorite(string code)
    {
        await _service.RemoveFavoriteAsync(code);
        return NoContent();
    }

    [HttpGet("presets/{code}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<List<ReportFilterPresetDTO>>> Presets(string code)
    {
        return Ok(await _service.GetPresetsAsync(code));
    }

    [HttpPost("presets/{code}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<ReportFilterPresetDTO>> SavePreset(string code, [FromBody] SaveReportFilterPresetRequestDTO request)
    {
        return Ok(await _service.SavePresetAsync(code, request));
    }

    [HttpDelete("presets/item/{presetId:guid}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<IActionResult> DeletePreset(Guid presetId)
    {
        await _service.DeletePresetAsync(presetId);
        return NoContent();
    }

    [HttpGet("crb/data-quality")]
    [HasPermission(AppPermissions.Reports.Risk)]
    public async Task<ActionResult<CrbDataQualityDashboardDTO>> GetCrbDataQuality()
    {
        return Ok(await _service.GetCrbDataQualityAsync());
    }
}
