import { API_ENDPOINTS } from './apiConfig';
import { httpClient } from './httpClient';

export interface OperationsMetric {
  key: string;
  label: string;
  value: string;
  severity: string;
  subtitle?: string | null;
}

export interface OperationsWorkItem {
  id: string;
  category: string;
  severity: string;
  title: string;
  detail: string;
  routeHint?: string | null;
  amount?: number | null;
  count?: number | null;
  reference?: string | null;
}

export interface OperationsControlSummary {
  businessDate: string;
  platformStatus: string;
  metrics: OperationsMetric[];
  workItems: OperationsWorkItem[];
}

export interface Customer360Account {
  id: string;
  productCode?: string | null;
  status: string;
  currency: string;
  balance: number;
  openDate: string;
}

export interface Customer360Loan {
  id: string;
  productCode?: string | null;
  status: string;
  principal: number;
  outstandingBalance: number;
  parBucket: string;
  repaymentFrequency?: string | null;
}

export interface Customer360Investment {
  id: string;
  productCode: string;
  status: string;
  principal: number;
  rate: number;
  maturityDate?: string | null;
}

export interface Customer360Transaction {
  id: string;
  accountId: string;
  type: string;
  status: string;
  currency: string;
  amount: number;
  date: string;
  description?: string | null;
}

export interface Customer360EngagementItem {
  type: string;
  title: string;
  detail: string;
  severity: string;
  at: string;
}

export interface Customer360Response {
  profile: any;
  financialSummary: {
    activeAccountCount: number;
    activeLoanCount: number;
    activeInvestmentCount: number;
    totalDeposits90Days: number;
    totalWithdrawals90Days: number;
    totalBalances: number;
    totalOutstandingLoans: number;
    totalInvestmentBook: number;
    primaryCurrency: string;
    estimatedAnnualRevenue: number;
    relationshipOwner: string;
  };
  accounts: Customer360Account[];
  loans: Customer360Loan[];
  investments: Customer360Investment[];
  recentTransactions: Customer360Transaction[];
  engagementTimeline: Customer360EngagementItem[];
}

export interface CollectionCaseEvent {
  eventType: string;
  performedBy?: string | null;
  detail: string;
  metadataJson?: string | null;
  createdAt: string;
}

export interface CollectionCase {
  id: string;
  loanId: string;
  customerId: string;
  customerName: string;
  status: string;
  priority: string;
  recoveryStage: string;
  delinquencyDays: number;
  outstandingBalance: number;
  amountInArrears: number;
  assignedTo?: string | null;
  nextActionDate?: string | null;
  promiseToPayDate?: string | null;
  promiseToPayAmount?: number | null;
  lastContactAt?: string | null;
  lastPaymentAt?: string | null;
  nextEscalationDate?: string | null;
  notes?: string | null;
  recoveryStrategy?: string | null;
  legalStatus?: string | null;
  settlementAmount?: number | null;
  settlementExpiryDate?: string | null;
  assignedAgency?: string | null;
  repossessionStatus?: string | null;
  approvalStatus?: string | null;
  writeOffRecommendedAmount?: number | null;
  writeOffReason?: string | null;
  events: CollectionCaseEvent[];
}

export interface UpdateCollectionCaseRequest {
  status?: string;
  priority?: string;
  recoveryStage?: string;
  assignedTo?: string;
  nextActionDate?: string;
  promiseToPayDate?: string;
  promiseToPayAmount?: number;
  settlementAmount?: number;
  settlementExpiryDate?: string;
  notes?: string;
  recoveryStrategy?: string;
  legalStatus?: string;
  eventType: string;
  detail: string;
}

export interface ExecuteCollectionActionRequest {
  actionType: string;
  detail?: string;
  promiseToPayDate?: string;
  promiseToPayAmount?: number;
  settlementAmount?: number;
  settlementExpiryDate?: string;
  nextActionDate?: string;
  assignedAgency?: string;
  writeOffReason?: string;
}

export interface ProductSimulationResult {
  productId: string;
  productName: string;
  productType: string;
  amount: number;
  termMonths: number;
  annualRate: number;
  projectedInterest: number;
  projectedMaturityValue: number;
  projectedInstallment?: number | null;
  summary: string;
}

export interface ReconciliationMetric {
  key: string;
  label: string;
  value: string;
  severity: string;
}

export interface ReconciliationException {
  id: string;
  category: string;
  sourceSystem: string;
  reference: string;
  status: string;
  severity: string;
  currency: string;
  amount: number;
  ownerUserId?: string | null;
  summary: string;
  detail: string;
  detectedAt: string;
  dueAt?: string | null;
  resolvedAt?: string | null;
  retryCount: number;
  lastAttemptAt?: string | null;
  workflowStage?: string | null;
  resolutionCode?: string | null;
}

