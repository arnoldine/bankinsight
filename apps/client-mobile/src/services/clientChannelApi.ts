import { apiClient, resolveApiUrl } from "./apiClient";

export interface ClientBootstrap {
  identity: {
    userId: string;
    name: string;
    email: string;
    role: string;
    permissions: string[];
  };
  linkedCustomer?: {
    id: string;
    name: string;
    email?: string | null;
    phone?: string | null;
    kycLevel?: string | null;
    riskRating?: string | null;
  } | null;
  warnings: string[];
}

export interface ClientProfile {
  id: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  digitalAddress?: string | null;
  kycLevel?: string | null;
  riskRating?: string | null;
  ghanaCard?: string | null;
  profilePhoto?: ClientMediaAsset | null;
  signature?: ClientMediaAsset | null;
  idCardFront?: ClientMediaAsset | null;
  idCardBack?: ClientMediaAsset | null;
  mediaAssets?: ClientMediaAsset[];
}

export interface ClientMediaAsset {
  id: string;
  mediaType: string;
  mediaSide?: string | null;
  fileName: string;
  contentType: string;
  previewUrl: string;
  status: string;
  fileSizeBytes?: number | null;
  uploadedBy?: string | null;
  uploadedAt: string;
}

export interface ClientKycChecklistItem {
  key: string;
  label: string;
  isSatisfied: boolean;
  detail: string;
}

export interface ClientKycReadiness {
  isReadyForAccountOpening: boolean;
  isReadyForLoanOrigination: boolean;
  missingRequirements: string[];
  checklist: ClientKycChecklistItem[];
}

export interface ClientKycCaseEvent {
  id: string;
  eventType: string;
  title: string;
  description: string;
  actorName?: string | null;
  createdAt: string;
}

export interface ClientKycCase {
  id: string;
  reference: string;
  status: string;
  reason: string;
  summary: string;
  submittedAt: string;
  reviewedAt?: string | null;
  reviewerName?: string | null;
  decisionNote?: string | null;
  events: ClientKycCaseEvent[];
}

export interface ClientKycOverview {
  customerId: string;
  kycLevel: string;
  readiness: ClientKycReadiness;
  cases: ClientKycCase[];
}

export interface ClientAccount {
  id: string;
  type: string;
  currency: string;
  balance: number;
  lienAmount: number;
  status: string;
  productCode?: string | null;
  lastTransDate?: string | null;
}

export interface ClientSession {
  id: string;
  ipAddress: string;
  userAgent?: string | null;
  createdAt: string;
  expiresAt: string;
  lastActivity: string;
  isActive: boolean;
}

export interface ClientComplaintEvent {
  id: string;
  eventType: string;
  title: string;
  description: string;
  visibility: string;
  actorName?: string | null;
  createdAt: string;
}

export interface ClientComplaint {
  id: string;
  reference: string;
  category: string;
  summary: string;
  details?: string;
  status: string;
  ownerTeam: string;
  createdAt: string;
  updatedAt: string;
  slaDueAt: string;
  closedAt?: string | null;
  events?: ClientComplaintEvent[];
  attachments?: ClientComplaintAttachment[];
}

export interface ClientComplaintAttachment {
  id: string;
  fileName: string;
  contentType: string;
  contentUrl?: string | null;
  status: string;
  uploadedAt: string;
}

export interface ClientStatementSummary {
  statementId: string;
  accountId: string;
  periodLabel: string;
  year: number;
  month: number;
  entryCount: number;
  totalDebits: number;
  totalCredits: number;
  generatedAt: string;
}

export interface ClientStatementExport {
  statementId: string;
  accountId: string;
  periodLabel: string;
  fileName: string;
  contentType: string;
  exportedAt: string;
  checksumSha256: string;
  lineCount: number;
  contentBase64: string;
}

export interface ClientBankingOverview {
  totalVisibleBalance: number;
  activeAccountCount: number;
  activeStandingOrderCount: number;
  activeLoanCount: number;
  activeInvestmentCount: number;
  totalLoanExposure: number;
  totalInvestmentBalance: number;
}

export interface ClientMerchant {
  code: string;
  name: string;
  category: string;
  settlementType: string;
  currency: string;
  destinationAccountId?: string | null;
  merchantKind: string;
  merchantProfileId?: string | null;
  settlementCustomerId?: string | null;
  acceptsQrPayments: boolean;
  qrScheme?: string | null;
}

