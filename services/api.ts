/**
 * BankInsight MySQL API Service
 * Centralized Axios client for MySQL-backed REST API backend.
 * API base URL and timeout come from localStorage (Setup → API Config).
 */

import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { buildApiBaseUrl, getApiConfig } from '../lib/configStore';

function getApiBase(): string {
  return buildApiBaseUrl();
}

export const api = axios.create({
  baseURL: '',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
  timeout: 30000,
});

// --- System Maintenance (API timeout / 503) ---
let maintenanceCallback: ((active: boolean) => void) | null = null;
export const setMaintenanceListener = (cb: (active: boolean) => void) => {
  maintenanceCallback = cb;
};
export const clearMaintenanceListener = () => {
  maintenanceCallback = null;
};

const showMaintenance = () => maintenanceCallback?.(true);
const hideMaintenance = () => maintenanceCallback?.(false);

api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const apiBase = getApiBase();
    if (config.url?.startsWith('/api') || config.url?.startsWith(apiBase)) {
      config.withCredentials = true;
      const { apiKey } = getApiConfig();
      if (apiKey) {
        config.headers['X-API-Key'] = apiKey;
      }
    }
    const { timeoutMs } = getApiConfig();
    if (timeoutMs != null && timeoutMs > 0) config.timeout = timeoutMs;
    return config;
  },
  (err) => Promise.reject(err)
);

api.interceptors.response.use(
  (res) => {
    hideMaintenance();
    return res;
  },
  (err: AxiosError) => {
    const status = err.response?.status;
    const message = (err.response?.data as any)?.message ?? err.message ?? '';
    const is503 = status === 503;
    const isServerDown =
      /Server Not Responding|timeout|ETIMEDOUT|ECONNREFUSED/i.test(message) ||
      (status === 502 || status === 504);
    if (is503 || isServerDown) {
      showMaintenance();
    }
    return Promise.reject(err);
  }
);

// --- REST API Endpoints ---

export interface GetCifListParams {
  branch?: string;
  page?: number;
  limit?: number;
  search?: string;
}

export interface AccountSummaryResult {
  accountId: string;
  cif: string;
  clearBalance: number;
  lienAmount: number;
  status: string;
  mandate?: { instructions: string; signatories: Array<{ name: string; role: string }> };
  currency: string;
  productCode: string;
  type: string;
  lastTransDate?: string;
}

export interface PostTellerTxnParams {
  accountId: string;
  type: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'LOAN_REPAYMENT';
  amount: number;
  narration: string;
  tellerId: string;
  eLevyAmount?: number;
}

export interface PostTellerTxnResult {
  success: boolean;
  txnRef?: string;
  status: 'POSTED' | 'PENDING_APPROVAL';
  message?: string;
  approvalRequestId?: string;
  serverELevy?: number;
}

export interface ProcessEodParams {
  processDate: string;
  step?: string;
}

/** GET /customers - Customer list for branch/search */
export function getCifList(params: GetCifListParams = {}) {
  const q = new URLSearchParams();
  if (params.branch) q.set('branch', params.branch);
  if (params.page != null) q.set('page', String(params.page));
  if (params.limit != null) q.set('limit', String(params.limit));
  if (params.search) q.set('search', params.search);
  const query = q.toString();
  return api.get(`${getApiBase()}/customers${query ? `?${query}` : ''}`).then((r) => r.data);
}

/** GET /customers/:cif - Single customer */
export function getCifDetail(cif: string) {
  return api.get(`${getApiBase()}/customers/${cif}`).then((r) => r.data);
}

/** GET /accounts/:accountId/summary - Real-time balance, lien, mandate (for teller validation) */
export function getAccountSummary(accountId: string) {
  return api.get(`${getApiBase()}/accounts/${accountId}/summary`).then((r) => r.data as AccountSummaryResult);
}

/** GET /accounts - Accounts for branch (for dashboard/teller list) */
export function getBranchAccounts(branchId: string) {
  return api.get(`${getApiBase()}/accounts`, { params: { branchId } }).then((r) => r.data);
}

