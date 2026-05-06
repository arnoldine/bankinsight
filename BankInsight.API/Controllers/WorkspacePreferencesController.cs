using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/workspace-preferences")]
[Authorize]
public class WorkspacePreferencesController : ControllerBase
{
    private readonly WorkspacePreferencesService _service;

    public WorkspacePreferencesController(WorkspacePreferencesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<WorkspacePreferencesSummaryDto>> GetSummary(CancellationToken cancellationToken)
        => Ok(await _service.GetSummaryAsync(cancellationToken));

    [HttpPost("favorites/{workspaceKey}")]
    public async Task<IActionResult> UpsertFavorite(string workspaceKey, [FromBody] UpsertWorkspaceFavoriteRequest request, CancellationToken cancellationToken)
    {
        await _service.UpsertFavoriteAsync(workspaceKey, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("favorites/{workspaceKey}")]
    public async Task<IActionResult> RemoveFavorite(string workspaceKey, CancellationToken cancellationToken)
    {
        await _service.RemoveFavoriteAsync(workspaceKey, cancellationToken);
        return NoContent();
    }

    [HttpPost("views")]
    public async Task<ActionResult<WorkspaceSavedViewDto>> SaveView([FromBody] SaveWorkspaceViewRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SaveViewAsync(request, cancellationToken));

    [HttpDelete("views/{id}")]
    public async Task<IActionResult> DeleteView(string id, CancellationToken cancellationToken)
    {
        await _service.DeleteViewAsync(id, cancellationToken);
        return NoContent();
    }
}
