using System;
using System.Collections.Generic;

namespace BankInsight.API.Entities;

public class ProcessDefinition
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Module { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string TriggerType { get; set; } = default!; // Manual, Event, Api
    public string? TriggerEventType { get; set; }
    public bool IsSystemProcess { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByUserId { get; set; } = default!;

    public ICollection<ProcessDefinitionVersion> Versions { get; set; } = new List<ProcessDefinitionVersion>();
}

public class ProcessDefinitionVersion
{
    public Guid Id { get; set; }
    public Guid ProcessDefinitionId { get; set; }
    public ProcessDefinition ProcessDefinition { get; set; } = default!;
    public int VersionNo { get; set; }
    public string Status { get; set; } = default!; // Draft, Published, Archived
    public bool IsPublished { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByUserId { get; set; } = default!;
    public DateTime? PublishedAtUtc { get; set; }
    public string? PublishedByUserId { get; set; }

    public ICollection<ProcessStepDefinition> Steps { get; set; } = new List<ProcessStepDefinition>();
    public ICollection<ProcessTransitionDefinition> Transitions { get; set; } = new List<ProcessTransitionDefinition>();
}

public class ProcessStepDefinition
{
    public Guid Id { get; set; }
    public Guid ProcessDefinitionVersionId { get; set; }
    public ProcessDefinitionVersion ProcessDefinitionVersion { get; set; } = default!;
    public string StepCode { get; set; } = default!;
    public string StepName { get; set; } = default!;
    public string StepType { get; set; } = default!; // Start, UserTask, ApprovalTask, SystemTask, Decision, Notification, End
    public int OrderNo { get; set; }
    public bool IsStartStep { get; set; }
    public bool IsEndStep { get; set; }

    public string? AssignmentType { get; set; } // Role, Permission, User, Dynamic, None
    public string? AssignedRoleCode { get; set; }
    public string? AssignedPermissionCode { get; set; }
    public string? AssignedUserFieldPath { get; set; }

    public int? SlaHours { get; set; }
    public bool RequireMakerCheckerSeparation { get; set; }
    public string? AutoActionConfigJson { get; set; }
}

public class ProcessTransitionDefinition
{
    public Guid Id { get; set; }
    public Guid ProcessDefinitionVersionId { get; set; }
    public ProcessDefinitionVersion ProcessDefinitionVersion { get; set; } = default!;

    public Guid FromStepId { get; set; }
    public Guid ToStepId { get; set; }

    public string TransitionName { get; set; } = default!;
    public string? ConditionRuleCode { get; set; }
    public string? RequiredOutcome { get; set; } // Approve, Reject, Complete, Escalate
    public bool IsDefault { get; set; }
}