export interface SettlementInstruction {
  id: string;
  reconciliationExceptionId: string;
  instructionType: string;
  status: string;
  currency: string;
  amount: number;
  settlementAccount?: string | null;
  counterparty?: string | null;
  dueAt?: string | null;
  completedAt?: string | null;
  notes?: string | null;
}

export interface ReconciliationHubSummary {
  metrics: ReconciliationMetric[];
  exceptions: ReconciliationException[];
  settlementInstructions: SettlementInstruction[];
}

export interface CollateralRecord {
  id: string;
  loanId: string;
  customerId: string;
  customerName: string;
  collateralType: string;
  description: string;
  registeredValue: number;
  currentValuation: number;
  valuationDate?: string | null;
  valuationExpiryDate?: string | null;
  perfectionStatus: string;
  documentReference?: string | null;
  custodyLocation?: string | null;
  status: string;
}

export interface CovenantRecord {
  id: string;
  loanId: string;
  name: string;
  covenantType: string;
  status: string;
  dueDate?: string | null;
  lastReviewedAt?: string | null;
  detail: string;
}

export interface CollateralManagementSummary {
  collateralItems: CollateralRecord[];
  covenants: CovenantRecord[];
  expiringValuationsCount: number;
  overdueCovenantsCount: number;
}

export interface DeveloperPortalMetric {
  key: string;
  label: string;
  value: string;
  severity: string;
}

export interface ApiProductDefinition {
  id: string;
  name: string;
  slug: string;
  category: string;
  audience: string;
  status: string;
  version: string;
  authModel: string;
  basePath: string;
  documentationPath: string;
  rateLimitPerMinute: number;
  supportsWebhooks: boolean;
  supportsSandbox: boolean;
  scopeSummary: string;
  lastPublishedAt?: string | null;
}

export interface PartnerApplication {
  id: string;
  name: string;
  partnerName: string;
  status: string;
  environment: string;
  callbackUrl: string;
  contactEmail: string;
  apiProductIds: string[];
  sandboxKeyPreview: string;
  productionKeyPreview?: string | null;
  lastKeyRotatedAt?: string | null;
  lastActivityAt?: string | null;
  productionKeyActivatedAt?: string | null;
}

export interface WebhookSubscription {
  id: string;
  partnerApplicationId: string;
  partnerApplicationName: string;
  eventName: string;
  targetUrl: string;
  status: string;
  signingSecretPreview: string;
  lastDeliveryAt?: string | null;
  lastDeliveryStatus?: string | null;
}

export interface WebhookDeliveryLog {
  id: string;
  webhookSubscriptionId: string;
  eventName: string;
  deliveryStatus: string;
  responseCode?: number | null;
  attemptNumber: number;
  failureReason?: string | null;
  deliveredAt: string;
}

export interface WebhookEventCatalogItem {
  eventName: string;
  category: string;
  description: string;
}

export interface DeveloperPortalSummary {
  products: ApiProductDefinition[];
  partnerApplications: PartnerApplication[];
  webhookSubscriptions: WebhookSubscription[];
  deliveryLogs: WebhookDeliveryLog[];
  eventCatalog: WebhookEventCatalogItem[];
  metrics: DeveloperPortalMetric[];
}

export interface SupervisoryMetric {
  key: string;
  label: string;
  value: string;
  severity: string;
  subtitle?: string | null;
}

export interface RelationshipCustomerItem {
  customerId: string;
  customerName: string;
  segment: string;
  activeAccountCount: number;
  activeLoanCount: number;
  activeInvestmentCount: number;
  depositBalance: number;
  loanExposure: number;
  investmentBalance: number;
  estimatedRelationshipValue: number;
  estimatedAnnualRevenue: number;
  householdOrGroupLinks: number;
  openComplaintCount: number;
  riskSummary: string;
  relationshipOwnerUserId?: string | null;
  relationshipOwner: string;
  lastEngagementAt?: string | null;
}

export interface AssignableStaffItem {
  userId: string;
  name: string;
  email: string;
  branchId?: string | null;
  status: string;
}

export interface RelationshipPortfolioBreakdownItem {
  category: string;
  count: number;
  balance: number;
  contribution: number;
}

export interface RelationshipPortfolioDetail {
  customerId: string;
  customerName: string;
  segment: string;
  riskSummary: string;
  relationshipOwnerUserId?: string | null;
  relationshipOwner: string;
  depositBalance: number;
  investmentBalance: number;
  loanExposure: number;
  estimatedAnnualRevenue: number;
  estimatedRelationshipValue: number;
  lastEngagementAt?: string | null;
  openComplaintCount: number;
  householdOrGroupLinks: number;
  productBreakdown: RelationshipPortfolioBreakdownItem[];
  recentEngagements: RelationshipEngagementItem[];
}

