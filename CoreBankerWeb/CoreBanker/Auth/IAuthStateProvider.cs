namespace CoreBanker.Auth
{
    public interface IAuthStateProvider
    {
        bool HasPermission(string permission);
        bool IsAuthenticated { get; }
        bool IsInitializing { get; }
        string UserEmail { get; }
        string UserName { get; }
        string UserRole { get; }
        List<string> Permissions { get; }
    }
}
