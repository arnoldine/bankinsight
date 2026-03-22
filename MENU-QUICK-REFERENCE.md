# Enhanced Menu System - Quick Reference

## Menu Structure at a Glance

```
CORE OPERATIONS
├── Dashboard                 (no permission required)
├── Client Management         (permission: ACCOUNT_READ)
│   ├── Client List
│   └── Onboarding
├── Accounts                  (permission: ACCOUNT_READ)
│   ├── Account List
│   └── Create Account
├── Groups                    (permission: ACCOUNT_READ)
├── Teller Operations         (permission: TELLER_POST)
│   ├── Cash Deposits
│   ├── Cash Withdrawals
│   └── Transfers
└── Transactions              (permission: ACCOUNT_READ)

LOAN MANAGEMENT
├── Loans                     (permission: LOAN_READ)
│   ├── Pipeline
│   ├── Portfolio
│   └── Approvals
└── Approvals                 (permission: LOAN_APPROVE)

FINANCIAL MANAGEMENT
├── Accounting                (permission: GL_READ)
│   ├── Journal Entries
│   ├── Reconciliation
│   └── GL Accounts
├── Statements                (permission: ACCOUNT_READ)
├── Treasury                  (permission: ACCOUNT_READ)
│   ├── Position Monitor
│   ├── FX Management
│   └── Investments
└── Vault                     (permission: ACCOUNT_READ)

OPERATIONS & RISK
├── Operations                (permission: ACCOUNT_READ)
│   ├── Fees
│   ├── Penalties
│   └── NPL Management
└── Reporting                 (permission: REPORT_VIEW)

WORKSPACES
├── Loan Officer              (permission: LOAN_READ)
├── Accountant                (permission: GL_POST)
├── Customer Service          (permission: ACCOUNT_READ)
└── Compliance                (permission: AUDIT_READ)

SYSTEM
├── Products                  (permission: SYSTEM_CONFIG)
├── Settings                  (permission: SYSTEM_CONFIG)
├── End of Day                (permission: SYSTEM_CONFIG)
├── Audit Trail               (permission: SYSTEM_CONFIG)
├── Extensibility             (permission: SYSTEM_CONFIG)
└── Dev Tasks                 (no permission required)
```

## Permission to Menu Item Mapping

### ACCOUNT_READ
- Client Management
- Accounts
- Groups
- Transactions
- Statements
- Treasury
- Vault
- Operations
- Customer Service

### TELLER_POST
- Teller Operations

### LOAN_READ
- Loans
- Loan Officer Workspace

### LOAN_APPROVE
- Approvals

### GL_READ
- Accounting

### GL_POST
- Accountant Workspace

### REPORT_VIEW
- Reporting

### SYSTEM_CONFIG
- Products
- Settings
- End of Day
- Audit Trail
- Extensibility

### AUDIT_READ
- Compliance Workspace

## Screen Components Implemented

| Screen ID | Component | Status | API Endpoints |
|-----------|-----------|--------|---------------|
| dashboard | DashboardView | ✅ Implemented | N/A |
| clients | ClientManager | ✅ Implemented | GET /customers, POST /customers |
| clients-list | ClientManager | ✅ Implemented | GET /customers |
| clients-onboard | ClientManager | ✅ Implemented | POST /customers |
| groups | GroupManager | ✅ Implemented | GET /groups, POST /groups |
| teller | TellerTerminal | ✅ Implemented | POST /transactions |
| teller-deposit | TellerTerminal | ✅ Implemented | POST /transactions?type=DEPOSIT |
| teller-withdrawal | TellerTerminal | ✅ Implemented | POST /transactions?type=WITHDRAWAL |
| teller-transfers | TellerTerminal | ✅ Implemented | POST /transactions?type=TRANSFER |
| transactions | TransactionExplorer | ✅ Implemented | GET /transactions |
| statements | StatementVerification | ✅ Implemented | GET /statements |
| accounting | AccountingEngine | ✅ Implemented | GET /gl/accounts, POST /gl/journals |
| accounting-je | AccountingEngine | ✅ Implemented | POST /gl/journals |
| accounting-reconcile | AccountingEngine | ✅ Implemented | GET /gl/accounts |
| accounting-gl | AccountingEngine | ✅ Implemented | GET /gl/accounts |
| loans | LoanManagementHub | ✅ Implemented | GET /loans |
| loans-pipeline | LoanManagementHub | ✅ Implemented | GET /loans |
| loans-portfolio | LoanManagementHub | ✅ Implemented | GET /loans |
| loans-approvals | LoanManagementHub | ✅ Implemented | GET /loans |
| approvals | ApprovalInbox | ✅ Implemented | GET /approvals, POST /approvals/{id}/approve |
| vault | VaultManagementHub | ✅ Implemented | GET /vault |
| treasury | TreasuryManagementHub | ✅ Implemented | GET /treasury/position |
| treasury-position | TreasuryManagementHub | ✅ Implemented | GET /treasury/position |
| treasury-fx | FxRateManagement | ✅ Implemented | GET /fx-rates |
| treasury-investments | InvestmentPortfolio | ✅ Implemented | GET /investments |
| operations | OperationsHub | ✅ Implemented | GET /operations |
| operations-fees | FeePanel | ✅ Implemented | GET /fees |
| operations-penalties | PenaltyPanel | ✅ Implemented | GET /penalties |
| operations-npl | NPLPanel | ✅ Implemented | GET /npl |
| reporting | ReportingHub | ✅ Implemented | GET /reports |
| loanofficer | LoanOfficerWorkspace | ✅ Implemented | GET /loans, POST /loans |
| accountant | AccountantWorkspace | ✅ Implemented | GET /gl/accounts, POST /gl/journals |
| customerservice | CustomerServiceWorkspace | ✅ Implemented | GET /customers, GET /transactions |
| compliance | ComplianceOfficerWorkspace | ✅ Implemented | GET /customers |
| products | ProductDesigner | ✅ Implemented | GET /products, POST /products |
| eod | EodConsole | ✅ Implemented | POST /eod/process |
| audit | AuditTrail | ✅ Implemented | GET /audit |
| extensibility | ExtensibilityTestPage | ✅ Implemented | N/A |
| settings | Settings | ✅ Implemented | PUT /config |
| tasks | DevelopmentTasks | ✅ Implemented | N/A |

