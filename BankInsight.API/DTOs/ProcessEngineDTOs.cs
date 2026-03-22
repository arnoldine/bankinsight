using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CreateProcessDefinitionRequest
{
    [Required] [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required] [MaxLength(50)] public string Module { get; set; } = string.Empty;
    [Required] [MaxLength(50)] public string EntityType { get; set; } = string.Empty;
    [Required] [MaxLength(20)] public string TriggerType { get; set; } = "Manual";
    [MaxLength(100)] public string? TriggerEventType { get; set; }
}

public class ProcessDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string? TriggerEventType { get; set; }
    public bool IsSystemProcess { get; set; }
    public bool IsActive { get; set; }
}

public class ProcessDefinitionVersionDto
{
    public Guid Id { get; set; }
    public Guid ProcessDefinitionId { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateProcessStepRequest
{
    [Required] [MaxLength(50)] public string StepCode { get; set; } = string.Empty;
    [Required] [MaxLength(100)] public string StepName { get; set; } = string.Empty;
    [Required] [MaxLength(50)] public string StepType { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    public bool IsStartStep { get; set; }
    public bool IsEndStep { get; set; }
    public string? AssignmentType { get; set; }
    public string? AssignedRoleCode { get; set; }
    public string? AssignedPermissionCode { get; set; }
    public string? AssignedUserFieldPath { get; set; }
    public int? SlaHours { get; set; }
    public bool RequireMakerCheckerSeparation { get; set; }
}

public class ProcessStepDto : CreateProcessStepRequest
{
    public Guid Id { get; set; }
    public Guid ProcessDefinitionVersionId { get; set; }
}

public class CreateProcessTransitionRequest
{
    public Guid FromStepId { get; set; }
    public Guid ToStepId { get; set; }
    [Required] [MaxLength(100)] public string TransitionName { get; set; } = string.Empty;
    [MaxLength(100)] public string? ConditionRuleCode { get; set; }
    [MaxLength(50)] public string? RequiredOutcome { get; set; }
    public bool IsDefault { get; set; }
}

public class ProcessTransitionDto : CreateProcessTransitionRequest
{
    public Guid Id { get; set; }
    public Guid ProcessDefinitionVersionId { get; set; }
}

public class ProcessValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class StartProcessRequest
{
    [Required] [MaxLength(50)] public string EntityType { get; set; } = string.Empty;
    [Required] [MaxLength(50)] public string EntityId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? PayloadJson { get; set; }
}

public class CompleteTaskRequest
{
    [MaxLength(50)] public string? Outcome { get; set; }
    [MaxLength(500)] public string? Remarks { get; set; }
    public string? PayloadJson { get; set; }
}
