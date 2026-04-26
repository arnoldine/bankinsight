# BankInsight.API Exhaustive Documentation

Last updated: 2026-04-13  
Repository root: [C:\Backup old\dev\bankinsight](C:\Backup old\dev\bankinsight)  
API project: [C:\Backup old\dev\bankinsight\BankInsight.API](C:\Backup old\dev\bankinsight\BankInsight.API)

## Purpose

`BankInsight.API` is the core backend for the BankInsight platform and its companion operational frontend, CoreBanker. It provides:

- Core banking operations
- Customer and KYC management
- Accounts, ledger, deposits, teller, and transaction posting
- Loan origination, appraisal, servicing, accruals, classification, write-off, recovery, and credit checks
- Treasury, FX, investments, vault, cash control, and branch operations
- Reporting, regulatory returns, ORASS, and enterprise reporting
- BankingOS workflow and runtime orchestration
- Security operations, WAF, sessions, user activity, audit, and device monitoring
- Data migration and file/media handling
- Client self-service channel APIs
- Fintech rails through the HybridTransfer module

This document is based on the actual code structure in the repository, especially:

- [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs)
- [ApplicationDbContext.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\ApplicationDbContext.cs)
- [DatabaseSchemaBootstrapper.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\DatabaseSchemaBootstrapper.cs)
- [Controllers](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers)
- [Services](C:\Backup old\dev\bankinsight\BankInsight.API\Services)
- [DTOs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs)
- [Entities](C:\Backup old\dev\bankinsight\BankInsight.API\Entities)

## High-Level Architecture

## Solution Shape

The API is an ASP.NET Core `net8.0` web application with:

- Entity Framework Core using PostgreSQL for persistence
- JWT-based authentication for staff and client channels
- Permission-based authorization
- Middleware-driven security and operational controls
- Hosted services for scheduled and background processing
- A separate fintech submodule integrated into the same API host

Primary project file:

- [BankInsight.API.csproj](C:\Backup old\dev\bankinsight\BankInsight.API\BankInsight.API.csproj)

## Architectural Layers

The API follows a practical layered structure:

- `Controllers`: HTTP API surface
- `Services`: business logic and orchestration
- `DTOs`: request/response contracts
- `Entities`: EF Core persistence models
- `Data`: `DbContext`, schema bootstrap, seeding, migrations
- `Security` and `Infrastructure`: auth, permissioning, middleware, request pipeline utilities
- `Modules\Fintech`: HybridTransfer integration for external rails and fintech posting

## Primary Runtime Model

At runtime, the API hosts:

- the core banking domain database context
- the HybridTransfer fintech persistence context
- background jobs such as EOD and file scan services
- middleware for WAF, IP allowlisting, rate limiting, scripting sandboxing, and performance monitoring

## Technology Stack

- .NET 8 / ASP.NET Core
- Entity Framework Core 8
- Npgsql / PostgreSQL
- JWT Bearer authentication
- Swashbuckle / Swagger
- ML.NET for internal credit scoring
- Jint for controlled scripting features
- HybridTransfer domain/application/infrastructure modules for fintech rails

## Core Startup and Composition

Startup lives in [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs).

Important behaviors:

- Resolves `DB_CONNECTION_STRING` from environment first
- Forces both `DefaultConnection` and `HybridTransferDb` to the same resolved connection string during startup
- Registers the full service container for banking, reporting, security, treasury, workflow, client channel, and fintech services
- Configures JWT auth for staff, Clerk, and client channels
- Applies migrations and optional schema bootstrap
- Seeds required system metadata
- Maps health and controller endpoints

## Request Pipeline

The HTTP pipeline order in [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs) is:

1. global error handling
2. performance monitoring
3. WAF
4. rate limiting
5. IP whitelist enforcement
6. Jint scripting sandbox
7. response compression
8. CORS
9. HTTPS redirection
10. authentication
11. authorization
12. antiforgery
13. controller routing

## Health Endpoint

Anonymous health endpoint:

