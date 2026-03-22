
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

export type CustomerType = 'INDIVIDUAL' | 'CORPORATE';

export interface Customer {
    id: string; // CIF Number
    type: CustomerType;
    name: string;

    // Individual Fields
    gender?: 'M' | 'F';
    dateOfBirth?: string;
    ghanaCard?: string; // GHA-000000000-0
    nationality?: string;
    maritalStatus?: 'SINGLE' | 'MARRIED' | 'DIVORCED' | 'WIDOWED';
    spouseName?: string;
    employer?: string;
    jobTitle?: string;
    ssnitNo?: string;

    // Corporate Fields
    businessRegistrationNo?: string;
    registrationDate?: string;
    tin?: string; // Tax Identification Number
    sector?: string; // e.g. Agriculture, Commerce
    legalForm?: 'LIMITED_LIABILITY' | 'SOLE_PROPRIETORSHIP' | 'PARTNERSHIP';

    // Common
    digitalAddress: string; // GPS
    postalAddress?: string;
    kycLevel: 'Tier 1' | 'Tier 2' | 'Tier 3'; // BoG KYC Tiers
    phone: string;
    secondaryPhone?: string;
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

export interface Financials {
    monthlyIncome: number;
    monthlyExpense: number;
    existingDebtService: number; // Monthly payments on other loans
    totalAssets: number;
    totalLiabilities: number;
}

export interface CreditScore {
    score: number; // 0 - 100
    grade: 'A' | 'B' | 'C' | 'D' | 'F';
    recommendation: 'APPROVE' | 'REVIEW' | 'REJECT';
    maxLoanLimit: number;
    factors: string[]; // Reasons for the score
    calculatedAt: string;
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
    creditScore?: CreditScore;
    financials?: Financials;
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
    type: 'TRANSACTION_LIMIT' | 'LOAN_DISBURSEMENT' | 'NEW_USER' | 'GL_POSTING';
    requesterName: string;
    requestDate: string;
    description: string;
    amount?: number;
    status: 'PENDING' | 'APPROVED' | 'REJECTED';
    payload: any; // Stores data needed to execute the action
    data?: any;
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
    // Client & Accounts
    | 'CLIENT_READ' | 'CLIENT_WRITE'
    | 'ACCOUNT_READ' | 'ACCOUNT_WRITE'
    | 'GROUP_READ' | 'GROUP_WRITE'
    // Lending
    | 'LOAN_READ' | 'LOAN_WRITE' | 'LOAN_APPROVE' | 'LOAN_DISBURSE'
    // Operations
    | 'TELLER_TRANSACTION'
    | 'APPROVAL_TASK'
    // Finance
    | 'GL_READ' | 'GL_WRITE' | 'GL_CONFIG'
    // Reporting & Compliance
    | 'REPORT_VIEW'
    | 'AUDIT_READ'
    // System
    | 'SYSTEM_ADMIN' // Users, Roles, Config
    | 'SYSTEM_DESIGN' // Workflows, Forms, Products, Menus
    | 'DATA_MIGRATION';

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
    status: 'Active' | 'Inactive' | 'Locked';
    lastLogin: string;
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
    dbProvider: 'MYSQL' | 'SQLSERVER' | 'POSTGRES';
    auth: {
        provider: 'LOCAL' | 'AZURE_AD' | 'LDAP';
        enabled: boolean;
        tenantId?: string;
        clientId?: string;
        ldapServer?: string;
    };
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

export type UIComponentType = 'TEXT_INPUT' | 'NUMBER_INPUT' | 'DATE_PICKER' | 'SELECT' | 'HEADER' | 'CARD' | 'TABLE';

export interface UIComponentConfig {
    id: string;
    type: UIComponentType;
    label: string;
    width: 'FULL' | 'HALF' | 'THIRD';
    required?: boolean;
    placeholder?: string;
    variableName?: string;
}

export interface UILayout {
    id: string;
    name: string;
    description: string;
    components: UIComponentConfig[];
    published: boolean;
    linkedTable?: string;
    linkedWorkflow?: string;
    menuLabel?: string;
    menuIcon?: string;
}

export interface MenuItem {
    id: string;
    label: string;
    icon: string;
    type: 'WORKFLOW' | 'FORM';
    targetId: string;
    requiredRoleIds: string[];
}

// API Documentation Types
export interface ApiEndpoint {
    method: 'GET' | 'POST' | 'PUT' | 'DELETE';
    path: string;
    summary: string;
    parameters: {
        name: string;
        in: 'body' | 'query' | 'path' | 'header';
        type: string;
        required: boolean;
    }[];
    response: string;
}

export type DBProviderType = 'MYSQL' | 'SQLSERVER' | 'POSTGRES';

export interface DevTask {
    id: string;
    title: string;
    status: 'TODO' | 'IN_PROGRESS' | 'DONE';
    category: 'BACKEND' | 'FRONTEND' | 'DEVOPS';
    priority: 'HIGH' | 'MEDIUM' | 'LOW';
}
