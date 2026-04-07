using HybridTransfer.Domain.Common;

namespace HybridTransfer.Domain.Compliance;

public sealed class Alert
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CustomerId { get; init; }
    public string AlertCode { get; init; }
    public AlertSeverity Severity { get; init; }
    public int Score { get; init; }
    public AlertStatus Status { get; private set; } = AlertStatus.Open;
    public string PayloadJson { get; init; }

    public Alert(Guid customerId, string alertCode, AlertSeverity severity, int score, string payloadJson)
    {
        CustomerId = customerId;
        AlertCode = alertCode;
        Severity = severity;
        Score = score;
        PayloadJson = payloadJson;
    }

    public void Escalate() => Status = AlertStatus.Escalated;
    public void Close() => Status = AlertStatus.Closed;
}
