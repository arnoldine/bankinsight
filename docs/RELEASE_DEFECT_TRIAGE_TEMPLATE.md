# Release Defect Triage Template

## Purpose

Use this template when a defect is found during BankInsight or CoreBanker signoff. The goal is to make triage fast, consistent, and actionable across both products.

## Defect Record

- ID:
- Product:
  - `BankInsight React`
  - `CoreBanker`
  - `Shared API`
- Environment:
- Date found:
- Found by:
- Status:
  - `Open`
  - `In progress`
  - `Blocked`
  - `Fixed`
  - `Retest required`
  - `Closed`

## Severity

- `P0` Critical: blocks release or causes data loss, security failure, or unusable core workflow
- `P1` High: major workflow broken, but release may continue only with explicit approval
- `P2` Medium: workflow impaired, workaround exists
- `P3` Low: cosmetic, content, or minor usability issue

## Area

- Auth and session
- Navigation and shell
- Clients and KYC
- Accounts
- Teller and payments
- Cheques and cheque books
- Loans and approvals
- Group lending
- Treasury and vault
- Accounting and EOD
- Settings and audit
- BankingOS and workflow
- Reporting and security
- Deployment or environment

## Summary

- Short title:
- Affected workflow:
- User role:

## Reproduction

1. 
2. 
3. 

## Expected Result

-

## Actual Result

-

## Evidence

- URL:
- Screenshot or recording:
- Console output:
- API response:
- Related log lines:

## Technical Notes

- Suspected files:
- Suspected service or endpoint:
- Temporary workaround:

## Decision

- Release blocking: `Yes/No`
- Owner:
- Target fix date:
- Retest owner:
- Notes:
