using HybridTransfer.Domain.Common;

namespace HybridTransfer.Domain.Transfers;

public sealed class TransferOrder
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public TransferType Type { get; init; }
    public TransferChannel Channel { get; init; }
    public string FundingSource { get; init; }
    public Guid? BeneficiaryId { get; init; }
    public Guid SourceWalletId { get; init; }
    public string DestinationDetails { get; init; }
    public string Currency { get; init; }
    public string DestinationCountryCode { get; init; }
    public decimal Amount { get; init; }
    public decimal Fee { get; private set; }
    public decimal? FxRate { get; private set; }
    public TransferStatus Status { get; private set; }
    public RiskStatus RiskStatus { get; private set; }
    public ComplianceStatus ComplianceStatus { get; private set; }
    public string? PartnerReference { get; private set; }
    public string? FailureReason { get; private set; }
    public string CreatedBy { get; init; }
    public string? ApprovedBy { get; private set; }
    public string IdempotencyKey { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool IsCrossBorder => !string.Equals(DestinationCountryCode, "GH", StringComparison.OrdinalIgnoreCase);

    public TransferOrder(TransferType type, TransferChannel channel, string fundingSource, Guid sourceWalletId, string destinationDetails, string currency, string destinationCountryCode, decimal amount, string createdBy, string idempotencyKey, Guid? beneficiaryId = null)
    {
        Type = type;
        Channel = channel;
        FundingSource = fundingSource;
        SourceWalletId = sourceWalletId;
        DestinationDetails = destinationDetails;
        Currency = currency.Trim().ToUpperInvariant();
        DestinationCountryCode = string.IsNullOrWhiteSpace(destinationCountryCode) ? "GH" : destinationCountryCode.Trim().ToUpperInvariant();
        Amount = amount;
        CreatedBy = createdBy;
        IdempotencyKey = idempotencyKey;
        BeneficiaryId = beneficiaryId;
        Status = TransferStatus.Draft;
        RiskStatus = RiskStatus.Clear;
        ComplianceStatus = ComplianceStatus.Clear;
    }

    public void MarkAwaitingApproval() => Status = TransferStatus.AwaitingApproval;

    public void Authorize(string? approvedBy)
    {
        ApprovedBy = approvedBy;
        Status = TransferStatus.Authorized;
    }

    public void Submit(string partnerReference)
    {
        PartnerReference = partnerReference;
        Status = TransferStatus.Submitted;
    }

    public void MarkPendingSettlement()
    {
        Status = TransferStatus.PendingSettlement;
    }

    public void MarkSettled()
    {
        Status = TransferStatus.Posted;
        FailureReason = null;
    }

    public void MarkReversed(string? reason)
    {
        Status = TransferStatus.Reversed;
        FailureReason = reason;
    }

    public void SetOutcome(TransferStatus status, string? failureReason = null)
    {
        Status = status;
        FailureReason = failureReason;
    }

    public void ApplyRisk(RiskStatus riskStatus, ComplianceStatus complianceStatus)
    {
        RiskStatus = riskStatus;
        ComplianceStatus = complianceStatus;
    }

    public void ApplyPricing(decimal fee, decimal? fxRate)
    {
        Fee = fee;
        FxRate = fxRate;
    }
}
