# CoreBanker Parity Implementation Roadmap

This roadmap translates the current React BankInsight implementation into a prioritized delivery plan for the `CoreBankerWeb/CoreBanker` Blazor WebAssembly project.

## Current Readiness

`CoreBankerWeb` has:

- a usable MudBlazor shell
- broad module/page coverage by route
- permission-aware navigation scaffolding
- a starter service layer

It does not yet have:

- production-ready authentication/session handling
- a shared API/error/auth pipeline
- integrated multi-workspace orchestration
- feature parity for the major operational workbenches

The React application remains the functional source of truth.

## Source Of Truth

Use these React areas as the parity baseline:

- `src/AppIntegrated.tsx`
- `src/components/EnhancedDashboardLayout.tsx`
- `src/components/LoanManagementHub.tsx`
- `src/components/SecurityOperationsHub.tsx`
- `src/components/MigrationHub.tsx`
- `src/components/ReportingHub.tsx`
- `src/components/BankingOSControlCenter.tsx`
- `src/components/group-lending/GroupLendingHub.tsx`
- `components/Settings.tsx`
- `components/ApprovalInbox.tsx`
- `components/AuditTrail.tsx`
- `components/EodConsole.tsx`
- `components/ProductDesigner.tsx`
- `components/OperationsHub.tsx`
- `components/TellerTerminal.tsx`
- `components/StatementVerification.tsx`
- `components/AccountingEngine.tsx`

## Delivery Order

1. Platform foundation
2. Application shell and workspace orchestration
3. Core banking workflows
4. Operational control modules
5. BankingOS and extensibility
6. Hardening and rollout

## Phase 1: Platform Foundation

Goal: make the Blazor app safe to build on.

### 1. Authentication and session lifecycle

React baseline:

- `src/AppIntegrated.tsx`
- `src/services/authService.ts`
- `src/services/httpClient.ts`

Blazor targets:

- `Auth/AuthService.cs`
- `Auth/AuthStateProvider.cs`
- `Auth/IAuthStateProvider.cs`
- `State/AppState.cs`
- `Pages/Login.razor`
- `App.razor`

Implement:

- real login API call
- MFA challenge/verify/resend flow
- authenticated redirect after successful MFA
- persisted session restore on app load
- logout cleanup
- permission hydration from backend
- optional future token bridge design for Clerk-like federation

Exit criteria:

- login is backend-driven
- refresh preserves session if token is valid
- protected routes respect real auth state
- logout fully clears local state

### 2. Shared API client pipeline

React baseline:

- `src/services/httpClient.ts`
- `src/services/apiConfig.ts`

Blazor targets:

- `Services/ApiClientBase.cs`
- `Program.cs`

Implement:

- configurable API base URL
- auth header injection
- consistent exception mapping
- validation/error extraction
- retry/timeouts where appropriate
- file upload/download helpers

Exit criteria:

- all services use the same auth/error conventions
- no page performs raw ad hoc HTTP handling

### 3. Project consolidation

Blazor targets:

- `Layout/`
- `Layouts/`
- `_Imports.razor`

Implement:

- consolidate duplicate layout structures
- choose one namespace convention: `CoreBanker.Layouts`
- remove obsolete scaffold leftovers
- align README and target framework

Exit criteria:

- one layout system
- one nav system
- one naming convention

## Phase 2: Shell And Workspace Orchestration

Goal: reproduce the React control-center experience.

### 4. Integrated dashboard shell

React baseline:

- `src/components/EnhancedDashboardLayout.tsx`

Blazor targets:

- `Layouts/MainLayout.razor`
- `Components/Shared/AppNavMenu.razor`
- `Pages/Index.razor`
- `Pages/Dashboard.razor`
- new shared workspace shell components under `Components/Shared/`

Implement:

- default route resolution by permission
- screen title and active context header
- refresh affordances
- workspace shortcuts
- global error/notification area
- permission-aware lazy data loading
- unsaved-change guard pattern

Exit criteria:

- dashboard shell can host multiple operational workspaces
- navigation feels like one integrated operating console