- `GET /health`

Returns service status, environment, and current UTC time.

## Configuration

Base config is in [appsettings.json](C:\Backup old\dev\bankinsight\BankInsight.API\appsettings.json).  
Development overrides are in [appsettings.Development.json](C:\Backup old\dev\bankinsight\BankInsight.API\appsettings.Development.json).

## Key Configuration Areas

### Database

- `DB_CONNECTION_STRING`
- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:HybridTransferDb`

### JWT

- `JWT_SECRET`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`
- `JwtSettings:ClientIssuer`
- `JwtSettings:ClientAudience`

### Startup Flags

- `SKIP_DB_MIGRATIONS`
- `ENABLE_SCHEMA_BOOTSTRAP`

These shortcuts are only allowed in Development or Testing.

### Security

- `Security:IpWhitelist:Enabled`
- `Security:IpWhitelist:AllowedIps`
- `Security:SuspiciousActivity:LargeTransactionThreshold`

### SMTP / Alerts

- `SmtpSettings:*`

### Credit Bureau

- `CreditBureau:*`

### Fintech Providers

- `FintechProviders:MobileMoney:*`
- `FintechProviders:BankTransfer:*`
- `FintechProviders:CryptoCustody:*`
- `FintechProviders:Webhook:*`
- `FintechLedger:*`

### Client and Portal Features

- `ClientPortal:*`
- `ClientFileStorage:*`

### Webhook and External Identity

- `Clerk:*`
- `Clerk:WebhookSecret`

### FX and Treasury

- `BankOfGhana:FxRatesUrl`
- `Treasury:*`

### Syncfusion / UI Consumers

The API itself is frontend-agnostic, but recent UI integrations rely on stable paging, filtering, and compact DTOs. That means API changes should preserve:

- lightweight list endpoints
- paged list contracts
- deterministic enum/string values
- stable route shapes for both React and Blazor consumers

## Authentication and Authorization

Authentication is multi-channel and is intentionally split by actor type.

### Staff Authentication

Primary staff login is exposed through [AuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AuthController.cs) under `api/auth`.

Key features:

- username/password login
- MFA verification and resend
- token refresh
- session validation
- self profile inspection
- logout

### Clerk Authentication

[ClerkAuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClerkAuthController.cs) supports Clerk-backed identity synchronization and webhook-driven user reconciliation.

### Client Authentication

[ClientAuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientAuthController.cs) provides retail/client-channel auth, including:

- registration and verification
- MFA
- password reset
- step-up authentication
- transaction PIN setup
- refresh and logout

### Authorization Model

Authorization is layered:

1. authentication schemes establish identity
2. `[Authorize]` protects controllers
3. `[HasPermission(...)]` enforces business permissions
4. some routes add role restrictions such as `Administrator`

Permission constants live under the security layer, primarily in files under [BankInsight.API\Security](C:\Backup old\dev\bankinsight\BankInsight.API\Security).

## Security and Operational Protection

Security is not a single feature. It is spread across middleware, controller endpoints, logging, session control, and anomaly detection.

### WAF

The built-in WAF is implemented in:

- [WafMiddleware.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Infrastructure\WafMiddleware.cs)
- [WafService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\WafService.cs)
- [SecurityController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\SecurityController.cs)

The WAF layer is designed to:

- inspect inbound requests
- block obviously malicious patterns
- log incidents
- expose profile and tuning data to the security UI

### Rate Limiting and IP Whitelisting

[Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs) wires:

- rate limiting
- IP whitelist enforcement

These controls are important for admin surfaces, integration routes, and sensitive transaction APIs.

### Session and Device Security

Session and device controls are handled through:

- [SessionController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\SessionController.cs)
- [SecurityController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\SecurityController.cs)
- [DeviceSecurityService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\DeviceSecurityService.cs)
- [SessionService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\SessionService.cs)

Capabilities include:

- active session listing
- session invalidation
- device registration and actioning
- outdated-device scans
- suspicious/irregular transaction inspection
- failed-login monitoring

