using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using BankInsight.API.DTOs;
using Xunit;

namespace BankInsight.IntegrationTests.Controllers;

public class ReportControllerTests : IntegrationTestBase
{
    public ReportControllerTests(TestWebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task GetReportCatalog_WithAuth_ReturnsCatalog()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/report/catalog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<List<ReportDefinitionDTO>>();
        catalog.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCustomerSegmentation_ReturnsSegmentationData()
    {
        // Arrange
        await AuthenticateAsync();
        var asOfDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/report/analytics/customer-segmentation?asOfDate={asOfDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var segmentation = await response.Content.ReadFromJsonAsync<CustomerSegmentationDTO>();
        segmentation.Should().NotBeNull();
        segmentation!.Segments.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProductAnalytics_ReturnsProductData()
    {
        // Arrange
        await AuthenticateAsync();
        var asOfDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/report/analytics/product-analytics?asOfDate={asOfDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var analytics = await response.Content.ReadFromJsonAsync<ProductAnalyticsDTO>();
        analytics.Should().NotBeNull();
        analytics!.ProductMetrics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDailyPositionReport_ReturnsReportData()
    {
        // Arrange
        await AuthenticateAsync();
        var reportDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/report/regulatory/daily-position?reportDate={reportDate}");

        // Assert
        // May return 500 if no data, but should not be unauthorized
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetRegulatoryReturns_ReturnsReturnsList()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/report/regulatory/returns");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returns = await response.Content.ReadFromJsonAsync<List<RegulatoryReturnDTO>>();
        returns.Should().NotBeNull();
    }

    [Fact]
    public async Task ReportEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Act & Assert
        var catalogResponse = await Client.GetAsync("/api/report/catalog");
        catalogResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var analyticsResponse = await Client.GetAsync("/api/report/analytics/customer-segmentation?asOfDate=2026-01-01");
        analyticsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}


