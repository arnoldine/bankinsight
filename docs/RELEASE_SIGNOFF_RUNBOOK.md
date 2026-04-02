# Release Signoff Runbook

## Purpose

This runbook explains how to execute release signoff for both `BankInsight React` and `CoreBanker` using the signoff artifacts already stored in the repository.

Use this document as the master sequence for final validation before release approval.

## Products Covered

- `BankInsight React`
- `CoreBanker`

## Signoff Artifacts

### BankInsight React

- Browser checklist:
  - [BANKINSIGHT_BROWSER_SIGNOFF_CHECKLIST.md](C:\Backup old\dev\bankinsight\docs\BANKINSIGHT_BROWSER_SIGNOFF_CHECKLIST.md)
- Production readiness checklist:
  - [BANKINSIGHT_PRODUCTION_READINESS_CHECKLIST.md](C:\Backup old\dev\bankinsight\docs\BANKINSIGHT_PRODUCTION_READINESS_CHECKLIST.md)
- Execution log:
  - [BANKINSIGHT_SIGNOFF_EXECUTION_LOG.md](C:\Backup old\dev\bankinsight\docs\BANKINSIGHT_SIGNOFF_EXECUTION_LOG.md)

### CoreBanker

- Browser checklist:
  - [BROWSER_SIGNOFF_CHECKLIST.md](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\BROWSER_SIGNOFF_CHECKLIST.md)
- Production readiness checklist:
  - [PRODUCTION-READINESS-CHECKLIST.md](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\PRODUCTION-READINESS-CHECKLIST.md)
- Execution log:
  - [SIGNOFF_EXECUTION_LOG.md](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\SIGNOFF_EXECUTION_LOG.md)

### Shared

- Defect triage template:
  - [RELEASE_DEFECT_TRIAGE_TEMPLATE.md](C:\Backup old\dev\bankinsight\docs\RELEASE_DEFECT_TRIAGE_TEMPLATE.md)

## Recommended Signoff Order

1. Confirm environment availability.
2. Confirm route reachability and API health.
3. Execute the browser checklist for BankInsight React.
4. Record results in the BankInsight execution log.
5. Triage and fix any defects.
6. Re-test failed BankInsight items.
7. Execute the browser checklist for CoreBanker.
8. Record results in the CoreBanker execution log.
9. Triage and fix any defects.
10. Re-test failed CoreBanker items.
11. Review both production-readiness checklists.
12. Capture final evidence and approver names.
13. Mark final signoff outcome.

## Execution Rules

- Use real test users where possible.
- Use at least one admin user and one restricted user.
- Record every failure in the execution log.
- For any non-trivial issue, create a proper defect record using the shared triage template.
- Do not mark release `Ready` if any `P0` or `P1` defect remains open.
- Re-test every fixed issue before closing it.

## Suggested Tester Roles

- `Admin or Super Admin`
  - validates full-access operational paths
- `Restricted Operational User`
  - validates permission filtering and access denial behavior
- `Finance or Operations User`
  - validates teller, loans, treasury, accounting, or EOD flows depending on role setup

## Minimal Evidence Required

- Frontend URL used
- API environment used
- Signoff date
- Test users or roles used
- Build verification result
- Route and API sweep result
- Completed browser checklist status
- Defect list with status
- Final approver name

## Release Decision Rules

- `Ready`
  - all critical flows passed
  - no release-blocking defects remain
  - production configuration is confirmed
- `Conditionally Ready`
  - only low-risk known issues remain with explicit approval
- `Not Ready`
  - critical flows failed
  - production configuration is incomplete
  - unresolved security, auth, or financial-operation defects remain

## Suggested Final Output

At the end of signoff, record:

- Product
- Commit SHA
- Environment
- Signoff result
- Open defects
- Approval date
- Approved by

## Current Technical Baseline

The following have already been validated from the code and HTTP side:

- Local frontend route reachability for both products
- API health
- MFA-authenticated protected API sweep across major modules

What still remains is the manual browser walkthrough and final approval recording.
