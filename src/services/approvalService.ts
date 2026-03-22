import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import { ApprovalRequest } from '../../types';

interface ApprovalApiModel {
  id: string;
  workflowId?: string | null;
  workflowName?: string | null;
  entityType: string;
  entityId: string;
  requesterId?: string | null;
  status: string;
  currentStep: number;
  createdAt: string;
  updatedAt: string;
  remarks?: string | null;
  referenceNo?: string | null;
  payloadJson?: string | null;
  loanDetails?: {
    loanId: string;
    customerId: string;
    customerName?: string | null;
    productCode?: string | null;
    productName?: string | null;
    principal: number;
    outstandingBalance?: number | null;
    rate: number;
    termMonths: number;
    collateralType?: string | null;
    collateralValue?: number | null;
    parBucket?: string | null;
    status: string;
    appliedAt?: string | null;
  } | null;
}

const mapApprovalType = (entityType?: string | null): ApprovalRequest['type'] => {
  const normalized = String(entityType || '').toUpperCase();
  if (normalized.includes('LOAN')) return 'LOAN_DISBURSEMENT';
  if (normalized.includes('USER')) return 'NEW_USER';
  if (normalized.includes('CASH') || normalized.includes('TILL')) return 'CASH_EXCEPTION';
  return 'TRANSACTION_LIMIT';
};

const mapApprovalStatus = (status?: string | null): ApprovalRequest['status'] => {
  const normalized = String(status || 'PENDING').toUpperCase();
  if (normalized === 'APPROVED') return 'APPROVED';
  if (normalized === 'REJECTED') return 'REJECTED';
  return 'PENDING';
};

const mapApproval = (approval: ApprovalApiModel): ApprovalRequest => ({
  id: approval.id,
  type: mapApprovalType(approval.entityType),
  requesterName: approval.requesterId || 'System',
  requestDate: approval.createdAt,
  description: approval.loanDetails
    ? `${approval.loanDetails.productName || approval.loanDetails.productCode || 'Loan'} for ${approval.loanDetails.customerName || approval.loanDetails.customerId}`
    : approval.workflowName
      ? `${approval.workflowName} for ${approval.entityType} ${approval.entityId}`
      : `${approval.entityType} request for ${approval.entityId}`,
  amount: approval.loanDetails?.principal,
  status: mapApprovalStatus(approval.status),
  remarks: approval.remarks || undefined,
  referenceNo: approval.referenceNo || undefined,
  payload: {
    entityType: approval.entityType,
    entityId: approval.entityId,
    workflowId: approval.workflowId || undefined,
    workflowName: approval.workflowName || undefined,
    currentStep: approval.currentStep,
    payloadJson: approval.payloadJson || undefined,
    loanDetails: approval.loanDetails ? {
      loanId: approval.loanDetails.loanId,
      customerId: approval.loanDetails.customerId,
      customerName: approval.loanDetails.customerName || undefined,
      productCode: approval.loanDetails.productCode || undefined,
      productName: approval.loanDetails.productName || undefined,
      principal: approval.loanDetails.principal,
      outstandingBalance: approval.loanDetails.outstandingBalance ?? undefined,
      rate: approval.loanDetails.rate,
      termMonths: approval.loanDetails.termMonths,
      collateralType: approval.loanDetails.collateralType || undefined,
      collateralValue: approval.loanDetails.collateralValue ?? undefined,
      parBucket: approval.loanDetails.parBucket || undefined,
      status: approval.loanDetails.status,
      appliedAt: approval.loanDetails.appliedAt || undefined,
    } : undefined,
  },
});

class ApprovalService {
  async getApprovals(): Promise<ApprovalRequest[]> {
    const approvals = await httpClient.get<ApprovalApiModel[]>(API_ENDPOINTS.approvals.list);
    return approvals.map(mapApproval);
  }

  async createApproval(request: {
    workflowId?: string | null;
    entityType: string;
    entityId: string;
    requesterId?: string | null;
    payloadJson?: string | null;
    remarks?: string | null;
    referenceNo?: string | null;
  }): Promise<void> {
    await httpClient.post(API_ENDPOINTS.approvals.create, request);
  }

  async approveApproval(id: string, workflowStep = 1): Promise<void> {
    await httpClient.put(API_ENDPOINTS.approvals.update(id), {
      status: 'APPROVED',
      currentStep: workflowStep,
    });
  }

  async rejectApproval(id: string): Promise<void> {
    await httpClient.put(API_ENDPOINTS.approvals.update(id), {
      status: 'REJECTED',
      currentStep: 0,
    });
  }
}

export const approvalService = new ApprovalService();

