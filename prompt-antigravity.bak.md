
# BankInsight System Prompt

I want to build a web app that uses a simulation of **OpenInsight** as the backend. The app is called **BankInsight**. It is a React-based Core Banking System simulation that mimics the functionality of an OpenInsight (RevG) / Linear Hash database environment, specifically tailored for Bank of Ghana compliance.

Please implement the application with the following architecture and file structure.

## Tech Stack
- **Frontend**: React 19, Tailwind CSS, Lucide React Icons.
- **Backend Simulation**: A custom hook (`useBankingSystem`) that manages state as if it were a MultiValue (NoSQL) database.
- **AI Integration**: Google Gemini API for generating insights, Basic+ code, and data migration mapping.

## File Structure & Content

### index.tsx
```tsx
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error("Could not find root element to mount to");
}

const root = ReactDOM.createRoot(rootElement);
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
```

### metadata.json
```json
{
  "name": "BankInsight",
  "description": "A Bank of Ghana compliant Core Banking System simulation using React frontend and OpenInsight Basic+ backend logic.",
  "requestFramePermissions": []
}
```

### index.html
```html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>BankInsight</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
      body {
        font-family: 'Inter', sans-serif;
        background-color: #f3f4f6;
      }
      /* Custom Scrollbar for sleek look */
      ::-webkit-scrollbar {
        width: 8px;
        height: 8px;
      }
      ::-webkit-scrollbar-track {
        background: #f1f1f1;
      }
      ::-webkit-scrollbar-thumb {
        background: #cbd5e1;
        border-radius: 4px;
      }
      ::-webkit-scrollbar-thumb:hover {
        background: #94a3b8;
      }
    </style>
  <script type="importmap">
{
  "imports": {
    "recharts": "https://esm.sh/recharts@^3.7.0",
    "react/": "https://esm.sh/react@^19.2.4/",
    "react": "https://esm.sh/react@^19.2.4",
    "react-dom/": "https://esm.sh/react-dom@^19.2.4/",
    "lucide-react": "https://esm.sh/lucide-react@^0.563.0",
    "@google/genai": "https://esm.sh/@google/genai@^1.41.0"
  }
}
</script>
</head>
  <body>
    <div id="root"></div>
  </body>
</html>
```

### types.ts
```ts
export interface ClientDocument {
  id: string;
  type: string;
  name: string;
  status: 'VERIFIED' | 'PENDING' | 'REJECTED';
  uploadDate: string;
}

export interface ClientNote {
  id: string;
  author: string;
  text: string;
  date: string;
  category: 'GENERAL' | 'COLLECTION' | 'COMPLIANCE';
}

export interface Customer {
  id: string; // CIF Number
  name: string;
  ghanaCard: string; // GHA-000000000-0
  digitalAddress: string; // GPS
  kycLevel: 'Tier 1' | 'Tier 2' | 'Tier 3'; // BoG KYC Tiers
  phone: string;
  email: string;
  riskRating: 'Low' | 'Medium' | 'High';
  // Optional CRM fields
  documents?: ClientDocument[];
  notes?: ClientNote[];
}

export interface Group {
    id: string;
    name: string;
    officer: string;
    meetingDay: string; // e.g., 'Tuesday'
    formationDate: string;
    members: string[]; // Array of Customer IDs (CIFs)
    status: 'ACTIVE' | 'DISSOLVED' | 'PENDING';
}

export interface Product {
    id: string; // Product Code e.g., 'SA-100'
    name: string;
    description: string;
    type: 'SAVINGS' | 'CURRENT' | 'FIXED_DEPOSIT' | 'LOAN';
    currency: 'GHS' | 'USD';
    interestRate: number; // Annual Interest Rate %
    interestMethod?: 'FLAT' | 'REDUCING_BALANCE' | 'COMPOUND' | 'NONE';
    minAmount: number; // Min Balance or Min Principal
    maxAmount?: number;
    defaultTerm?: number; // Months (for Loans/FD)
    minTerm?: number;
    maxTerm?: number;
    status: 'ACTIVE' | 'RETIRED';
}

export interface AccountMandate {
    instructions: string; // e.g. "Sole Signatory", "Either to sign"
    signatories: {
        name: string;
        role: string;
        signatureUrl?: string; // Mock URL or placeholder
        photoUrl?: string;
    }[];
}

export interface Account {
  id: string; // Account Number
  cif: string; // Link to Customer
  branchId: string; // Link to Branch
  type: 'SAVINGS' | 'CURRENT' | 'FIXED_DEPOSIT';
  currency: 'GHS' | 'USD';
  balance: number;
  lienAmount: number;
  status: 'ACTIVE' | 'DORMANT' | 'FROZEN';
  productCode: string; // e.g., 'Sav-01'
  lastTransDate: string;
  mandate?: AccountMandate;
}

export interface Loan {
  id: string;
  cif: string; 
  groupId?: string; 
  memberSplits?: Record<string, number>; // Maps Member CIF to Principal Portion
  productName: string; 
  productCode?: string; 
  principal: number;
  rate: number;
  termMonths: number;
  disbursementDate: string;
  parBucket: '0' | '1-30' | '31-90' | '91+'; // Portfolio At Risk
  outstandingBalance: number;
  collateralType: string;
  status: 'ACTIVE' | 'PENDING' | 'WRITTEN_OFF' | 'CLOSED';
}

export interface Transaction {
  id: string;
  accountId: string;
  type: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'LOAN_REPAYMENT';
  amount: number;
  narration: string;
  date: string;
  tellerId: string;
  status: 'POSTED' | 'PENDING' | 'REJECTED';
  reference: string;
}

export interface ApprovalRequest {
    id: string;
    type: 'TRANSACTION_LIMIT' | 'LOAN_DISBURSEMENT' | 'NEW_USER';
    requesterName: string;
    requestDate: string;
    description: string;
    amount?: number;
    status: 'PENDING' | 'APPROVED' | 'REJECTED';
    payload: any; // Stores data needed to execute the action
}

export interface ComplianceMetric {
  label: string;
  value: string | number;
  threshold: string;
  status: 'pass' | 'warning' | 'fail';
  code: string; // e.g., 'CAR' (Capital Adequacy Ratio)
}

export interface AIInsight {
  title: string;
  description: string;
  severity: 'low' | 'medium' | 'high';
  type: 'compliance' | 'fraud' | 'liquidity';
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'ai';
  content: string;
  timestamp: Date;
}

export interface InsightMetric {
  label: string;
  value: string | number;
  trend: 'up' | 'down' | 'neutral';
  percentage: string;
}

export interface MigrationFieldMapping {
  legacyHeader: string;
  targetDict: string;
  confidence: number; // AI confidence score
}

export interface MigrationLog {
  row: number;
  status: 'SUCCESS' | 'ERROR';
  message: string;
  data: string;
}

export interface AuditLog {
  id: string;
  timestamp: string;
  user: string;
  action: string;
  details: string;
  module: string;
  status: 'SUCCESS' | 'FAILURE' | 'WARNING';
}

export interface GLAccount {
  code: string;
  name: string;
  category: 'ASSET' | 'LIABILITY' | 'EQUITY' | 'INCOME' | 'EXPENSE';
  balance: number;
  currency: string;
  isHeader?: boolean;
}

export interface JournalLine {
  accountCode: string;
  debit: number;
  credit: number;
}

export interface JournalEntry {
  id: string;
  date: string;
  reference: string;
  description: string;
  lines: JournalLine[];
  postedBy: string;
  status: 'POSTED' | 'DRAFT';
}

export interface AmortizationSchedule {
    period: number;
    dueDate: string;
    principal: number;
    interest: number;
    total: number;
    balance: number;
    status: 'PAID' | 'DUE' | 'OVERDUE';
}

export type Permission = 
    | 'USER_READ' | 'USER_WRITE' 
    | 'ACCOUNT_READ' | 'ACCOUNT_WRITE' 
    | 'LOAN_READ' | 'LOAN_WRITE' | 'LOAN_APPROVE' 
    | 'TELLER_POST' 
    | 'GL_READ' | 'GL_POST' | 'GL_MANAGE' 
    | 'SYSTEM_CONFIG' | 'REPORT_VIEW';

export interface Role {
    id: string;
    name: string;
    description: string;
    permissions: Permission[];
}

export interface Branch {
    id: string;
    name: string;
    code: string; // e.g. 201
    location: string;
    status: 'ACTIVE' | 'CLOSED';
}

export interface StaffUser {
  id: string;
  name: string;
  password?: string; // For simulation auth
  roleId: string; // Links to Role.id
  roleName?: string; // Display only
  branchId: string; // Branch link
  email: string;
  phone: string;
  avatarInitials: string;
  status: 'Active' | 'Inactive';
  lastLogin: string;
}

export interface DevTask {
    id: string;
    title: string;
    category: 'BACKEND' | 'FRONTEND' | 'DEVOPS';
    priority: 'HIGH' | 'MEDIUM' | 'LOW';
    status: 'TODO' | 'IN_PROGRESS' | 'DONE';
}

export interface StoredProcedureResult {
    success: boolean;
    output: string;
    data?: any;
    o4wResponse?: O4WResponse; // Added for O4W context
}

export interface O4WResponse {
    status: '200' | '400' | '500';
    json: any;
    headers: Record<string, string>;
}

export interface EodLog {
    timestamp: string;
    step: string;
    message: string;
    status: 'INFO' | 'SUCCESS' | 'ERROR';
}

// System Configuration for Limits
export interface KycLimits {
    maxBalance: number;
    dailyLimit: number;
}

export interface SystemConfig {
    kycLimits: {
        'Tier 1': KycLimits;
        'Tier 2': KycLimits;
        'Tier 3': KycLimits;
    };
    amlThreshold: number;
}

// Business Process Designer Types
export interface WorkflowStep {
    id: string;
    name: string;
    type: 'APPROVAL' | 'NOTIFICATION' | 'SYSTEM_ACTION' | 'DOCUMENT_UPLOAD';
    requiredRoleId?: string; // e.g., 'R004' (Manager)
    description: string;
    order: number;
}

export interface Workflow {
    id: string;
    name: string;
    description: string;
    trigger: 'LOAN_ORIGINATION' | 'TRANSACTION_LIMIT' | 'NEW_ACCOUNT' | 'GL_POSTING';
    isActive: boolean;
    steps: WorkflowStep[];
}
```

### data/mockData.ts
```ts
import { Customer, Account, Loan, Transaction, ComplianceMetric, AuditLog, GLAccount, JournalEntry, DevTask, Group, Product, ApprovalRequest, Branch, StaffUser, Role, Workflow } from '../types';

// Mock Branches
export const BRANCHES: Branch[] = [
    { id: 'BR001', code: '201', name: 'Head Office', location: 'Accra High Street', status: 'ACTIVE' },
    { id: 'BR002', code: '202', name: 'Kumasi Main', location: 'Adum, Kumasi', status: 'ACTIVE' },
    { id: 'BR003', code: '203', name: 'Tamale Branch', location: 'Tamale Central', status: 'ACTIVE' }
];

// Mock Customers (CIF)
export const CUSTOMERS: Customer[] = Array.from({ length: 50 }, (_, i) => ({
  id: `CIF${100000 + i}`,
  name: `Customer ${i + 1}`,
  ghanaCard: `GHA-${700000000 + i}-1`,
  digitalAddress: `GA-${100 + i}-${2000 + i}`,
  kycLevel: i % 5 === 0 ? 'Tier 3' : 'Tier 2',
  phone: `+233${200000000 + i}`,
  email: `cust${i}@example.com`,
  riskRating: i === 4 ? 'High' : 'Low',
}));

// Mock Groups
export const GROUPS: Group[] = [
    {
        id: 'GRP-001',
        name: 'Makola Market Women A',
        officer: 'STF003',
        meetingDay: 'Tuesday',
        formationDate: '2023-01-15',
        members: ['CIF100000', 'CIF100001', 'CIF100002'],
        status: 'ACTIVE'
    },
    {
        id: 'GRP-002',
        name: 'Circle Spare Parts Assoc',
        officer: 'STF003',
        meetingDay: 'Thursday',
        formationDate: '2023-03-10',
        members: ['CIF100005', 'CIF100006'],
        status: 'ACTIVE'
    }
];

// Mock Products (New)
export const PRODUCTS: Product[] = [
    // SAVINGS
    { id: 'SA-100', name: 'Ordinary Savings', description: 'Standard savings account for individuals', type: 'SAVINGS', currency: 'GHS', interestRate: 5.0, minAmount: 50, interestMethod: 'COMPOUND', status: 'ACTIVE' },
    { id: 'SA-200', name: 'High Yield Savings', description: 'Tiered interest for higher balances', type: 'SAVINGS', currency: 'GHS', interestRate: 12.5, minAmount: 1000, interestMethod: 'COMPOUND', status: 'ACTIVE' },
    
    // CURRENT
    { id: 'CA-100', name: 'Individual Current', description: 'Zero interest transactional account', type: 'CURRENT', currency: 'GHS', interestRate: 0, minAmount: 0, interestMethod: 'NONE', status: 'ACTIVE' },
    { id: 'CA-200', name: 'Business Current', description: 'Corporate transactional account', type: 'CURRENT', currency: 'GHS', interestRate: 0, minAmount: 500, interestMethod: 'NONE', status: 'ACTIVE' },
    
    // FIXED DEPOSIT
    { id: 'FD-091', name: 'Fixed Deposit (91 Day)', description: 'Short term investment', type: 'FIXED_DEPOSIT', currency: 'GHS', interestRate: 18.0, minAmount: 5000, defaultTerm: 3, minTerm: 3, maxTerm: 3, interestMethod: 'FLAT', status: 'ACTIVE' },
    
    // LOANS
    { id: 'LN-SME', name: 'SME Business Loan', description: 'Working capital for small businesses', type: 'LOAN', currency: 'GHS', interestRate: 28.5, minAmount: 10000, maxAmount: 500000, defaultTerm: 12, minTerm: 6, maxTerm: 24, interestMethod: 'REDUCING_BALANCE', status: 'ACTIVE' },
    { id: 'LN-PER', name: 'Personal Salary Loan', description: 'Consumer loan against salary', type: 'LOAN', currency: 'GHS', interestRate: 32.5, minAmount: 1000, maxAmount: 50000, defaultTerm: 6, minTerm: 3, maxTerm: 12, interestMethod: 'FLAT', status: 'ACTIVE' },
    { id: 'LN-GRP', name: 'Micro-Finance Group Loan', description: 'Joint liability group lending', type: 'LOAN', currency: 'GHS', interestRate: 45.0, minAmount: 500, maxAmount: 5000, defaultTerm: 4, minTerm: 4, maxTerm: 6, interestMethod: 'FLAT', status: 'ACTIVE' },
    { id: 'LN-MTG', name: 'Staff Mortgage Scheme', description: 'Long term housing finance', type: 'LOAN', currency: 'GHS', interestRate: 12.0, minAmount: 50000, maxAmount: 1000000, defaultTerm: 60, minTerm: 12, maxTerm: 120, interestMethod: 'REDUCING_BALANCE', status: 'ACTIVE' }
];

// Mock Accounts
export const ACCOUNTS: Account[] = CUSTOMERS.map((cust, i) => ({
  id: `201${100000 + i}01`, // Branch 201, Sequence, Check digit
  cif: cust.id,
  branchId: i % 10 === 0 ? 'BR002' : 'BR001',
  type: i % 3 === 0 ? 'CURRENT' : 'SAVINGS',
  currency: 'GHS',
  balance: Math.floor(Math.random() * 50000) + 100,
  lienAmount: i % 10 === 0 ? 500 : 0,
  status: i % 20 === 0 ? 'DORMANT' : 'ACTIVE',
  productCode: i % 3 === 0 ? 'CA-101' : 'SA-202',
  lastTransDate: new Date().toISOString().split('T')[0],
  mandate: {
      instructions: i % 3 === 0 ? "Any two to sign" : "Sole Signatory",
      signatories: [
          { 
              name: cust.name, 
              role: "Account Holder", 
              // Mock signature/photo would be URLs in real app
              signatureUrl: "signature-placeholder", 
              photoUrl: "photo-placeholder"
          },
          ...(i % 3 === 0 ? [{ name: "Partner Signatory", role: "Joint Holder" }] : [])
      ]
  }
}));

// Mock Loans
export const LOANS: Loan[] = Array.from({ length: 15 }, (_, i) => ({
  id: `LN${500000 + i}`,
  cif: CUSTOMERS[i].id,
  productName: i % 2 === 0 ? 'SME Business Loan' : 'Personal Salary Loan',
  productCode: i % 2 === 0 ? 'LN-SME' : 'LN-PER',
  principal: 10000 + (i * 1000),
  rate: 28.5, // Annual Rate
  termMonths: 12,
  disbursementDate: '2023-10-01',
  parBucket: i === 0 ? '91+' : '0',
  outstandingBalance: 8000 + (i * 500),
  collateralType: 'Landed Property',
  status: i === 0 ? 'WRITTEN_OFF' : 'ACTIVE'
}));

// Mock GL Accounts (Chart of Accounts)
export const GL_ACCOUNTS: GLAccount[] = [
  // Assets
  { code: '10000', name: 'ASSETS', category: 'ASSET', balance: 0, currency: 'GHS', isHeader: true },
  { code: '10100', name: 'Vault Cash - Main', category: 'ASSET', balance: 450000, currency: 'GHS' },
  { code: '10101', name: 'ATM Cash 01', category: 'ASSET', balance: 120000, currency: 'GHS' },
  { code: '10200', name: 'BoG Clearing Account', category: 'ASSET', balance: 2500000, currency: 'GHS' },
  { code: '12000', name: 'Loan Portfolio - SME', category: 'ASSET', balance: 8400000, currency: 'GHS' },
  
  // Liabilities
  { code: '20000', name: 'LIABILITIES', category: 'LIABILITY', balance: 0, currency: 'GHS', isHeader: true },
  { code: '20100', name: 'Customer Deposits - Savings', category: 'LIABILITY', balance: 3500000, currency: 'GHS' },
  { code: '20101', name: 'Customer Deposits - Current', category: 'LIABILITY', balance: 1200000, currency: 'GHS' },
  
  // Equity
  { code: '30000', name: 'EQUITY', category: 'EQUITY', balance: 0, currency: 'GHS', isHeader: true },
  { code: '30100', name: 'Share Capital', category: 'EQUITY', balance: 1000000, currency: 'GHS' },
  { code: '30200', name: 'Retained Earnings', category: 'EQUITY', balance: 500000, currency: 'GHS' },

  // Income
  { code: '40000', name: 'INCOME', category: 'INCOME', balance: 0, currency: 'GHS', isHeader: true },
  { code: '40100', name: 'Interest Income - Loans', category: 'INCOME', balance: 450000, currency: 'GHS' },
  { code: '40200', name: 'Fees & Commissions', category: 'INCOME', balance: 85000, currency: 'GHS' },

  // Expense
  { code: '50000', name: 'EXPENSES', category: 'EXPENSE', balance: 0, currency: 'GHS', isHeader: true },
  { code: '50100', name: 'Staff Salaries', category: 'EXPENSE', balance: 200000, currency: 'GHS' },
  { code: '50200', name: 'Utility Bills', category: 'EXPENSE', balance: 15000, currency: 'GHS' },
];

export const JOURNAL_ENTRIES: JournalEntry[] = [
  {
    id: 'JV-20231024-001',
    date: '2023-10-24',
    reference: 'OPENING-BAL',
    description: 'Opening Balances Migration',
    postedBy: 'SYS_ADMIN',
    status: 'POSTED',
    lines: [
      { accountCode: '10100', debit: 450000, credit: 0 },
      { accountCode: '30100', debit: 0, credit: 450000 }
    ]
  }
];

// Compliance Metrics (BoG Standards)
export const COMPLIANCE_METRICS: ComplianceMetric[] = [
  { label: 'Capital Adequacy Ratio (CAR)', value: '14.2%', threshold: '> 13%', status: 'pass', code: 'PRUD-01' },
  { label: 'Non-Performing Loans (NPL)', value: '8.5%', threshold: '< 10%', status: 'warning', code: 'ASSET-Q' },
  { label: 'Liquidity Ratio', value: '45%', threshold: '> 35%', status: 'pass', code: 'LIQ-05' },
  { label: 'Unmatched FX Position', value: '$120k', threshold: '< $100k', status: 'fail', code: 'FX-EXP' },
];

// Mock Audit Logs
export const AUDIT_LOGS: AuditLog[] = Array.from({ length: 30 }, (_, i) => {
  const actions = ['USER_LOGIN', 'CASH_DEPOSIT', 'CASH_WITHDRAWAL', 'SYSTEM_BACKUP', 'RATE_UPDATE', 'GL_POSTING'];
  const modules = ['SECURITY', 'TELLER', 'TELLER', 'SYSTEM', 'TREASURY', 'GL'];
  const users = ['TLR001', 'TLR002', 'SYS_ADMIN', 'MGR_OPS'];
  const statuses: ('SUCCESS' | 'FAILURE' | 'WARNING')[] = ['SUCCESS', 'SUCCESS', 'SUCCESS', 'SUCCESS', 'FAILURE', 'WARNING'];
  
  const rand = Math.floor(Math.random() * actions.length);
  const statusRand = Math.floor(Math.random() * statuses.length);

  return {
    id: `AUD-${Date.now() - i * 900000}`,
    timestamp: new Date(Date.now() - i * 900000).toISOString(),
    user: users[Math.floor(Math.random() * users.length)],
    action: actions[rand],
    details: `Event executed on partition DATA_VOL. Ref: ${Math.random().toString(36).substring(7).toUpperCase()}`,
    module: modules[rand],
    status: statuses[statusRand]
  };
});

// Mock Dev Tasks
export const DEV_TASKS: DevTask[] = [
    { id: '1', title: 'Implement JWT Authentication for O4W API', category: 'BACKEND', priority: 'HIGH', status: 'TODO' },
    { id: '2', title: 'Create Linear Hash File System Wrapper in Node.js', category: 'BACKEND', priority: 'HIGH', status: 'IN_PROGRESS' },
    { id: '3', title: 'Integrate BoG ORASS XML Export Module', category: 'BACKEND', priority: 'MEDIUM', status: 'DONE' },
    { id: '4', title: 'Refactor React Components to use React Query', category: 'FRONTEND', priority: 'MEDIUM', status: 'TODO' },
    { id: '5', title: 'Set up Docker container for OpenInsight Engine', category: 'DEVOPS', priority: 'HIGH', status: 'DONE' },
    { id: '6', title: 'Implement Real-time Websockets for Teller Updates', category: 'FRONTEND', priority: 'LOW', status: 'DONE' },
    { id: '7', title: 'Build GL Chart of Accounts Tree View', category: 'FRONTEND', priority: 'MEDIUM', status: 'DONE' },
];

export const APPROVAL_REQUESTS: ApprovalRequest[] = [
    {
        id: 'REQ-1001',
        type: 'TRANSACTION_LIMIT',
        requesterName: 'Kwame Teller',
        requestDate: new Date(Date.now() - 3600000).toISOString(),
        description: 'Withdrawal exceeds limit for Tier 2 Customer',
        amount: 25000,
        status: 'PENDING',
        payload: {
            accountId: '20110000101',
            type: 'WITHDRAWAL',
            amount: 25000,
            narration: 'Urgent Medical Bills',
            tellerId: 'STF002'
        }
    },
    {
        id: 'REQ-1002',
        type: 'LOAN_DISBURSEMENT',
        requesterName: 'Loan Officer',
        requestDate: new Date(Date.now() - 7200000).toISOString(),
        description: 'Disburse SME Loan to GRP-001',
        amount: 50000,
        status: 'APPROVED',
        payload: { loanId: 'LN500001' }
    }
];

// Initial Roles
export const ROLES: Role[] = [
    { 
        id: 'R001', 
        name: 'Super Administrator', 
        description: 'Full system access', 
        permissions: ['USER_READ', 'USER_WRITE', 'ACCOUNT_READ', 'ACCOUNT_WRITE', 'LOAN_READ', 'LOAN_WRITE', 'LOAN_APPROVE', 'TELLER_POST', 'GL_READ', 'GL_POST', 'GL_MANAGE', 'SYSTEM_CONFIG', 'REPORT_VIEW'] 
    },
    { 
        id: 'R002', 
        name: 'Teller', 
        description: 'Front desk cash operations', 
        permissions: ['ACCOUNT_READ', 'TELLER_POST'] 
    },
    { 
        id: 'R003', 
        name: 'Loan Officer', 
        description: 'Loan origination and portfolio management', 
        permissions: ['ACCOUNT_READ', 'LOAN_READ', 'LOAN_WRITE', 'REPORT_VIEW'] 
    },
    { 
        id: 'R004', 
        name: 'Branch Manager', 
        description: 'Approvals and overrides', 
        permissions: ['ACCOUNT_READ', 'ACCOUNT_WRITE', 'LOAN_READ', 'LOAN_APPROVE', 'TELLER_POST', 'REPORT_VIEW'] 
    }
];

// Initial Staff
export const STAFF: StaffUser[] = [
    { id: 'STF001', name: 'Admin User', roleId: 'R001', roleName: 'Super Administrator', branchId: 'BR001', email: 'admin@openinsight.bank', phone: '0200000001', avatarInitials: 'AD', status: 'Active', lastLogin: new Date().toLocaleString(), password: 'password' },
    { id: 'STF002', name: 'Kwame Teller', roleId: 'R002', roleName: 'Teller', branchId: 'BR001', email: 'kwame@openinsight.bank', phone: '0200000002', avatarInitials: 'KT', status: 'Active', lastLogin: new Date().toLocaleString(), password: 'password' },
    { id: 'STF003', name: 'Adwoa Manager', roleId: 'R004', roleName: 'Branch Manager', branchId: 'BR002', email: 'adwoa@openinsight.bank', phone: '0200000003', avatarInitials: 'AM', status: 'Active', lastLogin: new Date().toLocaleString(), password: 'password' },
];

// Mock Workflows
export const WORKFLOWS: Workflow[] = [
    {
        id: 'WF-001',
        name: 'High Value Loan Approval',
        description: 'Approval chain for SME loans > 50,000 GHS',
        trigger: 'LOAN_ORIGINATION',
        isActive: true,
        steps: [
            { id: 'S1', name: 'Risk Assessment', type: 'DOCUMENT_UPLOAD', description: 'Upload Credit Bureau Report', order: 1 },
            { id: 'S2', name: 'Supervisor Review', type: 'APPROVAL', requiredRoleId: 'R003', description: 'Initial review by Senior Loan Officer', order: 2 },
            { id: 'S3', name: 'Manager Approval', type: 'APPROVAL', requiredRoleId: 'R004', description: 'Final authorization by Branch Manager', order: 3 },
            { id: 'S4', name: 'Disbursement Notice', type: 'NOTIFICATION', description: 'Notify client via SMS', order: 4 }
        ]
    },
    {
        id: 'WF-002',
        name: 'Large Withdrawal Override',
        description: 'Teller override for withdrawals > 20,000 GHS',
        trigger: 'TRANSACTION_LIMIT',
        isActive: true,
        steps: [
            { id: 'S1', name: 'Manager Authorization', type: 'APPROVAL', requiredRoleId: 'R004', description: 'Verify source of funds and ID', order: 1 }
        ]
    }
];

export const MOCK_DATA = {
  branches: BRANCHES,
  customers: CUSTOMERS,
  groups: GROUPS,
  products: PRODUCTS,
  accounts: ACCOUNTS,
  loans: LOANS,
  glAccounts: GL_ACCOUNTS,
  journalEntries: JOURNAL_ENTRIES,
  compliance: COMPLIANCE_METRICS,
  auditLogs: AUDIT_LOGS,
  devTasks: DEV_TASKS,
  approvalRequests: APPROVAL_REQUESTS,
  roles: ROLES,
  staff: STAFF,
  workflows: WORKFLOWS
};
```

