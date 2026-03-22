using System;
using System.Collections.Generic;

namespace BankInsight.API.Entities;

public class ProcessInstance
{
    public Guid Id { get; set; }
    public Guid ProcessDefinitionVersionId { get; set; }
    public ProcessDefinitionVersion ProcessDefinitionVersion { get; set; } = default!;

    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public Guid CurrentStepId { get; set; }
    public string Status { get; set; } = default!; // Running, Waiting, Completed, Cancelled, Rejected, Failed
    public string? CorrelationId { get; set; }

    public string StartedByUserId { get; set; } = default!;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<ProcessTask> Tasks { get; set; } = new List<ProcessTask>();
    public ICollection<ProcessInstanceHistory> History { get; set; } = new List<ProcessInstanceHistory>();
}

public class ProcessTask
{
    public Guid Id { get; set; }
    public Guid ProcessInstanceId { get; set; }
    public ProcessInstance ProcessInstance { get; set; } = default!;

    public Guid ProcessStepDefinitionId { get; set; }
    public ProcessStepDefinition ProcessStepDefinition { get; set; } = default!;

    public string? AssignedUserId { get; set; }
    public string? AssignedRoleCode { get; set; }
    public string? AssignedPermissionCode { get; set; }

    public string Status { get; set; } = default!; // Pending, Claimable, Claimed, Completed, Rejected, Cancelled, Overdue
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public string? ClaimedByUserId { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public string? Outcome { get; set; }
    public string? Remarks { get; set; }
    public string? CompletedByUserId { get; set; }
}

public class ProcessInstanceHistory
{
    public Guid Id { get; set; }
    public Guid ProcessInstanceId { get; set; }
    public ProcessInstance ProcessInstance { get; set; } = default!;

    public string ActionType { get; set; } = default!; // Started, TaskCreated, TaskClaimed, TaskCompleted, Transitioned, Completed, Rejected, Escalated
    public string? FromStepCode { get; set; }
    public string? ToStepCode { get; set; }
    public string? Outcome { get; set; }
    public string? Remarks { get; set; }
    public string? ActionByUserId { get; set; }
    public DateTime ActionAtUtc { get; set; }
    public string? PayloadJson { get; set; }
}

public class ProcessEventSubscription
{
    public Guid Id { get; set; }
    public Guid ProcessDefinitionId { get; set; }
    public ProcessDefinition ProcessDefinition { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
