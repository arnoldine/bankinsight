using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public partial class GroupLendingService
{
    /*
     * GROUP LOAN GAP REPORT
     * IMPLEMENTED: customers/CIF, branches, base groups, loans, GL posting, audit trail, approvals, credit bureau abstraction, loan disclosures, delinquency dashboard.
     * PARTIALLY IMPLEMENTED: product designer metadata, weekly schedule support, arrears tracking, restructuring, collections, officer workflows, reporting dashboards.
     * MISSING BEFORE THIS CHANGE: product-driven group lending rules, centers, rich group membership, cycle applications, meeting operations, bulk collections, joint-liability tracking, member exposure snapshots, group PAR reporting.
     */
    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ICreditBureauService _creditBureauService;
    private readonly ILoanAccountingPostingService _loanAccountingPostingService;
    private readonly ApprovalService _approvalService;
    private readonly ICurrentUserContext _currentUser;
    private readonly LoanService _loanService;

    public GroupLendingService(ApplicationDbContext context, IAuditLoggingService auditLoggingService, ICreditBureauService creditBureauService, ILoanAccountingPostingService loanAccountingPostingService, ApprovalService approvalService, ICurrentUserContext currentUser, LoanService loanService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _creditBureauService = creditBureauService;
        _loanAccountingPostingService = loanAccountingPostingService;
        _approvalService = approvalService;
        _currentUser = currentUser;
        _loanService = loanService;
    }

    public async Task<ProductGroupRulesDto> GetGroupRulesAsync(string productId)
    {
        var rules = await _context.ProductGroupRules.FirstOrDefaultAsync(x => x.ProductId == productId);
        return rules == null ? new ProductGroupRulesDto { ProductId = productId } : Map(rules);
    }

    public async Task<ProductGroupRulesDto> UpsertGroupRulesAsync(string productId, ProductGroupRulesDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == productId) ?? throw new InvalidOperationException("Product not found");
        var rules = await _context.ProductGroupRules.FirstOrDefaultAsync(x => x.ProductId == productId) ?? new ProductGroupRule { ProductId = productId };
        rules.MinMembersRequired = dto.MinMembersRequired; rules.MaxMembersAllowed = dto.MaxMembersAllowed; rules.MinWeeks = dto.MinWeeks; rules.MaxWeeks = dto.MaxWeeks; rules.RequiresCompulsorySavings = dto.RequiresCompulsorySavings; rules.MinSavingsToLoanRatio = dto.MinSavingsToLoanRatio; rules.RequiresGroupApprovalMeeting = dto.RequiresGroupApprovalMeeting; rules.RequiresJointLiability = dto.RequiresJointLiability; rules.AllowTopUp = dto.AllowTopUp; rules.AllowReschedule = dto.AllowReschedule; rules.MaxCycleNumber = dto.MaxCycleNumber; rules.CycleIncrementRulesJson = dto.CycleIncrementRulesJson; rules.DefaultRepaymentFrequency = dto.DefaultRepaymentFrequency; rules.DefaultInterestMethod = dto.DefaultInterestMethod; rules.PenaltyPolicyJson = dto.PenaltyPolicyJson; rules.AttendanceRuleJson = dto.AttendanceRuleJson; rules.EligibilityRuleJson = dto.EligibilityRuleJson; rules.MeetingCollectionRuleJson = dto.MeetingCollectionRuleJson; rules.AllocationOrderJson = dto.AllocationOrderJson; rules.AccountingProfileJson = dto.AccountingProfileJson; rules.DisclosureTemplate = dto.DisclosureTemplate; rules.UpdatedAt = DateTime.UtcNow;
        if (_context.Entry(rules).State == EntityState.Detached) _context.ProductGroupRules.Add(rules);
        product.IsGroupLoanEnabled = true; product.RequiresGroup = true; product.DefaultRepaymentFrequency = dto.DefaultRepaymentFrequency; product.SupportsWeeklyRepayment = string.Equals(dto.DefaultRepaymentFrequency, "Weekly", StringComparison.OrdinalIgnoreCase); product.MinimumGroupSize = dto.MinMembersRequired; product.MaximumGroupSize = dto.MaxMembersAllowed; product.RequiresCompulsorySavings = dto.RequiresCompulsorySavings; product.MinimumSavingsToLoanRatio = dto.MinSavingsToLoanRatio; product.RequiresGroupApprovalMeeting = dto.RequiresGroupApprovalMeeting; product.SupportsJointLiability = dto.RequiresJointLiability; product.AllowTopUpWithinGroup = dto.AllowTopUp; product.AllowRescheduleWithinGroup = dto.AllowReschedule; product.MaxCycleNumber = dto.MaxCycleNumber; product.LendingMethodology = "GROUP";
        await _context.SaveChangesAsync();
        await LogAsync("GROUP_RULES_UPDATED", "PRODUCT", productId, dto);
        return Map(rules);
    }

    public async Task<ProductEligibilityRulesDto> GetEligibilityRulesAsync(string productId)
    {
        var rules = await _context.ProductEligibilityRules.FirstOrDefaultAsync(x => x.ProductId == productId);
        return rules == null ? new ProductEligibilityRulesDto { ProductId = productId } : Map(rules);
    }

    public async Task<ProductEligibilityRulesDto> UpsertEligibilityRulesAsync(string productId, ProductEligibilityRulesDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == productId) ?? throw new InvalidOperationException("Product not found");
        var rules = await _context.ProductEligibilityRules.FirstOrDefaultAsync(x => x.ProductId == productId) ?? new ProductEligibilityRule { ProductId = productId };
        rules.RequiresKycComplete = dto.RequiresKycComplete; rules.BlockOnSevereArrears = dto.BlockOnSevereArrears; rules.MaxAllowedExposure = dto.MaxAllowedExposure; rules.MinMembershipDays = dto.MinMembershipDays; rules.MinAttendanceRate = dto.MinAttendanceRate; rules.RequireCreditBureauCheck = dto.RequireCreditBureauCheck; rules.CreditBureauProvider = dto.CreditBureauProvider; rules.MinimumCreditScore = dto.MinimumCreditScore; rules.RuleJson = dto.RuleJson; rules.UpdatedAt = DateTime.UtcNow;
        if (_context.Entry(rules).State == EntityState.Detached) _context.ProductEligibilityRules.Add(rules);
        product.UsesMemberLevelUnderwriting = true;
        await _context.SaveChangesAsync();
        await LogAsync("ELIGIBILITY_RULES_UPDATED", "PRODUCT", productId, dto);
        return Map(rules);
    }

    public async Task<LendingGroupDto> CreateGroupAsync(CreateLendingGroupRequest request)
    {
        var id = $"GLG-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var group = new Group { Id = id, Name = request.GroupName, GroupCode = string.IsNullOrWhiteSpace(request.GroupCode) ? id : request.GroupCode, BranchId = string.IsNullOrWhiteSpace(request.BranchId) ? ResolveBranchId() : request.BranchId, CenterId = request.CenterId, MeetingDay = request.MeetingDayOfWeek, MeetingDayOfWeek = request.MeetingDayOfWeek, MeetingFrequency = request.MeetingFrequency, MeetingLocation = request.MeetingLocation, OfficerId = request.AssignedOfficerId, AssignedOfficerId = request.AssignedOfficerId, ChairpersonCustomerId = request.ChairpersonCustomerId, SecretaryCustomerId = request.SecretaryCustomerId, TreasurerCustomerId = request.TreasurerCustomerId, FormationDate = request.FormationDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Status = "PENDING", IsJointLiabilityEnabled = request.IsJointLiabilityEnabled, MaxMembers = request.MaxMembers, Notes = request.Notes, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Groups.Add(group); await _context.SaveChangesAsync(); await LogAsync("GROUP_CREATED", "GROUP", group.Id, request); return await GetGroupAsync(group.Id);
    }

    public async Task<List<LendingGroupDto>> GetGroupsAsync()
    {
        var groups = await _context.Groups
            .Include(g => g.Members)
            .ThenInclude(m => m.Customer)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return groups.Select(g => new LendingGroupDto
        {
            Id = g.Id,
            BranchId = g.BranchId,
            CenterId = g.CenterId,
            GroupCode = g.GroupCode,
            GroupName = g.Name,
            MeetingDayOfWeek = g.MeetingDayOfWeek ?? g.MeetingDay,
            MeetingFrequency = g.MeetingFrequency,
            MeetingLocation = g.MeetingLocation,
            AssignedOfficerId = g.AssignedOfficerId ?? g.OfficerId,
            ChairpersonCustomerId = g.ChairpersonCustomerId,
            SecretaryCustomerId = g.SecretaryCustomerId,
            TreasurerCustomerId = g.TreasurerCustomerId,
            FormationDate = g.FormationDate,
            Status = g.Status,
            IsJointLiabilityEnabled = g.IsJointLiabilityEnabled,
            MaxMembers = g.MaxMembers,
            Notes = g.Notes,
            Members = g.Members.Select(Map).ToList()
        }).ToList();
    }

    public async Task<LendingGroupDto> GetGroupAsync(string id)
    {
        var group = await _context.Groups.Include(g => g.Members).ThenInclude(m => m.Customer).FirstOrDefaultAsync(g => g.Id == id) ?? throw new InvalidOperationException("Group not found");
        return new LendingGroupDto { Id = group.Id, BranchId = group.BranchId, CenterId = group.CenterId, GroupCode = group.GroupCode, GroupName = group.Name, MeetingDayOfWeek = group.MeetingDayOfWeek ?? group.MeetingDay, MeetingFrequency = group.MeetingFrequency, MeetingLocation = group.MeetingLocation, AssignedOfficerId = group.AssignedOfficerId ?? group.OfficerId, ChairpersonCustomerId = group.ChairpersonCustomerId, SecretaryCustomerId = group.SecretaryCustomerId, TreasurerCustomerId = group.TreasurerCustomerId, FormationDate = group.FormationDate, Status = group.Status, IsJointLiabilityEnabled = group.IsJointLiabilityEnabled, MaxMembers = group.MaxMembers, Notes = group.Notes, Members = group.Members.Select(Map).ToList() };
    }

    public async Task<LendingGroupDto> UpdateGroupAsync(string id, UpdateLendingGroupRequest request)
    {
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == id) ?? throw new InvalidOperationException("Group not found");
        group.Name = request.GroupName; group.GroupCode = request.GroupCode ?? group.GroupCode; group.BranchId = request.BranchId; group.CenterId = request.CenterId; group.MeetingDay = request.MeetingDayOfWeek; group.MeetingDayOfWeek = request.MeetingDayOfWeek; group.MeetingFrequency = request.MeetingFrequency; group.MeetingLocation = request.MeetingLocation; group.AssignedOfficerId = request.AssignedOfficerId; group.OfficerId = request.AssignedOfficerId; group.ChairpersonCustomerId = request.ChairpersonCustomerId; group.SecretaryCustomerId = request.SecretaryCustomerId; group.TreasurerCustomerId = request.TreasurerCustomerId; group.FormationDate = request.FormationDate; group.Status = request.Status; group.IsJointLiabilityEnabled = request.IsJointLiabilityEnabled; group.MaxMembers = request.MaxMembers; group.Notes = request.Notes; group.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(); await LogAsync("GROUP_UPDATED", "GROUP", id, request); return await GetGroupAsync(id);
    }

    public Task<LendingGroupDto> ActivateGroupAsync(string id) => ChangeGroupStatusAsync(id, "ACTIVE");
    public Task<LendingGroupDto> SuspendGroupAsync(string id) => ChangeGroupStatusAsync(id, "SUSPENDED");

    public async Task<GroupMemberSummaryDto> AddMemberAsync(string groupId, AddLendingGroupMemberRequest request)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId) ?? throw new InvalidOperationException("Group not found");
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId) ?? throw new InvalidOperationException("Customer not found");
        if (group.MaxMembers.HasValue && group.Members.Count >= group.MaxMembers.Value) throw new InvalidOperationException("Group has reached its member limit");
        if (group.Members.Any(m => m.CustomerId == request.CustomerId && m.Status == "ACTIVE")) throw new InvalidOperationException("Customer is already an active member of the group");
        var member = new GroupMember { Id = $"GLM-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(100, 999)}", GroupId = groupId, CustomerId = request.CustomerId, MemberNo = (group.Members.Count + 1).ToString("D3"), JoinDate = DateOnly.FromDateTime(DateTime.UtcNow), MemberRole = request.MemberRole, Status = "ACTIVE", IsFoundingMember = request.IsFoundingMember, KycStatus = NormalizeKycStatus(customer.KycLevel), IsEligibleForLoan = NormalizeKycStatus(customer.KycLevel) == "COMPLETE", ShareContribution = request.ShareContribution, GuarantorIndicator = request.GuarantorIndicator, SocialCollateralNotes = request.SocialCollateralNotes };
        _context.GroupMembers.Add(member); await _context.SaveChangesAsync(); await LogAsync("GROUP_MEMBER_ADDED", "GROUP", groupId, request); return Map(member, customer.Name);
    }

    public async Task RemoveMemberAsync(string groupId, string memberId)
    {
        var member = await _context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.Id == memberId) ?? throw new InvalidOperationException("Group member not found");
        member.Status = "EXITED"; member.ExitDate = DateOnly.FromDateTime(DateTime.UtcNow); await _context.SaveChangesAsync(); await LogAsync("GROUP_MEMBER_REMOVED", "GROUP", groupId, new { memberId });
    }

    public async Task<LendingCenterDto> CreateCenterAsync(CreateLendingCenterRequest request)
    {
        var center = new LendingCenter { Id = $"CTR-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", BranchId = string.IsNullOrWhiteSpace(request.BranchId) ? ResolveBranchId() : request.BranchId, CenterCode = request.CenterCode, CenterName = request.CenterName, MeetingDayOfWeek = request.MeetingDayOfWeek, MeetingLocation = request.MeetingLocation, AssignedOfficerId = request.AssignedOfficerId, Status = "ACTIVE" };
        _context.LendingCenters.Add(center); await _context.SaveChangesAsync(); await LogAsync("CENTER_CREATED", "CENTER", center.Id, request); return Map(center);
    }

    public async Task<List<LendingCenterDto>> GetCentersAsync() => (await _context.LendingCenters.OrderBy(x => x.CenterName).ToListAsync()).Select(Map).ToList();
}