## How to Add a New Menu Item

1. Create your component in `/src/components/`
2. Import it in `EnhancedDashboardLayout.tsx`
3. Add it to the appropriate `MenuGroup` in the `menuGroups` array:
   ```typescript
   {
     id: 'unique-id',
     label: 'Display Name',
     icon: <IconName size={18} />,
     permission: 'REQUIRED_PERMISSION', // or omit for no restriction
     subItems: [
       { id: 'sub-id', label: 'Sub Name', icon: <SubIcon size={16} /> }
     ]
   }
   ```
4. Add a case in `renderScreenContent()` switch statement:
   ```typescript
   case 'unique-id':
     return <YourComponent {...requiredProps} />;
   ```
5. Rebuild the app: `npm run build`

## How to Test Permission Filtering

1. Login with admin user (has all permissions)
2. Should see all 6 menu groups with all items
3. Open browser DevTools → Application → LocalStorage
4. Copy the `bankinsight_token` JWT
5. Decode it at jwt.io to see permissions
6. Use different user accounts with fewer permissions
7. Verify menu items hide/show based on their permission requirements

## File Structure

```
src/
├── components/
│   ├── EnhancedDashboardLayout.tsx    ← Main layout file
│   ├── DashboardLayout.tsx            ← Old version (deprecated)
│   ├── ClientManager.tsx
│   ├── GroupManager.tsx
│   ├── TellerTerminal.tsx
│   ├── TransactionExplorer.tsx
│   ├── StatementVerification.tsx
│   ├── AccountingEngine.tsx
│   ├── LoanManagementHub.tsx
│   ├── ApprovalInbox.tsx
│   ├── VaultManagementHub.tsx
│   ├── TreasuryManagementHub.tsx
│   ├── OperationsHub.tsx
│   ├── ReportingHub.tsx
│   ├── LoanOfficerWorkspace.tsx
│   ├── AccountantWorkspace.tsx
│   ├── CustomerServiceWorkspace.tsx
│   ├── ComplianceOfficerWorkspace.tsx
│   ├── ProductDesigner.tsx
│   ├── EodConsole.tsx
│   ├── AuditTrail.tsx
│   ├── DevelopmentTasks.tsx
│   ├── FeePanel.tsx
│   ├── PenaltyPanel.tsx
│   ├── NPLPanel.tsx
│   ├── RiskDashboard.tsx
│   ├── InvestmentPortfolio.tsx
│   ├── FxRateManagement.tsx
│   ├── FxTradingDesk.tsx
│   ├── Settings.tsx
│   ├── DynamicForms/
│   │   └── ExtensibilityTestPage.tsx
│   └── ...
├── AppIntegrated.tsx                 ← Uses EnhancedDashboardLayout
├── lib/
│   └── jwtUtils.ts                   ← hasPermission function
└── hooks/
    └── useApi.ts                     ← useAuth hook
```

## Performance Metrics

- Frontend Bundle Size: 600.73 kB (gzip: 142.99 kB)
- Modules Transformed: 1802
- Build Time: 9.68s
- Menu Items: 30+
- Submenu Items: 12
- Permission Filters: 8 distinct permissions
- Screen Components: 31

## Troubleshooting

### Menu items not showing
- Check user's JWT token has the required permissions
- Verify `hasPermission()` function in `jwtUtils.ts`
- Look at browser console for errors
- Logout and login again to refresh token

### Submenu not expanding
- Check `toggleMenu()` function is being called
- Verify `expandedMenus` state is updating
- Look for CSS issues with `ChevronDown` rotation

### Component imports failing
- Verify component file exists in `/src/components/`
- Check import path is correct (relative vs absolute)
- Run `npm run build` to see actual error messages

### API calls not working
- Verify backend API is running on localhost:5176
- Check network tab in DevTools for failed requests
- Verify JWT token is valid (not expired)
- Check CORS configuration in backend

## Next Steps

1. Test all menu items and screens end-to-end
2. Implement proper error handling in each screen
3. Add loading states for API calls
4. Implement real data fetching from backend
5. Add form validation and submission handling
6. Implement breadcrumb navigation
7. Add keyboard shortcuts for power users
8. Create responsive mobile layout
9. Add analytics tracking for usage
10. Optimize bundle size with code splitting
