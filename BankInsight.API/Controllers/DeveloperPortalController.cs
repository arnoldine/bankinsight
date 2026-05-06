using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/developer-portal")]
public class DeveloperPortalController : ControllerBase
{
    private readonly DeveloperPortalService _service;
    private readonly ICurrentUserContext _currentUserContext;

    public DeveloperPortalController(DeveloperPortalService service, ICurrentUserContext currentUserContext)
    {
        _service = service;
        _currentUserContext = currentUserContext;
    }

    [HttpGet("summary")]
    [RequirePermission("roles.view")]
    public async Task<ActionResult<DeveloperPortalSummaryDto>> GetSummary()
        => Ok(await _service.GetSummaryAsync());

    [HttpPost("partner-applications")]
    [RequirePermission("users.manage")]
    public async Task<ActionResult<PartnerApplicationDto>> CreatePartnerApplication([FromBody] CreatePartnerApplicationRequest request)
        => Ok(await _service.CreatePartnerApplicationAsync(request, _currentUserContext.UserId));

    [HttpPut("partner-applications/{id}")]
    [RequirePermission("users.manage")]
    public async Task<ActionResult<PartnerApplicationDto>> UpdatePartnerApplication(string id, [FromBody] UpdatePartnerApplicationRequest request)
    {
        var result = await _service.UpdatePartnerApplicationAsync(id, request, _currentUserContext.UserId);
        return result is null
            ? NotFound(new { message = "Partner application not found" })
            : Ok(result);
    }

    [HttpPost("partner-applications/{id}/rotate-sandbox-key")]
    [RequirePermission("users.manage")]
    public async Task<ActionResult<PartnerApplicationDto>> RotateSandboxKey(string id)
    {
        var result = await _service.RotateSandboxKeyAsync(id, _currentUserContext.UserId);
        return result is null
            ? NotFound(new { message = "Partner application not found" })
            : Ok(result);
    }

    [HttpPost("partner-applications/{id}/promote")]
    [RequirePermission("users.manage")]
    public async Task<ActionResult<PartnerApplicationDto>> PromotePartnerApplication(string id, [FromBody] PromotePartnerApplicationRequest request)
    {
        var result = await _service.PromotePartnerApplicationAsync(id, request, _currentUserContext.UserId);
        return result is null
            ? NotFound(new { message = "Partner application not found" })
            : Ok(result);
    }

    [HttpPost("webhook-subscriptions")]
    [RequirePermission("users.manage")]
    public async Task<ActionResult<WebhookSubscriptionDto>> CreateWebhookSubscription([FromBody] CreateWebhookSubscriptionRequest request)
        => Ok(await _service.CreateWebhookSubscriptionAsync(request, _currentUserContext.UserId));

    [HttpPost("webhook-subscriptions/replay")]
    [RequirePermission("users.manage")]
    public async Task<ActionResult<WebhookDeliveryLogDto>> ReplayWebhook([FromBody] ReplayWebhookRequest request)
    {
        var result = await _service.ReplayWebhookAsync(request, _currentUserContext.UserId);
        return result is null
            ? NotFound(new { message = "Webhook subscription not found" })
            : Ok(result);
    }
}
