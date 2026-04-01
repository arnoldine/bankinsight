# BankInsight Browser Signoff Checklist

## Purpose

Use this checklist to complete the final manual browser validation for the React BankInsight application before release signoff. This checklist assumes the API is running, seeded data is available, and the operator has a role with broad access such as admin or super-admin.

## Environment

- Frontend URL: `http://localhost:3001`
- API URL: `http://localhost:5176`
- Recommended browsers:
  - Chrome
  - Brave
  - Edge
- Test with at least one admin user and one restricted user if available.

## Sign-in And Session

- Open `/login`.
- Confirm the page loads cleanly with no broken layout or console error.
- Sign in with a valid user.
- Complete MFA successfully.
- Confirm the workspace shell loads after login.
- Refresh the page and confirm the session is restored.
- Open a protected route directly in the address bar and confirm it still resolves correctly when authenticated.
- Sign out and confirm protected routes return to login.
- If possible, test session expiry and confirm the app shows a clear reauthentication path.

## Navigation And Shell

- Confirm the main workspace loads with no missing panels.
- Confirm the navigation menu shows expected modules for the user role.
- Use workspace search and verify results navigate correctly.
- Open dashboard, clients, accounts, loans, settings, and BankingOS from the left rail.
- Resize the browser and confirm navigation remains usable on smaller widths.

## Clients And KYC

- Create a new client with normal onboarding fields.
- Upload a profile photo.
- Upload a signature image.
- Upload ID front and ID back.
- Confirm media previews appear after upload.
- Verify and reject media items and confirm status badges update correctly.
- Confirm KYC readiness displays the correct missing or completed items.
- Confirm the client profile loads again after refresh with the same media and statuses.

## Accounts

- Open a new account for a KYC-ready client.
- Confirm non-ready clients are blocked with a clear explanation.
- Verify the account appears in the client and account workspaces.
- Search for the account by account number and client name.

## Teller

- Load an account using account lookup.
- Post a standard cash deposit.
- Post a standard withdrawal if data and permissions allow.
- Lodge a same-bank cheque.
- Lodge an other-bank cheque and confirm the hold or clearing date is shown.
- Attempt a cheque withdrawal and confirm issued cheque suggestions appear for the selected account.
- Confirm success and failure messages are clear.

## Transactions And Payments

- Open the transaction explorer.
- Create a bulk payment batch with at least one valid line.
- Confirm account suggestions appear while entering batch lines.
- Submit the batch and confirm the result status is visible.
- Open batch detail and confirm per-line outcomes are shown.
- View the cheque clearing queue.
- Return a cheque from the queue and confirm status updates correctly.
- Issue a cheque book using guided account selection.
- Confirm cheque-book inventory, leaves, and statuses render correctly.

## Loans

- Open the loan management workspace.
- Start a new application using guided customer selection.
- Confirm KYC gating is visible for non-ready customers.
- Run credit check where available.
- Preview repayment schedule.
- Submit a loan application.
- Review an existing loan using the servicing and operations views.
- Test guided account selection for servicing and collateral accounts.
- Post a repayment with a guided settlement account.
- Verify penalty assessment and loan classification actions render and respond cleanly.

## Approvals

- Open the approvals queue.
- Confirm items load with filters and statuses.
- Approve one item if data and permissions allow.
- Reject one item and confirm rejection feedback is captured and displayed.

## Settings And Admin

- Open Settings.
- Confirm users, roles, branches, config, and ORASS areas load.
- Create or edit a process definition using the guided code, module, entity, and event suggestions.
- Confirm ORASS setup shows guided institution-code and report-code suggestions.
- If allowed, test a simple user or branch update and confirm it persists.

## BankingOS

- Open BankingOS Control Center.
- Confirm process catalog, form catalog, theme catalog, and runtime data load.
- Create or edit a process stage and confirm actor-role suggestions appear.
- Bind a stage to a form and confirm live form code suggestions appear.
- If runtime tasks are available, claim and complete a task.

## Treasury, Vault, And Reporting

- Open treasury and confirm summary data loads.
- Open vault and confirm teller till or vault summaries load.
- Open reporting and run at least one report.
- Confirm export, run history, or refresh flows behave as expected.

## Role And Permission Validation

- Sign in as a restricted user.
- Confirm inaccessible modules are hidden from navigation.
- Attempt to open a restricted route directly and confirm access is denied cleanly.

## Browser Validation

- Repeat a short smoke pass in Edge:
  - login
  - dashboard
  - clients
  - loans
  - settings
- Confirm no stale asset or CSS cache issues remain after hard refresh.

## Release Decision

Mark the release ready when all of the following are true:

- No blocking console errors appear in normal use.
- All high-risk flows above complete successfully.
- Permission behavior matches expectations.
- Error states are readable and non-destructive.
- Session expiry and reauthentication behavior are clear.
- The app behaves consistently in at least two browsers.
