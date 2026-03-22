# BankInsight Smoke Test Results

Date: 2026-03-03
Environment: Local (Windows), API + Frontend + PostgreSQL

## Runtime Status

- Frontend: Running on `http://localhost:3000`
- Backend API: Running on `http://localhost:5176`
- Database: `bankinsight-postgres` container running on `localhost:5432`

Port check:
- 3000: LISTEN (frontend)
- 5176: LISTEN (backend)

Container check (relevant):
- `bankinsight-postgres`: Up

## Code/Schema Fixes Applied During Validation

1. Auth runtime compatibility fix
   - Updated login verification in `BankInsight.API/Services/AuthService.cs`
   - Supports existing plaintext legacy password rows and automatically rehashes to BCrypt after successful login
   - Eliminated runtime `BCrypt.Net.SaltParseException`

2. Transaction rollback/audit stability fix
   - Updated `BankInsight.API/Services/TransactionService.cs`
   - Rollback now occurs only in the transactional (pre-commit) failure path
   - Audit logging is performed post-commit and cannot invalidate a committed financial post

3. Audit log persistence migration
   - Generated migration:
     - `BankInsight.API/Migrations/20260303083741_AddAuditLogTable.cs`
     - `BankInsight.API/Migrations/20260303083741_AddAuditLogTable.Designer.cs`
   - Applied migration with `dotnet ef database update`
   - Confirmed `audit_logs` table exists and writes records

## Executed Smoke Tests

### 1) Authentication (Login)

- Endpoint: `POST /api/auth/login`
- Credentials used: seeded admin (`admin@bankinsight.local`)
- Result: **200 OK** with JWT + refresh token

### 2) Authorization Enforcement

- Endpoint: `GET /api/users` without token
- Result: **401 Unauthorized**

- Endpoint: `GET /api/users` with bearer token from login
- Result: **200 OK**

### 3) Accounts CRUD Smoke

- Create account: `POST /api/accounts` for seeded customer `CUST0001`
- Result: **201 Created**, account id returned (`20156831301`)

- Fetch account by id: `GET /api/accounts/{id}`
- Result: **200 OK**, returned id matched created id

- List by customer: `GET /api/accounts/customer/CUST0001`
- Result: **200 OK**, account appears in customer list

### 4) Transactions + Balance Movement

- Deposit test on account `20156831301`
- Result: **201 Created**, balance increased by exact posted amount

- Withdrawal test on account `20156831301`
- Result: **201 Created**, balance decreased by exact posted amount

### 5) Negative Case (Business Rule)

- Oversized withdrawal request
- Result: **400 Bad Request**
- Failure reason surfaced: KYC Tier limit exceeded

### 6) Audit Logging Verification

Queried latest records from PostgreSQL `audit_logs`:

- `POST_TRANSACTION|TRANSACTION|TXN1772527110350|SUCCESS`
- `POST_TRANSACTION|TRANSACTION|TXN1772527175653|SUCCESS`
- `POST_TRANSACTION_FAILED|TRANSACTION||FAILED|Transaction amount $999,999.00 exceeds KYC Tier 2 limit of $1,000.00`

Result: **Success and failure transaction attempts are both auditable**.

## Final Outcome

Core live-path verification is successful:

- API boots and serves endpoints
- Frontend and backend are reachable
- Authentication and authorization work
- Account create/read works
- Transaction posting updates balances correctly
- Negative transaction path returns expected validation failure
- Audit logs persist in database for compliance traceability

## Optional Next Checks (not required for current pass)

- Add an automated script to replay this smoke suite in one command
- Add integration tests for transaction commit/audit sequencing
- Add KYC tier matrix tests (Tier 1/2/3 boundary amounts)
