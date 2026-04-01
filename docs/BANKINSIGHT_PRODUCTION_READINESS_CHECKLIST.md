# BankInsight Production Readiness Checklist

## Purpose

Use this checklist to determine whether the React BankInsight application is ready for production rollout. This document complements the browser validation in [BANKINSIGHT_BROWSER_SIGNOFF_CHECKLIST.md](C:\Backup old\dev\bankinsight\docs\BANKINSIGHT_BROWSER_SIGNOFF_CHECKLIST.md) and focuses on release controls, operational readiness, and deployment confidence.

## Scope

This checklist applies to:

- React BankInsight frontend
- BankInsight API dependencies required by the frontend
- authentication and authorization behavior visible from the frontend
- production configuration required for live operations

## Release Gate

The release is ready only when all of the following are true:

- The latest production candidate builds successfully.
- The browser signoff checklist is completed.
- No blocking defects remain in high-risk workflows.
- Production secrets and external integration values are configured.
- Role-based access is verified for privileged and restricted users.
- Monitoring and rollback instructions are available to operators.

## Build And Packaging

- `npm run build` passes on the release candidate.
- The generated frontend bundle is deployed from the same commit approved for release.
- Static assets resolve correctly in the target environment.
- No stale cache issue remains after deployment and hard refresh.

## Authentication And Session

- Login succeeds for production-capable user accounts.
- MFA succeeds for required user categories.
- Logout works cleanly.
- Session restore works after refresh.
- Session expiry leads to a clear reauthentication path.
- Direct URL access respects authentication state.

## Authorization

- Admin or super-admin users can access the expected modules.
- Restricted users only see allowed modules in navigation.
- Restricted users are blocked from privileged routes through direct navigation.
- Approval, settings, audit, and BankingOS actions respect backend permissions.

## High-Risk Workflow Coverage

### Customers And KYC

- Customer onboarding works end to end.
- Profile photo, signature, and ID card uploads work.
- Media verification and rejection actions work.
- KYC readiness updates correctly.

### Accounts

- Account opening works for KYC-ready customers.
- Non-ready customers are blocked with a clear reason.
- New accounts become visible in the relevant workspaces.

### Teller And Payments

- Cash deposit and withdrawal workflows work.
- Same-bank cheque deposit works.
- Other-bank cheque deposit works with the expected hold or clearing behavior.
- Bulk payment batch creation and monitoring work.
- Cheque-book issue and inventory views work.

### Loans

- Loan origination works with guided customer selection.
- Loan KYC gating works.
- Credit check and schedule preview work where configured.
- Disbursement, repayment, penalty, and classification actions work.

### Administration And Configuration

- Settings loads all required admin data.
- Roles, branches, and core config areas load correctly.
- ORASS setup fields save successfully if enabled.
- BankingOS catalogs and runtime task flows work.

### Operational Finance

- Treasury loads summary and operational data.
- Vault loads till or vault summaries.
- Reporting runs successfully.
- Dashboard and cross-module summaries load without blocking errors.

## Production Configuration

- `Clerk:WebhookSecret` is configured in the production API environment.
- `BankOfGhana:FxRatesUrl` is configured in the production API environment.
- Any production API base URL used by the frontend is correct.
- Environment-specific secrets are not hardcoded in the frontend bundle.

## Error Handling And UX

- Empty states are readable and actionable.
- Permission-denied states are clear.
- Validation errors are visible and specific.
- Long-running actions provide enough feedback.
- Failures do not leave the UI in a broken or unrecoverable state.

## Monitoring And Support

- Operators know where to inspect frontend deployment status.
- Operators know where to inspect API logs if the frontend surfaces errors.
- Support staff have the current user manuals and role guides.
- A rollback path is documented for the deployed frontend image or build.

## Browser Coverage

- A primary signoff pass is completed in Chrome or Brave.
- A compatibility pass is completed in Edge.
- No browser-specific login, layout, or asset-cache issue remains unresolved.

## Evidence To Capture

- Release commit SHA
- Build success output
- Browser signoff date
- Test users and roles used during validation
- Known non-blocking issues, if any
- Deployer name and deployment timestamp

## Final Signoff

Record the following before promoting the release:

- Product or project name
- Release commit SHA
- Environment
- Signoff date
- Approved by
- Notes or exceptions
