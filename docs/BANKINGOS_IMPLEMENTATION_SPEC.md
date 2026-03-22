# BankingOS Implementation Spec

## Goal
This document turns the BankingOS vision into an implementation-ready plan for the current repository. It is intentionally grounded in the existing code and focuses on the next concrete build steps.

## Scope
Convert BankInsight into BankingOS with:
- new product identity
- improved backend-driven banking process flow
- frontend-ready metadata contracts
- low-code form design
- theme design
- seeded sample banking processes, products, and themes

## Repo-Aware Starting Point

### Backend
The existing backend already provides strong foundations for:
- workflow and process runtime
- approvals and audit
- lending and account services
- transaction and posting support
- branch and vault operations
- reporting and regulatory support

### Frontend
The existing frontend already includes:
- a JSON form renderer
- a form designer screen
- a process designer
- a task inbox
- multiple operational hubs

This means BankingOS should be built as an extension and reorganization effort, not a greenfield rewrite.

## Implementation Objectives

### 1. Rename and product transition
Implement BankingOS as the new platform identity by:
- updating the app shell
- updating documentation
- updating seeded branding and sample themes
- introducing BankingOS naming in configuration payloads and platform labels

### 2. Unified process flow engine
Upgrade the current runtime so all key banking processes can be expressed as:
- stage-based workflows
- typed task definitions
- action contracts
- validation rules
- approval policies
- UI render metadata

### 3. Metadata-driven frontend
Upgrade the frontend so task and process screens are generated from backend contracts wherever possible.

### 4. Configuration governance
Add publish/version/rollback and maker-checker support for:
- forms
- themes
- process definitions
- product definitions

## Recommended New Artifacts

### Backend entities to add
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

### Backend service areas to add or extend
- `ConfigurationCatalogService`
- `FormRuntimeService`
- `FormDesignerService`
- `ThemeService`
- `ThemePreviewService`
- `ProductDefinitionService`
- `ProcessMetadataService`
- `QueueRuntimeService`
- `PublishWorkflowService`

### Frontend areas to add or extend
- `src/platform/configuration`
- `src/platform/process-runtime`
- `src/platform/theming`
- `src/modules/config-studio`
- `src/modules/theme-studio`
- `src/modules/operations-control`

## Improved Banking Process Flows

### Design standard
Every seeded process must include:
- process code
- business objective
- trigger
- actor roles
- ordered stages
- stage entry criteria
- stage exit rules
- required forms or review panels
- approvals
- posting behavior if financial
- exception path
- audit events
- frontend screen metadata

## Process 1: Customer Onboarding

### Objective
Create a compliant customer record that can be reused across account opening, lending, and service workflows.

### Roles
- relationship officer
- KYC officer
- compliance reviewer
- supervisor

### Stages
1. `lead_capture`
2. `identity_capture`
3. `kyc_document_upload`
4. `screening_validation`
5. `risk_review`
6. `approval`
7. `cif_creation`
8. `customer_activation`

### Actions
- save draft
- submit for review
- request more information
- validate identity
- approve onboarding
- reject onboarding
- activate customer

### Frontend contract
- stepper layout
- checklist pane
- documents panel
- risk flag summary
- decision action tray

## Process 2: Account Opening

### Objective
Open a product-backed account using product rules and configurable onboarding requirements.

### Roles
- customer service officer
- operations reviewer
- supervisor

### Stages
1. `customer_selection`
2. `product_selection`
3. `eligibility_check`
4. `account_setup`
5. `funding_instruction`
6. `approval`
7. `account_creation`
8. `initial_posting`
9. `account_activation`

### Actions
- validate product eligibility
- collect mandates/signatories
- assign account number
- approve opening
- post opening balance
- activate account

### Frontend contract
- dynamic product form
- fee preview
- eligibility result card
- summary confirmation screen

## Process 3: Cash Deposit

### Objective
Receive and post a deposit with teller, account, and branch controls enforced.

### Roles
- teller
- supervisor

### Stages
1. `transaction_initiated`
2. `cash_count`
3. `account_validation`
4. `limit_check`
5. `approval_if_required`
6. `cash_accepted`
7. `transaction_posted`
8. `receipt_generated`
9. `completed`

