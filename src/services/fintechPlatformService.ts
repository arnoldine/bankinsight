const env = (import.meta as any).env;
const configuredBaseUrl = env.VITE_FINTECH_API_BASE_URL as string | undefined;
const configuredHealthUrl = env.VITE_FINTECH_API_HEALTH_URL as string | undefined;

const normalizeBaseUrl = (value: string) => value.replace(/\/$/, '');
const resolveBaseUrl = () => normalizeBaseUrl(configuredBaseUrl || 'http://localhost:5176');
const resolveHealthUrl = () => configuredHealthUrl || `${resolveBaseUrl()}/health`;
const resolveApiBaseUrl = () => `${resolveBaseUrl()}/api/v1`;

export interface FintechHealthStatus {
  status: 'Healthy' | 'Unavailable';
  checkedAt: string;
}

export interface FintechWalletSummary {
  walletId: string;
  currency: string;
  availableBalance: number;
  reservedBalance: number;
  status: string;
}

export interface FintechAlert {
  alertId: string;
  customerId: string;
  alertCode: string;
  severity: string;
  score: number;
  status: string;
  summary: string;
}

export interface FintechApprovalQueueItem {
  approvalRequestId: string;
  transferOrderId: string;
  actionCode: string;
  status: string;
  requestedBy: string;
  reason: string;
  createdAtUtc: string;
}

export interface FintechReconciliationItem {
  reconciliationItemId: string;
  reconciliationType: string;
  externalReference: string;
  internalReference: string;
  amount: number;
  currency: string;
  status: string;
  notes: string;
}

export interface FintechTransferExplorerItem {
  transferOrderId: string;
  type: string;
  channel: string;
  status: string;
  riskStatus: string;
  complianceStatus: string;
  partnerReference?: string | null;
  amount: number;
  createdBy: string;
  createdAtUtc: string;
}

export interface FintechTransferDetail {
  transferOrderId: string;
  type: string;
  channel: string;
  status: string;
  riskStatus: string;
  complianceStatus: string;
  partnerReference?: string | null;
  failureReason?: string | null;
  amount: number;
  fee: number;
  sourceWalletId: string;
}

export interface FintechJournalLine {
  ledgerAccountId: string;
  debit: number;
  credit: number;
  currency: string;
  narrative: string;
}

export interface FintechJournalEntryDetail {
  journalEntryId: string;
  reference: string;
  status: string;
  sourceModule: string;
  idempotencyKey: string;
  transferOrderId?: string | null;
  reversalOfJournalEntryId?: string | null;
  lines: FintechJournalLine[];
}

export interface FintechAuditEvent {
  auditEventId: string;
  action: string;
  entityType: string;
  entityId: string;
  actorId: string;
  createdAtUtc: string;
  beforeJson?: string | null;
  afterJson?: string | null;
}

export interface FintechOperationsWatch {
  duplicateWebhookEvents: FintechAuditEvent[];
  divergenceEvents: FintechAuditEvent[];
}

export interface FintechApprovalDecisionRequest {
  approvedBy: string;
  decision: string;
  decisionNotes: string;
}

export interface FintechTransferResponse {
  transferId: string;
  status: string;
  riskStatus: string;
  complianceStatus: string;
  providerReference?: string | null;
}

export interface FintechApprovalDecisionResult {
  approval: FintechApprovalQueueItem;
  transfer?: FintechTransferResponse | null;
}

export interface FintechManualReconciliationRequest {
  reconciliationType: string;
  externalReference: string;
  internalReference: string;
  amount: number;
  currency: string;
  notes: string;
}

export interface FintechPagedResponse<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

export interface FintechWorkspaceSnapshot {
  health: FintechHealthStatus;
  wallets: FintechWalletSummary[];
  alerts: FintechAlert[];
  approvals: FintechApprovalQueueItem[];
  reconciliationItems: FintechReconciliationItem[];
  transfers: FintechTransferExplorerItem[];
  operationsWatch: FintechOperationsWatch;
}

export interface FintechTransferInvestigation {
  transfer: FintechTransferDetail;
  journals: FintechJournalEntryDetail[];
  auditEvents: FintechAuditEvent[];
}

class FintechPlatformService {
  private readonly baseUrl = resolveBaseUrl();
  private readonly apiBaseUrl = resolveApiBaseUrl();
  private readonly healthUrl = resolveHealthUrl();

  private buildHeaders(): HeadersInit {
    const token = localStorage.getItem('auth_token');
    return token
      ? { Authorization: `Bearer ${token}` }
      : {};
  }

  private async request<T>(url: string, options?: RequestInit): Promise<T> {
    const response = await fetch(url, {
      ...options,
      headers: {
        ...(options?.body ? { 'Content-Type': 'application/json' } : {}),
        ...this.buildHeaders(),
        ...(options?.headers || {}),
      },
    });

    if (!response.ok) {
      let message = `Request failed (${response.status}).`;
      try {
        const data = await response.json();
        if (typeof data?.message === 'string' && data.message.trim()) {
          message = data.message;
        } else if (typeof data?.error === 'string' && data.error.trim()) {
          message = data.error;
        }
      } catch {
      }

      throw new Error(message);
    }

    if (response.status === 204) {
      return {} as T;
    }

    return await response.json() as T;
  }

