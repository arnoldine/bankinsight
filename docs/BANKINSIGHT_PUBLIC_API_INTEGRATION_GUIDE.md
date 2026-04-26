# BankInsight Public API Reference

Last updated: 2026-04-26  
Repository root: [C:\Backup old\dev\bankinsight](C:\Backup old\dev\bankinsight)  
API project: [C:\Backup old\dev\bankinsight\BankInsight.API](C:\Backup old\dev\bankinsight\BankInsight.API)

## Purpose

This document is the external integration reference for the BankInsight API. It is written as an actual API documentation set rather than a platform overview.

It is based on the live controller and DTO surface in:

- [Controllers](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers)
- [DTOs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs)
- [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs)

This guide focuses on the public and integration-facing APIs that external systems, digital channels, middleware, and partner applications are most likely to consume.

## Base URLs

Local development:

- `http://localhost:5176`

Typical deployed base URL:

- [https://bankinsight.rproxyserv.net](https://bankinsight.rproxyserv.net)

Examples in this document are shown relative to the API base URL.

## Authentication

## Staff JWT

Used for staff, partner middleware acting as staff/system users, and operational frontends.

Main routes:

- `POST /api/auth/login`
- `POST /api/auth/mfa/verify`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

Bearer format:

```http
Authorization: Bearer <staff-jwt>
```

DTO source:

- [AuthDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\AuthDTOs.cs)

## Client / Customer JWT

Used for customer self-service and digital channel APIs.

Main routes:

- `POST /api/client-auth/login`
- `POST /api/client-auth/register`
- `POST /api/client-auth/mfa/verify`
- `POST /api/client-auth/refresh`
- `GET /api/client-auth/me`

Bearer format:

```http
Authorization: Bearer <client-jwt>
```

DTO source:

- [ClientChannelDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\ClientChannelDTOs.cs)

## Common Conventions

## Content type

Most APIs use:

```http
Content-Type: application/json
Accept: application/json
```

## Common response behavior

Typical status codes:

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `500 Internal Server Error`

Most POST routes return JSON objects rather than empty responses.

## Pagination

Large list endpoints may return a paged wrapper from:

- [PagedResultDto.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\PagedResultDto.cs)

Common query parameters:

- `pageNumber`
- `pageSize`
- `search`
- domain-specific filters like `type`

## Idempotency and references

Where supported, send a stable client reference.

Common fields:

- `ClientReference`
- `clientReference`
- `Reference`

This is especially useful for:

- transactions
- loan disbursement and repayment
- digital lending
- bulk payments

## Health API

### `GET /health`

Purpose:

- service reachability
- smoke tests
- load balancer health checks

Response example:

```json
{
  "status": "ok",
  "service": "bankinsight-api",
  "environment": "Development",
  "timestampUtc": "2026-04-26T14:27:05.5983009Z"
}
```

## Staff Authentication API

Controller:

- [AuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AuthController.cs)

DTOs:

- [AuthDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\AuthDTOs.cs)

### `POST /api/auth/login`

Description:

- Starts a staff login session.
- May return either a full token response or an MFA challenge.

Request body:

```json
{
  "email": "admin@bankinsight.local",
  "password": "P@ssw0rd!"
}
```

Request fields:

- `email`: staff login email
- `password`: staff password

Response fields:

- `user`: user profile object
- `token`: JWT when MFA is not pending
- `refreshToken`: refresh token when login is completed
- `mfaRequired`: indicates whether step two is required
- `mfaToken`: challenge token for MFA verification
- `deliveryChannel`: OTP delivery channel
- `deliveryHint`: redacted destination hint
- `deliveryStatus`: delivery outcome
- `deliveryMessage`: delivery message
- `mfaExpiresAtUtc`: challenge expiry
- `allowedFactors`: supported MFA factors
- `debugCode`: development helper, not for production reliance

Response example when MFA is required:

```json
{
  "mfaRequired": true,
  "mfaToken": "mfa_tok_123",
  "deliveryChannel": "email",
  "deliveryHint": "a***@bankinsight.local",
  "allowedFactors": ["otp"]
}
```

### `POST /api/auth/mfa/verify`

Request body:

```json
{
  "mfaToken": "mfa_tok_123",
  "code": "123456"
}
```

Response:

- same token-bearing shape as completed login

### `POST /api/auth/mfa/resend`

Request body:

```json
{
  "mfaToken": "mfa_tok_123"
}
```

### `GET /api/auth/me`

Auth:

- staff bearer token required

Response highlights:

- authenticated user identity
- roles/permissions from claims

### `POST /api/auth/refresh`

Request body:

```json
{
  "refreshToken": "refresh_token_value"
}
```

### `POST /api/auth/logout`

Auth:

- staff bearer token required

Behavior:

- invalidates the active token/session context

## Client Authentication API

Controller:

- [ClientAuthController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientAuthController.cs)

DTOs:

- [ClientChannelDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\ClientChannelDTOs.cs)

### `POST /api/client-auth/login`

Request body:

```json
{
  "email": "customer@example.com",
  "password": "P@ssw0rd!"
}
```

Response highlights:

- `user`
- `token`
- `refreshToken`
- `mfaRequired`
- `mfaToken`
- `allowedFactors`

### `POST /api/client-auth/register`

Request body:

```json
{
  "name": "Ama Mensah",
  "email": "ama@example.com",
  "phone": "+233201234567",
  "digitalAddress": "GA-123-4567",
  "ghanaCard": "GHA-123456789-0",
  "password": "P@ssw0rd!"
}
```

### `POST /api/client-auth/register/verify`

Request body:

```json
{
  "registrationToken": "reg_tok_123",
  "code": "123456"
}
```

### `POST /api/client-auth/mfa/verify`

Request body:

```json
{
  "mfaToken": "mfa_tok_123",
  "code": "123456"
}
```

### `POST /api/client-auth/password/forgot`

Request body:

```json
{
  "email": "ama@example.com"
}
```

### `POST /api/client-auth/password/reset`

Request body:

```json
{
  "resetToken": "reset_tok_123",
  "code": "123456",
  "newPassword": "NewP@ssw0rd!"
}
```

### `POST /api/client-auth/step-up/initiate`

Auth:

- client bearer token required

Request body:

```json
{
  "purpose": "merchant_payment",
  "factor": "otp"
}
```

Response fields:

- `challengeRequired`
- `challengeToken`
- `deliveryChannel`
- `deliveryHint`
- `deliveryStatus`
- `deliveryMessage`
- `expiresAtUtc`
- `factor`
- `allowedFactors`

### `POST /api/client-auth/step-up/verify`

Request body:

```json
{
  "challengeToken": "challenge_123",
  "code": "123456"
}
```

Response fields:

- `stepUpToken`
- `purpose`
- `expiresAtUtc`
- `factor`

### `POST /api/client-auth/transaction-pin`

Request body:

```json
{
  "password": "P@ssw0rd!",
  "pin": "1234"
}
```

## Customer / Client Channel API

Controller:

- [ClientChannelController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientChannelController.cs)

DTOs:

- [ClientChannelDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\ClientChannelDTOs.cs)

Auth:

- client bearer token required

## Bootstrap and profile

### `GET /api/client-channel/bootstrap`

Returns:

- `identity`
- `linkedCustomer`
- `warnings`

### `GET /api/client-channel/profile`

Returns linked customer profile.

### `GET /api/client-channel/kyc`

Returns:

- `customerId`
- `kycLevel`
- `readiness`
- `cases`

### `PUT /api/client-channel/profile`

Request body:

```json
{
  "name": "Ama Mensah",
  "email": "ama@example.com",
  "phone": "+233201234567",
  "digitalAddress": "GA-123-4567",
  "stepUpToken": "stepup_123"
}
```

### `POST /api/client-channel/profile/media`

Request body:

```json
{
  "mediaType": "PROFILE_PHOTO",
  "mediaSide": null,
  "fileName": "photo.png",
  "contentType": "image/png",
  "dataUrl": "data:image/png;base64,...",
  "stepUpToken": "stepup_123"
}
```

### `POST /api/client-channel/kyc/refresh`

Request body:

```json
{
  "reason": "Address update",
  "summary": "Customer updated address and wants profile refreshed",
  "stepUpToken": "stepup_123"
}
```

## Accounts and customer overview

### `GET /api/client-channel/accounts`

Response item shape:

- `id`
- `type`
- `currency`
- `balance`
- `lienAmount`
- `status`
- `productCode`
- `lastTransDate`

### `GET /api/client-channel/banking/overview`

Response fields:

- `totalVisibleBalance`
- `activeAccountCount`
- `activeStandingOrderCount`
- `activeLoanCount`
- `activeInvestmentCount`
- `totalLoanExposure`
- `totalInvestmentBalance`

## Merchant and payment flows

### `GET /api/client-channel/banking/merchants`

Returns merchant catalog entries.

### `GET /api/client-channel/banking/merchant-acceptance/eligibility`

Returns:

- `canEnroll`
- `customerId`
- `customerType`
- `businessName`
- `reason`
- `eligibleSettlementAccounts`

### `GET /api/client-channel/banking/merchant-acceptance/profiles`

Returns merchant acceptance profiles.

### `POST /api/client-channel/banking/merchant-acceptance/profiles`

Request body:

```json
{
  "settlementAccountId": "ACC-001",
  "displayName": "Ama Shop",
  "category": "Retail",
  "stepUpToken": "stepup_123"
}
```

### `POST /api/client-channel/banking/transfers/internal`

Request body:

```json
{
  "fromAccountId": "ACC-001",
  "toAccountId": "ACC-002",
  "amount": 250.00,
  "narration": "Own transfer",
  "stepUpToken": "stepup_123"
}
```

### `POST /api/client-channel/banking/payments/merchants`

Request body:

```json
{
  "merchantCode": "MRC-001",
  "sourceAccountId": "ACC-001",
  "amount": 75.00,
  "narration": "Store payment",
  "stepUpToken": "stepup_123"
}
```

### `POST /api/client-channel/banking/payments/qr/resolve`

Request body:

```json
{
  "qrPayload": "BANKINSIGHTQR:..."
}
```

### `POST /api/client-channel/banking/payments/qr`

Request body:

```json
{
  "qrPayload": "BANKINSIGHTQR:...",
  "sourceAccountId": "ACC-001",
  "amount": 75.00,
  "narration": "QR payment",
  "stepUpToken": "stepup_123"
}
```

## Standing orders

### `GET /api/client-channel/banking/standing-orders`

### `POST /api/client-channel/banking/standing-orders`

Request body:

```json
{
  "sourceAccountId": "ACC-001",
  "instructionType": "TRANSFER",
  "destinationAccountId": "ACC-002",
  "amount": 100.00,
  "frequency": "MONTHLY",
  "narration": "Monthly family support",
  "startDate": "2026-05-01T00:00:00Z",
  "endDate": null,
  "stepUpToken": "stepup_123"
}
```

### `POST /api/client-channel/banking/standing-orders/{standingOrderId}/status`

Request body:

```json
{
  "status": "SUSPENDED"
}
```

## Client investments and client loans

### `GET /api/client-channel/banking/investments`

### `POST /api/client-channel/banking/investments`

Request body:

```json
{
  "sourceAccountId": "ACC-001",
  "principal": 5000.00,
  "rate": 0.15,
  "tenureDays": 91,
  "currency": "GHS",
  "stepUpToken": "stepup_123"
}
```

### `GET /api/client-channel/banking/loans`

Response item highlights:

- `id`
- `productCode`
- `productName`
- `principal`
- `rate`
- `termMonths`
- `status`
- `outstandingBalance`
- `servicingAccountId`
- `repaymentFrequency`
- `disbursementDate`
- `parBucket`

### `GET /api/client-channel/banking/loan-products`

### `POST /api/client-channel/banking/loans/apply`

Request body:

```json
{
  "loanProductId": "LP-001",
  "principal": 10000.00,
  "servicingAccountId": "ACC-001",
  "stepUpToken": "stepup_123"
}
```

### `GET /api/client-channel/banking/loans/{loanId}/schedule`

### `GET /api/client-channel/banking/loans/{loanId}/statement`

## Statements

### `GET /api/client-channel/statements`

Response item highlights:

- `statementId`
- `accountId`
- `periodLabel`
- `year`
- `month`
- `entryCount`
- `totalDebits`
- `totalCredits`
- `generatedAt`

### `GET /api/client-channel/statements/{accountId}?year=2026&month=3`

Response fields:

- `statementId`
- `accountId`
- `periodLabel`
- `currency`
- `openingBalance`
- `closingBalance`
- `totalDebits`
- `totalCredits`
- `entries[]`

### `GET /api/client-channel/statements/{accountId}/export?year=2026&month=3&format=csv`

Response fields:

- `fileName`
- `contentType`
- `checksumSha256`
- `lineCount`
- `contentBase64`

## Complaints

### `GET /api/client-channel/complaints`

### `GET /api/client-channel/complaints/{complaintId}`

### `POST /api/client-channel/complaints`

Request body:

```json
{
  "category": "TRANSACTION",
  "summary": "Debit not recognized",
  "details": "Customer disputes debit posted on 2026-04-20"
}
```

### `POST /api/client-channel/complaints/{complaintId}/reopen`

Request body:

```json
{
  "reason": "Issue persists"
}
```

### `POST /api/client-channel/complaints/{complaintId}/attachments`

Request body:

```json
{
  "fileName": "evidence.png",
  "contentType": "image/png",
  "dataUrl": "data:image/png;base64,..."
}
```

## Customer Master API

Controller:

- [CustomerController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\CustomerController.cs)

DTOs:

- [CustomerDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\CustomerDTOs.cs)

Auth:

- staff bearer token with customer permissions

### `GET /api/customers`

Returns customer list items.

Response item highlights:

- `id`
- `type`
- `name`
- `email`
- `phone`
- `digitalAddress`
- `kycLevel`
- `riskRating`
- `ghanaCard`
- `status`
- `createdAt`

### `GET /api/customers/paged?pageNumber=1&pageSize=50&search=ama`

Paged wrapper around customer list items.

### `GET /api/customers/{id}`

Returns customer summary.

### `GET /api/customers/{id}/profile`

Returns expanded profile with:

- base profile fields
- `notes[]`
- `documents[]`
- `mediaAssets[]`
- `profilePhoto`
- `signature`
- `idCardFront`
- `idCardBack`
- `kycReadiness`

### `GET /api/customers/{id}/kyc`

Returns:

- `customerId`
- `kycLevel`
- `transactionLimit`
- `dailyLimit`
- `remainingDailyLimit`
- `isUnlimited`
- `ghanaCardMatchesProfile`
- `todayPostedTotal`
- `readiness`

### `POST /api/customers`

Request body:

```json
{
  "firstName": "Ama",
  "lastName": "Mensah",
  "otherName": "Efua",
  "type": "INDIVIDUAL",
  "ghanaCard": "GHA-123456789-0",
  "idType": "GHANACARD",
  "idNumber": "GHA-123456789-0",
  "digitalAddress": "GA-123-4567",
  "address": "Accra",
  "branchId": "BR001",
  "dateOfBirth": "1990-02-15",
  "gender": "F",
  "kycLevel": "TIER1",
  "phone": "+233201234567",
  "email": "ama@example.com",
  "riskRating": "LOW"
}
```

### `PUT /api/customers/{id}`

Request body:

```json
{
  "name": "Ama Efua Mensah",
  "digitalAddress": "GA-123-4567",
  "phone": "+233201234567",
  "email": "ama@example.com",
  "riskRating": "LOW"
}
```

### `POST /api/customers/{id}/notes`

Request body:

```json
{
  "text": "Customer visited branch for KYC refresh",
  "category": "KYC"
}
```

### `POST /api/customers/{id}/documents`

Request body:

```json
{
  "type": "UTILITY_BILL",
  "name": "March utility bill"
}
```

### `POST /api/customers/{id}/media`

Request body:

```json
{
  "mediaType": "ID_CARD",
  "mediaSide": "FRONT",
  "fileName": "id-front.png",
  "contentType": "image/png",
  "dataUrl": "data:image/png;base64,..."
}
```

## Accounts API

Controller:

- [AccountController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\AccountController.cs)

DTOs:

- [AccountDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\AccountDTOs.cs)

### `GET /api/accounts`

Returns account list items.

Response item fields:

- `id`
- `customerId`
- `customerName`
- `branchId`
- `type`
- `currency`
- `balance`
- `lienAmount`
- `status`
- `productCode`
- `lastTransDate`
- `createdAt`

### `GET /api/accounts/paged?pageNumber=1&pageSize=50&search=ama&type=SAVINGS`

Paged account list.

### `GET /api/accounts/{id}`

Returns account detail.

### `GET /api/accounts/customer/{cif}`

Returns all accounts owned by a customer.

### `POST /api/accounts`

Request body:

```json
{
  "customerId": "CIF-2604-00001",
  "branchId": "BR001",
  "type": "SAVINGS",
  "currency": "GHS",
  "productCode": "SAV-GEN",
  "isConfidential": false,
  "ownerStaffId": null
}
```

## Transactions API

Controller:

- [TransactionController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\TransactionController.cs)

DTOs:

- [TransactionDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\TransactionDTOs.cs)

### `GET /api/transactions`

Returns posted transactions.

### `GET /api/transactions/{id}`

Returns one transaction.

### `POST /api/transactions`

Request body:

```json
{
  "accountId": "ACC-001",
  "type": "CREDIT",
  "amount": 500.00,
  "narration": "Cash deposit",
  "tellerId": "TLR-001",
  "clientReference": "EXT-POST-0001"
}
```

## Payment Operations API

Controller:

- [PaymentOperationsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\PaymentOperationsController.cs)

DTOs:

- [PaymentOperationsDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\PaymentOperationsDTOs.cs)

## Bulk payments

### `GET /api/payments/bulk`

Returns bulk batches.

### `GET /api/payments/bulk/{batchId}`

Returns batch with line-level items.

### `POST /api/payments/bulk`

Request body:

```json
{
  "currency": "GHS",
  "narration": "Salary batch",
  "submittedBy": "ops.user",
  "items": [
    {
      "accountId": "ACC-001",
      "transactionType": "CREDIT",
      "amount": 2500.00,
      "narration": "April salary",
      "tellerId": "SYS-BATCH",
      "clientReference": "SAL-APR-0001"
    }
  ]
}
```

Response highlights:

- `id`
- `batchReference`
- `status`
- `currency`
- `totalAmount`
- `processedAmount`
- `itemCount`
- `processedCount`
- `failedCount`
- `items[]`

## Cheques

### `GET /api/payments/cheques`

### `GET /api/payments/cheques/{itemId}`

### `POST /api/payments/cheques/deposits`

Request body:

```json
{
  "accountId": "ACC-001",
  "chequeNumber": "000123",
  "amount": 1500.00,
  "currency": "GHS",
  "drawerName": "Kwame Mensah",
  "drawerAccountNumber": "0123456789",
  "presentingBankCode": "BANK001",
  "draweeBankCode": "BANK002",
  "isOtherBankCheque": true,
  "clearingChannel": "GHIPSS",
  "bogRegulatoryClass": "LOCAL",
  "tellerId": "TLR-001",
  "narration": "Cheque lodgement"
}
```

### `POST /api/payments/cheques/withdrawals`

Request body:

```json
{
  "accountId": "ACC-001",
  "chequeNumber": "000124",
  "amount": 500.00,
  "currency": "GHS",
  "tellerId": "TLR-001",
  "narration": "Cheque withdrawal"
}
```

### `POST /api/payments/cheques/{itemId}/return`

Request body:

```json
{
  "reason": "Signature mismatch"
}
```

## Cheque books

### `GET /api/payments/cheque-books`

Optional query:

- `accountId`

### `GET /api/payments/cheque-books/{bookId}`

### `POST /api/payments/cheque-books/stock`

Request body:

```json
{
  "branchId": "BR001",
  "seriesPrefix": "CB",
  "startSerialNumber": 100001,
  "leafCount": 25,
  "remarks": "New stock"
}
```

### `POST /api/payments/cheque-books/{bookId}/issue`

Request body:

```json
{
  "accountId": "ACC-001",
  "issuedBy": "ops.user",
  "remarks": "Issued to customer"
}
```

### `POST /api/payments/cheque-books/leaves/{leafId}/cancel`

Request body:

```json
{
  "reason": "Damaged leaf"
}
```

### `POST /api/payments/cheque-books/leaves/use-history`

Request body:

```json
{
  "accountId": "ACC-001",
  "chequeNumber": "CB100010",
  "historicalTransactionId": "LEG-TRX-0001",
  "usedAt": "2026-03-31T10:30:00Z",
  "remarks": "Migrated historical usage"
}
```

## Loans API

Controller:

- [LoanController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\LoanController.cs)

DTOs:

- [LoanDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\LoanDTOs.cs)

## Portfolio and origination

### `GET /api/loans`

Returns `LoanDto` items.

Response item highlights:

- `id`
- `cif`
- `groupId`
- `productCode`
- `productName`
- `principal`
- `rate`
- `termMonths`
- `outstandingBalance`
- `collateralType`
- `collateralValue`
- `servicingAccountId`
- `collateralAccountId`
- `status`
- `isConfidential`
- `ownerStaffId`

### `POST /api/loans/apply`

Request body:

```json
{
  "customerId": "CIF-2604-00001",
  "loanProductId": "LP-001",
  "principal": 10000.00,
  "clientReference": "LOS-0001",
  "isConfidential": false,
  "ownerStaffId": null,
  "servicingAccountId": "ACC-001",
  "collateralAccountId": "ACC-002"
}
```

### `POST /api/loans/approve`

Request body:

```json
{
  "loanId": "LN-001",
  "decisionNotes": "Approved after review"
}
```

### `POST /api/loans/disburse`

Request body:

```json
{
  "loanId": "LN-001",
  "clientReference": "DISB-0001",
  "servicingAccountId": "ACC-001",
  "collateralAccountId": "ACC-002"
}
```

### `POST /api/loans/appraise`

Request body:

```json
{
  "loanId": "LN-001",
  "decision": "Reviewed",
  "notes": "Collateral verified"
}
```

## Credit scoring and bureau

### `POST /api/loans/check-credit`

Request body:

```json
{
  "customerId": "CIF-2604-00001",
  "loanId": null,
  "providerName": "xds"
}
```

Response fields:

- `customerId`
- `loanId`
- `score`
- `bureauScore`
- `internalScore`
- `compositeScore`
- `probabilityGood`
- `riskBand`
- `riskGrade`
- `decision`
- `recommendation`
- `providerName`
- `inquiryReference`
- `assessmentSource`
- `internalDecision`
- `bureauDecision`
- `modelVersion`
- `trainingSampleCount`
- `bureauStatus`
- `bureauFailureReason`
- `featureSummary`
- `checkedAt`

Example response:

```json
{
  "customerId": "CIF-2604-00001",
  "internalScore": 642,
  "bureauScore": 610,
  "compositeScore": 632,
  "probabilityGood": 0.71,
  "riskBand": "MEDIUM",
  "riskGrade": "B",
  "decision": "REVIEW",
  "recommendation": "Borderline case, send to manual review",
  "providerName": "xds",
  "assessmentSource": "INTERNAL_ML_PLUS_BUREAU",
  "featureSummary": {
    "depositCount90d": 34,
    "withdrawalCount90d": 28,
    "repaymentRatio": 0.88
  }
}
```

### `GET /api/loans/credit-bureau/providers`

Returns configured bureau providers.

### `GET /api/loans/credit-scoring/status`

Returns:

- `modelReady`
- `modelVersion`
- `trainedAtUtc`
- `trainingSampleCount`
- `positiveSampleCount`
- `negativeSampleCount`
- `heuristicFallbackEnabled`
- `statusMessage`

## Repayment and servicing

### `POST /api/loans/repay`

Request body:

```json
{
  "loanId": "LN-001",
  "accountId": "ACC-001",
  "amount": 500.00,
  "clientReference": "RPY-0001"
}
```

### `POST /api/loans/{id}/repay`

Request body:

```json
{
  "amount": 500.00,
  "accountId": "ACC-001",
  "clientReference": "RPY-0001"
}
```

### `POST /api/loans/restructure`

Request body:

```json
{
  "loanId": "LN-001",
  "newTermInPeriods": 18,
  "newAnnualRate": 0.24,
  "newRepaymentFrequency": "MONTHLY",
  "reason": "Cash flow pressure"
}
```

### `POST /api/loans/{id}/penalty`

Request body:

```json
{
  "penaltyRate": 0.05,
  "reason": "Past due",
  "clientReference": "PEN-0001"
}
```

### `POST /api/loans/{id}/classify`

No body documented in DTO file. Use the endpoint as an operational evaluation trigger.

### `POST /api/loans/repay/reverse`

Request body:

```json
{
  "loanId": "LN-001",
  "repaymentId": "00000000-0000-0000-0000-000000000000",
  "reason": "Posting correction"
}
```

### `POST /api/loans/writeoff`

Request body:

```json
{
  "loanId": "LN-001",
  "amount": 1500.00,
  "reason": "Approved write-off"
}
```

### `POST /api/loans/recover`

Request body:

```json
{
  "loanId": "LN-001",
  "amount": 250.00,
  "accountId": "ACC-001",
  "reference": "RCV-0001"
}
```

## Loan schedules, statements, and reports

### `POST /api/loans/generate-schedule`

Request body:

```json
{
  "loanProductId": "LP-001",
  "principal": 10000.00,
  "annualInterestRate": 0.24,
  "termInPeriods": 12,
  "interestMethod": "Flat",
  "repaymentFrequency": "Monthly",
  "scheduleType": "Monthly",
  "startDate": "2026-05-01"
}
```

### `GET /api/loans/{id}/statement`

### `GET /api/loans/{id}/schedule`

### `GET /api/loans/{id}/gl-postings`

### `GET /api/loans/{id}/accrual`

### `GET /api/loans/dashboards/delinquency`

### `GET /api/loans/reports/profitability`

### `GET /api/loans/reports/balance-sheet`

## Digital Banking API

Controller:

- [DigitalBankingController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\DigitalBankingController.cs)

DTOs:

- [DigitalBankingDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\DigitalBankingDTOs.cs)

### `GET /api/digital-banking/dashboard`

Response fields:

- `activeSavingsAccounts`
- `totalSavingsBalance`
- `activeInvestmentProfiles`
- `totalInvestmentBalance`
- `activeLoans`
- `totalLoanExposure`
- `pendingApprovals`

### `GET /api/digital-banking/savings/products`

Returns `DigitalBankingProductDto` items.

### `GET /api/digital-banking/savings/accounts/{customerId}`

Returns savings accounts linked to the customer.

### `POST /api/digital-banking/savings/accounts`

Request body:

```json
{
  "customerId": "CIF-2604-00001",
  "productCode": "SAV-GEN",
  "branchId": "BR001",
  "currency": "GHS",
  "initialDepositAmount": 100.00,
  "fundingAccountId": "ACC-001",
  "isConfidential": false,
  "ownerStaffId": null
}
```

### `POST /api/digital-banking/savings/accounts/{accountId}/fund`

### `POST /api/digital-banking/savings/accounts/{accountId}/withdraw`

Shared request body:

```json
{
  "counterpartyAccountId": "ACC-002",
  "amount": 50.00,
  "narration": "Digital savings funding"
}
```

### `GET /api/digital-banking/investments/portfolio?customerId=CIF-2604-00001`

Response fields:

- `activeProfiles`
- `totalPrincipal`
- `totalProjectedMaturityValue`
- `byCurrency`
- `items[]`

### `POST /api/digital-banking/investments`

Request body:

```json
{
  "customerId": "CIF-2604-00001",
  "fundingAccountId": "ACC-001",
  "productCode": "INV-091",
  "principal": 5000.00,
  "rate": 0.15,
  "tenorDays": 91,
  "payoutOption": "AT_MATURITY",
  "autoRollover": false,
  "notes": "Self-service placement"
}
```

### `POST /api/digital-banking/investments/{profileId}/top-up`

### `POST /api/digital-banking/investments/{profileId}/rollover`

### `POST /api/digital-banking/investments/{profileId}/liquidate`

Shared request body:

```json
{
  "fundingAccountId": "ACC-001",
  "amount": 1000.00,
  "newMaturityDate": null,
  "newRate": null,
  "destinationAccountId": "ACC-001",
  "penaltyAmount": 0,
  "notes": "Requested by customer"
}
```

### `POST /api/digital-banking/loans/eligibility`

Request body:

```json
{
  "customerId": "CIF-2604-00001",
  "loanProductId": "LP-001",
  "principal": 5000.00,
  "providerName": "xds"
}
```

Response fields:

- `isEligible`
- `reasons[]`
- `creditCheck`

### `POST /api/digital-banking/loans/apply`

Request body:

```json
{
  "customerId": "CIF-2604-00001",
  "loanProductId": "LP-001",
  "principal": 5000.00,
  "servicingAccountId": "ACC-001",
  "collateralAccountId": "ACC-002",
  "clientReference": "DIGI-LOAN-0001"
}
```

### `POST /api/digital-banking/loans/{loanId}/repay`

Request body:

```json
{
  "amount": 500.00,
  "accountId": "ACC-001",
  "clientReference": "DIGI-RPY-0001"
}
```

### `POST /api/digital-banking/loans/restructure`

Uses the same `LoanRestructureRequest` contract as the core loans API.

### `GET /api/digital-banking/loans/{loanId}/statement`

### `GET /api/digital-banking/loans/{loanId}/schedule`

## Reporting APIs

Primary controllers:

- [ReportController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ReportController.cs)
- [ReportingController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ReportingController.cs)

Use these families for:

- report catalog discovery
- report definition management
- financial statement retrieval
- analytics and generated report runs

Key routes:

- `GET /api/report/catalog`
- `POST /api/report/definitions`
- `GET /api/report/definitions/{id}`
- `POST /api/report/generate`
- `GET /api/report/history/{reportId}`
- `GET /api/report/runs/{runId}`
- `GET /api/report/financial/balance-sheet`
- `GET /api/report/financial/income-statement`
- `GET /api/report/financial/cash-flow`
- `GET /api/report/financial/trial-balance`
- `POST /api/reporting/definitions`
- `GET /api/reporting/definitions`
- `GET /api/reporting/definitions/{id}`
- `POST /api/reporting/generate/{reportId}`
- `GET /api/reporting/history/{reportId}`
- `GET /api/reporting/runs/{runId}`
- `DELETE /api/reporting/definitions/{id}`

## Regulatory and ORASS APIs

Controllers:

- [RegulatoryReportsController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\RegulatoryReportsController.cs)
- [OrassController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\OrassController.cs)

Key routes:

- `GET|POST /api/regulatory-reports/daily-position`
- `GET|POST /api/regulatory-reports/monthly-return-1`
- `GET|POST /api/regulatory-reports/monthly-return-2`
- `GET|POST /api/regulatory-reports/monthly-return-3`
- `GET|POST /api/regulatory-reports/prudential`
- `GET /api/regulatory-reports/prudential-return`
- `GET|POST /api/regulatory-reports/large-exposure`
- `POST /api/regulatory-reports/submit/{returnId}`
- `POST /api/regulatory-reports/submit-to-bog/{returnId}`
- `GET /api/regulatory-reports/history`
- `GET /api/orass/profile`
- `GET /api/orass/readiness`
- `GET /api/orass/queue`
- `GET /api/orass/history`
- `POST /api/orass/submit/{returnId:int}`
- `GET /api/orass/evidence/{returnId:int}`
- `POST /api/orass/acknowledge/{returnId:int}`
- `POST /api/orass/reconcile`

## Security and Session APIs

Controllers:

- [SecurityController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\SecurityController.cs)
- [SessionController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\SessionController.cs)

Key routes:

- `GET /api/security/alerts`
- `GET /api/security/failed-logins`
- `GET /api/security/sessions`
- `GET /api/security/summary`
- `GET /api/security/devices`
- `POST /api/security/devices`
- `POST /api/security/devices/{deviceId}/actions`
- `POST /api/security/devices/scan-outdated`
- `GET /api/security/irregular-transactions`
- `GET /api/security/waf`
- `PUT /api/security/waf`
- `POST /api/session/refresh`
- `POST /api/session/{sessionId}/invalidate`
- `POST /api/session/invalidate-all`
- `GET /api/session/active`
- `GET /api/session/user/{staffId}`
- `GET /api/session/stats`

## Migration API

Controller:

- [DataMigrationController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\DataMigrationController.cs)

Routes:

- `GET /api/migration/datasets`
- `POST /api/migration/import/{dataset}`

Use cases:

- cutover migration
- bulk conversion loads
- controlled historical backfill

## File Retrieval API

Controller:

- [ClientFileController.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Controllers\ClientFileController.cs)

Routes:

- `GET /api/client-files/customer-media/{mediaId}`
- `GET /api/client-files/complaint-attachments/{attachmentId}`

## Integration Recommendations

Use these API groups as the main entry points for external systems:

- customer onboarding and KYC: `api/customers`, `api/client-auth`, `api/client-channel/profile*`
- portfolio and balances: `api/accounts`, `api/client-channel/accounts`, `api/digital-banking/dashboard`
- posting and settlement: `api/transactions`, `api/payments`
- lending and scoring: `api/loans`, `api/digital-banking/loans/*`
- digital self-service: `api/client-channel`, `api/digital-banking`
- reporting and regulatory exchange: `api/report*`, `api/regulatory-reports`, `api/orass`

## Related Technical References

- [BANKINSIGHT_API_EXHAUSTIVE_DOCUMENTATION.md](C:\Backup old\dev\bankinsight\docs\BANKINSIGHT_API_EXHAUSTIVE_DOCUMENTATION.md)
- [INTERNAL_CREDIT_SCORING_DESIGN.md](C:\Backup old\dev\bankinsight\docs\INTERNAL_CREDIT_SCORING_DESIGN.md)