### Audit and Suspicious Activity

Operational security evidence is preserved through:

- [AuditController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AuditController.cs)
- [UserActivityController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\UserActivityController.cs)
- audit logging inside core services

## Persistence and Database Model

### Main EF Core Context

Primary persistence lives in [ApplicationDbContext.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\ApplicationDbContext.cs).

It contains entity sets for the main banking platform, including:

- customers
- accounts
- transactions
- loans
- products
- approvals
- audit logs
- reporting artifacts
- branch and treasury objects
- cheque and payment operations
- workflow objects
- client-channel/KYC objects
- security and device records

### Schema Bootstrap and Migrations

The application supports both conventional EF migrations and dynamic schema bootstrap:

- [DatabaseSchemaBootstrapper.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\DatabaseSchemaBootstrapper.cs)
- EF migration folders in [BankInsight.API\Migrations](C:\Backup old\dev\bankinsight\BankInsight.API\Migrations)

This is useful in environments where the data model evolved quickly and runtime safety checks are needed for missing columns/tables.

### Seeding

Required metadata is seeded through [DatabaseSeeder.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\DatabaseSeeder.cs).

The platform now avoids reseeding demo business data by default, which is especially important after migration runs.

## Background and Hosted Services

### End-of-Day Scheduling

[EodSchedulerHostedService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\EodSchedulerHostedService.cs) drives scheduled operations and can coordinate:

- EOD processing
- accrual-type tasks
- automated operational checks
- loan collection and cheque clearing flows where configured

### Client File Scanning

[ClientFileScanHostedService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\ClientFileScanHostedService.cs) handles deferred or periodic scanning/inspection of client-uploaded artifacts.

## Domain Modules

This section documents the major business domains in the API.

### 1. Authentication and Identity

Primary files:

- [AuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AuthController.cs)
- [ClerkAuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClerkAuthController.cs)
- [ClientAuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientAuthController.cs)
- [AuthService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\AuthService.cs)
- [ClerkAuthService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\ClerkAuthService.cs)
- [ClientAuthService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\ClientAuthService.cs)

Responsibilities:

- staff sign-in and MFA
- client onboarding and sign-in
- token refresh
- logout and session lifecycle
- Clerk synchronization

### 2. Users, Roles, Sessions, Audit, and Privilege Control

Primary files:

- [UserController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\UserController.cs)
- [RoleController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\RoleController.cs)
- [SessionController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\SessionController.cs)
- [AuditController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AuditController.cs)
- [PrivilegeLeaseController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\PrivilegeLeaseController.cs)

Responsibilities:

- user administration
- role and permission assignment
- active session management
- audit inspection
- temporary privilege leasing and revocation

### 3. Customers, KYC, Client Files, Complaints, and Portal Profile

Primary files:

- [CustomerController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\CustomerController.cs)
- [KycController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\KycController.cs)
- [ClientKycOperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientKycOperationsController.cs)
- [ClientComplaintOperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientComplaintOperationsController.cs)
- [ClientChannelController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientChannelController.cs)
- [ClientFileController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientFileController.cs)

Responsibilities:

- customer creation and maintenance
- customer profile assembly
- notes and document registration
- profile photo, ID, and signature media upload
- KYC limit calculation and validation
- complaint queue operations
- client self-service profile and banking APIs

### 4. Products, Fees, and Charges

Primary files:

- [ProductController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ProductController.cs)
- [FeeController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\FeeController.cs)
- [ProductService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\ProductService.cs)
- [FeeService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\FeeService.cs)

Responsibilities:

- product catalog retrieval and maintenance
- pricing/charge application
- product-safe DTO exposure for frontend use

### 5. Accounts, Ledger, Transactions, and Deposits

Primary files:

- [AccountController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AccountController.cs)
- [LedgerController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\LedgerController.cs)
- [TransactionController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\TransactionController.cs)
- [DepositController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\DepositController.cs)
- [AccountService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\AccountService.cs)
- [TransactionService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\TransactionService.cs)
- [DepositEngine.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\DepositEngine.cs)

