using System.Threading.Tasks;
using CoreBanker.Models.Dto;

namespace CoreBanker.Services
{
    public class UserService : ApiClientBase
    {
        public UserService(HttpClient httpClient) : base(httpClient) { }

        public async Task<UserDto> GetCurrentUserAsync()
        {
            // TODO: Call backend API to get current user
            return new UserDto { Id = "1", Email = "user@bank.com", Roles = new[] { "Admin" }, Permissions = new[] { "Dashboard.View" } };
        }
    }
}
