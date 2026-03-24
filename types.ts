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
  cif?: string; // Alias for id (for backward compatibility)
  name: string;
  firstName?: string; // For individual customers
  lastName?: string; // For individual customers
  ghanaCard: string; // GHA-000000000-0
  digitalAddress: string; // GPS
  city?: string; // City/Town
  kycLevel: 'Tier 1' | 'Tier 2' | 'Tier 3'; // BoG KYC Tiers
  phone: string;
  phoneNumber?: string; // Alias for phone
  email: string;
  riskRating: 'Low' | 'Medium' | 'High';
  status?: 'ACTIVE' | 'PENDING' | 'INACTIVE' | 'SUSPENDED';
  kycVerified?: boolean;
  registrationDate?: string;
  // Customer type
  type?: 'INDIVIDUAL' | 'CORPORATE';
  // Individual-specific fields
  dateOfBirth?: string;
  gender?: 'Male' | 'Female' | 'Other';
  maritalStatus?: 'Single' | 'Married' | 'Divorced' | 'Widowed';
  spouseName?: string;
  ssnitNo?: string; // Social Security Number
  nationality?: string;
  employer?: string;
  // Corporate-specific fields
  businessRegistrationNo?: string;
  tin?: string; // Tax Identification Number
  sector?: string; // e.g., 'Manufacturing', 'Services'
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
  type: 'TRANSACTION_LIMIT' | 'LOAN_DISBURSEMENT' | 'NEW_USER' | 'CASH_EXCEPTION';
  requesterName: string;
  requestDate: string;
  description: string;
  amount?: number;
  status: 'PENDING' | 'APPROVED' | 'REJECTED';
  remarks?: string;
  referenceNo?: string;
  payload: {
    entityType: string;
    entityId: string;
    workflowId?: string;
    workflowName?: string;
    currentStep?: number;
    payloadJson?: string;
    loanDetails?: {
      loanId: string;
      customerId: string;
      customerName?: string;
      productCode?: string;
      productName?: string;
      principal: number;
      outstandingBalance?: number;
      rate: number;
      termMonths: number;
      collateralType?: string;
      collateralValue?: number;
      parBucket?: string;
      status: string;
      appliedAt?: string;
    };
  };
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
  entityType?: string;
  entityId?: string;
  ipAddress?: string;
  oldValues?: string;
  newValues?: string;
  errorMessage?: string;
}

export interface SecuritySummary {
  windowHours: number;
  failedLoginCount: number;
  securityAlertCount: number;
  largeTransactionAlertCount: number;
  registeredDevices: number;
  activeDevices: number;
  blockedDevices: number;
  isolatedDevices: number;
  outdatedDevices: number;
  irregularActivityCount: number;
  newlyObservedDevices: number;
  monitoredDevices: number;
  suspiciousDevices: number;
  restrictedDevices: number;
  revokedDevices: number;
  activeSessions: number;
  minimumSupportedVersion: string;
  generatedAt: string;
}

export interface FailedLoginAttempt {
  id: number;
  email: string;
  ipAddress: string;
  failureReason?: string;
  userAgent?: string;
  attemptedAt: string;
}

export interface SecurityAlert {
  id: number;
  action: string;
  entityType: string;
  entityId?: string;
  userId?: string;
  description?: string;
  ipAddress?: string;
  status: string;
  errorMessage?: string;
  newValues?: string;
  createdAt: string;
}

