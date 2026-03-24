using System.Collections.Generic;
using System.Linq;

namespace BankInsight.API.Security;

public static class AppPermissions
{
    public static class Dashboard { public const string View = "dashboard.view"; }
    public static class Customers { public const string View = "customers.view"; public const string Create = "customers.create"; public const string Edit = "customers.edit"; public const string Approve = "customers.approve"; public const string Freeze = "customers.freeze"; }
    public static class Accounts { public const string View = "accounts.view"; public const string Open = "accounts.open"; public const string Edit = "accounts.edit"; public const string Close = "accounts.close"; public const string Freeze = "accounts.freeze"; public const string Reactivate = "accounts.reactivate"; }
    public static class Loans { public const string View = "loans.view"; public const string Create = "loans.create"; public const string Edit = "loans.edit"; public const string Approve = "loans.approve"; public const string Disburse = "loans.disburse"; public const string Restructure = "loans.restructure"; public const string WriteOff = "loans.writeoff"; public const string Reschedule = "loans.reschedule"; public const string ConfigureProducts = "loans.products.configure"; }
    public static class Transactions { public const string View = "transactions.view"; public const string Post = "transactions.post"; public const string Reverse = "transactions.reverse"; public const string Approve = "transactions.approve"; }
    public static class GeneralLedger { public const string View = "gl.view"; public const string Post = "gl.post"; public const string Approve = "gl.approve"; }
    public static class Reports { public const string View = "reports.view"; public const string Financial = "reports.financial"; public const string Regulatory = "reports.regulatory"; public const string Risk = "reports.risk"; public const string Generate = "reports.generate"; public const string Approve = "reports.approve"; public const string Submit = "reports.submit"; public const string Configure = "reports.configure"; }
    public static class Workflow { public const string View = "workflow.view"; public const string Approve = "workflow.approve"; }
    public static class Processes { public const string View = "processes.view"; public const string Manage = "processes.manage"; public const string Publish = "processes.publish"; public const string Start = "processes.start"; }
    public static class Tasks { public const string View = "tasks.view"; public const string Claim = "tasks.claim"; public const string Complete = "tasks.complete"; }
    public static class Users { public const string View = "users.view"; public const string Create = "users.create"; public const string Edit = "users.edit"; public const string Disable = "users.disable"; public const string ResetPassword = "users.resetpassword"; public const string Manage = "users.manage"; }
    public static class Roles { public const string View = "roles.view"; public const string Manage = "roles.manage"; }
    public static class Permissions { public const string View = "permissions.view"; public const string Manage = "permissions.manage"; }
    public static class Audit { public const string View = "audit.view"; }
    public static class Branches { public const string View = "branches.view"; public const string Manage = "branches.manage"; }
    public static class GroupLending { public const string View = "group-lending.view"; public const string ManageGroups = "group-lending.groups.manage"; public const string ManageCenters = "group-lending.centers.manage"; public const string ManageApplications = "group-lending.applications.manage"; public const string ApproveApplications = "group-lending.applications.approve"; public const string Disburse = "group-lending.disburse"; public const string Collect = "group-lending.collections.post"; public const string ReverseCollections = "group-lending.collections.reverse"; public const string ManageMeetings = "group-lending.meetings.manage"; public const string ConfigureProducts = "group-lending.products.configure"; public const string ViewReports = "group-lending.reports.view"; public const string Reschedule = "group-lending.reschedule"; }

    public static IEnumerable<string> GetAll()
    {
        var type = typeof(AppPermissions);
        var modules = type.GetNestedTypes();
        var allPermissions = new List<string>();
        foreach (var module in modules)
        {
            var fields = module.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            allPermissions.AddRange(fields.Where(f => f.IsLiteral && !f.IsInitOnly).Select(x => x.GetValue(null)?.ToString() ?? string.Empty).Where(x => !string.IsNullOrEmpty(x)));
        }
        return allPermissions;
    }
}