### services/geminiService.ts
```ts
import { GoogleGenAI, Type } from "@google/genai";
import { Account, AIInsight, Customer, Loan, MigrationFieldMapping } from '../types';

const getClient = () => new GoogleGenAI({ apiKey: process.env.API_KEY });

export const generateDashboardInsights = async (data: any): Promise<AIInsight[]> => {
  try {
    const ai = getClient();
    // Summarize critical data points
    const context = {
       liquidity: "45%", // Mock
       unmatchedFX: "120000 USD",
       nplRatio: "8.5%",
       totalDeposits: 45000000
    };

    const response = await ai.models.generateContent({
      model: 'gemini-3-flash-preview',
      contents: `You are the Core Banking System Architect for a Bank of Ghana licensed institution.
      Analyze the following Bank Key Risk Indicators (KRIs):
      ${JSON.stringify(context)}
      
      Identify 3 critical insights related to:
      1. Liquidity compliance (BoG requirement > 35%).
      2. FX Exposure limits.
      3. Credit Risk (NPL).
      
      Return purely in JSON format.`,
      config: {
        responseMimeType: "application/json",
        responseSchema: {
          type: Type.ARRAY,
          items: {
            type: Type.OBJECT,
            properties: {
              title: { type: Type.STRING },
              description: { type: Type.STRING },
              severity: { type: Type.STRING, enum: ['low', 'medium', 'high'] },
              type: { type: Type.STRING, enum: ['compliance', 'fraud', 'liquidity'] }
            },
            required: ['title', 'description', 'severity', 'type']
          }
        }
      }
    });

    if (response.text) {
        return JSON.parse(response.text) as AIInsight[];
    }
    return [];
  } catch (error) {
    console.error("Error generating banking insights:", error);
    return [];
  }
};

export const chatWithOpenInsight = async (history: string[], currentMessage: string, contextData: any): Promise<string> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are an Expert OpenInsight Developer specializing in Core Banking Systems (T24, Flexcube style logic but built on Linear Hash).
        
        You understand:
        - BoG (Bank of Ghana) Prudential Returns.
        - Chart of Accounts (General Ledger).
        - Basic+ Stored Procedures.
        - Linear Hash Dictionaries (Dict.MFS).
        
        Chat History:
        ${history.join('\n')}
        
        User: ${currentMessage}
        
        Answer professionally as a Senior Systems Analyst.
        `;

        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        return response.text || "System Busy.";
    } catch (error) {
        return "Error connecting to Core Banking AI.";
    }
};

export const runBasicPlusCode = async (code: string, contextData: any): Promise<string> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are the OpenInsight 10.2 Engine.
        Execute the following Basic+ Code.
        
        Context:
        - The code is likely calculating Loan Interest (Reducing Balance) or formatting JSON for O4W.
        - Return the printed output or the return value.
        - If the code uses 'Calculations', perform them accurately.
        
        Code:
        ${code}
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        return response.text || "No output.";
    } catch (error) {
        return "FS109: SYS_NET_ERR";
    }
};

export const autoMapMigrationFields = async (legacyHeaders: string[], targetDictionaries: string[]): Promise<MigrationFieldMapping[]> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are a Data Migration Specialist moving legacy banking data to OpenInsight.
        
        Task: Map the 'Legacy Headers' to the best matching 'Target Dictionary' field.
        
        Legacy Headers: ${JSON.stringify(legacyHeaders)}
        Target Dictionaries: ${JSON.stringify(targetDictionaries)}
        
        Return JSON mapping.
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
            config: {
                responseMimeType: "application/json",
                responseSchema: {
                    type: Type.ARRAY,
                    items: {
                        type: Type.OBJECT,
                        properties: {
                            legacyHeader: { type: Type.STRING },
                            targetDict: { type: Type.STRING },
                            confidence: { type: Type.NUMBER, description: "0 to 1 confidence" }
                        },
                        required: ['legacyHeader', 'targetDict', 'confidence']
                    }
                }
            }
        });

        if (response.text) {
            return JSON.parse(response.text) as MigrationFieldMapping[];
        }
        return [];
    } catch (error) {
        console.error("Mapping error", error);
        // Fallback default mapping
        return legacyHeaders.map(h => ({ legacyHeader: h, targetDict: '', confidence: 0 }));
    }
};

