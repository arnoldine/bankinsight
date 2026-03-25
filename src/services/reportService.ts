import { BlobDownloadResult, httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface ReportCatalogItem {
  id: number;
  reportCode: string;
  reportName: string;
  description: string;
  reportType: string;
  frequency: string;
  isActive: boolean;
  requiresApproval?: boolean;
}

export interface ReportRunDTO {
  id: number;
  reportDefinitionId: number;
  status: string;
  startedAt: string;
  completedAt?: string | null;
  fileName?: string | null;
  rowCount: number;
  format: string;
  executionTimeMs?: number | null;
}

export interface CustomerSegmentationDTO {
  asOfDate: string;
  generatedDate: string;
  segments: Array<{
    segmentName: string;
    customerCount: number;
    totalBalance: number;
    averageBalance: number;
    averageAge?: number;
    churnRate?: number;
  }>;
}

export interface ProductAnalyticsDTO {
  asOfDate: string;
  generatedDate: string;
  totalProducts: number;
  totalAccounts: number;
  totalBalance: number;
  productMetrics: Array<{
    productId: string;
    productName: string;
    productType?: string;
    accountCount: number;
    totalBalance: number;
    averageBalance?: number;
    interestRate?: number;
    revenueContribution?: number;
  }>;
}

export interface BalanceSheetDTO {
  asOfDate: string;
  generatedDate: string;
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  assets: Array<{ lineItem: string; amount: number; percentage?: number }>;
  liabilities: Array<{ lineItem: string; amount: number; percentage?: number }>;
  equity: Array<{ lineItem: string; amount: number; percentage?: number }>;
}

export interface IncomeStatementDTO {
  periodStart: string;
  periodEnd: string;
  generatedDate: string;
  totalRevenue: number;
  totalExpenses: number;
  netProfit: number;
  revenueItems: Array<{ lineItem: string; amount: number }>;
  expenseItems: Array<{ lineItem: string; amount: number }>;
}

export interface CashFlowStatementDTO {
  periodStart: string;
  periodEnd: string;
  generatedDate: string;
  operatingActivities: Array<{ activity: string; amount: number; category: string }>;
  investingActivities: Array<{ activity: string; amount: number; category: string }>;
  financingActivities: Array<{ activity: string; amount: number; category: string }>;
  netOperatingCashFlow: number;
  netInvestingCashFlow: number;
  netFinancingCashFlow: number;
  netChangeInCash: number;
}

export interface TrialBalanceDTO {
  asOfDate: string;
  generatedDate: string;
  accounts: Array<{
    accountNumber: string;
    accountName: string;
    balance: number;
    debitBalance: number;
    creditBalance: number;
  }>;
  totalDebits: number;
  totalCredits: number;
  isBalanced: boolean;
}

export interface DailyPositionReportDTO {
  reportDate: string;
  generatedAt: string;
  totalPositionGHS: number;
  totalPositionUSD: number;
  positions: Array<{
    currency: string;
    closingBalance: number;
    totalDeposits: number;
    totalWithdrawals: number;
    lastReconciliationDate?: string | null;
  }>;
}

export interface PrudentialReturnDTO {
  asOfDate: string;
  generatedDate: string;
  capitalMetrics: {
    tier1Capital: number;
    tier2Capital: number;
    totalCapital: number;
    riskWeightedAssets: number;
    capitalAdequacyRatio: number;
    tier1Ratio: number;
  };
  riskMetrics: {
    valueAtRisk: number;
    liquidityCoverageRatio: number;
    averageCurrencyExposure: number;
  };
  formulaVersion?: string;
  approvalStatus?: string;
  validationFindings?: string[];
  sourceBalances?: Array<{
    sourceType: string;
    sourceCode: string;
    description: string;
    amount: number;
    currency?: string;
  }>;
}

export interface LargeExposureReportDTO {
  reportDate: string;
  generatedDate: string;
  totalLargeExposures: number;
  capitalBase?: number;
  reportingThreshold?: number;
  formulaVersion?: string;
  validationFindings?: string[];
  sourceBalances?: Array<{
    sourceType: string;
    sourceCode: string;
    description: string;
    amount: number;
    currency?: string;
  }>;
  largeExposures: Array<{
    customerId: string;
    customerName: string;
    customerType: string;
    totalExposure: number;
    percentageOfCapital: number;
    exposureCategory: string;
    breachesReportingThreshold?: boolean;
    activeFacilityCount?: number;
  }>;
}

export interface RegulatoryReturnDTO {
  id: number;
  returnType: string;
  returnDate: string;
  submissionStatus: string;
  submissionDate?: string | null;
  bogReferenceNumber?: string | null;
  totalRecords: number;
  createdAt: string;
  validationStatus: 'VALID' | 'WARNING' | 'ERROR';
  validationErrors: string[];
  requiresApproval: boolean;
  isReadyForSubmission: boolean;
}

export interface CashControlAlertDTO {
  alertType: string;
  severity: string;
  scopeType: string;
  scopeId: string;
  scopeName: string;
  message: string;
  thresholdAmount?: number;
  actualAmount?: number;
  observedAt: string;
}

export interface VaultCashPositionDTO {
  branchId: string;
  branchCode: string;
  branchName: string;
  currency: string;
  vaultCash: number;
  minBalance: number;
  maxHoldingLimit: number;
  tillCash: number;
  totalOperationalCash: number;
  glCashBalance: number;
  glVariance: number;
  lastCountDate?: string | null;
  status: string;
}

export interface BranchCashSummaryDTO {
  branchId: string;
  branchCode: string;
  branchName: string;
  currency: string;
  vaultCash: number;
  tellerTillCash: number;
  totalOperationalCash: number;
  pendingCashInTransitOut: number;
  pendingCashInTransitIn: number;
  openTillCount: number;
  varianceTillCount: number;
  staleTransferCount: number;
  glCashBalance: number;
  reconciliationVariance: number;
  reconciliationStatus: string;
}

export interface CashReconciliationSummaryDTO {
  currency: string;
  generatedAt: string;
  totalVaultCash: number;
  totalTillCash: number;
  totalOperationalCash: number;
  totalGlCashBalance: number;
  totalVariance: number;
  branchesOutOfBalance: number;
  tillsOverLimit: number;
  tillsWithVariance: number;
  staleCashInTransitItems: number;
  branches: BranchCashSummaryDTO[];
  alerts: CashControlAlertDTO[];
}

export interface CashTransitItemDTO {
  transferId: string;
  fromBranchId: string;
  fromBranchName: string;
  toBranchId: string;
  toBranchName: string;
  currency: string;
  amount: number;
  status: string;
  transitStage: string;
  reference?: string | null;
  narration?: string | null;
  createdAt: string;
  approvedAt?: string | null;
  completedAt?: string | null;
  hoursOpen: number;
  isStale: boolean;
}

export interface CashIncidentDTO {
  id: string;
  branchId: string;
  branchName: string;
  storeType: string;
  storeId: string;
  incidentType: string;
  currency: string;
  amount: number;
  status: string;
  reference?: string | null;
  narration?: string | null;
  reportedBy?: string | null;
  reportedByName?: string | null;
  resolvedBy?: string | null;
  resolvedByName?: string | null;
  reportedAt: string;
  resolvedAt?: string | null;
}


export interface EnterpriseReportParameterSchema {
  name: string;
  label: string;
  type: string;
  required: boolean;
  defaultValue?: string | null;
  placeholder?: string | null;
  options: string[];
}

export interface EnterpriseReportCatalogItem {
  reportCode: string;
  reportName: string;
  category: string;
  subCategory: string;
  description: string;
  applicableInstitutionTypes: string[];
  requiredPermissions: string[];
  dataSource: string;
  parameterSchema: EnterpriseReportParameterSchema[];
  defaultSort?: string | null;
  defaultColumns: string[];
  exportFormats: string[];
  isRegulatory: boolean;
  requiresApprovalBeforeFinalExport: boolean;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  version: string;
  isActive: boolean;
  isFavorite: boolean;
  supportsBranchScope: boolean;
  supportsHeadOfficeScope: boolean;
}

export interface EnterpriseReportExecutionRequest {
  parameters: Record<string, string>;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
}

export interface EnterpriseReportSummaryMetric {
  label: string;
  value: string;
  helper?: string | null;
}

export interface EnterpriseReportExecutionResponse {
  reportCode: string;
  reportName: string;
  category: string;
  subCategory: string;
  runId?: number | null;
  generatedAt: string;
  columns: string[];
  rows: Array<Record<string, unknown>>;
  totalRows: number;
  page: number;
  pageSize: number;
  summary: EnterpriseReportSummaryMetric[];
  appliedFilters: Record<string, string>;
  validationMessages: string[];
  isMasked: boolean;
}

export type EnterpriseReportDownloadResult = BlobDownloadResult;

export interface EnterpriseReportHistoryItem {
  runId?: number | null;
  reportCode: string;
  reportName: string;
  status: string;
  format: string;
  fileName?: string | null;
  rowCount: number;
  startedAt: string;
  completedAt?: string | null;
  executionTimeMs?: number | null;
  generatedBy: string;
  actionType: string;
}

export interface ReportFavoriteItem {
  reportCode: string;
  createdAt: string;
}

export interface ReportFilterPresetItem {
  id: string;
  reportCode: string;
  presetName: string;
  parameters: Record<string, string>;
  createdAt: string;
  updatedAt: string;
}

export interface CrbDataQualityDashboardDTO {
  totalChecks: number;
  failedChecks: number;
  missingMandatoryConsumerFields: number;
  missingMandatoryBusinessFields: number;
  pendingSubmissionReadinessItems: number;
  rejectedRecords: number;
  resubmissionCandidates: number;
  breakdown: EnterpriseReportSummaryMetric[];
}
class ReportService {
  async getReportCatalog(): Promise<ReportCatalogItem[]> {
    return httpClient.get<ReportCatalogItem[]>(API_ENDPOINTS.reports.catalog);
  }

  async getCustomerSegmentation(asOfDate: string): Promise<CustomerSegmentationDTO> {
    return httpClient.get<CustomerSegmentationDTO>(
      `${API_ENDPOINTS.reports.customerSegmentation}?asOfDate=${asOfDate}`
    );
  }

  async getProductAnalytics(asOfDate: string): Promise<ProductAnalyticsDTO> {
    return httpClient.get<ProductAnalyticsDTO>(
      `${API_ENDPOINTS.reports.productAnalytics}?asOfDate=${asOfDate}`
    );
  }

  async getBalanceSheet(asOfDate: string): Promise<BalanceSheetDTO> {
    return httpClient.get<BalanceSheetDTO>(
      `${API_ENDPOINTS.reports.balanceSheet}?asOfDate=${asOfDate}`
    );
  }

  async getIncomeStatement(periodStart: string, periodEnd: string): Promise<IncomeStatementDTO> {
    return httpClient.get<IncomeStatementDTO>(
      `${API_ENDPOINTS.reports.incomeStatement}?periodStart=${periodStart}&periodEnd=${periodEnd}`
    );
  }

  async getCashFlowStatement(periodStart: string, periodEnd: string): Promise<CashFlowStatementDTO> {
    return httpClient.get<CashFlowStatementDTO>(
      `${API_ENDPOINTS.reports.cashFlow}?periodStart=${periodStart}&periodEnd=${periodEnd}`
    );
  }

  async getTrialBalance(asOfDate: string): Promise<TrialBalanceDTO> {
    return httpClient.get<TrialBalanceDTO>(
      `${API_ENDPOINTS.reports.trialBalance}?asOfDate=${asOfDate}`
    );
  }

  async getDailyPositionReport(reportDate: string): Promise<DailyPositionReportDTO> {
    return httpClient.get<DailyPositionReportDTO>(
      `${API_ENDPOINTS.reports.dailyPosition}?reportDate=${reportDate}`
    );
  }

  async getRegulatoryReturns(returnType?: string): Promise<RegulatoryReturnDTO[]> {
    const url = returnType
      ? `${API_ENDPOINTS.reports.regulatoryReturns}?returnType=${returnType}`
      : API_ENDPOINTS.reports.regulatoryReturns;
    return httpClient.get<RegulatoryReturnDTO[]>(url);
  }

  async getReportHistory(reportId: number): Promise<ReportRunDTO[]> {
    return httpClient.get<ReportRunDTO[]>(API_ENDPOINTS.reports.history(String(reportId)));
  }

  async approveRegulatoryReturn(returnId: number): Promise<RegulatoryReturnDTO> {
    return httpClient.post<RegulatoryReturnDTO>(API_ENDPOINTS.reports.approveReturn(String(returnId)), {});
  }

  async rejectRegulatoryReturn(returnId: number, reason: string): Promise<RegulatoryReturnDTO> {
    return httpClient.post<RegulatoryReturnDTO>(API_ENDPOINTS.reports.rejectReturn(String(returnId)), { reason });
  }

  async submitRegulatoryReturn(returnId: number): Promise<RegulatoryReturnDTO> {
    return httpClient.post<RegulatoryReturnDTO>(API_ENDPOINTS.reports.submitReturn(String(returnId)), {});
  }

  async generateReport(reportId: number, parameters: Record<string, string>, format = 'JSON'): Promise<ReportRunDTO> {
    return httpClient.post<ReportRunDTO>(API_ENDPOINTS.reports.generate, {
      reportId,
      parameters,
      format,
    });
  }

  async getPrudentialReturn(asOfDate: string): Promise<PrudentialReturnDTO> {
    return httpClient.get<PrudentialReturnDTO>(
      `${API_ENDPOINTS.reports.prudentialReturn}?asOfDate=${asOfDate}`
    );
  }

  async getLargeExposureReport(asOfDate: string): Promise<LargeExposureReportDTO> {
    return httpClient.get<LargeExposureReportDTO>(
      `${API_ENDPOINTS.reports.largeExposure}?asOfDate=${asOfDate}`
    );
  }

  async getVaultCashPosition(branchId?: string, currency = 'GHS'): Promise<VaultCashPositionDTO[]> {
    const params = new URLSearchParams();
    if (branchId) {
      params.append('branchId', branchId);
    }
    params.append('currency', currency);
    return httpClient.get<VaultCashPositionDTO[]>(
      `${API_ENDPOINTS.cashControl.vaultCashPosition}?${params.toString()}`
    );
  }

  async getBranchCashSummary(branchId?: string, currency = 'GHS'): Promise<BranchCashSummaryDTO[]> {
    const params = new URLSearchParams();
    if (branchId) {
      params.append('branchId', branchId);
    }
    params.append('currency', currency);
    return httpClient.get<BranchCashSummaryDTO[]>(
      `${API_ENDPOINTS.cashControl.branchCashSummary}?${params.toString()}`
    );
  }

  async getCashReconciliation(currency = 'GHS'): Promise<CashReconciliationSummaryDTO> {
    return httpClient.get<CashReconciliationSummaryDTO>(
      `${API_ENDPOINTS.cashControl.reconciliation}?currency=${currency}`
    );
  }

  async getCashTransitItems(currency = 'GHS'): Promise<CashTransitItemDTO[]> {
    return httpClient.get<CashTransitItemDTO[]>(
      `${API_ENDPOINTS.cashControl.transitItems}?currency=${currency}`
    );
  }

  async getCashIncidents(branchId?: string, status?: string): Promise<CashIncidentDTO[]> {
    const params = new URLSearchParams();
    if (branchId) {
      params.append('branchId', branchId);
    }
    if (status) {
      params.append('status', status);
    }
    const suffix = params.toString();
    return httpClient.get<CashIncidentDTO[]>(
      suffix ? `${API_ENDPOINTS.cashIncidents.list}?${suffix}` : API_ENDPOINTS.cashIncidents.list
    );
  }

  async resolveCashIncident(incidentId: string, resolutionNote: string): Promise<CashIncidentDTO> {
    return httpClient.post<CashIncidentDTO>(API_ENDPOINTS.cashIncidents.resolve(incidentId), { resolutionNote });
  }
  async getEnterpriseCatalog(): Promise<EnterpriseReportCatalogItem[]> {
    return httpClient.get<EnterpriseReportCatalogItem[]>(API_ENDPOINTS.enterpriseReports.catalog);
  }

  async executeEnterpriseReport(reportCode: string, request: EnterpriseReportExecutionRequest): Promise<EnterpriseReportExecutionResponse> {
    return httpClient.post<EnterpriseReportExecutionResponse>(API_ENDPOINTS.enterpriseReports.execute(reportCode), request);
  }

  async exportEnterpriseReport(reportCode: string, format: string, request: EnterpriseReportExecutionRequest): Promise<EnterpriseReportDownloadResult> {
    return httpClient.downloadBlob(API_ENDPOINTS.enterpriseReports.export(reportCode, format), 'POST', request);
  }

  async getEnterpriseHistory(): Promise<EnterpriseReportHistoryItem[]> {
    return httpClient.get<EnterpriseReportHistoryItem[]>(API_ENDPOINTS.enterpriseReports.history);
  }

  async getEnterpriseFavorites(): Promise<ReportFavoriteItem[]> {
    return httpClient.get<ReportFavoriteItem[]>(API_ENDPOINTS.enterpriseReports.favorites);
  }

  async addEnterpriseFavorite(reportCode: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.enterpriseReports.favorite(reportCode), {});
  }

  async removeEnterpriseFavorite(reportCode: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.enterpriseReports.favorite(reportCode));
  }

  async getEnterprisePresets(reportCode: string): Promise<ReportFilterPresetItem[]> {
    return httpClient.get<ReportFilterPresetItem[]>(API_ENDPOINTS.enterpriseReports.presets(reportCode));
  }

  async saveEnterprisePreset(reportCode: string, presetName: string, parameters: Record<string, string>): Promise<ReportFilterPresetItem> {
    return httpClient.post<ReportFilterPresetItem>(API_ENDPOINTS.enterpriseReports.presets(reportCode), { presetName, parameters });
  }

  async deleteEnterprisePreset(presetId: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.enterpriseReports.presetItem(presetId));
  }

  async getCrbDataQualityDashboard(): Promise<CrbDataQualityDashboardDTO> {
    return httpClient.get<CrbDataQualityDashboardDTO>(API_ENDPOINTS.enterpriseReports.crbDataQuality);
  }
}

export const reportService = new ReportService();