### Actions
- validate denominations
- validate account status
- apply charges
- request approval
- post deposit
- generate receipt

### Frontend contract
- denomination grid
- charge preview
- available account summary
- receipt panel

## Process 4: Cash Withdrawal

### Objective
Perform controlled cash withdrawal with identity, balance, and limit checks.

### Roles
- teller
- supervisor

### Stages
1. `request_initiated`
2. `identity_verification`
3. `balance_hold_check`
4. `limit_check`
5. `approval_if_required`
6. `cash_paid`
7. `transaction_posted`
8. `receipt_generated`
9. `completed`

### Actions
- verify customer
- validate available balance
- apply fees
- request threshold approval
- confirm payout

### Frontend contract
- ID verification panel
- balance summary
- approval state banner
- acknowledgment capture

## Process 5: Loan Origination

### Objective
Move a loan from application to approved offer using configurable rules and credit controls.

### Roles
- loan officer
- credit analyst
- credit manager
- committee approver

### Stages
1. `application_draft`
2. `submission`
3. `eligibility_rules_check`
4. `document_completion`
5. `credit_analysis`
6. `appraisal`
7. `committee_review`
8. `approval_decision`
9. `offer_generation`
10. `offer_acceptance`
11. `ready_for_disbursement`

### Actions
- run affordability checks
- attach collateral/guarantors
- compute repayment plan
- record committee decision
- generate offer
- accept or decline offer

### Frontend contract
- multi-step flow
- credit memo summary
- collateral panels
- repayment simulation
- decision timeline

## Process 6: Loan Disbursement

### Objective
Release approved funds only after all conditions precedent are met.

### Roles
- operations officer
- supervisor

### Stages
1. `disbursement_request`
2. `final_pre_disbursement_check`
3. `approval_confirmation`
4. `posting_instruction`
5. `funds_release`
6. `customer_notification`
7. `completed`

### Actions
- verify approved terms
- verify settlement account
- validate conditions precedent
- post disbursement
- notify customer

### Frontend contract
- disbursement checklist
- approved terms snapshot
- posting preview
- completion receipt

## Process 7: Repayment Collection

### Objective
Accept and allocate loan repayments according to configured repayment and penalty rules.

### Roles
- teller
- collections officer
- supervisor

### Stages
1. `account_lookup`
2. `due_installment_resolution`
3. `payment_capture`
4. `allocation_preview`
5. `approval_if_exception`
6. `posting`
7. `receipt`
8. `completed`

### Actions
- resolve due schedule
- allocate payment
- apply penalty rules
- request exception approval
- post repayment

### Frontend contract
- schedule grid
- allocation breakdown
- source-of-funds selector
- receipt preview

## Process 8: Arrears Management

### Objective
Track delinquency and guide collections or restructuring actions.

### Roles
- collections officer
- branch manager
- recovery manager

### Stages
1. `delinquency_detection`
2. `classification`
3. `contact_attempt`
4. `promise_to_pay`
5. `escalation_review`
6. `recovery_action`
7. `resolution_or_writeoff_recommendation`

### Actions
- classify arrears bucket
- log contact attempt
- set follow-up
- recommend restructure
- escalate recovery action

### Frontend contract
- collector queue
- arrears aging panel
- contact log
- recommendation panel

## Process 9: Fixed Deposit Lifecycle

### Objective
Manage deposit placement, accrual, maturity, and rollover or payout.

### Roles
- customer service officer
- operations officer
- supervisor

### Stages
1. `placement_request`
2. `funding_validation`
3. `term_setup`
4. `approval`
5. `booking`
6. `accrual_monitoring`
7. `maturity_instruction`
8. `rollover_or_payout`
9. `closure`

### Actions
- validate source funds
- confirm tenor and rate
- book placement
- capture maturity instruction
- roll over or close

### Frontend contract
- tenor preview
- maturity instruction form
- accrual summary
- payout decision panel

## Process 10: Teller Funding and Vault Transfer

### Objective
Control physical cash movement with custody tracking and maker-checker enforcement.

### Roles
- teller
- vault custodian
- branch operations supervisor