/** POST /transactions - All cash operations; server applies E-Levy (source of truth) */
export function postTellerTxn(params: PostTellerTxnParams) {
  return api.post(`${getApiBase()}/transactions`, params).then((r) => r.data as PostTellerTxnResult);
}

/** POST /eod - End of Day batch processing */
export function processEod(params: ProcessEodParams) {
  return api.post(`${getApiBase()}/eod`, params).then((r) => r.data);
}

/** POST /approvals - Maker-Checker: route to approvals when over amlThreshold */
export function createApprovalRequest(payload: {
  type: string;
  requesterName: string;
  description: string;
  amount?: number;
  payload: Record<string, unknown>;
}) {
  return api.post(`${getApiBase()}/approvals`, payload).then((r) => r.data);
}

/** POST /auth/login - Authentication */
export function authLogin(email: string, password: string) {
  return api
    .post(`${getApiBase()}/auth/login`, { email, password }, { withCredentials: true })
    .then((r) => r.data);
}

/** POST /auth/logout - Logout */
export function authLogout() {
  return api.post(`${getApiBase()}/auth/logout`, {}, { withCredentials: true }).then((r) => r.data);
}

/** GET /config/system - KYC limits, amlThreshold */
export function getSystemConfig() {
  return api.get(`${getApiBase()}/config/system`).then((r) => r.data);
}

/** GET /approvals - Pending approval list */
export function getApprovalRequests(branchId?: string) {
  const params = branchId ? { branchId } : {};
  return api.get(`${getApiBase()}/approvals`, { params }).then((r) => r.data);
}

/** POST /approvals/:id/approve - Maker-Checker actions */
export function approveRequest(requestId: string, approverName: string) {
  return api
    .post(`${getApiBase()}/approvals/${requestId}/approve`, { approverName })
    .then((r) => r.data);
}

export function rejectRequest(requestId: string, approverName: string) {
  return api
    .post(`${getApiBase()}/approvals/${requestId}/reject`, { approverName })
    .then((r) => r.data);
}

/** GET /branches - Branch list */
export function getBranches() {
  return api.get(`${getApiBase()}/branches`).then((r) => r.data);
}

/** GET /loans - Loans (optional branch filter) */
export function getLoans(branchId?: string) {
  const params = branchId ? { branchId } : {};
  return api.get(`${getApiBase()}/loans`, { params }).then((r) => r.data);
}

/** GET /gl/accounts - Chart of accounts */
export function getGlAccounts() {
  return api.get(`${getApiBase()}/gl/accounts`).then((r) => r.data);
}

/** GET /gl/journal-entries - Journal list (optional date) */
export function getJournalEntries(date?: string) {
  const params = date ? { date } : {};
  return api.get(`${getApiBase()}/gl/journal-entries`, { params }).then((r) => r.data);
}

/** GET /audit/logs - Audit trail */
export function getAuditLogs(module?: string, limit?: number) {
  const params: Record<string, string | number> = {};
  if (module) params.module = module;
  if (limit != null) params.limit = limit;
  return api.get(`${getApiBase()}/audit/logs`, { params }).then((r) => r.data);
}

/** POST /customers - CIF creation (Ghana Card validated on backend) */
export function createCustomer(payload: Record<string, unknown>) {
  return api.post(`${getApiBase()}/customers`, payload).then((r) => r.data);
}

/** PATCH /customers/:cif - Non-financial updates (optimistic-friendly) */
export function updateCustomer(cif: string, payload: Record<string, unknown>) {
  return api.patch(`${getApiBase()}/customers/${cif}`, payload).then((r) => r.data);
}

/** GET /validation/ghana-card - Server-side Ghana Card validation */
export function validateGhanaCard(cardNumber: string) {
  return api
    .get(`${getApiBase()}/validation/ghana-card`, { params: { card: cardNumber } })
    .then((r) => r.data as { isValid: boolean });
}

/** GET /config/business-date - Current system business date */
export function getBusinessDate() {
  return api.get(`${getApiBase()}/config/business-date`).then((r) => r.data as { businessDate: string });
}

/** GET /transactions - Transaction history (optional account/branch filter) */
export function getTransactions(params?: { accountId?: string; branchId?: string; limit?: number }) {
  return api.get(`${getApiBase()}/transactions`, { params: params ?? {} }).then((r) => r.data);
}

