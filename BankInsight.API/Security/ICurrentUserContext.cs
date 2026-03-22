using System.Collections.Generic;
using BankInsight.API.Entities;

namespace BankInsight.API.Security;

public interface ICurrentUserContext
{
    string UserId { get; }
    string Email { get; }
    string BranchId { get; }
    AccessScopeType ScopeType { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
}
