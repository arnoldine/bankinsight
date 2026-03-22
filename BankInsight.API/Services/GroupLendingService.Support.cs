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
    public async Task<GroupCollectionBatchDto> CreateCollectionBatchAsync(CreateGroupCollectionBatchRequest request)
    {
        var batch = new GroupCollectionBatch { Id = $"GCB-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", GroupId = request.GroupId, GroupMeetingId = request.GroupMeetingId, BranchId = request.BranchId, OfficerId = request.OfficerId, CollectionDate = request.CollectionDate, Status = "OPEN", Channel = request.Channel, ReferenceNo = request.ReferenceNo };
        foreach (var line in request.Lines) batch.Lines.Add(new GroupCollectionBatchLine { Id = $"GCBL-{Guid.NewGuid():N}"[..22], LoanAccountId = line.LoanAccountId, GroupMemberId = line.GroupMemberId, CustomerId = line.CustomerId, ExpectedInstallment = line.ExpectedInstallment, AmountCollected = line.AmountCollected, SavingsComponent = line.SavingsComponent, Status = "PENDING" });
        batch.TotalExpectedAmount = batch.Lines.Sum(x => x.ExpectedInstallment); batch.TotalCollectedAmount = batch.Lines.Sum(x => x.AmountCollected + x.SavingsComponent); batch.VarianceAmount = batch.TotalCollectedAmount - batch.TotalExpectedAmount;
        _context.GroupCollectionBatches.Add(batch); await _context.SaveChangesAsync(); await LogAsync("GROUP_COLLECTION_BATCH_CREATED", "GROUP_COLLECTION_BATCH", batch.Id, request); return await GetCollectionBatchAsync(batch.Id);
    }

    public async Task<GroupCollectionBatchDto> GetCollectionBatchAsync(string id)
    {
        var batch = await _context.GroupCollectionBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == id) ?? throw new InvalidOperationException("Collection batch not found"); return Map(batch);
    }

    public async Task<GroupCollectionBatchDto> PostCollectionBatchAsync(string id)
    {
        var batch = await _context.GroupCollectionBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == id) ?? throw new InvalidOperationException("Collection batch not found"); if (batch.Status != "OPEN") throw new InvalidOperationException("Only open batches can be posted"); foreach (var line in batch.Lines) { await ApplyRepaymentAsync(line.LoanAccountId, line.AmountCollected, line.CustomerId, batch.ReferenceNo, line); line.Status = "POSTED"; } batch.TotalCollectedAmount = batch.Lines.Sum(x => x.AmountCollected + x.SavingsComponent); batch.TotalExpectedAmount = batch.Lines.Sum(x => x.ExpectedInstallment); batch.VarianceAmount = batch.TotalCollectedAmount - batch.TotalExpectedAmount; batch.Status = "POSTED"; await _context.SaveChangesAsync(); await LogAsync("GROUP_COLLECTION_BATCH_POSTED", "GROUP_COLLECTION_BATCH", id, null); return await GetCollectionBatchAsync(id);
    }

    public async Task<GroupCollectionBatchDto> ReverseCollectionBatchAsync(string id)
    {
        var batch = await _context.GroupCollectionBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == id) ?? throw new InvalidOperationException("Collection batch not found"); batch.Status = "REVERSED"; foreach (var line in batch.Lines) line.Status = "REVERSED"; await _context.SaveChangesAsync(); await LogAsync("GROUP_COLLECTION_BATCH_REVERSED", "GROUP_COLLECTION_BATCH", id, null); return await GetCollectionBatchAsync(id);
    }

    public async Task<object> RepayLoanAsync(string loanId, GroupRepaymentRequest request)
    {
        await ApplyRepaymentAsync(loanId, request.Amount, null, request.ClientReference, null); return await GetLoanStatementAsync(loanId);
    }
    public async Task<object> RescheduleLoanAsync(string loanId, LoanRestructureRequest request)
    {
        var groupLoanAccount = await _context.GroupLoanAccounts.FirstOrDefaultAsync(x => x.LoanAccountId == loanId) ?? throw new InvalidOperationException("Loan is not linked to a group loan account");
        var application = await _context.GroupLoanApplications.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == groupLoanAccount.GroupLoanApplicationId) ?? throw new InvalidOperationException("Group loan application not found");
        var product = application.Product ?? await _context.Products.FirstOrDefaultAsync(x => x.Id == application.ProductId) ?? throw new InvalidOperationException("Group loan product not found");
        var rules = await _context.ProductGroupRules.FirstOrDefaultAsync(x => x.ProductId == product.Id);
        var allowReschedule = rules?.AllowReschedule ?? product.AllowRescheduleWithinGroup;
        if (!allowReschedule)
        {
            throw new InvalidOperationException("Selected group loan product does not allow rescheduling");
        }

        var normalizedRequest = new LoanRestructureRequest
        {
            LoanId = loanId,
            NewTermInPeriods = request.NewTermInPeriods,
            NewAnnualRate = request.NewAnnualRate,
            NewRepaymentFrequency = request.NewRepaymentFrequency,
            Reason = request.Reason
        };

        await _loanService.RestructureLoanAsync(normalizedRequest, _currentUser.UserId);
        groupLoanAccount.RestructuredFlag = true;
        groupLoanAccount.ImpairmentStageHint = "Stage2";
        await _context.SaveChangesAsync();
        await RefreshSnapshotAsync(loanId);
        await LogAsync("GROUP_LOAN_RESCHEDULED", "LOAN", loanId, normalizedRequest);
        return await GetLoanStatementAsync(loanId);
    }

    public async Task<List<LoanScheduleDto>> GetLoanScheduleAsync(string loanId) => await _context.LoanSchedules.Where(x => x.LoanId == loanId).OrderBy(x => x.Period).Select(x => new LoanScheduleDto { Period = x.Period ?? 0, DueDate = x.DueDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Principal = x.Principal ?? 0m, Interest = x.Interest ?? 0m, Total = x.Total ?? 0m, Balance = x.Balance ?? 0m, Status = x.Status ?? "DUE", PaidDate = x.PaidDate }).ToListAsync();

    public async Task<object> GetLoanStatementAsync(string loanId)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(x => x.Id == loanId) ?? throw new InvalidOperationException("Loan not found");
        var schedule = await GetLoanScheduleAsync(loanId);
        var repayments = await _context.LoanRepayments
            .Where(x => x.LoanId == loanId)
            .OrderByDescending(x => x.RepaymentDate)
            .Select(x => new
            {
                x.Id,
                x.RepaymentDate,
                x.Amount,
                x.PrincipalComponent,
                x.InterestComponent,
                x.PenaltyComponent,
                x.Reference,
                x.ProcessedBy,
                x.IsReversal,
                x.ReversalReference
            })
            .ToListAsync();
        var groupAccount = await _context.GroupLoanAccounts.FirstOrDefaultAsync(x => x.LoanAccountId == loanId);
        return new
        {
            LoanId = loan.Id,
            CustomerId = loan.CustomerId,
            GroupId = groupAccount?.GroupId,
            Principal = loan.Principal,
            OutstandingBalance = loan.OutstandingBalance,
            Status = loan.Status,
            RepaymentFrequency = loan.RepaymentFrequency,
            Schedule = schedule,
            Repayments = repayments,
            CycleNo = groupAccount?.LoanCycleNo,
            GuaranteeReference = groupAccount?.GroupGuaranteeReference,
            RestructuredFlag = groupAccount?.RestructuredFlag ?? false
        };
    }

    public async Task<List<GroupParReportItemDto>> GetParReportAsync() { await RefreshSnapshotsAsync(); return await _context.GroupLoanDelinquencySnapshots.Join(_context.Groups, s => s.GroupId, g => g.Id, (s, g) => new GroupParReportItemDto { GroupId = g.Id, GroupName = g.Name, OutstandingPrincipal = s.OutstandingPrincipal, DaysPastDue = s.DaysPastDue, ParBucket = s.ParBucket }).OrderByDescending(x => x.DaysPastDue).ToListAsync(); }
    public async Task<GroupPortfolioSummaryDto> GetGroupPerformanceAsync() { await RefreshSnapshotsAsync(); var startOfWeek = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1)); var endOfWeek = startOfWeek.AddDays(6); return new GroupPortfolioSummaryDto { ActiveGroups = await _context.Groups.CountAsync(g => g.Status == "ACTIVE"), ActiveMembers = await _context.GroupMembers.CountAsync(m => m.Status == "ACTIVE"), TotalPortfolio = await _context.Loans.Where(l => l.GroupId != null && l.Status == "ACTIVE").SumAsync(l => l.OutstandingBalance ?? 0m), Par30 = await _context.GroupLoanDelinquencySnapshots.Where(s => s.DaysPastDue > 30).SumAsync(s => s.OutstandingPrincipal), WeeklyDueThisWeek = await _context.LoanSchedules.Include(s => s.Loan).Where(s => s.DueDate >= startOfWeek && s.DueDate <= endOfWeek && s.Loan != null && s.Loan.GroupId != null).SumAsync(s => s.Total ?? 0m), CollectionsThisWeek = await _context.GroupCollectionBatches.Where(b => b.CollectionDate >= startOfWeek && b.CollectionDate <= endOfWeek && b.Status == "POSTED").SumAsync(b => b.TotalCollectedAmount) }; }
    public async Task<object> GetOfficerPerformanceAsync() => await _context.Groups.GroupBy(g => g.AssignedOfficerId ?? g.OfficerId ?? "UNASSIGNED").Select(g => new { OfficerId = g.Key, ActiveGroups = g.Count(), ActiveMembers = g.SelectMany(x => x.Members).Count(x => x.Status == "ACTIVE") }).ToListAsync();
    public async Task<object> GetCycleAnalysisAsync() => await _context.GroupLoanAccounts.GroupBy(x => x.LoanCycleNo).Select(g => new { CycleNo = g.Key, Accounts = g.Count(), Exposure = g.Join(_context.Loans, a => a.LoanAccountId, l => l.Id, (a, l) => l.OutstandingBalance ?? 0m).Sum() }).OrderBy(x => x.CycleNo).ToListAsync();
    public async Task<object> GetDelinquencyReportAsync() { await RefreshSnapshotsAsync(); return await _context.GroupLoanDelinquencySnapshots.GroupBy(x => x.ParBucket).Select(g => new { ParBucket = g.Key, Accounts = g.Count(), Exposure = g.Sum(x => x.OutstandingPrincipal) }).OrderBy(x => x.ParBucket).ToListAsync(); }
    public async Task<object> GetMeetingCollectionsReportAsync() => await _context.GroupCollectionBatches.GroupBy(x => x.CollectionDate).Select(g => new { CollectionDate = g.Key, Batches = g.Count(), TotalCollected = g.Sum(x => x.TotalCollectedAmount), TotalExpected = g.Sum(x => x.TotalExpectedAmount) }).OrderByDescending(x => x.CollectionDate).ToListAsync();

    private async Task<LendingGroupDto> ChangeGroupStatusAsync(string id, string status) { var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == id) ?? throw new InvalidOperationException("Group not found"); group.Status = status; group.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); await LogAsync($"GROUP_{status}", "GROUP", id, null); return await GetGroupAsync(id); }
    private async Task<MemberEligibilityEvaluation> EvaluateMemberEligibilityAsync(GroupMember member, GroupLoanApplicationMemberRequest request, ProductEligibilityRule? rules) { var isEligible = true; var reasons = new List<string>(); Guid? creditCheckId = null; if (rules?.RequiresKycComplete != false && !string.Equals(member.KycStatus, "COMPLETE", StringComparison.OrdinalIgnoreCase)) { isEligible = false; reasons.Add("KYC incomplete"); } if (rules?.BlockOnSevereArrears == true && member.ArrearsFlag) { isEligible = false; reasons.Add("Member has unresolved arrears"); } if (rules?.MaxAllowedExposure.HasValue == true && member.CurrentExposure + request.RequestedAmount > rules.MaxAllowedExposure.Value) { isEligible = false; reasons.Add("Exposure limit exceeded"); } if (rules?.RequireCreditBureauCheck == true) { var bureauResult = await _creditBureauService.CheckCreditAsync(member.CustomerId, rules.CreditBureauProvider); var check = new CreditBureauCheck { CustomerId = member.CustomerId, BureauName = bureauResult.BureauName, ProviderName = bureauResult.ProviderName, InquiryReference = bureauResult.InquiryReference, Score = bureauResult.Score, RiskBand = bureauResult.RiskBand, RiskGrade = bureauResult.RiskGrade, Decision = bureauResult.Decision, Recommendation = bureauResult.Recommendation, RequestPayload = bureauResult.RequestPayload, RawResponse = bureauResult.RawResponse, IsTimeout = bureauResult.IsTimeout, RetryCount = bureauResult.RetryCount, Status = bureauResult.Status, CheckedAt = DateTime.UtcNow }; _context.CreditBureauChecks.Add(check); await _context.SaveChangesAsync(); creditCheckId = check.Id; if (rules.MinimumCreditScore.HasValue && bureauResult.Score < rules.MinimumCreditScore.Value) { isEligible = false; reasons.Add($"Credit score below minimum threshold ({bureauResult.Score})"); } } return new MemberEligibilityEvaluation { IsEligible = isEligible, CreditBureauCheckId = creditCheckId, ScoreResult = reasons.Count == 0 ? "Eligible" : string.Join("; ", reasons) }; }
    private async Task ApplyRepaymentAsync(string loanId, decimal amount, string? customerId, string? reference, GroupCollectionBatchLine? batchLine) { var loan = await _context.Loans.Include(l => l.Schedules).FirstOrDefaultAsync(l => l.Id == loanId) ?? throw new InvalidOperationException("Loan not found"); var remaining = amount; decimal principal = 0m; decimal interest = 0m; foreach (var schedule in loan.Schedules.Where(s => s.Status != "PAID").OrderBy(s => s.Period)) { var outstandingInterest = Math.Max(0m, (schedule.Interest ?? 0m) - Math.Max(0m, (schedule.PaidAmount ?? 0m) - (schedule.Principal ?? 0m))); var outstandingPrincipal = Math.Max(0m, (schedule.Principal ?? 0m) - Math.Min(schedule.PaidAmount ?? 0m, schedule.Principal ?? 0m)); var due = outstandingInterest + outstandingPrincipal; if (due <= 0m || remaining <= 0m) continue; var applied = Math.Min(remaining, due); var interestPart = Math.Min(applied, outstandingInterest); var principalPart = Math.Min(applied - interestPart, outstandingPrincipal); interest += interestPart; principal += principalPart; remaining -= applied; schedule.PaidAmount = (schedule.PaidAmount ?? 0m) + applied; if ((schedule.PaidAmount ?? 0m) >= (schedule.Total ?? 0m)) { schedule.Status = "PAID"; schedule.PaidDate = DateOnly.FromDateTime(DateTime.UtcNow); } } loan.OutstandingBalance = Math.Max(0m, (loan.OutstandingBalance ?? loan.Principal) - principal); loan.ParBucket = DetermineParBucket(CalculateDaysPastDue(loan.Schedules)); _context.LoanRepayments.Add(new LoanRepayment { LoanId = loanId, RepaymentDate = DateTime.UtcNow, Amount = amount, PrincipalComponent = principal, InterestComponent = interest, PenaltyComponent = 0m, Reference = string.IsNullOrWhiteSpace(reference) ? $"GRP-RPY-{DateTime.UtcNow:yyyyMMddHHmmss}" : reference, ProcessedBy = _currentUser.UserId }); if (batchLine != null) { batchLine.PrincipalComponent = principal; batchLine.InterestComponent = interest; batchLine.ArrearsRecovered = Math.Max(0m, amount - batchLine.ExpectedInstallment); } await _loanAccountingPostingService.PostEventAsync(loan, LoanAccountingEventType.Repayment, amount, _currentUser.UserId, $"Group repayment for {loanId}"); await RefreshSnapshotAsync(loanId); }
    private async Task RefreshSnapshotsAsync() { var loanIds = await _context.GroupLoanAccounts.Select(x => x.LoanAccountId).Distinct().ToListAsync(); foreach (var loanId in loanIds) await RefreshSnapshotAsync(loanId); }
    private async Task RefreshSnapshotAsync(string loanId) { var loan = await _context.Loans.Include(l => l.Schedules).FirstOrDefaultAsync(l => l.Id == loanId); var groupLoanAccount = await _context.GroupLoanAccounts.FirstOrDefaultAsync(x => x.LoanAccountId == loanId); if (loan == null || groupLoanAccount == null) return; var dpd = CalculateDaysPastDue(loan.Schedules); var parBucket = DetermineParBucket(dpd); var snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow); var existing = await _context.GroupLoanDelinquencySnapshots.FirstOrDefaultAsync(x => x.LoanAccountId == loanId && x.SnapshotDate == snapshotDate); var snapshot = existing ?? new GroupLoanDelinquencySnapshot { Id = $"GLDS-{Guid.NewGuid():N}"[..22], LoanAccountId = loanId, GroupId = groupLoanAccount.GroupId, GroupMemberId = groupLoanAccount.GroupMemberId, SnapshotDate = snapshotDate }; snapshot.DaysPastDue = dpd; snapshot.InstallmentsInArrears = loan.Schedules.Count(x => x.Status != "PAID" && x.DueDate < snapshotDate); snapshot.OutstandingPrincipal = loan.OutstandingBalance ?? 0m; snapshot.OutstandingInterest = loan.Schedules.Where(x => x.Status != "PAID").Sum(x => x.Interest ?? 0m); snapshot.OutstandingPenalty = 0m; snapshot.ParBucket = parBucket; snapshot.Classification = Classify(dpd); snapshot.IsNpl = dpd >= 90; if (existing == null) _context.GroupLoanDelinquencySnapshots.Add(snapshot); var member = await _context.GroupMembers.FirstOrDefaultAsync(x => x.Id == groupLoanAccount.GroupMemberId); if (member != null) { member.CurrentExposure = loan.OutstandingBalance ?? 0m; member.ArrearsFlag = dpd > 0; member.IsEligibleForLoan = dpd == 0; } await _context.SaveChangesAsync(); }
    private static List<LoanScheduleDto> GenerateSchedule(decimal principal, decimal annualRate, int periods, string interestMethod, string repaymentFrequency, DateOnly startDate) { var lines = new List<LoanScheduleDto>(); var weekly = string.Equals(repaymentFrequency, "Weekly", StringComparison.OrdinalIgnoreCase); var periodsPerYear = weekly ? 52m : 12m; var ratePerPeriod = annualRate <= 0 ? 0m : (annualRate / 100m) / periodsPerYear; var opening = principal; if (string.Equals(interestMethod, "Flat", StringComparison.OrdinalIgnoreCase)) { var totalInterest = Math.Round(principal * (annualRate / 100m) * (periods / periodsPerYear), 2, MidpointRounding.AwayFromZero); var interestPerPeriod = Math.Round(totalInterest / periods, 2, MidpointRounding.AwayFromZero); var principalPerPeriod = Math.Round(principal / periods, 2, MidpointRounding.AwayFromZero); for (var i = 1; i <= periods; i++) { var principalComponent = i == periods ? opening : principalPerPeriod; var balance = Math.Max(0m, Math.Round(opening - principalComponent, 2, MidpointRounding.AwayFromZero)); lines.Add(new LoanScheduleDto { Period = i, DueDate = weekly ? startDate.AddDays(7 * i) : startDate.AddMonths(i), Principal = principalComponent, Interest = interestPerPeriod, Total = principalComponent + interestPerPeriod, Balance = balance, Status = "DUE" }); opening = balance; } return lines; } var installment = ratePerPeriod == 0m ? Math.Round(principal / periods, 2, MidpointRounding.AwayFromZero) : Math.Round((decimal)((double)principal * (double)ratePerPeriod / (1 - Math.Pow(1 + (double)ratePerPeriod, -periods))), 2, MidpointRounding.AwayFromZero); for (var i = 1; i <= periods; i++) { var interest = Math.Round(opening * ratePerPeriod, 2, MidpointRounding.AwayFromZero); var principalComponent = i == periods ? opening : Math.Round(installment - interest, 2, MidpointRounding.AwayFromZero); var balance = Math.Max(0m, Math.Round(opening - principalComponent, 2, MidpointRounding.AwayFromZero)); var total = i == periods ? principalComponent + interest : installment; lines.Add(new LoanScheduleDto { Period = i, DueDate = weekly ? startDate.AddDays(7 * i) : startDate.AddMonths(i), Principal = principalComponent, Interest = interest, Total = total, Balance = balance, Status = "DUE" }); opening = balance; } return lines; }
    private static int CalculateDaysPastDue(IEnumerable<LoanSchedule> schedules) { var today = DateOnly.FromDateTime(DateTime.UtcNow); var oldest = schedules.Where(x => x.Status != "PAID" && x.DueDate.HasValue && x.DueDate.Value < today).OrderBy(x => x.DueDate).FirstOrDefault(); return oldest?.DueDate is DateOnly dueDate ? (today.DayNumber - dueDate.DayNumber) : 0; }
    private static string DetermineParBucket(int dpd) => dpd <= 0 ? "0" : dpd <= 7 ? "1-7" : dpd <= 14 ? "8-14" : dpd <= 30 ? "15-30" : dpd <= 60 ? "31-60" : dpd <= 90 ? "61-90" : "90+";
    private static string Classify(int dpd) => dpd <= 0 ? "CURRENT" : dpd <= 30 ? "WATCHLIST" : dpd <= 90 ? "SUBSTANDARD" : dpd <= 180 ? "DOUBTFUL" : "LOSS";
    private static string NormalizeKycStatus(string? kycLevel) { var normalized = (kycLevel ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant(); return normalized is "TIER2" or "TIER3" ? "COMPLETE" : "PENDING"; }
    private static int ResolveNextCycle(IEnumerable<GroupMember> members) => members.Any() ? members.Max(x => x.CurrentLoanCycle) + 1 : 1;
    private static string BuildDisclosureSnapshot(Product product, ProductGroupRule? rules) => JsonSerializer.Serialize(new { product.Id, product.Name, product.InterestRate, product.InterestMethod, product.DefaultRepaymentFrequency, product.RequiresCompulsorySavings, product.MinimumSavingsToLoanRatio, product.SupportsJointLiability, product.GroupPenaltyPolicy, product.GroupDelinquencyPolicy, AllocationOrder = rules?.AllocationOrderJson, AccountingProfile = rules?.AccountingProfileJson, DisclosureTemplate = rules?.DisclosureTemplate });
    private async Task LogAsync(string action, string entityType, string entityId, object? payload) => await _auditLoggingService.LogActionAsync(action, entityType, entityId, _currentUser.UserId, status: "SUCCESS", newValues: new { Payload = payload, BranchId = ResolveBranchId(), Channel = "WEB", DeviceOrIp = "API" });
    private string ResolveBranchId() => string.IsNullOrWhiteSpace(_currentUser.BranchId) ? "BR001" : _currentUser.BranchId;
    private static GroupMemberSummaryDto Map(GroupMember member) => Map(member, member.Customer?.Name ?? member.CustomerId);
    private static GroupMemberSummaryDto Map(GroupMember member, string customerName) => new() { Id = member.Id, CustomerId = member.CustomerId, CustomerName = customerName, MemberRole = member.MemberRole, Status = member.Status, KycStatus = member.KycStatus, IsEligibleForLoan = member.IsEligibleForLoan, CurrentLoanCycle = member.CurrentLoanCycle, CurrentExposure = member.CurrentExposure, ArrearsFlag = member.ArrearsFlag };
    private static ProductGroupRulesDto Map(ProductGroupRule rules) => new() { ProductId = rules.ProductId, MinMembersRequired = rules.MinMembersRequired, MaxMembersAllowed = rules.MaxMembersAllowed, MinWeeks = rules.MinWeeks, MaxWeeks = rules.MaxWeeks, RequiresCompulsorySavings = rules.RequiresCompulsorySavings, MinSavingsToLoanRatio = rules.MinSavingsToLoanRatio, RequiresGroupApprovalMeeting = rules.RequiresGroupApprovalMeeting, RequiresJointLiability = rules.RequiresJointLiability, AllowTopUp = rules.AllowTopUp, AllowReschedule = rules.AllowReschedule, MaxCycleNumber = rules.MaxCycleNumber, CycleIncrementRulesJson = rules.CycleIncrementRulesJson, DefaultRepaymentFrequency = rules.DefaultRepaymentFrequency, DefaultInterestMethod = rules.DefaultInterestMethod, PenaltyPolicyJson = rules.PenaltyPolicyJson, AttendanceRuleJson = rules.AttendanceRuleJson, EligibilityRuleJson = rules.EligibilityRuleJson, MeetingCollectionRuleJson = rules.MeetingCollectionRuleJson, AllocationOrderJson = rules.AllocationOrderJson, AccountingProfileJson = rules.AccountingProfileJson, DisclosureTemplate = rules.DisclosureTemplate };
    private static ProductEligibilityRulesDto Map(ProductEligibilityRule rules) => new() { ProductId = rules.ProductId, RequiresKycComplete = rules.RequiresKycComplete, BlockOnSevereArrears = rules.BlockOnSevereArrears, MaxAllowedExposure = rules.MaxAllowedExposure, MinMembershipDays = rules.MinMembershipDays, MinAttendanceRate = rules.MinAttendanceRate, RequireCreditBureauCheck = rules.RequireCreditBureauCheck, CreditBureauProvider = rules.CreditBureauProvider, MinimumCreditScore = rules.MinimumCreditScore, RuleJson = rules.RuleJson };
    private static LendingCenterDto Map(LendingCenter center) => new() { Id = center.Id, BranchId = center.BranchId, CenterCode = center.CenterCode, CenterName = center.CenterName, MeetingDayOfWeek = center.MeetingDayOfWeek, MeetingLocation = center.MeetingLocation, AssignedOfficerId = center.AssignedOfficerId, Status = center.Status };
    private static GroupCollectionBatchDto Map(GroupCollectionBatch batch) => new() { Id = batch.Id, GroupId = batch.GroupId, GroupMeetingId = batch.GroupMeetingId, BranchId = batch.BranchId, OfficerId = batch.OfficerId, CollectionDate = batch.CollectionDate, Status = batch.Status, TotalCollectedAmount = batch.TotalCollectedAmount, TotalExpectedAmount = batch.TotalExpectedAmount, VarianceAmount = batch.VarianceAmount, Channel = batch.Channel, ReferenceNo = batch.ReferenceNo, Lines = batch.Lines.Select(x => new GroupCollectionBatchLineDto { Id = x.Id, LoanAccountId = x.LoanAccountId, GroupMemberId = x.GroupMemberId, CustomerId = x.CustomerId, ExpectedInstallment = x.ExpectedInstallment, AmountCollected = x.AmountCollected, PrincipalComponent = x.PrincipalComponent, InterestComponent = x.InterestComponent, PenaltyComponent = x.PenaltyComponent, SavingsComponent = x.SavingsComponent, FeeComponent = x.FeeComponent, ArrearsRecovered = x.ArrearsRecovered, Status = x.Status }).ToList() };
    private static int NormalizeTermMonths(string repaymentFrequency, int tenureWeeks) => string.Equals(repaymentFrequency, "Weekly", StringComparison.OrdinalIgnoreCase) ? Math.Max(1, (int)Math.Ceiling(tenureWeeks / 4.0)) : tenureWeeks;
    private sealed class MemberEligibilityEvaluation { public bool IsEligible { get; set; } public Guid? CreditBureauCheckId { get; set; } public string ScoreResult { get; set; } = string.Empty; }
}


