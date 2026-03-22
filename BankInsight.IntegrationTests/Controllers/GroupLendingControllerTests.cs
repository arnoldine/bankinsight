using System.Net;
using System.Net.Http.Json;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BankInsight.IntegrationTests.Controllers;

public class GroupLendingControllerTests : IntegrationTestBase
{
    public GroupLendingControllerTests(TestWebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task GroupEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        var response = await Client.GetAsync("/api/group-lending/groups");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProductGroupRules_RoundTripSuccessfully()
    {
        await AuthenticateAsync();

        var getResponse = await Client.GetAsync("/api/group-lending/product-designer/loan-products/PRD_GRP_WEEKLY/group-rules");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var existing = await getResponse.Content.ReadFromJsonAsync<ProductGroupRulesDto>();
        existing.Should().NotBeNull();
        existing!.ProductId.Should().Be("PRD_GRP_WEEKLY");
        existing.DefaultRepaymentFrequency.Should().Be("Weekly");
        existing.MinMembersRequired.Should().BeGreaterThan(0);

        var updateRequest = new ProductGroupRulesDto
        {
            ProductId = "PRD_GRP_WEEKLY",
            MinMembersRequired = 5,
            MaxMembersAllowed = 35,
            MinWeeks = 8,
            MaxWeeks = 30,
            RequiresCompulsorySavings = true,
            MinSavingsToLoanRatio = 0.15m,
            RequiresGroupApprovalMeeting = true,
            RequiresJointLiability = true,
            AllowTopUp = true,
            AllowReschedule = true,
            MaxCycleNumber = 6,
            DefaultRepaymentFrequency = "Weekly",
            DefaultInterestMethod = "Flat",
            AllocationOrderJson = "[\"Penalty\",\"Fees\",\"Interest\",\"Principal\",\"Savings\"]",
            MeetingCollectionRuleJson = "{\"mode\":\"MEETING_BATCH\"}",
            AccountingProfileJson = "{\"portfolioGl\":\"1300\"}",
            DisclosureTemplate = "GH_GROUP_WEEKLY_STANDARD_V2"
        };

        var putResponse = await Client.PutAsJsonAsync("/api/group-lending/product-designer/loan-products/PRD_GRP_WEEKLY/group-rules", updateRequest);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await putResponse.Content.ReadFromJsonAsync<ProductGroupRulesDto>();
        updated.Should().NotBeNull();
        updated!.MaxMembersAllowed.Should().Be(35);
        updated.MinSavingsToLoanRatio.Should().Be(0.15m);
        updated.DisclosureTemplate.Should().Be("GH_GROUP_WEEKLY_STANDARD_V2");
    }

    [Fact]
    public async Task GroupLoanLifecycle_CreatesDisbursesAndPostsCollectionBatch()
    {
        await AuthenticateAsync();

        var scenario = await CreateDisbursedScenarioAsync(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));

        var scheduleResponse = await Client.GetAsync($"/api/group-lending/loans/{scenario.FirstLoanAccount!.LoanAccountId}/schedule");
        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var schedule = await scheduleResponse.Content.ReadFromJsonAsync<List<LoanScheduleDto>>();
        schedule.Should().NotBeNull();
        schedule!.Should().HaveCount(12);
        schedule.Sum(x => x.Total).Should().BeGreaterThan(1000m);

        var meetingResponse = await Client.PostAsJsonAsync("/api/group-lending/meetings", new CreateGroupMeetingRequest
        {
            GroupId = scenario.Group!.Id,
            CenterId = scenario.Center!.Id,
            MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            MeetingType = "REGULAR",
            Location = "Adenta Market",
            OfficerId = "STF0001",
            Notes = "Weekly center meeting"
        });
        meetingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var meeting = await meetingResponse.Content.ReadFromJsonAsync<GroupMeetingDto>();
        meeting.Should().NotBeNull();

        var attendanceResponse = await Client.PostAsJsonAsync($"/api/group-lending/meetings/{meeting!.Id}/attendance", new GroupMeetingAttendanceRequest
        {
            Attendances = scenario.Group.Members.Select(member => new GroupMeetingAttendanceLineRequest
            {
                GroupMemberId = member.Id,
                CustomerId = member.CustomerId,
                AttendanceStatus = "PRESENT",
                ArrivalTime = DateTime.UtcNow
            }).ToList()
        });
        attendanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstInstallment = schedule.First().Total;
        var batchResponse = await Client.PostAsJsonAsync("/api/group-lending/collections/batches", new CreateGroupCollectionBatchRequest
        {
            GroupId = scenario.Group.Id,
            GroupMeetingId = meeting.Id,
            BranchId = "BR001",
            OfficerId = "STF0001",
            CollectionDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Channel = "CASH",
            ReferenceNo = $"COLL-{scenario.Suffix}",
            Lines = new List<GroupCollectionBatchLineRequest>
            {
                new()
                {
                    LoanAccountId = scenario.FirstLoanAccount.LoanAccountId,
                    GroupMemberId = scenario.FirstLoanAccount.GroupMemberId,
                    CustomerId = scenario.FirstLoanAccount.CustomerId,
                    ExpectedInstallment = firstInstallment,
                    AmountCollected = firstInstallment,
                    SavingsComponent = 20m
                }
            }
        });
        batchResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var batch = await batchResponse.Content.ReadFromJsonAsync<GroupCollectionBatchDto>();
        batch.Should().NotBeNull();
        batch!.Status.Should().Be("OPEN");

        var postBatchResponse = await Client.PostAsync($"/api/group-lending/collections/batches/{batch.Id}/post", JsonContent.Create(new { }));
        postBatchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var postedBatch = await postBatchResponse.Content.ReadFromJsonAsync<GroupCollectionBatchDto>();
        postedBatch.Should().NotBeNull();
        postedBatch!.Status.Should().Be("POSTED");
        postedBatch.Lines.Should().ContainSingle();
        postedBatch.Lines[0].PrincipalComponent.Should().BeGreaterThan(0m);

        var statementResponse = await Client.GetAsync($"/api/group-lending/loans/{scenario.FirstLoanAccount.LoanAccountId}/statement");
        statementResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var statementJson = await statementResponse.Content.ReadAsStringAsync();
        statementJson.Should().Contain("repayments");

        var parResponse = await Client.GetAsync("/api/group-lending/reports/par");
        parResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var performanceResponse = await Client.GetAsync("/api/group-lending/reports/group-performance");
        performanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var performance = await performanceResponse.Content.ReadFromJsonAsync<GroupPortfolioSummaryDto>();
        performance.Should().NotBeNull();
        performance!.ActiveGroups.Should().BeGreaterThan(0);
        performance.ActiveMembers.Should().BeGreaterThanOrEqualTo(5);
        performance.CollectionsThisWeek.Should().BeGreaterThan(0m);

        var closeMeetingResponse = await Client.PostAsync($"/api/group-lending/meetings/{meeting.Id}/close", JsonContent.Create(new { }));
        closeMeetingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateApplication_WhenArrearsReduceEligibleMembersBelowMinimum_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var suffix = $"ARR-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var setup = await CreateGroupWithMembersAsync(suffix);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var firstMemberId = setup.Group!.Members.First().Id;
            var member = await db.GroupMembers.FirstAsync(x => x.Id == firstMemberId);
            member.ArrearsFlag = true;
            member.CurrentExposure = 4800m;
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsJsonAsync("/api/group-lending/applications", new CreateGroupLoanApplicationRequest
        {
            GroupId = setup.Group!.Id,
            ProductId = "PRD_GRP_WEEKLY",
            BranchId = "BR001",
            OfficerId = "STF0001",
            MeetingReference = $"MREF-{suffix}",
            GroupResolutionReference = $"GRES-{suffix}",
            Notes = "Arrears eligibility validation",
            Members = setup.Group.Members.Select(member => new GroupLoanApplicationMemberRequest
            {
                GroupMemberId = member.Id,
                RequestedAmount = 1000m,
                TenureWeeks = 12,
                InterestRate = 36m,
                InterestMethod = "Flat",
                RepaymentFrequency = "Weekly",
                LoanPurpose = "Working capital",
                SavingsBalanceAtApplication = 250m,
                GuarantorNotes = "Approved by group"
            }).ToList()
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        body.Should().Contain("Minimum eligible member count");
    }

    [Fact]
    public async Task ParReport_WhenScheduleIsOverdue_UpdatesBucketAndClassification()
    {
        await AuthenticateAsync();

        var scenario = await CreateDisbursedScenarioAsync($"PAR-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var loanId = scenario.FirstLoanAccount!.LoanAccountId;
            var schedules = await db.LoanSchedules.Where(x => x.LoanId == loanId).OrderBy(x => x.Period).ToListAsync();
            schedules[0].DueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-40));
            schedules[0].Status = "DUE";
            schedules[0].PaidAmount = 0m;
            schedules[0].PaidDate = null;
            schedules[1].DueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-33));
            schedules[1].Status = "DUE";
            schedules[1].PaidAmount = 0m;
            schedules[1].PaidDate = null;

