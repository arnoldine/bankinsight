# CoreBanker User Manual

Banking Operations Workspace Guide

Version 1.0

Prepared: 31 March 2026

Audience:

- branch operations teams
- tellers and front office users
- lending and treasury users
- finance, risk, audit, and administrators

Document purpose:

- explain the CoreBanker interface
- guide users through the main operational workflows
- support onboarding, reference, and day-to-day execution

Confidentiality notice:

This document is intended for authorized banking, operations, and administrative users only.

## 1. Introduction

CoreBanker is the MudBlazor-based banking workspace in this repository. It is designed for branch operations, customer service, teller work, lending, treasury, accounting, compliance, and administrative control.

This manual explains how to use the CoreBanker interface and its day-to-day business workflows.

## 2. Intended Users

This guide is intended for:

- branch and front-office staff
- customer onboarding officers
- tellers
- loan officers and approvers
- treasury and finance teams
- compliance and security users
- system administrators and super admins

## 3. Signing In

### 3.1 Login Flow

CoreBanker uses a protected login flow.

Standard steps:

1. Open the CoreBanker URL.
2. Enter your username or email and password.
3. Complete MFA if prompted.
4. Wait for the dashboard to load.

### 3.2 Session Handling

CoreBanker includes:

- protected routes
- role-based page visibility
- session-expired messaging
- route-level access control

If the session expires, sign in again before continuing operational work.

## 4. Navigation

CoreBanker uses:

- a left-side navigation drawer
- a dashboard landing page
- workspace pages with guided panels and summary cards

The left menu only shows pages your role can access.

Main pages include:

- Dashboard
- Clients
- Clients Onboard
- Accounts
- Teller
- Transactions
- Loan Management
- Approvals
- Group Lending
- Treasury
- Vault
- Operations Risk
- Accounting
- Statements
- End of Day
- Reporting
- Security Ops
- Migration Workbench
- BankingOS Control
- Products
- Settings
- Audit

## 5. Dashboard

The dashboard is the main operational landing page.

Typical dashboard content:

- customer and account posture
- loan portfolio summaries
- approval and audit indicators
- quick links to operational workspaces

Use the dashboard to decide which workspace needs attention first.

## 6. Client Onboarding and KYC

### 6.1 Creating Clients

Use `Clients Onboard` to create new client records.

Typical tasks:

- capture personal and identity data
- enter customer profile details
- save the client

### 6.2 Client Media

CoreBanker supports media handling for:

- profile photo
- signature image
- ID card front
- ID card back

Each item can move through verification states such as:

- pending
- verified
- rejected

### 6.3 KYC Readiness

CoreBanker uses a KYC readiness model to control downstream actions.

KYC readiness determines whether a client can proceed to:

- account opening
- loan origination

If a client is not ready, the page will show missing requirements and block the next process.

## 7. Clients Workspace

Use `Clients` to:

- search and review client profiles
- inspect KYC readiness
- review uploaded media and documents
- verify or reject profile evidence

Best practice:

1. Confirm the client record.
2. Check readiness status.
3. Review missing requirements before sending the client to another desk.

## 8. Accounts

Use `Accounts` to open and review customer accounts.

Typical workflow:

1. Select the customer.
2. Confirm KYC readiness.
3. Choose the product.
4. Submit account creation.
5. Review balances and status.

CoreBanker blocks account creation when the client is not ready for account opening.

## 9. Teller

The `Teller` workspace is used for frontline transaction work.

Common actions:

- cash deposit
- cash withdrawal
- cheque deposit
- cheque withdrawal

When processing cheque withdrawals, tellers should use issued cheque leaves instead of manual cheque numbers when cheque-book inventory is available.

## 10. Transactions

The `Transactions` workspace supports:

- transaction review
- bulk payment batches
- cheque queue monitoring
- cheque returns
- cheque-book inventory operations

### 10.1 Bulk Payments

Typical flow:

1. Create a batch.
2. Enter payment lines.
3. Submit the batch.
4. Review the result as completed, partial, or failed.

### 10.2 Cheque Processing

CoreBanker supports:

- same-bank cheque deposits
- other-bank cheque deposits
- cheque withdrawals
- cheque return handling
- clearing queue review

### 10.3 Cheque Book Inventory

The system supports:

- stock intake
- issue of cheque books
- issued-book tracking
- leaf-level history
- cancellation of unused leaves

## 11. Loan Management

The `Loan Management` page is a full workbench for lending operations.

Main tabs:

- Portfolio
- Origination
- Review
- Repayment
- Schedule
- Operations

### 11.1 Portfolio