### 5. Common state containers

React baseline:

- workspace state inside `EnhancedDashboardLayout.tsx`

Blazor targets:

- `State/AppState.cs`
- new state containers by domain under `State/`

Implement:

- customer/account/loan/audit/reporting shared state
- refresh methods per domain
- dirty-state tracking for forms/workbenches

Exit criteria:

- pages stop owning all data independently
- reload behavior is predictable across modules

## Phase 3: Core Banking Workflows

Goal: make the daily banking paths functional.

### 6. Clients and account opening

React baseline:

- `components/ClientManager.tsx`
- account flow inside `src/components/EnhancedDashboardLayout.tsx`

Blazor targets:

- `Pages/Clients.razor`
- `Pages/ClientsOnboard.razor`
- `Pages/Accounts.razor`
- `Services/ClientService.cs`
- `Services/AccountService.cs`

Implement:

- client search and onboarding
- account creation with product-driven rules
- backend validation display
- customer-to-account workflow continuity

Exit criteria:

- user can onboard a client and open a valid account end-to-end

### 7. Teller and transactions

React baseline:

- `components/TellerTerminal.tsx`
- `components/TransactionExplorer.tsx`
- `components/TellerNotesScreen.tsx`
- `components/CashOpsNotesScreen.tsx`

Blazor targets:

- `Pages/Teller.razor`
- `Pages/Transactions.razor`
- `Services/TellerService.cs`
- `Services/TransactionService.cs`

Implement:

- deposits, withdrawals, transfers
- transaction search and detail
- approval-aware posting states where required
- note capture where supported by backend

Exit criteria:

- teller flow supports real posting and transaction review

### 8. Loan workbench

React baseline:

- `src/components/LoanManagementHub.tsx`
- `src/services/loanService.ts`

Blazor targets:

- `Pages/LoanManagement.razor`
- `Services/LoanService.cs`
- new loan subcomponents under `Components/Modules/Loans/`

Implement:

- portfolio summary
- origination form
- schedule preview
- credit check
- appraisal and approval queue view
- disbursement
- repayment posting
- amortization schedule viewer

Exit criteria:

- Blazor loan screen supports the full lifecycle now present in React

### 9. Approvals and maker-checker

React baseline:

- `components/ApprovalInbox.tsx`

Blazor targets:

- `Pages/Approvals.razor`
- `Services/ApprovalService.cs`

Implement:

- pending queue
- approve/reject actions
- workflow metadata display

Exit criteria:

- approvals no longer exist only inside other pages

### 10. Accounting, statements, and EOD essentials

React baseline:

- `components/AccountingEngine.tsx`
- `components/StatementVerification.tsx`
- `components/EodConsole.tsx`

Blazor targets:

- `Pages/Accounting.razor`
- `Pages/Statements.razor`
- `Pages/EndOfDay.razor`
- `Services/AccountingService.cs`

Implement:

- GL account listing
- journal posting
- statement verification workflow
- EOD dashboard and key actions

Exit criteria:

- accounting and operational close functions are no longer placeholders

## Phase 4: Operational Control Modules

Goal: reach parity on higher-order operational tooling.

### 11. Reporting workbench

React baseline:

- `src/components/ReportingHub.tsx`

Blazor targets:

- `Pages/Reporting.razor`
- `Services/ReportingService.cs`

Upgrade from current simple preview to:

- report categories/subcategories
- presets
- favorites
- preview paging/sorting
- export formats
- execution history
- CRB data quality panel

### 12. Security operations

React baseline:

- `src/components/SecurityOperationsHub.tsx`
- `src/services/securityService.ts`

Blazor targets:

- `Pages/SecurityOps.razor`
- `Pages/Security.razor`
- `Services/SecurityService.cs`

Implement:

- overview KPIs
- terminal registry
- investigations stream
- failed login review
- active sessions
- device actions and setup

### 13. Migration workbench

React baseline:

- `src/components/MigrationHub.tsx`
- `src/services/migrationService.ts`

Blazor targets:

