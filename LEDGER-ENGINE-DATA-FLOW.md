# Ledger Engine Data Flow & Integration Guide

## End-to-End Transaction Flow

### 1. User Initiates Deposit

```
┌─────────────────┐
│ Teller opens    │
│ TellerForms     │
│ component       │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ TellerForms Component               │
│ User fills:                         │
│ • Customer (CIF001)                 │
│ • Ghana Card: GHA-000000000-0       │
│ • Account: ACC001                   │
│ • Amount: GHS 5,000                 │
│ • Deposit Method: CASH              │
│ • Narration: Salary                 │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ validateForm()                      │
│ • Check all fields present          │
│ • Validate amount > 0               │
│ • Verify account balance available  │
│ • Check KYC daily limit             │
└────────┬────────────────────────────┘
         │
         ▼
    ┌────────┐
    │ Valid? │
    └────┬───┘
      Yes│ No
         │ │
         │ ▼
         │ Show error messages
         │ Stop process
         │
         ▼
┌─────────────────────────────────────┐
│ handleSubmit()                      │
│ Calls: handlePostTransaction()      │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ validateBogCompliance()             │
│ useLedger hook calls:               │
│ LedgerService.getKycLimits()        │
│ ✓ Returns limits                    │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ checkSuspiciousActivity()            │
│ LedgerService.checkSuspiciousActivity│
│ • Risk scoring                      │
│ • Flag if > 80 score                │
└────────┬────────────────────────────┘
         │
         ▼
┌───────────────────────────────────────────────────────────┐
│ Construct transaction request:                           │
│ {                                                        │
│   accountId: "ACC001",                                  │
│   customerId: "CIF001",                                 │
│   amount: 5000,                                         │
│   depositMethod: "CASH",                                │
│   narration: "Salary",                                  │
│   tellerId: "TLR001",                                   │
│   customerGhanaCard: "GHA-000000000-0"                  │
│ }                                                        │
└──────────┬────────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────┐
│ useLedger.postDeposit()              │
│ Calls: LedgerService.postDeposit()   │
└──────────┬─────────────────────────┘
           │
           ▼
  ╔════════════════════════════════════╗
  ║ HTTP POST to backend               ║
  ║ /api/ledger/deposits               ║
  ║                                    ║
  ║ Headers:                           ║
  ║ • Authorization: Bearer JWT        ║
  ║ • Content-Type: application/json   ║
  ╚════════════┬═══════════════════════╝
               │
               ▼ ════════ NETWORK ════════
               │
```

### 2. Backend Processes Deposit

```
┌─────────────────────────────────────┐
│ LedgerController.PostDeposit()      │
│ • Receives DepositRequest           │
│ • JWT token validated               │
│ • Logs: "Deposit request"           │
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│ _ledgerEngine.PostDepositAsync()    │
│ ┌─────────────────────────────────┐ │
│ │ Step 1: Validate customer       │ │
│ │ Customer cif=CIF001 exists? ✓   │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 2: Validate account        │ │
│ │ Account id=ACC001 exists? ✓      │ │
│ │ Status=ACTIVE? ✓                 │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 3: BOG Customer ID Verify  │ │
│ │ await ValidateCustomerIdAsync() │ │
│ │ Ghana Card = GHA-000000000-0     │ │
│ │ Matches DB? ✓                    │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 4: KYC Daily Limit Check   │ │
│ │ await _kycService               │ │
│ │ .GetKycLimitInfoAsync()          │ │
│ │ ✓ Returns: DailyLimit=25000      │ │
│ │ Today's total: 0                 │ │
│ │ 0 + 5000 < 25000? ✓              │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 5: Calculate Fees          │ │
│ │ Method=CASH → Fee=0              │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 6: Create Transaction      │ │
│ │ txnId = "TXN1234567890"          │ │
│ │ refNum = "LEG-20240305..."       │ │
│ │ _context.Transactions.Add()      │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 7: Update Account Balance  │ │
│ │ account.Balance += 5000          │ │
│ │ New Balance = 10000              │ │
│ │ LastTransDate = now              │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 8: Post GL Entries         │ │
│ │ Debit:  10100 (Cash)    5000    │ │
│ │ Credit: 20100 (Deposit) 5000    │ │
│ │ _context.JournalLines.AddRange()│ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 9: Save to Database        │ │
│ │ await _context.SaveChangesAsync()│ │
│ │ await transaction.CommitAsync()  │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 10: Audit Log              │ │
│ │ await _auditLoggingService      │ │
│ │ .LogActionAsync(...)            │ │
│ │ Action: "DEPOSIT_POSTED"        │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Step 11: Suspicious Check       │ │
│ │ Amount 5000 > 10000? NO          │ │
│ │ Skip AML check                  │ │
│ └─────────────────────────────────┘ │
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│ LedgerController handles response   │
│ Status: 201 Created                 │
│ Returns LedgerPostingResult         │
└──────────┬──────────────────────────┘
           │
           ▼
```

