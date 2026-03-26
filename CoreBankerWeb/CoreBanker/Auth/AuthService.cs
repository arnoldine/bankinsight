using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoreBanker.State;
using Microsoft.JSInterop;

namespace CoreBanker.Auth;

public class AuthService
{
    private const string SessionStorageKey = "corebanker.auth.session";

    private readonly AppState _appState;
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public AuthService(AppState appState, HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _appState = appState;
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (!_appState.IsInitializing)
        {
            return;
        }

        try
        {
            var persistedSession = await ReadPersistedSessionAsync();
            if (persistedSession == null || string.IsNullOrWhiteSpace(persistedSession.Token))
            {
                _appState.SetInitializing(false);
                return;
            }

            SetAuthorizationHeader(persistedSession.Token);

            var restoredUser = await FetchCurrentUserAsync();
            if (restoredUser != null)
            {
                ApplyAuthenticatedState(restoredUser, persistedSession.Token, persistedSession.RefreshToken);
                return;
            }

            if (!string.IsNullOrWhiteSpace(persistedSession.RefreshToken))
            {
                var refreshed = await RefreshSessionAsync(persistedSession.RefreshToken);
                if (refreshed?.User != null && !string.IsNullOrWhiteSpace(refreshed.Token))
                {
                    ApplyAuthenticatedState(refreshed.User, refreshed.Token!, refreshed.RefreshToken ?? persistedSession.RefreshToken);
                    await PersistSessionAsync(refreshed.Token!, refreshed.RefreshToken ?? persistedSession.RefreshToken);
                    return;
                }
            }
        }
        catch
        {
            // Ignore restore failures and fall back to an unauthenticated state.
        }

        await ClearSessionAsync();
    }

    public async Task<AuthChallengeResult> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequestDto
            {
                Email = email.Trim(),
                Password = password
            });

        var payload = await ReadResponseAsync<AuthResponseDto>(response);
        await EnsureSuccessAsync(response, payload);

        if (payload?.MfaRequired == true)
        {
            return new AuthChallengeResult
            {
                RequiresMfa = true,
                MfaToken = payload.MfaToken,
                DeliveryChannel = payload.DeliveryChannel,
                DeliveryHint = payload.DeliveryHint,
                DeliveryMessage = payload.DeliveryMessage,
                DebugCode = payload.DebugCode
            };
        }

        if (payload?.User == null || string.IsNullOrWhiteSpace(payload.Token))
        {
            throw new InvalidOperationException("The authentication response did not include a valid session.");
        }

        ApplyAuthenticatedState(payload.User, payload.Token!, payload.RefreshToken ?? string.Empty);
        await PersistSessionAsync(payload.Token!, payload.RefreshToken ?? string.Empty);

        return new AuthChallengeResult { RequiresMfa = false };
    }

    public async Task<bool> VerifyMfaAsync(string mfaToken, string code)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/mfa/verify",
            new VerifyMfaRequestDto
            {
                MfaToken = mfaToken,
                Code = code.Trim()
            });

        var payload = await ReadResponseAsync<AuthResponseDto>(response);
        await EnsureSuccessAsync(response, payload);

        if (payload?.User == null || string.IsNullOrWhiteSpace(payload.Token))
        {
            return false;
        }

        ApplyAuthenticatedState(payload.User, payload.Token!, payload.RefreshToken ?? string.Empty);
        await PersistSessionAsync(payload.Token!, payload.RefreshToken ?? string.Empty);
        return true;
    }

    public async Task<AuthChallengeResult> ResendMfaAsync(string mfaToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/mfa/resend",
            new ResendMfaRequestDto
            {
                MfaToken = mfaToken
            });

        var payload = await ReadResponseAsync<AuthResponseDto>(response);
        await EnsureSuccessAsync(response, payload);

        return new AuthChallengeResult
        {
            RequiresMfa = payload?.MfaRequired ?? true,
            MfaToken = payload?.MfaToken ?? mfaToken,
            DeliveryChannel = payload?.DeliveryChannel,
            DeliveryHint = payload?.DeliveryHint,
            DeliveryMessage = payload?.DeliveryMessage,
            DebugCode = payload?.DebugCode
        };
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (_appState.IsAuthenticated)
            {
                var response = await _httpClient.PostAsync("api/auth/logout", content: null);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }
        catch
        {
            // Logout cleanup should continue even if the backend call fails.
        }

        await ClearSessionAsync();
    }

    private async Task<AuthResponseDto?> RefreshSessionAsync(string refreshToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/refresh",
            new RefreshTokenRequestDto
            {
                RefreshToken = refreshToken
            });

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadResponseAsync<AuthResponseDto>(response);
    }

    private async Task<AuthUserDto?> FetchCurrentUserAsync()
    {
        var response = await _httpClient.GetAsync("api/auth/me");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthUserDto>(_jsonOptions);
    }

    private void ApplyAuthenticatedState(AuthUserDto user, string token, string refreshToken)
    {
        SetAuthorizationHeader(token);
        _appState.SetSession(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.Permissions ?? [],
            token,
            refreshToken);
    }

    private async Task<PersistedSessionDto?> ReadPersistedSessionAsync()
    {
        var raw = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SessionStorageKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return JsonSerializer.Deserialize<PersistedSessionDto>(raw, _jsonOptions);
    }

    private async Task PersistSessionAsync(string token, string refreshToken)
    {
        var payload = JsonSerializer.Serialize(
            new PersistedSessionDto
            {
                Token = token,
                RefreshToken = refreshToken
            },
            _jsonOptions);

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SessionStorageKey, payload);
    }

    private async Task ClearSessionAsync()
    {
        ClearAuthorizationHeader();
        _appState.ClearSession();
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SessionStorageKey);
    }

    private void SetAuthorizationHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ClearAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private static async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.Content == null)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, AuthResponseDto? payload)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = payload?.DeliveryMessage;
        if (string.IsNullOrWhiteSpace(message) && response.Content != null)
        {
            try
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (document.RootElement.TryGetProperty("message", out var messageElement))
                {
                    message = messageElement.GetString();
                }
            }
            catch
            {
                // Fall back to status-based messaging below.
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            message = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "The request was rejected. Check the submitted fields and try again.",
                HttpStatusCode.Unauthorized => "The provided credentials or verification code were not accepted.",
                _ => "The authentication request failed."
            };
        }

        throw new InvalidOperationException(message);
    }
}
