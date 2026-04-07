namespace HybridTransfer.Infrastructure.Configuration;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";
    public string Provider { get; set; } = "InMemory";
}
