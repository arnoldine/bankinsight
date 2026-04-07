using HybridTransfer.Application.DTOs;
using HybridTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/risk")]
public sealed class RiskController : ControllerBase
{
    private readonly RiskAssessmentService _riskAssessmentService;
    private readonly ComplianceExplorerService _complianceExplorerService;

    public RiskController(RiskAssessmentService riskAssessmentService, ComplianceExplorerService complianceExplorerService)
    {
        _riskAssessmentService = riskAssessmentService;
        _complianceExplorerService = complianceExplorerService;
    }

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(IEnumerable<AlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlertResponse>>> GetAlerts(CancellationToken cancellationToken)
    {
        var alerts = await _riskAssessmentService.GetOpenAlertsAsync(cancellationToken);
        return Ok(alerts);
    }

    [HttpGet("alerts/explorer")]
    [ProducesResponseType(typeof(PagedResponse<AlertExplorerItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AlertExplorerItemResponse>>> SearchAlerts([FromQuery] AlertExplorerQueryRequest request, CancellationToken cancellationToken)
    {
        var alerts = await _complianceExplorerService.SearchAlertsAsync(request, cancellationToken);
        return Ok(alerts);
    }
}