export interface ClientMerchantAcceptanceEligibility {
  canEnroll: boolean;
  customerId: string;
  customerType: string;
  businessName: string;
  reason?: string | null;
  eligibleSettlementAccounts: ClientAccount[];
}

export interface ClientMerchantProfile {
  id: string;
  customerId: string;
  merchantCode: string;
  displayName: string;
  category: string;
  settlementAccountId: string;
  settlementAccountLabel: string;
  currency: string;
  status: string;
  qrScheme: string;
  qrPayload: string;
  acceptsAppPayments: boolean;
  ghQrReady: boolean;
  futureScheme: string;
  createdAt: string;
  lastPaymentAt?: string | null;
}

export interface ClientQrPaymentPreview {
  merchantCode: string;
  merchantName: string;
  category: string;
  currency: string;
  qrScheme: string;
  ghQrReady: boolean;
  suggestedAmount?: number | null;
  destinationAccountId: string;
  merchantProfileId: string;
}

export interface ClientTransferResult {
  transactionId: string;
  reference: string;
  narration: string;
  amount: number;
  appliedFees: number;
  netAmount: number;
  newBalance: number;
  status: string;
  message: string;
}

export interface ClientStandingOrder {
  id: string;
  sourceAccountId: string;
  instructionType: string;
  merchantCode?: string | null;
  merchantName?: string | null;
  destinationAccountId?: string | null;
  amount: number;
  currency: string;
  frequency: string;
  narration: string;
  startDate: string;
  nextRunAt: string;
  endDate?: string | null;
  lastRunAt?: string | null;
  status: string;
}

export interface ClientFixedDeposit {
  id: string;
  accountId: string;
  principal: number;
  rate: number;
  tenureDays: number;
  startDate: string;
  maturityDate: string;
  currency: string;
  status: string;
  maturityValue: number;
}

export interface ClientLoanProduct {
  id: string;
  code: string;
  name: string;
  productType: string;
  repaymentFrequency: string;
  termInPeriods: number;
  annualInterestRate: number;
  minAmount: number;
  maxAmount: number;
}

export interface ClientLoanSummary {
  id: string;
  productCode?: string | null;
  productName?: string | null;
  principal: number;
  rate: number;
  termMonths: number;
  status: string;
  outstandingBalance?: number | null;
  servicingAccountId?: string | null;
  repaymentFrequency?: string | null;
  disbursementDate?: string | null;
  parBucket: string;
}

export interface ClientLoanScheduleLine {
  period: number;
  dueDate: string;
  principal: number;
  interest: number;
  total: number;
  balance: number;
  status: string;
  paidDate?: string | null;
}

export interface ClientLoanStatement {
  loanId: string;
  customerId: string;
  principal: number;
  outstandingBalance: number;
  totalInterestPaid: number;
  totalPenaltyPaid: number;
  status: string;
  schedule: ClientLoanScheduleLine[];
}

export interface UpdateClientProfileRequest {
  name?: string;
  email?: string;
  phone?: string;
  digitalAddress?: string;
  stepUpToken: string;
}

export interface SubmitClientKycRefreshRequest {
  reason: string;
  summary: string;
  stepUpToken: string;
}

export interface UploadClientProfileMediaRequest {
  mediaType: string;
  mediaSide?: string;
  fileName: string;
  contentType: string;
  dataUrl: string;
  stepUpToken: string;
}

export interface CreateClientComplaintRequest {
  category: string;
  summary: string;
  details: string;
}

export interface ReopenClientComplaintRequest {
  reason: string;
}

export interface CreateClientInternalTransferRequest {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  narration: string;
  stepUpToken: string;
}

export interface CreateClientMerchantPaymentRequest {
  merchantCode: string;
  sourceAccountId: string;
  amount: number;
  narration?: string;
  stepUpToken: string;
}

export interface CreateClientMerchantProfileRequest {
  settlementAccountId: string;
  displayName: string;
  category: string;
  stepUpToken: string;
}

export interface ResolveClientQrPaymentRequest {
  qrPayload: string;
}

