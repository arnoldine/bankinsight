using System;
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
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        [HttpGet("customer-segmentation")]
        [ProducesResponseType(typeof(CustomerSegmentationDTO), 200)]
        public async Task<IActionResult> GetCustomerSegmentation([FromQuery] DateTime asOfDate)
        {
            try
            {
                var analytics = await _analyticsService.GetCustomerSegmentationAsync(asOfDate);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting customer segmentation: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("transaction-trends")]
        [ProducesResponseType(typeof(TransactionTrendsDTO), 200)]
        public async Task<IActionResult> GetTransactionTrends([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        {
            try
            {
                var analytics = await _analyticsService.GetTransactionTrendsAsync(periodStart, periodEnd);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting transaction trends: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("product-analytics")]
        [ProducesResponseType(typeof(ProductAnalyticsDTO), 200)]
        public async Task<IActionResult> GetProductAnalytics([FromQuery] DateTime asOfDate)
        {
            try
            {
                var analytics = await _analyticsService.GetProductAnalyticsAsync(asOfDate);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting product analytics: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("channel-analytics")]
        [ProducesResponseType(typeof(ChannelAnalyticsDTO), 200)]
        public async Task<IActionResult> GetChannelAnalytics([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        {
            try
            {
                var analytics = await _analyticsService.GetChannelAnalyticsAsync(periodStart, periodEnd);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting channel analytics: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("staff-productivity")]
        [ProducesResponseType(typeof(StaffProductivityDTO), 200)]
        public async Task<IActionResult> GetStaffProductivity([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        {
            try
            {
                var analytics = await _analyticsService.GetStaffProductivityAnalyticsAsync(periodStart, periodEnd);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting staff productivity: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