Use the portfolio tab to:

- search loans
- filter by status
- view outstanding balances
- route a facility into review, schedule, repayment, or operations

### 11.2 Origination

Use the origination tab to:

- select customer
- confirm KYC readiness
- choose product
- enter principal
- run credit check
- preview the schedule
- submit the application

### 11.3 Review

Use the review tab to:

- inspect queued applications
- appraise a loan
- approve a loan
- add decision notes
- route a loan to disbursement operations

### 11.4 Repayment

Use the repayment tab to:

- select a live facility
- choose a settlement account
- post repayment
- inspect schedule context

### 11.5 Schedule

Use the schedule tab to:

- load a facility
- inspect amortization lines
- review due dates, principal, interest, total, and balance

### 11.6 Operations

Use the operations tab to:

- disburse approved facilities
- provide servicing account details
- provide collateral account details
- assess penalties
- classify loan quality

## 12. Approvals

Use `Approvals` for maker-checker workflows.

Typical actions:

- load pending approvals
- approve an item
- reject an item
- review operational context before decision

## 13. Group Lending

The `Group Lending` workspace supports:

- group and center setup
- membership management
- application handling
- meetings
- collections
- statements and schedules
- reschedule actions

## 14. Treasury

The `Treasury` workspace supports:

- treasury positions
- FX trades
- investment operations
- summaries and reconciliation

Typical tasks:

- create a position
- reconcile a position
- enter FX trades
- settle or approve treasury actions
- manage investments through their lifecycle

## 15. Vault

The `Vault` workspace supports:

- till opening
- cash allocation
- cash return
- till close
- vault counts
- vault movements

Operational control fields may include:

- control reference
- witness officer
- seal number

## 16. Operations Risk

Use `Operations Risk` to review:

- risk indicators
- exposure calculations
- operational exceptions
- related loan servicing and classification controls

## 17. Accounting

Use `Accounting` for:

- chart of accounts review
- journal posting
- ledger inspection

Users should verify values, references, and balancing before posting journals.

## 18. Statements

The `Statements` page supports:

- income statement
- cash flow
- trial balance

Always confirm the reporting period and currency context before using results operationally.

## 19. End of Day

Use `End of Day` to:

- inspect EOD status
- run operational steps
- review scheduler state
- inspect logs and outcomes

Automated operational jobs may include:

- cheque clearing
- overdue loan servicing or collection checks

## 20. Reporting

Use `Reporting` to:

- choose a report
- apply filters
- run the report
- review history and output

## 21. Security Ops

Use `Security Ops` to monitor:

- security alerts
- failed logins
- sessions
- devices
- suspicious activity

## 22. Migration Workbench

Use `Migration Workbench` for controlled data imports.

Typical flow:

1. choose the migration target
2. use the correct file template
3. preview the data
4. submit the import
5. review error or success output

## 23. BankingOS Control

Use `BankingOS Control` for:

- forms
- process catalogs
- bundles
- workflow context
- product rules
- runtime task handling

This area is intended for controlled configuration and workflow operations, not casual data editing.

## 24. Products

Use `Products` to review and configure product context such as:

- product definitions
- amount ranges
- currency context

## 25. Settings

Use `Settings` for administrative controls such as:

- users
- roles
- branches
- privilege leases
- system configuration

Only authorized admin users should change settings or access-control data.

## 26. Audit

Use `Audit` to:

- inspect audit trails
- filter by module or status
- review error or failure events
- inspect changed values where available

## 27. Currency Display

CoreBanker uses standardized currency formatting across the main workspaces.

Users should still confirm:

- the displayed currency code on cross-currency views
- the account or product currency before acting

## 28. Troubleshooting

### 28.1 If a Page Does Not Open

- confirm your role has the needed permission
- refresh the page
- sign out and sign in again

### 28.2 If a Workflow Is Blocked

Possible reasons:

- missing KYC evidence
- approval not completed
- account or loan status does not allow the action
- server-side validation failed

### 28.3 If a Session Expires

- sign in again
- reopen the workspace
- confirm the operation did not partially post before retrying

## 29. Best Practices

- Verify identity before transactional work.
- Use references and notes for audit-sensitive actions.
- Review KYC readiness before accounts or loans.
- Use cheque-book inventory controls consistently.
- Inspect schedule impact after repayments or disbursement.
- Use audit and reporting tools for investigations.

## 30. Conclusion

CoreBanker is a full banking operations workspace for controlled execution across front office, lending, treasury, accounting, reporting, and administration. Users should rely on page-level guidance, readiness indicators, and approval flows to complete work safely and consistently.
