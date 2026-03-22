using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankInsight.API.DTOs;
using FluentAssertions;
using Xunit;

namespace BankInsight.IntegrationTests.Controllers;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(TestWebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var loginRequest = new LoginRequest
        {
            Email = "admin@bankinsight.local",
            Password = "password123"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.Token.Should().NotBeNullOrEmpty();
        loginResponse.User.Should().NotBeNull();
        JsonSerializer.Serialize(loginResponse.User).Should().Contain("admin@bankinsight.local");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest
        {
            Email = "admin@bankinsight.local",
            Password = "wrongpassword"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "password123"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/report/catalog");

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/report/catalog");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}