export interface RelationshipEngagementItem {
  customerId: string;
  customerName: string;
  source: string;
  title: string;
  detail: string;
  severity: string;
  occurredAt: string;
}

export interface RelationshipBankingSummary {
  metrics: SupervisoryMetric[];
  topRelationships: RelationshipCustomerItem[];
  managerPerformance: Array<{
    relationshipOwner: string;
    customerCount: number;
    depositBalance: number;
    loanExposure: number;
    estimatedAnnualRevenue: number;
    highRiskRelationships: number;
    openComplaintCount: number;
  }>;
  recentEngagements: RelationshipEngagementItem[];
  assignableStaff: AssignableStaffItem[];
}

export interface DigitalChannelMetric {
  channelName: string;
  transactionCount: number;
  transactionVolume: number;
  percentageOfTotal: number;
}

export interface DigitalSessionRiskItem {
  sessionId: string;
  customerId: string;
  customerName: string;
  ipAddress: string;
  userAgent?: string | null;
  lastActivity: string;
  expiresAt: string;
  isActive: boolean;
  riskLabel: string;
}

export interface DigitalComplaintItem {
  complaintId: string;
  reference: string;
  customerId: string;
  customerName: string;
  category: string;
  status: string;
  ownerTeam: string;
  slaDueAt: string;
  summary: string;
}

export interface DigitalKycItem {
  kycCaseId: string;
  reference: string;
  customerId: string;
  customerName: string;
  status: string;
  reason: string;
  submittedAt: string;
  reviewerName?: string | null;
}

export interface DigitalChannelOperationsSummary {
  metrics: SupervisoryMetric[];
  channelMetrics: DigitalChannelMetric[];
  sessionRiskItems: DigitalSessionRiskItem[];
  complaintQueue: DigitalComplaintItem[];
  kycQueue: DigitalKycItem[];
}

export interface RegulatoryVarianceItem {
  reference: string;
  returnType: string;
  severity: string;
  title: string;
  detail: string;
  actionHint: string;
  resolutionStatus: string;
  ownerUserId?: string | null;
  ownerName?: string | null;
  assignedByName?: string | null;
  assignedAt?: string | null;
  resolutionNote?: string | null;
  resolvedAt?: string | null;
  updatedAt?: string | null;
  events: Array<{
    eventType: string;
    performedByUserId?: string | null;
    performedByName?: string | null;
    detail: string;
    createdAt: string;
  }>;
}

export interface RegulatoryIntelligenceSummary {
  metrics: SupervisoryMetric[];
  readiness: {
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
  };
  queue: Array<{
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
  }>;
  history: Array<{
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
  }>;
  variances: RegulatoryVarianceItem[];
}

export interface OrassReconciliationResult {
  scannedCount: number;
  updatedCount: number;
  pendingCount: number;
  executionMode: string;
  executedAt: string;
  notes: string[];
  updatedItems: Array<{
    id: number;
    returnType: string;
    acknowledgementStatus: string;
    transportStatus: string;
  }>;
}