### 3. Frontend Receives Response

```
┌────────────────────────────────────┐
│ useLedger.postDeposit() receives   │
│ LedgerPostingResult:               │
│ {                                  │
│   success: true,                   │
│   transactionId: "TXN123...",      │
│   reference: "LEG-20240305...",    │
│   amount: 5000,                    │
│   appliedFees: 0,                  │
│   newBalance: 10000,               │
│   journalLines: [...]              │
│ }                                  │
└──────────┬───────────────────────┘
           │
           ▼
┌────────────────────────────────────┐
│ Update useLedger state:            │
│ • loading: false                   │
│ • success: "Deposit posted..."     │
│ • result: { ... }                  │
└──────────┬───────────────────────┘
           │
           ▼
┌────────────────────────────────────┐
│ TellerForms shows:                 │
│ • Green success message            │
│ • Receipt modal                    │
│ • Transaction ID & Reference       │
│ • New Balance: GHS 10,000          │
│ • Applied Fees: GHS 0              │
│ • Print Receipt button             │
└──────────┬───────────────────────┘
           │
           ▼
┌────────────────────────────────────┐
│ Form resets:                       │
│ • Clear customer selection         │
│ • Clear amount field               │
│ • Ready for next transaction       │
└────────────────────────────────────┘
```

## Database Schema Integration

### Customer Verification

```sql
-- Customers table (BOG Compliance)
SELECT * FROM customers 
WHERE id = 'CIF001'
AND ghana_card = 'GHA-000000000-0';  -- Verification check

-- Result: Must match for transaction to proceed
┌────┬──────────┬───────────────────────┬──────────┐
│ id │ name     │ ghana_card            │ kyc_level│
├────┼──────────┼───────────────────────┼──────────┤
│CIF │John Doe  │GHA-000000000-0        │ TIER1    │
│001 │          │                       │          │
└────┴──────────┴───────────────────────┴──────────┘
```

### Account Update

```sql
-- Update account balance
UPDATE accounts
SET balance = balance + 5000,
    last_trans_date = NOW()
WHERE id = 'ACC001';

-- Result: Balance changed from 5000 to 10000
┌────────┬────────┬─────────┬──────────────┐
│ id     │ balance│ lien    │ status       │
├────────┼────────┼─────────┼──────────────┤
│ACC0 │ 10000  │ 0      │ ACTIVE       │
│01   │        │        │              │
└────────┴────────┴─────────┴──────────────┘
```

### GL Posting

```sql
-- Create journal entry
INSERT INTO journal_entries (id, date, description, posted_by, status)
VALUES ('TXN1234567890', NOW(), 'Cash Deposit', 'TLR001', 'POSTED');

-- Create journal lines (double-entry)
INSERT INTO journal_lines (journal_id, account_code, debit, credit)
VALUES 
  ('TXN1234567890', '10100', 5000, 0),      -- Debit Cash
  ('TXN1234567890', '20100', 0, 5000);      -- Credit Deposits

-- Result: Balanced GL entries
┌──────────────┬──────┬──────┬───────┐
│ journal_id   │ code │ debit│credit │
├──────────────┼──────┼──────┼───────┤
│TXN1234 │ 10100│ 5000│  0   │
│567890  │      │     │      │
├──────────────┼──────┼──────┼───────┤
│TXN1234 │ 20100│  0  │ 5000│
│567890  │      │     │      │
└──────────────┴──────┴──────┴───────┘
Total Debit: 5000, Total Credit: 5000 ✓
```

### Audit Trail

```sql
-- Audit log entry
INSERT INTO audit_logs (user_id, action, entity_type, description, status)
VALUES 
  ('TLR001', 'DEPOSIT_POSTED', 'TRANSACTION', 
   'Deposit of GHS 5000 via CASH to account ACC001', 
   'SUCCESS');

-- Result: Complete transaction history
┌────────┬──────────────┬────────────┬──────────────────┐
│ user_id│ action       │ timestamp  │ description      │
├────────┼──────────────┼────────────┼──────────────────┤
│TLR001  │DEPOSIT_POSTED│2024-03...  │Deposit of GHS... │
└────────┴──────────────┴────────────┴──────────────────┘
```

## API Request/Response Examples

### Request: Deposit (CASH)

```json
POST /api/ledger/deposits HTTP/1.1
Host: localhost:5176
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "accountId": "ACC001",
  "customerId": "CIF001",
  "amount": 5000,
  "depositMethod": "CASH",
  "narration": "Salary payment",
  "tellerId": "TLR001",
  "customerGhanaCard": "GHA-000000000-0",
  "branchId": "BR001"
}
```

