# BankingOS Architecture Blueprint

## Purpose
BankingOS is the target platform evolved from the existing BankInsight workspace. It should become a configurable, multi-tenant core banking platform for a Bank of Ghana regulated institution, with backend-defined banking processes, frontend-ready metadata, low-code forms, white-label theming, and governed configuration publishing.

This blueprint is grounded in the current repository:
- .NET 8 backend in `BankInsight.API`
- React + TypeScript + Vite frontend in `src`
- existing workflow/process runtime services
- existing dynamic form renderer
- existing approval, audit, GL, transaction, branch, treasury, lending, and reporting services

## Current Reusable Foundations

### Backend foundations already present
- Entities for customers, accounts, loans, products, transactions, GL, workflows, process definitions, process instances, approvals, audit, branches, vaults, treasury, reporting, and permissions
- Services including:
  - `ProcessDefinitionService`
  - `ProcessRuntimeService`
  - `ProcessTaskService`
  - `ApprovalService`
  - `AuditLoggingService`
  - `ProductService`
  - `KycService`
  - `LoanService`
  - `AccountService`
  - `TransactionService`
  - `PostingEngine`
  - `LedgerEngine`
  - `VaultManagementService`
  - `OperationsService`
  - `RegulatoryReportService`

### Frontend foundations already present
- `src/components/DynamicForms/JsonFormRenderer.tsx`
- `src/components/FormDesignerScreen.tsx`
- `src/components/ProcessDesigner.tsx`
- `src/components/TaskInbox.tsx`
- `src/services/workflowRuntimeService.ts`
- workspace-style operational hubs for loans, treasury, reporting, vaults, and group lending

## Target Product Identity
- Product name: BankingOS
- BankInsight becomes the source code lineage, not the live product identity
- Rename solution/app shell, docs, terminology, seeded branding, and configuration labels to BankingOS

## Architecture Principles
- Backend is the source of truth for process rules, task actions, validations, approval policies, and renderable metadata.
- Frontend renders queues, forms, summaries, and action bars from metadata wherever practical.
- Configuration is versioned, publishable, auditable, and rollback-safe.
- Operational data is separated from configuration data.
- Branch, tenant, and role boundaries are enforced server-side.
- Maker-checker is first-class for sensitive business and configuration operations.

## Target Platform Domains

### 1. Core Banking Domain
Responsibilities:
- customer and party management
- KYC and document lifecycle
- account lifecycle
- savings/current/fixed deposit support
- loan origination through collection
- charges, fees, taxes, penalties
- teller and branch cash operations
- transfers and transaction orchestration

Primary current anchors:
- `Customer.cs`, `Account.cs`, `Loan.cs`, `Product.cs`, `Transaction.cs`
- `CustomerService.cs`, `AccountService.cs`, `LoanService.cs`, `TransactionService.cs`, `FeeService.cs`

### 2. Process Runtime Domain
Responsibilities:
- process definitions
- stage models
- task generation
- stage transition rules
- assignment and escalation
- process event emission
- frontend task metadata

Primary current anchors:
- `ProcessDefinition.cs`, `ProcessInstance.cs`, `Workflow.cs`, `ApprovalRequest.cs`
- `ProcessDefinitionService.cs`, `ProcessRuntimeService.cs`, `ProcessTaskService.cs`, `ProcessAssignmentService.cs`, `WorkflowService.cs`

### 3. Product Factory Domain
Responsibilities:
- product definitions
- pricing and fee rules
- eligibility rules
- repayment or maturity behavior
- GL mapping references
- configurable product bundles

Primary current anchors:
- `Product.cs`
- `ProductService.cs`
- `PostingRule.cs`

### 4. Config Studio Domain
Responsibilities:
- form design
- process design
- queue and screen schema design
- publish bundles
- versioning and rollback
- approval workflow for configuration changes

Primary current anchors:
- `FormDesignerScreen.tsx`
- `ProcessDesigner.tsx`
- `ConfigService.cs`

### 5. Theme Studio Domain
Responsibilities:
- white-label brand identity
- token-driven theme variants
- theme preview and publish
- tenant theme assignment

New platform layer to add:
- `ThemeDefinition`
- `ThemeVersion`
- frontend theme token runtime

### 6. Operations Control Domain
Responsibilities:
- teller supervision
- branch cash and vault operations
- exception handling
- end-of-day control
- work queues and operational dashboards

Primary current anchors:
- `VaultManagementService.cs`
- `CashControlService.cs`
- `InterBranchTransferService.cs`
- `OperationsService.cs`
- `TaskInbox.tsx`

### 7. Reporting and Regulatory Domain
Responsibilities:
- management reporting
- operational dashboards
- audit packs
- regulatory extract support
- reporting schedule and export controls

Primary current anchors:
- `ReportingService.cs`
- `RegulatoryReportService.cs`
- `FinancialReportService.cs`
- `EnterpriseReportingService.cs`

