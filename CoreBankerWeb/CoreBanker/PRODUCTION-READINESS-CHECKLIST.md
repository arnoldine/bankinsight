# CoreBanker Production Readiness Checklist

This checklist tracks what still needs to be validated or hardened before CoreBanker can be signed off as fully production ready.

## Overall Status

- Product coverage: strong
- React parity: close
- Main remaining work: live validation, workflow hardening, and production signoff

## Signoff Gates

- `Auth and session` must survive login, MFA, refresh, expiry, logout, and route protection scenarios.
- `Permissions` must be confirmed against real backend users for navigation visibility and direct URL access.
- `Operational workflows` must be smoke-tested with seeded or production-like data.
- `Error handling` must degrade gracefully for `400`, `401`, `403`, `404`, and partial-processing scenarios.
- `Deployment` must be verified on the target host with the real API base URL, static assets, and browser cache behavior.

## Core Shell

Files:
- [App.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\App.razor)
- [MainLayout.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Layouts\MainLayout.razor)
- [AppNavMenu.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Components\Shared\AppNavMenu.razor)
- [AppRouteRegistry.cs](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Navigation\AppRouteRegistry.cs)

Checklist:
- Verify drawer, app bar, and route transitions on desktop and smaller laptop widths.
- Verify every menu item shown to a role can be opened successfully.
- Verify unauthorized pages redirect cleanly to access-denied or login states.
- Verify session-expired banner appears and recovers the user correctly.

## Auth and Session

Files:
- [AuthService.cs](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Auth\AuthService.cs)
- [Login.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Login.razor)
- [AppState.cs](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\State\AppState.cs)

Checklist:
- Verify valid login and MFA for admin and non-admin users.
- Verify invalid password, invalid MFA, and expired session messaging.
- Verify refresh and restored session behavior after browser reload.
- Verify logout fully clears local state and protected routes.

## Customer and Account Operations

Files:
- [Clients.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Clients.razor)
- [ClientsOnboard.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\ClientsOnboard.razor)
- [Accounts.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Accounts.razor)

Checklist:
- Create a new client and verify it appears in portfolio views.
- Open a new account for an existing client and verify balances and ownership display correctly.
- Verify duplicate or invalid account-opening input returns useful UI feedback.

## Teller and Payments

Files:
- [Teller.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Teller.razor)
- [Transactions.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Transactions.razor)
- [TransactionService.cs](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Services\TransactionService.cs)

Checklist:
- Post cash deposit and withdrawal successfully from teller.
- Lodge same-bank cheque and verify queue behavior.
- Lodge other-bank cheque and verify hold days and clearing date.
- Pay cheque withdrawal only from issued cheque leaves.
- Create bulk payment batch and confirm partial and complete outcomes display correctly.
- Record cheque-book stock, issue a book, cancel an unused leaf, and verify teller reflects leaf availability.

## Loans

Files:
- [LoanManagement.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\LoanManagement.razor)

Checklist:
- Originate a loan with real client and product data.
- Run credit-check flow and confirm optional or required behavior matches backend config.
- Approve and disburse a loan.
- Post repayment and confirm schedule updates.
- Verify overdue or exception cases surface clearly.

## Approvals

Files:
- [Approvals.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Approvals.razor)

Checklist:
- Load approval queue for an authorized user.
- Approve and reject real items with clear feedback.
- Verify unauthorized users do not see or cannot open the workspace.

## Group Lending

Files:
- [GroupLending.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\GroupLending.razor)

Checklist:
- Create a group and center.
- Add members and create an application.
- Record attendance and meeting collection operations.
- Verify statement, schedule, and reschedule flows.

## Treasury, Vault, and Risk

Files:
- [Treasury.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Treasury.razor)
- [Vault.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Vault.razor)
- [OperationsRisk.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\OperationsRisk.razor)

Checklist:
- Create and reconcile treasury positions.
- Create, approve, and settle FX trades.
- Create and manage investments through approval and closeout actions.
- Open till, allocate cash, return cash, and close till.
- Verify operational risk calculations and exception handling.

## Accounting and Financial Control

Files:
- [Accounting.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Accounting.razor)
- [Statements.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Statements.razor)
- [EndOfDay.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\EndOfDay.razor)

Checklist:
- Post balanced journals and verify ledger visibility.
- Run trial balance, income statement, and cash flow.
- Execute EOD steps and confirm logs/status behavior.
- Verify scheduled jobs affecting loans, cheque clearing, and other operations are visible or auditable.

## BankingOS, Migration, Reporting, and Security

Files:
- [BankingOSControl.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\BankingOSControl.razor)
- [Extensibility.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Extensibility.razor)
- [MigrationWorkbench.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\MigrationWorkbench.razor)
- [Reporting.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Reporting.razor)
- [SecurityOps.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\SecurityOps.razor)

Checklist:
- Verify BankingOS catalogs, bundles, rules, and task actions load for authorized users.
- Verify migration import path with both valid and invalid files.
- Run reports and confirm history, refresh, and export behavior.
- Verify security alerts, sessions, and investigations load without payload-shape issues.

## Admin and Audit

Files:
- [Settings.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Settings.razor)
- [Audit.razor](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\Pages\Audit.razor)

Checklist:
- Create and edit users, roles, branches, and privilege leases.
- Verify configuration toggles persist correctly.
- Verify audit events load, filter, and display detailed values safely.

## UI and UX Hardening

Files:
- [app.css](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\wwwroot\css\app.css)

Checklist:
- Verify page layout consistency across all high-traffic modules.
- Verify empty states, loading states, and error states feel intentional.
- Verify tables remain readable with large datasets.
- Verify form spacing, labels, and action buttons remain consistent.

## Deployment and Environment

Files:
- [Dockerfile](C:\Backup old\dev\bankinsight\CoreBankerWeb\Dockerfile)
- [nginx.conf](C:\Backup old\dev\bankinsight\CoreBankerWeb\nginx.conf)
- [DEPLOYMENT.md](C:\Backup old\dev\bankinsight\CoreBankerWeb\DEPLOYMENT.md)

Checklist:
- Verify containerized frontend uses the correct API base URL in the target environment.
- Verify asset caching does not leave the browser on stale bundles after deployment.
- Verify direct deep links resolve correctly through nginx SPA fallback.

## Current Recommendation

The next best step is a formal smoke-test pass in this order:

1. Login and session handling
2. Clients and accounts
3. Teller, payments, and cheque-book inventory
4. Loans and approvals
5. Treasury, vault, and accounting
6. BankingOS, settings, and audit

Once that pass is clean, CoreBanker can move from `near-production` to `production signoff candidate`.
