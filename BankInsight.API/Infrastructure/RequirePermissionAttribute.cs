using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankInsight.API.Infrastructure;

public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission) : base(typeof(RequirePermissionFilter))
    {
        Arguments = new object[] { permission };
    }
}

public class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    private static readonly IReadOnlyDictionary<string, string[]> PermissionAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["VIEW_USERS"] = new[] { "users.view" },
            ["MANAGE_USERS"] = new[] { "users.manage", "users.create", "users.edit", "users.disable", "users.resetpassword" },
            ["VIEW_ROLES"] = new[] { "roles.view" },
            ["MANAGE_ROLES"] = new[] { "roles.manage" },
            ["VIEW_CONFIG"] = new[] { "roles.view", "permissions.view" },
            ["MANAGE_CONFIG"] = new[] { "roles.manage", "permissions.manage" },
            ["VIEW_PRODUCTS"] = new[] { "roles.view" },
            ["MANAGE_PRODUCTS"] = new[] { "roles.manage" },
            ["VIEW_ACCOUNTS"] = new[] { "accounts.view" },
            ["CREATE_ACCOUNTS"] = new[] { "accounts.open" },
            ["POST_TRANSACTIONS"] = new[] { "transactions.post" },
            ["VIEW_GL"] = new[] { "gl.view" },
            ["MANAGE_GL"] = new[] { "gl.post", "gl.approve" },
            ["POST_JOURNAL"] = new[] { "gl.post" },
            ["VIEW_GROUPS"] = new[] { "accounts.view" },
            ["MANAGE_GROUPS"] = new[] { "accounts.open" },
            ["CREATE_GROUPS"] = new[] { "accounts.open" },
            ["VIEW_APPROVALS"] = new[] { "loans.approve", "workflow.approve" },
            ["CREATE_APPROVALS"] = new[] { "loans.create", "workflow.approve" },
            ["MANAGE_APPROVALS"] = new[] { "loans.approve", "workflow.approve" },
            ["VIEW_WORKFLOWS"] = new[] { "processes.view", "workflow.view" },
            ["MANAGE_WORKFLOWS"] = new[] { "processes.manage", "workflow.approve" },
        };

    private readonly string _permission;
    private readonly IPrivilegeLeaseService _privilegeLeaseService;

    public RequirePermissionFilter(string permission, IPrivilegeLeaseService privilegeLeaseService)
    {
        _permission = permission;
        _privilegeLeaseService = privilegeLeaseService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Authentication token missing or invalid" });
            return;
        }

        var acceptedPermissions = GetAcceptedPermissions(_permission);
        bool hasSysAdmin = user.Claims.Any(c => c.Type == "permissions" && string.Equals(c.Value, "SYSTEM_ADMIN", StringComparison.OrdinalIgnoreCase));
        bool hasPermission = user.Claims.Any(c => c.Type == "permissions" && acceptedPermissions.Contains(c.Value, StringComparer.OrdinalIgnoreCase));
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        bool hasLeasedPermission = false;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            foreach (var permission in acceptedPermissions)
            {
                if (await _privilegeLeaseService.HasActivePermissionLeaseAsync(userId, permission))
                {
                    hasLeasedPermission = true;
                    break;
                }
            }
        }

        if (!hasSysAdmin && !hasPermission && !hasLeasedPermission)
        {
            context.Result = new ObjectResult(new { message = "Forbidden: Insufficient privileges" })
            {
                StatusCode = 403
            };
        }
    }

    private static IReadOnlyCollection<string> GetAcceptedPermissions(string requiredPermission)
    {
        var accepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            requiredPermission
        };

        if (PermissionAliases.TryGetValue(requiredPermission, out var aliases))
        {
            foreach (var alias in aliases)
            {
                accepted.Add(alias);
            }
        }

        return accepted.ToArray();
    }
}
