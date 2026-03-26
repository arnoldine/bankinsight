using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class ExtensibilityService
    {
        private readonly HttpClient _httpClient;
        public ExtensibilityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DynamicFormSchemaDto?> GetFormSchemaAsync(string formId)
        {
            return await _httpClient.GetFromJsonAsync<DynamicFormSchemaDto>($"/api/extensibility/forms/{formId}/schema");
        }

        public async Task<bool> SubmitFormAsync(string formId, Dictionary<string, object> values)
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/extensibility/forms/{formId}/submit", values);
            return response.IsSuccessStatusCode;
        }
    }

    public class DynamicFormSchemaDto
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public List<DynamicFormFieldDto>? Fields { get; set; }
    }

    public class DynamicFormFieldDto
    {
        public string? Name { get; set; }
        public string? Label { get; set; }
        public string? Type { get; set; } // text, number, date, select, etc.
        public List<string>? Options { get; set; }
        public bool Required { get; set; }
    }
}