### Response: Success

```json
{
  "success": true,
  "transactionId": "TXN1234567890",
  "reference": "LEG-20240305120530-ABC123",
  "narration": "CASH Deposit: Salary payment",
  "amount": 5000,
  "appliedFees": 0,
  "netAmount": 5000,
  "newBalance": 10000,
  "availableMargin": 35000,
  "status": "POSTED",
  "message": "Deposit successfully posted. Fees: GHS 0.00",
  "journalLines": [
    {
      "glCode": "10100",
      "glName": "Cash on Hand",
      "debit": 5000,
      "credit": 0,
      "narration": "Deposit"
    },
    {
      "glCode": "20100",
      "glName": "Savings Deposits",
      "debit": 0,
      "credit": 5000,
      "narration": "Deposit"
    }
  ]
}
```

### Response: Error (Ghana Card Mismatch)

```json
{
  "success": false,
  "status": "REJECTED",
  "message": "Ghana Card number does not match customer records. Transaction blocked for compliance."
}
```

### Response: Error (Daily Limit Exceeded)

```json
{
  "success": false,
  "status": "REJECTED",
  "message": "Daily transaction limit exceeded. Limit: GHS 5,000.00, Today's total: GHS 3,000.00"
}
```

## State Management Flow

### React Component State (useLedger)

```typescript
Initial State:
{
  loading: false,
  error: null,
  success: null,
  result: null,
  ledgerEntries: [],
  balance: null
}

↓ User clicks "Submit Transaction"

{
  loading: true,      // Disable submit button
  error: null,
  success: null,
  ...
}

↓ API call completes successfully

{
  loading: false,
  error: null,
  success: "Deposit posted successfully...",
  result: { ...LedgerPostingResult },
  ...
}

↓ User closes receipt (after 1.5 seconds)

form.reset()
Form back to initial state, ready for next transaction
```

## Permission & Security Flow

```
┌─────────────┐
│ User Login  │
└──────┬──────┘
       │
       ▼
┌─────────────────────┐
│ JWT Token Generated │
│ Claims include:     │
│ • role_id           │
│ • permissions[]     │
└──────┬──────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ TellerWorkspace wrapped in            │
│ <PermissionGuard permission="TELLER_ │
│_POST">                                │
└──────┬───────────────────────────────┘
       │
       ▼
    Check: Does token have 'TELLER_POST'?
       │
       ├─ YES → Render component
       │
       └─ NO  → Show "Access Denied"
```

## Error Handling Flow

```
Exception in PostDepositAsync()

┌──────────────────────────────────┐
│ Catch InvalidOperationException  │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ Rollback database transaction    │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ Log to audit:                    │
│ Action: "DEPOSIT_FAILED"         │
│ Status: "FAILED"                 │
│ Error: Exception message         │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ Return LedgerPostingResult:      │
│ {                                │
│   success: false,                │
│   message: "Error detail"        │
│ }                                │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ Frontend useLedger updates state:│
│ error = "Error detail"           │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ TellerForms displays error       │
│ message in red alert             │
└──────────────────────────────────┘
```

## Performance Considerations

### Transaction Processing Latency

```
┌─────────────────────────────────────┐
│ User submits form                   │
└──────────┬──────────────────────────┘
           │
           ▼ 50ms
┌─────────────────────────────────────┐
│ Frontend validation                 │
└──────────┬──────────────────────────┘
           │
           ▼ 100ms
┌─────────────────────────────────────┐
│ API call overhead                   │
└──────────┬──────────────────────────┘
           │
           ▼ 50ms
┌─────────────────────────────────────┐
│ Controller routing                  │
└──────────┬──────────────────────────┘
           │
           ▼ 200ms
┌─────────────────────────────────────┐
│ Ledger Engine processing:           │
│ • Customer validation: 10ms         │
│ • KYC check: 10ms                  │
│ • GL posting: 50ms                 │
│ • DB save: 100ms                   │
└──────────┬──────────────────────────┘
           │
           ▼ 50ms
┌─────────────────────────────────────┐
│ API response serialization          │
└──────────┬──────────────────────────┘
           │
           ▼ 100ms
┌─────────────────────────────────────┐
│ Network round-trip                  │
└──────────┬──────────────────────────┘
           │
           ▼ 100ms
┌─────────────────────────────────────┐
│ Frontend state update & re-render   │
└─────────────────────────────────────┘

TOTAL: ~650ms average
```

---

This document provides the complete end-to-end data flow for understanding how the Ledger Engine processes transactions from the teller interface through to final GL posting and audit logging.