Responsibilities:

- account creation and retrieval
- customer-specific account lists
- paged account search
- deposits, withdrawals, transfers, and cheque ledger postings
- fixed/term deposit create, renew, and close

### 6. Loans and Credit Scoring

Primary files:

- [LoanController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\LoanController.cs)
- [LoanService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\LoanService.cs)
- [CreditBureauService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\CreditBureauService.cs)
- [InternalCreditScoringService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\InternalCreditScoringService.cs)
- [InternalCreditScoreAssessment.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Entities\InternalCreditScoreAssessment.cs)

Responsibilities:

- application intake
- appraisal and approval
- disbursement
- repayment and reversal
- restructuring
- write-off and recovery
- accrual processing
- product and accounting profile configuration
- credit bureau integration
- ML.NET-based internal behavioral scoring
- composite eligibility decisions

### 7. Group Lending

Primary files:

- [GroupController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\GroupController.cs)
- [GroupLendingController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\GroupLendingController.cs)
- [GroupLendingService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\GroupLendingService.cs)

Responsibilities:

- group creation and maintenance
- center creation
- group applications
- group meetings and attendance
- collection batches
- group loan schedule and statement views
- rescheduling and repayment
- PAR and performance reporting

### 8. General Ledger and Accounting

Primary files:

- [GlController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\GlController.cs)
- [LedgerController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\LedgerController.cs)
- [FinancialReportService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\FinancialReportService.cs)

Responsibilities:

- GL account maintenance
- journal entry posting
- regulatory seeding for account structures
- financial statement generation
- trial balance production aligned to migrated GL balances

### 9. Treasury, FX, Investments, and Risk

Primary files:

- [TreasuryPositionController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\TreasuryPositionController.cs)
- [FxRateController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\FxRateController.cs)
- [FxTradingController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\FxTradingController.cs)
- [InvestmentController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\InvestmentController.cs)
- [RiskAnalyticsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\RiskAnalyticsController.cs)

Responsibilities:

- treasury position capture and reconciliation
- FX rate maintenance and conversion
- FX dealing, approval, and settlement
- investment creation, approval, rollover, liquidation, and maturity
- risk metrics such as VaR, liquidity, exposure, alerts, and dashboards

### 10. Branch, Vault, Cash, and Inter-Branch Operations

Primary files:

- [BranchController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\BranchController.cs)
- [BranchConfigController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\BranchConfigController.cs)
- [BranchHierarchyController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\BranchHierarchyController.cs)
- [BranchLimitController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\BranchLimitController.cs)
- [VaultController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\VaultController.cs)
- [CashControlController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\CashControlController.cs)
- [CashIncidentController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\CashIncidentController.cs)
- [InterBranchTransferController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\InterBranchTransferController.cs)

Responsibilities:

- branch maintenance
- branch configuration and hierarchy
- branch limits and validation
- till open/allocate/return/close operations
- vault counts and vault transactions
- branch cash position and reconciliation
- cash incident tracking
- inter-branch transfer orchestration

### 11. Payments, Cheques, and Cheque Books

Primary files:

- [PaymentOperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\PaymentOperationsController.cs)
- [PaymentOperationsService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\PaymentOperationsService.cs)
- [PaymentOperationsDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\PaymentOperationsDTOs.cs)

Responsibilities:

- bulk payment batch creation and retrieval
- cheque deposit lodgement
- cheque withdrawals
- cheque returns
- cheque queue inspection
- cheque-book stock creation
- cheque-book issuance
- cheque-leaf cancellation
- historical cheque-leaf usage import

### 12. Workflows, Runtime Tasks, and Approvals

Primary files:

- [ApprovalController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ApprovalController.cs)
- [WorkflowController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\WorkflowController.cs)
- [WorkflowDefinitionController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\WorkflowDefinitionController.cs)
- [WorkflowRuntimeController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\WorkflowRuntimeController.cs)

