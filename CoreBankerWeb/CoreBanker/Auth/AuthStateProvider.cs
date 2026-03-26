using System.Collections.Generic;
using CoreBanker.State;

namespace CoreBanker.Auth
{
    public class AuthStateProvider : IAuthStateProvider
    {
        private readonly AppState _appState;
        public AuthStateProvider(AppState appState)
        {
            _appState = appState;
        }

        public bool HasPermission(string permission)
        {
            return _appState.IsAuthenticated && _appState.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsInitializing => _appState.IsInitializing;
        public bool IsAuthenticated => _appState.IsAuthenticated;
        public string UserEmail => _appState.UserEmail ?? string.Empty;
        public string UserName => _appState.UserName ?? string.Empty;
        public string UserRole => _appState.UserRole ?? string.Empty;
        public List<string> Permissions => _appState.Permissions;
    }
}