            var loan = await db.Loans.FirstAsync(x => x.Id == loanId);
            loan.OutstandingBalance = loan.Principal;
            loan.ParBucket = "0";
            await db.SaveChangesAsync();
        }

        var parResponse = await Client.GetAsync("/api/group-lending/reports/par");
        parResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var parItems = await parResponse.Content.ReadFromJsonAsync<List<GroupParReportItemDto>>();
        parItems.Should().NotBeNull();
        var groupItem = parItems!.FirstOrDefault(x => x.GroupId == scenario.Group!.Id);
        groupItem.Should().NotBeNull();
        groupItem!.DaysPastDue.Should().BeGreaterThanOrEqualTo(40);
        groupItem.ParBucket.Should().Be("31-60");

        var delinquencyResponse = await Client.GetAsync("/api/group-lending/reports/delinquency");
        delinquencyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var delinquencyJson = await delinquencyResponse.Content.ReadAsStringAsync();
        delinquencyJson.Should().Contain("31-60");

        using var verificationScope = Factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var snapshot = await verificationDb.GroupLoanDelinquencySnapshots
            .Where(x => x.LoanAccountId == scenario.FirstLoanAccount.LoanAccountId)
            .OrderByDescending(x => x.SnapshotDate)
            .FirstOrDefaultAsync();
        snapshot.Should().NotBeNull();
        snapshot!.ParBucket.Should().Be("31-60");
        snapshot.Classification.Should().Be("SUBSTANDARD");
    }

    [Fact]
    public async Task CreateApplication_WhenPriorCycleCompleted_AssignsNextCycleAndUpdatesCycleAnalysis()
    {
        await AuthenticateAsync();

        var scenario = await CreateDisbursedScenarioAsync($"CYCLE-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
        await MarkScenarioLoansClosedAsync(scenario);

        var secondCycleResponse = await Client.PostAsJsonAsync("/api/group-lending/applications", new CreateGroupLoanApplicationRequest
        {
            GroupId = scenario.Group!.Id,
            ProductId = "PRD_GRP_WEEKLY",
            BranchId = "BR001",
            OfficerId = "STF0001",
            MeetingReference = $"MREF2-{scenario.Suffix}",
            GroupResolutionReference = $"GRES2-{scenario.Suffix}",
            Notes = "Cycle two application",
            Members = scenario.Group.Members.Select(member => new GroupLoanApplicationMemberRequest
            {
                GroupMemberId = member.Id,
                RequestedAmount = 1200m,
                TenureWeeks = 10,
                InterestRate = 30m,
                InterestMethod = "Flat",
                RepaymentFrequency = "Weekly",
                LoanPurpose = "Inventory expansion",
                SavingsBalanceAtApplication = 300m,
                GuarantorNotes = "Cycle two approved"
            }).ToList()
        });
        secondCycleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondCycleApplication = await secondCycleResponse.Content.ReadFromJsonAsync<GroupLoanApplicationDto>();
        secondCycleApplication.Should().NotBeNull();
        secondCycleApplication!.LoanCycleNo.Should().Be(2);

        var submitResponse = await Client.PostAsync($"/api/group-lending/applications/{secondCycleApplication.Id}/submit", JsonContent.Create(new { }));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approveResponse = await Client.PostAsJsonAsync($"/api/group-lending/applications/{secondCycleApplication.Id}/approve", new ApproveGroupLoanApplicationRequest
        {
            DecisionNotes = "Approved for cycle two"
        });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var disburseResponse = await Client.PostAsJsonAsync($"/api/group-lending/applications/{secondCycleApplication.Id}/disburse", new DisburseGroupLoanApplicationRequest
        {
            DisbursementDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            ClientReference = $"DISB2-{scenario.Suffix}"
        });
        disburseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cycleAnalysisResponse = await Client.GetAsync("/api/group-lending/reports/cycle-analysis");
        cycleAnalysisResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cycleAnalysisJson = await cycleAnalysisResponse.Content.ReadAsStringAsync();
        cycleAnalysisJson.Should().Contain("\"cycleNo\":1");
        cycleAnalysisJson.Should().Contain("\"cycleNo\":2");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var groupMemberIds = scenario.Group.Members.Select(x => x.Id).ToList();
        var members = await db.GroupMembers.Where(x => groupMemberIds.Contains(x.Id)).ToListAsync();
        members.Should().OnlyContain(x => x.CurrentLoanCycle == 2);
    }

    [Fact]
    public async Task RescheduleLoan_WhenProductAllows_UpdatesScheduleAndFlagsGroupAccount()
    {
        await AuthenticateAsync();

        var scenario = await CreateDisbursedScenarioAsync($"RSCH-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

        var response = await Client.PostAsJsonAsync($"/api/group-lending/loans/{scenario.FirstLoanAccount!.LoanAccountId}/reschedule", new LoanRestructureRequest
        {
            LoanId = scenario.FirstLoanAccount.LoanAccountId,
            NewTermInPeriods = 16,
            NewAnnualRate = 28m,
            NewRepaymentFrequency = "Weekly",
            Reason = "Meeting-approved reschedule"
        });
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
        responseBody.Should().Contain("restructuredFlag", StringComparison.OrdinalIgnoreCase);
        responseBody.Should().Contain("true");

        var scheduleResponse = await Client.GetAsync($"/api/group-lending/loans/{scenario.FirstLoanAccount.LoanAccountId}/schedule");
        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var schedule = await scheduleResponse.Content.ReadFromJsonAsync<List<LoanScheduleDto>>();
        schedule.Should().NotBeNull();
        schedule!.Should().HaveCount(16);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var groupLoanAccount = await db.GroupLoanAccounts.FirstAsync(x => x.LoanAccountId == scenario.FirstLoanAccount.LoanAccountId);
        groupLoanAccount.RestructuredFlag.Should().BeTrue();
        groupLoanAccount.ImpairmentStageHint.Should().Be("Stage2");
    }

    [Fact]
    public async Task RescheduleLoan_WhenProductRuleDisallows_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var scenario = await CreateDisbursedScenarioAsync($"RSCHBLOCK-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

        var rulesResponse = await Client.GetAsync("/api/group-lending/product-designer/loan-products/PRD_GRP_WEEKLY/group-rules");
        rulesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await rulesResponse.Content.ReadFromJsonAsync<ProductGroupRulesDto>();
        rules.Should().NotBeNull();
        rules!.AllowReschedule = false;

        var updateRulesResponse = await Client.PutAsJsonAsync("/api/group-lending/product-designer/loan-products/PRD_GRP_WEEKLY/group-rules", rules);
        updateRulesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await Client.PostAsJsonAsync($"/api/group-lending/loans/{scenario.FirstLoanAccount!.LoanAccountId}/reschedule", new LoanRestructureRequest
        {
            LoanId = scenario.FirstLoanAccount.LoanAccountId,
            NewTermInPeriods = 18,
            NewAnnualRate = 26m,
            NewRepaymentFrequency = "Weekly",
            Reason = "Blocked by product policy"
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        body.Should().Contain("does not allow rescheduling");
    }
    private async Task<GroupScenario> CreateDisbursedScenarioAsync(string suffix)
    {
        var setup = await CreateGroupWithMembersAsync(suffix);

        var applicationResponse = await Client.PostAsJsonAsync("/api/group-lending/applications", new CreateGroupLoanApplicationRequest
        {
            GroupId = setup.Group!.Id,
            ProductId = "PRD_GRP_WEEKLY",
            BranchId = "BR001",
            OfficerId = "STF0001",
            MeetingReference = $"MREF-{suffix}",
            GroupResolutionReference = $"GRES-{suffix}",
            Notes = "Cycle one application",
            Members = setup.Group.Members.Select(member => new GroupLoanApplicationMemberRequest
            {
                GroupMemberId = member.Id,
                RequestedAmount = 1000m,
                TenureWeeks = 12,
                InterestRate = 36m,
                InterestMethod = "Flat",
                RepaymentFrequency = "Weekly",
                LoanPurpose = "Working capital",
                SavingsBalanceAtApplication = 250m,
                GuarantorNotes = "Approved by group"
            }).ToList()
        });
        applicationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var application = await applicationResponse.Content.ReadFromJsonAsync<GroupLoanApplicationDto>();
        application.Should().NotBeNull();
        application!.Members.Should().HaveCount(5);
        application.TotalApprovedAmount.Should().Be(5000m);

        var submitResponse = await Client.PostAsync($"/api/group-lending/applications/{application.Id}/submit", JsonContent.Create(new { }));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approveResponse = await Client.PostAsJsonAsync($"/api/group-lending/applications/{application.Id}/approve", new ApproveGroupLoanApplicationRequest
        {
            DecisionNotes = "Approved for disbursement"
        });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var disburseResponse = await Client.PostAsJsonAsync($"/api/group-lending/applications/{application.Id}/disburse", new DisburseGroupLoanApplicationRequest
        {
            DisbursementDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            ClientReference = $"DISB-{suffix}"
        });
        var disburseBody = await disburseResponse.Content.ReadAsStringAsync();
        disburseResponse.StatusCode.Should().Be(HttpStatusCode.OK, disburseBody);
        var disbursedApplication = await disburseResponse.Content.ReadFromJsonAsync<GroupLoanApplicationDto>();
        disbursedApplication.Should().NotBeNull();
        disbursedApplication!.Status.Should().Be("DISBURSED");
        disbursedApplication.TotalDisbursedAmount.Should().Be(5000m);

        setup.Application = disbursedApplication;
        setup.FirstLoanAccount = await GetFirstLoanAccountAsync(application.Id);
        setup.FirstLoanAccount.Should().NotBeNull();

        return setup;
    }

    private async Task<GroupScenario> CreateGroupWithMembersAsync(string suffix)
    {
        var customers = await SeedCustomersAsync(suffix, 5);

        var centerResponse = await Client.PostAsJsonAsync("/api/group-lending/centers", new CreateLendingCenterRequest
        {
            BranchId = "BR001",
            CenterCode = $"CTR-{suffix}",
            CenterName = $"Center {suffix}",
            MeetingDayOfWeek = "Tuesday",
            MeetingLocation = "Adenta Market",
            AssignedOfficerId = "STF0001"
        });
        centerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var center = await centerResponse.Content.ReadFromJsonAsync<LendingCenterDto>();
        center.Should().NotBeNull();

        var groupResponse = await Client.PostAsJsonAsync("/api/group-lending/groups", new CreateLendingGroupRequest
        {
            GroupName = $"Trust Group {suffix}",
            BranchId = "BR001",
            CenterId = center!.Id,
            GroupCode = $"GRP-{suffix}",
            MeetingDayOfWeek = "Tuesday",
            MeetingFrequency = "Weekly",
            MeetingLocation = "Adenta Market",
            AssignedOfficerId = "STF0001",
            IsJointLiabilityEnabled = true,
            MaxMembers = 10,
            Notes = "Integration test group"
        });
        groupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var group = await groupResponse.Content.ReadFromJsonAsync<LendingGroupDto>();
        group.Should().NotBeNull();

        foreach (var customerId in customers)
        {
            var addMemberResponse = await Client.PostAsJsonAsync($"/api/group-lending/groups/{group!.Id}/members", new AddLendingGroupMemberRequest
            {
                CustomerId = customerId,
                MemberRole = "MEMBER",
                IsFoundingMember = true,
                ShareContribution = 100m,
                GuarantorIndicator = true,
                SocialCollateralNotes = "Peer guarantee"
            });

            addMemberResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var hydratedGroupResponse = await Client.GetAsync($"/api/group-lending/groups/{group!.Id}");
        hydratedGroupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var hydratedGroup = await hydratedGroupResponse.Content.ReadFromJsonAsync<LendingGroupDto>();
        hydratedGroup.Should().NotBeNull();
        hydratedGroup!.Members.Should().HaveCount(5);
        hydratedGroup.Members.Should().OnlyContain(member => member.KycStatus == "COMPLETE");

        return new GroupScenario
        {
            Suffix = suffix,
            Center = center,
            Group = hydratedGroup
        };
    }

    private async Task<List<string>> SeedCustomersAsync(string suffix, int count)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customerIds = new List<string>();

        for (var i = 1; i <= count; i++)
        {
            var customerId = $"CUST-GRP-{suffix}-{i:D2}";
            customerIds.Add(customerId);

            if (await db.Customers.FindAsync(customerId) != null)
            {
                continue;
            }

            db.Customers.Add(new Customer
            {
                Id = customerId,
                Type = "INDIVIDUAL",
                Name = $"Group Member {i} {suffix}",
                Email = $"groupmember{i}-{suffix}@bankinsight.local",
                Phone = $"233240000{i:D2}",
                BranchId = "BR001",
                KycLevel = "Tier 2",
                RiskRating = "Low",
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return customerIds;
    }

    private async Task MarkScenarioLoansClosedAsync(GroupScenario scenario)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var loanIds = await db.GroupLoanAccounts
            .Where(x => x.GroupId == scenario.Group!.Id && x.GroupLoanApplicationId == scenario.Application!.Id)
            .Select(x => x.LoanAccountId)
            .ToListAsync();

        var loans = await db.Loans.Where(x => loanIds.Contains(x.Id)).ToListAsync();
        foreach (var loan in loans)
        {
            loan.Status = "CLOSED";
            loan.OutstandingBalance = 0m;
            loan.ParBucket = "0";
        }

        var schedules = await db.LoanSchedules.Where(x => loanIds.Contains(x.LoanId)).ToListAsync();
        foreach (var schedule in schedules)
        {
            schedule.Status = "PAID";
            schedule.PaidAmount = schedule.Total;
            schedule.PaidDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        }

        var memberIds = scenario.Group.Members.Select(x => x.Id).ToList();
        var members = await db.GroupMembers.Where(x => memberIds.Contains(x.Id)).ToListAsync();
        foreach (var member in members)
        {
            member.CurrentExposure = 0m;
            member.ArrearsFlag = false;
            member.IsEligibleForLoan = true;
        }

        await db.SaveChangesAsync();
    }
    private async Task<GroupLoanAccount?> GetFirstLoanAccountAsync(string applicationId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.GroupLoanAccounts.FirstOrDefaultAsync(x => x.GroupLoanApplicationId == applicationId);
    }

    private sealed class GroupScenario
    {
        public string Suffix { get; set; } = string.Empty;
        public LendingCenterDto? Center { get; set; }
        public LendingGroupDto? Group { get; set; }
        public GroupLoanApplicationDto? Application { get; set; }
        public GroupLoanAccount? FirstLoanAccount { get; set; }
    }
}

