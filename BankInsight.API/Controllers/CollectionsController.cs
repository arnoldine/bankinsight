using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/collections")]
public class CollectionsController : ControllerBase
{
    private readonly CollectionsService _collectionsService;
    private readonly ICurrentUserContext _currentUserContext;

    public CollectionsController(CollectionsService collectionsService, ICurrentUserContext currentUserContext)
    {
        _collectionsService = collectionsService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet("cases")]
    [RequirePermission("loans.view")]
    public async Task<ActionResult<List<CollectionCaseDto>>> GetCases()
    {
        return Ok(await _collectionsService.GetCasesAsync());
    }

    [HttpPut("cases/{caseId}")]
    [RequirePermission("loans.restructure")]
    public async Task<ActionResult<CollectionCaseDto>> UpdateCase(string caseId, [FromBody] UpdateCollectionCaseRequest request)
    {
        var result = await _collectionsService.UpdateCaseAsync(caseId, request, _currentUserContext.UserId);
        if (result == null)
        {
            return NotFound(new { message = "Collection case not found" });
        }

        return Ok(result);
    }

    [HttpPost("cases/{caseId}/actions")]
    [RequirePermission("loans.restructure")]
    public async Task<ActionResult<CollectionCaseDto>> ExecuteAction(string caseId, [FromBody] ExecuteCollectionActionRequest request)
    {
        var result = await _collectionsService.ExecuteCaseActionAsync(caseId, request, _currentUserContext.UserId);
        if (result == null)
        {
            return NotFound(new { message = "Collection case not found" });
        }

        return Ok(result);
    }
}