export const generateMigrationBasicPlusCode = async (mappings: MigrationFieldMapping[], targetTable: string = "DATA_VOL"): Promise<string> => {
    try {
        const ai = getClient();
        
        const validMappings = mappings.filter(m => m.targetDict && m.targetDict !== '');
        
        const prompt = `
        System: You are an expert OpenInsight Basic+ Programmer.
        
        Task: Generate a robust Basic+ Stored Procedure to import CSV data into the '${targetTable}' table.
        
        Mappings provided (Legacy Header -> OI Dictionary):
        ${JSON.stringify(validMappings)}
        
        Code Requirements:
        1. Function Name: IMPORT_BATCH_${new Date().getTime().toString().slice(-6)}
        2. Open the table '${targetTable}'.
        3. Use OSREAD to load 'C:\\TEMP\\IMPORT_DATA.CSV'.
        4. Loop through lines (Swap CHAR(13):CHAR(10) with @FM).
        5. For each line:
           - Parse CSV columns (handle quotes if possible, or simple Swap).
           - Assign values to specific Field Marks (FM) based on the mappings.
             (Assign plausible Field Numbers, e.g., <1>, <2> etc for the dictionaries provided in the mapping).
           - Use 'Write Rec on FileVar, ID'
        6. Add comments explaining the mapping.
        7. Return a status string "Imported X records".
        8. Include error handling for file not found.
        
        Output: Return ONLY the Basic+ code block.
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        return response.text || "REM Code generation failed.";
    } catch (error) {
        console.error("Code generation error", error);
        return "REM Error connecting to AI service.";
    }
};

export const generateBasicPlusCode = async (description: string, template: string): Promise<string> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are an expert OpenInsight Basic+ (RevG) Developer.
        
        Task: Generate a robust, compilable Basic+ module.
        
        Template Context: ${template}
        User Requirements: ${description}
        
        Technical Constraints:
        - For 'O4W API Endpoint': Assume incoming JSON string in 'Request', return JSON string. Use 'O4WJSON' parser if needed or simple string manipulation.
        - For 'Validation': Return 1 (Valid) or 0 (Invalid) and set 'ANS' or specific error variable.
        - For 'Dictionary MFS': Handle standard MFS codes (READ, WRITE, DELETE).
        - Syntax: Must be valid Basic+. Use 'Compile Function' or 'Compile Subroutine'.
        - Include comments explaining the logic.
        
        Output: RAW CODE ONLY. Do not use markdown backticks.
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        let code = response.text || "REM Generation Failed";
        // Cleanup markdown
        code = code.replace(/```basic\+?/gi, '').replace(/```/g, '').trim();
        return code;
    } catch (error) {
        console.error("AI Gen Error", error);
        return "REM Error connecting to AI Service";
    }
};
```

### components/StatCard.tsx
```tsx
import React from 'react';
import { ArrowUpRight, ArrowDownRight, Minus } from 'lucide-react';
import { InsightMetric } from '../types';

interface StatCardProps {
  metric: InsightMetric;
  icon: React.ReactNode;
  color?: 'blue' | 'green' | 'orange' | 'red';
}

const StatCard: React.FC<StatCardProps> = ({ metric, icon, color = 'blue' }) => {
  const isUp = metric.trend === 'up';
  const isDown = metric.trend === 'down';

  const colorClasses = {
    blue: 'border-l-blue-500 text-blue-600 bg-blue-50',
    green: 'border-l-green-500 text-green-600 bg-green-50',
    orange: 'border-l-orange-500 text-orange-600 bg-orange-50',
    red: 'border-l-red-500 text-red-600 bg-red-50'
  };

  const iconBgClasses = {
    blue: 'bg-blue-100',
    green: 'bg-green-100',
    orange: 'bg-orange-100',
    red: 'bg-red-100'
  };

  return (
    <div className={`bg-white p-5 rounded shadow-sm border border-gray-200 border-l-4 ${colorClasses[color].split(' ')[0]}`}>
      <div className="flex justify-between items-start">
        <div>
           <p className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-1">{metric.label}</p>
           <h3 className="text-2xl font-bold text-gray-800">{metric.value}</h3>
           
           <div className="flex items-center mt-2 text-xs font-medium">
             {isUp && <span className="text-green-600 flex items-center"><ArrowUpRight size={12} className="mr-1"/> {metric.percentage}</span>}
             {isDown && <span className="text-red-600 flex items-center"><ArrowDownRight size={12} className="mr-1"/> {metric.percentage}</span>}
             {metric.trend === 'neutral' && <span className="text-gray-400 flex items-center"><Minus size={12} className="mr-1"/> {metric.percentage}</span>}
             <span className="text-gray-400 ml-1">vs last month</span>
           </div>
        </div>
        <div className={`p-3 rounded-full ${iconBgClasses[color]} ${colorClasses[color].split(' ')[1]}`}>
          {icon}
        </div>
      </div>
    </div>
  );
};

export default StatCard;
```

### components/DataGrid.tsx
```tsx
import React, { useState } from 'react';
import { Account } from '../types';
import { Search, Filter, Download, Book, MoreHorizontal } from 'lucide-react';

interface DataGridProps {
  data: Account[];
}

const DataGrid: React.FC<DataGridProps> = ({ data }) => {
  const [searchTerm, setSearchTerm] = useState('');

  const filteredData = data.filter(item => 
    item.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
    item.cif.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleExport = () => {
    if (filteredData.length === 0) {
      alert("No data to export.");
      return;
    }

    // Define CSV Headers
    const headers = ['Account No', 'Client (CIF)', 'Type', 'Product Code', 'Balance', 'Lien Amount', 'Status', 'Last Transaction'];
    
    // Map data to CSV rows
    const csvRows = filteredData.map(row => {
      return [
        `"${row.id}"`,
        `"${row.cif}"`,
        `"${row.type}"`,
        `"${row.productCode}"`,
        row.balance.toFixed(2),
        row.lienAmount.toFixed(2),
        `"${row.status}"`,
        `"${row.lastTransDate}"`
      ].join(',');
    });

    // Combine headers and rows
    const csvContent = [headers.join(','), ...csvRows].join('\n');

    // Create a Blob and trigger download
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `accounts_export_${new Date().toISOString().split('T')[0]}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  return (
    <div className="flex flex-col h-full">
      {/* Table Toolbar */}
      <div className="p-4 bg-gray-50 border-b border-gray-200 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-2">
           <div className="relative">
             <input 
               type="text" 
               placeholder="Filter by name or account..." 
               className="pl-3 pr-4 py-1.5 border border-gray-300 rounded text-sm w-64 focus:outline-none focus:border-blue-500"
               value={searchTerm}
               onChange={(e) => setSearchTerm(e.target.value)}
             />
           </div>
        </div>
        
        <div className="flex items-center gap-2">
          <button className="flex items-center gap-1 px-3 py-1.5 bg-white border border-gray-300 rounded text-sm text-gray-600 hover:bg-gray-50">
            <Filter size={14} /> Filter
          </button>
          <button 
            onClick={handleExport}
            className="flex items-center gap-1 px-3 py-1.5 bg-white border border-gray-300 rounded text-sm text-gray-600 hover:bg-gray-50"
          >
            <Download size={14} /> Export
          </button>
        </div>
      </div>
      
      {/* Table Area */}
      <div className="overflow-auto flex-1">
        <table className="w-full text-left text-sm border-collapse">
          <thead className="bg-gray-100 text-gray-600 font-semibold sticky top-0 border-b border-gray-200">
            <tr>
              <th className="px-4 py-3 text-xs uppercase tracking-wider">Account No</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider">Client (CIF)</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider">Product</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider text-right">Balance</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider text-right">Lien</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider text-center">Status</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider text-center">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {filteredData.slice(0, 50).map((row, idx) => (
              <tr key={row.id} className={`hover:bg-blue-50 transition-colors ${idx % 2 === 0 ? 'bg-white' : 'bg-gray-50'}`}>
                <td className="px-4 py-2.5 font-medium text-blue-600 cursor-pointer hover:underline">{row.id}</td>
                <td className="px-4 py-2.5 text-gray-700">{row.cif}</td>
                <td className="px-4 py-2.5 text-gray-600">
                   {row.type} <span className="text-xs text-gray-400 ml-1">({row.productCode})</span>
                </td>
                <td className="px-4 py-2.5 text-right font-medium text-gray-800">{row.balance.toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                <td className="px-4 py-2.5 text-right text-gray-500">{row.lienAmount > 0 ? row.lienAmount.toLocaleString() : '-'}</td>
                <td className="px-4 py-2.5 text-center">
                  <span className={`inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase
                    ${row.status === 'ACTIVE' ? 'bg-green-100 text-green-700' : 
                      row.status === 'DORMANT' ? 'bg-yellow-100 text-yellow-700' : 
                      'bg-red-100 text-red-700'}`}>
                    {row.status}
                  </span>
                </td>
                <td className="px-4 py-2.5 text-center">
                   <button className="text-gray-400 hover:text-blue-600">
                      <MoreHorizontal size={16} />
                   </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {filteredData.length === 0 && (
            <div className="p-8 text-center text-gray-500 bg-white">No records found.</div>
        )}
      </div>
      <div className="bg-gray-50 border-t border-gray-200 p-2 text-xs text-gray-500 text-right">
         Showing {filteredData.length} entries
      </div>
    </div>
  );
};

export default DataGrid;
```

### components/InsightPanel.tsx
```tsx
import React from 'react';
import { AIInsight } from '../types';
import { AlertTriangle, TrendingUp, Lightbulb, Loader2 } from 'lucide-react';

interface InsightPanelProps {
  insights: AIInsight[];
  loading: boolean;
  onRefresh: () => void;
}

const InsightPanel: React.FC<InsightPanelProps> = ({ insights, loading, onRefresh }) => {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-100 h-full flex flex-col">
      <div className="p-4 border-b border-gray-100 flex justify-between items-center">
        <h2 className="text-lg font-semibold text-gray-800 flex items-center gap-2">
          <Lightbulb className="text-yellow-500" size={20} />
          BankInsight Engine
        </h2>
        <button 
          onClick={onRefresh}
          disabled={loading}
          className="text-sm text-indigo-600 hover:text-indigo-800 font-medium flex items-center gap-1 disabled:opacity-50"
        >
          {loading ? <Loader2 className="animate-spin" size={14} /> : null}
          {loading ? 'Analyzing...' : 'Refresh Insights'}
        </button>
      </div>

      <div className="p-4 space-y-4 overflow-y-auto flex-1">
        {loading && insights.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-40 text-gray-400">
             <Loader2 className="animate-spin mb-2" size={32} />
             <p>Running deep analysis...</p>
          </div>
        ) : (
          insights.map((insight, idx) => (
            <div key={idx} className={`p-4 rounded-lg border-l-4 ${
              insight.severity === 'high' ? 'bg-red-50 border-red-500' :
              insight.severity === 'medium' ? 'bg-orange-50 border-orange-500' :
              'bg-blue-50 border-blue-500'
            }`}>
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-2 mb-1">
                  {insight.type === 'fraud' && <AlertTriangle size={16} className="text-red-600" />}
                  {insight.type === 'liquidity' && <TrendingUp size={16} className="text-blue-600" />}
                  {insight.type === 'compliance' && <Lightbulb size={16} className="text-orange-600" />}
                  <span className="font-semibold text-gray-900 text-sm uppercase tracking-wide">{insight.type}</span>
                </div>
                <span className={`text-xs px-2 py-0.5 rounded-full font-medium border ${
                    insight.severity === 'high' ? 'border-red-200 text-red-700' :
                    insight.severity === 'medium' ? 'border-orange-200 text-orange-700' :
                    'border-blue-200 text-blue-700'
                }`}>
                  {insight.severity} Priority
                </span>
              </div>
              <h3 className="text-gray-900 font-medium mb-1">{insight.title}</h3>
              <p className="text-gray-600 text-sm leading-relaxed">
                {insight.description}
              </p>
            </div>
          ))
        )}
        {!loading && insights.length === 0 && (
            <div className="text-center text-gray-500 py-10">
                Ready to analyze data. Click refresh.
            </div>
        )}
      </div>
    </div>
  );
};

export default InsightPanel;
```

### components/ChatInterface.tsx
```tsx
import React, { useState, useRef, useEffect } from 'react';
import { Send, Bot, User, Sparkles } from 'lucide-react';
import { ChatMessage, Account } from '../types';
import { chatWithOpenInsight } from '../services/geminiService';

interface ChatInterfaceProps {
    data: Account[];
}

const ChatInterface: React.FC<ChatInterfaceProps> = ({ data }) => {
    const [messages, setMessages] = useState<ChatMessage[]>([
        { id: '1', role: 'ai', content: 'Welcome to OpenInsight O4W. I can help you generate RList queries, write Basic+ code, or analyze your Linear Hash data.', timestamp: new Date() }
    ]);
    const [input, setInput] = useState('');
    const [isTyping, setIsTyping] = useState(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };

    useEffect(() => {
        scrollToBottom();
    }, [messages]);

    const handleSend = async () => {
        if (!input.trim() || isTyping) return;

        const userMsg: ChatMessage = {
            id: Date.now().toString(),
            role: 'user',
            content: input,
            timestamp: new Date()
        };

        setMessages(prev => [...prev, userMsg]);
        setInput('');
        setIsTyping(true);

        // Prepare history for context
        const historyStrings = messages.map(m => `${m.role.toUpperCase()}: ${m.content}`);

        const responseText = await chatWithOpenInsight(historyStrings, userMsg.content, data);

        const aiMsg: ChatMessage = {
            id: (Date.now() + 1).toString(),
            role: 'ai',
            content: responseText,
            timestamp: new Date()
        };

        setMessages(prev => [...prev, aiMsg]);
        setIsTyping(false);
    };

    return (
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 flex flex-col h-full">
            <div className="p-4 border-b border-gray-100 bg-gradient-to-r from-blue-800 to-blue-900 rounded-t-xl text-white">
                <h2 className="font-semibold flex items-center gap-2">
                    <Sparkles size={18} className="text-yellow-400" />
                    O4W AI Assistant
                </h2>
                <p className="text-blue-200 text-xs mt-1">OpenInsight 10.2 / Gemini 3</p>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-4">
                {messages.map((msg) => (
                    <div key={msg.id} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                        <div className={`flex gap-3 max-w-[85%] ${msg.role === 'user' ? 'flex-row-reverse' : 'flex-row'}`}>
                            <div className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${
                                msg.role === 'user' ? 'bg-gray-200' : 'bg-blue-100 text-blue-800'
                            }`}>
                                {msg.role === 'user' ? <User size={16} /> : <Bot size={16} />}
                            </div>
                            <div className={`p-3 rounded-2xl text-sm ${
                                msg.role === 'user' 
                                ? 'bg-gray-900 text-white rounded-tr-none' 
                                : 'bg-gray-100 text-gray-800 rounded-tl-none'
                            }`}>
                                {msg.content}
                            </div>
                        </div>
                    </div>
                ))}
                {isTyping && (
                    <div className="flex justify-start">
                        <div className="flex gap-3 max-w-[85%]">
                             <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-800 flex items-center justify-center">
                                <Bot size={16} />
                            </div>
                            <div className="bg-gray-100 p-3 rounded-2xl rounded-tl-none text-gray-500 text-sm italic">
                                Processing RList request...
                            </div>
                        </div>
                    </div>
                )}
                <div ref={messagesEndRef} />
            </div>

            <div className="p-3 border-t border-gray-100">
                <div className="relative flex items-center">
                    <input
                        type="text"
                        value={input}
                        onChange={(e) => setInput(e.target.value)}
                        onKeyPress={(e) => e.key === 'Enter' && handleSend()}
                        placeholder="Ask about records, or type an RList command..."
                        className="w-full pl-4 pr-12 py-3 bg-gray-50 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                    />
                    <button 
                        onClick={handleSend}
                        disabled={!input.trim() || isTyping}
                        className="absolute right-2 p-2 bg-blue-800 text-white rounded-lg hover:bg-blue-900 disabled:opacity-50 transition-colors"
                    >
                        <Send size={16} />
                    </button>
                </div>
            </div>
        </div>
    );
};

export default ChatInterface;
```

### App.tsx
```tsx
import React, { useEffect, useState, useMemo } from 'react';
import { useBankingSystem } from './hooks/useBankingSystem'; // Import the new engine
import { AIInsight, StaffUser } from './types';
import { generateDashboardInsights } from './services/geminiService';
import StatCard from './components/StatCard';
import DataGrid from './components/DataGrid';
import InsightPanel from './components/InsightPanel';
import BasicPlusEditor from './components/BasicPlusEditor';
import TellerTerminal from './components/TellerTerminal';
import CompliancePanel from './components/CompliancePanel';
import MigrationWizard from './components/MigrationWizard';
import AuditTrail from './components/AuditTrail';
import LoanEngine from './components/LoanEngine';
import AccountingEngine from './components/AccountingEngine';
import ClientManager from './components/ClientManager'; // Import new manager
import TransactionExplorer from './components/TransactionExplorer'; // Import Transaction Explorer
import UserProfile from './components/UserProfile';
import Settings from './components/Settings'; // New Settings Component
import DevelopmentTasks from './components/DevelopmentTasks'; // New Tasks Component
import EodConsole from './components/EodConsole'; // Import EOD Console
import GroupManager from './components/GroupManager'; // Import Group Manager
import ProductDesigner from './components/ProductDesigner'; // Import Product Designer
import ApprovalInbox from './components/ApprovalInbox'; // Import Approval Inbox
import LoginScreen from './components/LoginScreen'; // New Login
import StatementVerification from './components/StatementVerification'; // New Module

import { 
  LayoutDashboard, 
  Users, 
  Landmark, 
  FileText, 
  Settings as SettingsIcon, 
  Search,
  Bell,
  HelpCircle,
  Menu,
  ChevronRight,
  Database,
  RefreshCw,
  History,
  Code,
  AlertCircle,
  Briefcase,
  ArrowRightLeft,
  CheckSquare,
  Moon,
  Package,
  CheckCircle,
  MapPin,
  LogOut,
  FileCheck
} from 'lucide-react';
import {
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  AreaChart,
  Area
} from 'recharts';

function App() {
  const [activeTab, setActiveTab] = useState<'dashboard' | 'clients' | 'groups' | 'loans' | 'teller' | 'transactions' | 'accounting' | 'reports' | 'admin' | 'settings' | 'tasks' | 'audit' | 'profile' | 'eod' | 'products' | 'approvals' | 'statements'>('dashboard');
  
  // Use the Central Engine
  const { 
    currentUser,
    login,
    logout,
    hasPermission,
    authError,
    businessDate,
    branches,
    customers, 
    groups, 
    products, 
    accounts, 
    loans, 
    glAccounts, 
    journalEntries, 
    auditLogs, 
    processTellerTransaction, 
    createCustomer, 
    updateCustomer,
    createGroup, 
    addMemberToGroup, 
    removeMemberFromGroup, 
    createAccount, 
    disburseLoan, 
    setLoans,
    postGL,
    createGLAccount,
    transactions,
    roles,
    staff,
    devTasks, 
    approvalRequests, 
    workflows,
    systemConfig,
    approveRequest, 
    rejectRequest,
    createRole,
    updateUserRole,
    createStaff,
    resetUserPassword,
    changeMyPassword,
    updateSystemConfig,
    createWorkflow,
    updateWorkflow,
    createProduct,
    updateProduct,
    executeStoredProcedure,
    addDevTask, 
    updateDevTaskStatus, 
    deleteDevTask, 
    runEodStep 
  } = useBankingSystem();

  const [insights, setInsights] = useState<AIInsight[]>([]);
  const [loadingInsights, setLoadingInsights] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(true);

  // Metrics Calculation (Real-time from Engine)
  const metrics = useMemo(() => {
    const totalDeposits = accounts.reduce((acc, curr) => acc + curr.balance, 0);
    const activeAccounts = accounts.filter(a => a.status === 'ACTIVE').length;
    const frozenCount = accounts.filter(a => a.status === 'FROZEN' || a.lienAmount > 0).length;
    const activeLoans = loans.filter(l => l.status === 'ACTIVE').length;
    const totalLoanBal = loans.reduce((acc, l) => acc + l.outstandingBalance, 0);

    return [
      {
        label: "Total Liquidity",
        value: `GHS ${(totalDeposits / 1000000).toFixed(2)}M`,
        trend: 'up' as const,
        percentage: "+2.4%"
      },
      {
        label: "Active Clients",
        value: customers.length,
        trend: 'up' as const,
        percentage: `+${activeAccounts} Accts`
      },
      {
        label: "Loan Portfolio",
        value: `GHS ${(totalLoanBal / 1000).toFixed(1)}K`,
        trend: 'down' as const,
        percentage: `${activeLoans} Active`
      },
      {
        label: "Portfolio at Risk",
        value: frozenCount,
        trend: 'neutral' as const,
        percentage: "0"
      }
    ];
  }, [accounts, customers, loans]);

  // Chart Data Preparation (Liquidity Flow - Mocked but could be derived from GL)
  const chartData = useMemo(() => {
    return Array.from({length: 15}, (_, i) => ({
        date: `Oct ${i+1}`,
        amount: 4000000 + (Math.random() * 500000)
    }));
  }, []);

  const handleFetchInsights = async () => {
    setLoadingInsights(true);
    // Pass real engine data to AI
    const newInsights = await generateDashboardInsights({ customers, accounts, loans });
    setInsights(newInsights);
    setLoadingInsights(false);
  };

  useEffect(() => {
    if(currentUser) handleFetchInsights();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentUser]); 

  // --- LOGIN GUARD ---
  if (!currentUser) {
      return <LoginScreen onLogin={login} error={authError} />;
  }

  const userBranch = branches.find(b => b.id === currentUser.branchId);

  const SidebarItem = ({ id, icon: Icon, label, subLabel, permission }: { id: typeof activeTab, icon: any, label: string, subLabel?: string, permission?: string }) => {
    if (permission && !hasPermission(permission as any)) return null;
    return (
        <button 
        onClick={() => setActiveTab(id)}
        className={`w-full flex items-center gap-3 px-4 py-3 transition-colors border-l-4 ${
            activeTab === id 
            ? 'bg-slate-800 text-white border-blue-500' 
            : 'text-slate-400 hover:bg-slate-800 hover:text-white border-transparent'
        }`}
        >
        <Icon size={18} />
        <div className="text-left">
            <div className="text-sm font-medium">{label}</div>
            {subLabel && <div className="text-[10px] opacity-60">{subLabel}</div>}
        </div>
        </button>
    );
  };

  return (
    <div className="flex h-screen bg-gray-100 font-sans text-slate-900">
      {/* Sidebar - Mifos Style (Dark) */}
      <aside className={`bg-slate-900 flex flex-col fixed h-full z-20 transition-all duration-300 ${sidebarOpen ? 'w-64' : 'w-16'}`}>
        <div className="h-14 bg-blue-600 flex items-center justify-center shadow-md">
           {sidebarOpen ? (
             <h1 className="text-white font-bold text-lg tracking-tight">Bank<span className="font-light opacity-80">Insight</span></h1>
           ) : (
             <span className="text-white font-bold">BI</span>
           )}
        </div>
        
        <nav className="flex-1 overflow-y-auto py-4">
          {sidebarOpen ? (
            <>
              <div className="px-4 py-2 text-xs font-semibold text-slate-500 uppercase tracking-wider">Navigation</div>
              <SidebarItem id="dashboard" icon={LayoutDashboard} label="Dashboard" />
              <SidebarItem id="clients" icon={Users} label="Clients" subLabel="Accounts & Groups" permission="ACCOUNT_READ" />
              <SidebarItem id="groups" icon={Users} label="Groups" subLabel="Joint Liability" permission="ACCOUNT_READ" />
              <SidebarItem id="loans" icon={Briefcase} label="Loans" subLabel="Origination & Lifecycle" permission="LOAN_READ" />
              <SidebarItem id="teller" icon={Landmark} label="Teller" subLabel="Cash Operations" permission="TELLER_POST" />
              <SidebarItem id="transactions" icon={ArrowRightLeft} label="Transactions" subLabel="History & Search" permission="ACCOUNT_READ" />
              <SidebarItem id="statements" icon={FileCheck} label="Statements" subLabel="Verification" permission="ACCOUNT_READ" />
              <SidebarItem id="accounting" icon={FileText} label="Accounting" subLabel="GL & Journals" permission="GL_READ" />
              <SidebarItem id="reports" icon={Database} label="Reports" subLabel="Compliance & BI" permission="REPORT_VIEW" />
              
              <div className="px-4 py-2 mt-4 text-xs font-semibold text-slate-500 uppercase tracking-wider">System</div>
              <SidebarItem id="approvals" icon={CheckCircle} label="Approvals" subLabel="Maker-Checker" permission="LOAN_APPROVE" />
              <SidebarItem id="products" icon={Package} label="Product Designer" subLabel="Rates & Terms" permission="SYSTEM_CONFIG" />
              <SidebarItem id="eod" icon={Moon} label="End of Day" subLabel="Batch Processing" permission="SYSTEM_CONFIG" />
              <SidebarItem id="settings" icon={SettingsIcon} label="Settings" subLabel="Users & Configuration" permission="SYSTEM_CONFIG" />
              <SidebarItem id="admin" icon={RefreshCw} label="Migration" subLabel="Data Import" permission="SYSTEM_CONFIG" />
              <SidebarItem id="audit" icon={History} label="Audit Trail" permission="SYSTEM_CONFIG" />
              <SidebarItem id="tasks" icon={CheckSquare} label="Roadmap" subLabel="Dev Tasks" />
            </>
          ) : (
            <div className="flex flex-col items-center gap-4 mt-4">
               <LayoutDashboard className="text-slate-400" size={20} />
               <Users className="text-slate-400" size={20} />
               <Briefcase className="text-slate-400" size={20} />
               <Landmark className="text-slate-400" size={20} />
               <ArrowRightLeft className="text-slate-400" size={20} />
               <FileText className="text-slate-400" size={20} />
               <SettingsIcon className="text-slate-400" size={20} />
            </div>
          )}
        </nav>

        <div className="p-4 bg-slate-950 text-slate-400 text-xs border-t border-slate-800">
           {sidebarOpen && <p>BankInsight Backend v10.2</p>}
        </div>
      </aside>

      {/* Main Content */}
      <div className={`flex-1 flex flex-col transition-all duration-300 ${sidebarOpen ? 'ml-64' : 'ml-16'}`}>
        
        {/* Top Header - Mifos Style (Blue/White Enterprise) */}
        <header className="h-14 bg-white border-b border-gray-200 flex items-center justify-between px-4 sticky top-0 z-10 shadow-sm">
          <div className="flex items-center gap-4">
             <button onClick={() => setSidebarOpen(!sidebarOpen)} className="text-gray-500 hover:text-blue-600">
                <Menu size={20} />
             </button>
             {/* Global Search */}
             <div className="relative hidden md:block">
                <input 
                  type="text" 
                  placeholder="Search for client, loan or GL..." 
                  className="bg-gray-100 border border-gray-300 text-sm rounded px-3 py-1.5 pl-9 w-64 focus:outline-none focus:border-blue-500 focus:bg-white transition-all"
                />
                <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
             </div>
          </div>

          <div className="flex items-center gap-4">
             {userBranch && (
                 <div className="flex items-center gap-1 text-xs font-bold text-blue-800 bg-blue-100 px-3 py-1 rounded-full">
                     <MapPin size={12} /> {userBranch.name} ({userBranch.code})
                 </div>
             )}
             <div className="flex items-center gap-1 text-gray-500 hover:text-blue-600 cursor-pointer">
                <HelpCircle size={18} />
                <span className="text-xs font-medium hidden sm:block">Help</span>
             </div>
             <div className="relative">
                <Bell size={18} className="text-gray-500 hover:text-blue-600 cursor-pointer" />
                {approvalRequests.filter(r => r.status === 'PENDING').length > 0 && hasPermission('LOAN_APPROVE') && (
                    <span className="absolute -top-1 -right-1 w-2 h-2 bg-red-500 rounded-full animate-pulse"></span>
                )}
             </div>
             <div className="h-8 w-px bg-gray-300 mx-1"></div>
             <div 
                className="flex items-center gap-2 cursor-pointer hover:bg-gray-50 p-1 rounded transition-colors"
                onClick={() => setActiveTab('profile')}
             >
                <div className="w-8 h-8 rounded bg-blue-600 text-white flex items-center justify-center font-bold text-xs">
                   {currentUser.avatarInitials}
                </div>
                <div className="hidden sm:block text-sm">
                   <p className="font-semibold text-gray-700 leading-none">{currentUser.name}</p>
                   <p className="text-[10px] text-gray-500">{currentUser.roleName}</p>
                </div>
             </div>
             <button onClick={logout} className="text-red-500 hover:bg-red-50 p-2 rounded-full" title="Logout">
                 <LogOut size={18} />
             </button>
          </div>
        </header>

        {/* Breadcrumb Bar */}
        <div className="bg-gray-50 border-b border-gray-200 px-6 py-2 flex items-center gap-2 text-xs text-gray-500">
           <span>Home</span>
           <ChevronRight size={12} />
           <span className="font-medium text-blue-600 capitalize">{activeTab === 'profile' ? 'User Profile' : activeTab === 'eod' ? 'End of Day' : activeTab}</span>
        </div>

        {/* Content Area */}
        <main className="flex-1 p-6 overflow-y-auto">
          {activeTab === 'dashboard' && (
            <div className="space-y-6 max-w-7xl mx-auto">
              <div className="flex justify-between items-center">
                 <h2 className="text-xl font-bold text-gray-800">Dashboard</h2>
                 <span className="text-xs text-gray-500">System Date: {businessDate}</span>
              </div>

              {/* Stats Row - Mifos Style Cards */}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <StatCard metric={metrics[0]} icon={<Landmark size={24} />} color="blue" />
                <StatCard metric={metrics[1]} icon={<Users size={24} />} color="green" />
                <StatCard metric={metrics[2]} icon={<FileText size={24} />} color="orange" />
                <StatCard metric={metrics[3]} icon={<AlertCircle size={24} />} color="red" />
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 h-[500px]">
                {/* Chart Section */}
                <div className="lg:col-span-2 bg-white rounded shadow-sm border border-gray-200 flex flex-col">
                  <div className="p-4 border-b border-gray-200 bg-gray-50 flex justify-between items-center">
                     <h3 className="font-semibold text-gray-700">Liquidity Trend</h3>
                     <select className="text-xs border-gray-300 rounded">
                        <option>This Month</option>
                     </select>
                  </div>
                  <div className="flex-1 p-4">
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart data={chartData}>
                        <defs>
                          <linearGradient id="colorAmount" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="#2563eb" stopOpacity={0.1}/>
                            <stop offset="95%" stopColor="#2563eb" stopOpacity={0}/>
                          </linearGradient>
                        </defs>
                        <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e7eb" />
                        <XAxis dataKey="date" axisLine={false} tickLine={false} tick={{fill: '#6b7280', fontSize: 12}} dy={10} />
                        <YAxis axisLine={false} tickLine={false} tick={{fill: '#6b7280', fontSize: 12}} tickFormatter={(value) => `${(value/1000000).toFixed(1)}M`} />
                        <Tooltip />
                        <Area type="monotone" dataKey="amount" stroke="#2563eb" strokeWidth={2} fillOpacity={1} fill="url(#colorAmount)" />
                      </AreaChart>
                    </ResponsiveContainer>
                  </div>
                </div>

                {/* Insight Panel */}
                <div className="lg:col-span-1 h-full">
                  <InsightPanel 
                    insights={insights} 
                    loading={loadingInsights} 
                    onRefresh={handleFetchInsights} 
                  />
                </div>
              </div>
            </div>
          )}

          {activeTab === 'clients' && (
             <div className="h-full flex flex-col">
                 <ClientManager 
                    customers={customers} 
                    accounts={accounts} 
                    loans={loans}
                    transactions={transactions}
                    products={products}
                    onCreateCustomer={createCustomer}
                    onUpdateCustomer={updateCustomer}
                    onCreateAccount={createAccount}
                 />
             </div>
          )}

          {activeTab === 'groups' && (
             <div className="h-full flex flex-col">
                 <GroupManager 
                    groups={groups}
                    customers={customers}
                    onCreateGroup={createGroup}
                    onAddMember={addMemberToGroup}
                    onRemoveMember={removeMemberFromGroup}
                 />
             </div>
          )}

          {activeTab === 'loans' && (
             <div className="h-full flex flex-col">
                <LoanEngine 
                    loans={loans} 
                    setLoans={setLoans} 
                    onDisburse={disburseLoan} 
                    customers={customers} 
                    groups={groups} 
                    products={products}
                />
             </div>
          )}

          {activeTab === 'teller' && (
             <TellerTerminal accounts={accounts} customers={customers} onTransaction={processTellerTransaction} />
          )}

          {activeTab === 'transactions' && (
             <TransactionExplorer transactions={transactions} />
          )}

          {activeTab === 'statements' && (
             <StatementVerification accounts={accounts} transactions={transactions} customers={customers} />
          )}

          {activeTab === 'accounting' && (
             <div className="h-full flex flex-col gap-6">
                <AccountingEngine 
                    accounts={glAccounts} 
                    journalEntries={journalEntries} 
                    onPostJournal={postGL}
                    onCreateAccount={createGLAccount}
                />
                <div className="bg-white p-4 rounded shadow-sm border border-gray-200">
                   <h3 className="font-bold text-gray-700 mb-2">Stored Procedures</h3>
                   <div className="h-64">
                       <BasicPlusEditor data={accounts} onExecute={executeStoredProcedure} />
                   </div>
                </div>
             </div>
          )}

          {activeTab === 'reports' && (
             <CompliancePanel 
                metrics={metrics.map(m => ({ label: m.label, value: m.value, threshold: '-', status: 'pass', code: 'METRIC' }))} 
                glAccounts={glAccounts}
                accounts={accounts}
                loans={loans}
             />
          )}

          {activeTab === 'admin' && (
             <MigrationWizard />
          )}

          {activeTab === 'approvals' && (
             <ApprovalInbox 
                requests={approvalRequests}
                currentUser={currentUser}
                onApprove={approveRequest}
                onReject={rejectRequest}
             />
          )}

          {activeTab === 'products' && (
             <ProductDesigner 
                products={products}
                onCreateProduct={createProduct}
                onUpdateProduct={updateProduct}
             />
          )}

          {activeTab === 'settings' && (
             <Settings 
                users={staff}
                roles={roles}
                workflows={workflows}
                onCreateRole={createRole}
                onUpdateUserRole={updateUserRole}
                onCreateStaff={createStaff}
                onResetPassword={resetUserPassword}
                systemConfig={systemConfig}
                onUpdateConfig={updateSystemConfig}
                onCreateWorkflow={createWorkflow}
                onUpdateWorkflow={updateWorkflow}
             />
          )}

          {activeTab === 'tasks' && (
             <DevelopmentTasks 
                tasks={devTasks}
                onAddTask={addDevTask}
                onUpdateStatus={updateDevTaskStatus}
                onDelete={deleteDevTask}
             />
          )}

          {activeTab === 'audit' && (
             <div className="h-full">
                <AuditTrail logs={auditLogs} />
             </div>
          )}

          {activeTab === 'eod' && (
             <EodConsole businessDate={businessDate} onRunStep={runEodStep} />
          )}
          
          {activeTab === 'profile' && (
              <UserProfile 
                user={currentUser} 
                onUpdate={(u) => console.log(u)} 
                onChangePassword={changeMyPassword}
              />
          )}
        </main>
      </div>
    </div>
  );
}

export default App;
```

### components/BasicPlusEditor.tsx
```tsx
import React, { useState, useEffect } from 'react';
import { Play, Save, Terminal, FileCode, Loader2, ChevronDown, Sparkles, X, Database, Globe } from 'lucide-react';
import { runBasicPlusCode, generateBasicPlusCode } from '../services/geminiService';
import { StoredProcedureResult } from '../types';

interface BasicPlusEditorProps {
    data: any;
    onExecute?: (name: string, args: any[]) => StoredProcedureResult;
}

const SCRIPTS: Record<string, string> = {
    'O4W_CALC_LOAN_INTEREST': `Compile Function O4W_CALC_LOAN_INTEREST(Request)
*-------------------------------------------------------------------------
* Description: O4W Endpoint for Loan Simulation
* Input: JSON String via 'Request'
* Output: JSON String
*-------------------------------------------------------------------------

   $Insert O4W_Equates
   
   * Parse Incoming JSON
   JsonReq = Request<1>
   Principal = JsonReq{"principal"}
   AnnualRate = JsonReq{"rate"}
   TermMonths = JsonReq{"term"}

   If Unassigned(Principal) Then Principal = 10000
   If Unassigned(AnnualRate) Then AnnualRate = 28.5
   If Unassigned(TermMonths) Then TermMonths = 12

   MonthlyRate = (AnnualRate / 100) / 12
   
   * Amortization Formula
   Top = Principal * MonthlyRate * Power((1 + MonthlyRate), TermMonths)
   Bot = Power((1 + MonthlyRate), TermMonths) - 1
   
   MonthlyPayment = Top / Bot
   TotalPayment   = MonthlyPayment * TermMonths
   TotalInterest  = TotalPayment - Principal

   * Construct JSON Response
   Response  = '{'
   Response := '  "status": "OK",'
   Response := '  "data": {'
   Response := '    "principal": ' : Principal : ','
   Response := '    "monthly_payment": "' : Oconv(MonthlyPayment, "MD2,") : '",'
   Response := '    "total_interest": "' : Oconv(TotalInterest, "MD2,") : '",'
   Response := '    "apr": "' : AnnualRate : '%"'
   Response := '  }'
   Response := '}'

Return Response`,

    'O4W_POST_TRANSACTION': `Compile Function O4W_POST_TRANSACTION(Request)
*-------------------------------------------------------------------------
* Description: O4W Endpoint for Teller Posting
* Validates Token, Checks Limits, Posts to GL
*-------------------------------------------------------------------------

   * Simulate Parsing
   AccountId = Request{"accountId"}
   TxType    = Request{"type"}
   Amount    = Request{"amount"}
   TellerId  = Request{"tellerId"}

   Open "ACCOUNTS" To ACCOUNTS_FILE Else 
      Return '{"status": "ERROR", "message": "DB_OFFLINE"}'
   End

   Lock ACCOUNTS_FILE, AccountId Else 
      Return '{"status": "ERROR", "message": "RECORD_LOCKED"}'
   End
   
   Read Rec From ACCOUNTS_FILE, AccountId Else 
      Unlock ACCOUNTS_FILE, AccountId
      Return '{"status": "ERROR", "message": "INVALID_ACCOUNT"}'
   End
   
   * ... [Core Logic Removed for Brevity] ...
   
   * Construct Success Response
   Json = '{'
   Json := ' "status": "SUCCESS",'
   Json := ' "txn_ref": "' : TxId : '",'
   Json := ' "new_balance": "' : Oconv(NewBal, "MD2,") : '"'
   Json := '}'
   
   Return Json`,

    'SP_EOD_BATCH': `Compile Subroutine SP_EOD_BATCH(ProcessDate)
*-------------------------------------------------------------------------
* Description: End of Day Batch Processing (Internal Subroutine)
* Not directly exposed via O4W (called by O4W_RUN_EOD)
*-------------------------------------------------------------------------

   If Unassigned(ProcessDate) Then ProcessDate = Date()
   
   * --- 1. Interest Accrual (Savings) ---
   Select "ACCOUNTS" With TYPE = "SAVINGS" And STATUS = "ACTIVE"
   Done = 0
   AccrualTotal = 0
   
   Loop
      ReadNext ID Else Done = 1
   Until Done
      Read Rec From ACCOUNTS_FILE, ID Then
         Balance = Rec<4>
         Rate = 0.05 ;* 5% p.a.
         DailyInterest = Balance * (Rate / 365)
         
         * Accumulate in internal accrual bucket (Field 8)
         Rec<8> = Rec<8> + DailyInterest
         AccrualTotal += DailyInterest
         
         Write Rec On ACCOUNTS_FILE, ID
      End
   Repeat
   
   * Post GL for Total Accrual
   Call SP_POST_GL_JOURNAL("EOD Accrual", "50100", AccrualTotal, "20500", AccrualTotal)

   Return "BATCH_COMPLETED_SUCCESS"`,

   'O4W_AUTH_USER': `Compile Function O4W_AUTH_USER(Request)
*-------------------------------------------------------------------------
* Description: O4W Auth Endpoint
* Returns JWT Token if credentials match
*-------------------------------------------------------------------------

   User = Request{"username"}
   Pass = Request{"password"}
   
   Open "SECURITY_USERS" To USERS_FILE Else Return '{"error": "DB"}'
   
   * ... Verify Hash ...
   
   If Valid Then
       Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
       Return '{"status": "OK", "token": "' : Token : '"}'
   End Else
       Return '{"status": "DENIED", "message": "Invalid Credentials"}'
   End`
};

const TEMPLATES = [
    'O4W JSON Endpoint',
    'Internal Subroutine',
    'Dict.MFS Hook',
    'BoG Report Logic'
];

const BasicPlusEditor: React.FC<BasicPlusEditorProps> = ({ data, onExecute }) => {
    const [selectedScript, setSelectedScript] = useState('O4W_POST_TRANSACTION');
    const [code, setCode] = useState(SCRIPTS['O4W_POST_TRANSACTION']);
    const [output, setOutput] = useState('');
    const [isRunning, setIsRunning] = useState(false);
    
    // AI Generation State
    const [showGenModal, setShowGenModal] = useState(false);
    const [genPrompt, setGenPrompt] = useState('');
    const [genTemplate, setGenTemplate] = useState('O4W JSON Endpoint');
    const [isGenerating, setIsGenerating] = useState(false);

    // Update code when script selection changes
    const handleScriptChange = (scriptName: string) => {
        setSelectedScript(scriptName);
        if (SCRIPTS[scriptName]) {
            setCode(SCRIPTS[scriptName]);
        }
        setOutput('');
    };

    const handleRun = async () => {
        setIsRunning(true);
        setOutput(`> POST /api/o4w/${selectedScript}\n> Content-Type: application/json\n> Waiting for OEngine...`);

        // WIRING: Check if this is a known system procedure and we have a dispatcher
        // Mapping O4W names to internal simulation functions
        const procMap: Record<string, string> = {
            'O4W_POST_TRANSACTION': 'SP_POST_TRANSACTION',
            'O4W_CALC_LOAN_INTEREST': 'SP_CALC_LOAN_INTEREST',
            'SP_EOD_BATCH': 'SP_EOD_BATCH'
        };

        const internalName = procMap[selectedScript] || selectedScript;

        if (onExecute && ['SP_POST_TRANSACTION', 'SP_CREATE_ACCOUNT', 'SP_EOD_BATCH'].includes(internalName)) {
            setTimeout(() => {
                let args: any[] = [];
                let userCancelled = false;

                // Simulate O4W Request Body construction via Prompt
                if (internalName === 'SP_POST_TRANSACTION') {
                     const acct = prompt("Simulate JSON Payload - Account ID:", "2010000001");
                     const amt = prompt("Simulate JSON Payload - Amount:", "500");
                     if (acct && amt) args = [acct, 'DEPOSIT', amt, 'O4W Web Deposit', 'API_USER'];
                     else userCancelled = true;
                } else if (internalName === 'SP_EOD_BATCH') {
                     args = [new Date().toISOString().split('T')[0]];
                }

                if (!userCancelled) {
                    const result = onExecute(internalName, args);
                    // Simulate O4W Wrapping the result in JSON
                    const o4wJson = JSON.stringify({
                        status: result.success ? "200 OK" : "500 ERROR",
                        server: "OpenInsight 10.2 OEngine",
                        payload: result.data || result.output
                    }, null, 2);
                    
                    setOutput(prev => prev + `\n\n< HTTP/1.1 200 OK\n< Content-Type: application/json\n\n${o4wJson}`);
                } else {
                    setOutput(prev => prev + "\n> Request Aborted.");
                }
                setIsRunning(false);
            }, 800);
            return;
        }

        // Fallback to AI execution for custom scripts
        setTimeout(async () => {
             // Pass data context (accounts) to the AI runner so it can simulate "Read Rec"
             const result = await runBasicPlusCode(code, data);
             setOutput(prev => prev + "\n\n" + result);
             setIsRunning(false);
        }, 1200);
    };

    const handleAiGenerate = async () => {
        if (!genPrompt) return;
        setIsGenerating(true);
        const generatedCode = await generateBasicPlusCode(genPrompt, genTemplate);
        setCode(generatedCode);
        setSelectedScript(`O4W_GEN_${Date.now().toString().slice(-4)}`);
        setIsGenerating(false);
        setShowGenModal(false);
        setGenPrompt('');
    };

    return (
        <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden relative">
            {/* Toolbar */}
            <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 bg-gray-50">
                <div className="flex items-center gap-3">
                    <div className="bg-orange-100 p-1.5 rounded text-orange-700">
                        <Globe size={18} />
                    </div>
                    <div>
                        <div className="relative group">
                            <button className="font-semibold text-gray-700 text-sm flex items-center gap-1 hover:text-blue-600 focus:outline-none">
                                {selectedScript} <ChevronDown size={14} />
                            </button>
                            {/* Dropdown Menu */}
                            <div className="absolute top-full left-0 mt-1 w-64 bg-white border border-gray-200 rounded shadow-lg hidden group-hover:block z-50">
                                {Object.keys(SCRIPTS).map(script => (
                                    <button 
                                        key={script}
                                        onClick={() => handleScriptChange(script)}
                                        className={`w-full text-left px-4 py-2 text-xs hover:bg-gray-50 ${selectedScript === script ? 'text-blue-600 font-bold' : 'text-gray-700'}`}
                                    >
                                        {script}
                                    </button>
                                ))}
                            </div>
                        </div>
                        <span className="text-xs text-gray-500 block">O4W Endpoint • Basic+</span>
                    </div>
                </div>
                <div className="flex gap-2">
                     <button 
                        onClick={() => setShowGenModal(true)}
                        className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-purple-700 bg-purple-50 border border-purple-200 rounded hover:bg-purple-100 transition-colors mr-2"
                     >
                        <Sparkles size={14} /> AI Assist
                     </button>
                     <button className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-gray-600 bg-white border border-gray-300 rounded hover:bg-gray-50 hover:text-blue-700 transition-colors">
                        <Save size={14} /> Save
                     </button>
                     <button 
                        onClick={handleRun}
                        disabled={isRunning}
                        className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-white bg-green-600 rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm transition-colors"
                     >
                        {isRunning ? <Loader2 size={14} className="animate-spin"/> : <Play size={14} />} 
                        {isRunning ? 'Sending...' : 'Test API'}
                     </button>
                </div>
            </div>

            {/* Editor Area */}
            <div className="flex-1 flex flex-col lg:flex-row min-h-0">
                <div className="flex-1 bg-[#1e1e1e] relative overflow-hidden flex flex-col">
                    <div className="absolute top-0 left-0 bottom-0 w-10 bg-[#252526] text-gray-500 text-xs font-mono pt-4 text-right pr-2 select-none border-r border-[#333]">
                        {code.split('\n').map((_, i) => (
                            <div key={i} className="leading-6">{i + 1}</div>
                        ))}
                    </div>
                    <textarea 
                        value={code}
                        onChange={(e) => setCode(e.target.value)}
                        className="w-full h-full bg-transparent text-gray-300 font-mono text-sm resize-none focus:outline-none p-4 pl-12 leading-6"
                        spellCheck={false}
                        autoCapitalize="off"
                    />
                </div>
                
                {/* Output Console */}
                <div className="h-48 lg:h-auto lg:w-96 bg-[#0f0f0f] border-t lg:border-t-0 lg:border-l border-gray-700 flex flex-col">
                    <div className="px-4 py-2 bg-[#252526] text-gray-400 text-xs font-mono border-b border-gray-700 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                             <Terminal size={12} /> HTTP RESPONSE
                        </div>
                        <button onClick={() => setOutput('')} className="hover:text-white">Clear</button>
                    </div>
                    <div className="p-4 font-mono text-sm text-green-400 whitespace-pre-wrap overflow-auto flex-1 font-light leading-relaxed">
                        {output || <span className="text-gray-600 opacity-50">{"> Waiting for request..."}</span>}
                    </div>
                </div>
            </div>

            {/* AI Generator Modal */}
            {showGenModal && (
                <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center backdrop-blur-sm">
                    <div className="bg-white rounded-xl shadow-2xl p-6 w-[500px] border border-gray-200">
                        <div className="flex justify-between items-center mb-4">
                            <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2">
                                <Sparkles className="text-purple-600" size={20} />
                                Generate Basic+ O4W Module
                            </h3>
                            <button onClick={() => setShowGenModal(false)} className="text-gray-400 hover:text-gray-600">
                                <X size={20} />
                            </button>
                        </div>
                        
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Module Template</label>
                                <select 
                                    value={genTemplate}
                                    onChange={(e) => setGenTemplate(e.target.value)}
                                    className="w-full p-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-purple-500 outline-none"
                                >
                                    {TEMPLATES.map(t => <option key={t} value={t}>{t}</option>)}
                                </select>
                            </div>
                            
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Description / Requirements</label>
                                <textarea 
                                    value={genPrompt}
                                    onChange={(e) => setGenPrompt(e.target.value)}
                                    placeholder="Describe the API functionality (e.g., 'API to validate account balance and return user details as JSON')..."
                                    className="w-full p-3 border border-gray-300 rounded-lg text-sm h-32 resize-none focus:ring-2 focus:ring-purple-500 outline-none"
                                />
                            </div>
                            
                            <button 
                                onClick={handleAiGenerate}
                                disabled={isGenerating || !genPrompt}
                                className="w-full py-2.5 bg-purple-600 text-white rounded-lg font-bold hover:bg-purple-700 disabled:opacity-50 flex items-center justify-center gap-2"
                            >
                                {isGenerating ? <Loader2 className="animate-spin" size={18} /> : <Sparkles size={18} />}
                                {isGenerating ? 'Generating Code...' : 'Generate Module'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default BasicPlusEditor;
```

### hooks/useBankingTransaction.ts
```ts
import { useState } from 'react';
import { Transaction } from '../types';

interface TransactionResult {
  success: boolean;
  message: string;
  receiptId?: string;
  newBalance?: number;
}

export const useBankingTransaction = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const postTransaction = async (tx: Omit<Transaction, 'id' | 'status' | 'date'>): Promise<TransactionResult> => {
    setLoading(true);
    setError(null);

    return new Promise((resolve) => {
      setTimeout(() => {
        // Simulate Network/API Check
        const isOffline = Math.random() > 0.95; // 5% chance of network failure simulation
        
        if (isOffline) {
            setLoading(false);
            const msg = "ERR_NET_01: O4W API Gateway Unreachable. Transaction queued locally.";
            setError(msg);
            resolve({ success: false, message: msg });
            return;
        }

        // Simulate Basic+ Validation Logic
        if (tx.type === 'WITHDRAWAL' && tx.amount > 10000) {
           // AML Limit Check
           setLoading(false);
           resolve({ 
               success: false, 
               message: "AML_09: Amount exceeds Tier 2 daily withdrawal limit. Manager override required." 
           });
           return;
        }

        setLoading(false);
        resolve({
          success: true,
          message: "Transaction Posted Successfully to GL.",
          receiptId: `RCPT-${Date.now().toString().slice(-6)}`,
          newBalance: 0 // Mock, would be real in full app
        });
      }, 1500);
    });
  };

  return { postTransaction, loading, error };
};
```

### components/TellerTerminal.tsx
```tsx
import React, { useState } from 'react';
import { Account, Customer } from '../types';
import { Search, Save, XCircle, Printer, AlertCircle, CheckCircle, ShieldCheck, FileSignature, X, User, Clock } from 'lucide-react';

interface TellerTerminalProps {
  accounts: Account[];
  customers: Customer[]; // Added to link CIF for mandate details
  onTransaction: (accountId: string, type: 'DEPOSIT' | 'WITHDRAWAL', amount: number, narration: string, tellerId: string) => { success: boolean; id: string; message: string; status: 'POSTED' | 'PENDING_APPROVAL' };
}

const TellerTerminal: React.FC<TellerTerminalProps> = ({ accounts, customers, onTransaction }) => {
  const [accountNum, setAccountNum] = useState('');
  const [selectedAccount, setSelectedAccount] = useState<Account | null>(null);
  const [amount, setAmount] = useState('');
  const [narration, setNarration] = useState('');
  const [txType, setTxType] = useState<'DEPOSIT' | 'WITHDRAWAL'>('DEPOSIT');
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [pendingMsg, setPendingMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  
  // Mandate Modal State
  const [showMandateModal, setShowMandateModal] = useState(false);

  const GRA_RATE = 0.01; // 1%

  const findAccount = () => {
    const acc = accounts.find(a => a.id === accountNum);
    if (acc) {
        setSelectedAccount(acc);
        setError(null);
    } else {
        setError("Account not found in CUST_ACCOUNTS table.");
        setSelectedAccount(null);
    }
  };

  const handleSubmit = async () => {
    if (!selectedAccount || !amount) return;
    setError(null);
    setSuccessMsg(null);
    setPendingMsg(null);
    
    try {
        const result = onTransaction(
            selectedAccount.id,
            txType,
            parseFloat(amount),
            narration || `${txType} via Teller`,
            'TLR001'
        );
        
        if (result.status === 'POSTED') {
            setSuccessMsg(`Success: Posted (Ref: ${result.id})`);
            setAmount('');
            setNarration('');
            // Re-fetch account details locally to show updated balance
            const updatedAcc = accounts.find(a => a.id === selectedAccount.id);
            if (updatedAcc) setSelectedAccount(updatedAcc);
        } else if (result.status === 'PENDING_APPROVAL') {
            setPendingMsg(`Held: ${result.message} (Req: ${result.id})`);
            setAmount('');
            setNarration('');
        }

    } catch (e: any) {
        setError(e.message || "Transaction Failed");
    }
  };

  const calculateTax = () => {
      if (txType !== 'WITHDRAWAL' || !amount) return 0;
      const amt = parseFloat(amount);
      if (isNaN(amt) || amt <= 100) return 0; // Exempt under 100 in simulation
      return amt * GRA_RATE;
  };

  const tax = calculateTax();
  const totalDebit = parseFloat(amount || '0') + tax;

  // Mandate Modal Component
  const MandateModal = () => {
      if (!selectedAccount) return null;
      
      const customer = customers.find(c => c.id === selectedAccount.cif);
      
      return (
          <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4 backdrop-blur-sm">
              <div className="bg-white rounded-lg shadow-2xl w-full max-w-2xl overflow-hidden">
                  <div className="bg-blue-900 text-white p-4 flex justify-between items-center">
                      <div className="flex items-center gap-2">
                          <FileSignature size={20} />
                          <h3 className="font-bold text-lg">Account Mandate Verification</h3>
                      </div>
                      <button onClick={() => setShowMandateModal(false)} className="text-white hover:text-blue-200">
                          <X size={20} />
                      </button>
                  </div>
                  
                  <div className="p-6">
                      <div className="flex items-start gap-6 mb-6">
                          <div className="w-32 h-32 bg-gray-200 rounded-lg flex items-center justify-center border-2 border-dashed border-gray-400 relative overflow-hidden">
                              <User size={48} className="text-gray-400" />
                              <div className="absolute bottom-0 w-full bg-black/50 text-white text-[10px] text-center py-1">Photo</div>
                          </div>
                          <div className="flex-1">
                              <h4 className="font-bold text-xl text-gray-800">{customer?.name}</h4>
                              <p className="text-gray-500 text-sm mb-2">CIF: {selectedAccount.cif}</p>
                              <div className="grid grid-cols-2 gap-4 mt-4">
                                  <div className="bg-gray-50 p-3 rounded border border-gray-100">
                                      <span className="text-xs text-gray-500 uppercase font-bold block mb-1">Status</span>
                                      <span className="font-bold text-green-700">{selectedAccount.status}</span>
                                  </div>
                                  <div className="bg-gray-50 p-3 rounded border border-gray-100">
                                      <span className="text-xs text-gray-500 uppercase font-bold block mb-1">Instructions</span>
                                      <span className="font-bold text-blue-700">{selectedAccount.mandate?.instructions || "Not Set"}</span>
                                  </div>
                              </div>
                          </div>
                      </div>

                      <h4 className="font-bold text-gray-700 mb-3 border-b pb-2">Authorized Signatories</h4>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                          {selectedAccount.mandate?.signatories.map((sig, idx) => (
                              <div key={idx} className="border border-gray-200 rounded-lg p-3 hover:border-blue-300 transition-colors">
                                  <div className="flex justify-between items-start mb-2">
                                      <div>
                                          <p className="font-bold text-gray-800 text-sm">{sig.name}</p>
                                          <p className="text-xs text-gray-500">{sig.role}</p>
                                      </div>
                                      <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded font-bold">Active</span>
                                  </div>
                                  <div className="h-16 bg-gray-50 border border-gray-100 rounded flex items-center justify-center mt-2">
                                      <span className="font-handwriting text-2xl text-blue-800 italic opacity-70">
                                          {sig.name.split(' ')[0]}Sig
                                      </span>
                                  </div>
                              </div>
                          ))}
                          {(!selectedAccount.mandate?.signatories || selectedAccount.mandate.signatories.length === 0) && (
                              <p className="col-span-2 text-gray-400 italic text-center py-4">No digital signatories on file.</p>
                          )}
                      </div>
                  </div>
                  
                  <div className="p-4 bg-gray-50 border-t border-gray-200 flex justify-end">
                      <button 
                          onClick={() => setShowMandateModal(false)}
                          className="px-6 py-2 bg-blue-600 text-white font-bold rounded hover:bg-blue-700 flex items-center gap-2"
                      >
                          <CheckCircle size={16} /> Verified
                      </button>
                  </div>
              </div>
          </div>
      );
  };

  return (
    <div className="bg-gray-100 h-full p-4 flex gap-4">
      {showMandateModal && <MandateModal />}

      {/* Left: Input Form */}
      <div className="flex-1 bg-white rounded-lg shadow-sm border border-gray-300 flex flex-col">
        <div className="bg-blue-900 text-white p-3 rounded-t-lg flex justify-between items-center">
          <div className="flex items-center gap-2">
            <h2 className="font-bold tracking-wide">TELLER POSTING (TT)</h2>
            <span className="text-xs bg-yellow-500 text-black px-2 py-0.5 rounded font-bold">BoG LIVE</span>
          </div>
          <span className="text-xs bg-blue-800 px-2 py-1 rounded">O4W API: ONLINE</span>
        </div>

        <div className="p-6 space-y-6 flex-1">
          {/* Account Search */}
          <div className="flex gap-2">
            <div className="flex-1">
              <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Account Number (F2)</label>
              <div className="flex">
                <input 
                  type="text" 
                  value={accountNum}
                  onChange={(e) => setAccountNum(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && findAccount()}
                  className="flex-1 p-2 border border-gray-300 rounded-l focus:ring-2 focus:ring-blue-500 focus:border-blue-500 font-mono text-lg"
                  placeholder="201..."
                />
                <button onClick={findAccount} className="bg-gray-200 px-4 border border-l-0 border-gray-300 rounded-r hover:bg-gray-300">
                  <Search size={18} className="text-gray-600"/>
                </button>
              </div>
            </div>
            <div className="w-1/3">
               <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Transaction Code</label>
               <select 
                value={txType} 
                onChange={(e) => setTxType(e.target.value as any)}
                className="w-full p-2 border border-gray-300 rounded font-medium"
               >
                 <option value="DEPOSIT">Cash Deposit (CD)</option>
                 <option value="WITHDRAWAL">Cash Withdrawal (CW)</option>
               </select>
            </div>
          </div>

          {/* Account Details Box */}
          <div className="bg-blue-50 p-4 rounded border border-blue-100 h-36 relative">
            {selectedAccount ? (
              <>
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <span className="block text-xs text-blue-500">Account Name (CIF)</span>
                      <span className="font-bold text-gray-800">{selectedAccount.cif}</span>
                    </div>
                    <div>
                      <span className="block text-xs text-blue-500">Product</span>
                      <span className="font-bold text-gray-800">{selectedAccount.type} ({selectedAccount.productCode})</span>
                    </div>
                    <div>
                      <span className="block text-xs text-blue-500">Clear Balance</span>
                      <span className="font-mono text-lg text-green-700">GHS {selectedAccount.balance.toLocaleString()}</span>
                    </div>
                     <div>
                      <span className="block text-xs text-red-500">Status / Lien</span>
                      <span className="font-mono text-red-700 flex items-center gap-1">
                          {selectedAccount.status} 
                          {selectedAccount.status === 'FROZEN' && <AlertCircle size={12}/>}
                      </span>
                    </div>
                  </div>
                  <button 
                      onClick={() => setShowMandateModal(true)}
                      className="absolute bottom-4 right-4 bg-white border border-blue-200 text-blue-700 hover:bg-blue-100 text-xs font-bold px-3 py-1.5 rounded flex items-center gap-2 shadow-sm"
                  >
                      <FileSignature size={14} /> Verify Mandate
                  </button>
              </>
            ) : (
              <div className="h-full flex items-center justify-center text-blue-300 italic">
                Enter account number to fetch details...
              </div>
            )}
          </div>

          {/* Amount & Tax Calculation */}
          <div className="grid grid-cols-2 gap-6">
             <div className="col-span-2">
                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Amount (GHS)</label>
                <input 
                  type="number" 
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  className="w-full p-3 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 font-mono text-2xl text-right"
                  placeholder="0.00"
                />
             </div>
             
             {/* BoG E-Levy Section */}
             {txType === 'WITHDRAWAL' && amount && (
                 <div className="col-span-2 bg-yellow-50 border border-yellow-200 p-3 rounded flex justify-between items-center">
                     <div>
                         <div className="text-xs font-bold text-yellow-800 flex items-center gap-1">
                             <ShieldCheck size={12} /> GRA E-Levy (1%)
                         </div>
                         <div className="text-[10px] text-yellow-600">Statutory Tax Payable</div>
                     </div>
                     <div className="text-right">
                         <div className="text-xs text-gray-500">Tax Amount</div>
                         <div className="font-mono font-bold text-red-600">- GHS {tax.toFixed(2)}</div>
                     </div>
                     <div className="h-8 w-px bg-yellow-200 mx-2"></div>
                     <div className="text-right">
                         <div className="text-xs text-gray-500">Total Debit</div>
                         <div className="font-mono font-bold text-gray-900">GHS {totalDebit.toLocaleString(undefined, {minimumFractionDigits: 2})}</div>
                     </div>
                 </div>
             )}

             <div className="col-span-2">
                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Narration</label>
                <input 
                  type="text" 
                  value={narration}
                  onChange={(e) => setNarration(e.target.value)}
                  className="w-full p-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500"
                  placeholder="By Self / Rep by..."
                />
             </div>
          </div>

          {/* Messages */}
          {error && (
            <div className="p-3 bg-red-50 text-red-700 border border-red-200 rounded flex items-center gap-2">
              <AlertCircle size={20} />
              <span className="text-sm font-medium">{error}</span>
            </div>
          )}
           {successMsg && (
            <div className="p-3 bg-green-50 text-green-700 border border-green-200 rounded flex items-center gap-2">
              <CheckCircle size={20} />
              <span className="text-sm font-medium">{successMsg}</span>
            </div>
          )}
          {pendingMsg && (
            <div className="p-3 bg-orange-50 text-orange-700 border border-orange-200 rounded flex items-center gap-2">
              <Clock size={20} />
              <span className="text-sm font-medium">{pendingMsg}</span>
            </div>
          )}
        </div>

        <div className="p-4 border-t border-gray-200 bg-gray-50 rounded-b-lg flex justify-between items-center">
            <div className="text-xs text-gray-400">
               Batch: <span className="font-mono text-gray-600">B-{new Date().toISOString().split('T')[0]}</span>
            </div>
            <div className="flex gap-3">
               <button onClick={() => { setSelectedAccount(null); setAmount(''); setError(null); setSuccessMsg(null); setPendingMsg(null); }} className="px-6 py-2 border border-gray-300 text-gray-600 font-medium rounded hover:bg-gray-100 flex items-center gap-2">
                 <XCircle size={18} /> Clear (F9)
               </button>
               <button 
                onClick={handleSubmit} 
                disabled={!selectedAccount}
                className="px-6 py-2 bg-blue-700 text-white font-bold rounded hover:bg-blue-800 disabled:opacity-50 flex items-center gap-2 shadow-sm"
               >
                 <Save size={18} /> Commit (F10)
               </button>
            </div>
        </div>
      </div>

      {/* Right: Electronic Journal / Last Txns */}
      <div className="w-80 bg-white rounded-lg shadow-sm border border-gray-300 flex flex-col">
        <div className="bg-gray-800 text-white p-3 rounded-t-lg flex justify-between">
           <span className="font-medium text-sm">ELECTRONIC JOURNAL</span>
           <Printer size={16} className="text-gray-400 cursor-pointer hover:text-white" />
        </div>
        <div className="flex-1 overflow-y-auto p-0">
          <table className="w-full text-xs">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="p-2 text-left text-gray-500">Time</th>
                <th className="p-2 text-left text-gray-500">Acct</th>
                <th className="p-2 text-right text-gray-500">Amt</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 font-mono">
               {[1,2,3].map(i => (
                 <tr key={i}>
                   <td className="p-2 text-gray-600">10:4{i}</td>
                   <td className="p-2 text-gray-800">...892</td>
                   <td className="p-2 text-right text-green-700">2,500.00</td>
                 </tr>
               ))}
            </tbody>
          </table>
        </div>
        <div className="p-3 border-t border-gray-200 bg-gray-50">
           <div className="flex justify-between text-sm font-bold text-gray-700">
              <span>Total Cash In:</span>
              <span>7,500.00</span>
           </div>
        </div>
      </div>
    </div>
  );
};

export default TellerTerminal;
```

### components/CompliancePanel.tsx
```tsx
import React from 'react';
import { ComplianceMetric, GLAccount, Account, Loan } from '../types';
import { ShieldCheck, AlertTriangle, FileText, Download, PieChart, Activity } from 'lucide-react';

interface CompliancePanelProps {
  metrics: ComplianceMetric[];
  glAccounts: GLAccount[];
  accounts: Account[];
  loans: Loan[];
}

const CompliancePanel: React.FC<CompliancePanelProps> = ({ metrics, glAccounts, accounts, loans }) => {
  
  // Helper to format currency for XML
  const formatXMLVal = (num: number) => num.toFixed(2);

  // --- GENERATORS ---

  const generateBS2 = (): string => {
      // Income Statement
      const income = glAccounts.filter(g => g.category === 'INCOME').reduce((sum, g) => sum + g.balance, 0);
      const expense = glAccounts.filter(g => g.category === 'EXPENSE').reduce((sum, g) => sum + g.balance, 0);
      const netProfit = income - expense;

      return `<?xml version="1.0" encoding="UTF-8"?>
<BoG_Return_BS2>
    <Header>
        <BankCode>O4W</BankCode>
        <ReportName>INCOME_STATEMENT</ReportName>
        <Period>${new Date().toISOString().slice(0, 7)}</Period>
        <SubmissionDate>${new Date().toISOString()}</SubmissionDate>
        <Currency>GHS</Currency>
    </Header>
    <Data>
        <LineItem code="1000" description="Interest Income">
            <Value>${formatXMLVal(income)}</Value>
        </LineItem>
        <LineItem code="2000" description="Interest Expense">
            <Value>${formatXMLVal(expense)}</Value>
        </LineItem>
        <LineItem code="3000" description="Net Operating Profit">
            <Value>${formatXMLVal(netProfit)}</Value>
        </LineItem>
        <Breakdown>
            ${glAccounts.filter(g => g.category === 'INCOME' || g.category === 'EXPENSE').map(g => `
            <GLAccount code="${g.code}">
                <Name>${g.name}</Name>
                <Balance>${formatXMLVal(g.balance)}</Balance>
            </GLAccount>`).join('')}
        </Breakdown>
    </Data>
    <Hash>${Math.random().toString(36).substring(7)}</Hash>
</BoG_Return_BS2>`;
  };

  const generateBS3 = (): string => {
      // Balance Sheet
      const assets = glAccounts.filter(g => g.category === 'ASSET').reduce((sum, g) => sum + g.balance, 0);
      const liabilities = glAccounts.filter(g => g.category === 'LIABILITY').reduce((sum, g) => sum + g.balance, 0);
      const equity = glAccounts.filter(g => g.category === 'EQUITY').reduce((sum, g) => sum + g.balance, 0);

      return `<?xml version="1.0" encoding="UTF-8"?>
<BoG_Return_BS3>
    <Header>
        <BankCode>O4W</BankCode>
        <ReportName>BALANCE_SHEET</ReportName>
        <Period>${new Date().toISOString().slice(0, 7)}</Period>
    </Header>
    <Data>
        <Section name="ASSETS">
            <Total>${formatXMLVal(assets)}</Total>
            ${glAccounts.filter(g => g.category === 'ASSET').map(g => `<Item code="${g.code}" val="${formatXMLVal(g.balance)}" />`).join('')}
        </Section>
        <Section name="LIABILITIES">
            <Total>${formatXMLVal(liabilities)}</Total>
            ${glAccounts.filter(g => g.category === 'LIABILITY').map(g => `<Item code="${g.code}" val="${formatXMLVal(g.balance)}" />`).join('')}
        </Section>
        <Section name="EQUITY">
            <Total>${formatXMLVal(equity)}</Total>
            ${glAccounts.filter(g => g.category === 'EQUITY').map(g => `<Item code="${g.code}" val="${formatXMLVal(g.balance)}" />`).join('')}
        </Section>
        <Check>
            <Eq>${formatXMLVal(assets - (liabilities + equity))}</Eq>
        </Check>
    </Data>
</BoG_Return_BS3>`;
  };

  const generateLR1 = (): string => {
      // Liquidity Return
      // Liquid Assets: Cash + Bank Balances (Usually GLs 101xx and 102xx)
      const liquidAssets = glAccounts
          .filter(g => g.code.startsWith('101') || g.code.startsWith('102'))
          .reduce((sum, g) => sum + g.balance, 0);
      
      // Volatile Liabilities: Customer Deposits
      const totalDeposits = accounts
          .filter(a => a.type === 'SAVINGS' || a.type === 'CURRENT')
          .reduce((sum, a) => sum + a.balance, 0);

      const ratio = totalDeposits > 0 ? (liquidAssets / totalDeposits) * 100 : 0;

      return `<?xml version="1.0" encoding="UTF-8"?>
<BoG_Return_LR1>
    <Header>
        <BankCode>O4W</BankCode>
        <ReportName>WEEKLY_LIQUIDITY</ReportName>
        <SubmissionDate>${new Date().toISOString()}</SubmissionDate>
    </Header>
    <Data>
        <Metric code="LQ01" name="Total Liquid Assets">
            <Value>${formatXMLVal(liquidAssets)}</Value>
        </Metric>
        <Metric code="LQ02" name="Total Volatile Liabilities">
            <Value>${formatXMLVal(totalDeposits)}</Value>
        </Metric>
        <Metric code="LQ03" name="Liquidity Ratio">
            <Value>${formatXMLVal(ratio)}%</Value>
            <Threshold>> 35%</Threshold>
            <Status>${ratio > 35 ? 'PASS' : 'FAIL'}</Status>
        </Metric>
        <Detail>
            <DepositCount>${accounts.length}</DepositCount>
            <ActiveLoans>${loans.filter(l => l.status === 'ACTIVE').length}</ActiveLoans>
        </Detail>
    </Data>
</BoG_Return_LR1>`;
  };

  const handleGenerateReturn = (reportCode: string) => {
      let xmlContent = '';
      let filename = '';

      switch (reportCode) {
          case 'BS2':
              xmlContent = generateBS2();
              filename = `BoG_BS2_IncomeStmt_${Date.now()}.xml`;
              break;
          case 'BS3':
              xmlContent = generateBS3();
              filename = `BoG_BS3_BalanceSheet_${Date.now()}.xml`;
              break;
          case 'LR1':
              xmlContent = generateLR1();
              filename = `BoG_LR1_Liquidity_${Date.now()}.xml`;
              break;
          default:
              alert("Report template not implemented yet.");
              return;
      }

      const blob = new Blob([xmlContent], { type: 'application/xml' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
  };

  return (
    <div className="h-full flex flex-col gap-6">
       {/* Header */}
       <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
          <div className="flex justify-between items-start">
             <div>
                <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
                    <ShieldCheck className="text-green-600" />
                    BoG ORASS Compliance
                </h2>
                <p className="text-sm text-gray-500 mt-1">
                    Bank of Ghana Online Regulatory Analytics Surveillance Systems (ORASS)
                </p>
             </div>
             <div className="flex gap-2">
                 <button 
                    onClick={() => handleGenerateReturn('BS2')}
                    className="flex items-center gap-2 px-3 py-2 bg-indigo-50 text-indigo-700 text-sm font-medium rounded-lg hover:bg-indigo-100 transition-colors"
                >
                    <FileText size={16} /> Generate Monthly Return
                 </button>
             </div>
          </div>
       </div>

       {/* Metrics Grid */}
       <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {metrics.map((m, idx) => (
             <div key={idx} className="bg-white p-5 rounded-xl border border-gray-100 shadow-sm relative overflow-hidden">
                <div className={`absolute top-0 left-0 w-1 h-full ${
                    m.status === 'pass' ? 'bg-green-500' : m.status === 'warning' ? 'bg-yellow-500' : 'bg-red-500'
                }`}></div>
                <div className="flex justify-between items-start mb-2">
                    <span className="text-xs font-bold text-gray-400 uppercase tracking-wider">{m.code}</span>
                    {m.status === 'fail' && <AlertTriangle size={16} className="text-red-500" />}
                </div>
                <h3 className="text-gray-600 text-sm font-medium">{m.label}</h3>
                <div className="mt-2 flex items-baseline gap-2">
                    <span className="text-2xl font-bold text-gray-900">{m.value}</span>
                    <span className="text-xs text-gray-500">Target: {m.threshold}</span>
                </div>
             </div>
          ))}
       </div>

       {/* Reports Section */}
       <div className="flex-1 bg-white rounded-xl shadow-sm border border-gray-100 p-6">
          <h3 className="font-semibold text-gray-800 mb-4">Prudential Returns Submission (ORASS)</h3>
          <table className="w-full text-sm text-left">
             <thead className="bg-gray-50 text-gray-500 font-medium">
                <tr>
                   <th className="p-3">Report Code</th>
                   <th className="p-3">Description</th>
                   <th className="p-3">Frequency</th>
                   <th className="p-3">Due Date</th>
                   <th className="p-3">Status</th>
                   <th className="p-3">Action</th>
                </tr>
             </thead>
             <tbody className="divide-y divide-gray-100">
                <tr className="hover:bg-gray-50">
                   <td className="p-3 font-mono text-blue-600 font-bold">BS2</td>
                   <td className="p-3 font-medium flex items-center gap-2"><PieChart size={14} className="text-gray-400"/> Income Statement</td>
                   <td className="p-3">Monthly</td>
                   <td className="p-3 text-red-600 font-medium">Oct 14, 2023</td>
                   <td className="p-3"><span className="px-2 py-1 bg-yellow-100 text-yellow-700 rounded text-xs font-bold">Pending Review</span></td>
                   <td className="p-3">
                       <button onClick={() => handleGenerateReturn('BS2')} className="p-2 hover:bg-indigo-50 rounded-full text-gray-400 hover:text-indigo-600 transition-colors" title="Download XML">
                           <Download size={16} />
                       </button>
                   </td>
                </tr>
                <tr className="hover:bg-gray-50">
                   <td className="p-3 font-mono text-blue-600 font-bold">BS3</td>
                   <td className="p-3 font-medium flex items-center gap-2"><Activity size={14} className="text-gray-400"/> Balance Sheet</td>
                   <td className="p-3">Monthly</td>
                   <td className="p-3">Oct 14, 2023</td>
                   <td className="p-3"><span className="px-2 py-1 bg-yellow-100 text-yellow-700 rounded text-xs font-bold">Pending Review</span></td>
                   <td className="p-3">
                       <button onClick={() => handleGenerateReturn('BS3')} className="p-2 hover:bg-indigo-50 rounded-full text-gray-400 hover:text-indigo-600 transition-colors" title="Download XML">
                           <Download size={16} />
                       </button>
                   </td>
                </tr>
                <tr className="hover:bg-gray-50">
                   <td className="p-3 font-mono text-blue-600 font-bold">LR1</td>
                   <td className="p-3 font-medium flex items-center gap-2"><Activity size={14} className="text-gray-400"/> Weekly Liquidity Return</td>
                   <td className="p-3">Weekly</td>
                   <td className="p-3">Oct 06, 2023</td>
                   <td className="p-3"><span className="px-2 py-1 bg-green-100 text-green-700 rounded text-xs font-bold">Submitted</span></td>
                   <td className="p-3">
                       <button onClick={() => handleGenerateReturn('LR1')} className="p-2 hover:bg-indigo-50 rounded-full text-gray-400 hover:text-indigo-600 transition-colors" title="Download XML">
                           <Download size={16} />
                       </button>
                   </td>
                </tr>
             </tbody>
          </table>
       </div>
    </div>
  );
};

export default CompliancePanel;
```

### components/MigrationWizard.tsx
```tsx
import React, { useState } from 'react';
import { Upload, ArrowRight, ChevronRight, Play, FileSpreadsheet, RefreshCw, Server } from 'lucide-react';
import { MigrationFieldMapping, MigrationLog } from '../types';
import { autoMapMigrationFields, generateMigrationBasicPlusCode } from '../services/geminiService';

// --- EXISTING MIGRATION CONSTANTS ---
const TARGET_DICTIONARIES = [
    'CIF_NO', 'FULL_NAME', 'GHANA_CARD_ID', 'DIGITAL_ADDRESS_GPS', 
    'PHONE_MOBILE', 'EMAIL_ADDR', 'KYC_TIER', 'DATE_OF_BIRTH', 
    'NEXT_OF_KIN', 'RELATIONSHIP_MGR'
];

const MOCK_CSV_DATA = [
    { "Client_ID": "OLD_001", "Client_Name": "Kwame Mensah", "Nat_ID": "GHA-728192823-1", "Phone": "0244123456", "GPS": "GA-100-2020" },
    { "Client_ID": "OLD_002", "Client_Name": "Adwoa Boateng", "Nat_ID": "GHA-111222333-X", "Phone": "0509988776", "GPS": "AK-039-2911" },
    { "Client_ID": "OLD_003", "Client_Name": "Kofi Annan", "Nat_ID": "INVALID-ID", "Phone": "0201122334", "GPS": "WR-222-1111" },
    { "Client_ID": "OLD_004", "Client_Name": "Ama Serwaa", "Nat_ID": "GHA-999888777-2", "Phone": "0245556666", "GPS": "ER-333-4444" },
    { "Client_ID": "OLD_005", "Client_Name": "Yaw Osei", "Nat_ID": "GHA-123456789-0", "Phone": "0277778888", "GPS": "AS-555-6666" },
];

const MigrationWizard: React.FC = () => {
    const [step, setStep] = useState<1 | 2 | 3 | 4>(1);
    const [rawData, setRawData] = useState<any[]>([]);
    const [headers, setHeaders] = useState<string[]>([]);
    const [mappings, setMappings] = useState<MigrationFieldMapping[]>([]);
    const [logs, setLogs] = useState<MigrationLog[]>([]);
    const [progress, setProgress] = useState(0);
    const [analyzing, setAnalyzing] = useState(false);
    const [generatedScript, setGeneratedScript] = useState('');
    const [generatingScript, setGeneratingScript] = useState(false);

    // --- MIGRATION HANDLERS ---
    const handleFileUpload = () => {
        setAnalyzing(true);
        setTimeout(async () => {
            const data = MOCK_CSV_DATA;
            const heads = Object.keys(data[0]);
            setRawData(data);
            setHeaders(heads);
            const suggestions = await autoMapMigrationFields(heads, TARGET_DICTIONARIES);
            setMappings(suggestions);
            setAnalyzing(false);
            setStep(2);
        }, 1500);
    };

    const handleMappingChange = (legacyHeader: string, newDict: string) => {
        setMappings(prev => prev.map(m => m.legacyHeader === legacyHeader ? { ...m, targetDict: newDict, confidence: 1 } : m));
    };

    const handleGenerateScript = async () => {
        setGeneratingScript(true);
        const script = await generateMigrationBasicPlusCode(mappings, "CUST_ACCOUNTS");
        setGeneratedScript(script);
        setGeneratingScript(false);
    };

    const executeMigration = () => {
        setStep(4);
        setLogs([]);
        setProgress(0);
        let currentIndex = 0;
        const total = rawData.length;
        const interval = setInterval(() => {
            if (currentIndex >= total) {
                clearInterval(interval);
                return;
            }
            const row = rawData[currentIndex];
            const ghanaCardHeader = mappings.find(m => m.targetDict === 'GHANA_CARD_ID')?.legacyHeader;
            const ghanaCardValue = ghanaCardHeader ? row[ghanaCardHeader] : '';
            const isValidID = /^GHA-\d{9}-\d{1}$/.test(ghanaCardValue);
            const newLog: MigrationLog = {
                row: currentIndex + 1,
                status: isValidID ? 'SUCCESS' : 'ERROR',
                message: isValidID ? 'Record written to DATA_VOL' : 'Validation Failed: Invalid Ghana Card Format',
                data: JSON.stringify(row)
            };
            setLogs(prev => [newLog, ...prev]);
            setProgress(Math.round(((currentIndex + 1) / total) * 100));
            currentIndex++;
        }, 800);
    };

    return (
        <div className="h-full flex flex-col bg-gray-50 rounded-xl overflow-hidden">
            {/* Header */}
            <div className="bg-white px-6 py-4 border-b border-gray-200">
                <div className="flex justify-between items-center">
                     <div>
                         <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
                            <Server className="text-blue-700" size={24} />
                            Data Migration Utility
                        </h1>
                        <p className="text-gray-500 text-sm mt-1">
                            Import legacy data into OpenInsight linear hash files.
                        </p>
                    </div>
                </div>
            </div>

            {/* Content */}
            <div className="flex-1 p-8 overflow-y-auto">
                <div className="flex items-center gap-2 text-sm font-medium text-gray-500 mb-6">
                    <span className={`px-3 py-1 rounded-full ${step >= 1 ? 'bg-blue-100 text-blue-800' : 'bg-gray-100'}`}>1. Source</span>
                    <ChevronRight size={16} />
                    <span className={`px-3 py-1 rounded-full ${step >= 2 ? 'bg-blue-100 text-blue-800' : 'bg-gray-100'}`}>2. Map</span>
                    <ChevronRight size={16} />
                    <span className={`px-3 py-1 rounded-full ${step >= 3 ? 'bg-blue-100 text-blue-800' : 'bg-gray-100'}`}>3. Review</span>
                    <ChevronRight size={16} />
                    <span className={`px-3 py-1 rounded-full ${step >= 4 ? 'bg-blue-100 text-blue-800' : 'bg-gray-100'}`}>4. Load</span>
                </div>

                {step === 1 && (
                    <div className="h-full flex flex-col items-center justify-center border-2 border-dashed border-gray-300 rounded-xl bg-white p-12 text-center">
                        <div className="w-16 h-16 bg-blue-50 text-blue-600 rounded-full flex items-center justify-center mb-4">
                            <Upload size={32} />
                        </div>
                        <h3 className="text-xl font-semibold text-gray-900 mb-2">Upload Legacy Data File</h3>
                        <p className="text-gray-500 mb-6 max-w-md">Supported formats: CSV, Excel, XML.</p>
                        <button onClick={handleFileUpload} disabled={analyzing} className="px-6 py-3 bg-blue-700 text-white font-medium rounded-lg hover:bg-blue-800 flex items-center gap-2 disabled:opacity-50">
                            {analyzing ? 'Analyzing...' : <><FileSpreadsheet size={18} /> Select File</>}
                        </button>
                    </div>
                )}
                
                {step === 2 && (
                    <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
                            <div className="p-4 bg-gray-50 border-b border-gray-200">
                            <h3 className="font-semibold text-gray-700">Dictionary Mapping</h3>
                            </div>
                            <table className="w-full text-sm text-left">
                            <thead className="bg-gray-50 text-gray-500 uppercase font-medium"><tr><th className="p-4">Legacy Header</th><th className="p-4 text-center">Mapping</th><th className="p-4">Target Dictionary</th></tr></thead>
                            <tbody className="divide-y divide-gray-100">
                                {mappings.map((map, idx) => (
                                    <tr key={idx}>
                                        <td className="p-4 font-mono text-gray-800">{map.legacyHeader}</td>
                                        <td className="p-4 text-center text-gray-400"><ArrowRight size={16} className="mx-auto"/></td>
                                        <td className="p-4">
                                            <select value={map.targetDict} onChange={(e) => handleMappingChange(map.legacyHeader, e.target.value)} className={`w-full p-2 border rounded ${map.confidence > 0.8 ? 'bg-green-50 border-green-300' : ''}`}>
                                                <option value="">-- Ignore --</option>
                                                {TARGET_DICTIONARIES.map(d => <option key={d} value={d}>{d}</option>)}
                                            </select>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                            </table>
                            <div className="p-4 border-t border-gray-200 flex justify-end">
                            <button onClick={() => setStep(3)} className="px-6 py-2 bg-blue-700 text-white rounded-lg">Next</button>
                            </div>
                    </div>
                )}
                
                {step === 3 && (
                    <div className="max-w-3xl mx-auto space-y-6">
                            <div className="bg-blue-50 border border-blue-200 rounded-xl p-6">
                            <h3 className="text-lg font-bold text-blue-900 mb-2">Summary</h3>
                            <div className="text-sm">Mapping {mappings.filter(m => m.targetDict).length} fields for {rawData.length} records.</div>
                            </div>
                            <div className="bg-gray-900 rounded-xl overflow-hidden border border-gray-700">
                            <div className="px-4 py-3 bg-gray-800 border-b border-gray-700 flex justify-between items-center text-white">
                                <span className="font-mono text-sm">Import Script</span>
                                <button onClick={handleGenerateScript} disabled={generatingScript} className="text-xs bg-blue-700 px-3 py-1 rounded">Generate</button>
                            </div>
                            {generatedScript && <div className="p-4"><textarea readOnly value={generatedScript} className="w-full h-48 bg-transparent text-green-400 font-mono text-xs resize-none focus:outline-none"/></div>}
                            </div>
                            <div className="flex justify-end gap-3">
                                <button onClick={() => setStep(2)} className="px-6 py-2 border rounded-lg">Back</button>
                                <button onClick={executeMigration} className="px-6 py-2 bg-green-600 text-white rounded-lg flex items-center gap-2"><Play size={16}/> Start</button>
                            </div>
                    </div>
                )}
                
                {step === 4 && (
                    <div className="space-y-6">
                            <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
                            <div className="flex justify-between mb-2"><span className="font-semibold">Importing...</span><span className="font-mono text-blue-600">{progress}%</span></div>
                            <div className="w-full bg-gray-100 rounded-full h-3 overflow-hidden"><div className="bg-blue-600 h-3 rounded-full transition-all duration-300" style={{ width: `${progress}%` }}></div></div>
                            </div>
                            <div className="bg-gray-900 rounded-xl overflow-hidden border border-gray-800 h-96 flex flex-col">
                            <div className="px-4 py-2 bg-gray-800 text-gray-400 font-mono text-xs">MIGRATION_LOG.TXT</div>
                            <div className="flex-1 p-4 overflow-y-auto font-mono text-xs space-y-1">
                                {logs.map((log, i) => (
                                    <div key={i} className="flex gap-3"><span className={log.status === 'SUCCESS' ? 'text-green-500' : 'text-red-500'}>{log.status}</span><span className="text-gray-300">{log.message}</span></div>
                                ))}
                            </div>
                            </div>
                            {progress === 100 && <div className="flex justify-end"><button onClick={() => { setStep(1); setGeneratedScript(''); }} className="px-6 py-2 bg-blue-700 text-white rounded-lg">New Job</button></div>}
                    </div>
                )}
            </div>
        </div>
    );
};

export default MigrationWizard;
```

### components/AuditTrail.tsx
```tsx
import React, { useState } from 'react';
import { AuditLog } from '../types';
import { Search, Filter, Shield, Clock, AlertCircle, CheckCircle, FileText, Lock } from 'lucide-react';

interface AuditTrailProps {
  logs: AuditLog[];
}

const AuditTrail: React.FC<AuditTrailProps> = ({ logs }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filterModule, setFilterModule] = useState<string>('ALL');

  const filteredLogs = logs.filter(log => {
    const matchesSearch = 
      log.user.toLowerCase().includes(searchTerm.toLowerCase()) || 
      log.action.toLowerCase().includes(searchTerm.toLowerCase()) ||
      log.id.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesModule = filterModule === 'ALL' || log.module === filterModule;
    return matchesSearch && matchesModule;
  });

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-100 flex flex-col h-full overflow-hidden">
      {/* Header */}
      <div className="p-6 border-b border-gray-200">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
              <Shield className="text-blue-700" size={24} />
              System Audit Trail
            </h2>
            <p className="text-gray-500 text-sm mt-1">
              Immutable event log stored in Linear Hash (AUDIT_HIST). 
              <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-800">
                <Lock size={10} className="mr-1"/> Indexed
              </span>
            </p>
          </div>
          <div className="flex gap-2">
             <button className="flex items-center gap-2 px-3 py-2 bg-gray-100 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-200 border border-gray-300">
                <FileText size={16} /> Export Log
             </button>
          </div>
        </div>

        {/* Filters */}
        <div className="flex gap-4 items-center">
            <div className="relative flex-1 max-w-md">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
                <input 
                    type="text" 
                    placeholder="Search User, Action or Event ID..." 
                    className="w-full pl-9 pr-4 py-2 bg-gray-50 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                />
            </div>
            <div className="flex items-center gap-2">
                <Filter size={16} className="text-gray-500" />
                <select 
                    className="bg-gray-50 border border-gray-300 text-gray-700 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block p-2"
                    value={filterModule}
                    onChange={(e) => setFilterModule(e.target.value)}
                >
                    <option value="ALL">All Modules</option>
                    <option value="TELLER">Teller</option>
                    <option value="SYSTEM">System</option>
                    <option value="SECURITY">Security</option>
                    <option value="GL">General Ledger</option>
                </select>
            </div>
        </div>
      </div>

      {/* Log Table */}
      <div className="flex-1 overflow-auto">
        <table className="w-full text-left text-sm">
            <thead className="bg-gray-50 text-gray-600 font-medium sticky top-0 z-10">
                <tr>
                    <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Event ID</th>
                    <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Timestamp</th>
                    <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">User</th>
                    <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Module</th>
                    <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Action</th>
                    <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Status</th>
                    <th className="px-6 py-3 font-mono text-xs uppercase tracking-wider">Details</th>
                </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
                {filteredLogs.map((log) => (
                    <tr key={log.id} className="hover:bg-gray-50 transition-colors group">
                        <td className="px-6 py-3 font-mono text-gray-500 text-xs">{log.id}</td>
                        <td className="px-6 py-3 text-gray-700 whitespace-nowrap flex items-center gap-2">
                            <Clock size={14} className="text-gray-400" />
                            {new Date(log.timestamp).toLocaleString()}
                        </td>
                        <td className="px-6 py-3 font-medium text-gray-900">
                            <span className="bg-blue-50 text-blue-700 px-2 py-0.5 rounded text-xs font-mono">
                                {log.user}
                            </span>
                        </td>
                        <td className="px-6 py-3 text-gray-600">{log.module}</td>
                        <td className="px-6 py-3 font-medium text-gray-800">{log.action}</td>
                        <td className="px-6 py-3">
                            {log.status === 'SUCCESS' && (
                                <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-green-50 text-green-700">
                                    <CheckCircle size={12} /> Success
                                </span>
                            )}
                            {log.status === 'FAILURE' && (
                                <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-red-50 text-red-700">
                                    <AlertCircle size={12} /> Failure
                                </span>
                            )}
                            {log.status === 'WARNING' && (
                                <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-orange-50 text-orange-700">
                                    <AlertCircle size={12} /> Warning
                                </span>
                            )}
                        </td>
                         <td className="px-6 py-3 text-gray-500 truncate max-w-xs" title={log.details}>
                            {log.details}
                        </td>
                    </tr>
                ))}
            </tbody>
        </table>
        {filteredLogs.length === 0 && (
            <div className="flex flex-col items-center justify-center h-48 text-gray-500">
                <Search size={32} className="mb-2 opacity-20" />
                <p>No audit logs found matching criteria.</p>
            </div>
        )}
      </div>
      
      {/* Footer */}
      <div className="p-3 border-t border-gray-200 bg-gray-50 text-xs text-gray-500 flex justify-between items-center">
        <span>Showing {filteredLogs.length} records</span>
        <span className="font-mono">INDEX: IDX_AUDIT_DATE_TIME</span>
      </div>
    </div>
  );
};

export default AuditTrail;
```

### components/LoanEngine.tsx
```tsx
import React, { useState, useEffect, useMemo } from 'react';
import { Loan, AmortizationSchedule, Customer, Group, Product } from '../types';
import { Plus, Calculator, Calendar, User, DollarSign, FileText, CheckCircle, XCircle, AlertCircle, ShieldAlert, BadgeCheck, ChevronRight, ChevronLeft, Upload, File, Check, Loader2, Users, PieChart, Wallet, Save, TrendingUp } from 'lucide-react';

interface LoanEngineProps {
  loans: Loan[];
  setLoans: React.Dispatch<React.SetStateAction<Loan[]>>;
  onDisburse: (loanId: string) => void;
  customers: Customer[];
  groups?: Group[];
  products: Product[]; // Now receives dynamic products
}

const STEPS = [
    { id: 1, label: 'Borrower Info' },
    { id: 2, label: 'Loan Terms' },
    { id: 3, label: 'Collateral & Docs' },
    { id: 4, label: 'Review & Submit' }
];

const LoanEngine: React.FC<LoanEngineProps> = ({ loans, setLoans, onDisburse, customers, groups = [], products = [] }) => {
  const [view, setView] = useState<'LIST' | 'WIZARD' | 'DETAILS'>('LIST');
  const [selectedLoan, setSelectedLoan] = useState<Loan | null>(null);
  const [schedule, setSchedule] = useState<AmortizationSchedule[]>([]);
  const [customerDetails, setCustomerDetails] = useState<Customer | null>(null);
  const [groupDetails, setGroupDetails] = useState<Group | null>(null);
  const [validationError, setValidationError] = useState<string>('');

  // Group Loan State
  const [memberSplits, setMemberSplits] = useState<Record<string, number>>({});
  const [memberContributions, setMemberContributions] = useState<Record<string, number>>({});

  // Filter for only LOAN products
  const loanProducts = useMemo(() => products.filter(p => p.type === 'LOAN' && p.status === 'ACTIVE'), [products]);

  // Wizard State
  const [wizardStep, setWizardStep] = useState(1);
  const [uploadedDocs, setUploadedDocs] = useState<string[]>([]);
  const [submissionStatus, setSubmissionStatus] = useState<'IDLE' | 'SUBMITTING' | 'SUCCESS'>('IDLE');

  // Origination Form State
  const [formData, setFormData] = useState({
    borrowerType: 'INDIVIDUAL' as 'INDIVIDUAL' | 'GROUP',
    cif: '', // Holds CIF for Individual, GroupID for Group
    productCode: '', // Changed to store ID instead of Name
    principal: 0,
    rate: 0,
    term: 0,
    collateralType: 'Unsecured',
    collateralValue: ''
  });

  // Set default product when products load
  useEffect(() => {
      if (loanProducts.length > 0 && !formData.productCode) {
          const defaultProd = loanProducts[0];
          setFormData(prev => ({
              ...prev,
              productCode: defaultProd.id,
              principal: defaultProd.minAmount || 0,
              rate: defaultProd.interestRate,
              term: defaultProd.defaultTerm || 12
          }));
      }
  }, [loanProducts]);

  // Validate Borrower when type or ID changes
  useEffect(() => {
    setCustomerDetails(null);
    setGroupDetails(null);
    setValidationError('');
    setMemberSplits({});

    if (formData.cif.length >= 3) {
        if (formData.borrowerType === 'INDIVIDUAL') {
            const cust = customers.find(c => c.id === formData.cif);
            if (cust) {
                setCustomerDetails(cust);
            } else {
                setValidationError('Customer not found');
            }
        } else {
            const grp = groups.find(g => g.id === formData.cif);
            if (grp) {
                setGroupDetails(grp);
                // Initialize splits
                const initialSplits: Record<string, number> = {};
                grp.members.forEach(m => initialSplits[m] = 0);
                setMemberSplits(initialSplits);
            } else {
                setValidationError('Group not found');
            }
        }
    }
  }, [formData.cif, formData.borrowerType, customers, groups]);

  // Reset wizard when opening new app
  useEffect(() => {
    if (view === 'WIZARD') {
        setWizardStep(1);
        setUploadedDocs([]);
        setSubmissionStatus('IDLE');
        // Reset form
        const defaultProd = loanProducts[0];
        setFormData({
            borrowerType: 'INDIVIDUAL',
            cif: '',
            productCode: defaultProd?.id || '',
            principal: defaultProd?.minAmount || 1000,
            rate: defaultProd?.interestRate || 0,
            term: defaultProd?.defaultTerm || 12,
            collateralType: 'Unsecured',
            collateralValue: ''
        });
        setCustomerDetails(null);
        setGroupDetails(null);
        setSchedule([]);
        setMemberSplits({});
    }
  }, [view]);

  // Reset contribution state when details view opens
  useEffect(() => {
      if (view === 'DETAILS') {
          setMemberContributions({});
      }
  }, [view, selectedLoan]);

  // Auto-calculate schedule when terms change
  useEffect(() => {
    if (view === 'WIZARD' && formData.principal > 0 && formData.term > 0) {
        calculateSchedule(formData.principal, formData.rate, formData.term);
    }
  }, [formData.principal, formData.rate, formData.term, view]);

  // Group Splits Logic
  const handleSplitChange = (memberId: string, value: string) => {
      const val = parseFloat(value) || 0;
      setMemberSplits(prev => {
          const next = { ...prev, [memberId]: val };
          // Calculate new total principal
          const newTotal = Object.values(next).reduce((a, b) => a + b, 0);
          setFormData(f => ({ ...f, principal: newTotal }));
          return next;
      });
  };

  // Update defaults when product changes
  const handleProductChange = (prodCode: string) => {
      const product = loanProducts.find(p => p.id === prodCode);
      if (product) {
          setFormData(prev => ({
              ...prev,
              productCode: prodCode,
              rate: product.interestRate,
              term: product.defaultTerm || 12,
              principal: Math.max(prev.principal, product.minAmount) // Ensure min amount
          }));
      }
  };

  // Determine required documents based on product
  const requiredDocs = useMemo(() => {
      if (formData.borrowerType === 'GROUP') {
          return ['Group Constitution', 'Member List', 'Guarantor Forms'];
      }
      const docs = ['National ID (Ghana Card)', 'Passport Photo'];
      // Logic could be extended to check Product metadata for doc requirements
      if (formData.collateralType === 'Landed Property') {
          docs.push('Proof of Address', 'Indenture / Land Title');
      } else {
          docs.push('Proof of Address');
      }
      return docs;
  }, [formData.productCode, formData.collateralType, formData.borrowerType]);

  const allPossibleDocs = useMemo(() => {
      const base = ['National ID (Ghana Card)', 'Proof of Address', 'Recent Pay Slip', 'Passport Photo', 'Indenture / Land Title', 'Business Registration Cert', 'Group Constitution', 'Member List', 'Guarantor Forms'];
      return Array.from(new Set([...base, ...requiredDocs]));
  }, [requiredDocs]);

  const calculateSchedule = (principal: number, rate: number, term: number) => {
    const newSchedule: AmortizationSchedule[] = [];
    const startDate = new Date();
    
    let payment = 0;
    
    if (term <= 0) return;

    if (rate <= 0) {
        payment = principal / term;
        let balance = principal;
        for (let i = 1; i <= term; i++) {
            balance -= payment;
            if (i === term && Math.abs(balance) < 0.01) balance = 0;
            startDate.setMonth(startDate.getMonth() + 1);
            newSchedule.push({
                period: i,
                dueDate: startDate.toISOString().split('T')[0],
                principal: payment,
                interest: 0,
                total: payment,
                balance: balance > 0 ? balance : 0,
                status: 'DUE'
            });
        }
    } else {
        const monthlyRate = (rate / 100) / 12;
        payment = (principal * monthlyRate) / (1 - Math.pow(1 + monthlyRate, -term));
        
        let balance = principal;
        for (let i = 1; i <= term; i++) {
            const interest = balance * monthlyRate;
            const principalPart = payment - interest;
            balance -= principalPart;
            if (i === term && Math.abs(balance) < 1) balance = 0;
            startDate.setMonth(startDate.getMonth() + 1);
            newSchedule.push({
                period: i,
                dueDate: startDate.toISOString().split('T')[0],
                principal: principalPart,
                interest: interest,
                total: payment,
                balance: balance > 0 ? balance : 0,
                status: 'DUE'
            });
        }
    }
    setSchedule(newSchedule);
  };

  const handleNextStep = () => {
    setValidationError('');
    const selectedProd = loanProducts.find(p => p.id === formData.productCode);

    if (wizardStep === 1) {
        if (formData.borrowerType === 'INDIVIDUAL' && !customerDetails) {
            setValidationError('Please select a valid customer.');
            return;
        }
        if (formData.borrowerType === 'GROUP' && !groupDetails) {
            setValidationError('Please select a valid group.');
            return;
        }
    }
    
    if (wizardStep === 2) {
        if (formData.principal <= 0 || formData.term <= 0) {
            setValidationError('Principal and Term must be positive.');
            return;
        }
        if (selectedProd) {
            if (formData.principal < selectedProd.minAmount) {
                setValidationError(`Minimum principal for this product is GHS ${selectedProd.minAmount.toLocaleString()}`);
                return;
            }
            if (selectedProd.maxAmount && formData.principal > selectedProd.maxAmount) {
                setValidationError(`Maximum principal for this product is GHS ${selectedProd.maxAmount.toLocaleString()}`);
                return;
            }
            if (selectedProd.minTerm && formData.term < selectedProd.minTerm) {
                setValidationError(`Minimum term is ${selectedProd.minTerm} months`);
                return;
            }
            if (selectedProd.maxTerm && formData.term > selectedProd.maxTerm) {
                setValidationError(`Maximum term is ${selectedProd.maxTerm} months`);
                return;
            }
        }
        if (formData.borrowerType === 'INDIVIDUAL' && customerDetails?.kycLevel === 'Tier 1' && formData.principal > 5000) {
             setValidationError('Tier 1 Customers limited to GHS 5,000.');
             return;
        }
        // Group Validation: Ensure all members have allocation if needed, or total matches
        if (formData.borrowerType === 'GROUP' && groupDetails) {
             const sum = Object.values(memberSplits).reduce((a,b)=>a+b, 0);
             if (Math.abs(sum - formData.principal) > 1) {
                 setValidationError(`Member splits sum (${sum}) does not match principal (${formData.principal}).`);
                 return;
             }
        }
    }

    if (wizardStep === 3) {
        const missing = requiredDocs.filter(doc => !uploadedDocs.includes(doc));
        if (missing.length > 0) {
            setValidationError(`Missing required documents: ${missing.join(', ')}`);
            return;
        }
        // Ensure schedule is fresh
        calculateSchedule(formData.principal, formData.rate, formData.term);
    }

    setWizardStep(prev => prev + 1);
  };

  const handleBackStep = () => {
      setWizardStep(prev => prev - 1);
  };

  const handleSimulateUpload = (docName: string) => {
      if (!uploadedDocs.includes(docName)) {
          setUploadedDocs(prev => [...prev, docName]);
          // Clear error if it was about docs
          if (validationError.startsWith('Missing required')) {
              setValidationError('');
          }
      }
  };

  const saveLoan = () => {
      setSubmissionStatus('SUBMITTING');
      const selectedProd = loanProducts.find(p => p.id === formData.productCode);
      
      // Simulate network request
      setTimeout(() => {
          const newLoan: Loan = {
              id: `LN${Date.now().toString().slice(-6)}`,
              cif: formData.borrowerType === 'INDIVIDUAL' ? formData.cif : '', // Legacy Field
              groupId: formData.borrowerType === 'GROUP' ? formData.cif : undefined,
              memberSplits: formData.borrowerType === 'GROUP' ? memberSplits : undefined,
              productName: selectedProd?.name || 'Unknown Loan',
              productCode: selectedProd?.id,
              principal: formData.principal,
              rate: formData.rate,
              termMonths: formData.term,
              disbursementDate: new Date().toISOString().split('T')[0],
              parBucket: '0',
              outstandingBalance: formData.principal,
              collateralType: formData.collateralType,
              status: 'PENDING'
          };
          setLoans(prev => [newLoan, ...prev]);
          setSubmissionStatus('SUCCESS');
      }, 2000);
  };

  // Group Contribution Helpers
  const handleContributionChange = (memberId: string, amount: string) => {
      setMemberContributions(prev => ({...prev, [memberId]: parseFloat(amount) || 0}));
  };

  const totalContribution = Object.values(memberContributions).reduce((a, b) => a + b, 0);

  const submitGroupRepayment = () => {
      if (totalContribution <= 0 || !selectedLoan) return;
      
      setLoans(prev => prev.map(l => {
          if (l.id === selectedLoan.id) {
              const newBal = l.outstandingBalance - totalContribution;
              return { ...l, outstandingBalance: Math.max(0, newBal) };
          }
          return l;
      }));
      
      // Update selected loan local state to reflect change immediately in view
      setSelectedLoan(prev => prev ? ({ ...prev, outstandingBalance: Math.max(0, prev.outstandingBalance - totalContribution) }) : null);
      
      setMemberContributions({});
      alert(`Group Repayment of GHS ${totalContribution.toLocaleString()} processed successfully.`);
  };

  return (
    <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
        <h2 className="text-lg font-bold text-gray-800 flex items-center gap-2">
            <Calculator className="text-blue-600" /> Loan Engine
        </h2>
        {view === 'LIST' && (
            <button 
                onClick={() => setView('WIZARD')}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 flex items-center gap-2"
            >
                <Plus size={16} /> New Application
            </button>
        )}
        {view !== 'LIST' && (
             <button 
                onClick={() => setView('LIST')}
                className="text-gray-600 hover:text-blue-600 text-sm font-medium"
            >
                &larr; Back to Portfolio
            </button>
        )}
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-6">
        
        {/* VIEW: LIST */}
        {view === 'LIST' && (
             <div className="overflow-x-auto">
                <table className="w-full text-sm text-left">
                    <thead className="bg-gray-100 text-gray-600 font-semibold uppercase text-xs">
                        <tr>
                            <th className="p-3">Loan ID</th>
                            <th className="p-3">Borrower</th>
                            <th className="p-3">Type</th>
                            <th className="p-3">Product</th>
                            <th className="p-3 text-right">Principal</th>
                            <th className="p-3 text-right">Outstanding</th>
                            <th className="p-3 text-center">Status</th>
                            <th className="p-3 text-center">Action</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {loans.map(loan => (
                            <tr key={loan.id} className="hover:bg-gray-50">
                                <td className="p-3 font-mono text-blue-600 font-medium">{loan.id}</td>
                                <td className="p-3">
                                    {loan.groupId ? 
                                        <span className="flex items-center gap-1"><Users size={12} className="text-purple-500"/> {loan.groupId}</span> : 
                                        <span className="flex items-center gap-1"><User size={12} className="text-gray-500"/> {loan.cif}</span>
                                    }
                                </td>
                                <td className="p-3 text-xs">{loan.groupId ? 'Group' : 'Individual'}</td>
                                <td className="p-3">{loan.productName}</td>
                                <td className="p-3 text-right font-mono">{loan.principal.toLocaleString()}</td>
                                <td className="p-3 text-right font-mono font-bold text-gray-800">{loan.outstandingBalance.toLocaleString()}</td>
                                <td className="p-3 text-center">
                                     <span className={`px-2 py-1 rounded-full text-xs font-bold border ${
                                        loan.status === 'ACTIVE' ? 'bg-green-50 text-green-700 border-green-200' : 
                                        loan.status === 'WRITTEN_OFF' ? 'bg-red-50 text-red-700 border-red-200' :
                                        'bg-gray-50 text-gray-700 border-gray-200'
                                    }`}>
                                        {loan.status}
                                    </span>
                                </td>
                                <td className="p-3 text-center">
                                    <button 
                                        onClick={() => { setSelectedLoan(loan); calculateSchedule(loan.principal, loan.rate, loan.termMonths); setView('DETAILS'); }}
                                        className="text-blue-600 hover:text-blue-800 text-xs font-medium"
                                    >
                                        View
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
             </div>
        )}

        {/* VIEW: WIZARD (ORIGINATE) */}
        {view === 'WIZARD' && (
            <div className="max-w-4xl mx-auto">
                {/* Submission Success State */}
                {submissionStatus === 'SUCCESS' ? (
                    <div className="flex flex-col items-center justify-center h-96 text-center animate-in fade-in zoom-in-95 duration-500">
                        <div className="w-20 h-20 bg-green-100 text-green-600 rounded-full flex items-center justify-center mb-6">
                            <CheckCircle size={48} />
                        </div>
                        <h3 className="text-2xl font-bold text-gray-900 mb-2">Application Submitted!</h3>
                        <p className="text-gray-500 max-w-md mb-8">
                            The loan application for <span className="font-bold text-gray-800">{formData.borrowerType === 'INDIVIDUAL' ? customerDetails?.name : groupDetails?.name}</span> has been successfully queued for approval.
                        </p>
                        <div className="flex gap-4">
                            <button onClick={() => setView('LIST')} className="px-6 py-2 bg-gray-100 text-gray-700 font-bold rounded-lg hover:bg-gray-200">
                                Return to Portfolio
                            </button>
                            <button onClick={() => setView('WIZARD')} className="px-6 py-2 bg-blue-600 text-white font-bold rounded-lg hover:bg-blue-700">
                                Start New Application
                            </button>
                        </div>
                    </div>
                ) : (
                    <>
                        {/* Stepper */}
                        <div className="flex items-center justify-between mb-8 px-4 relative">
                            <div className="absolute left-0 top-1/2 transform -translate-y-1/2 w-full h-1 bg-gray-200 -z-10"></div>
                            {STEPS.map((step) => (
                                <div key={step.id} className="flex flex-col items-center bg-white px-2">
                                    <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-sm border-2 mb-1 transition-colors ${
                                        wizardStep >= step.id 
                                        ? 'bg-blue-600 text-white border-blue-600' 
                                        : 'bg-white text-gray-400 border-gray-300'
                                    }`}>
                                        {step.id < wizardStep ? <Check size={16} /> : step.id}
                                    </div>
                                    <span className={`text-xs font-medium ${wizardStep >= step.id ? 'text-blue-800' : 'text-gray-400'}`}>
                                        {step.label}
                                    </span>
                                </div>
                            ))}
                        </div>

                        <div className="bg-white border border-gray-200 rounded-lg p-8 shadow-sm min-h-[400px] flex flex-col relative">
                            {submissionStatus === 'SUBMITTING' && (
                                <div className="absolute inset-0 bg-white/80 z-50 flex flex-col items-center justify-center rounded-lg">
                                    <Loader2 className="animate-spin text-blue-600 mb-2" size={32} />
                                    <span className="text-blue-700 font-bold">Submitting Application...</span>
                                </div>
                            )}
                            
                            {/* STEP 1: BORROWER */}
                            {wizardStep === 1 && (
                                <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
                                    <h3 className="text-xl font-bold text-gray-900">Select Borrower</h3>
                                    
                                    <div className="flex gap-4 mb-4">
                                        <button 
                                            className={`flex-1 py-3 border rounded-lg flex items-center justify-center gap-2 ${formData.borrowerType === 'INDIVIDUAL' ? 'bg-blue-50 border-blue-500 text-blue-700 font-bold' : 'border-gray-200 text-gray-500'}`}
                                            onClick={() => setFormData({...formData, borrowerType: 'INDIVIDUAL', cif: ''})}
                                        >
                                            <User size={18} /> Individual Client
                                        </button>
                                        <button 
                                            className={`flex-1 py-3 border rounded-lg flex items-center justify-center gap-2 ${formData.borrowerType === 'GROUP' ? 'bg-purple-50 border-purple-500 text-purple-700 font-bold' : 'border-gray-200 text-gray-500'}`}
                                            onClick={() => setFormData({...formData, borrowerType: 'GROUP', cif: ''})}
                                        >
                                            <Users size={18} /> Joint Liability Group
                                        </button>
                                    </div>

                                    <div className="max-w-md">
                                        <label className="block text-sm font-medium text-gray-700 mb-1">{formData.borrowerType === 'INDIVIDUAL' ? 'Customer CIF' : 'Group ID'}</label>
                                        <div className={`flex items-center gap-2 border rounded p-2.5 ${validationError && !customerDetails && !groupDetails ? 'border-red-300 bg-red-50' : 'bg-gray-50 border-gray-300'}`}>
                                            <User size={18} className={validationError ? "text-red-400" : "text-gray-400"} />
                                            <input 
                                                type="text" 
                                                value={formData.cif} 
                                                onChange={e => setFormData({...formData, cif: e.target.value})}
                                                placeholder={formData.borrowerType === 'INDIVIDUAL' ? "e.g. CIF100001" : "e.g. GRP-001"}
                                                className="bg-transparent focus:outline-none w-full text-sm font-medium"
                                                autoFocus
                                            />
                                            {(customerDetails || groupDetails) && (
                                                <span className="text-sm font-bold text-green-600 px-2 bg-green-100 rounded flex items-center gap-1">
                                                    <BadgeCheck size={14}/> Found
                                                </span>
                                            )}
                                        </div>
                                        {customerDetails && (
                                            <div className="mt-4 p-4 bg-blue-50 border border-blue-100 rounded-lg">
                                                <p className="font-bold text-blue-900 text-lg">{customerDetails.name}</p>
                                                <p className="text-sm text-blue-700 mb-3">{customerDetails.email} • {customerDetails.phone}</p>
                                                <div className="flex gap-2">
                                                    <span className={`text-xs px-2 py-1 rounded font-bold border ${customerDetails.riskRating === 'High' ? 'bg-red-100 border-red-200 text-red-700' : 'bg-green-100 border-green-200 text-green-700'}`}>
                                                        Risk: {customerDetails.riskRating}
                                                    </span>
                                                    <span className="text-xs px-2 py-1 rounded font-bold bg-white border border-blue-200 text-blue-700">
                                                        KYC: {customerDetails.kycLevel}
                                                    </span>
                                                </div>
                                            </div>
                                        )}
                                        {groupDetails && (
                                            <div className="mt-4 p-4 bg-purple-50 border border-purple-100 rounded-lg">
                                                <div className="flex justify-between items-start">
                                                    <div>
                                                        <p className="font-bold text-purple-900 text-lg">{groupDetails.name}</p>
                                                        <p className="text-sm text-purple-700 mb-2">Meeting: {groupDetails.meetingDay}</p>
                                                    </div>
                                                    <span className="text-xs px-2 py-1 rounded font-bold bg-white border border-purple-200 text-purple-700">
                                                        {groupDetails.status}
                                                    </span>
                                                </div>
                                                <div className="mt-3 pt-3 border-t border-purple-200">
                                                    <p className="text-xs font-bold text-purple-800 uppercase mb-2">Active Members</p>
                                                    <div className="flex flex-wrap gap-2">
                                                        {groupDetails.members.slice(0,5).map(mId => {
                                                            const mem = customers.find(c => c.id === mId);
                                                            return (
                                                                <span key={mId} className="text-xs bg-white px-2 py-1 rounded border border-purple-100 text-gray-600">
                                                                    {mem?.name || mId}
                                                                </span>
                                                            );
                                                        })}
                                                        {groupDetails.members.length > 5 && (
                                                            <span className="text-xs text-purple-500 italic flex items-center">+{groupDetails.members.length - 5} more</span>
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                        )}
                                        {!customerDetails && !groupDetails && (
                                            <p className="text-xs text-gray-500 mt-2">Enter a valid ID to proceed.</p>
                                        )}
                                    </div>
                                </div>
                            )}

                            {/* STEP 2: TERMS */}
                            {wizardStep === 2 && (
                                <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
                                    <h3 className="text-xl font-bold text-gray-900">Loan Terms</h3>
                                    
                                    {formData.borrowerType === 'GROUP' && groupDetails && (
                                        <div className="bg-purple-50 border border-purple-200 rounded-lg p-4">
                                            <h4 className="font-bold text-purple-900 text-sm mb-3 flex items-center gap-2"><Users size={16}/> Group Member Allocation</h4>
                                            <div className="grid grid-cols-2 gap-3 mb-2">
                                                {groupDetails.members.map(memberId => {
                                                    const cust = customers.find(c => c.id === memberId);
                                                    return (
                                                        <div key={memberId} className="flex justify-between items-center bg-white p-2 rounded border border-purple-100">
                                                            <div className="text-xs">
                                                                <div className="font-bold text-gray-700">{cust?.name}</div>
                                                                <div className="text-gray-400">{memberId}</div>
                                                            </div>
                                                            <input 
                                                                type="number" 
                                                                placeholder="0.00"
                                                                className="w-24 border rounded p-1 text-right text-sm font-mono focus:ring-1 focus:ring-purple-500 outline-none"
                                                                value={memberSplits[memberId] || ''}
                                                                onChange={e => handleSplitChange(memberId, e.target.value)}
                                                            />
                                                        </div>
                                                    )
                                                })}
                                            </div>
                                            <div className="text-right text-xs font-bold text-purple-700 mt-2">
                                                Total Allocated: {Object.values(memberSplits).reduce((a,b)=>a+b, 0).toLocaleString()} GHS
                                            </div>
                                        </div>
                                    )}

                                    <div className="grid grid-cols-2 gap-6">
                                        <div className="col-span-2">
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Loan Product</label>
                                            <select 
                                                value={formData.productCode}
                                                onChange={e => handleProductChange(e.target.value)}
                                                className="w-full border border-gray-300 rounded p-3 text-sm bg-white focus:ring-2 focus:ring-blue-500 outline-none"
                                            >
                                                {loanProducts.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                                            </select>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Principal (GHS)</label>
                                            <input 
                                                type="number" 
                                                value={formData.principal}
                                                onChange={e => setFormData({...formData, principal: Number(e.target.value)})}
                                                className={`w-full border border-gray-300 rounded p-3 text-lg font-mono font-bold focus:ring-2 focus:ring-blue-500 outline-none ${formData.borrowerType === 'GROUP' ? 'bg-gray-100 text-gray-500 cursor-not-allowed' : ''}`}
                                                readOnly={formData.borrowerType === 'GROUP'} // Auto-calculated for groups
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Interest Rate (%)</label>
                                            <input 
                                                type="number" 
                                                value={formData.rate}
                                                onChange={e => setFormData({...formData, rate: Number(e.target.value)})}
                                                className="w-full border border-gray-300 rounded p-3 bg-gray-50 text-gray-600 focus:outline-none"
                                                readOnly // Rate usually fixed by product
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Term (Months)</label>
                                            <input 
                                                type="number" 
                                                value={formData.term}
                                                onChange={e => setFormData({...formData, term: Number(e.target.value)})}
                                                className="w-full border border-gray-300 rounded p-3 text-lg font-mono focus:ring-2 focus:ring-blue-500 outline-none"
                                            />
                                        </div>
                                    </div>

                                    {/* Live Amortization Preview */}
                                    {schedule.length > 0 && (
                                        <div className="bg-blue-50 border border-blue-100 rounded-lg p-4 mt-6">
                                            <h4 className="font-bold text-blue-900 mb-3 flex items-center gap-2">
                                                <Calculator size={16}/> Payment Preview
                                            </h4>
                                            <div className="grid grid-cols-3 gap-4 mb-4">
                                                <div className="bg-white p-3 rounded border border-blue-100 shadow-sm">
                                                    <span className="text-xs text-gray-500 uppercase font-bold">Monthly Installment</span>
                                                    <span className="block text-lg font-mono font-bold text-blue-700">
                                                        {schedule[0]?.total.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}
                                                    </span>
                                                </div>
                                                <div className="bg-white p-3 rounded border border-blue-100 shadow-sm">
                                                    <span className="text-xs text-gray-500 uppercase font-bold">Total Interest</span>
                                                    <span className="block text-lg font-mono font-bold text-blue-700">
                                                        {schedule.reduce((acc, row) => acc + row.interest, 0).toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}
                                                    </span>
                                                </div>
                                                <div className="bg-white p-3 rounded border border-blue-100 shadow-sm">
                                                    <span className="text-xs text-gray-500 uppercase font-bold">Total Repayment</span>
                                                    <span className="block text-lg font-mono font-bold text-blue-700">
                                                        {schedule.reduce((acc, row) => acc + row.total, 0).toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}
                                                    </span>
                                                </div>
                                            </div>
                                            
                                            <details className="text-sm">
                                                <summary className="cursor-pointer text-blue-600 font-medium hover:text-blue-800">View Full Schedule</summary>
                                                <div className="mt-3 max-h-48 overflow-y-auto bg-white rounded border border-gray-200">
                                                    <table className="w-full text-xs text-right">
                                                        <thead className="bg-gray-50 text-gray-500 sticky top-0">
                                                            <tr>
                                                                <th className="p-2 text-center">#</th>
                                                                <th className="p-2">Principal</th>
                                                                <th className="p-2">Interest</th>
                                                                <th className="p-2">Total</th>
                                                                <th className="p-2">Balance</th>
                                                            </tr>
                                                        </thead>
                                                        <tbody>
                                                            {schedule.map(row => (
                                                                <tr key={row.period} className="border-t border-gray-50">
                                                                    <td className="p-2 text-center text-gray-400">{row.period}</td>
                                                                    <td className="p-2">{row.principal.toFixed(2)}</td>
                                                                    <td className="p-2">{row.interest.toFixed(2)}</td>
                                                                    <td className="p-2 font-bold">{row.total.toFixed(2)}</td>
                                                                    <td className="p-2 text-gray-500">{row.balance.toFixed(2)}</td>
                                                                </tr>
                                                            ))}
                                                        </tbody>
                                                    </table>
                                                </div>
                                            </details>
                                        </div>
                                    )}
                                </div>
                            )}

                            {/* STEP 3: COLLATERAL & DOCS */}
                            {wizardStep === 3 && (
                                <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
                                    <h3 className="text-xl font-bold text-gray-900">Collateral & Documents</h3>
                                    
                                    <div className="grid grid-cols-2 gap-6">
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Collateral Type</label>
                                            <select 
                                                value={formData.collateralType}
                                                onChange={e => setFormData({...formData, collateralType: e.target.value})}
                                                className="w-full border border-gray-300 rounded p-2.5 text-sm bg-white"
                                            >
                                                <option value="Unsecured">Unsecured</option>
                                                <option value="Cash Lien">Cash Lien</option>
                                                <option value="Landed Property">Landed Property</option>
                                                <option value="Vehicle">Vehicle</option>
                                                <option value="Guarantor">Guarantor</option>
                                                {formData.borrowerType === 'GROUP' && <option value="Group Guarantee">Group Guarantee</option>}
                                                {formData.borrowerType === 'GROUP' && <option value="Group Savings Lien">Group Savings Lien</option>}
                                            </select>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">Estimated Value (GHS)</label>
                                            <input 
                                                type="text" 
                                                placeholder="Optional"
                                                value={formData.collateralValue}
                                                onChange={e => setFormData({...formData, collateralValue: e.target.value})}
                                                className="w-full border border-gray-300 rounded p-2.5 text-sm"
                                            />
                                        </div>
                                    </div>

                                    <div className="border-t pt-4">
                                        <div className="flex justify-between items-end mb-3">
                                            <h4 className="text-sm font-bold text-gray-700">Required Documents</h4>
                                            <span className="text-xs text-gray-500">{uploadedDocs.length} / {allPossibleDocs.length} Uploaded</span>
                                        </div>
                                        <div className="space-y-3">
                                            {allPossibleDocs.map((doc) => {
                                                const isRequired = requiredDocs.includes(doc);
                                                const isUploaded = uploadedDocs.includes(doc);
                                                return (
                                                    <div key={doc} className={`flex items-center justify-between p-3 border rounded-lg transition-colors ${
                                                        isUploaded ? 'bg-green-50 border-green-200' : 
                                                        isRequired ? 'bg-white border-orange-200 hover:bg-orange-50' : 
                                                        'bg-white border-gray-200 hover:bg-gray-50'
                                                    }`}>
                                                        <div className="flex items-center gap-3">
                                                            <div className={`p-2 rounded-full ${isUploaded ? 'bg-green-100 text-green-600' : isRequired ? 'bg-orange-100 text-orange-500' : 'bg-gray-100 text-gray-400'}`}>
                                                                <File size={18} />
                                                            </div>
                                                            <div>
                                                                <span className={`text-sm block ${isUploaded ? 'text-gray-900 font-bold' : 'text-gray-700'}`}>{doc}</span>
                                                                {isRequired && !isUploaded && <span className="text-[10px] text-orange-600 font-bold uppercase tracking-wide">Required</span>}
                                                                {!isRequired && !isUploaded && <span className="text-[10px] text-gray-400 uppercase tracking-wide">Optional</span>}
                                                            </div>
                                                        </div>
                                                        {isUploaded ? (
                                                            <span className="flex items-center gap-1 text-xs font-bold text-green-600"><Check size={14}/> Uploaded</span>
                                                        ) : (
                                                            <button 
                                                                onClick={() => handleSimulateUpload(doc)}
                                                                className="text-xs flex items-center gap-1 text-blue-600 hover:text-blue-800 font-medium px-3 py-1.5 border border-blue-200 rounded hover:bg-blue-50"
                                                            >
                                                                <Upload size={14} /> Upload
                                                            </button>
                                                        )}
                                                    </div>
                                                );
                                            })}
                                        </div>
                                    </div>
                                </div>
                            )}

                            {/* STEP 4: REVIEW */}
                            {wizardStep === 4 && (
                                <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
                                    <h3 className="text-xl font-bold text-gray-900">Review Application</h3>
                                    
                                    <div className="grid grid-cols-2 gap-4 text-sm">
                                        <div className="bg-gray-50 p-4 rounded-lg">
                                            <span className="block text-gray-500 text-xs uppercase mb-1">Borrower</span>
                                            <span className="font-bold text-gray-900 block">{formData.borrowerType === 'INDIVIDUAL' ? customerDetails?.name : groupDetails?.name}</span>
                                            <span className="text-gray-500">{formData.cif}</span>
                                        </div>
                                        <div className="bg-gray-50 p-4 rounded-lg">
                                            <span className="block text-gray-500 text-xs uppercase mb-1">Product</span>
                                            <span className="font-bold text-gray-900 block">{loanProducts.find(p => p.id === formData.productCode)?.name}</span>
                                            <span className="text-gray-500">{formData.rate}% Interest</span>
                                        </div>
                                        <div className="bg-blue-50 p-4 rounded-lg border border-blue-100">
                                            <span className="block text-blue-500 text-xs uppercase mb-1">Principal</span>
                                            <span className="font-bold text-blue-900 block text-lg">{formData.principal.toLocaleString()} GHS</span>
                                        </div>
                                        <div className="bg-blue-50 p-4 rounded-lg border border-blue-100">
                                            <span className="block text-blue-500 text-xs uppercase mb-1">Total Payment</span>
                                            <span className="font-bold text-blue-900 block text-lg">
                                                {schedule.reduce((acc, curr) => acc + curr.total, 0).toLocaleString(undefined, {maximumFractionDigits: 2})} GHS
                                            </span>
                                        </div>
                                    </div>

                                    {/* Mini Schedule Preview */}
                                    <div className="border rounded-lg overflow-hidden">
                                        <div className="bg-gray-100 px-4 py-2 text-xs font-bold text-gray-600 uppercase">Amortization Preview</div>
                                        <div className="max-h-40 overflow-y-auto">
                                            <table className="w-full text-xs text-right">
                                                <thead className="bg-gray-50 text-gray-500">
                                                    <tr>
                                                        <th className="p-2 text-left">Period</th>
                                                        <th className="p-2">Principal</th>
                                                        <th className="p-2">Interest</th>
                                                        <th className="p-2">Total</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    {schedule.map(row => (
                                                        <tr key={row.period} className="border-t border-gray-100">
                                                            <td className="p-2 text-left">{row.period}</td>
                                                            <td className="p-2">{row.principal.toFixed(2)}</td>
                                                            <td className="p-2">{row.interest.toFixed(2)}</td>
                                                            <td className="p-2 font-bold">{row.total.toFixed(2)}</td>
                                                        </tr>
                                                    ))}
                                                </tbody>
                                            </table>
                                        </div>
                                    </div>
                                    
                                    <div className="bg-yellow-50 p-3 rounded text-xs text-yellow-800 border border-yellow-200 flex gap-2">
                                        <ShieldAlert size={16} className="shrink-0" />
                                        <span>
                                            By submitting this application, you confirm that all KYC documents have been verified and the borrower meets the eligibility criteria for the selected product.
                                        </span>
                                    </div>
                                </div>
                            )}

                            {/* Footer Actions */}
                            <div className="mt-auto pt-6 border-t border-gray-100 flex justify-between items-center">
                                {validationError && (
                                    <div className="text-red-600 text-sm flex items-center gap-2 font-medium">
                                        <AlertCircle size={16} /> {validationError}
                                    </div>
                                )}
                                {!validationError && <div></div>} {/* Spacer */}
                                
                                <div className="flex gap-3">
                                    <button 
                                        onClick={wizardStep === 1 ? () => setView('LIST') : handleBackStep}
                                        className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm font-medium flex items-center gap-1"
                                    >
                                        {wizardStep === 1 ? 'Cancel' : <><ChevronLeft size={16}/> Back</>}
                                    </button>
                                    
                                    {wizardStep < 4 ? (
                                        <button 
                                            onClick={handleNextStep}
                                            className="px-6 py-2 bg-blue-600 text-white rounded-lg text-sm font-bold hover:bg-blue-700 flex items-center gap-2"
                                        >
                                            Next Step <ChevronRight size={16} />
                                        </button>
                                    ) : (
                                        <button 
                                            onClick={saveLoan}
                                            className="px-6 py-2 bg-green-600 text-white rounded-lg text-sm font-bold hover:bg-green-700 flex items-center gap-2 shadow-sm"
                                        >
                                            <CheckCircle size={16} /> Submit Application
                                        </button>
                                    )}
                                </div>
                            </div>

                        </div>
                    </>
                )}
            </div>
        )}

        {/* VIEW: DETAILS (EXISTING LOAN) */}
        {view === 'DETAILS' && selectedLoan && (
            <div className="space-y-6">
                <div className="flex justify-between items-start bg-blue-50 p-6 rounded-lg border border-blue-100">
                    <div>
                        <div className="flex items-center gap-2 mb-2">
                            <span className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs font-bold rounded">{selectedLoan.id}</span>
                            <span className={`px-2 py-0.5 rounded text-xs font-bold border ${
                                selectedLoan.status === 'ACTIVE' ? 'bg-green-100 text-green-700 border-green-200' : 'bg-gray-100 text-gray-700 border-gray-200'
                            }`}>{selectedLoan.status}</span>
                        </div>
                        <h3 className="text-xl font-bold text-blue-900 mb-1">{selectedLoan.productName}</h3>
                        <div className="flex gap-4 text-sm text-blue-800">
                            {selectedLoan.groupId ? 
                                <span className="flex items-center gap-1"><Users size={14}/> Group: {selectedLoan.groupId}</span> :
                                <span className="flex items-center gap-1"><User size={14}/> Client: {selectedLoan.cif}</span>
                            }
                            <span className="flex items-center gap-1"><Calendar size={14}/> {selectedLoan.termMonths} Months</span>
                            <span className="flex items-center gap-1"><FileText size={14}/> {selectedLoan.rate}% p.a.</span>
                        </div>
                    </div>
                    <div className="text-right">
                         <span className="block text-sm text-blue-600 uppercase font-bold">Outstanding Balance</span>
                         <span className="text-3xl font-mono text-blue-900 font-bold">
                             {selectedLoan.outstandingBalance.toLocaleString()}
                        </span>
                        <span className="text-sm text-blue-600 ml-1">GHS</span>
                    </div>
                </div>

                {/* Group Repayment Section - Only for Group Loans */}
                {selectedLoan.groupId && groups.find(g => g.id === selectedLoan.groupId) && (
                    <div className="bg-purple-50 border border-purple-200 rounded-lg p-6">
                        <div className="flex justify-between items-center mb-4">
                            <h4 className="font-bold text-purple-900 flex items-center gap-2">
                                <Users size={18} /> Group Repayment Console
                            </h4>
                            <div className="text-sm font-bold text-purple-700">
                                Total Collection: GHS {totalContribution.toLocaleString()}
                            </div>
                        </div>
                        
                        {/* Member Breakdown */}
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 mb-4">
                            {groups.find(g => g.id === selectedLoan.groupId)?.members.map(mId => {
                                const mem = customers.find(c => c.id === mId);
                                // Determine expected amount based on split ratio for the current installment
                                const split = selectedLoan.memberSplits?.[mId] || 0;
                                const ratio = selectedLoan.principal > 0 ? split / selectedLoan.principal : 0;
                                
                                // Find current due schedule
                                const currentSchedule = schedule.find(s => s.status === 'DUE') || schedule[0];
                                const expectedInstallment = currentSchedule ? currentSchedule.total * ratio : 0;
                                const currentPaid = memberContributions[mId] || 0;
                                const isFulfilled = currentPaid >= expectedInstallment;

                                return (
                                    <div key={mId} className={`bg-white p-3 rounded border flex flex-col gap-2 ${isFulfilled ? 'border-green-300 ring-1 ring-green-100' : 'border-purple-100'}`}>
                                        <div className="flex justify-between items-start">
                                            <div>
                                                <div className="font-bold text-gray-800 text-sm truncate">{mem?.name || mId}</div>
                                                <div className="text-[10px] text-gray-500">Loan Share: {split.toLocaleString()}</div>
                                            </div>
                                            {isFulfilled && <CheckCircle size={16} className="text-green-500" />}
                                        </div>
                                        
                                        <div className="flex justify-between items-center mt-1">
                                            <span className="text-[10px] text-gray-500">Due: <span className="font-bold">{expectedInstallment.toFixed(2)}</span></span>
                                            <input 
                                                type="number" 
                                                placeholder="0.00"
                                                className="w-24 border border-gray-300 rounded p-1 text-right text-sm font-mono focus:ring-2 focus:ring-purple-500 outline-none"
                                                value={memberContributions[mId] || ''}
                                                onChange={(e) => handleContributionChange(mId, e.target.value)}
                                            />
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                        
                        <div className="flex justify-end gap-3">
                            <button 
                                onClick={submitGroupRepayment}
                                disabled={totalContribution <= 0}
                                className="px-4 py-2 bg-purple-600 text-white rounded font-bold hover:bg-purple-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                            >
                                <Save size={16}/> Post Repayment
                            </button>
                        </div>
                    </div>
                )}

                {/* Amortization Schedule */}
                <div className="bg-white border border-gray-200 rounded-lg overflow-hidden shadow-sm">
                    <div className="p-4 bg-gray-50 border-b border-gray-200 flex justify-between items-center">
                        <h4 className="font-bold text-gray-700">Repayment Schedule</h4>
                    </div>
                    <div className="max-h-96 overflow-y-auto">
                        <table className="w-full text-sm text-right">
                            <thead className="bg-gray-50 text-gray-500 font-medium text-xs sticky top-0 shadow-sm">
                                <tr>
                                    <th className="p-3 text-center">#</th>
                                    <th className="p-3 text-center">Due Date</th>
                                    <th className="p-3">Principal</th>
                                    <th className="p-3">Interest</th>
                                    <th className="p-3">Total Installment</th>
                                    <th className="p-3">Balance</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-gray-100 font-mono">
                                {schedule.map((row) => (
                                    <tr key={row.period} className="hover:bg-gray-50">
                                        <td className="p-3 text-center text-gray-500">{row.period}</td>
                                        <td className="p-3 text-center text-gray-600">{row.dueDate}</td>
                                        <td className="p-3">{row.principal.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</td>
                                        <td className="p-3 text-red-600">{row.interest.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</td>
                                        <td className="p-3 font-bold">{row.total.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</td>
                                        <td className="p-3 text-gray-400">{row.balance.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>

                {selectedLoan.status === 'PENDING' && (
                     <div className="flex justify-end gap-3 pt-4 border-t">
                        <button 
                            onClick={() => { onDisburse(selectedLoan.id); setView('LIST'); }} 
                            className="px-6 py-2 bg-green-600 text-white rounded font-medium hover:bg-green-700 flex items-center gap-2"
                        >
                            <CheckCircle size={16} /> Approve & Disburse Funds
                        </button>
                    </div>
                )}
            </div>
        )}
      </div>
    </div>
  );
};

export default LoanEngine;
```

### components/AccountingEngine.tsx
```tsx
import React, { useState, useMemo } from 'react';
import { GLAccount, JournalEntry } from '../types';
import { FileText, Plus, BookOpen, Search, Filter, Check, List, Layers, ChevronRight, ChevronDown, Folder, File, Save, X } from 'lucide-react';

interface AccountingEngineProps {
  accounts: GLAccount[];
  journalEntries: JournalEntry[];
  onPostJournal: (description: string, lines: { code: string; debit: number; credit: number }[]) => void;
  onCreateAccount: (account: GLAccount) => void;
}

const AccountingEngine: React.FC<AccountingEngineProps> = ({ accounts, journalEntries: jvs, onPostJournal, onCreateAccount }) => {
  const [view, setView] = useState<'COA' | 'JV' | 'LEDGER'>('COA');
  const [jvLines, setJvLines] = useState([{ accountCode: '', debit: 0, credit: 0 }, { accountCode: '', debit: 0, credit: 0 }]);
  const [jvDescription, setJvDescription] = useState('');
  const [showAddGLModal, setShowAddGLModal] = useState(false);
  const [newGL, setNewGL] = useState<Partial<GLAccount>>({ category: 'ASSET', currency: 'GHS', balance: 0, isHeader: false });
  
  // COA Tree State
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set(['10000', '20000', '30000', '40000', '50000']));

  const toggleNode = (code: string) => {
      const next = new Set(expandedNodes);
      if (next.has(code)) next.delete(code);
      else next.add(code);
      setExpandedNodes(next);
  };

  const postJournal = () => {
      const totalDebit = jvLines.reduce((sum, line) => sum + Number(line.debit), 0);
      const totalCredit = jvLines.reduce((sum, line) => sum + Number(line.credit), 0);

      if (totalDebit !== totalCredit) {
          alert(`Unbalanced Journal! Difference: ${totalDebit - totalCredit}`);
          return;
      }
      
      onPostJournal(
          jvDescription,
          jvLines.map(l => ({ code: l.accountCode, debit: l.debit, credit: l.credit }))
      );

      setJvLines([{ accountCode: '', debit: 0, credit: 0 }, { accountCode: '', debit: 0, credit: 0 }]);
      setJvDescription('');
      alert("Journal Posted Successfully");
  };

  const handleCreateGL = () => {
      if (!newGL.code || !newGL.name) return;
      try {
          onCreateAccount(newGL as GLAccount);
          setShowAddGLModal(false);
          setNewGL({ category: 'ASSET', currency: 'GHS', balance: 0, isHeader: false });
      } catch (e: any) {
          alert(e.message);
      }
  };

  const addLine = () => setJvLines([...jvLines, { accountCode: '', debit: 0, credit: 0 }]);

  const updateLine = (index: number, field: string, value: any) => {
      const newLines = [...jvLines];
      // @ts-ignore
      newLines[index][field] = value;
      setJvLines(newLines);
  };

  // Group Accounts for Tree View
  const treeData = useMemo(() => {
      // Simple parent detection: Ends in '0000' is level 1, '00' is level 2, else level 3
      const roots = accounts.filter(a => a.isHeader);
      return roots.map(root => ({
          ...root,
          children: accounts.filter(a => !a.isHeader && a.code.startsWith(root.code.charAt(0)))
      }));
  }, [accounts]);

  return (
    <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden relative">
        {/* Navigation Tabs */}
        <div className="flex border-b border-gray-200 bg-gray-50">
            <button 
                onClick={() => setView('COA')}
                className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'COA' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}
            >
                <Layers size={16} /> Chart of Accounts
            </button>
            <button 
                onClick={() => setView('JV')}
                className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'JV' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}
            >
                <Plus size={16} /> Post Journal
            </button>
            <button 
                onClick={() => setView('LEDGER')}
                className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'LEDGER' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}
            >
                <BookOpen size={16} /> General Ledger
            </button>
        </div>

        {/* Content Area */}
        <div className="flex-1 overflow-y-auto p-6 bg-gray-50/50">
            
            {/* VIEW: CHART OF ACCOUNTS (TREE) */}
            {view === 'COA' && (
                <div className="bg-white border border-gray-200 rounded-lg shadow-sm">
                    <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
                        <h3 className="font-bold text-gray-800">General Ledger Structure</h3>
                        <div className="flex gap-2">
                            <div className="relative">
                                <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                                <input type="text" placeholder="Search GL..." className="pl-9 pr-4 py-1.5 text-sm border rounded-lg focus:outline-none focus:border-blue-500" />
                            </div>
                            <button 
                                onClick={() => setShowAddGLModal(true)}
                                className="bg-blue-600 text-white px-3 py-1.5 rounded-lg text-xs font-bold hover:bg-blue-700 flex items-center gap-1"
                            >
                                <Plus size={14} /> New Account
                            </button>
                        </div>
                    </div>
                    <div className="p-4">
                        {treeData.map(root => (
                            <div key={root.code} className="mb-4">
                                <div 
                                    className="flex items-center gap-2 p-2 hover:bg-gray-50 rounded cursor-pointer select-none"
                                    onClick={() => toggleNode(root.code)}
                                >
                                    <span className="text-gray-400">
                                        {expandedNodes.has(root.code) ? <ChevronDown size={16}/> : <ChevronRight size={16}/>}
                                    </span>
                                    <Folder size={18} className="text-blue-500 fill-blue-100" />
                                    <span className="font-bold text-gray-800">{root.code} - {root.name}</span>
                                    <span className="ml-auto text-xs px-2 py-0.5 bg-gray-100 text-gray-600 rounded uppercase">{root.category}</span>
                                </div>
                                
                                {expandedNodes.has(root.code) && (
                                    <div className="ml-8 border-l-2 border-gray-100 pl-2 mt-1 space-y-1">
                                        {root.children?.map(child => (
                                            <div key={child.code} className="flex items-center justify-between p-2 hover:bg-blue-50 rounded group">
                                                <div className="flex items-center gap-3">
                                                    <File size={16} className="text-gray-400 group-hover:text-blue-500" />
                                                    <div>
                                                        <div className="text-sm font-medium text-gray-700 group-hover:text-blue-700">{child.code} - {child.name}</div>
                                                    </div>
                                                </div>
                                                <div className="font-mono text-sm text-gray-800 font-medium">
                                                    {child.balance.toLocaleString('en-US', { minimumFractionDigits: 2 })} GHS
                                                </div>
                                            </div>
                                        ))}
                                        {root.children?.length === 0 && <div className="text-xs text-gray-400 italic p-2">No sub-accounts</div>}
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* VIEW: POST JOURNAL */}
            {view === 'JV' && (
                <div className="max-w-4xl mx-auto bg-white border border-gray-200 rounded-lg shadow-sm p-6">
                    <h3 className="text-lg font-bold text-gray-800 mb-6 flex items-center gap-2">
                        <FileText className="text-blue-600" /> New Journal Entry
                    </h3>
                    
                    <div className="mb-6 grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                            <input 
                                type="text" 
                                value={jvDescription}
                                onChange={e => setJvDescription(e.target.value)}
                                className="w-full border rounded p-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none" 
                                placeholder="e.g. Monthly accruals"
                            />
                        </div>
                         <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Date</label>
                            <input type="date" className="w-full border rounded p-2 text-sm bg-gray-50" defaultValue={new Date().toISOString().split('T')[0]} />
                        </div>
                    </div>

                    <div className="border rounded-lg overflow-hidden mb-6">
                        <table className="w-full text-sm">
                            <thead className="bg-gray-50 text-gray-600 text-xs uppercase">
                                <tr>
                                    <th className="p-3 text-left w-1/2">Account</th>
                                    <th className="p-3 text-right">Debit</th>
                                    <th className="p-3 text-right">Credit</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-gray-100">
                                {jvLines.map((line, idx) => (
                                    <tr key={idx}>
                                        <td className="p-2">
                                            <select 
                                                className="w-full p-2 border rounded bg-white"
                                                value={line.accountCode}
                                                onChange={e => updateLine(idx, 'accountCode', e.target.value)}
                                            >
                                                <option value="">Select Account...</option>
                                                {accounts.filter(a => !a.isHeader).map(acc => (
                                                    <option key={acc.code} value={acc.code}>{acc.code} - {acc.name}</option>
                                                ))}
                                            </select>
                                        </td>
                                        <td className="p-2">
                                            <input 
                                                type="number" 
                                                value={line.debit}
                                                onChange={e => updateLine(idx, 'debit', Number(e.target.value))}
                                                className="w-full p-2 border rounded text-right font-mono focus:ring-blue-500 outline-none"
                                            />
                                        </td>
                                        <td className="p-2">
                                            <input 
                                                type="number" 
                                                value={line.credit}
                                                onChange={e => updateLine(idx, 'credit', Number(e.target.value))}
                                                className="w-full p-2 border rounded text-right font-mono focus:ring-blue-500 outline-none"
                                            />
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                            <tfoot className="bg-gray-50 font-bold text-gray-700">
                                <tr>
                                    <td className="p-3 text-right">Totals:</td>
                                    <td className="p-3 text-right font-mono">
                                        {jvLines.reduce((s, l) => s + l.debit, 0).toLocaleString(undefined, {minimumFractionDigits: 2})}
                                    </td>
                                    <td className="p-3 text-right font-mono">
                                        {jvLines.reduce((s, l) => s + l.credit, 0).toLocaleString(undefined, {minimumFractionDigits: 2})}
                                    </td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>

                    <div className="flex justify-between">
                        <button onClick={addLine} className="text-blue-600 hover:bg-blue-50 px-4 py-2 rounded text-sm font-medium flex items-center gap-1">
                            <Plus size={16} /> Add Line
                        </button>
                        <div className="flex gap-3">
                            <button className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded text-sm">Cancel</button>
                            <button onClick={postJournal} className="px-6 py-2 bg-blue-600 text-white rounded font-medium hover:bg-blue-700 text-sm shadow-sm flex items-center gap-2">
                                <Check size={16} /> Post Entry
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* VIEW: LEDGER */}
            {view === 'LEDGER' && (
                <div className="bg-white border border-gray-200 rounded-lg shadow-sm">
                     <div className="p-4 border-b border-gray-200 flex justify-between items-center">
                        <h3 className="font-bold text-gray-800">Transaction Journal</h3>
                        <div className="flex gap-2">
                            <button className="p-2 border rounded hover:bg-gray-50"><Filter size={16} className="text-gray-500"/></button>
                            <button className="p-2 border rounded hover:bg-gray-50"><List size={16} className="text-gray-500"/></button>
                        </div>
                    </div>
                    <table className="w-full text-sm text-left">
                        <thead className="bg-gray-50 text-gray-600 font-medium">
                            <tr>
                                <th className="p-3">Date</th>
                                <th className="p-3">Ref</th>
                                <th className="p-3">Description</th>
                                <th className="p-3">Account</th>
                                <th className="p-3 text-right">Debit</th>
                                <th className="p-3 text-right">Credit</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100">
                            {jvs.flatMap(jv => jv.lines.map((line, idx) => (
                                <tr key={`${jv.id}-${idx}`} className="hover:bg-gray-50">
                                    <td className="p-3 text-gray-500">{jv.date}</td>
                                    <td className="p-3 font-mono text-blue-600 text-xs">{jv.id}</td>
                                    <td className="p-3 text-gray-800">{jv.description}</td>
                                    <td className="p-3">
                                        <span className="bg-gray-100 text-gray-600 px-2 py-0.5 rounded text-xs font-mono">{line.accountCode}</span>
                                    </td>
                                    <td className="p-3 text-right font-mono text-gray-600">{line.debit > 0 ? line.debit.toLocaleString() : '-'}</td>
                                    <td className="p-3 text-right font-mono text-gray-600">{line.credit > 0 ? line.credit.toLocaleString() : '-'}</td>
                                </tr>
                            )))}
                        </tbody>
                    </table>
                </div>
            )}

            {/* ADD GL MODAL */}
            {showAddGLModal && (
                <div className="absolute inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
                    <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6">
                        <div className="flex justify-between items-center mb-4">
                            <h3 className="font-bold text-lg text-gray-800">Add GL Account</h3>
                            <button onClick={() => setShowAddGLModal(false)}><X size={20} className="text-gray-400 hover:text-gray-600"/></button>
                        </div>
                        <div className="space-y-4">
                            <div>
                                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Account Code</label>
                                <input 
                                    type="text" 
                                    className="w-full border p-2 rounded text-sm font-mono" 
                                    value={newGL.code || ''} 
                                    onChange={e => setNewGL({...newGL, code: e.target.value})}
                                    placeholder="e.g. 60100"
                                />
                            </div>
                            <div>
                                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Account Name</label>
                                <input 
                                    type="text" 
                                    className="w-full border p-2 rounded text-sm" 
                                    value={newGL.name || ''} 
                                    onChange={e => setNewGL({...newGL, name: e.target.value})}
                                    placeholder="e.g. Office Supplies"
                                />
                            </div>
                            <div>
                                <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Category</label>
                                <select 
                                    className="w-full border p-2 rounded text-sm bg-white"
                                    value={newGL.category}
                                    onChange={e => setNewGL({...newGL, category: e.target.value as any})}
                                >
                                    <option value="ASSET">ASSET</option>
                                    <option value="LIABILITY">LIABILITY</option>
                                    <option value="EQUITY">EQUITY</option>
                                    <option value="INCOME">INCOME</option>
                                    <option value="EXPENSE">EXPENSE</option>
                                </select>
                            </div>
                            <div className="flex items-center gap-2 pt-2">
                                <input 
                                    type="checkbox" 
                                    id="isHeader" 
                                    checked={newGL.isHeader} 
                                    onChange={e => setNewGL({...newGL, isHeader: e.target.checked})}
                                />
                                <label htmlFor="isHeader" className="text-sm text-gray-700">Is Header Account?</label>
                            </div>
                            <button 
                                onClick={handleCreateGL}
                                className="w-full bg-blue-600 text-white font-bold py-2 rounded mt-4 hover:bg-blue-700"
                            >
                                Create Account
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    </div>
  );
};

export default AccountingEngine;
```

### hooks/useBankingSystem.ts
```ts
import { useState, useEffect } from 'react';
import { Customer, Account, Loan, GLAccount, JournalEntry, AuditLog, Transaction, StaffUser, Role, Permission, StoredProcedureResult, DevTask, Group, Product, ApprovalRequest, Branch, SystemConfig, Workflow } from '../types';
import { MOCK_DATA } from '../data/mockData';

const GRA_E_LEVY_RATE = 0.01; // 1%
const GRA_GL_ACCOUNT = '20400'; // Statutory Payables: GRA

// Rate Limiting Config (Transactions per minute)
const RATE_LIMIT_THRESHOLD = 5;
const RATE_LIMIT_WINDOW = 60000; // 1 minute

export const useBankingSystem = () => {
  // --- AUTH STATE ---
  const [currentUser, setCurrentUser] = useState<StaffUser | null>(null);
  const [authError, setAuthError] = useState<string | null>(null);

  // --- DATABASE TABLES (Simulated Linear Hash Files) ---
  const [businessDate, setBusinessDate] = useState(new Date().toISOString().split('T')[0]);
  const [branches, setBranches] = useState<Branch[]>(MOCK_DATA.branches);
  const [customers, setCustomers] = useState<Customer[]>(MOCK_DATA.customers);
  const [groups, setGroups] = useState<Group[]>(MOCK_DATA.groups);
  const [products, setProducts] = useState<Product[]>(MOCK_DATA.products);
  const [accounts, setAccounts] = useState<Account[]>(MOCK_DATA.accounts);
  const [loans, setLoans] = useState<Loan[]>(MOCK_DATA.loans);
  const [glAccounts, setGlAccounts] = useState<GLAccount[]>(MOCK_DATA.glAccounts);
  const [journalEntries, setJournalEntries] = useState<JournalEntry[]>(MOCK_DATA.journalEntries);
  const [auditLogs, setAuditLogs] = useState<AuditLog[]>(MOCK_DATA.auditLogs);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [devTasks, setDevTasks] = useState<DevTask[]>(MOCK_DATA.devTasks);
  const [approvalRequests, setApprovalRequests] = useState<ApprovalRequest[]>(MOCK_DATA.approvalRequests);
  const [workflows, setWorkflows] = useState<Workflow[]>(MOCK_DATA.workflows);
  
  // RBAC Tables
  const [roles, setRoles] = useState<Role[]>(MOCK_DATA.roles);
  const [staff, setStaff] = useState<StaffUser[]>(MOCK_DATA.staff);

  // System Configuration (Limits)
  const [systemConfig, setSystemConfig] = useState<SystemConfig>({
      kycLimits: {
          'Tier 1': { maxBalance: 5000, dailyLimit: 2000 },
          'Tier 2': { maxBalance: 30000, dailyLimit: 15000 },
          'Tier 3': { maxBalance: 1000000000, dailyLimit: 1000000000 } // Effectively unlimited
      },
      amlThreshold: 20000
  });

  // EOD Temp State
  const [pendingAccrual, setPendingAccrual] = useState(0);

  // Rate Limit State
  const [actionTimestamps, setActionTimestamps] = useState<number[]>([]);

  // --- AUTHENTICATION & USER MANAGEMENT ---
  const login = (id: string, pass: string) => {
      const user = staff.find(u => (u.id === id || u.email === id) && u.password === pass);
      if (user) {
          if (user.status !== 'Active') {
              setAuthError("Account is inactive.");
              return;
          }
          setCurrentUser(user);
          setAuthError(null);
          addAuditLog(user.id, 'USER_LOGIN', 'SECURITY', 'User logged in successfully');
      } else {
          setAuthError("Invalid credentials.");
      }
  };

  const logout = () => {
      if(currentUser) addAuditLog(currentUser.id, 'USER_LOGOUT', 'SECURITY', 'User logged out');
      setCurrentUser(null);
  };

  const resetUserPassword = (userId: string) => {
      if (!hasPermission('USER_WRITE')) throw new Error("Permission Denied");
      setStaff(prev => prev.map(u => u.id === userId ? { ...u, password: 'password123' } : u));
      addAuditLog(currentUser?.id || 'SYS', 'RESET_PASSWORD', 'SECURITY', `Reset password for ${userId}`);
  };

  const changeMyPassword = (newPassword: string) => {
      if (!currentUser) return;
      setStaff(prev => prev.map(u => u.id === currentUser.id ? { ...u, password: newPassword } : u));
      setCurrentUser(prev => prev ? ({ ...prev, password: newPassword }) : null);
      addAuditLog(currentUser.id, 'CHANGE_PASSWORD', 'SECURITY', 'User changed own password');
  };

  // --- RBAC & ACCESS ---
  const hasPermission = (permission: Permission): boolean => {
      if (!currentUser) return false;
      const role = roles.find(r => r.id === currentUser.roleId);
      return role ? role.permissions.includes(permission) : false;
  };

  // Filter accounts/loans based on branch (Unless Super Admin)
  const getVisibleAccounts = () => {
      if (!currentUser) return [];
      if (currentUser.roleId === 'R001') return accounts; // Super Admin sees all
      return accounts.filter(a => a.branchId === currentUser.branchId);
  };

  // --- SYSTEM CONFIG ---
  const updateSystemConfig = (newConfig: SystemConfig) => {
      if (!hasPermission('SYSTEM_CONFIG')) throw new Error("Permission Denied");
      setSystemConfig(newConfig);
      addAuditLog(currentUser?.id || 'SYS', 'UPDATE_CONFIG', 'SYSTEM', 'Updated System Limits');
  };

  // --- RATE LIMITING ---
  const checkRateLimit = () => {
      const now = Date.now();
      const recentActions = actionTimestamps.filter(t => now - t < RATE_LIMIT_WINDOW);
      
      if (recentActions.length >= RATE_LIMIT_THRESHOLD) {
          throw new Error("System busy: Rate limit exceeded. Please wait.");
      }
      
      setActionTimestamps([...recentActions, now]);
  };

  // --- CORE FUNCTIONS ---

  const addAuditLog = (user: string, action: string, module: string, details: string, status: 'SUCCESS' | 'FAILURE' | 'WARNING' = 'SUCCESS') => {
    const newLog: AuditLog = {
      id: `AUD-${Date.now()}`,
      timestamp: new Date().toISOString(),
      user,
      action,
      module,
      details,
      status
    };
    setAuditLogs(prev => [newLog, ...prev]);
  };

  const createApprovalRequest = (
      type: ApprovalRequest['type'], 
      payload: any, 
      description: string, 
      amount: number,
      requesterName: string
  ) => {
      const newReq: ApprovalRequest = {
          id: `REQ-${Date.now()}`,
          type,
          requesterName,
          requestDate: new Date().toISOString(),
          description,
          amount,
          status: 'PENDING',
          payload
      };
      setApprovalRequests(prev => [newReq, ...prev]);
      addAuditLog(requesterName, 'CREATE_REQUEST', 'APPROVAL', `Created request ${newReq.id}: ${description}`);
      return newReq.id;
  };

  const approveRequest = (id: string, approverName: string) => {
      if (!hasPermission('LOAN_APPROVE')) { 
          throw new Error("Insufficient Permissions");
      }
      const req = approvalRequests.find(r => r.id === id);
      if (!req || req.status !== 'PENDING') return;

      // Execute Logic based on type
      if (req.type === 'TRANSACTION_LIMIT') {
          processTellerTransaction(
              req.payload.accountId,
              req.payload.type,
              req.payload.amount,
              req.payload.narration + ` (Approved by ${approverName})`,
              req.payload.tellerId,
              true // FORCE FLAG (Bypass limits)
          );
      } else if (req.type === 'LOAN_DISBURSEMENT') {
          disburseLoan(req.payload.loanId, true);
      }

      setApprovalRequests(prev => prev.map(r => r.id === id ? { ...r, status: 'APPROVED' } : r));
      addAuditLog(approverName, 'APPROVE_REQUEST', 'APPROVAL', `Approved ${id}`);
  };

  const rejectRequest = (id: string, approverName: string) => {
      setApprovalRequests(prev => prev.map(r => r.id === id ? { ...r, status: 'REJECTED' } : r));
      addAuditLog(approverName, 'REJECT_REQUEST', 'APPROVAL', `Rejected ${id}`);
  };

  const postGL = (description: string, lines: { code: string; debit: number; credit: number }[], user: string = 'SYSTEM') => {
    const newJV: JournalEntry = {
      id: `JV-${Date.now()}`,
      date: businessDate,
      reference: 'AUTO-POST',
      description,
      lines: lines.map(l => ({ accountCode: l.code, debit: l.debit, credit: l.credit })),
      postedBy: user,
      status: 'POSTED'
    };

    setJournalEntries(prev => [newJV, ...prev]);

    // Update GL Balances
    setGlAccounts(prev => prev.map(acc => {
      const line = lines.find(l => l.code === acc.code);
      if (!line) return acc;
      
      let balanceChange = 0;
      if (['ASSET', 'EXPENSE'].includes(acc.category)) {
        balanceChange = line.debit - line.credit;
      } else {
        balanceChange = line.credit - line.debit;
      }

      return { ...acc, balance: acc.balance + balanceChange };
    }));
  };

  // --- GL MANAGEMENT ---
  const createGLAccount = (account: GLAccount) => {
      if (!hasPermission('GL_MANAGE')) throw new Error("Permission Denied: GL_MANAGE required.");
      if (glAccounts.some(g => g.code === account.code)) throw new Error("GL Code already exists.");
      setGlAccounts(prev => [...prev, account]);
      addAuditLog(currentUser?.id || 'SYS', 'CREATE_GL', 'GL', `Created GL ${account.code} - ${account.name}`);
  };

  const updateGLAccount = (code: string, updates: Partial<GLAccount>) => {
      if (!hasPermission('GL_MANAGE')) throw new Error("Permission Denied: GL_MANAGE required.");
      setGlAccounts(prev => prev.map(g => g.code === code ? { ...g, ...updates } : g));
      addAuditLog(currentUser?.id || 'SYS', 'UPDATE_GL', 'GL', `Updated GL ${code}`);
  };

  const processTellerTransaction = (
    accountId: string, 
    type: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'LOAN_REPAYMENT', 
    amount: number, 
    narration: string, 
    tellerId: string,
    force: boolean = false
  ): { success: boolean; id: string; message: string; status: 'POSTED' | 'PENDING_APPROVAL' } => {
    
    // Rate Limit Check
    if (!force) checkRateLimit();

    const account = accounts.find(a => a.id === accountId);
    if (!account) throw new Error("Account not found");

    if (currentUser && currentUser.roleId !== 'R001' && account.branchId !== currentUser.branchId) {
        addAuditLog(tellerId, 'CROSS_BRANCH', 'TELLER', `Txn on ${accountId} (Branch: ${account.branchId})`, 'WARNING');
    }

    const customer = customers.find(c => c.id === account.cif);
    if (!customer) throw new Error("Customer CIF link broken");

    // --- BoG COMPLIANCE & LIMIT CHECKS (Dynamic) ---
    if (!force) {
        // Use Dynamic Limits from systemConfig
        const limits = systemConfig.kycLimits[customer.kycLevel] || systemConfig.kycLimits['Tier 1'];
        
        if (type === 'WITHDRAWAL' || type === 'TRANSFER') {
            if (amount > limits.dailyLimit) {
                 const reqId = createApprovalRequest(
                     'TRANSACTION_LIMIT', 
                     { accountId, type, amount, narration, tellerId }, 
                     `${type} exceeds Daily Limit (${limits.dailyLimit}) for ${customer.kycLevel}`, 
                     amount, 
                     tellerId
                 );
                 return { success: true, id: reqId, message: "Limit Exceeded. Sent for Approval.", status: 'PENDING_APPROVAL' };
            }
            if (account.balance < amount) {
                throw new Error("Insufficient Funds");
            }
        } else if (type === 'DEPOSIT') {
            if ((account.balance + amount) > limits.maxBalance) {
                 const reqId = createApprovalRequest(
                     'TRANSACTION_LIMIT', 
                     { accountId, type, amount, narration, tellerId }, 
                     `Deposit exceeds Max Balance (${limits.maxBalance}) for ${customer.kycLevel}`, 
                     amount, 
                     tellerId
                 );
                 return { success: true, id: reqId, message: "Max Balance Exceeded. Sent for Approval.", status: 'PENDING_APPROVAL' };
            }
        }

        // --- AML LARGE CASH CHECK (Dynamic) ---
        if (amount > systemConfig.amlThreshold) {
            const reqId = createApprovalRequest(
                'TRANSACTION_LIMIT', 
                { accountId, type, amount, narration, tellerId }, 
                `Large Cash Transaction (>${systemConfig.amlThreshold}) requires Supervisor Authorization`, 
                amount, 
                tellerId
            );
            return { success: true, id: reqId, message: "AML Limit. Sent for Approval.", status: 'PENDING_APPROVAL' };
        }
    }

    // --- FINANCIAL POSTING ---
    let taxAmount = 0;
    if (type === 'WITHDRAWAL' && amount > 100) {
        taxAmount = amount * GRA_E_LEVY_RATE;
    }

    const totalDebit = (type === 'WITHDRAWAL' || type === 'TRANSFER') ? amount + taxAmount : 0;
    const totalCredit = type === 'DEPOSIT' ? amount : 0;

    if ((type === 'WITHDRAWAL' || type === 'TRANSFER') && account.balance < totalDebit) {
         throw new Error(`Insufficient funds for Amount + E-Levy (GHS ${taxAmount.toFixed(2)})`);
    }

    setAccounts(prev => prev.map(a => 
      a.id === accountId 
        ? { 
            ...a, 
            balance: (type === 'DEPOSIT' || type === 'LOAN_REPAYMENT') ? a.balance + amount : a.balance - totalDebit, 
            lastTransDate: businessDate 
          } 
        : a
    ));

    const tx: Transaction = {
      id: `TXN-${Date.now()}`,
      accountId,
      type,
      amount: (type === 'WITHDRAWAL' || type === 'TRANSFER') ? totalDebit : amount,
      narration: `${narration} ${taxAmount > 0 ? `(Inc. GHS ${taxAmount.toFixed(2)} E-Levy)` : ''}`,
      date: new Date().toISOString(),
      tellerId,
      status: 'POSTED',
      reference: `REF-${Math.floor(Math.random() * 10000)}`
    };
    setTransactions(prev => [tx, ...prev]);

    const glCode = account.type === 'SAVINGS' ? '20100' : '20101';
    
    if (type === 'DEPOSIT') {
      postGL(`Cash Deposit - ${accountId}`, [
        { code: '10100', debit: amount, credit: 0 },
        { code: glCode, debit: 0, credit: amount } 
      ], tellerId);
    } else if (type === 'WITHDRAWAL') {
      const lines = [
          { code: glCode, debit: amount, credit: 0 },
          { code: '10100', debit: 0, credit: amount }
      ];
      if (taxAmount > 0) {
          lines.push({ code: glCode, debit: taxAmount, credit: 0 }); 
          lines.push({ code: GRA_GL_ACCOUNT, debit: 0, credit: taxAmount }); 
      }
      postGL(`Cash Withdrawal - ${accountId}`, lines, tellerId);
    } 
    
    addAuditLog(tellerId, type, 'TELLER', `${type} of ${amount} to ${accountId}`);
    return { success: true, id: tx.id, message: "Posted Successfully", status: 'POSTED' };
  };

  const createCustomer = (customerData: Omit<Customer, 'id'>) => {
      if (!hasPermission('USER_WRITE')) throw new Error("Permission Denied");
      
      const ghanaCardRegex = /^GHA-\d{9}-\d{1}$/;
      if (!ghanaCardRegex.test(customerData.ghanaCard)) {
          throw new Error("Invalid Ghana Card Format. Must be GHA-xxxxxxxxx-x");
      }

      const newId = `CIF${100000 + customers.length + 1}`;
      const newCustomer = { ...customerData, id: newId };
      setCustomers(prev => [newCustomer, ...prev]);
      addAuditLog(currentUser?.id || 'SYS', 'CREATE_CUSTOMER', 'CIF', `Created Customer ${newId} [Tier: ${customerData.kycLevel}]`);
      return newId;
  };

  const updateCustomer = (id: string, updates: Partial<Customer>) => {
      if (!hasPermission('USER_WRITE')) throw new Error("Permission Denied");
      setCustomers(prev => prev.map(c => c.id === id ? { ...c, ...updates } : c));
      addAuditLog(currentUser?.id || 'SYS', 'UPDATE_CUSTOMER', 'CRM', `Updated profile for ${id}`);
  };

  const createAccount = (cif: string, productCode: string, currency: 'GHS'|'USD' = 'GHS') => {
      if (!hasPermission('ACCOUNT_WRITE')) throw new Error("Permission Denied");
      const product = products.find(p => p.id === productCode);
      if(!product) throw new Error("Invalid Product Code");

      const branchCode = currentUser?.branchId ? branches.find(b => b.id === currentUser.branchId)?.code : '201';

      const newAcctId = `${branchCode}${100000 + accounts.length + 1}01`; // Use Branch Code
      const newAccount: Account = {
          id: newAcctId,
          cif,
          branchId: currentUser?.branchId || 'BR001',
          type: product.type as Account['type'], 
          currency,
          balance: 0,
          lienAmount: 0,
          status: 'ACTIVE',
          productCode,
          lastTransDate: businessDate
      };
      setAccounts(prev => [newAccount, ...prev]);
      addAuditLog(currentUser?.id || 'SYS', 'OPEN_ACCOUNT', 'CIF', `Opened Account ${newAcctId} (${product.name}) for ${cif}`);
      return newAcctId;
  };

  const disburseLoan = (loanId: string, force: boolean = false) => {
      if (!hasPermission('LOAN_WRITE')) throw new Error("Permission Denied");
      const loan = loans.find(l => l.id === loanId);
      if(!loan) return;
      if(loan.status === 'ACTIVE') return; 

      if (!force && loan.principal > 10000) {
          createApprovalRequest(
              'LOAN_DISBURSEMENT',
              { loanId },
              `Disburse ${loan.productName} to ${loan.cif}`,
              loan.principal,
              currentUser?.name || 'LOAN_OFFICER'
          );
          return; 
      }

      setLoans(prev => prev.map(l => l.id === loanId ? { ...l, status: 'ACTIVE', disbursementDate: businessDate } : l));

      postGL(`Loan Disbursement - ${loanId}`, [
          { code: '12000', debit: loan.principal, credit: 0 },
          { code: '10200', debit: 0, credit: loan.principal }
      ], 'LOAN_MGR');

      addAuditLog('LOAN_MGR', 'LOAN_DISBURSE', 'LENDING', `Disbursed Loan ${loanId}: ${loan.principal}`);
  };

  const createGroup = (groupData: Omit<Group, 'id' | 'formationDate'>) => {
      const newId = `GRP-${Date.now().toString().slice(-4)}`;
      const newGroup: Group = {
          ...groupData,
          id: newId,
          formationDate: new Date().toISOString().split('T')[0],
      };
      setGroups(prev => [newGroup, ...prev]);
      addAuditLog('SYS_ADMIN', 'CREATE_GROUP', 'LENDING', `Created Group ${newId}: ${groupData.name}`);
      return newId;
  };

  const addMemberToGroup = (groupId: string, cifId: string) => {
      setGroups(prev => prev.map(g => {
          if (g.id === groupId && !g.members.includes(cifId)) {
              return { ...g, members: [...g.members, cifId] };
          }
          return g;
      }));
      addAuditLog('SYS_ADMIN', 'ADD_GROUP_MEMBER', 'LENDING', `Added ${cifId} to ${groupId}`);
  };

  const removeMemberFromGroup = (groupId: string, cifId: string) => {
      setGroups(prev => prev.map(g => {
          if (g.id === groupId) {
              return { ...g, members: g.members.filter(m => m !== cifId) };
          }
          return g;
      }));
      addAuditLog('SYS_ADMIN', 'REM_GROUP_MEMBER', 'LENDING', `Removed ${cifId} from ${groupId}`);
  };

  const createProduct = (product: Product) => {
      setProducts(prev => [...prev, product]);
      addAuditLog('SYS_ADMIN', 'CREATE_PRODUCT', 'CONFIG', `Created Product ${product.id}: ${product.name}`);
  };

  const updateProduct = (id: string, updates: Partial<Product>) => {
      setProducts(prev => prev.map(p => p.id === id ? { ...p, ...updates } : p));
      addAuditLog('SYS_ADMIN', 'UPDATE_PRODUCT', 'CONFIG', `Updated Product ${id}`);
  };

  // --- WORKFLOW MANAGEMENT ---
  const createWorkflow = (workflow: Workflow) => {
      if (!hasPermission('SYSTEM_CONFIG')) throw new Error("Permission Denied");
      setWorkflows(prev