export interface CreateClientQrPaymentRequest {
  qrPayload: string;
  sourceAccountId: string;
  amount: number;
  narration?: string;
  stepUpToken: string;
}

export interface CreateClientStandingOrderRequest {
  sourceAccountId: string;
  instructionType: string;
  merchantCode?: string;
  destinationAccountId?: string;
  amount: number;
  frequency: string;
  narration: string;
  startDate?: string;
  endDate?: string;
  stepUpToken: string;
}

export interface CreateClientFixedDepositRequest {
  sourceAccountId: string;
  principal: number;
  rate: number;
  tenureDays: number;
  currency: string;
  stepUpToken: string;
}

export interface CreateClientLoanApplicationRequest {
  loanProductId: string;
  principal: number;
  servicingAccountId?: string;
  stepUpToken: string;
}

export async function getBootstrap(): Promise<ClientBootstrap> {
  return apiClient.get<ClientBootstrap>("/client-channel/bootstrap");
}

export async function getClientAccounts(): Promise<ClientAccount[]> {
  return apiClient.get<ClientAccount[]>("/client-channel/accounts");
}

export async function getClientBankingOverview(): Promise<ClientBankingOverview> {
  return apiClient.get<ClientBankingOverview>("/client-channel/banking/overview");
}

export async function getClientMerchants(): Promise<ClientMerchant[]> {
  return apiClient.get<ClientMerchant[]>("/client-channel/banking/merchants");
}

export async function getClientMerchantAcceptanceEligibility(): Promise<ClientMerchantAcceptanceEligibility> {
  return apiClient.get<ClientMerchantAcceptanceEligibility>("/client-channel/banking/merchant-acceptance/eligibility");
}

export async function getClientMerchantProfiles(): Promise<ClientMerchantProfile[]> {
  return apiClient.get<ClientMerchantProfile[]>("/client-channel/banking/merchant-acceptance/profiles");
}

export async function createClientMerchantProfile(payload: CreateClientMerchantProfileRequest): Promise<ClientMerchantProfile> {
  return apiClient.post<ClientMerchantProfile>("/client-channel/banking/merchant-acceptance/profiles", payload);
}

export async function createClientInternalTransfer(payload: CreateClientInternalTransferRequest): Promise<ClientTransferResult> {
  return apiClient.post<ClientTransferResult>("/client-channel/banking/transfers/internal", payload);
}

export async function createClientMerchantPayment(payload: CreateClientMerchantPaymentRequest): Promise<ClientTransferResult> {
  return apiClient.post<ClientTransferResult>("/client-channel/banking/payments/merchants", payload);
}

export async function resolveClientQrPayment(payload: ResolveClientQrPaymentRequest): Promise<ClientQrPaymentPreview> {
  return apiClient.post<ClientQrPaymentPreview>("/client-channel/banking/payments/qr/resolve", payload);
}

export async function createClientQrPayment(payload: CreateClientQrPaymentRequest): Promise<ClientTransferResult> {
  return apiClient.post<ClientTransferResult>("/client-channel/banking/payments/qr", payload);
}

export async function getClientStandingOrders(): Promise<ClientStandingOrder[]> {
  return apiClient.get<ClientStandingOrder[]>("/client-channel/banking/standing-orders");
}

export async function createClientStandingOrder(payload: CreateClientStandingOrderRequest): Promise<ClientStandingOrder> {
  return apiClient.post<ClientStandingOrder>("/client-channel/banking/standing-orders", payload);
}

export async function updateClientStandingOrderStatus(id: string, status: string): Promise<ClientStandingOrder> {
  return apiClient.post<ClientStandingOrder>(`/client-channel/banking/standing-orders/${id}/status`, { status });
}

export async function getClientFixedDeposits(): Promise<ClientFixedDeposit[]> {
  return apiClient.get<ClientFixedDeposit[]>("/client-channel/banking/investments");
}

export async function createClientFixedDeposit(payload: CreateClientFixedDepositRequest): Promise<ClientFixedDeposit> {
  return apiClient.post<ClientFixedDeposit>("/client-channel/banking/investments", payload);
}

export async function getClientLoanProducts(): Promise<ClientLoanProduct[]> {
  return apiClient.get<ClientLoanProduct[]>("/client-channel/banking/loan-products");
}

