using System;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers
{
    [ApiController]
    [Route("api/financial-reports")]
    [Authorize]
    public class FinancialReportsController : ControllerBase
    {
        private readonly IFinancialReportService _reportService;
        private readonly ILogger<FinancialReportsController> _logger;

        public FinancialReportsController(IFinancialReportService reportService, ILogger<FinancialReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        [HttpGet("balance-sheet")]
        [ProducesResponseType(typeof(BalanceSheetDTO), 200)]
        public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTime asOfDate)
        {
            try
            {
                var utcAsOfDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
                var report = await _reportService.GenerateBalanceSheetAsync(utcAsOfDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating balance sheet: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("income-statement")]
        [ProducesResponseType(typeof(IncomeStatementDTO), 200)]
        public async Task<IActionResult> GetIncomeStatement([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        {
            try
            {
                var utcPeriodStart = DateTime.SpecifyKind(periodStart.Date, DateTimeKind.Utc);
                var utcPeriodEnd = DateTime.SpecifyKind(periodEnd.Date, DateTimeKind.Utc);
                var report = await _reportService.GenerateIncomeStatementAsync(utcPeriodStart, utcPeriodEnd);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating income statement: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("cash-flow")]
        [ProducesResponseType(typeof(CashFlowStatementDTO), 200)]
        public async Task<IActionResult> GetCashFlowStatement([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        {
            try
            {
                var utcPeriodStart = DateTime.SpecifyKind(periodStart.Date, DateTimeKind.Utc);
                var utcPeriodEnd = DateTime.SpecifyKind(periodEnd.Date, DateTimeKind.Utc);
                var report = await _reportService.GenerateCashFlowStatementAsync(utcPeriodStart, utcPeriodEnd);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating cash flow statement: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("trial-balance")]
        [ProducesResponseType(typeof(TrialBalanceDTO), 200)]
        public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime asOfDate)
        {
            try
            {
                var utcAsOfDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
                var report = await _reportService.GenerateTrialBalanceAsync(utcAsOfDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating trial balance: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
