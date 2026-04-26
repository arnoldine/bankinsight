using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BankInsight.API.DTOs;
using FluentAssertions;

namespace BankInsight.IntegrationTests.Controllers;

public class ClientChannelControllerTests : IntegrationTestBase
{
    public ClientChannelControllerTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task ClientLogin_WithDemoCustomer_CompletesMfaAndReturnsToken()
    {
        var response = await Client.PostAsJsonAsync("/api/client-auth/login", new ClientLoginRequest
        {
            Email = "akosua.mensah@bankinsight.local",
            Password = "ClientPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await response.Content.ReadFromJsonAsync<ClientLoginResponse>();
        login.Should().NotBeNull();
        login!.MfaRequired.Should().BeTrue();
        login.MfaToken.Should().NotBeNullOrEmpty();
        login.DebugCode.Should().NotBeNullOrEmpty();

        response = await Client.PostAsJsonAsync("/api/client-auth/mfa/verify", new ClientVerifyMfaRequest
        {
            MfaToken = login.MfaToken!,
            Code = login.DebugCode!
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var verified = await response.Content.ReadFromJsonAsync<ClientLoginResponse>();
        verified!.Token.Should().NotBeNullOrEmpty();
        verified.User!.CustomerId.Should().Be("CIF-2604-00001");
    }

    [Fact]
    public async Task ClientRegistration_ThenVerification_CreatesUsableCustomerSession()
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/client-auth/register", new ClientRegisterRequest
        {
            Name = "Kwame Test",
            Email = "kwame.test@bankinsight.local",
            Phone = "+233240000099",
            DigitalAddress = "GA-999-0001",
            Password = "Register123!"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var challenge = await registerResponse.Content.ReadFromJsonAsync<ClientVerificationChallengeResponse>();
        challenge!.ChallengeToken.Should().NotBeNullOrEmpty();
        challenge.DebugCode.Should().NotBeNullOrEmpty();

        var verifyResponse = await Client.PostAsJsonAsync("/api/client-auth/register/verify", new ClientVerifyRegistrationRequest
        {
            RegistrationToken = challenge.ChallengeToken,
            Code = challenge.DebugCode!
        });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verified = await verifyResponse.Content.ReadFromJsonAsync<ClientLoginResponse>();
        verified!.Token.Should().NotBeNullOrEmpty();
        verified.User!.Email.Should().Be("kwame.test@bankinsight.local");
    }

    [Fact]
    public async Task PasswordReset_CompletesWithVerificationCode_AndAllowsNewLogin()
    {
        var resetStart = await Client.PostAsJsonAsync("/api/client-auth/password/forgot", new ClientStartPasswordResetRequest
        {
            Email = "akosua.mensah@bankinsight.local"
        });

        resetStart.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetChallenge = await resetStart.Content.ReadFromJsonAsync<ClientPasswordResetStartResponse>();
        resetChallenge!.ResetToken.Should().NotBeNullOrEmpty();
        resetChallenge.DebugCode.Should().NotBeNullOrEmpty();

        var resetComplete = await Client.PostAsJsonAsync("/api/client-auth/password/reset", new ClientCompletePasswordResetRequest
        {
            ResetToken = resetChallenge.ResetToken!,
            Code = resetChallenge.DebugCode!,
            NewPassword = "ClientPass456!"
        });

        resetComplete.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await Client.PostAsJsonAsync("/api/client-auth/login", new ClientLoginRequest
        {
            Email = "akosua.mensah@bankinsight.local",
            Password = "ClientPass456!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BootstrapAndComplaintFlow_WorkForAuthenticatedCustomer()
    {
        await AuthenticateClientAsync();

        var bootstrapResponse = await Client.GetAsync("/api/client-channel/bootstrap");
        bootstrapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var bootstrap = await bootstrapResponse.Content.ReadFromJsonAsync<ClientChannelBootstrapResponse>();
        bootstrap!.LinkedCustomer.Should().NotBeNull();
        bootstrap.LinkedCustomer!.Email.Should().Be("akosua.mensah@bankinsight.local");

        var createResponse = await Client.PostAsJsonAsync("/api/client-channel/complaints", new CreateClientComplaintRequest
        {
            Category = "Account access",
            Summary = "Unexpected login review",
            Details = "Please review recent device activity and confirm if any suspicious sign-in occurred."
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var complaint = await createResponse.Content.ReadFromJsonAsync<ClientComplaintDetailDto>();
        complaint!.Reference.Should().NotBeNullOrEmpty();
        complaint.Events.Should().HaveCountGreaterOrEqualTo(2);

        var listResponse = await Client.GetAsync("/api/client-channel/complaints");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var complaints = await listResponse.Content.ReadFromJsonAsync<List<ClientComplaintListItemDto>>();
        complaints.Should().Contain(item => item.Id == complaint.Id);

        var detailResponse = await Client.GetAsync($"/api/client-channel/complaints/{complaint.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProfileUpdate_RequiresVerifiedStepUpToken()
    {
        await AuthenticateClientAsync();

        var stepUpStart = await Client.PostAsJsonAsync("/api/client-auth/step-up/initiate", new ClientStartStepUpRequest
        {
            Purpose = "PROFILE_UPDATE"
        });

        stepUpStart.StatusCode.Should().Be(HttpStatusCode.OK);
        var challenge = await stepUpStart.Content.ReadFromJsonAsync<ClientVerificationChallengeResponse>();
        challenge!.DebugCode.Should().NotBeNullOrEmpty();

        var stepUpVerify = await Client.PostAsJsonAsync("/api/client-auth/step-up/verify", new ClientVerifyStepUpRequest
        {
            ChallengeToken = challenge.ChallengeToken,
            Code = challenge.DebugCode!
        });

        stepUpVerify.StatusCode.Should().Be(HttpStatusCode.OK);
        var verified = await stepUpVerify.Content.ReadFromJsonAsync<ClientVerifiedStepUpResponse>();
        verified!.StepUpToken.Should().NotBeNullOrEmpty();

        var updateResponse = await Client.PutAsJsonAsync("/api/client-channel/profile", new UpdateClientProfileRequest
        {
            Name = "Akosua Mensah Updated",
            Email = "akosua.mensah@bankinsight.local",
            Phone = "+233240000001",
            DigitalAddress = "GA-123-4567",
            StepUpToken = verified.StepUpToken
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await updateResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        profile!.Name.Should().Be("Akosua Mensah Updated");
    }

    [Fact]
    public async Task StatementExport_ReturnsAuditableCsvPayload()
    {
        await AuthenticateClientAsync();

        var summariesResponse = await Client.GetAsync("/api/client-channel/statements");
        summariesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaries = await summariesResponse.Content.ReadFromJsonAsync<List<ClientStatementSummaryDto>>();
        summaries.Should().NotBeNull();
        summaries.Should().NotBeEmpty();

        var first = summaries!.First();
        var exportResponse = await Client.GetAsync($"/api/client-channel/statements/{first.AccountId}/export?year={first.Year}&month={first.Month}&format=csv");

        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var export = await exportResponse.Content.ReadFromJsonAsync<ClientStatementExportDto>();
        export.Should().NotBeNull();
        export!.FileName.Should().EndWith(".csv");
        export.ContentType.Should().Be("text/csv");
        export.ChecksumSha256.Should().NotBeNullOrEmpty();
        export.ContentBase64.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ClientKycRefresh_SubmitsAndCanBeReviewedByStaff()
    {
        await AuthenticateClientAsync();

        var stepUpStart = await Client.PostAsJsonAsync("/api/client-auth/step-up/initiate", new ClientStartStepUpRequest
        {
            Purpose = "KYC_REFRESH"
        });

        stepUpStart.StatusCode.Should().Be(HttpStatusCode.OK);
        var challenge = await stepUpStart.Content.ReadFromJsonAsync<ClientVerificationChallengeResponse>();
        challenge!.DebugCode.Should().NotBeNullOrEmpty();

        var stepUpVerify = await Client.PostAsJsonAsync("/api/client-auth/step-up/verify", new ClientVerifyStepUpRequest
        {
            ChallengeToken = challenge.ChallengeToken,
            Code = challenge.DebugCode!
        });

        stepUpVerify.StatusCode.Should().Be(HttpStatusCode.OK);
        var verified = await stepUpVerify.Content.ReadFromJsonAsync<ClientVerifiedStepUpResponse>();
        verified!.StepUpToken.Should().NotBeNullOrEmpty();

        var submitResponse = await Client.PostAsJsonAsync("/api/client-channel/kyc/refresh", new SubmitClientKycRefreshRequest
        {
            Reason = "Profile documents need review",
            Summary = "I have updated my identity details and want my KYC record reviewed.",
            StepUpToken = verified.StepUpToken
        });

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<ClientKycCaseDto>();
        submitted.Should().NotBeNull();
        submitted!.Status.Should().Be("SUBMITTED");

        var overviewResponse = await Client.GetAsync("/api/client-channel/kyc");
        overviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var overview = await overviewResponse.Content.ReadFromJsonAsync<ClientKycOverviewDto>();
        overview.Should().NotBeNull();
        overview!.Cases.Should().Contain(c => c.Id == submitted.Id);

        Client.DefaultRequestHeaders.Authorization = null;
        var staffToken = await AuthenticateAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var reviewResponse = await Client.PostAsJsonAsync($"/api/client-kyc-ops/{submitted.Id}/review", new ReviewClientKycCaseRequest
        {
            Decision = "UNDER_REVIEW",
            Note = "Customer KYC evidence has been routed to the review team."
        });

        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewed = await reviewResponse.Content.ReadFromJsonAsync<ClientKycCaseDto>();
        reviewed.Should().NotBeNull();
        reviewed!.Status.Should().Be("UNDER_REVIEW");
    }

    [Fact]
    public async Task StaffComplaintOps_CanTriageAndCloseClientComplaint()
    {
        await AuthenticateClientAsync();

        var createResponse = await Client.PostAsJsonAsync("/api/client-channel/complaints", new CreateClientComplaintRequest
        {
            Category = "Statement issue",
            Summary = "Statement balance mismatch",
            Details = "My latest statement looks incorrect and I need a review."
        });
        var complaint = await createResponse.Content.ReadFromJsonAsync<ClientComplaintDetailDto>();
        complaint.Should().NotBeNull();

        Client.DefaultRequestHeaders.Authorization = null;
        var staffToken = await AuthenticateAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var triageResponse = await Client.PostAsJsonAsync($"/api/client-complaint-ops/{complaint!.Id}/triage", new TriageClientComplaintRequest
        {
            OwnerTeam = "Compliance Review",
            Status = "UNDER_REVIEW",
            Note = "Complaint routed for specialist review."
        });

        triageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var closeResponse = await Client.PostAsJsonAsync($"/api/client-complaint-ops/{complaint.Id}/close", new CloseClientComplaintRequest
        {
            ResolutionCode = "RESOLVED",
            ResolutionNote = "Statement regenerated and customer notified."
        });

        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedComplaint = await closeResponse.Content.ReadFromJsonAsync<ClientComplaintDetailDto>();
        closedComplaint!.Status.Should().Be("CLOSED");
        closedComplaint.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StaffComplaintOps_CanSummarizeAndEscalateComplaintQueue()
    {
        await AuthenticateClientAsync();

        var createResponse = await Client.PostAsJsonAsync("/api/client-channel/complaints", new CreateClientComplaintRequest
        {
            Category = "Complaint handling",
            Summary = "Slow complaint response",
            Details = "I need a quicker update on my complaint and would like it escalated."
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var complaint = await createResponse.Content.ReadFromJsonAsync<ClientComplaintDetailDto>();
        complaint.Should().NotBeNull();

        Client.DefaultRequestHeaders.Authorization = null;
        var staffToken = await AuthenticateAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var summaryResponse = await Client.GetAsync("/api/client-complaint-ops/queue/summary");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<ComplaintQueueSummaryDto>();
        summary.Should().NotBeNull();
        summary!.TotalOpen.Should().BeGreaterThan(0);

        var escalateResponse = await Client.PostAsJsonAsync($"/api/client-complaint-ops/{complaint!.Id}/escalate", new EscalateClientComplaintRequest
        {
            EscalationTeam = "Complaint Escalations",
            Reason = "Customer requested formal escalation after SLA concern.",
            ResetSlaWindow = true
        });

        escalateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var escalated = await escalateResponse.Content.ReadFromJsonAsync<ClientComplaintDetailDto>();
        escalated.Should().NotBeNull();
        escalated!.Status.Should().Be("ESCALATED");
        escalated.OwnerTeam.Should().Be("Complaint Escalations");
        escalated.Events.Should().Contain(e => e.EventType == "ESCALATED");
    }
}