export const platformEnhancementService = {
  getOperationsControlSummary: () => httpClient.get<OperationsControlSummary>(API_ENDPOINTS.operationsControl.summary),
  getCustomer360: (customerId: string) => httpClient.get<Customer360Response>(API_ENDPOINTS.customer360.get(customerId)),
  getCollectionCases: () => httpClient.get<CollectionCase[]>(API_ENDPOINTS.collections.cases),
  updateCollectionCase: (caseId: string, payload: UpdateCollectionCaseRequest) =>
    httpClient.put<CollectionCase>(API_ENDPOINTS.collections.updateCase(caseId), payload),
  executeCollectionAction: (caseId: string, payload: ExecuteCollectionActionRequest) =>
    httpClient.post<CollectionCase>(API_ENDPOINTS.collections.executeAction(caseId), payload),
  updateProductLifecycle: (productId: string, payload: { lifecycleStatus: string; effectiveFrom?: string | null; notes?: string | null }) =>
    httpClient.put<any>(API_ENDPOINTS.products.lifecycle(productId), payload),
  simulateProduct: (productId: string, payload: { amount: number; termMonths?: number; annualRateOverride?: number }) =>
    httpClient.post<ProductSimulationResult>(API_ENDPOINTS.products.simulate(productId), payload),
  getReconciliationSummary: () => httpClient.get<ReconciliationHubSummary>(API_ENDPOINTS.reconciliationHub.summary),
  updateReconciliationException: (id: string, payload: { status?: string; ownerUserId?: string; detail?: string; workflowStage?: string; resolutionCode?: string }) =>
    httpClient.put<ReconciliationException>(API_ENDPOINTS.reconciliationHub.updateException(id), payload),
  retryReconciliationException: (id: string, payload: { detail?: string }) =>
    httpClient.post<ReconciliationException>(API_ENDPOINTS.reconciliationHub.retryException(id), payload),
  createSettlementInstruction: (payload: { reconciliationExceptionId: string; instructionType: string; currency?: string; amount: number; settlementAccount?: string; counterparty?: string; dueAt?: string; notes?: string }) =>
    httpClient.post<SettlementInstruction>(API_ENDPOINTS.reconciliationHub.createSettlementInstruction, payload),
  getCollateralSummary: () => httpClient.get<CollateralManagementSummary>(API_ENDPOINTS.collateralManagement.summary),
  updateCollateralRecord: (id: string, payload: { currentValuation?: number; valuationDate?: string; valuationExpiryDate?: string; perfectionStatus?: string; documentReference?: string; custodyLocation?: string; status?: string }) =>
    httpClient.put<CollateralRecord>(API_ENDPOINTS.collateralManagement.updateCollateral(id), payload),
  updateCovenantRecord: (id: string, payload: { status?: string; dueDate?: string; lastReviewedAt?: string; detail?: string }) =>
    httpClient.put<CovenantRecord>(API_ENDPOINTS.collateralManagement.updateCovenant(id), payload),
  getDeveloperPortalSummary: () => httpClient.get<DeveloperPortalSummary>(API_ENDPOINTS.developerPortal.summary),
  createPartnerApplication: (payload: { name: string; partnerName: string; callbackUrl: string; contactEmail: string; apiProductIds: string[] }) =>
    httpClient.post<PartnerApplication>(API_ENDPOINTS.developerPortal.createPartnerApplication, payload),
  updatePartnerApplication: (id: string, payload: { name?: string; partnerName?: string; status?: string; environment?: string; callbackUrl?: string; contactEmail?: string; apiProductIds?: string[] }) =>
    httpClient.put<PartnerApplication>(API_ENDPOINTS.developerPortal.updatePartnerApplication(id), payload),
  rotatePartnerSandboxKey: (id: string) =>
    httpClient.post<PartnerApplication>(API_ENDPOINTS.developerPortal.rotateSandboxKey(id), {}),
  promotePartnerApplication: (id: string, payload: { environment?: string }) =>
    httpClient.post<PartnerApplication>(API_ENDPOINTS.developerPortal.promotePartnerApplication(id), payload),
  createWebhookSubscription: (payload: { partnerApplicationId: string; eventName: string; targetUrl: string }) =>
    httpClient.post<WebhookSubscription>(API_ENDPOINTS.developerPortal.createWebhookSubscription, payload),
  replayWebhook: (payload: { webhookSubscriptionId: string; eventName: string }) =>
    httpClient.post<WebhookDeliveryLog>(API_ENDPOINTS.developerPortal.replayWebhook, payload),
  getRelationshipBankingSummary: () =>
    httpClient.get<RelationshipBankingSummary>(API_ENDPOINTS.supervisory.relationshipBanking),
  getRelationshipPortfolioDetail: (customerId: string) =>
    httpClient.get<RelationshipPortfolioDetail>(API_ENDPOINTS.supervisory.relationshipPortfolio(customerId)),
  getAssignableRelationshipStaff: () =>
    httpClient.get<AssignableStaffItem[]>(API_ENDPOINTS.supervisory.relationshipStaffDirectory),
  assignRelationshipOwner: (payload: { customerId: string; ownerUserId?: string | null; assignmentNote?: string }) =>
    httpClient.post<RelationshipCustomerItem>(API_ENDPOINTS.supervisory.assignRelationshipOwner, payload),
  getDigitalChannelOperationsSummary: () =>
    httpClient.get<DigitalChannelOperationsSummary>(API_ENDPOINTS.supervisory.digitalChannelOperations),
  getRegulatoryIntelligenceSummary: () =>
    httpClient.get<RegulatoryIntelligenceSummary>(API_ENDPOINTS.supervisory.regulatoryIntelligence),
  resolveRegulatoryVariance: (payload: { reference: string; returnType: string; resolutionNote?: string }) =>
    httpClient.post<RegulatoryVarianceItem>(API_ENDPOINTS.supervisory.resolveVariance, payload),
  reopenRegulatoryVariance: (payload: { reference: string; returnType: string; resolutionNote?: string }) =>
    httpClient.post<RegulatoryVarianceItem>(API_ENDPOINTS.supervisory.reopenVariance, payload),
  assignRegulatoryVariance: (payload: { reference: string; returnType: string; ownerUserId?: string | null; assignmentNote?: string }) =>
    httpClient.post<RegulatoryVarianceItem>(API_ENDPOINTS.supervisory.assignVariance, payload),
  submitRegulatoryReturn: (returnId: number) =>
    httpClient.post<any>(`/orass/submit/${returnId}`, {}),
  reconcileRegulatoryAcknowledgements: () =>
    httpClient.post<OrassReconciliationResult>('/orass/reconcile', {}),
};
