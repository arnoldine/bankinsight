using System.Text.Json;

namespace CoreBanker.Services
{
    public class GroupLendingService : ApiClientBase
    {
        public GroupLendingService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<List<LendingGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<LendingGroupDto>>("/api/group-lending/groups", cancellationToken);
            return result ?? new List<LendingGroupDto>();
        }

        public async Task<List<LendingCenterDto>> GetCentersAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<LendingCenterDto>>("/api/group-lending/centers", cancellationToken);
            return result ?? new List<LendingCenterDto>();
        }

        public async Task<LendingGroupDto?> CreateGroupAsync(CreateLendingGroupRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateLendingGroupRequest, LendingGroupDto>("/api/group-lending/groups", request, cancellationToken);
        }

        public async Task<GroupMemberSummaryDto?> AddMemberAsync(string groupId, AddLendingGroupMemberRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<AddLendingGroupMemberRequest, GroupMemberSummaryDto>($"/api/group-lending/groups/{Uri.EscapeDataString(groupId)}/members", request, cancellationToken);
        }

        public async Task<LendingCenterDto?> CreateCenterAsync(CreateLendingCenterRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateLendingCenterRequest, LendingCenterDto>("/api/group-lending/centers", request, cancellationToken);
        }

        public async Task<GroupLoanApplicationDto?> CreateApplicationAsync(CreateGroupLoanApplicationRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateGroupLoanApplicationRequest, GroupLoanApplicationDto>("/api/group-lending/applications", request, cancellationToken);
        }

        public async Task<GroupLoanApplicationDto?> SubmitApplicationAsync(string applicationId, CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, GroupLoanApplicationDto>($"/api/group-lending/applications/{Uri.EscapeDataString(applicationId)}/submit", new { }, cancellationToken);
        }

        public async Task<GroupLoanApplicationDto?> ApproveApplicationAsync(string applicationId, ApproveGroupLoanApplicationRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<ApproveGroupLoanApplicationRequest, GroupLoanApplicationDto>($"/api/group-lending/applications/{Uri.EscapeDataString(applicationId)}/approve", request, cancellationToken);
        }

        public async Task<GroupLoanApplicationDto?> RejectApplicationAsync(string applicationId, RejectGroupLoanApplicationRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<RejectGroupLoanApplicationRequest, GroupLoanApplicationDto>($"/api/group-lending/applications/{Uri.EscapeDataString(applicationId)}/reject", request, cancellationToken);
        }

        public async Task<GroupLoanApplicationDto?> DisburseApplicationAsync(string applicationId, DisburseGroupLoanApplicationRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<DisburseGroupLoanApplicationRequest, GroupLoanApplicationDto>($"/api/group-lending/applications/{Uri.EscapeDataString(applicationId)}/disburse", request, cancellationToken);
        }

        public async Task<GroupMeetingDto?> CreateMeetingAsync(CreateGroupMeetingRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateGroupMeetingRequest, GroupMeetingDto>("/api/group-lending/meetings", request, cancellationToken);
        }

        public async Task<GroupMeetingDto?> GetMeetingAsync(string meetingId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<GroupMeetingDto>($"/api/group-lending/meetings/{Uri.EscapeDataString(meetingId)}", cancellationToken);
        }

        public async Task<GroupMeetingDto?> RecordAttendanceAsync(string meetingId, GroupMeetingAttendanceRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<GroupMeetingAttendanceRequest, GroupMeetingDto>($"/api/group-lending/meetings/{Uri.EscapeDataString(meetingId)}/attendance", request, cancellationToken);
        }

        public async Task<GroupMeetingDto?> CloseMeetingAsync(string meetingId, CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, GroupMeetingDto>($"/api/group-lending/meetings/{Uri.EscapeDataString(meetingId)}/close", new { }, cancellationToken);
        }

        public async Task<GroupCollectionBatchDto?> CreateCollectionBatchAsync(CreateGroupCollectionBatchRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateGroupCollectionBatchRequest, GroupCollectionBatchDto>("/api/group-lending/collections/batches", request, cancellationToken);
        }

        public async Task<GroupCollectionBatchDto?> GetCollectionBatchAsync(string batchId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<GroupCollectionBatchDto>($"/api/group-lending/collections/batches/{Uri.EscapeDataString(batchId)}", cancellationToken);
        }

        public async Task<GroupCollectionBatchDto?> PostCollectionBatchAsync(string batchId, CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, GroupCollectionBatchDto>($"/api/group-lending/collections/batches/{Uri.EscapeDataString(batchId)}/post", new { }, cancellationToken);
        }

        public async Task<GroupCollectionBatchDto?> ReverseCollectionBatchAsync(string batchId, CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, GroupCollectionBatchDto>($"/api/group-lending/collections/batches/{Uri.EscapeDataString(batchId)}/reverse", new { }, cancellationToken);
        }

        public async Task<GroupLoanStatementDto?> GetLoanStatementAsync(string loanId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<GroupLoanStatementDto>($"/api/group-lending/loans/{Uri.EscapeDataString(loanId)}/statement", cancellationToken);
        }

        public async Task<List<LoanScheduleDto>> GetLoanScheduleAsync(string loanId, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<LoanScheduleDto>>($"/api/group-lending/loans/{Uri.EscapeDataString(loanId)}/schedule", cancellationToken);
            return result ?? new List<LoanScheduleDto>();
        }

        public async Task<GroupLoanStatementDto?> RescheduleLoanAsync(string loanId, GroupLoanRescheduleRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<GroupLoanRescheduleRequest, GroupLoanStatementDto>($"/api/group-lending/loans/{Uri.EscapeDataString(loanId)}/reschedule", request, cancellationToken);
        }

        public async Task<GroupPortfolioSummaryDto?> GetGroupPerformanceAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<GroupPortfolioSummaryDto>("/api/group-lending/reports/group-performance", cancellationToken);
        }

        public async Task<List<GroupParReportItemDto>> GetParReportAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<GroupParReportItemDto>>("/api/group-lending/reports/par", cancellationToken);
            return result ?? new List<GroupParReportItemDto>();
        }

        public async Task<List<GroupReportRowDto>> GetOfficerPerformanceAsync(CancellationToken cancellationToken = default)
        {
            return await GetGenericReportAsync("/api/group-lending/reports/officer-performance", cancellationToken);
        }

        public async Task<List<GroupReportRowDto>> GetCycleAnalysisAsync(CancellationToken cancellationToken = default)
        {
            return await GetGenericReportAsync("/api/group-lending/reports/cycle-analysis", cancellationToken);
        }

        public async Task<List<GroupReportRowDto>> GetDelinquencyReportAsync(CancellationToken cancellationToken = default)
        {
            return await GetGenericReportAsync("/api/group-lending/reports/delinquency", cancellationToken);
        }

        public async Task<List<GroupReportRowDto>> GetMeetingCollectionsReportAsync(CancellationToken cancellationToken = default)
        {
            return await GetGenericReportAsync("/api/group-lending/reports/meeting-collections", cancellationToken);
        }

        private async Task<List<GroupReportRowDto>> GetGenericReportAsync(string path, CancellationToken cancellationToken)
        {
            var result = await GetAsync<List<Dictionary<string, JsonElement>>>(path, cancellationToken);
            return (result ?? new List<Dictionary<string, JsonElement>>()).ConvertAll(MapReportRow);
        }

        private static GroupReportRowDto MapReportRow(Dictionary<string, JsonElement> fields)
        {
            return new GroupReportRowDto
            {
                Fields = fields.ToDictionary(item => item.Key, item => item.Value.ToString())
            };
        }
    }

    public class LendingGroupDto
    {
        public string Id { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string? CenterId { get; set; }
        public string? GroupCode { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? MeetingDayOfWeek { get; set; }
        public string MeetingFrequency { get; set; } = "Weekly";
        public string? MeetingLocation { get; set; }
        public string? AssignedOfficerId { get; set; }
        public DateOnly? FormationDate { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public bool IsJointLiabilityEnabled { get; set; }
        public int? MaxMembers { get; set; }
        public string? Notes { get; set; }
        public List<GroupMemberSummaryDto> Members { get; set; } = new();
    }

    public class GroupMemberSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string MemberRole { get; set; } = "MEMBER";
        public string Status { get; set; } = "ACTIVE";
        public string KycStatus { get; set; } = "PENDING";
        public bool IsEligibleForLoan { get; set; }
        public int CurrentLoanCycle { get; set; }
        public decimal CurrentExposure { get; set; }
        public bool ArrearsFlag { get; set; }
    }

    public class LendingCenterDto
    {
        public string Id { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string CenterCode { get; set; } = string.Empty;
        public string CenterName { get; set; } = string.Empty;
        public string? MeetingDayOfWeek { get; set; }
        public string? MeetingLocation { get; set; }
        public string? AssignedOfficerId { get; set; }
        public string Status { get; set; } = "ACTIVE";
    }

    public class CreateLendingGroupRequest
    {
        public string GroupName { get; set; } = string.Empty;
        public string BranchId { get; set; } = "BR001";
        public string? CenterId { get; set; }
        public string? GroupCode { get; set; }
        public string? MeetingDayOfWeek { get; set; }
        public string MeetingFrequency { get; set; } = "Weekly";
        public string? MeetingLocation { get; set; }
        public string? AssignedOfficerId { get; set; }
        public DateOnly? FormationDate { get; set; }
        public bool IsJointLiabilityEnabled { get; set; }
        public int? MaxMembers { get; set; }
        public string? Notes { get; set; }
    }

    public class AddLendingGroupMemberRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string MemberRole { get; set; } = "MEMBER";
        public bool IsFoundingMember { get; set; }
        public decimal ShareContribution { get; set; }
        public bool GuarantorIndicator { get; set; }
        public string? SocialCollateralNotes { get; set; }
    }

    public class CreateLendingCenterRequest
    {
        public string BranchId { get; set; } = "BR001";
        public string CenterCode { get; set; } = string.Empty;
        public string CenterName { get; set; } = string.Empty;
        public string? MeetingDayOfWeek { get; set; }
        public string? MeetingLocation { get; set; }
        public string? AssignedOfficerId { get; set; }
    }

    public class CreateGroupLoanApplicationRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string BranchId { get; set; } = "BR001";
        public string? OfficerId { get; set; }
        public int LoanCycleNo { get; set; }
        public string? MeetingReference { get; set; }
        public string? GroupResolutionReference { get; set; }
        public string? Notes { get; set; }
        public List<GroupLoanApplicationMemberRequest> Members { get; set; } = new();
    }

    public class GroupLoanApplicationMemberRequest
    {
        public string GroupMemberId { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
        public int TenureWeeks { get; set; }
        public decimal InterestRate { get; set; }
        public string InterestMethod { get; set; } = "Flat";
        public string RepaymentFrequency { get; set; } = "Weekly";
        public string? LoanPurpose { get; set; }
        public decimal SavingsBalanceAtApplication { get; set; }
        public string? GuarantorNotes { get; set; }
    }

    public class GroupLoanApplicationDto
    {
        public string Id { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int LoanCycleNo { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string? OfficerId { get; set; }
        public string Status { get; set; } = "DRAFT";
        public decimal TotalApprovedAmount { get; set; }
        public decimal TotalRequestedAmount { get; set; }
        public decimal TotalDisbursedAmount { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? DisbursementDate { get; set; }
        public string? MeetingReference { get; set; }
        public string? GroupResolutionReference { get; set; }
        public string? Notes { get; set; }
        public List<GroupLoanApplicationMemberDto> Members { get; set; } = new();
    }

    public class GroupLoanApplicationMemberDto
    {
        public string Id { get; set; } = string.Empty;
        public string GroupMemberId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal DisbursedAmount { get; set; }
        public int TenureWeeks { get; set; }
        public decimal InterestRate { get; set; }
        public string InterestMethod { get; set; } = "Flat";
        public string RepaymentFrequency { get; set; } = "Weekly";
        public string EligibilityStatus { get; set; } = "PENDING";
        public string Status { get; set; } = "DRAFT";
    }

    public class ApproveGroupLoanApplicationRequest
    {
        public string DecisionNotes { get; set; } = string.Empty;
    }

    public class RejectGroupLoanApplicationRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class DisburseGroupLoanApplicationRequest
    {
        public DateOnly? DisbursementDate { get; set; }
        public string? ClientReference { get; set; }
    }

    public class CreateGroupMeetingRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string? CenterId { get; set; }
        public DateOnly MeetingDate { get; set; }
        public string MeetingType { get; set; } = "REGULAR";
        public string? Location { get; set; }
        public string? OfficerId { get; set; }
        public string? Notes { get; set; }
    }

    public class GroupMeetingDto
    {
        public string Id { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string? CenterId { get; set; }
        public DateOnly MeetingDate { get; set; }
        public string MeetingType { get; set; } = "REGULAR";
        public string? Location { get; set; }
        public string? OfficerId { get; set; }
        public string Status { get; set; } = "OPEN";
        public int AttendanceCount { get; set; }
        public string? Notes { get; set; }
        public List<GroupMeetingAttendanceLineDto> Attendances { get; set; } = new();
    }

    public class GroupMeetingAttendanceRequest
    {
        public List<GroupMeetingAttendanceLineDto> Attendances { get; set; } = new();
    }

    public class GroupMeetingAttendanceLineDto
    {
        public string GroupMemberId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string AttendanceStatus { get; set; } = "PRESENT";
        public DateTime? ArrivalTime { get; set; }
        public string? Notes { get; set; }
    }

    public class GroupParReportItemDto
    {
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public decimal OutstandingPrincipal { get; set; }
        public int DaysPastDue { get; set; }
        public string ParBucket { get; set; } = "0";
    }

    public class GroupPortfolioSummaryDto
    {
        public int ActiveGroups { get; set; }
        public int ActiveMembers { get; set; }
        public decimal TotalPortfolio { get; set; }
        public decimal Par30 { get; set; }
        public decimal WeeklyDueThisWeek { get; set; }
        public decimal CollectionsThisWeek { get; set; }
    }

    public class GroupReportRowDto
    {
        public Dictionary<string, string> Fields { get; set; } = new();
    }

    public class CreateGroupCollectionBatchRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string? GroupMeetingId { get; set; }
        public string BranchId { get; set; } = "BR001";
        public string? OfficerId { get; set; }
        public DateOnly CollectionDate { get; set; }
        public string Channel { get; set; } = "CASH";
        public string? ReferenceNo { get; set; }
        public List<GroupCollectionBatchLineRequest> Lines { get; set; } = new();
    }

    public class GroupCollectionBatchLineRequest
    {
        public string LoanAccountId { get; set; } = string.Empty;
        public string GroupMemberId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal ExpectedInstallment { get; set; }
        public decimal AmountCollected { get; set; }
        public decimal SavingsComponent { get; set; }
    }

    public class GroupCollectionBatchDto
    {
        public string Id { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string? GroupMeetingId { get; set; }
        public string BranchId { get; set; } = string.Empty;
        public string? OfficerId { get; set; }
        public DateOnly CollectionDate { get; set; }
        public string Status { get; set; } = "OPEN";
        public decimal TotalCollectedAmount { get; set; }
        public decimal TotalExpectedAmount { get; set; }
        public decimal VarianceAmount { get; set; }
        public string Channel { get; set; } = "CASH";
        public string? ReferenceNo { get; set; }
        public List<GroupCollectionBatchLineDto> Lines { get; set; } = new();
    }

    public class GroupCollectionBatchLineDto
    {
        public string Id { get; set; } = string.Empty;
        public string LoanAccountId { get; set; } = string.Empty;
        public string GroupMemberId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal ExpectedInstallment { get; set; }
        public decimal AmountCollected { get; set; }
        public decimal PrincipalComponent { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal PenaltyComponent { get; set; }
        public decimal SavingsComponent { get; set; }
        public decimal FeeComponent { get; set; }
        public decimal ArrearsRecovered { get; set; }
        public string Status { get; set; } = "PENDING";
    }

    public class GroupLoanStatementDto
    {
        public string LoanId { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? GroupId { get; set; }
        public decimal Principal { get; set; }
        public decimal? OutstandingBalance { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RepaymentFrequency { get; set; }
        public int? CycleNo { get; set; }
        public string? GuaranteeReference { get; set; }
        public bool RestructuredFlag { get; set; }
        public List<LoanScheduleDto> Schedule { get; set; } = new();
        public List<GroupRepaymentHistoryDto> Repayments { get; set; } = new();
    }

    public class GroupRepaymentHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime RepaymentDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PrincipalComponent { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal PenaltyComponent { get; set; }
        public string? Reference { get; set; }
        public string? ProcessedBy { get; set; }
        public bool IsReversal { get; set; }
        public string? ReversalReference { get; set; }
    }

    public class GroupLoanRescheduleRequest
    {
        public string LoanId { get; set; } = string.Empty;
        public int NewTermInPeriods { get; set; }
        public decimal? NewAnnualRate { get; set; }
        public string? NewRepaymentFrequency { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
