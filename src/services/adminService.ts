import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import { StaffUser, Role, Branch, SystemConfig, Product, AuditLog, RegulatoryChartSeedResponse } from '../../types';

export interface PrivilegeLease {
    id: string;
    staffId: string;
    permission: string;
    reason: string;
    approvedBy: string;
    approvedAt: string;
    startsAt: string;
    expiresAt: string;
    isRevoked: boolean;
    isActive: boolean;
}

export interface OrassReadiness {
    profileConfigured: boolean;
    readyForSubmission: boolean;
    submissionMode: string;
    sourceReportCode: string;
    pendingReturns: number;
    returnsReadyForSubmission: number;
    missingRequirements: string[];
    notes: string[];
    lastPreparedReturnDate?: string | null;
    lastSubmissionAt?: string | null;
}

export interface OrassQueueItem {
    id: number;
    returnType: string;
    returnDate: string;
    reportingPeriodStart: string;
    reportingPeriodEnd: string;
    submissionStatus: string;
    totalRecords: number;
    isReadyForSubmission: boolean;
    validationStatus: string;
    validationMessages: string[];
}

export interface OrassSubmissionHistoryItem {
    id: number;
    returnType: string;
    returnDate: string;
    submissionStatus: string;
    submissionDate?: string | null;
    submittedBy: string;
    bogReferenceNumber: string;
    transportStatus: string;
    acknowledgementStatus: string;
    acknowledgementReference?: string | null;
    acknowledgedAt?: string | null;
    transportMessage?: string | null;
    validationMessages: string[];
}

export interface OrassSubmissionEvidence {
    returnId: number;
    transmissionId: string;
    submissionMode: string;
    endpointUrl: string;
    transportStatus: string;
    acknowledgementStatus: string;
    acknowledgementReference?: string | null;
    submittedAt?: string | null;
    acknowledgedAt?: string | null;
    payloadHash?: string | null;
    providerStatusCode?: string | null;
    transportMessage?: string | null;
    notes: string[];
}

export interface OrassReconciliationResult {
    scannedCount: number;
    updatedCount: number;
    pendingCount: number;
    executionMode: string;
    executedAt: string;
    notes: string[];
    updatedItems: OrassSubmissionHistoryItem[];
}

class AdminService {
    // Users
    async getUsers(): Promise<StaffUser[]> {
        return httpClient.get<StaffUser[]>(API_ENDPOINTS.users.list);
    }

    async createUser(data: Partial<StaffUser>): Promise<StaffUser> {
        return httpClient.post<StaffUser>(API_ENDPOINTS.users.create, data);
    }

    async updateUser(id: string, data: Partial<StaffUser>): Promise<StaffUser> {
        return httpClient.put<StaffUser>(API_ENDPOINTS.users.update(id), data);
    }

    async updateUserRole(id: string, roleId: string): Promise<StaffUser> {
        return httpClient.put<StaffUser>(API_ENDPOINTS.users.update(id), { roleId });
    }

    async deleteUser(id: string): Promise<void> {
        return httpClient.delete<void>(API_ENDPOINTS.users.delete(id));
    }

    async resetPassword(id: string): Promise<void> {
        // In a real app we'd have a specific endpoint or use update. 
        // Creating stub using PUT for now assuming backend supports password reset via update.
        return httpClient.put<void>(API_ENDPOINTS.users.update(id), { resetPassword: true, password: 'password123' });
    }

    // Roles
    async getRoles(): Promise<Role[]> {
        return httpClient.get<Role[]>(API_ENDPOINTS.users.roles);
    }

    async createRole(data: Partial<Role>): Promise<Role> {
        // Assuming POST /users/roles exists based on standard REST.
        return httpClient.post<Role>(API_ENDPOINTS.users.roles, data);
    }

    async updateRole(id: string, data: Partial<Role>): Promise<Role> {
        // Assuming PUT /users/roles/:id exists
        return httpClient.put<Role>(`${API_ENDPOINTS.users.roles}/${id}`, data);
    }

    // Branches
    async getBranches(): Promise<Branch[]> {
        return httpClient.get<Branch[]>(API_ENDPOINTS.branches.list);
    }

    async createBranch(data: Partial<Branch>): Promise<Branch> {
        return httpClient.post<Branch>(API_ENDPOINTS.branches.create, data);
    }

    async updateBranch(id: string, data: Partial<Branch>): Promise<Branch> {
        return httpClient.put<Branch>(API_ENDPOINTS.branches.update(id), data);
    }

    async deleteBranch(id: string): Promise<void> {
        // Assuming DELETE /branches/:id
        return httpClient.delete<void>(`${API_ENDPOINTS.branches.list}/${id}`);
    }

