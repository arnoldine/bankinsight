using HybridTransfer.Domain.Common;

namespace HybridTransfer.Domain.Customers;

public sealed class Customer
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CustomerNumber { get; private set; }
    public CustomerType CustomerType { get; private set; }
    public KycTier KycTier { get; private set; }
    public KycStatus KycStatus { get; private set; }
    public RiskRating RiskRating { get; private set; }
    public CustomerStatus Status { get; private set; }
    public string Jurisdiction { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public string FullName { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public Customer(string customerNumber, string fullName, string email, string phoneNumber, string jurisdiction, CustomerType customerType)
    {
        CustomerNumber = customerNumber;
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Jurisdiction = jurisdiction;
        CustomerType = customerType;
        KycTier = KycTier.Tier0;
        KycStatus = KycStatus.Draft;
        RiskRating = RiskRating.Medium;
        Status = CustomerStatus.Pending;
    }

    public void ApproveKyc(KycTier tier)
    {
        KycTier = tier;
        KycStatus = KycStatus.Approved;
        Status = CustomerStatus.Active;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Restrict(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Restriction reason is required.");
        Status = CustomerStatus.Restricted;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