/** POST /gl/accounts - Chart of accounts management */
export function createGLAccount(payload: Record<string, unknown>) {
  return api.post(`${getApiBase()}/gl/accounts`, payload).then((r) => r.data);
}

export function updateGLAccount(code: string, payload: Record<string, unknown>) {
  return api.patch(`${getApiBase()}/gl/accounts/${code}`, payload).then((r) => r.data);
}

/** GET /roles - RBAC roles */
export function getRoles() {
  return api.get(`${getApiBase()}/roles`).then((r) => r.data);
}

/** GET /staff - Staff users */
export function getStaff() {
  return api.get(`${getApiBase()}/staff`).then((r) => r.data);
}

/** POST /accounts - Create new account */
export function createAccount(payload: { cif: string; productCode: string; currency: string }) {
  return api.post(`${getApiBase()}/accounts`, payload).then((r) => r.data);
}

/** POST /loans/:id/disburse - Disburse loan */
export function disburseLoan(loanId: string, force = false) {
  return api.post(`${getApiBase()}/loans/${loanId}/disburse`, { force }).then((r) => r.data);
}

/** POST /loans/:id/repay - Repay loan */
export function repayLoan(loanId: string, payload: { accountId: string; amount: number; clientReference?: string }) {
  return api.post(`${getApiBase()}/loans/${loanId}/repay`, payload).then((r) => r.data);
}

/** GET /loans/:id/accrual - Get loan accrual snapshot */
export function getLoanAccrual(loanId: string) {
  return api.get(`${getApiBase()}/loans/${loanId}/accrual`).then((r) => r.data);
}

/** GET /loans/:id/schedule - Get loan amortization schedule */
export function getLoanSchedule(loanId: string) {
  return api.get(`${getApiBase()}/loans/${loanId}/schedule`).then((r) => r.data);
}

/** POST /loans/:id/penalty - Assess loan penalty */
export function assessLoanPenalty(loanId: string, payload: { penaltyRate: number; reason?: string; clientReference?: string }) {
  return api.post(`${getApiBase()}/loans/${loanId}/penalty`, payload).then((r) => r.data);
}

/** POST /loans/:id/classify - Classify loan and calculate provisioning */
export function classifyLoan(loanId: string) {
  return api.post(`${getApiBase()}/loans/${loanId}/classify`, {}).then((r) => r.data);
}

/** POST /fees - Assess account fee */
export function assessAccountFee(payload: { feeCode: string; amount: number; accountId: string; narration?: string; clientReference?: string }) {
  return api.post(`${getApiBase()}/fees`, payload).then((r) => r.data);
}

/** POST /gl/journal-entries - Post GL journal entry */
export function postGL(payload: { description: string; lines: Array<{ accountCode: string; debit: number; credit: number }>; postedBy: string }) {
  return api.post(`${getApiBase()}/gl/journal-entries`, payload).then((r) => r.data);
}

/** POST /roles - Create role */
export function createRole(payload: Record<string, unknown>) {
  return api.post(`${getApiBase()}/roles`, payload).then((r) => r.data);
}

/** PATCH /staff/:id/role - Update user role */
export function updateUserRole(userId: string, roleId: string) {
  return api.patch(`${getApiBase()}/staff/${userId}/role`, { roleId }).then((r) => r.data);
}

/** POST /staff - Create staff user */
export function createStaff(payload: Record<string, unknown>) {
  return api.post(`${getApiBase()}/staff`, payload).then((r) => r.data);
}

/** POST /branches - Create branch */
export function createBranch(payload: Record<string, unknown>) {
  return api.post(`${getApiBase()}/branches`, payload).then((r) => r.data);
}

/** PUT /branches/:id - Update branch */
export function updateBranch(id: string, payload: Record<string, unknown>) {
  return api.put(`${getApiBase()}/branches/${id}`, payload).then((r) => r.data);
}

/** DELETE /branches/:id - Delete branch */
export function deleteBranch(id: string) {
  return api.delete(`${getApiBase()}/branches/${id}`).then((r) => r.data);
}
