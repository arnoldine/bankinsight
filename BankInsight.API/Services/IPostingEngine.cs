using System.Threading.Tasks;
using BankInsight.API.Entities;

namespace BankInsight.API.Services;

public interface IPostingEngine
{
    /// <summary>
    /// Processes a single financial event, mapping it to posting rules and generating internal journal entries.
    /// This should be called within an active transaction to guarantee atomic integrity.
    /// </summary>
    Task<PostingResult> ProcessEventAsync(FinancialEvent financialEvent);
}

public class PostingResult
{
    public bool Success { get; set; }
    public string? JournalEntryId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
