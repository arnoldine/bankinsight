using System;
using System.Collections.Generic;

namespace BankingOS.Api.Contracts;

public sealed class FormDefinitionContract
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string TenantScope { get; set; } = "global";
    public string Status { get; set; } = "Draft";
    public Guid? CurrentPublishedVersionId { get; set; }
}

public sealed class FormVersionContract
{
    public Guid Id { get; set; }
    public Guid FormDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPublished { get; set; }
    public string SchemaJson { get; set; } = "{}";
    public string ValidationJson { get; set; } = "{}";
    public string LayoutJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
}

public sealed class ThemeDefinitionContract
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TenantScope { get; set; } = "global";
    public Guid? CurrentPublishedVersionId { get; set; }
}

public sealed class ThemeVersionContract
{
    public Guid Id { get; set; }
    public Guid ThemeDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPublished { get; set; }
    public string TokenJson { get; set; } = "{}";
    public string PreviewMetadataJson { get; set; } = "{}";
}

public sealed class ProcessDefinitionContract
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string TriggerType { get; set; } = "Manual";
    public string LifecycleState { get; set; } = "Draft";
    public List<ProcessStageContract> Stages { get; set; } = new();
}

public sealed class ProcessStageContract
{
    public string StageCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string EntryCriteriaJson { get; set; } = "{}";
    public string ExitCriteriaJson { get; set; } = "{}";
    public string ScreenSchemaJson { get; set; } = "{}";
    public string ValidationPolicyJson { get; set; } = "{}";
    public int? SlaHours { get; set; }
    public List<ProcessActionContract> Actions { get; set; } = new();
}

public sealed class ProcessActionContract
{
    public string ActionCode { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public string ValidationPolicyJson { get; set; } = "{}";
    public string OutcomeMappingJson { get; set; } = "{}";
}

public sealed class QueueDefinitionContract
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string FilterJson { get; set; } = "{}";
    public string ColumnsJson { get; set; } = "[]";
}

public sealed class ScreenSchemaContract
{
    public string Layout { get; set; } = "two-column";
    public List<string> Regions { get; set; } = new();
    public string SummarySchemaJson { get; set; } = "{}";
    public string ActionBarSchemaJson { get; set; } = "{}";
}
