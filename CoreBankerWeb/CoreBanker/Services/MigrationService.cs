using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CoreBanker.Services
{
    public class MigrationService : ApiClientBase
    {
        public MigrationService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<List<MigrationDatasetDto>> GetDatasetsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<MigrationDatasetDto>>("/api/migration/datasets", cancellationToken);
            return result ?? new List<MigrationDatasetDto>();
        }

        public async Task<MigrationResultDto?> ImportAsync(string datasetId, Stream fileStream, string fileName, CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", fileName);

            using var response = await _httpClient.PostAsync($"/api/migration/import/{Uri.EscapeDataString(datasetId)}", content, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<MigrationResultDto>(cancellationToken: cancellationToken);
        }
    }

    public class MigrationDatasetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class MigrationPreviewDto
    {
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
        public int RowCount { get; set; }
    }

    public class MigrationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}
