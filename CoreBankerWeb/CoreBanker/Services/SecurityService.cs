using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class SecurityService
    {
        private readonly HttpClient _httpClient;
        public SecurityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TerminalDto>> GetTerminalsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<TerminalDto>>("/api/security/terminals") ?? new List<TerminalDto>();
        }

        public async Task<List<SecurityAlertDto>> GetAlertsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<SecurityAlertDto>>("/api/security/alerts") ?? new List<SecurityAlertDto>();
        }

        public async Task<bool> UpdateTerminalAsync(TerminalDto terminal)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/security/terminals/{terminal.Id}", terminal);
            return response.IsSuccessStatusCode;
        }
    }

    public class TerminalDto
    {
        public string? Id { get; set; }
        public string? Branch { get; set; }
        public string? Staff { get; set; }
        public string? IP { get; set; }
        public string? Status { get; set; }
        public string? Risk { get; set; }
        public string? Version { get; set; }
    }

    public class SecurityAlertDto
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Message { get; set; }
        public string? Severity { get; set; }
        public string? Timestamp { get; set; }
    }
}
