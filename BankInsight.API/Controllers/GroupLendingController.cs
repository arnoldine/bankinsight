using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/group-lending")]
public class GroupLendingController : ControllerBase
{
    private readonly GroupLendingService _service;

    public GroupLendingController(GroupLendingService service)
    {
        _service = service;
    }

    [HttpGet("reports/par")]
    [HasPermission(AppPermissions.GroupLending.ViewReports)]
    public async Task<IActionResult> GetPar() => Ok(await _service.GetParReportAsync());

    [HttpGet("reports/group-performance")]
    [HasPermission(AppPermissions.GroupLending.ViewReports)]
    public async Task<IActionResult> GetGroupPerformance() => Ok(await _service.GetGroupPerformanceAsync());

    [HttpGet("reports/officer-performance")]
    [HasPermission(AppPermissions.GroupLending.ViewReports)]
    public async Task<IActionResult> GetOfficerPerformance() => Ok(await _service.GetOfficerPerformanceAsync());

    [HttpGet("reports/cycle-analysis")]
    [HasPermission(AppPermissions.GroupLending.ViewReports)]
    public async Task<IActionResult> GetCycleAnalysis() => Ok(await _service.GetCycleAnalysisAsync());

    [HttpGet("reports/delinquency")]
    [HasPermission(AppPermissions.GroupLending.ViewReports)]
    public async Task<IActionResult> GetDelinquency() => Ok(await _service.GetDelinquencyReportAsync());

    [HttpGet("reports/meeting-collections")]
    [HasPermission(AppPermissions.GroupLending.ViewReports)]
    public async Task<IActionResult> GetMeetingCollections() => Ok(await _service.GetMeetingCollectionsReportAsync());

    [HttpGet("groups")]
    [HasPermission(AppPermissions.GroupLending.View)]
    public async Task<IActionResult> GetGroups() => Ok(await _service.GetGroupsAsync());

    [HttpGet("groups/{id}")]
    [HasPermission(AppPermissions.GroupLending.View)]
    public async Task<IActionResult> GetGroup(string id) => Ok(await _service.GetGroupAsync(id));

    [HttpPost("groups")]
    [HasPermission(AppPermissions.GroupLending.ManageGroups)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateLendingGroupRequest request) => StatusCode(201, await _service.CreateGroupAsync(request));

    [HttpPut("groups/{id}")]
    [HasPermission(AppPermissions.GroupLending.ManageGroups)]
    public async Task<IActionResult> UpdateGroup(string id, [FromBody] UpdateLendingGroupRequest request) => Ok(await _service.UpdateGroupAsync(id, request));

    [HttpPost("groups/{id}/activate")]
    [HasPermission(AppPermissions.GroupLending.ManageGroups)]
    public async Task<IActionResult> ActivateGroup(string id) => Ok(await _service.ActivateGroupAsync(id));

    [HttpPost("groups/{id}/suspend")]
    [HasPermission(AppPermissions.GroupLending.ManageGroups)]
    public async Task<IActionResult> SuspendGroup(string id) => Ok(await _service.SuspendGroupAsync(id));

    [HttpPost("groups/{id}/members")]
    [HasPermission(AppPermissions.GroupLending.ManageGroups)]
    public async Task<IActionResult> AddMember(string id, [FromBody] AddLendingGroupMemberRequest request) => Ok(await _service.AddMemberAsync(id, request));

    [HttpDelete("groups/{id}/members/{memberId}")]
    [HasPermission(AppPermissions.GroupLending.ManageGroups)]
    public async Task<IActionResult> RemoveMember(string id, string memberId) { await _service.RemoveMemberAsync(id, memberId); return Ok(); }

    [HttpPost("centers")]
    [HasPermission(AppPermissions.GroupLending.ManageCenters)]
    public async Task<IActionResult> CreateCenter([FromBody] CreateLendingCenterRequest request) => StatusCode(201, await _service.CreateCenterAsync(request));

    [HttpGet("centers")]
    [HasPermission(AppPermissions.GroupLending.View)]
    public async Task<IActionResult> GetCenters() => Ok(await _service.GetCentersAsync());

    [HttpGet("applications/{id}")]
    [HasPermission(AppPermissions.GroupLending.View)]
    public async Task<IActionResult> GetApplication(string id) => Ok(await _service.GetApplicationAsync(id));

    [HttpPost("applications")]
    [HasPermission(AppPermissions.GroupLending.ManageApplications)]
    public async Task<IActionResult> CreateApplication([FromBody] CreateGroupLoanApplicationRequest request) => StatusCode(201, await _service.CreateApplicationAsync(request));

    [HttpPost("applications/{id}/submit")]
    [HasPermission(AppPermissions.GroupLending.ManageApplications)]
    public async Task<IActionResult> SubmitApplication(string id) => Ok(await _service.SubmitApplicationAsync(id));

    [HttpPost("applications/{id}/review")]
    [HasPermission(AppPermissions.GroupLending.ManageApplications)]
    public async Task<IActionResult> ReviewApplication(string id, [FromBody] ReviewGroupLoanApplicationRequest request) => Ok(await _service.ReviewApplicationAsync(id, request));

