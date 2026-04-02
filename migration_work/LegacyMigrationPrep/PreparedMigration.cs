namespace LegacyMigrationPrep;

internal sealed class PreparedMigration
{
    public required Dictionary<string, Dictionary<string, string>> Products { get; init; }
    public required List<Dictionary<string, string>> Customers { get; init; }
    public required List<Dictionary<string, string>> Accounts { get; init; }
    public required List<Dictionary<string, string>> Loans { get; init; }
    public required List<Dictionary<string, string>> GlTemplate { get; init; }
    public required List<Dictionary<string, string>> CustomerEnrichment { get; init; }
    public required List<Dictionary<string, string>> LoanEnrichment { get; init; }
    public required List<Dictionary<string, string>> ChequeInventory { get; init; }
}
