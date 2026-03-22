# BankInsight Ledger Engine & Teller Forms Implementation

## Overview

This document describes the implementation of the **Ledger Engine** and **Teller Forms** system that enables BOG (Bank of Ghana) compliant banking transactions with comprehensive customer ID verification, KYC-based limits, and double-entry bookkeeping.

## Architecture

### Backend Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    API Controllers                              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ LedgerController: RESTful API Endpoints                 │   │
│  │ - POST /api/ledger/deposits (cash & cheque)             │   │
│  │ - POST /api/ledger/withdrawals (cash & cheque)          │   │
│  │ - POST /api/ledger/transfers (inter-account)            │   │
│  │ - POST /api/ledger/cheques (clearing)                   │   │
│  │ - GET /api/ledger/ledger (entries)                      │   │
│  │ - GET /api/ledger/balance/{accountId}                   │   │
│  │ - GET /api/ledger/margins/{customerId}                  │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Ledger Engine Service                        │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ ILedgerEngine: Double-Entry Bookkeeping                 │  │
│  │                                                          │  │
│  │ Methods:                                                │  │
│  │ • PostDepositAsync()     - Cash/cheque deposits         │  │
│  │ • PostWithdrawalAsync()  - Cash/cheque withdrawals      │  │
│  │ • PostTransferAsync()    - Inter-account transfers      │  │
│  │ • PostChequeAsync()      - Cheque clearing              │  │
│  │ • GetLedgerEntriesAsync()- Journal entries query        │  │
│  │ • GetAccountBalanceAsync()- Balance & daily totals      │  │
│  │ • GetAvailableMarginAsync()- Credit margins             │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Support Services                             │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ IKycService         - KYC Level & Limit Validation       │  │
│  │ IAuditLoggingService- Transaction Audit Trail            │  │
│  │ ISuspiciousActivity - Large Transaction Monitoring       │  │
│  │ IFeeService         - Transaction Fee Calculation        │  │
│  │ ApplicationDbContext - PostgreSQL Database Access        │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Frontend Architecture