export interface SecurityDevice {
  id: string;
  name: string;
  deviceType: string;
  status: 'ACTIVE' | 'BLOCKED' | 'ISOLATED' | 'FLAGGED' | 'PENDING_SETUP' | 'RESTRICTED' | 'REVOKED';
  lifecycleState: 'NEW_OBSERVED' | 'ALLOWED' | 'MONITORED' | 'SUSPICIOUS' | 'RESTRICTED' | 'REVOKED';
  accessDecision: 'ALLOWED' | 'RESTRICTED' | 'REVOKED';
  riskLevel: 'LOW' | 'MEDIUM' | 'HIGH';
  softwareStatus: 'COMPLIANT' | 'OUTDATED';
  softwareVersion: string;
  minimumSupportedVersion: string;
  branchId?: string;
  branchName?: string;
  assignedStaffId?: string;
  assignedStaffName?: string;
  serialNumber?: string;
  ipAddress?: string;
  notes?: string;
  blockReason?: string;
  detectionSource?: string;
  userAgent?: string;
  lastSeenUserId?: string;
  lastSeenUserName?: string;
  lastAction?: string;
  lastActionByUserId?: string;
  autoObserved: boolean;
  requiresReview: boolean;
  observationCount: number;
  firstObservedAt?: string;
  createdAt: string;
  updatedAt: string;
  lastSeenAt?: string;
  lastPatchedAt?: string;
  lastBlockedAt?: string;
  lastActionAt?: string;
}

export interface SecuritySession {
  id: string;
  staffId: string;
  staffName: string;
  email: string;
  ipAddress: string;
  userAgent?: string;
  createdAt: string;
  expiresAt: string;
  lastActivity: string;
  isActive: boolean;
}

export interface DeviceScanResult {
  minimumSupportedVersion: string;
  scannedCount: number;
  outdatedCount: number;
  flaggedCount: number;
  devices: SecurityDevice[];
  scannedAt: string;
}

export interface IrregularTransaction {
  id: string;
  transactionId: string;
  reference?: string;
  accountId?: string;
  customerId?: string;
  customerName?: string;
  type: string;
  amount: number;
  severity: 'LOW' | 'MEDIUM' | 'HIGH';
  riskScore: number;
  summary: string;
  flags: string[];
  tellerId?: string;
  tellerName?: string;
  status?: string;
  transactionDate: string;
  detectedAt: string;
}

export interface GLAccount {
  code: string;
  name: string;
  category: 'ASSET' | 'LIABILITY' | 'EQUITY' | 'INCOME' | 'EXPENSE';
  balance: number;
  currency: string;
  isHeader?: boolean;
}

export interface RegulatoryChartSeedResponse {
  regionCode: string;
  standardName: string;
  insertedCount: number;
  existingCount: number;
  totalStandardAccounts: number;
  insertedCodes: string[];
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
  | 'CLIENT_READ' | 'CLIENT_WRITE'
  | 'GROUP_READ' | 'GROUP_WRITE'
  | 'LOAN_READ' | 'LOAN_WRITE' | 'LOAN_APPROVE' | 'LOAN_DISBURSE'
  | 'TELLER_POST' | 'TELLER_TRANSACTION'
  | 'GL_READ' | 'GL_POST' | 'GL_MANAGE' | 'GL_WRITE' | 'GL_CONFIG' | 'REPORT_VIEW'
  | 'APPROVAL_TASK' | 'AUDIT_READ' | 'SYSTEM_ADMIN' | 'SYSTEM_DESIGN' | 'DATA_MIGRATION' | 'SYSTEM_CONFIG';

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
  scopeType?: 'BranchOnly' | 'Regional' | 'All';
  roles?: string[];
  permissions?: string[];
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
  status: 'INFO' | 'SUCCESS' | 'WARNING' | 'ERROR';
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
  auth: {
    enabled: boolean;
    provider: string; // 'LOCAL', 'AZURE_AD', 'LDAP'
    tenantId?: string;
    clientId?: string;
    ldapServer?: string;
  };
  eodScheduler: {
    enabled: boolean;
    timeUtc: string;
    lastRunDate?: string;
  };
  orass: {
    enabled: boolean;
    institutionCode: string;
    submissionMode: 'TEST' | 'PRODUCTION';
    endpointUrl: string;
    username?: string;
    password?: string;
    certificateAlias?: string;
    sourceReportCode: string;
    autoSubmit: boolean;
    cutoffTimeUtc: string;
    fallbackEmail?: string;
    lastSubmissionAt?: string;
  };
  dbProvider: string; // 'MYSQL', 'SQLSERVER', 'POSTGRES'
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

export interface MenuItem {
  id: string;
  label: string;
  icon?: string;
  path?: string;
  type?: 'WORKFLOW' | 'FORM' | 'PAGE';
  targetId?: string;
  requiredRoleIds?: string[];
  children?: MenuItem[];
  roles?: string[];
  description?: string;
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
  description?: string;
  components?: UIComponentConfig[];
  published?: boolean;
  linkedTable?: string;
  linkedWorkflow?: string;
  menuLabel?: string;
  menuIcon?: string;
  createdBy?: string;
  createdByName?: string;
  isTemplate?: boolean;
  isLocked?: boolean;
  createdAt?: string;
  updatedAt?: string;
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

export type DBProviderType = 'MYSQL' | 'SQLSERVER' | 'POSTGRES';

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

