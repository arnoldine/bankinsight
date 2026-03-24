export const Permissions = {
  Dashboard: {
    View: "dashboard.view"
  },
  Customers: {
    View: "customers.view",
    Create: "customers.create",
    Edit: "customers.edit",
    Approve: "customers.approve",
    Freeze: "customers.freeze"
  },
  Accounts: {
    View: "accounts.view",
    Open: "accounts.open",
    Edit: "accounts.edit",
    Close: "accounts.close",
    Freeze: "accounts.freeze",
    Reactivate: "accounts.reactivate"
  },
  Loans: {
    View: "loans.view",
    Create: "loans.create",
    Edit: "loans.edit",
    Approve: "loans.approve",
    Disburse: "loans.disburse",
    Restructure: "loans.restructure",
    WriteOff: "loans.writeoff",
    Reschedule: "loans.reschedule"
  },
  Transactions: {
    View: "transactions.view",
    Post: "transactions.post",
    Reverse: "transactions.reverse",
    Approve: "transactions.approve"
  },
  GeneralLedger: {
    View: "gl.view",
    Post: "gl.post",
    Approve: "gl.approve"
  },
  Reports: {
    View: "reports.view",
    Financial: "reports.financial",
    Regulatory: "reports.regulatory",
    Risk: "reports.risk",
    Generate: "reports.generate",
    Approve: "reports.approve",
    Submit: "reports.submit",
    Configure: "reports.configure"
  },
  Workflow: {
    View: "workflow.view",
    Approve: "workflow.approve"
  },
  Processes: {
    View: "processes.view",
    Manage: "processes.manage",
    Publish: "processes.publish",
    Start: "processes.start"
  },
  Tasks: {
    View: "tasks.view",
    Claim: "tasks.claim",
    Complete: "tasks.complete"
  },
  Users: {
    View: "users.view",
    Create: "users.create",
    Edit: "users.edit",
    Disable: "users.disable",
    ResetPassword: "users.resetpassword",
    Manage: "users.manage"
  },
  Roles: {
    View: "roles.view",
    Manage: "roles.manage"
  },
  Permissions: {
    View: "permissions.view",
    Manage: "permissions.manage"
  },
  Audit: {
    View: "audit.view"
  },
  Branches: {
    View: "branches.view",
    Manage: "branches.manage"
  },
  GroupLending: {
    View: "group-lending.view",
    ManageGroups: "group-lending.groups.manage",
    ManageCenters: "group-lending.centers.manage",
    ManageApplications: "group-lending.applications.manage",
    ApproveApplications: "group-lending.applications.approve",
    Disburse: "group-lending.disburse",
    Collect: "group-lending.collections.post",
    ReverseCollections: "group-lending.collections.reverse",
    ManageMeetings: "group-lending.meetings.manage",
    ConfigureProducts: "group-lending.products.configure",
    ViewReports: "group-lending.reports.view",
    Reschedule: "group-lending.reschedule"
  }
} as const;

// Flatten the Permissions nested object to create union types if needed
export type AppPermissionPath = typeof Permissions[keyof typeof Permissions][keyof typeof Permissions[keyof typeof Permissions]];