```
┌──────────────────────────────────────────────────────────┐
│          TellerWorkspace Component                        │
│  ┌────────────────────────────────────────────────────┐  │
│  │ • Forms Tab → TellerForms Component                │  │
│  │ • Ledger Tab → Ledger Entries View                 │  │
│  │ • Accounts Tab → Account List                      │  │
│  │ • Reports Tab → Daily Statistics                   │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
                         ↓
┌──────────────────────────────────────────────────────────┐
│      TellerForms Component (React)                        │
│  ┌────────────────────────────────────────────────────┐  │
│  │ • Deposit Form (cash & cheque)                     │  │
│  │ • Withdrawal Form (cash & cheque)                  │  │
│  │ • Transfer Form (inter-account)                    │  │
│  │ • Customer ID Verification (Ghana Card)            │  │
│  │ • BOG KYC Limit Checking                           │  │
│  │ • Margins & Credit Limit Verification              │  │
│  │ • Transaction Receipt Generation                   │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
                         ↓
┌──────────────────────────────────────────────────────────┐
│      useLedger Hook (React)                              │
│  ┌────────────────────────────────────────────────────┐  │
│  │ • postDeposit()                                    │  │
│  │ • postWithdrawal()                                 │  │
│  │ • postTransfer()                                   │  │
│  │ • postCheque()                                     │  │
│  │ • validateBogCompliance()                          │  │
│  │ • checkGhanaCardValidity()                         │  │
│  │ • checkSuspiciousActivity()                        │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
                         ↓
┌──────────────────────────────────────────────────────────┐
│      LedgerService (TypeScript)                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ • API endpoints to backend LedgerController        │  │
│  │ • KYC validation & compliance checks               │  │
│  │ • Margins API integration                          │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

## Core Features

### 1. **Deposit Processing**

#### Cash Deposits
- Accept cash deposit with customer verification
- Apply optional processing fees
- Update account balance immediately
- Post GL entries: Debit Cash (10100), Credit Customer Deposit (20100/20101)

#### Cheque Deposits
- Capture cheque details (number, bank, amount)
- Hold amount in clearing account (10110) until clearance
- Apply cheque processing fee (GHS 50 minimum or 0.1%)
- Post GL entries: Debit Bank Clearing (10110), Credit Customer Deposit

### 2. **Withdrawal Processing**

#### Cash Withdrawals
- Verify available balance (balance - lien amount)
- Apply withdrawal fee for large withdrawals (≥GHS 5,000): 0.5%
- Deduct amount from account
- Post GL entries: Debit Customer Deposit, Credit Cash (10100)

#### Cheque Withdrawals
- Generate cheque number (if not provided)
- Charge fixed cheque issuance fee (GHS 100)
- Hold amount in cheque clearing
- Post GL entries: Debit Customer Deposit, Credit Bank Clearing

### 3. **Inter-Account Transfers**

- Verify source account has sufficient funds
- Apply transfer fee based on amount:
  - < GHS 10,000: Free
  - GHS 10,000 - GHS 99,999: 0.5%
  - ≥ GHS 100,000: 0.8%
- Post GL entries: Debit From Account, Credit To Account
- Both accounts updated immediately

### 4. **BOG Compliance - Customer ID Verification**

Every transaction requires:

1. **Customer Ghana Card Verification**
   - Customer selects their CIF number
   - Enters their Ghana Card number
   - System validates it matches customer records
   - Transaction blocked if verification fails

2. **KYC Tier-Based Daily Limits**
   ```
   Tier 1: GHS 5,000 per transaction, GHS 25,000 daily
   Tier 2: GHS 10,000 per transaction, GHS 50,000 daily
   Tier 3: Unlimited transactions
   ```

3. **Suspicious Activity Monitoring**
   - Transactions > GHS 10,000 auto-flagged
   - Risk scoring based on:
     - Customer risk rating
     - Transaction frequency
     - Deviation from normal patterns
   - High-risk transactions (score > 80) require manager approval

### 5. **Double-Entry Bookkeeping**

All transactions post to GL with balanced entries:

**GL Account Codes:**
```
10100  - Cash on Hand (Asset)
10110  - Bank Cheque Clearing (Asset)
20100  - Savings Deposits (Liability)
20101  - Current Deposits (Liability)
40500  - Fees Earned (Income)
50500  - Clearing Fees (Expense)
```

Example: Deposit GL Posting
```
Debit:  10100 (Cash)         GHS 1,000.00
Credit: 20100 (Savings)      GHS 1,000.00
```

### 6. **Credit Margins & Lending Limits**

Available margin calculated as:
```
Base Margin (per KYC tier) × Risk Adjustment - Existing Loan Exposure
```

**Base Margins:**
- Tier 1: GHS 10,000
- Tier 2: GHS 50,000
- Tier 3: GHS 500,000

**Risk Adjustments:**
- Low Risk: 100%
- Medium Risk: 75%
- High Risk: 50%

## API Endpoints

### POST /api/ledger/deposits

**Request:**
```json
{
  "accountId": "ACC001",
  "customerId": "CIF001",
  "amount": 5000,
  "depositMethod": "CASH",
  "narration": "Salary payment",
  "tellerId": "TLR001",
  "customerGhanaCard": "GHA-000000000-0"
}
```

**Response:** `LedgerPostingResult`
```json
{
  "success": true,
  "transactionId": "TXN1234567890",
  "reference": "LEG-20240305120530-ABC123",
  "amount": 5000,
  "appliedFees": 0,
  "newBalance": 15000,
  "status": "POSTED",
  "journalLines": [
    {"glCode": "10100", "debit": 5000, "credit": 0},
    {"glCode": "20100", "debit": 0, "credit": 5000}
  ]
}
```

### POST /api/ledger/withdrawals

Request format similar to deposits.

### POST /api/ledger/transfers

**Request:**
```json
{
  "fromAccountId": "ACC001",
  "toAccountId": "ACC002",
  "customerId": "CIF001",
  "amount": 2500,
  "narration": "Transfer to savings",
  "tellerId": "TLR001",
  "customerGhanaCard": "GHA-000000000-0"
}
```

### POST /api/ledger/cheques

**Request:**
```json
{
  "accountId": "ACC001",
  "customerId": "CIF001",
  "chequeNumber": "123456",
  "chequeBank": "GCB Bank",
  "amount": 1000,
  "narration": "Customer cheque deposit",
  "transactionType": "DEPOSIT",
  "tellerId": "TLR001"
}
```

### GET /api/ledger/ledger

**Query Parameters:**
- `accountId` (required): Account ID
- `fromDate` (optional): Start date (ISO 8601)
- `toDate` (optional): End date (ISO 8601)

**Response:** `List<LedgerEntry>`
```json
[
  {
    "id": "TXN123",
    "journalId": "TXN123",
    "glCode": "10100",
    "debit": 5000,
    "credit": 0,
    "narration": "Cash deposit",
    "postedDate": "2024-03-05T12:05:30Z"
  }
]
```

### GET /api/ledger/balance/{accountId}

**Response:** `LedgerBalance`
```json
{
  "accountId": "ACC001",
  "balance": 15000,
  "lienAmount": 2000,
  "availableBalance": 13000,
  "dailyDebitTotal": 5000,
  "dailyCreditTotal": 10000
}
```

### GET /api/ledger/margins/{customerId}

**Response:**
```json
{
  "availableMargin": 35000
}
```

## Frontend Components

### TellerForms Component

**Props:**
```typescript
interface TellerFormsProps {
  customers: Customer[];
  accounts: Account[];
  onPostTransaction: (data: any) => Promise<LedgerPostingResult>;
}
```

**Features:**
- Multi-tab form interface (Deposit, Withdrawal, Transfer, Cheque)
- Real-time validation
- BOG compliance checks
- Transaction receipts
- Error/success messaging

### TellerWorkspace Component

**Props:**
```typescript
interface TellerWorkspaceProps {
  customers: Customer[];
  accounts: Account[];
  transactions: Transaction[];
}
```

**Tabs:**
1. Transaction Forms - Full TellerForms component
2. Ledger Entries - Filtered ledger for selected account
3. Accounts - Account directory with balances
4. Reports - Daily statistics & compliance status

### useLedger Hook

**Returns:**
```typescript
{
  // State
  loading: boolean;
  error: string | null;
  success: string | null;
  result: LedgerPostingResult | null;
  
  // Methods
  postDeposit: (request) => Promise<LedgerPostingResult>;
  postWithdrawal: (request) => Promise<LedgerPostingResult>;
  postTransfer: (request) => Promise<LedgerPostingResult>;
  postCheque: (request) => Promise<LedgerPostingResult>;
  validateBogCompliance: (customerId, amount) => Promise<...>;
  checkGhanaCardValidity: (customerId, card) => Promise<...>;
  checkSuspiciousActivity: (accountId, amount) => Promise<...>;
}
```

## Integration Steps

### 1. Backend Setup

1. Copy `LedgerEngine.cs` to `BankInsight.API/Services/`
2. Copy `LedgerController.cs` to `BankInsight.API/Controllers/`
3. Register `ILedgerEngine` in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<ILedgerEngine, LedgerEngine>();
   ```