Responsibilities:

- approval queue retrieval and actioning
- workflow design-time configuration
- versioning and validation
- runtime task assignment, claiming, completion, and rejection

### 13. BankingOS

Primary files:

- [BankingOSController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\BankingOSController.cs)
- [BankingOSService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\BankingOSService.cs)

Responsibilities:

- process pack retrieval
- process catalog and process launch
- task context retrieval and completion
- forms, themes, and publish bundle workflows
- BankingOS product management

### 14. Reporting, Financials, Regulatory Reporting, ORASS, and Analytics

Primary files:

- [EnterpriseReportsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\EnterpriseReportsController.cs)
- [ReportController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ReportController.cs)
- [ReportingController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ReportingController.cs)
- [FinancialReportsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\FinancialReportsController.cs)
- [RegulatoryReportsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\RegulatoryReportsController.cs)
- [AnalyticsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AnalyticsController.cs)
- [OrassController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\OrassController.cs)

Responsibilities:

- report catalogs and definitions
- report execution, history, favorites, and presets
- financial statements and trial balance
- regulatory return preparation and submission
- ORASS readiness, queue, history, evidence, reconciliation, and acknowledgement
- analytics such as segmentation, trends, product, channel, and staff productivity

### 15. Operations and End of Day

Primary files:

- [OperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\OperationsController.cs)
- [OperationsService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\OperationsService.cs)
- [EodSchedulerHostedService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\EodSchedulerHostedService.cs)

Responsibilities:

- EOD status inspection
- step-level EOD execution
- scheduled loan collection, cheque clearing, and other batch operations where configured

### 16. Migration

Primary files:

- [DataMigrationController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\DataMigrationController.cs)
- [MigrationService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\MigrationService.cs)

Responsibilities:

- enumerating importable datasets
- importing customers, products, accounts, loans, GL accounts, and related migration payloads

### 17. Fintech Rails and HybridTransfer

The API hosts the core banking domain but also wires in the HybridTransfer fintech module.

Primary wiring files:

- [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs)
- [ServiceCollectionExtensions.cs](C:\Backup old\dev\bankinsight\Modules\Fintech\src\HybridTransfer.Infrastructure\DependencyInjection\ServiceCollectionExtensions.cs)

This module covers:

- mobile money
- bank transfer rails
- webhooks
- crypto custody abstractions
- fintech ledger integration

## Controller and Route Map

This section is intended as a practical route inventory. It lists the major controllers and the route families they expose.

### Account and Transaction Surface

- `api/accounts`
  - `GET /api/accounts`
  - `GET /api/accounts/paged`
  - `GET /api/accounts/{id}`
  - `GET /api/accounts/customer/{cif}`
  - `POST /api/accounts`
- `api/transactions`
  - `GET /api/transactions`
  - `GET /api/transactions/{id}`
  - `POST /api/transactions`
- `api/ledger`
  - `POST /api/ledger/deposits`
  - `POST /api/ledger/withdrawals`
  - `POST /api/ledger/transfers`
  - `POST /api/ledger/cheques`
  - `GET /api/ledger/ledger`
  - `GET /api/ledger/balance/{accountId}`
  - `GET /api/ledger/margins/{customerId}`
- `api/deposits`
  - `GET /api/deposits`
  - `GET /api/deposits/{id}`
  - `POST /api/deposits`
  - `POST /api/deposits/{id}/renew`
  - `POST /api/deposits/{id}/close`

### Customer and Client Surface

- `api/customers`
  - `GET /api/customers`
  - `GET /api/customers/paged`
  - `GET /api/customers/{id}`
  - `GET /api/customers/{id}/profile`
  - `GET /api/customers/{id}/kyc`
  - `POST /api/customers`
  - `PUT /api/customers/{id}`
  - `POST /api/customers/{id}/notes`
  - `POST /api/customers/{id}/documents`
  - `POST /api/customers/{id}/media`
