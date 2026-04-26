# BankInsight Client Channel Scaffold

This is the original web-only scaffold. The active client direction is now the React Native multi-platform app in [apps/client-mobile](C:\Backup old\dev\bankinsight\apps\client-mobile\README.md).

This folder contains a standalone scaffold for the customer-facing BankInsight channel.

## Purpose

The scaffold is intentionally isolated from the existing admin and operations frontend so the customer experience can evolve with:

- MFA-first authentication
- complaint and recourse workflows
- secure customer messaging
- session and device visibility
- BoG-aligned security and audit controls

## Run locally

From this folder:

```powershell
npm install
npm run dev
```

Default dev port: `5177`

## Next implementation steps

1. Replace placeholder page data with authenticated API calls.
2. Introduce route-level auth and step-up challenges for protected screens.
3. Connect complaint, statements, profile, and security pages to real BankInsight services.
4. Add append-only audit emission for every critical customer action.
5. Split the single-screen scaffold into routed modules and feature folders once the API contracts are fixed.

## Suggested backend dependencies

- Auth service
- Customer profile service
- Account read service
- Statements service
- Complaints service
- Secure messaging service
- Consent/privacy service
- Audit service
