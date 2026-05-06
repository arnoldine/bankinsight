namespace BankInsight.API.DTOs;

public class WorkspaceFavoriteDto
{
    public string WorkspaceKey { get; set; } = string.Empty;
    public string? Route { get; set; }
    public bool IsPinned { get; set; }
}

public class WorkspaceSavedViewDto
{
    public string Id { get; set; } = string.Empty;
    public string WorkspaceKey { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? FilterJson { get; set; }
    public bool IsDefault { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkspacePreferencesSummaryDto
{
    public List<WorkspaceFavoriteDto> Favorites { get; set; } = new();
    public List<WorkspaceSavedViewDto> SavedViews { get; set; } = new();
}

public class UpsertWorkspaceFavoriteRequest
{
    public string? Route { get; set; }
    public bool IsPinned { get; set; }
}

public class SaveWorkspaceViewRequest
{
    public string WorkspaceKey { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? FilterJson { get; set; }
    public bool IsDefault { get; set; }
}
