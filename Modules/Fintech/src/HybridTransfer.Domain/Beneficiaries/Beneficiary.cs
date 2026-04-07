using HybridTransfer.Domain.Common;

namespace HybridTransfer.Domain.Beneficiaries;

public sealed class Beneficiary
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CustomerId { get; init; }
    public BeneficiaryType Type { get; init; }
    public string Alias { get; init; }
    public VerificationStatus VerificationStatus { get; private set; }
    public string? AccountName { get; init; }
    public string? BankCode { get; init; }
    public string? AccountNumber { get; init; }
    public string? MomoNetwork { get; init; }
    public string? MomoNumber { get; init; }
    public string? CryptoAddress { get; init; }
    public IReadOnlyCollection<string> RiskFlags => _riskFlags;

    private readonly List<string> _riskFlags = new();

    public Beneficiary(Guid customerId, BeneficiaryType type, string alias)
    {
        CustomerId = customerId;
        Type = type;
        Alias = alias;
        VerificationStatus = VerificationStatus.Pending;
    }

    public void MarkVerified() => VerificationStatus = VerificationStatus.Verified;
    public void AddRiskFlag(string flag) => _riskFlags.Add(flag);
}
