using System.Text.Json;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;

namespace HybridTransfer.Application.Services;

public sealed class AuditTrailService
{
    private readonly IAuditEventRepository _auditEventRepository;

    public AuditTrailService(IAuditEventRepository auditEventRepository)
    {
        _auditEventRepository = auditEventRepository;
    }

    public Task RecordAsync(string actorId, string actorType, string action, string entityType, string entityId, object? before, object? after, CancellationToken cancellationToken)
    {
        var record = new AuditEventRecord(
            Guid.NewGuid(),
            actorId,
            actorType,
            action,
            entityType,
            entityId,
            before is null ? null : JsonSerializer.Serialize(before),
            after is null ? null : JsonSerializer.Serialize(after),
            null,
            null,
            null,
            DateTimeOffset.UtcNow);

        return _auditEventRepository.SaveAsync(record, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditEventResponse>> GetForEntityAsync(string entityType, string entityId, CancellationToken cancellationToken)
    {
        var items = await _auditEventRepository.GetByEntityAsync(entityType, entityId, cancellationToken);
        return items.Select(x => new AuditEventResponse(x.Id, x.Action, x.EntityType, x.EntityId, x.ActorId, x.CreatedAtUtc, x.BeforeJson, x.AfterJson)).ToArray();
    }
}
