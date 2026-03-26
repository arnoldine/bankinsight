using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class GroupLendingService
    {
        private readonly HttpClient _httpClient;
        public GroupLendingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<GroupDto>> GetGroupsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<GroupDto>>("/api/groups");
            return result ?? new List<GroupDto>();
        }

        public async Task<List<GroupMeetingDto>> GetMeetingsAsync(string groupId)
        {
            var result = await _httpClient.GetFromJsonAsync<List<GroupMeetingDto>>($"/api/groups/{groupId}/meetings");
            return result ?? new List<GroupMeetingDto>();
        }
    }

    public class GroupDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Officer { get; set; }
        public string? Status { get; set; }
        public string? FormationDate { get; set; }
    }

    public class GroupMeetingDto
    {
        public string? Id { get; set; }
        public string? Date { get; set; }
        public string? Status { get; set; }
        public List<string>? MembersPresent { get; set; }
    }
}
