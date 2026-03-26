using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class MigrationService
    {
        private readonly HttpClient _httpClient;
        public MigrationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<MigrationDatasetDto>> GetDatasetsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<MigrationDatasetDto>>("/api/migration/datasets") ?? new List<MigrationDatasetDto>();
        }

        public async Task<MigrationPreviewDto?> PreviewCsvAsync(string datasetId, byte[] fileBytes)
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileBytes), "file", "import.csv");
            var response = await _httpClient.PostAsync($"/api/migration/preview/{datasetId}", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MigrationPreviewDto>();
            }
            return null;
        }

        public async Task<MigrationResultDto?> ImportAsync(string datasetId, byte[] fileBytes)
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileBytes), "file", "import.csv");
            var response = await _httpClient.PostAsync($"/api/migration/import/{datasetId}", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MigrationResultDto>();
            }
            return null;
        }
    }

    public class MigrationDatasetDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class MigrationPreviewDto
    {
        public List<string>? Headers { get; set; }
        public List<List<string>>? Rows { get; set; }
    }

    public class MigrationResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}
