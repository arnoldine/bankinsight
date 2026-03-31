using MudBlazor;

namespace CoreBanker.State
{
    public class AppState
    {
        public string? UserId { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsInitializing { get; set; } = true;
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? WorkspaceMessage { get; private set; }
        public Severity WorkspaceMessageSeverity { get; private set; } = Severity.Info;
        public bool SessionExpired { get; private set; }
        public List<string> Permissions { get; set; } = new();

        public event Action? Changed;

        public void SetSession(string userId, string name, string email, string role, List<string> permissions, string accessToken, string refreshToken)
        {
            UserId = userId;
            IsAuthenticated = true;
            IsInitializing = false;
            UserEmail = email;
            UserName = name;
            UserRole = role;
            Permissions = permissions;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            WorkspaceMessage = null;
            SessionExpired = false;
            NotifyStateChanged();
        }

        public void SetInitializing(bool isInitializing)
        {
            IsInitializing = isInitializing;
            NotifyStateChanged();
        }

        public void ClearSession()
        {
            UserId = null;
            IsAuthenticated = false;
            IsInitializing = false;
            UserEmail = null;
            UserName = null;
            UserRole = null;
            AccessToken = null;
            RefreshToken = null;
            WorkspaceMessage = null;
            SessionExpired = false;
            Permissions.Clear();
            NotifyStateChanged();
        }

        public void SetWorkspaceMessage(string message, Severity severity = Severity.Info, bool sessionExpired = false)
        {
            WorkspaceMessage = message;
            WorkspaceMessageSeverity = severity;
            SessionExpired = sessionExpired;
            NotifyStateChanged();
        }

        public void ClearWorkspaceMessage()
        {
            WorkspaceMessage = null;
            SessionExpired = false;
            NotifyStateChanged();
        }

        public string GetDefaultRoute()
        {
            return Navigation.AppRouteRegistry.GetDefaultRoute(Permissions);
        }

        private void NotifyStateChanged()
        {
            Changed?.Invoke();
        }
    }
}
