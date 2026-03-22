using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using BankInsight.API.Entities;
using Microsoft.AspNetCore.Http;

namespace BankInsight.API.Security;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    public string Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    public string BranchId => User?.FindFirst("branch_id")?.Value ?? string.Empty;
    
    public AccessScopeType ScopeType
    {
        get
        {
            var scopeClaim = User?.FindFirst("access_scope_type")?.Value;
            if (Enum.TryParse<AccessScopeType>(scopeClaim, out var scope))
            {
                return scope;
            }
            return AccessScopeType.BranchOnly;
        }
    }

    public IReadOnlyList<string> Permissions => User?.FindAll("permissions").Select(c => c.Value).ToList() ?? new List<string>();

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}
