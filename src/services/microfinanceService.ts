import { API_ENDPOINTS } from './apiConfig';
import { httpClient } from './httpClient';

export interface MicrofinanceMetric {
  key: string;
  label: string;
  value: string;
  severity: string;
  subtitle?: string | null;
}

export interface FieldStaffDirectoryItem {
  staffId: string;
  name: string;
  email?: string | null;
  branchId?: string | null;
  status: string;
}

export interface CustomerSearchItem {
  customerId: string;
  customerName: string;
  branchId?: string | null;
  phoneNumber?: string | null;
}

export interface AccountSearchItem {
  accountId: string;
  accountNumber: string;
  customerId: string;
  customerName: string;
  productCode?: string | null;
  status: string;
  currency: string;
  availableBalance: number;
  isCompulsorySavings: boolean;
}

export interface MicrofinanceLoanPolicy {
  loanProductId: string;
  loanProductCode: string;
  loanProductName: string;
  repaymentFrequency: string;
  requiresCompulsorySavings: boolean;
  minimumSavingsToLoanRatio?: number | null;
}

export interface CollectorAssignment {
  id: string;
  staffId: string;
  staffName: string;
  customerId: string;
  customerName: string;
  routeCode?: string | null;
  meetingDay?: string | null;
  collectionFrequency: string;
  targetDepositAccountId?: string | null;
  targetLoanId?: string | null;
  isPrimaryCollector: boolean;
  status: string;
  assignedAtUtc: string;
  assignedBy?: string | null;
  nextCollectionDate?: string | null;
}

export interface FieldCollectionBatchLine {
  id: string;
  customerId: string;
  customerName: string;
  assignmentId?: string | null;
  targetAccountId?: string | null;
  targetLoanId?: string | null;
  collectionType: string;
  amount: number;
  currency: string;
  status: string;
  narrative?: string | null;
  externalReference?: string | null;
  collectedAtUtc: string;
  receiptNumber?: string | null;
}

export interface FieldCollectionBatch {
  id: string;
  staffId: string;
  staffName: string;
  businessDate: string;
  routeCode?: string | null;
  collectionType: string;
  openingFloat: number;
  expectedAmount: number;
  collectedAmount: number;
  settledAmount: number;
  varianceAmount: number;
  currency: string;
  status: string;
  openedAtUtc: string;
  submittedAtUtc?: string | null;
  settledAtUtc?: string | null;
  settlementReference?: string | null;
  notes?: string | null;
  lines: FieldCollectionBatchLine[];
}

export interface CompulsorySavingsAlert {
  customerId: string;
  customerName: string;
  loanProductName: string;
  requiredAmount: number;
  currentAmount: number;
  shortfallAmount: number;
  recommendation: string;
}

export interface MicrofinanceSummary {
  businessDate: string;
  metrics: MicrofinanceMetric[];
  fieldStaff: FieldStaffDirectoryItem[];
  activeAssignments: CollectorAssignment[];
  openBatches: FieldCollectionBatch[];
  compulsorySavingsAlerts: CompulsorySavingsAlert[];
  loanPolicies: MicrofinanceLoanPolicy[];
}

export interface UpsertCollectorAssignmentRequest {
  staffId: string;
  customerId: string;
  routeCode?: string;
  meetingDay?: string;
  collectionFrequency: string;
  targetDepositAccountId?: string;
  targetLoanId?: string;
  isPrimaryCollector?: boolean;
}

export interface OpenFieldCollectionBatchRequest {
  staffId: string;
  businessDate?: string;
  routeCode?: string;
  collectionType: string;
  openingFloat?: number;
  currency?: string;
  notes?: string;
}

export interface RecordFieldCollectionRequest {
  assignmentId?: string;
  customerId: string;
  targetAccountId?: string;
  targetLoanId?: string;
  collectionType: string;
  amount: number;
  currency?: string;
  narrative?: string;
  externalReference?: string;
}

export interface SubmitFieldCollectionBatchRequest {
  notes?: string;
}

export interface SettleFieldCollectionBatchRequest {
  settledAmount: number;
  settlementReference?: string;
  notes?: string;
}

class MicrofinanceService {
  async getSummary(): Promise<MicrofinanceSummary> {
    return httpClient.get<MicrofinanceSummary>(API_ENDPOINTS.microfinance.summary);
  }

  async searchCustomers(query: string): Promise<CustomerSearchItem[]> {
    const suffix = query.trim() ? `?query=${encodeURIComponent(query.trim())}` : '';
    return httpClient.get<CustomerSearchItem[]>(`${API_ENDPOINTS.microfinance.searchCustomers}${suffix}`);
  }

  async searchAccounts(query: string, customerId?: string): Promise<AccountSearchItem[]> {
    const params = new URLSearchParams();
    if (query.trim()) {
      params.set('query', query.trim());
    }
    if (customerId?.trim()) {
      params.set('customerId', customerId.trim());
    }
    const suffix = params.toString() ? `?${params.toString()}` : '';
    return httpClient.get<AccountSearchItem[]>(`${API_ENDPOINTS.microfinance.searchAccounts}${suffix}`);
  }

  async getLoanPolicies(): Promise<MicrofinanceLoanPolicy[]> {
    return httpClient.get<MicrofinanceLoanPolicy[]>(API_ENDPOINTS.microfinance.loanPolicies);
  }

  async upsertAssignment(payload: UpsertCollectorAssignmentRequest): Promise<CollectorAssignment> {
    return httpClient.post<CollectorAssignment>(API_ENDPOINTS.microfinance.upsertAssignment, payload);
  }

  async openBatch(payload: OpenFieldCollectionBatchRequest): Promise<FieldCollectionBatch> {
    return httpClient.post<FieldCollectionBatch>(API_ENDPOINTS.microfinance.openBatch, payload);
  }

  async recordCollection(batchId: string, payload: RecordFieldCollectionRequest): Promise<FieldCollectionBatchLine> {
    return httpClient.post<FieldCollectionBatchLine>(API_ENDPOINTS.microfinance.recordCollection(batchId), payload);
  }

  async submitBatch(batchId: string, payload: SubmitFieldCollectionBatchRequest = {}): Promise<FieldCollectionBatch> {
    return httpClient.post<FieldCollectionBatch>(API_ENDPOINTS.microfinance.submitBatch(batchId), payload);
  }

  async settleBatch(batchId: string, payload: SettleFieldCollectionBatchRequest): Promise<FieldCollectionBatch> {
    return httpClient.post<FieldCollectionBatch>(API_ENDPOINTS.microfinance.settleBatch(batchId), payload);
  }
}

export const microfinanceService = new MicrofinanceService();
