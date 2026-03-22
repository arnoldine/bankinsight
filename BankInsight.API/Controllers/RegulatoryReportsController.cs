using System;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers
{
    [ApiController]
    [Route("api/regulatory-reports")]
    [Authorize]
    public class RegulatoryReportsController : ControllerBase
    {
        private readonly IRegulatoryReportService _reportService;
        private readonly ILogger<RegulatoryReportsController> _logger;

        public RegulatoryReportsController(IRegulatoryReportService reportService, ILogger<RegulatoryReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        [HttpPost("daily-position")]
        [HttpGet("daily-position")]
        [ProducesResponseType(typeof(DailyPositionReportDTO), 200)]
        public async Task<IActionResult> GenerateDailyPositionReport([FromQuery] DateTime reportDate)
        {
            try
            {
                var utcReportDate = DateTime.SpecifyKind(reportDate.Date, DateTimeKind.Utc);
                var report = await _reportService.GenerateDailyPositionReportAsync(utcReportDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating daily position report: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("monthly-return-1")]
        [HttpGet("monthly-return-1")]
        [ProducesResponseType(typeof(MonthlyReturnDTO), 200)]
        public async Task<IActionResult> GenerateMonthlyReturn1([FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var report = await _reportService.GenerateMonthlyReturn1Async(month, year);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating monthly return 1: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("monthly-return-2")]
        [HttpGet("monthly-return-2")]
        [ProducesResponseType(typeof(MonthlyReturnDTO), 200)]
        public async Task<IActionResult> GenerateMonthlyReturn2([FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var report = await _reportService.GenerateMonthlyReturn2Async(month, year);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating monthly return 2: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("monthly-return-3")]
        [HttpGet("monthly-return-3")]
        [ProducesResponseType(typeof(MonthlyReturnDTO), 200)]
        public async Task<IActionResult> GenerateMonthlyReturn3([FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var report = await _reportService.GenerateMonthlyReturn3Async(month, year);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating monthly return 3: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("prudential")]
        [HttpGet("prudential")]
        [HttpGet("prudential-return")]
        [ProducesResponseType(typeof(PrudentialReturnDTO), 200)]
        public async Task<IActionResult> GeneratePrudentialReturn([FromQuery] DateTime asOfDate)
        {
            try
            {
                var utcAsOfDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
                var report = await _reportService.GeneratePrudentialReturnAsync(utcAsOfDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating prudential return: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("large-exposure")]
        [HttpGet("large-exposure")]
        [ProducesResponseType(typeof(LargeExposureReportDTO), 200)]
        public async Task<IActionResult> GenerateLargeExposureReport([FromQuery] DateTime asOfDate)
        {
            try
            {
                var utcAsOfDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
                var report = await _reportService.GenerateLargeExposureReportAsync(utcAsOfDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating large exposure report: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("submit/{returnId}")]
        [HttpPost("submit-to-bog/{returnId}")]
        [ProducesResponseType(typeof(RegulatoryReturnDTO), 200)]
        public async Task<IActionResult> SubmitReturnToBog(int returnId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? "system";
                var result = await _reportService.SubmitReturnToBogAsync(returnId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Regulatory return not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error submitting return to BoG: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetRegulatoryReturns([FromQuery] string? returnType = null)
        {
            try
            {
                var returns = await _reportService.GetRegulatoryReturnsAsync(returnType);
                return Ok(returns);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching regulatory returns: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