- `api/kyc`
  - `GET /api/kyc/limits/{customerId}`
  - `GET /api/kyc/daily-limit/{customerId}`
  - `POST /api/kyc/validate-ghana-card`
- `api/client-channel`
  - profile, media, KYC refresh, banking overview, merchants, payments, loans, sessions, statements, complaints
- `api/client-files`
  - customer media and complaint attachment retrieval
- `api/client-kyc-ops`
  - queue and review actions
- `api/client-complaint-ops`
  - queue, summary, detail, triage, escalation, close, SLA processing

### Loan and Group Lending Surface

- `api/loans`
  - `GET /api/loans`
  - `POST /api/loans/disburse`
  - `POST /api/loans/apply`
  - `POST /api/loans/approve`
  - `POST /api/loans/repay`
  - `POST /api/loans/check-credit`
  - `POST /api/loans/generate-schedule`
  - `POST /api/loans/products/configure`
  - `GET /api/loans/products`
  - `POST /api/loans/accounting-profiles/configure`
  - `POST /api/loans/appraise`
  - `POST /api/loans/restructure`
  - `POST /api/loans/repay/reverse`
  - `POST /api/loans/accruals/process`
  - `POST /api/loans/writeoff`
  - `POST /api/loans/recover`
  - `GET /api/loans/{id}/statement`
  - `GET /api/loans/{id}/gl-postings`
  - `GET /api/loans/dashboards/delinquency`
  - `GET /api/loans/reports/profitability`
  - `GET /api/loans/reports/balance-sheet`
  - `GET /api/loans/credit-bureau/providers`
  - `GET /api/loans/credit-scoring/status`
  - `GET /api/loans/{id}/schedule`
  - `GET /api/loans/{id}/accrual`
  - `POST /api/loans/{id}/repay`
  - `POST /api/loans/{id}/penalty`
  - `POST /api/loans/{id}/classify`
- `api/groups`
  - basic group and membership maintenance
- `api/group-lending`
  - group, center, application, meeting, collection, product-designer, statement, schedule, PAR, and performance endpoints

### Payments and Cheques Surface

- `api/payments`
  - `GET /api/payments/bulk`
  - `GET /api/payments/bulk/{batchId}`
  - `POST /api/payments/bulk`
  - `GET /api/payments/cheques`
  - `GET /api/payments/cheques/{itemId}`
  - `POST /api/payments/cheques/deposits`
  - `POST /api/payments/cheques/withdrawals`
  - `POST /api/payments/cheques/{itemId}/return`
  - `GET /api/payments/cheque-books`
  - `GET /api/payments/cheque-books/{bookId}`
  - `POST /api/payments/cheque-books/stock`
  - `POST /api/payments/cheque-books/{bookId}/issue`
  - `POST /api/payments/cheque-books/leaves/{leafId}/cancel`
  - `POST /api/payments/cheque-books/leaves/use-history`

### Branch, Cash, Vault, and Treasury Surface

- `api/Branch`
  - branch CRUD
- `api/BranchConfig`
  - branch config create, read, list, delete
- `api/BranchHierarchy`
  - branch hierarchy create, tree, children, delete
- `api/BranchLimit`
  - branch limit create, update, validate, list, delete
- `api/Vault`
  - vault and till views
  - till open, allocate, return, close
  - vault count and vault transaction
- `api/cash-control`
  - vault cash position, branch summary, reconciliation, transit items
- `api/cash-incidents`
  - incident list, create, resolve
- `api/InterBranchTransfer`
  - create, approve, dispatch, receive, list, pending
- `api/TreasuryPosition`
  - create, update, reconcile, summary, latest-by-currency, close
- `api/FxRate`
  - CRUD, latest, history, convert, sync-bog
- `api/FxTrading`
  - create, approve, settle, pending, stats
- `api/treasury/investments`
  - create, approve, rollover, liquidate, mature, portfolio, accrue
- `api/RiskAnalytics`
  - VaR, LCR, currency exposure, alerts, dashboard, daily calculations

### Reporting and Regulatory Surface

