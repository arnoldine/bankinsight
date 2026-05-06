using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/supervisory")]
public class SupervisoryIntelligenceController : ControllerBase
{
    private readonly SupervisoryIntelligenceService _service;

    public SupervisoryIntelligenceController(SupervisoryIntelligenceService service)
    {
        _service = service;
    }

    [HttpGet("relationship-banking")]
    [RequirePermission("customers.view")]
    public async Task<ActionResult<RelationshipBankingSummaryDto>> GetRelationshipBankingSummary(CancellationToken cancellationToken)
        => Ok(await _service.GetRelationshipBankingSummaryAsync(cancellationToken));

    [HttpGet("relationship-banking/staff-directory")]
    [RequirePermission("customers.view")]
    public async Task<ActionResult<List<AssignableStaffItemDto>>> GetAssignableStaff(CancellationToken cancellationToken)
        => Ok(await _service.GetAssignableStaffAsync(cancellationToken));

    [HttpGet("relationship-banking/{customerId}")]
    [RequirePermission("customers.view")]
    public async Task<ActionResult<RelationshipPortfolioDetailDto>> GetRelationshipPortfolioDetail(string customerId, CancellationToken cancellationToken)
    {
        var result = await _service.GetRelationshipPortfolioDetailAsync(customerId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("relationship-banking/assign-owner")]
    [RequirePermission("customers.edit")]
    public async Task<ActionResult<RelationshipCustomerItemDto>> AssignRelationshipOwner([FromBody] AssignRelationshipOwnerRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AssignRelationshipOwnerAsync(request, cancellationToken));

    [HttpGet("digital-channel-operations")]
    [HasPermission(AppPermissions.Audit.View)]
    public async Task<ActionResult<DigitalChannelOperationsSummaryDto>> GetDigitalChannelOperationsSummary(CancellationToken cancellationToken)
        => Ok(await _service.GetDigitalChannelOperationsSummaryAsync(cancellationToken));

    [HttpGet("regulatory-intelligence")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<RegulatoryIntelligenceSummaryDto>> GetRegulatoryIntelligenceSummary(CancellationToken cancellationToken)
        => Ok(await _service.GetRegulatoryIntelligenceSummaryAsync(cancellationToken));

    [HttpPost("regulatory-intelligence/variances/resolve")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<RegulatoryVarianceItemDto>> ResolveVariance([FromBody] ResolveRegulatoryVarianceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.ResolveRegulatoryVarianceAsync(request, cancellationToken));

    [HttpPost("regulatory-intelligence/variances/reopen")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<RegulatoryVarianceItemDto>> ReopenVariance([FromBody] ResolveRegulatoryVarianceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.ReopenRegulatoryVarianceAsync(request, cancellationToken));

    [HttpPost("regulatory-intelligence/variances/assign")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<RegulatoryVarianceItemDto>> AssignVariance([FromBody] AssignRegulatoryVarianceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AssignRegulatoryVarianceAsync(request, cancellationToken));
}
