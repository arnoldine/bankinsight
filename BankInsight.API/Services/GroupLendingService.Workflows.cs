using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public partial class GroupLendingService
{
    public async Task<GroupLoanApplicationDto> CreateApplicationAsync(CreateGroupLoanApplicationRequest request)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId) ?? throw new InvalidOperationException("Product not found");
        if (!product.IsGroupLoanEnabled && !string.Equals(product.LendingMethodology, "GROUP", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Selected product is not group-lending enabled");
        var group = await _context.Groups.Include(g => g.Members).ThenInclude(m => m.Customer).FirstOrDefaultAsync(g => g.Id == request.GroupId) ?? throw new InvalidOperationException("Group not found");
        var groupRules = await _context.ProductGroupRules.FirstOrDefaultAsync(r => r.ProductId == request.ProductId); var eligibilityRules = await _context.ProductEligibilityRules.FirstOrDefaultAsync(r => r.ProductId == request.ProductId);
        var cycleNo = request.LoanCycleNo <= 0 ? ResolveNextCycle(group.Members) : request.LoanCycleNo;
        if (groupRules?.MaxCycleNumber is int maxCycleNumber && cycleNo > maxCycleNumber) throw new InvalidOperationException($"Maximum cycle number of {maxCycleNumber} was exceeded for this product");
        var application = new GroupLoanApplication { Id = $"GLA-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", GroupId = request.GroupId, LoanCycleNo = cycleNo, ProductId = request.ProductId, BranchId = string.IsNullOrWhiteSpace(request.BranchId) ? group.BranchId : request.BranchId, OfficerId = request.OfficerId ?? group.AssignedOfficerId ?? group.OfficerId, Status = "DRAFT", MeetingReference = request.MeetingReference, GroupResolutionReference = request.GroupResolutionReference, Notes = request.Notes, DisclosedTermsSnapshotJson = BuildDisclosureSnapshot(product, groupRules) };
        foreach (var memberRequest in request.Members)
        {
            var member = group.Members.FirstOrDefault(m => m.Id == memberRequest.GroupMemberId && m.Status == "ACTIVE") ?? throw new InvalidOperationException($"Group member {memberRequest.GroupMemberId} not found or inactive");
            var evaluation = await EvaluateMemberEligibilityAsync(member, memberRequest, eligibilityRules);
            application.Members.Add(new GroupLoanApplicationMember { Id = $"GLAM-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(100, 999)}", GroupLoanApplicationId = application.Id, GroupMemberId = member.Id, CustomerId = member.CustomerId, RequestedAmount = memberRequest.RequestedAmount, ApprovedAmount = evaluation.IsEligible ? memberRequest.RequestedAmount : 0m, TenureWeeks = memberRequest.TenureWeeks, InterestRate = memberRequest.InterestRate, InterestMethod = memberRequest.InterestMethod, RepaymentFrequency = string.IsNullOrWhiteSpace(memberRequest.RepaymentFrequency) ? product.DefaultRepaymentFrequency : memberRequest.RepaymentFrequency, LoanPurpose = memberRequest.LoanPurpose, ScoreResult = evaluation.ScoreResult, EligibilityStatus = evaluation.IsEligible ? "ELIGIBLE" : "BLOCKED", CreditBureauCheckId = evaluation.CreditBureauCheckId, ExistingExposureAmount = member.CurrentExposure, SavingsBalanceAtApplication = memberRequest.SavingsBalanceAtApplication, GuarantorNotes = memberRequest.GuarantorNotes, Status = "DRAFT" });
        }
        var eligibleCount = application.Members.Count(m => m.EligibilityStatus == "ELIGIBLE"); if (groupRules != null && eligibleCount < groupRules.MinMembersRequired) throw new InvalidOperationException($"Minimum eligible member count of {groupRules.MinMembersRequired} was not met");
        application.TotalRequestedAmount = application.Members.Sum(x => x.RequestedAmount); application.TotalApprovedAmount = application.Members.Sum(x => x.ApprovedAmount);
        _context.GroupLoanApplications.Add(application); await _context.SaveChangesAsync(); await LogAsync("GROUP_APPLICATION_CREATED", "GROUP_LOAN_APPLICATION", application.Id, request); return await GetApplicationAsync(application.Id);
    }

    public async Task<GroupLoanApplicationDto> GetApplicationAsync(string id)
    {
        var application = await _context.GroupLoanApplications.Include(a => a.Group).Include(a => a.Product).Include(a => a.Members).FirstOrDefaultAsync(a => a.Id == id) ?? throw new InvalidOperationException("Application not found");
        var customerIds = application.Members.Select(x => x.CustomerId).Distinct().ToList(); var customerNames = await _context.Customers.Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);
        return new GroupLoanApplicationDto { Id = application.Id, GroupId = application.GroupId, GroupName = application.Group?.Name ?? application.GroupId, LoanCycleNo = application.LoanCycleNo, ApplicationDate = application.ApplicationDate, ProductId = application.ProductId, ProductName = application.Product?.Name ?? application.ProductId, BranchId = application.BranchId, OfficerId = application.OfficerId, Status = application.Status, TotalApprovedAmount = application.TotalApprovedAmount, TotalRequestedAmount = application.TotalRequestedAmount, TotalDisbursedAmount = application.TotalDisbursedAmount, ApprovalDate = application.ApprovalDate, DisbursementDate = application.DisbursementDate, MeetingReference = application.MeetingReference, GroupResolutionReference = application.GroupResolutionReference, Notes = application.Notes, Members = application.Members.Select(m => new GroupLoanApplicationMemberDto { Id = m.Id, GroupMemberId = m.GroupMemberId, CustomerId = m.CustomerId, CustomerName = customerNames.GetValueOrDefault(m.CustomerId, m.CustomerId), RequestedAmount = m.RequestedAmount, ApprovedAmount = m.ApprovedAmount, DisbursedAmount = m.DisbursedAmount, TenureWeeks = m.TenureWeeks, InterestRate = m.InterestRate, InterestMethod = m.InterestMethod, RepaymentFrequency = m.RepaymentFrequency, LoanPurpose = m.LoanPurpose, EligibilityStatus = m.EligibilityStatus, ExistingExposureAmount = m.ExistingExposureAmount, SavingsBalanceAtApplication = m.SavingsBalanceAtApplication, ScoreResult = m.ScoreResult, Status = m.Status }).ToList() };
    }

    public async Task<GroupLoanApplicationDto> SubmitApplicationAsync(string id)
    {
        var application = await _context.GroupLoanApplications.Include(a => a.Members).FirstOrDefaultAsync(a => a.Id == id) ?? throw new InvalidOperationException("Application not found"); application.Status = "SUBMITTED"; foreach (var member in application.Members.Where(m => m.EligibilityStatus == "ELIGIBLE")) member.Status = "SUBMITTED"; await _context.SaveChangesAsync(); await _approvalService.CreateApprovalAsync(new CreateApprovalRequest { WorkflowId = null, EntityType = "GROUP_LOAN_APPLICATION", EntityId = application.Id, RequesterId = _currentUser.UserId }); await LogAsync("GROUP_APPLICATION_SUBMITTED", "GROUP_LOAN_APPLICATION", id, null); return await GetApplicationAsync(id);
    }

    public async Task<GroupLoanApplicationDto> ReviewApplicationAsync(string id, ReviewGroupLoanApplicationRequest request)
    {
        var application = await _context.GroupLoanApplications.Include(a => a.Members).FirstOrDefaultAsync(a => a.Id == id) ?? throw new InvalidOperationException("Application not found"); application.Status = "REVIEWED"; if (request.ApprovedAmounts != null) foreach (var member in application.Members) if (request.ApprovedAmounts.TryGetValue(member.Id, out var amount)) member.ApprovedAmount = amount; application.TotalApprovedAmount = application.Members.Sum(m => m.ApprovedAmount); await _context.SaveChangesAsync(); await LogAsync("GROUP_APPLICATION_REVIEWED", "GROUP_LOAN_APPLICATION", id, request); return await GetApplicationAsync(id);
    }

    public async Task<GroupLoanApplicationDto> ApproveApplicationAsync(string id, ApproveGroupLoanApplicationRequest request)
    {
        var application = await _context.GroupLoanApplications.Include(a => a.Members).FirstOrDefaultAsync(a => a.Id == id) ?? throw new InvalidOperationException("Application not found"); var rules = await _context.ProductGroupRules.FirstOrDefaultAsync(r => r.ProductId == application.ProductId); var eligibleCount = application.Members.Count(m => m.ApprovedAmount > 0 && m.EligibilityStatus == "ELIGIBLE"); if (rules != null && eligibleCount < rules.MinMembersRequired) throw new InvalidOperationException("Minimum eligible member count was not met for approval"); application.Status = "APPROVED"; application.ApprovalDate = DateTime.UtcNow; foreach (var member in application.Members.Where(m => m.ApprovedAmount > 0)) member.Status = "APPROVED"; await _context.SaveChangesAsync(); await LogAsync("GROUP_APPLICATION_APPROVED", "GROUP_LOAN_APPLICATION", id, request); return await GetApplicationAsync(id);
    }

    public async Task<GroupLoanApplicationDto> RejectApplicationAsync(string id, RejectGroupLoanApplicationRequest request)
    {
        var application = await _context.GroupLoanApplications.Include(a => a.Members).FirstOrDefaultAsync(a => a.Id == id) ?? throw new InvalidOperationException("Application not found"); application.Status = "REJECTED"; foreach (var member in application.Members) member.Status = "REJECTED"; await _context.SaveChangesAsync(); await LogAsync("GROUP_APPLICATION_REJECTED", "GROUP_LOAN_APPLICATION", id, request); return await GetApplicationAsync(id);
    }

    public async Task<GroupLoanApplicationDto> DisburseApplicationAsync(string id, DisburseGroupLoanApplicationRequest request)
    {
        var application = await _context.GroupLoanApplications.Include(a => a.Group).Include(a => a.Product).Include(a => a.Members).FirstOrDefaultAsync(a => a.Id == id) ?? throw new InvalidOperationException("Application not found"); if (!string.Equals(application.Status, "APPROVED", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only approved applications can be disbursed");
        var product = application.Product ?? throw new InvalidOperationException("Application product not found"); var rules = await _context.ProductGroupRules.FirstOrDefaultAsync(r => r.ProductId == application.ProductId); var disbursementDate = request.DisbursementDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var member in application.Members.Where(m => m.ApprovedAmount > 0))
        {
            var loanId = $"GLN-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(100, 999)}"; var loan = new Loan { Id = loanId, CustomerId = member.CustomerId, GroupId = application.GroupId, ProductCode = product.Id, Principal = member.ApprovedAmount, Rate = member.InterestRate, TermMonths = NormalizeTermMonths(member.RepaymentFrequency, member.TenureWeeks), InterestMethod = member.InterestMethod, RepaymentFrequency = member.RepaymentFrequency, ScheduleType = member.RepaymentFrequency, DisbursementDate = disbursementDate, Status = "ACTIVE", ApplicationDate = application.ApplicationDate, ApprovedAt = application.ApprovalDate, ApprovedBy = _currentUser.UserId, MakerId = application.OfficerId, CheckerId = _currentUser.UserId, DisbursedAt = DateTime.UtcNow, OutstandingBalance = member.ApprovedAmount, ParBucket = "0", BranchId = application.BranchId }; _context.Loans.Add(loan);
            _context.LoanAccounts.Add(new LoanAccount { LoanId = loanId, BranchId = application.BranchId, AppraisalStatus = LoanAppraisalStatus.Approved, ExposureAmount = member.ApprovedAmount, ConcentrationGroup = application.Group?.GroupCode ?? application.GroupId, AppraisedAt = application.ApprovalDate, AppraisedBy = _currentUser.UserId, CreatedAt = DateTime.UtcNow });
            foreach (var line in GenerateSchedule(member.ApprovedAmount, member.InterestRate, member.TenureWeeks, member.InterestMethod, member.RepaymentFrequency, disbursementDate)) _context.LoanSchedules.Add(new LoanSchedule { LoanId = loanId, Period = line.Period, DueDate = line.DueDate, Principal = line.Principal, Interest = line.Interest, Total = line.Total, Balance = line.Balance, Status = "DUE" });
            _context.GroupLoanAccounts.Add(new GroupLoanAccount { Id = $"GLACC-{Guid.NewGuid():N}"[..22], LoanAccountId = loanId, GroupId = application.GroupId, GroupLoanApplicationId = application.Id, GroupMemberId = member.GroupMemberId, CustomerId = member.CustomerId, LoanCycleNo = application.LoanCycleNo, GroupGuaranteeReference = application.GroupResolutionReference, IsUnderJointLiability = product.SupportsJointLiability, MeetingDayOfWeek = application.Group?.MeetingDayOfWeek ?? application.Group?.MeetingDay, AssignedOfficerId = application.OfficerId, ImpairmentStageHint = "Stage1" });
            _context.LoanDisclosures.Add(new LoanDisclosure { LoanId = loanId, DisclosureText = application.DisclosedTermsSnapshotJson ?? BuildDisclosureSnapshot(product, rules), Accepted = true, AcceptedAt = application.ApprovalDate ?? DateTime.UtcNow, Channel = "GROUP_MEETING" });
            member.DisbursedAmount = member.ApprovedAmount; member.Status = "DISBURSED"; var groupMember = await _context.GroupMembers.FirstOrDefaultAsync(x => x.Id == member.GroupMemberId); if (groupMember != null) { groupMember.CurrentLoanCycle = application.LoanCycleNo; groupMember.CurrentExposure = member.ApprovedAmount; groupMember.IsEligibleForLoan = false; }
            await _loanAccountingPostingService.PostEventAsync(loan, LoanAccountingEventType.Disbursement, member.ApprovedAmount, _currentUser.UserId, $"Group loan disbursement for {application.Id}");
        }
        application.TotalDisbursedAmount = application.Members.Sum(x => x.DisbursedAmount); application.DisbursementDate = DateTime.UtcNow; application.Status = "DISBURSED"; await _context.SaveChangesAsync(); await LogAsync("GROUP_APPLICATION_DISBURSED", "GROUP_LOAN_APPLICATION", id, request); return await GetApplicationAsync(id);
    }

    public async Task<GroupMeetingDto> CreateMeetingAsync(CreateGroupMeetingRequest request)
    {
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId) ?? throw new InvalidOperationException("Group not found"); var meeting = new GroupMeeting { Id = $"GMT-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", GroupId = request.GroupId, CenterId = request.CenterId ?? group.CenterId, MeetingDate = request.MeetingDate, MeetingType = request.MeetingType, Location = request.Location ?? group.MeetingLocation, OfficerId = request.OfficerId ?? group.AssignedOfficerId, Status = "OPEN", Notes = request.Notes }; _context.GroupMeetings.Add(meeting); await _context.SaveChangesAsync(); await LogAsync("GROUP_MEETING_CREATED", "GROUP_MEETING", meeting.Id, request); return await GetMeetingAsync(meeting.Id);
    }

    public async Task<GroupMeetingDto> GetMeetingAsync(string id)
    {
        var meeting = await _context.GroupMeetings.Include(m => m.Group).Include(m => m.Attendances).FirstOrDefaultAsync(m => m.Id == id) ?? throw new InvalidOperationException("Meeting not found");
        return new GroupMeetingDto { Id = meeting.Id, GroupId = meeting.GroupId, GroupName = meeting.Group?.Name ?? meeting.GroupId, CenterId = meeting.CenterId, MeetingDate = meeting.MeetingDate, MeetingType = meeting.MeetingType, Location = meeting.Location, OfficerId = meeting.OfficerId, Status = meeting.Status, AttendanceCount = meeting.AttendanceCount, Notes = meeting.Notes, Attendances = meeting.Attendances.Select(a => new GroupMeetingAttendanceLineRequest { GroupMemberId = a.GroupMemberId, CustomerId = a.CustomerId, AttendanceStatus = a.AttendanceStatus, ArrivalTime = a.ArrivalTime, Notes = a.Notes }).ToList() };
    }

    public async Task<GroupMeetingDto> RecordAttendanceAsync(string id, GroupMeetingAttendanceRequest request)
    {
        var meeting = await _context.GroupMeetings.Include(m => m.Attendances).FirstOrDefaultAsync(m => m.Id == id) ?? throw new InvalidOperationException("Meeting not found"); _context.GroupMeetingAttendances.RemoveRange(meeting.Attendances); foreach (var attendance in request.Attendances) _context.GroupMeetingAttendances.Add(new GroupMeetingAttendance { Id = $"GMTA-{Guid.NewGuid():N}"[..22], GroupMeetingId = id, GroupMemberId = attendance.GroupMemberId, CustomerId = attendance.CustomerId, AttendanceStatus = attendance.AttendanceStatus, ArrivalTime = attendance.ArrivalTime, Notes = attendance.Notes }); meeting.AttendanceCount = request.Attendances.Count(x => string.Equals(x.AttendanceStatus, "PRESENT", StringComparison.OrdinalIgnoreCase)); await _context.SaveChangesAsync(); await LogAsync("GROUP_MEETING_ATTENDANCE_RECORDED", "GROUP_MEETING", id, request); return await GetMeetingAsync(id);
    }

    public async Task<GroupMeetingDto> CloseMeetingAsync(string id)
    {
        var meeting = await _context.GroupMeetings.FirstOrDefaultAsync(m => m.Id == id) ?? throw new InvalidOperationException("Meeting not found"); meeting.Status = "CLOSED"; await _context.SaveChangesAsync(); await LogAsync("GROUP_MEETING_CLOSED", "GROUP_MEETING", id, null); return await GetMeetingAsync(id);
    }
}


