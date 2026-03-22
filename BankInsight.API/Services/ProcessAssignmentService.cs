using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BankInsight.API.Services;

public class ProcessAssignmentService
{
    public ProcessAssignmentService()
    {
    }

    /// <summary>
    /// Returns the UserId if the task should be directly assigned.
    /// Returns null if the task should be claimable via Role/Permission queues.
    /// </summary>
    public Task<string?> ResolveAssigneeUserAsync(
        string? assignmentType, 
        string? assignedUserFieldPath,
        string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(assignmentType) || assignmentType == "None")
        {
            return Task.FromResult<string?>(null);
        }

        if (assignmentType == "User")
        {
            // Direct user ID mapped statically
            if (!string.IsNullOrWhiteSpace(assignedUserFieldPath) && !assignedUserFieldPath.StartsWith("$."))
            {
                return Task.FromResult<string?>(assignedUserFieldPath);
            }

            // Simple JSON path extraction e.g. $.InitiatorId
            if (!string.IsNullOrWhiteSpace(assignedUserFieldPath) && assignedUserFieldPath.StartsWith("$.") && !string.IsNullOrWhiteSpace(payloadJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(payloadJson);
                    var propName = assignedUserFieldPath.Substring(2);
                    if (doc.RootElement.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
                    {
                        return Task.FromResult<string?>(prop.GetString());
                    }
                }
                catch
                {
                    // Ignore parsing errors, fall through to null
                }
            }
        }
        
        // Dynamic assignment logic could go here
        
        return Task.FromResult<string?>(null);
    }
}
