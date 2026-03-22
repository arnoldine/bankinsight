using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace BankInsight.API.DTOs;

public class CreateWorkflowRequest
{
    public string Name { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public JsonArray Steps { get; set; } = new();
    public string? Status { get; set; }
}

public class UpdateWorkflowRequest
{
    public string Name { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public JsonArray Steps { get; set; } = new();
    public string? Status { get; set; }
}
