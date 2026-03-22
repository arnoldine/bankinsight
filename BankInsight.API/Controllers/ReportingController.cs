using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportingController : ControllerBase
    {
        private readonly IReportingService _reportingService;
        private readonly ILogger<ReportingController> _logger;

        public ReportingController(IReportingService reportingService, ILogger<ReportingController> logger)
        {
            _reportingService = reportingService;
            _logger = logger;
        }

        [HttpPost("definitions")]
        [ProducesResponseType(typeof(ReportDefinitionDTO), 201)]
        public async Task<IActionResult> CreateReportDefinition(CreateReportDefinitionRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "system";
                var report = await _reportingService.CreateReportDefinitionAsync(request, userId);
                return CreatedAtAction(nameof(GetReportDefinition), new { id = report.Id }, report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating report definition: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("definitions")]
        [ProducesResponseType(typeof(List<ReportDefinitionDTO>), 200)]
        public async Task<IActionResult> GetReportCatalog([FromQuery] string? reportType = null)
        {
            try
            {
                var reports = await _reportingService.GetReportCatalogAsync(reportType);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching report catalog: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("definitions/{id}")]
        [ProducesResponseType(typeof(ReportDefinitionDTO), 200)]
        public async Task<IActionResult> GetReportDefinition(int id)
        {
            try
            {
                var report = await _reportingService.GetReportDefinitionAsync(id);
                return Ok(report);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Report definition not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching report definition: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("generate/{reportId}")]
        [ProducesResponseType(typeof(ReportRunDTO), 200)]
        public async Task<IActionResult> GenerateReport(int reportId, [FromQuery] string format = "JSON")
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "system";
                // In real implementation, extract parameters from request body
                var parameters = new Dictionary<string, object>();
                var run = await _reportingService.GenerateReportAsync(reportId, parameters, userId, format);
                return Ok(run);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Report definition not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating report: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("history/{reportId}")]
        [ProducesResponseType(typeof(List<ReportRunDTO>), 200)]
        public async Task<IActionResult> GetReportHistory(int reportId, [FromQuery] int pageSize = 20)
        {
            try
            {
                var history = await _reportingService.GetReportHistoryAsync(reportId, pageSize);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching report history: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("runs/{runId}")]
        [ProducesResponseType(typeof(ReportRunDTO), 200)]
        public async Task<IActionResult> GetReportRun(int runId)
        {
            try
            {
                var run = await _reportingService.GetReportRunAsync(runId);
                return Ok(run);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Report run not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching report run: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("definitions/{id}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteReportDefinition(int id)
        {
            try
            {
                await _reportingService.DeleteReportDefinitionAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting report definition: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
