using System.Text.Json;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Domain.Common;

namespace HybridTransfer.Application.Services;

public sealed class RiskAssessmentService
{
    private readonly IAlertRepository _alertRepository;

    public RiskAssessmentService(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<RiskAssessmentResult> EvaluateTransferAsync(Guid customerId, Guid transferId, decimal amount, string channel, bool isNewDevice, CancellationToken cancellationToken)
    {
        var score = 0;
        var reasons = new List<string>();

        if (amount >= 1000m)
        {
            score += 55;
            reasons.Add("High-value payout threshold breached.");
        }

        if (isNewDevice)
        {
            score += 35;
            reasons.Add("Transaction initiated from a newly observed device.");
        }

        if (string.Equals(channel, "Crypto", StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
            reasons.Add("Crypto withdrawal path requires heightened monitoring.");
        }

        var riskStatus = score >= 70 ? RiskStatus.Hold : score >= 40 ? RiskStatus.Monitor : RiskStatus.Clear;
        var complianceStatus = riskStatus == RiskStatus.Hold ? ComplianceStatus.PendingReview : ComplianceStatus.Clear;

        if (riskStatus != RiskStatus.Clear)
        {
            var payload = JsonSerializer.Serialize(new { transferId, amount, channel, isNewDevice, reasons });
            var alert = new AlertRecord(Guid.NewGuid(), customerId, "TRANSFER_RISK_REVIEW", riskStatus == RiskStatus.Hold ? "High" : "Medium", score, "Open", payload, DateTimeOffset.UtcNow);
            await _alertRepository.SaveAsync(alert, cancellationToken);
        }

        return new RiskAssessmentResult(riskStatus, complianceStatus, score, reasons);
    }

    public async Task<IReadOnlyCollection<AlertResponse>> GetOpenAlertsAsync(CancellationToken cancellationToken)
    {
        var alerts = await _alertRepository.GetOpenAlertsAsync(cancellationToken);
        return alerts
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AlertResponse(x.Id, x.CustomerId, x.AlertCode, x.Severity, x.Score, x.Status, x.PayloadJson))
            .ToArray();
    }
}

public sealed record RiskAssessmentResult(RiskStatus RiskStatus, ComplianceStatus ComplianceStatus, int Score, IReadOnlyCollection<string> Reasons);
