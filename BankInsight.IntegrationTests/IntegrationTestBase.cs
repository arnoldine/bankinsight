using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BankInsight.API.DTOs;
using FluentAssertions;

namespace BankInsight.IntegrationTests;

public class IntegrationTestBase : IClassFixture<TestWebApplicationFactory<Program>>
{
    protected readonly HttpClient Client;
    protected readonly TestWebApplicationFactory<Program> Factory;
    protected string? AuthToken;

    public IntegrationTestBase(TestWebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<string> AuthenticateAsync(string email = "admin@bankinsight.local", string password = "password123")
    {
        var loginRequest = new
        {
            email = email,
            password = password
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        AuthToken = loginResponse!.Token;

        // Set authorization header for subsequent requests
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);

        return AuthToken;
    }

    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    protected async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
    {
        return await Client.PostAsJsonAsync(url, data);
    }

    protected async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
    {
        return await Client.PutAsJsonAsync(url, data);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    protected JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true
    };
}