### Stages
1. `cash_request`
2. `custody_review`
3. `approval`
4. `transfer_preparation`
5. `handover`
6. `vault_update`
7. `branch_confirmation`
8. `closed`

### Actions
- request till funding
- approve transfer
- record custody chain
- confirm receipt
- update balances
- log mismatch incident

### Frontend contract
- custody timeline
- denomination movement sheet
- branch/till position summary
- incident capture

## Process 11: End-of-Day Operations

### Objective
Ensure controlled close of business with batch completion, reconciliation, and audit output.

### Roles
- operations officer
- finance officer
- supervisor

### Stages
1. `pre_eod_validation`
2. `pending_task_check`
3. `batch_processing`
4. `posting_finalization`
5. `reconciliation`
6. `exception_review`
7. `close_of_business`
8. `reports_and_audit_pack`

### Actions
- verify open items
- run batch jobs
- finalize postings
- review exceptions
- produce reports
- lock business date

### Frontend contract
- EOD checklist dashboard
- progress monitor
- exception queue
- audit pack viewer

## Schema Direction

### Form definition
Suggested fields:
- `Id`
- `Code`
- `Name`
- `TenantScope`
- `Module`
- `CurrentPublishedVersionId`
- `Status`

### Form version
Suggested fields:
- `Id`
- `FormDefinitionId`
- `VersionNumber`
- `SchemaJson`
- `ValidationJson`
- `LayoutJson`
- `IsPublished`
- `PublishedAt`
- `PublishedBy`

### Theme definition
Suggested fields:
- `Id`
- `Code`
- `Name`
- `TenantScope`
- `CurrentPublishedVersionId`

### Theme version
Suggested fields:
- `Id`
- `ThemeDefinitionId`
- `VersionNumber`
- `TokenJson`
- `PreviewMetadataJson`
- `IsPublished`

### Process stage definition
Suggested fields:
- `Id`
- `ProcessDefinitionId`
- `StageCode`
- `DisplayName`
- `Sequence`
- `RolePolicyJson`
- `EntryCriteriaJson`
- `ExitCriteriaJson`
- `ScreenSchemaJson`
- `SlaPolicyJson`

### Process action definition
Suggested fields:
- `Id`
- `ProcessStageDefinitionId`
- `ActionCode`
- `DisplayLabel`
- `RequiresApproval`
- `PermissionCode`
- `ValidationPolicyJson`
- `HandlerName`
- `OutcomeMappingJson`

## Suggested File Direction

### Backend
- `BankInsight.API/Entities/Configuration/`
- `BankInsight.API/Services/Configuration/`
- `BankInsight.API/Services/ProcessRuntime/`
- `BankInsight.API/Controllers/Configuration/`
- `BankInsight.API/DTOs/Configuration/`

### Frontend
- `src/platform/process-runtime/`
- `src/platform/configuration/`
- `src/platform/theming/`
- `src/modules/config-studio/`
- `src/modules/theme-studio/`
- `src/modules/process-workbench/`

## Delivery Phases

### Phase 1: BankingOS identity and schema foundation
- introduce BankingOS naming
- add configuration entities and DTOs
- document metadata contracts
- keep current business flows working

### Phase 2: Runtime process contract upgrade
- extend process services to return stage metadata
- define queue and task screen contracts
- wire frontend inbox and process details to metadata

### Phase 3: Form runtime and low-code design
- upgrade JSON form renderer
- formalize form versioning and publishing
- connect runtime forms to onboarding, account opening, and loans

### Phase 4: Theme studio and white-labeling
- add theme entities and tokens
- implement live preview and publish
- seed sample themes

### Phase 5: Seeded operational packs
- add sample product definitions
- add sample forms
- add sample process packs
- add sample dashboards and queues

### Phase 6: Governance hardening
- maker-checker for config publishing
- branch-aware permission enforcement
- audit and rollback for config and process changes

## Acceptance Criteria
- BankingOS identity exists in docs and implementation direction
- major workflows are defined as backend-driven stage flows
- frontend contracts are explicit enough to drive queue/task rendering
- low-code forms and theming have clear schema/version models
- rollout phases are realistic for the current repository