- `api/reports`
  - report catalog
  - execute/export
  - history, favorites, presets
  - CRB data-quality
- `api/Report`
  - definitions, generation, runs, regulatory outputs, financial outputs, analytics outputs
- reporting controller routes
  - definitions, generate, history, runs, delete
- financial report routes
  - balance sheet
  - income statement
  - cash flow
  - trial balance
- regulatory report routes
  - daily position
  - monthly returns
  - prudential
  - large exposure
  - submit and submit-to-bog
  - history
- `api/orass`
  - profile
  - readiness
  - queue
  - history
  - evidence
  - submit
  - acknowledge
  - reconcile
- analytics routes
  - customer segmentation
  - transaction trends
  - product analytics
  - channel analytics
  - staff productivity

### Workflow, Security, and Operations Surface

- `api/approvals`
  - queue and action routes
- `api/workflows`
  - workflow list/create/update
- `api/WorkflowDefinition`
  - definition CRUD-like authoring, versioning, steps, transitions, validate
- `api/WorkflowRuntime`
  - start, my tasks, claimable tasks, claim, complete, reject
- `api/security`
  - alerts
  - failed logins
  - sessions
  - summary
  - devices
  - device actions
  - irregular transactions
  - WAF profile get/update
- `api/operations/eod`
  - status
  - run-step

### Admin and Support Surface

- `api/auth`
  - login, MFA, validate, me, refresh, logout
- `api/clerk`
  - me, sync, webhook
- `api/client-auth`
  - login, register, verify, MFA, refresh, password reset, step-up, transaction-pin, logout
- `api/users`
  - list, get, create, update, delete
- `api/roles`
  - list, create, update
- `api/Session`
  - refresh, invalidate, invalidate-all, active, by-user, stats
- `api/config`
  - get and post config
- `api/migration`
  - datasets and dataset import

## Major Data Entities

The model is large, but these are the most important conceptual entities the API revolves around:

- `Customer`
- `CustomerProfile`
- `CustomerDocument`
- `CustomerMediaAsset`
- `Account`
- `Transaction`
- `Deposit`
- `Loan`
- `LoanProduct`
- `LoanAccountingProfile`
- `InternalCreditScoreAssessment`
- `Group`
- `LendingCenter`
- `Approval`
- `WorkflowDefinition`
- `WorkflowVersion`
- `WorkflowTask`
- `AuditLog`
- `User`
- `Role`
- `Branch`
- `BranchLimit`
- `VaultPosition` / till-related entities
- `TreasuryPosition`
- `FxRate`
- `FxTrade`
- `Investment`
- `ChequeClearingItem`
- `ChequeBookInventory`
- `ChequeLeaf`
- `BulkPaymentBatch`
- `BulkPaymentItem`
- `RegulatoryReturn`
- `ReportDefinition`
- `ReportRun`

## File and Media Handling

Customer and complaint-related files are handled through dedicated client-file services rather than raw static-file exposure.

Important files:

- [ClientFileController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientFileController.cs)
- [CustomerController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\CustomerController.cs)
- [ClientChannelController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientChannelController.cs)

Supported media concepts include:

- profile photo
- signature
- ID front
- ID back
- complaint attachments

## Observability and Auditability

The API favors operational traceability through:

- audit logs
- user activity reports
- report history
- approval logs
- security alerts
- login attempt records
- device and session tracking
- EOD status and batch-result records

Cross-cutting observability is implemented by middleware plus domain-service audit creation.

## Deployment and Runtime Notes

### Local Development

The API is typically run either:

- directly through `dotnet`
- through Docker Compose with the `bankinsight-api` service

The application expects a reachable PostgreSQL database and can optionally also initialize HybridTransfer fintech persistence.

### Containerization

Primary deployment files include:

- [Dockerfile](C:\Backup old\dev\bankinsight\BankInsight.API\Dockerfile)
- solution-level compose files in the repository root and deployment folders

### Reverse Proxy / Frontend Consumers

