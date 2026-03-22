import { Customer, Account, Loan, Transaction, ComplianceMetric, AuditLog, GLAccount, JournalEntry, DevTask, Group, Product, ApprovalRequest, Branch, StaffUser, Role, Workflow } from '../types';

export const BRANCHES: any[] = [];
export const CUSTOMERS: any[] = [];
export const GROUPS: any[] = [];
export const PRODUCTS: any[] = [];
export const ACCOUNTS: any[] = [];
export const LOANS: any[] = [];
export const GL_ACCOUNTS: any[] = [];
export const JOURNAL_ENTRIES: any[] = [];
export const COMPLIANCE_METRICS: any[] = [];
export const AUDIT_LOGS: AuditLog[] = [];
export const DEV_TASKS: any[] = [];
export const APPROVAL_REQUESTS: any[] = [];
export const ROLES: any[] = [];
export const STAFF: StaffUser[] = [];
export const WORKFLOWS: any[] = [];

export const MOCK_DATA = {
  branches: BRANCHES,
  customers: CUSTOMERS,
  groups: GROUPS,
  products: PRODUCTS,
  accounts: ACCOUNTS,
  loans: LOANS,
  glAccounts: GL_ACCOUNTS,
  journalEntries: JOURNAL_ENTRIES,
  compliance: COMPLIANCE_METRICS,
  auditLogs: AUDIT_LOGS,
  devTasks: DEV_TASKS,
  approvalRequests: APPROVAL_REQUESTS,
  roles: ROLES,
  staff: STAFF,
  workflows: WORKFLOWS
};