    // System Configuration
    async getSystemConfig(): Promise<SystemConfig> {
        const items = await httpClient.get<any[]>(API_ENDPOINTS.config.get);

        const config: any = {};
        const schedulerConfig = {
            enabled: false,
            timeUtc: '23:00',
            lastRunDate: '',
        };

        for (const item of items) {
            if (item.key === 'loan.credit_bureau_required_for_approval') {
                config.loanCreditBureauRequiredForApproval = item.value === 'true';
                continue;
            }

            if (item.key === 'eod_scheduler_enabled') {
                schedulerConfig.enabled = item.value === 'true';
                continue;
            }

            if (item.key === 'eod_scheduler_time_utc') {
                schedulerConfig.timeUtc = item.value || '23:00';
                continue;
            }

            if (item.key === 'eod_scheduler_last_run_date') {
                schedulerConfig.lastRunDate = item.value || '';
                continue;
            }

            try {
                // Try JSON parsing objects, strings will fallback
                config[item.key] = JSON.parse(item.value);
            } catch {
                if (!isNaN(Number(item.value))) {
                    config[item.key] = Number(item.value);
                } else if (item.value === 'true' || item.value === 'false') {
                    config[item.key] = item.value === 'true';
                } else {
                    config[item.key] = item.value;
                }
            }
        }

        config.eodScheduler = {
            ...schedulerConfig,
            ...(config.eodScheduler || {}),
        };

        return config as SystemConfig;
    }

    async updateSystemConfig(config: SystemConfig): Promise<SystemConfig> {
        // Flatten the nested UI config shape into the backend key/value store contract.
        const items = Object.entries(config)
            .filter(([key]) => key !== 'eodScheduler')
            .map(([key, value]) => ({
                key,
                value: typeof value === 'object' ? JSON.stringify(value) : String(value)
            }));

        items.push(
            { key: 'loan.credit_bureau_required_for_approval', value: String(config.loanCreditBureauRequiredForApproval) },
            { key: 'eod_scheduler_enabled', value: String(config.eodScheduler.enabled) },
            { key: 'eod_scheduler_time_utc', value: config.eodScheduler.timeUtc },
            { key: 'eod_scheduler_last_run_date', value: config.eodScheduler.lastRunDate || '' }
        );

        return httpClient.post<SystemConfig>(API_ENDPOINTS.config.update, items);
    }

    async getOrassReadiness(): Promise<OrassReadiness> {
        return httpClient.get<OrassReadiness>(API_ENDPOINTS.orass.readiness);
    }

    async getOrassQueue(): Promise<OrassQueueItem[]> {
        return httpClient.get<OrassQueueItem[]>(API_ENDPOINTS.orass.queue);
    }

    async getOrassHistory(take: number = 20): Promise<OrassSubmissionHistoryItem[]> {
        return httpClient.get<OrassSubmissionHistoryItem[]>(`${API_ENDPOINTS.orass.history}?take=${take}`);
    }

    async submitOrassReturn(returnId: number): Promise<OrassSubmissionHistoryItem> {
        return httpClient.post<OrassSubmissionHistoryItem>(API_ENDPOINTS.orass.submit(returnId), {});
    }

    async getOrassEvidence(returnId: number): Promise<OrassSubmissionEvidence> {
        return httpClient.get<OrassSubmissionEvidence>(API_ENDPOINTS.orass.evidence(returnId));
    }

    async acknowledgeOrassReturn(returnId: number, data: {
        status: 'RECEIVED' | 'ACCEPTED' | 'REJECTED';
        acknowledgementReference?: string;
        message?: string;
    }): Promise<OrassSubmissionHistoryItem> {
        return httpClient.post<OrassSubmissionHistoryItem>(API_ENDPOINTS.orass.acknowledge(returnId), data);
    }

    async reconcileOrassAcknowledgements(): Promise<OrassReconciliationResult> {
        return httpClient.post<OrassReconciliationResult>(API_ENDPOINTS.orass.reconcile, {});
    }

    async seedRegulatoryChartOfAccounts(regionCode: string = 'GH'): Promise<RegulatoryChartSeedResponse> {
        return httpClient.post<RegulatoryChartSeedResponse>(API_ENDPOINTS.gl.seedRegulatory, { regionCode });
    }

    // Privilege leases
    async getPrivilegeLeases(staffId: string): Promise<PrivilegeLease[]> {
        return httpClient.get<PrivilegeLease[]>(API_ENDPOINTS.privilegeLeases.listByStaff(staffId));
    }

    async getMyPrivilegeLeases(): Promise<PrivilegeLease[]> {
        return httpClient.get<PrivilegeLease[]>(API_ENDPOINTS.privilegeLeases.listMine);
    }

    async createPrivilegeLease(data: {
        staffId: string;
        permission: string;
        reason: string;
        approvedBy: string;
        startsAt?: string;
        expiresAt: string;
    }): Promise<PrivilegeLease> {
        return httpClient.post<PrivilegeLease>(API_ENDPOINTS.privilegeLeases.create, data);
    }

    async revokePrivilegeLease(leaseId: string, revokedBy: string): Promise<{ message: string }> {
        return httpClient.post<{ message: string }>(API_ENDPOINTS.privilegeLeases.revoke(leaseId), { revokedBy });
    }

    // Products
    async getProducts(): Promise<Product[]> {
        return httpClient.get<Product[]>(API_ENDPOINTS.products.list);
    }

    async createProduct(data: Partial<Product>): Promise<Product> {
        return httpClient.post<Product>(API_ENDPOINTS.products.create, data);
    }

    async updateProduct(id: string, data: Partial<Product>): Promise<Product> {
        return httpClient.put<Product>(API_ENDPOINTS.products.update(id), data);
    }

    // Audit Logs
    async getAuditLogs(limit: number = 100): Promise<AuditLog[]> {
        return httpClient.get<AuditLog[]>(`${API_ENDPOINTS.audit.list}?limit=${limit}`);
    }
}
export const adminService = new AdminService();



