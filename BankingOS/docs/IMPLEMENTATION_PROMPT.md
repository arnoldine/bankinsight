# BankingOS Complete Implementation Prompt

Convert the existing BankInsight repository into a new product called BankingOS.

## Important identity rule
- Use BankInsight only as the source code foundation.
- During implementation, create and use a new product identity named BankingOS.
- Update application naming, docs, sample branding, admin workspace naming, and platform terminology to BankingOS.
- The result must be a configurable core banking platform, not just a renamed app.

## Repo context
- The current backend is a .NET 8 API in `BankInsight.API`.
- The current frontend is React + TypeScript + Vite in `src`.
- The repository already includes reusable foundations such as workflow/process runtime services, dynamic form rendering, a form designer screen, a process designer, approvals, audit logging, RBAC, lending/account/transaction services, and branch/vault/treasury/reporting services.
- Reuse and extend those existing foundations where sensible instead of rebuilding from scratch.

## Primary objective
Build BankingOS as a fully customizable, multi-tenant, backend-driven core banking platform for a Bank of Ghana regulated institution, with:
- prebuilt banking processes
- frontend-ready metadata that automatically powers screens and actions
- low-code form designer
- theme designer
- seeded sample products, forms, workflows, and themes
- governance, maker-checker controls, auditability, versioning, and publish/rollback support

## Core platform principles
- backend is the source of truth for banking rules, process states, task actions, validations, approval policies, product behavior, and rendering metadata
- frontend should render queues, forms, summaries, reviews, and action trays from backend metadata wherever practical
- configuration data must be versioned and separated from live operational transaction data
- tenant, branch, and role boundaries must be enforced server-side
- sensitive business and configuration actions must support maker-checker approval
- do not make fake compliance claims; build technical controls and operational patterns appropriate for a Bank of Ghana regulated institution

## Build these platform domains
- BankingOS Foundation
- Core Banking Services
- Process Runtime
- Product Factory
- Config Studio
- Theme Studio
- Operations Control
- Reporting and Regulatory
- Security and Governance

## Shared process lifecycle
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

Each process stage must define:
- stage code
- display name
- actor role
- entry criteria
- required form or review schema
- validation rules
- allowed actions
- decision outcomes
- downstream events
- audit payload
- SLA target
- escalation route

## Seeded process flows to implement
- Customer Onboarding
- Account Opening
- Savings Cash Deposit
- Savings Cash Withdrawal
- Loan Origination
- Loan Disbursement
- Repayment Collection
- Arrears Management
- Fixed Deposit Lifecycle
- Teller Funding and Vault Transfer
- End-of-Day Operations

## Key deliverables
- BankingOS product identity
- backend contracts for forms, themes, products, queues, screens, and processes
- frontend runtime contracts for metadata-driven rendering
- seeded process pack
- seeded form pack
- seeded theme pack
- documentation and tests

## Delivery order
1. Create BankingOS identity and workspace structure.
2. Formalize metadata schemas for forms, themes, products, processes, queues, screens, and approvals.
3. Extend backend process runtime to return stage-aware UI contracts.
4. Extend frontend task inbox and runtime screens to consume those contracts.
5. Upgrade current JSON form renderer into a production runtime form engine.
6. Upgrade current form tooling into a low-code designer with versioning and publishing.
7. Add theme definitions, token runtime, theme designer, and sample themes.
8. Seed prebuilt banking process packs, products, forms, and queues.
9. Add governance controls, maker-checker, audit/version/publish workflows, and rollback.
10. Add tests and concise admin/developer documentation.
