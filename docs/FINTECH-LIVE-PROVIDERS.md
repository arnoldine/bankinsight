# Fintech Live Providers

`BankInsight.API` now hosts the fintech transfer backend and exposes the fintech routes under the shared API process.

## Operating Modes

Each fintech rail supports two deployment modes:
- `Mock`: deterministic local and test-safe behavior with no partner dependency.
- `Live`: outbound HTTP calls to the configured partner adapter.

Keep `Mock` as the default for local development, CI, and demos. Move a rail to `Live` only after partner onboarding, compliance review, and treasury sign-off are complete.

## Configuration Sources

The shared host reads fintech provider settings from:
- `BankInsight.API/appsettings.json`
- `BankInsight.API/appsettings.Development.json`
- environment variables using ASP.NET Core double-underscore binding

Relevant sections:
- `Persistence:Provider`
- `FintechProviders:MobileMoney:*`
- `FintechProviders:BankTransfer:*`
- `FintechProviders:CryptoCustody:*`
- `FintechProviders:Webhook:*`

Environment variable examples are included in `.env.example`.

## Paystack Sandbox Trial

The bank-transfer rail is now wired to support Paystack test mode through the shared host.

Based on Paystack's official API docs:
- resolve the beneficiary using `/bank/resolve`
- create a transfer recipient using `/transferrecipient`
- initiate the payout using `/transfer`
- send the secret key as `Authorization: Bearer <key>`
- send GHS amounts in pesewas

This behavior is activated when:
- `FintechProviders:BankTransfer:Mode=Live`
- `FintechProviders:BankTransfer:ProviderCode=paystack-bank-gh`

## Provider Contract Shape

The Bankinsight shared host now uses explicit request and response contracts for live provider calls instead of anonymous payloads:
- `MobileMoneyPayoutRequest`
- `BankTransferPayoutRequest`
- `PaystackResolveAccountEnvelope`
- `PaystackTransferRecipientRequest`
- `PaystackTransferRequest`
- `CryptoDepositAddressRequest`
- `CryptoWithdrawalBroadcastRequest`

This is the seam where partner-specific adapters, HMAC signing, beneficiary validation, and provider status polling should evolve next.

## Suggested Rollout

1. Keep all rails in `Mock` for local development and CI.
2. Enable one sandbox rail at a time by changing its `Mode` to `Live`.
3. For Paystack sandbox bank payouts, use your Paystack test secret key and `ProviderCode=paystack-bank-gh`.
4. Validate beneficiary resolution, recipient creation, payout submission, callbacks, reconciliation, and audit trails in non-production.
5. Promote to production only after compliance, cyber, operations, treasury, and partner approval.

## Smoke Test Script

For a local provider-connectivity check through `BankInsight.API`, use:
- `scripts/run-bankinsight-paystack-sandbox.ps1`
- `scripts/invoke-paystack-sandbox-bank-payout.ps1`

The sample payout script uses the Paystack official test transfer account details currently documented for Nigerian merchants:
- account number `0000000000`
- bank code `057` (Zenith Bank transfer test)
- currency `NGN`

That smoke path validates the shared Bankinsight adapter and request wiring. It is not a substitute for Ghana-specific payout certification, beneficiary validation, settlement, or Bank of Ghana compliance sign-off.