### 8. Security and Governance Domain
Responsibilities:
- RBAC
- branch-aware access
- tenant isolation
- audit trails
- approval delegation rules
- publish approval controls
- privilege leasing and session governance

Primary current anchors:
- `Permission.cs`, `Role.cs`, `RolePermission.cs`, `PrivilegeLease.cs`, `AuditLog.cs`
- `RoleService.cs`, `UserService.cs`, `PrivilegeLeaseService.cs`, `AuditLoggingService.cs`

## Configuration-Driven Platform Model

### Configuration classes to add or formalize
- `FormDefinition`
- `FormVersion`
- `ThemeDefinition`
- `ThemeVersion`
- `ProductDefinition`
- `ProductRule`
- `ProcessStageDefinition`
- `ProcessTaskDefinition`
- `ProcessActionDefinition`
- `ScreenSchema`
- `QueueDefinition`
- `ValidationRule`
- `ApprovalPolicy`
- `PublishBundle`
- `ConfigAuditEvent`

### Configuration storage rules
- Draft and published versions must coexist.
- Published versions must be immutable.
- Rollback must create a new published version from a historical baseline.
- Configuration changes must carry actor, timestamp, tenant, branch scope, and approval metadata.

## Shared Process Lifecycle
All business processes in BankingOS should follow a common lifecycle envelope:
- Draft
- Submitted
- Validation Pending
- Under Review
- Approved
- Rejected
- Returned for Correction
- Ready for Execution
- Executed
- Posted
- Completed
- Exception
- Reversed
- Archived

Each concrete process stage must define:
- stage code
- display name
- actor role
- entry criteria
- required form or review schema
- validation rules
- allowed actions
- decision outcomes
- event emissions
- audit payload
- SLA target
- escalation route

## Improved Process Model

### Process design pattern
Each process should expose:
- metadata envelope
- stage graph
- task templates
- action definitions
- backend handlers
- frontend rendering contract
- approval policy
- posting policy where applicable

### Frontend rendering contract
The backend should return enough metadata to render:
- queue cards
- detail header
- stage timeline
- form sections
- summary cards
- supporting documents
- validation errors
- action buttons
- decision dialogs
- audit trail preview

## Required Seeded Operational Flows
BankingOS should ship with prebuilt flows for:
- customer onboarding
- KYC review
- account opening
- cash deposit
- cash withdrawal
- loan origination
- loan disbursement
- repayment collection
- arrears management
- fixed deposit lifecycle
- teller funding and vault transfer
- end-of-day operations

## Backend-to-Frontend Contract Strategy

### Backend owns
- process states
- transition rules
- validation and approval policy
- product rules
- calculated summaries
- action permissions
- publish/version state

### Frontend owns
- rendering engine
- operator interaction patterns
- local draft experience
- previews for forms and themes
- workspace navigation and task presentation

### Avoid
- hardcoding stage buttons directly in feature screens
- duplicating validation logic in multiple frontend forms
- having per-screen process rules that bypass the runtime engine

## BankingOS Module Map

### Backend module direction
- `BankingOS.API/CoreBanking`
- `BankingOS.API/ProcessRuntime`
- `BankingOS.API/Configuration`
- `BankingOS.API/Security`
- `BankingOS.API/Reporting`

Near-term practical approach in this repo:
- preserve `BankInsight.API` structure initially
- introduce subfolders by concern under `Entities`, `Services`, `DTOs`, and `Controllers`
- complete naming migration after runtime contracts stabilize

### Frontend module direction
- `src/platform`
- `src/modules/core-banking`
- `src/modules/process-runtime`
- `src/modules/config-studio`
- `src/modules/theme-studio`
- `src/modules/operations-control`

Near-term practical approach in this repo:
- keep current `src/components` and `src/services`
- add `src/modules` and `src/platform` for new BankingOS abstractions
- progressively move hub-specific logic behind shared renderers

## Governance and Control Requirements
- all dynamic forms must be validated server-side
- all sensitive actions must support maker-checker policies
- all configuration publishing must be auditable
- all tenant and branch scopes must be enforced in services, not only the UI
- all operational overrides must record actor, reason, timestamp, and affected records

## Initial Migration Strategy

### Phase 1
- introduce BankingOS naming and documentation
- formalize metadata schemas
- keep current entities and services operational

### Phase 2
- extend process runtime to return richer UI contracts
- extend `JsonFormRenderer` into a production runtime form engine
- move high-volume workflows to metadata-driven process stages

### Phase 3
- add theme metadata and publish flow
- seed sample products, themes, and process packs
- add configuration approval and rollback

### Phase 4
- reduce remaining hardcoded workspaces
- standardize queues, reviews, summaries, and action trays
- align reporting and EOD around process/audit outputs

## Definition of Success
BankingOS is successful when:
- the live platform identity is BankingOS
- backend-defined processes can drive frontend work queues and task screens
- form changes can be made through low-code design and published safely
- themes can be designed and assigned per tenant
- core operational workflows ship preconfigured
- all critical business and configuration actions are auditable and approval-aware
