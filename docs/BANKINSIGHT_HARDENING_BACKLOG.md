# BankInsight Hardening Backlog

## Objective
Move BankInsight from broad functional coverage to a controlled, production-ready platform suitable for regulated financial operations, with emphasis on security, financial correctness, auditability, workflow completion, and release confidence.

## Current Readiness Summary
- Core backend platform: Implemented
- Domain breadth: Strong
- Frontend parity: Partial
- Operational controls: Partial
- Automated regression safety: Partial

## Workstreams

### 1. Platform and Environment Hardening
Priority: Critical
- Standardize environment profiles for Development, Testing, Staging, and Production.
- Centralize configuration validation at startup.
- Enforce secrets hygiene for JWT, database, SMTP, and provider credentials.
- Add health checks for database, queues, background jobs, and external providers.
- Add structured logging, trace correlation, and request diagnostics.
- Review rate limiting, timeout policies, retry policies, and startup behavior.

Exit criteria:
- Every environment boots with validated config.
- Core dependencies surface health status.
- Logs are structured and traceable across requests and jobs.

### 2. Security, RBAC, and Maker-Checker Closure
Priority: Critical
- Audit permissions by module and align UI visibility with actual backend authorization.
- Enforce maker-checker on approvals, disbursements, reversals, reschedules, write-off recommendations, product changes, and group closure.
- Standardize audit payloads with user, branch, channel, device/IP, before value, and after value.
- Add negative tests for unauthorized access and approval bypass attempts.

Exit criteria:
- Sensitive operations cannot execute without the required role and approval flow.
- Audit history is consistent and reviewable across all sensitive actions.

### 3. Financial Correctness and Reconciliation
Priority: Critical
- Validate loan schedule totals, accruals, repayment allocation order, reversals, and rounding behavior.
- Reconcile GL posting outputs for disbursement, repayment, penalties, fees, savings, treasury trades, and write-off flows.
- Add automated controls for out-of-balance collection batches, journal mismatches, and orphaned posting references.
- Add exception dashboards for failed postings and reconciliation gaps.

Exit criteria:
- Every financial workflow has deterministic posting and reconciliation rules.
- Reporting totals reconcile to source transactions and GL balances.

### 4. Workflow Completion Audit
Priority: High
- Audit each module for backend readiness, frontend coverage, permissions, and operational completion.
- Close gaps where APIs exist but UI actions are missing or placeholder-only.
- Standardize status transitions, error handling, user messaging, and task routing.
- Confirm group lending, loans, approvals, treasury, reporting, and customer servicing are complete end to end.

Exit criteria:
- Each major workflow is executable from UI to persistence with clear operator feedback.

### 5. Test Expansion and Release Gates
Priority: High
- Expand integration tests across customers, accounts, loans, group lending, treasury, reporting, and security.
- Add service-level tests for cycle eligibility, PAR transitions, GL allocation, and approval state changes.
- Add frontend tests for shell navigation, critical forms, and workflow handoffs.
- Add CI gates for backend build, frontend build, and automated test runs.

Exit criteria:
- Release candidates are blocked when critical financial, security, or workflow regressions occur.

### 6. Operational Readiness and Supportability
Priority: High
- Create runbooks for EOD, failed posting recovery, reversals, reconciliation breaks, and approval exceptions.
- Add admin support tools for reprocessing safe jobs, inspecting workflow state, and tracing customer activity.
- Prepare seeded demo/reference data for UAT and training.
- Define production exit checklist and smoke test checklist.

Exit criteria:
- Operations and support teams can diagnose and recover common issues without code intervention.

## Delivery Phases

### Phase 1: Control Baseline
- Platform/config hardening
- RBAC review
- Maker-checker enforcement audit
- Audit payload standardization
- Financial invariant checks

### Phase 2: Regression Safety
- Expand backend tests
- Add permission tests
- Add reconciliation tests
- Add focused frontend interaction tests

### Phase 3: Workflow Completion
- Close UI/API gaps across approvals, loans, treasury, reporting, and group lending
- Standardize validation, status messaging, and empty/error states

### Phase 4: Production Exit Review
- UAT support
- Reconciliation signoff
- Operations runbooks
- Release checklist and deployment readiness review

## First Recommended Sprint
- Complete a repo-wide hardening audit matrix by module.
- Close maker-checker gaps for all sensitive actions.
- Add financial reconciliation assertions for loan and group-lending postings.
- Expand test coverage for approvals, reversals, and delinquency transitions.
- Finalize the new shell/navigation UX and extend the same standard to approvals, loans, treasury, and reporting.
