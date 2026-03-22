using BankInsight.API.DTOs;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly IRegulatoryReportService _regulatoryReportService;
    private readonly IFinancialReportService _financialReportService;
    private readonly IAnalyticsService _analyticsService;

    public ReportController(
        IReportingService reportingService,
        IRegulatoryReportService regulatoryReportService,
        IFinancialReportService financialReportService,
        IAnalyticsService analyticsService)
    {
        _reportingService = reportingService;
        _regulatoryReportService = regulatoryReportService;
        _financialReportService = financialReportService;
        _analyticsService = analyticsService;
    }

    [HttpGet("catalog")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<List<ReportDefinitionDTO>>> GetReportCatalog([FromQuery] string? reportType = null)
    {
        var reports = await _reportingService.GetReportCatalogAsync(reportType);
        return Ok(reports);
    }

    [HttpPost("definitions")]
    [HasPermission(AppPermissions.Reports.Configure)]
    public async Task<ActionResult<ReportDefinitionDTO>> CreateReportDefinition(CreateReportDefinitionRequest request)
    {
        var createdReport = await _reportingService.CreateReportDefinitionAsync(request, User.Identity?.Name ?? "system");
        return CreatedAtAction(nameof(GetReportDefinition), new { id = createdReport.Id }, createdReport);
    }

    [HttpGet("definitions/{id}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<ReportDefinitionDTO>> GetReportDefinition(int id)
    {
        var report = await _reportingService.GetReportDefinitionAsync(id);
        return Ok(report);
    }

    [HttpPost("generate")]
    [HasPermission(AppPermissions.Reports.Generate)]
    public async Task<ActionResult<ReportRunDTO>> GenerateReport([FromBody] GenerateReportRequestDTO request)
    {
        var result = await _reportingService.GenerateReportAsync(
            request.ReportId,
            request.Parameters.ToDictionary(x => x.Key, x => (object)x.Value),
            User.Identity?.Name ?? "system",
            request.Format ?? "JSON");
        return Ok(result);
    }

    [HttpGet("history/{reportId}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<List<ReportRunDTO>>> GetReportHistory(int reportId, [FromQuery] int pageSize = 20)
    {
        var history = await _reportingService.GetReportHistoryAsync(reportId, pageSize);
        return Ok(history);
    }

    [HttpGet("runs/{runId}")]
    [HasPermission(AppPermissions.Reports.View)]
    public async Task<ActionResult<ReportRunDTO>> GetReportRun(int runId)
    {
        var run = await _reportingService.GetReportRunAsync(runId);
        return Ok(run);
    }

    [HttpGet("regulatory/daily-position")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<DailyPositionReportDTO>> GetDailyPositionReport([FromQuery] DateTime reportDate)
    {
        var utcReportDate = DateTime.SpecifyKind(reportDate.Date, DateTimeKind.Utc);
        var report = await _regulatoryReportService.GenerateDailyPositionReportAsync(utcReportDate);
        return Ok(report);
    }

    [HttpGet("regulatory/monthly-return-1")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<MonthlyReturnDTO>> GetMonthlyReturn1([FromQuery] int month, [FromQuery] int year)
    {
        var report = await _regulatoryReportService.GenerateMonthlyReturn1Async(month, year);
        return Ok(report);
    }

    [HttpGet("regulatory/monthly-return-2")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<MonthlyReturnDTO>> GetMonthlyReturn2([FromQuery] int month, [FromQuery] int year)
    {
        var report = await _regulatoryReportService.GenerateMonthlyReturn2Async(month, year);
        return Ok(report);
    }

    [HttpGet("regulatory/monthly-return-3")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<MonthlyReturnDTO>> GetMonthlyReturn3([FromQuery] int month, [FromQuery] int year)
    {
        var report = await _regulatoryReportService.GenerateMonthlyReturn3Async(month, year);
        return Ok(report);
    }

    [HttpGet("regulatory/prudential-return")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<PrudentialReturnDTO>> GetPrudentialReturn([FromQuery] DateTime asOfDate)
    {
        var utcAsOfDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
        var report = await _regulatoryReportService.GeneratePrudentialReturnAsync(utcAsOfDate);
        return Ok(report);
    }
    [HttpGet("regulatory/large-exposure")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<LargeExposureReportDTO>> GetLargeExposureReport([FromQuery] DateTime asOfDate)
    {
        var utcAsOfDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
        var report = await _regulatoryReportService.GenerateLargeExposureReportAsync(utcAsOfDate);
        return Ok(report);
    }

    [HttpGet("regulatory/returns")]
    [HasPermission(AppPermissions.Reports.Regulatory)]
    public async Task<ActionResult<List<RegulatoryReturnDTO>>> GetRegulatoryReturns([FromQuery] string? returnType = null)
    {
        var returns = await _regulatoryReportService.GetRegulatoryReturnsAsync(returnType);
        return Ok(returns);
    }

    [HttpPost("regulatory/returns/{returnId}/approve")]
    [HasPermission(AppPermissions.Reports.Approve)]
    public async Task<ActionResult<RegulatoryReturnDTO>> ApproveRegulatoryReturn(int returnId)
    {
        try
        {
            var result = await _regulatoryReportService.ApproveReturnAsync(returnId, User.Identity?.Name ?? "system");
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("regulatory/returns/{returnId}/reject")]
    [HasPermission(AppPermissions.Reports.Approve)]
    public async Task<ActionResult<RegulatoryReturnDTO>> RejectRegulatoryReturn(int returnId, [FromBody] RegulatoryReturnReviewRequest request)
    {
        try
        {
            var result = await _regulatoryReportService.RejectReturnAsync(returnId, User.Identity?.Name ?? "system", request.Reason);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("regulatory/submit-to-bog/{returnId}")]
    [HasPermission(AppPermissions.Reports.Submit)]
    public async Task<ActionResult<RegulatoryReturnDTO>> SubmitReturnToBog(int returnId)
    {
        try
        {
            var result = await _regulatoryReportService.SubmitReturnToBogAsync(returnId, User.Identity?.Name ?? "system");
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("financial/balance-sheet")]
    [HasPermission(AppPermissions.Reports.Financial)]
    public async Task<ActionResult<BalanceSheetDTO>> GetBalanceSheet([FromQuery] DateTime asOfDate)
    {
        var utcAsOfDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
        var balanceSheet = await _financialReportService.GenerateBalanceSheetAsync(utcAsOfDate);
        return Ok(balanceSheet);
    }

    [HttpGet("financial/income-statement")]
    [HasPermission(AppPermissions.Reports.Financial)]
    public async Task<ActionResult<IncomeStatementDTO>> GetIncomeStatement([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
    {
        var utcPeriodStart = DateTime.SpecifyKind(periodStart.Date, DateTimeKind.Utc);
        var utcPeriodEnd = DateTime.SpecifyKind(periodEnd.Date, DateTimeKind.Utc);
        var incomeStatement = await _financialReportService.GenerateIncomeStatementAsync(utcPeriodStart, utcPeriodEnd);
        return Ok(incomeStatement);
    }

    [HttpGet("financial/cash-flow")]
    [HasPermission(AppPermissions.Reports.Financial)]
    public async Task<ActionResult<CashFlowStatementDTO>> GetCashFlowStatement([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
    {
        var utcPeriodStart = DateTime.SpecifyKind(periodStart.Date, DateTimeKind.Utc);
        var utcPeriodEnd = DateTime.SpecifyKind(periodEnd.Date, DateTimeKind.Utc);
        var cashFlow = await _financialReportService.GenerateCashFlowStatementAsync(utcPeriodStart, utcPeriodEnd);
        return Ok(cashFlow);
    }

    [HttpGet("financial/trial-balance")]
    [HasPermission(AppPermissions.Reports.Financial)]
    public async Task<ActionResult<TrialBalanceDTO>> GetTrialBalance([FromQuery] DateTime asOfDate)
    {
        var utcAsOfDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
        var trialBalance = await _financialReportService.GenerateTrialBalanceAsync(utcAsOfDate);
        return Ok(trialBalance);
    }

    [HttpGet("analytics/customer-segmentation")]
    [HasPermission(AppPermissions.Reports.Risk)]
    public async Task<ActionResult<CustomerSegmentationDTO>> GetCustomerSegmentation([FromQuery] DateTime asOfDate)
    {
        var segmentation = await _analyticsService.GetCustomerSegmentationAsync(asOfDate);
        return Ok(segmentation);
    }

    [HttpGet("analytics/transaction-trends")]
    [HasPermission(AppPermissions.Reports.Risk)]
    public async Task<ActionResult<TransactionTrendsDTO>> GetTransactionTrends([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
    {
        var trends = await _analyticsService.GetTransactionTrendsAsync(periodStart, periodEnd);
        return Ok(trends);
    }

    [HttpGet("analytics/product-analytics")]
    [HasPermission(AppPermissions.Reports.Risk)]
    public async Task<ActionResult<ProductAnalyticsDTO>> GetProductAnalytics([FromQuery] DateTime asOfDate)
    {
        var analytics = await _analyticsService.GetProductAnalyticsAsync(asOfDate);
        return Ok(analytics);
    }

    [HttpGet("analytics/channel-analytics")]
    [HasPermission(AppPermissions.Reports.Risk)]
    public async Task<ActionResult<ChannelAnalyticsDTO>> GetChannelAnalytics([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
    {
        var analytics = await _analyticsService.GetChannelAnalyticsAsync(periodStart, periodEnd);
        return Ok(analytics);
    }

    [HttpGet("analytics/staff-productivity")]
    [HasPermission(AppPermissions.Reports.Risk)]
    public async Task<ActionResult<StaffProductivityDTO>> GetStaffProductivity([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
    {
        var analytics = await _analyticsService.GetStaffProductivityAnalyticsAsync(periodStart, periodEnd);
        return Ok(analytics);
    }
}

public class RegulatoryReturnReviewRequest
{
    public string? Reason { get; set; }
}

public class GenerateReportRequestDTO
{
    public int ReportId { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public string? Format { get; set; }
}

