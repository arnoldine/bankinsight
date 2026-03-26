using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoreBanker.Services
{
    public class AccountingService
    {
        private readonly HttpClient _httpClient;
        public AccountingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<JournalEntryDto>> GetJournalEntriesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<JournalEntryDto>>("/api/accounting/journal-entries") ?? new List<JournalEntryDto>();
        }

        public async Task<List<GLAccountDto>> GetGLAccountsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<GLAccountDto>>("/api/accounting/gl-accounts") ?? new List<GLAccountDto>();
        }

        public async Task<bool> PostJournalEntryAsync(JournalEntryDto entry)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/accounting/journal-entries", entry);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateGLAccountAsync(GLAccountDto account)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/accounting/gl-accounts", account);
            return response.IsSuccessStatusCode;
        }
    }

    public class JournalEntryDto
    {
        public int Id { get; set; }
        public string? Reference { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string? AccountCode { get; set; }
        public string? Date { get; set; }
        public string? Status { get; set; }
    }

    public class GLAccountDto
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public decimal Balance { get; set; }
    }
}
