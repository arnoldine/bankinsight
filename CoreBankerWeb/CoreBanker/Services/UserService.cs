using System.Threading.Tasks;
using CoreBanker.Models.Dto;

namespace CoreBanker.Services
{
    public class UserService : ApiClientBase
    {
        public UserService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<UserDto> GetCurrentUserAsync()
        {
            var currentUser = await GetAsync<CurrentUserApiModel>("/api/auth/me");
            if (currentUser is null)
            {
                return new UserDto();
            }

            return new UserDto
            {
                Id = currentUser.Id ?? string.Empty,
                Name = currentUser.Name ?? string.Empty,
                Email = currentUser.Email ?? string.Empty,
                Role = currentUser.Role ?? string.Empty,
                Roles = string.IsNullOrWhiteSpace(currentUser.Role) ? Array.Empty<string>() : [currentUser.Role],
                Permissions = currentUser.Permissions ?? Array.Empty<string>()
            };
        }

        private sealed class CurrentUserApiModel
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? Role { get; set; }
            public string[]? Permissions { get; set; }
        }
    }
}