4. Update database schema if needed (GL accounts already configured)

### 2. Frontend Setup

1. Copy `TellerForms.tsx` to `components/`
2. Copy `TellerWorkspace.tsx` to `components/`
3. Copy `ledgerService.ts` to `src/services/`
4. Copy `useLedger.ts` to `src/hooks/`
5. Update `types.ts` with ledger types

### 3. App Integration

Add to sidebar navigation:
```tsx
{icon: <Banknote size={20} />; label: "Teller"; onClick: () => setActiveTab('teller');}
```

Add to main app render:
```tsx
{activeTab === 'teller' && <TellerWorkspace {...props} />}
```

## BOG Compliance Checklist

✅ **Customer ID Verification**
- Ghana Card validation on every transaction
- Transaction blocked if card doesn't match

✅ **KYC Tier Limits**
- Tier 1: GHS 5,000 daily limit
- Tier 2: GHS 50,000 daily limit
- Tier 3: Unlimited

✅ **Suspicious Transaction Monitoring**
- Large transactions (>GHS 10,000) flagged
- Risk scoring enabled
- Manager approval for high-risk

✅ **Audit Trail**
- All transactions logged with:
  - Teller ID
  - Customer ID
  - Amount
  - Timestamp
  - GL entries

✅ **Double-Entry Bookkeeping**
- Every transaction balanced in GL
- Asset/Liability/Income/Expense accounts

## Testing

### Unit Tests (Backend)

```csharp
// Test BOG compliance
[Test]
public async Task PostDeposit_WithInvalidGhanaCard_ThrowsException()
{
    var request = new DepositRequest { customerGhanaCard = "INVALID" };
    Assert.ThrowsAsync<InvalidOperationException>(() => 
        ledgerEngine.PostDepositAsync(request));
}

// Test KYC limit
[Test]
public async Task PostDeposit_ExceedingDailyLimit_ThrowsException()
{
    var request = new DepositRequest { amount = 100000 }; // Over Tier 1 limit
    Assert.ThrowsAsync<InvalidOperationException>(() => 
        ledgerEngine.PostDepositAsync(request));
}
```

### Integration Tests (Frontend)

```typescript
// Test form submission
test('TellerForms accepts valid deposit', async () => {
  const { getByText, getByPlaceholderText } = render(
    <TellerForms customers={mockCustomers} accounts={mockAccounts} />
  );
  
  fireEvent.change(getByPlaceholderText('Amount'), { target: { value: '1000' } });
  fireEvent.click(getByText('Submit Transaction'));
  
  await waitFor(() => {
    expect(getByText(/Transaction posted successfully/)).toBeInTheDocument();
  });
});
```

## Troubleshooting

### Issue: "Ghana Card number does not match customer records"
- Ensure customer Ghana Card in database is accurate
- Verify customer entered their own card (not another's)

### Issue: "Daily transaction limit exceeded"
- Check customer's KYC tier
- Verify amount + today's total doesn't exceed limit
- Contact branch manager if limit increase needed

### Issue: Transaction flagged as suspicious
- High-risk transactions (score > 80) require approval
- Check customer's risk rating
- Verify transaction is legitimate with customer
- Manager can approve via approval workflow

### Issue: GL entries not posting
- Verify GL accounts exist in database
- Check account codes in LedgerEngine match GL chart
- Ensure double debit/credit are equal

## Future Enhancements

1. **Caching** - Cache KYC limits and customer data
2. **Batch Processing** - Support bulk uploads
3. **Mobile Integration** - Mobile teller app
4. **Real-time Analytics** - Live transaction dashboards
5. **Advanced Margins** - ML-based credit scoring
6. **Multi-currency** - Support USD, GBP, EUR
7. **Payment Integration** - Mobile money, card payments

## Reference Documentation

- Bank of Ghana KYC Guidelines: https://bog.gov.gh
- Ghana Card Standards: https://gra.gov.gh
- PCI DSS Compliance: https://www.pcisecuritystandards.org
- IFRS 9 Accounting: https://www.ifrs.org

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review test cases for examples
3. Check backend logs: `BankInsight.API/Logs/`
4. Contact development team
