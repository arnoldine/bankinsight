namespace CoreBanker.Services;

public sealed class WorkspacePreferenceService : ApiClientBase
{
    public WorkspacePreferenceService(HttpClient httpClient, State.AppState appState)
        : base(httpClient, appState)
    {
    }

    public async Task<WorkspacePreferenceSummaryVm> GetSummaryAsync(CancellationToken cancellationToken = default)
        => await GetAsync<WorkspacePreferenceSummaryVm>("api/workspace-preferences", cancellationToken)
           ?? new WorkspacePreferenceSummaryVm();

    public async Task SaveFavoriteAsync(string workspaceKey, string route, bool isPinned = false, CancellationToken cancellationToken = default)
        => await PostAsync<object, object>($"api/workspace-preferences/favorites/{Uri.EscapeDataString(workspaceKey)}", new
        {
            route,
            isPinned
        }, cancellationToken);

    public async Task RemoveFavoriteAsync(string workspaceKey, CancellationToken cancellationToken = default)
        => await DeleteAsync($"api/workspace-preferences/favorites/{Uri.EscapeDataString(workspaceKey)}", cancellationToken);

    public async Task<WorkspaceSavedViewVm> SaveViewAsync(string workspaceKey, string viewName, string route, CancellationToken cancellationToken = default)
        => await PostAsync<object, WorkspaceSavedViewVm>("api/workspace-preferences/views", new
        {
            workspaceKey,
            viewName,
            route
        }, cancellationToken) ?? new WorkspaceSavedViewVm();
}

public sealed class WorkspacePreferenceSummaryVm
{
    public List<WorkspaceFavoriteVm> Favorites { get; set; } = new();
    public List<WorkspaceSavedViewVm> SavedViews { get; set; } = new();
}

public sealed class WorkspaceFavoriteVm
{
    public string WorkspaceKey { get; set; } = string.Empty;
    public string? Route { get; set; }
    public bool IsPinned { get; set; }
}

public sealed class WorkspaceSavedViewVm
{
    public string Id { get; set; } = string.Empty;
    public string WorkspaceKey { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;
    public string? Route { get; set; }
    public bool IsDefault { get; set; }
    public DateTime UpdatedAt { get; set; }
}