Both frontends depend on this API:

- React BankInsight
- Blazor CoreBanker

That means API changes should be reviewed for compatibility in both:

- request shapes
- enum/string values
- response payload size
- paging support
- error contracts

## Operational Recommendations

### For New Developers

Read in this order:

1. [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs)
2. [ApplicationDbContext.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\ApplicationDbContext.cs)
3. [AuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AuthController.cs)
4. [CustomerController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\CustomerController.cs)
5. [AccountController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AccountController.cs)
6. [LoanController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\LoanController.cs)
7. [PaymentOperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\PaymentOperationsController.cs)
8. [SecurityController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\SecurityController.cs)
9. [OperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\OperationsController.cs)
10. one representative reporting controller such as [EnterpriseReportsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\EnterpriseReportsController.cs)

### For Production Hardening

Confirm these before release:

- JWT secrets are environment-managed
- Clerk webhook secret is configured
- Bank of Ghana FX source is configured
- database migrations are safe for the target environment
- WAF and IP whitelist settings are aligned to actual operations
- SMTP and alerting are configured
- report/export paths and file storage are validated
- long-running operations are monitored

### For Large Dataset Performance

Prefer:

- paged endpoints
- projection DTOs
- filtered queries
- compressed responses
- avoiding full-table list loads in UI consumers

This matters especially for:

- customers
- accounts
- loans
- transactions
- report histories

## Suggested Reading by Use Case

### If you are working on staff operations

Start with:

- [AuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AuthController.cs)
- [UserController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\UserController.cs)
- [RoleController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\RoleController.cs)
- [ApprovalController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ApprovalController.cs)
- [SecurityController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\SecurityController.cs)

### If you are working on retail/client channels

Start with:

- [ClientAuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientAuthController.cs)
- [ClientChannelController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientChannelController.cs)
- [ClientKycOperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientKycOperationsController.cs)
- [ClientComplaintOperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientComplaintOperationsController.cs)

### If you are working on lending

Start with:

- [LoanController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\LoanController.cs)
- [LoanService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\LoanService.cs)
- [InternalCreditScoringService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\InternalCreditScoringService.cs)
- [GroupLendingController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\GroupLendingController.cs)

### If you are working on accounting and reporting

Start with:

- [GlController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\GlController.cs)
- [FinancialReportsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\FinancialReportsController.cs)
- [ReportController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ReportController.cs)
- [RegulatoryReportsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\RegulatoryReportsController.cs)
- [OrassController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\OrassController.cs)

## Known Architectural Characteristics

- The API is broad and intentionally monolithic at the application boundary.
- Business domains are decomposed into many services, but startup composition is still centralized in [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs).
- Several controllers serve as orchestration layers over large service classes.
- The platform supports both internal staff operations and external client channels from the same backend.
- Reporting and regulatory workflows are first-class citizens, not bolt-on features.
- Migration support is embedded directly in the platform.
- Security is layered through middleware, services, and explicit operational endpoints.

## Document Scope and Limits

This document is exhaustive at the architectural and route-surface level, but it intentionally does not inline every DTO property or every entity field because those are numerous and evolve quickly. For exact contract details, read:

- [BankInsight.API\DTOs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs)
- [BankInsight.API\Entities](C:\Backup old\dev\bankinsight\BankInsight.API\Entities)
- [BankInsight.API\Controllers](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers)

## Quick Summary

BankInsight.API is a multi-domain banking backend that supports:

- staff and client authentication
- customer and KYC management
- accounts, deposits, ledger, and transactions
- loan origination, servicing, and internal credit scoring
- group lending
- branch, cash, vault, and treasury operations
- cheque, cheque-book, and bulk-payment workflows
- workflows, approvals, and privilege leasing
- BankingOS orchestration
- financial, enterprise, regulatory, and ORASS reporting
- EOD scheduling and migration tooling
- security operations, WAF management, and auditability

It is the shared backend for both the React BankInsight frontend and the Blazor CoreBanker frontend.
