using System.Net;
using System.Net.Http.Json;
using BankInsight.API.DTOs;
using FluentAssertions;
using Xunit;

namespace BankInsight.IntegrationTests.Controllers;

public class TreasuryControllerTests : IntegrationTestBase
{
    public TreasuryControllerTests(TestWebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task GetTreasuryPositions_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/TreasuryPosition");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAndListTreasuryPositions_WithAuth_Succeeds()
    {
        await AuthenticateAsync();

        var createRequest = new CreateTreasuryPositionRequest(
            DateTime.UtcNow.Date,
            "GHS",
            100000m,
            250000m);

        var createResponse = await Client.PostAsJsonAsync("/api/TreasuryPosition", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var position = await createResponse.Content.ReadFromJsonAsync<TreasuryPositionDto>();
        position.Should().NotBeNull();
        position!.Currency.Should().Be("GHS");

        var listResponse = await Client.GetAsync("/api/TreasuryPosition");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var positions = await listResponse.Content.ReadFromJsonAsync<List<TreasuryPositionDto>>();
        positions.Should().NotBeNull();
        positions!.Should().Contain(p => p.Id == position.Id);
    }

    [Fact]
    public async Task CreateFxRate_AndFetchRates_WithAuth_Succeeds()
    {
        await AuthenticateAsync();

        var createRequest = new CreateFxRateRequest(
            "GHS",
            "USD",
            12.45m,
            12.55m,
            12.50m,
            12.50m,
            DateTime.UtcNow,
            "MANUAL",
            "Integration test rate");

        var createResponse = await Client.PostAsJsonAsync("/api/FxRate", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var rate = await createResponse.Content.ReadFromJsonAsync<FxRateDto>();
        rate.Should().NotBeNull();
        rate!.MidRate.Should().Be(12.50m);

        var listResponse = await Client.GetAsync("/api/FxRate");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rates = await listResponse.Content.ReadFromJsonAsync<List<FxRateDto>>();
        rates.Should().NotBeNull();
        rates!.Should().Contain(r => r.Id == rate.Id);
    }

    [Fact]
    public async Task CreateInvestment_WithAuth_ReturnsCreated()
    {
        await AuthenticateAsync();

        var createRequest = new CreateInvestmentRequest(
            "PLACEMENT",
            "Treasury Bill",
            "Bank of Ghana",
            "GHS",
            100000m,
            25.5m,
            null,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(91),
            "10100",
            $"TB-{DateTime.UtcNow:yyyyMMddHHmmss}",
            "Integration test investment");

        var response = await Client.PostAsJsonAsync("/api/Investment", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var investment = await response.Content.ReadFromJsonAsync<InvestmentDto>();
        investment.Should().NotBeNull();
        investment!.Instrument.Should().Be("Treasury Bill");
        investment.PrincipalAmount.Should().Be(100000m);
    }
}
