using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreBanker.State;
using MudBlazor;

namespace CoreBanker.Services
{
    public abstract class ApiClientBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        protected readonly HttpClient _httpClient;
        private readonly AppState _appState;

        public ApiClientBase(HttpClient httpClient, AppState appState)
        {
            _httpClient = httpClient;
            _appState = appState;
        }

        protected Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
        {
            return GetAsyncCore<T>(requestUri, cancellationToken);
        }

        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(requestUri, request, JsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            if (response.Content.Headers.ContentLength == 0)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        }

        protected async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PutAsJsonAsync(requestUri, request, JsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            if (response.Content.Headers.ContentLength == 0)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        }

        protected async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.DeleteAsync(requestUri, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        protected async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var message = await ExtractErrorMessageAsync(response, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (_appState.IsAuthenticated)
                {
                    _appState.ClearSession();
                }

                _appState.SetWorkspaceMessage("Your session has expired or is no longer authorized. Sign in again to continue working safely.", Severity.Warning, sessionExpired: true);
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _appState.SetWorkspaceMessage("This account does not have permission for one or more requested workspaces.", Severity.Warning);
            }

            throw new ApiClientException(response.StatusCode, message);
        }

        private async Task<T?> GetAsyncCore<T>(string requestUri, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            if (response.Content.Headers.ContentLength == 0)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }

        private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
            {
                return response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Your session is not authorized for this workspace. Sign in again or use an account with access.",
                    HttpStatusCode.Forbidden => "Your account is signed in, but it does not have permission to access this workspace.",
                    _ => $"Request failed with status {(int)response.StatusCode}."
                };
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString() ?? body;
                }

                if (root.TryGetProperty("title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String)
                {
                    return titleElement.GetString() ?? body;
                }

                if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var property in errorsElement.EnumerateObject())
                    {
                        foreach (var error in property.Value.EnumerateArray())
                        {
                            if (error.ValueKind == JsonValueKind.String)
                            {
                                messages.Add(error.GetString() ?? string.Empty);
                            }
                        }
                    }

                    if (messages.Count > 0)
                    {
                        return string.Join(" ", messages);
                    }
                }
            }
            catch (JsonException)
            {
            }

            return body;
        }
    }

    public sealed class ApiClientException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public ApiClientException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
