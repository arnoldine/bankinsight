# CoreBanker Browser Signoff Checklist

## Purpose

Use this checklist to complete the final manual browser validation for the CoreBanker Blazor application before release signoff. This checklist complements [PRODUCTION-READINESS-CHECKLIST.md](C:\Backup old\dev\bankinsight\CoreBankerWeb\CoreBanker\PRODUCTION-READINESS-CHECKLIST.md) and focuses on real operator flows in the running UI.

## Environment

- Frontend URL: `http://localhost:3003`
- API URL: `http://localhost:5176`
- Recommended browsers:
  - Chrome
  - Brave
  - Edge
- Use at least one broad-access admin user and one restricted user if available.

## Sign-In And Session

- Open `/login`.
- Confirm the login page renders correctly with no broken layout.
- Sign in with valid credentials.
- Complete MFA successfully.
- Confirm the main workspace loads after login.
- Refresh the page and confirm the session restores correctly.
- Open a protected route directly in the address bar and confirm it resolves correctly while authenticated.
- Sign out and confirm the app returns to login.
- If possible, confirm expired-session handling shows a clear path back to sign-in.

## Navigation And Shell

- Confirm the main shell loads without console-breaking errors.
- Confirm the left navigation shows the expected modules for the user role.
- Use workspace search and quick-access navigation if available.
- Open dashboard, clients, accounts, loans, settings, and BankingOS from the left rail.
- Verify page headers, icons, and layout consistency on desktop and smaller laptop widths.

## Clients And KYC

- Create a new client from onboarding.
- Upload profile photo, signature, ID front, and ID back.
- Confirm previews render correctly after upload.
- Verify or reject media items and confirm status changes persist.
- Confirm KYC readiness shows the correct state and missing items.
- Reload the client workspace and confirm the same profile and media data are shown.

## Accounts

- Open a new account for a KYC-ready client.
- Confirm non-ready clients are blocked with a clear message.
- Confirm the new account appears in account listings and client views.
- Search by account number and client name.

## Teller And Payments

- Load an account from the teller screen.
- Post a cash deposit.
- Post a withdrawal if permissions and data allow.
- Lodge a same-bank cheque.
- Lodge an other-bank cheque and confirm hold or clearing information is visible.
- Attempt a cheque withdrawal and confirm only issued cheque leaves are available for selection.
- Confirm operator feedback is clear for both success and failure.

## Transactions, Bulk Payments, And Cheque Books

- Open the transactions workspace.
- Create a bulk payment batch with at least one valid line.
- Confirm batch detail shows per-line outcomes.
- Open the cheque clearing queue and inspect lodged items.
- Return a cheque from the queue and confirm status updates correctly.
- Record cheque-book stock.
- Issue a cheque book to an account.
- Confirm leaf-level status, used leaves, and cancelled leaves are visible.
- Cancel an unused leaf and confirm teller-side availability updates.

## Loans And Approvals

- Open the loan management workspace.
- Originate a loan using guided client selection.
- Confirm KYC gating appears for non-ready clients.
- Run credit check if configured.
- Preview schedule and submit application.
- Review or approve an application where allowed.
- Disburse an approved loan.
- Post a repayment and confirm schedule context updates.
- Run penalty and classification actions if available.
- Open approvals and complete at least one approve or reject flow.

## Group Lending

- Create a group and center if the environment allows.
- Add members and create a group application.
- Record a meeting or attendance action.
- Confirm collections, schedule, and statement views load correctly.

## Treasury, Vault, Risk, And Accounting

- Open treasury and confirm summaries and operational tabs load.
- Create or inspect an FX trade and investment record where data allows.
- Open vault and confirm till or vault data loads.
- Open operations risk and confirm calculations and summary data load.
- Open accounting and confirm journals and chart data load.
- Run statements and confirm trial balance, income statement, and cash flow views render correctly.

## End Of Day, Reporting, Security, And BankingOS

- Open End of Day and confirm status and logs load.
- Confirm scheduled-step visibility for loan collection, cheque clearing, and other jobs.
- Open reporting and run at least one report.
- Open security operations and confirm alerts, sessions, and investigations load.
- Open BankingOS and confirm catalogs, bundles, tasks, and product-rule areas load.
- If runtime tasks exist, claim and complete one.

## Settings And Audit

- Open Settings and confirm users, roles, branches, and config data load.
- Create or edit a simple admin record if allowed.
- Confirm privilege lease and ORASS-related areas load if enabled.
- Open audit and confirm filters, summaries, and detail panes render correctly.

## Role And Permission Validation

- Sign in as a restricted user.
- Confirm restricted modules are hidden from navigation.
- Attempt to open a restricted route directly and confirm access is denied cleanly.

## Browser Validation

- Repeat a short smoke pass in Edge:
  - login
  - dashboard
  - clients
  - loans
  - settings
- Confirm there are no stale CSS or asset-cache issues after hard refresh.

## Release Decision

Mark the release ready when all of the following are true:

- No blocking console or renderer errors occur in normal use.
- High-risk flows above complete successfully.
- Permission behavior is correct for broad and restricted users.
- Error states are readable and recoverable.
- Session expiry and reauthentication behave clearly.
- The app behaves consistently in at least two browsers.