    [HttpPost("applications/{id}/approve")]
    [HasPermission(AppPermissions.GroupLending.ApproveApplications)]
    public async Task<IActionResult> ApproveApplication(string id, [FromBody] ApproveGroupLoanApplicationRequest request) => Ok(await _service.ApproveApplicationAsync(id, request));

    [HttpPost("applications/{id}/reject")]
    [HasPermission(AppPermissions.GroupLending.ApproveApplications)]
    public async Task<IActionResult> RejectApplication(string id, [FromBody] RejectGroupLoanApplicationRequest request) => Ok(await _service.RejectApplicationAsync(id, request));

    [HttpPost("applications/{id}/disburse")]
    [HasPermission(AppPermissions.GroupLending.Disburse)]
    public async Task<IActionResult> DisburseApplication(string id, [FromBody] DisburseGroupLoanApplicationRequest request) => Ok(await _service.DisburseApplicationAsync(id, request));

    [HttpPost("meetings")]
    [HasPermission(AppPermissions.GroupLending.ManageMeetings)]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateGroupMeetingRequest request) => StatusCode(201, await _service.CreateMeetingAsync(request));

    [HttpGet("meetings/{id}")]
    [HasPermission(AppPermissions.GroupLending.View)]
    public async Task<IActionResult> GetMeeting(string id) => Ok(await _service.GetMeetingAsync(id));

    [HttpPost("meetings/{id}/attendance")]
    [HasPermission(AppPermissions.GroupLending.ManageMeetings)]
    public async Task<IActionResult> RecordAttendance(string id, [FromBody] GroupMeetingAttendanceRequest request) => Ok(await _service.RecordAttendanceAsync(id, request));

    [HttpPost("meetings/{id}/close")]
    [HasPermission(AppPermissions.GroupLending.ManageMeetings)]
    public async Task<IActionResult> CloseMeeting(string id) => Ok(await _service.CloseMeetingAsync(id));

    [HttpPost("collections/batches")]
    [HasPermission(AppPermissions.GroupLending.Collect)]
    public async Task<IActionResult> CreateBatch([FromBody] CreateGroupCollectionBatchRequest request) => StatusCode(201, await _service.CreateCollectionBatchAsync(request));

    [HttpGet("collections/batches/{id}")]
    [HasPermission(AppPermissions.GroupLending.View)]
    public async Task<IActionResult> GetBatch(string id) => Ok(await _service.GetCollectionBatchAsync(id));

    [HttpPost("collections/batches/{id}/post")]
    [HasPermission(AppPermissions.GroupLending.Collect)]
    public async Task<IActionResult> PostBatch(string id) => Ok(await _service.PostCollectionBatchAsync(id));

    [HttpPost("collections/batches/{id}/reverse")]
    [HasPermission(AppPermissions.GroupLending.ReverseCollections)]
    public async Task<IActionResult> ReverseBatch(string id) => Ok(await _service.ReverseCollectionBatchAsync(id));

    [HttpPost("loans/{loanId}/repayments")]
    [HasPermission(AppPermissions.GroupLending.Collect)]
    public async Task<IActionResult> RepayLoan(string loanId, [FromBody] GroupRepaymentRequest request) => Ok(await _service.RepayLoanAsync(loanId, request));

    [HttpGet("loans/{loanId}/schedule")]
    [HasPermission(AppPermissions.GroupLending.View)]
    public async Task<IActionResult> GetLoanSchedule(string loanId) => Ok(await _service.GetLoanScheduleAsync(loanId));

    [HttpGet("loans/{loanId}/statement")]
    [HasPermission(AppPermissions.GroupLending.View)]
    public async Task<IActionResult> GetLoanStatement(string loanId) => Ok(await _service.GetLoanStatementAsync(loanId));
    [HttpPost("loans/{loanId}/reschedule")]
    [HasPermission(AppPermissions.GroupLending.Reschedule)]
    public async Task<IActionResult> RescheduleLoan(string loanId, [FromBody] LoanRestructureRequest request) => Ok(await _service.RescheduleLoanAsync(loanId, request));

    [HttpGet("product-designer/loan-products/{id}/group-rules")]
    [HasPermission(AppPermissions.GroupLending.ConfigureProducts)]
    public async Task<IActionResult> GetGroupRules(string id) => Ok(await _service.GetGroupRulesAsync(id));

    [HttpPut("product-designer/loan-products/{id}/group-rules")]
    [HasPermission(AppPermissions.GroupLending.ConfigureProducts)]
    public async Task<IActionResult> PutGroupRules(string id, [FromBody] ProductGroupRulesDto request) => Ok(await _service.UpsertGroupRulesAsync(id, request));

    [HttpGet("product-designer/loan-products/{id}/eligibility-rules")]
    [HasPermission(AppPermissions.GroupLending.ConfigureProducts)]
    public async Task<IActionResult> GetEligibilityRules(string id) => Ok(await _service.GetEligibilityRulesAsync(id));

    [HttpPut("product-designer/loan-products/{id}/eligibility-rules")]
    [HasPermission(AppPermissions.GroupLending.ConfigureProducts)]
    public async Task<IActionResult> PutEligibilityRules(string id, [FromBody] ProductEligibilityRulesDto request) => Ok(await _service.UpsertEligibilityRulesAsync(id, request));
}