export async function getClientLoans(): Promise<ClientLoanSummary[]> {
  return apiClient.get<ClientLoanSummary[]>("/client-channel/banking/loans");
}

export async function createClientLoanApplication(payload: CreateClientLoanApplicationRequest): Promise<ClientLoanSummary> {
  return apiClient.post<ClientLoanSummary>("/client-channel/banking/loans/apply", payload);
}

export async function getClientLoanSchedule(loanId: string): Promise<ClientLoanScheduleLine[]> {
  return apiClient.get<ClientLoanScheduleLine[]>(`/client-channel/banking/loans/${loanId}/schedule`);
}

export async function getClientLoanStatement(loanId: string): Promise<ClientLoanStatement> {
  return apiClient.get<ClientLoanStatement>(`/client-channel/banking/loans/${loanId}/statement`);
}

export async function getClientProfile(): Promise<ClientProfile> {
  const profile = await apiClient.get<ClientProfile>("/client-channel/profile");
  return {
    ...profile,
    mediaAssets: profile.mediaAssets?.map(mapMediaAsset),
    profilePhoto: profile.profilePhoto ? mapMediaAsset(profile.profilePhoto) : profile.profilePhoto,
    signature: profile.signature ? mapMediaAsset(profile.signature) : profile.signature,
    idCardFront: profile.idCardFront ? mapMediaAsset(profile.idCardFront) : profile.idCardFront,
    idCardBack: profile.idCardBack ? mapMediaAsset(profile.idCardBack) : profile.idCardBack
  };
}

export async function getClientKycOverview(): Promise<ClientKycOverview> {
  return apiClient.get<ClientKycOverview>("/client-channel/kyc");
}

export async function updateClientProfile(payload: UpdateClientProfileRequest): Promise<ClientProfile> {
  return apiClient.put<ClientProfile>("/client-channel/profile", payload);
}

export async function submitClientKycRefresh(payload: SubmitClientKycRefreshRequest): Promise<ClientKycCase> {
  return apiClient.post<ClientKycCase>("/client-channel/kyc/refresh", payload);
}

export async function uploadClientProfileMedia(payload: UploadClientProfileMediaRequest): Promise<ClientMediaAsset> {
  const asset = await apiClient.post<ClientMediaAsset>("/client-channel/profile/media", payload);
  return mapMediaAsset(asset);
}

export async function getClientSessions(): Promise<ClientSession[]> {
  return apiClient.get<ClientSession[]>("/client-channel/sessions");
}

export async function getClientStatements(): Promise<ClientStatementSummary[]> {
  return apiClient.get<ClientStatementSummary[]>("/client-channel/statements");
}

export async function exportClientStatement(accountId: string, year: number, month: number): Promise<ClientStatementExport> {
  return apiClient.get<ClientStatementExport>(`/client-channel/statements/${accountId}/export?year=${year}&month=${month}&format=csv`);
}

export async function getClientComplaints(): Promise<ClientComplaint[]> {
  return apiClient.get<ClientComplaint[]>("/client-channel/complaints");
}

export async function createClientComplaint(payload: CreateClientComplaintRequest): Promise<ClientComplaint> {
  return apiClient.post<ClientComplaint>("/client-channel/complaints", payload);
}

export async function getClientComplaint(complaintId: string): Promise<ClientComplaint> {
  const complaint = await apiClient.get<ClientComplaint>(`/client-channel/complaints/${complaintId}`);
  return mapComplaint(complaint);
}

export async function reopenClientComplaint(complaintId: string, payload: ReopenClientComplaintRequest): Promise<ClientComplaint> {
  const complaint = await apiClient.post<ClientComplaint>(`/client-channel/complaints/${complaintId}/reopen`, payload);
  return mapComplaint(complaint);
}

function mapMediaAsset(asset: ClientMediaAsset): ClientMediaAsset {
  return {
    ...asset,
    previewUrl: resolveApiUrl(asset.previewUrl) ?? asset.previewUrl
  };
}

function mapComplaint(complaint: ClientComplaint): ClientComplaint {
  return {
    ...complaint,
    attachments: complaint.attachments?.map((attachment) => ({
      ...attachment,
      contentUrl: resolveApiUrl(attachment.contentUrl)
    }))
  };
}