- `Pages/MigrationWorkbench.razor`
- `Pages/Migration.razor`
- `Services/MigrationService.cs`

Implement:

- dataset catalog
- CSV template handling
- file preview
- import execution
- result and error display

### 14. Treasury, vault, and operations risk

React baseline:

- `src/components/TreasuryManagementHub.tsx`
- `src/components/VaultManagementHub.tsx`
- `components/OperationsHub.tsx`
- `components/FxRateManagement.tsx`
- `components/FxTradingDesk.tsx`
- `components/InvestmentPortfolio.tsx`
- `components/FeePanel.tsx`
- `components/PenaltyPanel.tsx`
- `components/NPLPanel.tsx`
- `components/RiskDashboard.tsx`

Blazor targets:

- `Pages/Treasury.razor`
- `Pages/Vault.razor`
- `Pages/OperationsRisk.razor`

Implement:

- treasury positions
- FX and investment actions
- vault/cash operations
- fees, penalties, NPL, and risk dashboards

## Phase 5: BankingOS And Extensibility

Goal: close the differentiating platform features, not just transactional screens.

### 15. BankingOS control center

React baseline:

- `src/components/BankingOSControlCenter.tsx`

Blazor targets:

- `Pages/BankingOSControl.razor`
- `Services/ExtensibilityService.cs`
- new BankingOS-specific DTO/service files

Implement:

- process catalog
- forms catalog
- theme catalog
- product configuration
- launch context
- task runtime workbench
- release management for forms/processes/themes

### 16. Dynamic forms and extensibility

React baseline:

- dynamic forms and process-designer areas in React

Blazor targets:

- `Pages/Extensibility.razor`
- `Pages/Settings.razor`

Implement:

- forms registry
- menu/config management
- extensibility test harness

### 17. Group lending parity

React baseline:

- `src/components/group-lending/GroupLendingHub.tsx`

Blazor targets:

- `Pages/GroupLending.razor`
- `Services/GroupLendingService.cs`

Upgrade from current list view to:

- dashboard
- groups and centers
- member management
- cycle applications
- meeting operations
- group reports

## Phase 6: Hardening And Release

Goal: make the Blazor app deployable as a real replacement.

### 18. UX and reliability

Implement:

- loading skeletons
- empty/error states
- toasts/snackbars
- optimistic refresh only where safe
- accessibility review
- mobile/responsive validation

### 19. Testing

Implement:

- service-level tests for mapping and error handling
- component tests for auth and major workbenches
- smoke tests for login, dashboard, accounts, loans, reporting

### 20. Deployment readiness

Implement:

- environment configuration strategy
- same-origin or proxied API routing decision
- production observability/logging hooks
- build pipeline validation

## Recommended Execution Plan

Use this order for actual implementation:

1. Auth/session/API foundation
2. Main shell and state containers
3. Clients/accounts/teller
4. Loan workbench
5. Approvals/accounting/statements/EOD
6. Reporting
7. Security ops
8. Migration
9. Treasury/vault/operations risk
10. BankingOS/extensibility/group lending
11. Hardening and rollout

## First Milestone

The first milestone should be:

- real auth
- shared API client
- consolidated layout
- permission-based landing route
- integrated dashboard shell
- clients/accounts/teller baseline

Reason:

This creates the reusable platform needed for every other module and unlocks real operator flows quickly.

## Suggested Work Breakdown

Parallel workstreams can be split like this:

- Workstream A: auth, API client, app shell
- Workstream B: clients, accounts, teller, transactions
- Workstream C: loans and approvals
- Workstream D: reporting, security, migration
- Workstream E: BankingOS, group lending, admin tooling

## Definition Of Parity

Parity should mean:

- same authenticated flows
- same backend integration coverage
- same critical operator tasks
- same permission-aware navigation behavior
- same key reporting, approval, and security workbenches

Parity should not mean:

- identical visual styling
- one-to-one component structure
- literal React-to-Blazor translation

The Blazor app can diverge in implementation as long as it preserves the product behavior and operational coverage.