  // === LEDGER ENGINE TYPES ===
  export interface LedgerPostingResult {
    success: boolean;
    transactionId: string;
    reference: string;
    narration: string;
    amount: number;
    appliedFees: number;
    netAmount: number;
    newBalance: number;
    availableMargin: number;
    status: 'POSTED' | 'PENDING' | 'REJECTED';
    message: string;
    journalLines?: Array<{
      glCode: string;
      debit: number;
      credit: number;
      narration: string;
    }>;
  }

  export interface LedgerEntry {
    id: string;
    journalId: string;
    glCode: string;
    glName: string;
    debit: number;
    credit: number;
    narration: string;
    postedDate: string;
  }

  export interface LedgerBalance {
    accountId: string;
    balance: number;
    lienAmount: number;
    availableBalance: number;
    dailyDebitTotal: number;
    dailyCreditTotal: number;
  }

  // === BOG COMPLIANCE TYPES ===
  export interface BogComplianceCheck {
    compliant: boolean;
    reason?: string;
    warnings?: string[];
    kycLevel: string;
    dailyLimit: number;
  }

  export interface SuspiciousActivityFlag {
    isSuspicious: boolean;
    riskScore: number;
    flags: string[];
  }






export interface ProductGroupRules {
  productId?: string;
  minMembersRequired: number;
  maxMembersAllowed: number;
  minWeeks?: number;
  maxWeeks?: number;
  requiresCompulsorySavings: boolean;
  minSavingsToLoanRatio?: number;
  requiresGroupApprovalMeeting: boolean;
  requiresJointLiability: boolean;
  allowTopUp: boolean;
  allowReschedule: boolean;
  maxCycleNumber?: number;
  cycleIncrementRulesJson?: string;
  defaultRepaymentFrequency: 'Weekly' | 'Monthly';
  defaultInterestMethod: 'Flat' | 'ReducingBalance';
  penaltyPolicyJson?: string;
  attendanceRuleJson?: string;
  eligibilityRuleJson?: string;
  meetingCollectionRuleJson?: string;
  allocationOrderJson?: string;
  accountingProfileJson?: string;
  disclosureTemplate?: string;
}

export interface ProductEligibilityRules {
  productId?: string;
  requiresKycComplete: boolean;
  blockOnSevereArrears: boolean;
  maxAllowedExposure?: number;
  minMembershipDays?: number;
  minAttendanceRate?: number;
  requireCreditBureauCheck: boolean;
  creditBureauProvider?: string;
  minimumCreditScore?: number;
  ruleJson?: string;
}

export interface GroupLendingProductFlags {
  lendingMethodology?: 'INDIVIDUAL' | 'GROUP' | 'HYBRID';
  isGroupLoanEnabled?: boolean;
  supportsJointLiability?: boolean;
  requiresCenter?: boolean;
  requiresGroup?: boolean;
  defaultRepaymentFrequency?: 'Weekly' | 'Monthly';
  allowedRepaymentFrequencies?: Array<'Weekly' | 'Monthly'>;
  supportsWeeklyRepayment?: boolean;
  minimumGroupSize?: number;
  maximumGroupSize?: number;
  requiresCompulsorySavings?: boolean;
  minimumSavingsToLoanRatio?: number;
  requiresGroupApprovalMeeting?: boolean;
  usesMemberLevelUnderwriting?: boolean;
  usesGroupLevelApproval?: boolean;
  loanCyclePolicyType?: string;
  maxCycleNumber?: number;
  graduatedCycleLimitRulesJson?: string;
  attendanceRuleType?: string;
  arrearsEligibilityRuleType?: string;
  groupGuaranteePolicyType?: string;
  meetingCollectionMode?: string;
  allowBatchDisbursement?: boolean;
  allowMemberLevelDisbursementAdjustment?: boolean;
  allowTopUpWithinGroup?: boolean;
  allowRescheduleWithinGroup?: boolean;
  groupPenaltyPolicy?: string;
  groupDelinquencyPolicy?: string;
  groupOfficerAssignmentMode?: string;
  groupRules?: ProductGroupRules;
  eligibilityRules?: ProductEligibilityRules;
}

export interface LendingCenter {
  id: string;
  branchId: string;
  centerCode: string;
  centerName: string;
  meetingDayOfWeek?: string;
  meetingLocation?: string;
  assignedOfficerId?: string;
  status: string;
}

export interface LendingGroupMember {
  id: string;
  customerId: string;
  customerName: string;
  memberRole: string;
  status: string;
  kycStatus: string;
  isEligibleForLoan: boolean;
  currentLoanCycle: number;
  currentExposure: number;
  arrearsFlag: boolean;
}

export interface LendingGroup {
  id: string;
  branchId: string;
  centerId?: string;
  groupCode?: string;
  groupName: string;
  meetingDayOfWeek?: string;
  meetingFrequency: string;
  meetingLocation?: string;
  assignedOfficerId?: string;
  chairpersonCustomerId?: string;
  secretaryCustomerId?: string;
  treasurerCustomerId?: string;
  formationDate?: string;
  status: string;
  isJointLiabilityEnabled: boolean;
  maxMembers?: number;
  notes?: string;
  members: LendingGroupMember[];
}

export interface GroupLoanApplicationMember {
  id: string;
  groupMemberId: string;
  customerId: string;
  customerName: string;
  requestedAmount: number;
  approvedAmount: number;
  disbursedAmount: number;
  tenureWeeks: number;
  interestRate: number;
  interestMethod: string;
  repaymentFrequency: string;
  loanPurpose?: string;
  eligibilityStatus: string;
  existingExposureAmount: number;
  savingsBalanceAtApplication: number;
  scoreResult?: string;
  status: string;
}

export interface GroupLoanApplication {
  id: string;
  groupId: string;
  groupName: string;
  loanCycleNo: number;
  applicationDate: string;
  productId: string;
  productName: string;
  branchId: string;
  officerId?: string;
  status: string;
  totalApprovedAmount: number;
  totalRequestedAmount: number;
  totalDisbursedAmount: number;
  approvalDate?: string;
  disbursementDate?: string;
  meetingReference?: string;
  groupResolutionReference?: string;
  notes?: string;
  members: GroupLoanApplicationMember[];
}

export interface GroupMeeting {
  id: string;
  groupId: string;
  groupName: string;
  centerId?: string;
  meetingDate: string;
  meetingType: string;
  location?: string;
  officerId?: string;
  status: string;
  attendanceCount: number;
  notes?: string;
  attendances: Array<{
    groupMemberId: string;
    customerId: string;
    attendanceStatus: string;
    arrivalTime?: string;
    notes?: string;
  }>;
}

export interface GroupCollectionBatch {
  id: string;
  groupId: string;
  groupMeetingId?: string;
  branchId: string;
  officerId?: string;
  collectionDate: string;
  status: string;
  totalCollectedAmount: number;
  totalExpectedAmount: number;
  varianceAmount: number;
  channel: string;
  referenceNo?: string;
  lines: Array<{
    id: string;
    loanAccountId: string;
    groupMemberId: string;
    customerId: string;
    expectedInstallment: number;
    amountCollected: number;
    principalComponent: number;
    interestComponent: number;
    penaltyComponent: number;
    savingsComponent: number;
    feeComponent: number;
    arrearsRecovered: number;
    status: string;
  }>;
}

export interface GroupPortfolioSummary {
  activeGroups: number;
  activeMembers: number;
  totalPortfolio: number;
  par30: number;
  weeklyDueThisWeek: number;
  collectionsThisWeek: number;
}

export interface Product extends GroupLendingProductFlags {}

