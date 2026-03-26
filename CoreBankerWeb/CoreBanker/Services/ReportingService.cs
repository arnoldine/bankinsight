using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class ReportingService
    {
        private readonly HttpClient _httpClient;
        public ReportingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ReportCatalogItemDto>> GetReportCatalogAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<ReportCatalogItemDto>>("/api/reports/catalog") ?? new List<ReportCatalogItemDto>();
        }

        public async Task<ReportResultDto?> RunReportAsync(string reportId, Dictionary<string, object> parameters)
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/reports/run/{reportId}", parameters);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ReportResultDto>();
            }
            return null;
        }
    }

    public class ReportCatalogItemDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
    }

    public class ReportResultDto
    {
        public string? ReportId { get; set; }
        public string? Name { get; set; }
        public List<string>? Columns { get; set; }
        public List<List<object>>? Rows { get; set; }
        public string? ExportUrl { get; set; }
    }
}