  async getHealth(): Promise<FintechHealthStatus> {
    try {
      const response = await fetch(this.healthUrl, {
        method: 'GET',
        headers: this.buildHeaders(),
      });

      if (!response.ok) {
        throw new Error(`Health check failed (${response.status}).`);
      }

      return { status: 'Healthy', checkedAt: new Date().toISOString() };
    } catch {
      return { status: 'Unavailable', checkedAt: new Date().toISOString() };
    }
  }

  async getWallets(): Promise<FintechWalletSummary[]> {
    return this.request<FintechWalletSummary[]>(`${this.apiBaseUrl}/wallets`);
  }

  async getAlerts(): Promise<FintechAlert[]> {
    return this.request<FintechAlert[]>(`${this.apiBaseUrl}/risk/alerts`);
  }

  async getPendingApprovals(): Promise<FintechApprovalQueueItem[]> {
    return this.request<FintechApprovalQueueItem[]>(`${this.apiBaseUrl}/admin/approvals`);
  }

  async getReconciliationItems(): Promise<FintechReconciliationItem[]> {
    return this.request<FintechReconciliationItem[]>(`${this.apiBaseUrl}/reconciliation/items`);
  }

  async createReconciliationItem(request: FintechManualReconciliationRequest): Promise<FintechReconciliationItem> {
    return this.request<FintechReconciliationItem>(`${this.apiBaseUrl}/reconciliation/items`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getTransfers(pageSize = 5): Promise<FintechPagedResponse<FintechTransferExplorerItem>> {
    return this.request<FintechPagedResponse<FintechTransferExplorerItem>>(`${this.apiBaseUrl}/admin/transfers?page=1&pageSize=${pageSize}`);
  }

  async getTransfer(transferOrderId: string): Promise<FintechTransferDetail> {
    return this.request<FintechTransferDetail>(`${this.apiBaseUrl}/admin/transfers/${transferOrderId}`);
  }

  async getTransferJournals(transferOrderId: string): Promise<FintechJournalEntryDetail[]> {
    return this.request<FintechJournalEntryDetail[]>(`${this.apiBaseUrl}/admin/transfers/${transferOrderId}/journals`);
  }

  async getEntityAudit(entityType: string, entityId: string): Promise<FintechAuditEvent[]> {
    return this.request<FintechAuditEvent[]>(`${this.apiBaseUrl}/admin/audit/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`);
  }

  async searchAudit(action: string, pageSize = 5): Promise<FintechPagedResponse<FintechAuditEvent>> {
    return this.request<FintechPagedResponse<FintechAuditEvent>>(
      `${this.apiBaseUrl}/admin/audit?page=1&pageSize=${pageSize}&action=${encodeURIComponent(action)}`,
    );
  }

  async getTransferInvestigation(transferOrderId: string): Promise<FintechTransferInvestigation> {
    const [transfer, journals, auditEvents] = await Promise.all([
      this.getTransfer(transferOrderId),
      this.getTransferJournals(transferOrderId),
      this.getEntityAudit('TransferOrder', transferOrderId),
    ]);

    return { transfer, journals, auditEvents };
  }

  async decideApproval(approvalRequestId: string, request: FintechApprovalDecisionRequest): Promise<FintechApprovalDecisionResult> {
    return this.request<FintechApprovalDecisionResult>(`${this.apiBaseUrl}/admin/approvals/${approvalRequestId}/decision`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getWorkspaceSnapshot(): Promise<FintechWorkspaceSnapshot> {
    const [health, wallets, alerts, approvals, reconciliationItems, transfers, duplicateWebhookEvents, divergenceEvents] = await Promise.all([
      this.getHealth(),
      this.getWallets(),
      this.getAlerts(),
      this.getPendingApprovals(),
      this.getReconciliationItems(),
      this.getTransfers(),
      this.searchAudit('WebhookDuplicateIgnored'),
      this.searchAudit('ProviderLedgerDivergenceDetected'),
    ]);

    return {
      health,
      wallets,
      alerts,
      approvals,
      reconciliationItems,
      transfers: transfers.items,
      operationsWatch: {
        duplicateWebhookEvents: duplicateWebhookEvents.items,
        divergenceEvents: divergenceEvents.items,
      },
    };
  }

  getAdminUrl(): string {
    return (env.VITE_FINTECH_ADMIN_URL as string | undefined) || 'https://localhost:7020';
  }

  getHealthUrl(): string {
    return this.healthUrl;
  }

  getSwaggerUrl(): string {
    return (env.VITE_FINTECH_API_DOCS_URL as string | undefined) || `${this.baseUrl}/swagger`;
  }
}

export const fintechPlatformService = new FintechPlatformService();

