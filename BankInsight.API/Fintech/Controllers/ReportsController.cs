using HybridTransfer.Application.DTOs;
using HybridTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HybridTransfer.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ReportingCatalogService _reportingCatalogService;

    public ReportsController(ReportingCatalogService reportingCatalogService)
    {
        _reportingCatalogService = reportingCatalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportDescriptorResponse>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ReportDescriptorResponse>> List()
    {
        return Ok(_reportingCatalogService.GetAvailableReports());
    }
}
