import { API_ENDPOINTS } from './apiConfig';
import { httpClient } from './httpClient';

export type ClientKycDecision = 'UNDER_REVIEW' | 'APPROVED' | 'REJECTED';

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
  customerId: string;
  customerName: string;
  status: string;
  reason: string;
  summary: string;
  submittedAt: string;
  reviewedAt?: string | null;
  reviewerName?: string | null;
  decisionNote?: string | null;
  events: ClientKycCaseEvent[];
}

class ClientKycOpsService {
  async getQueue(status?: string): Promise<ClientKycCase[]> {
    const query = status && status !== 'ALL'
      ? `${API_ENDPOINTS.clientKycOps.queue}?status=${encodeURIComponent(status)}`
      : API_ENDPOINTS.clientKycOps.queue;

    return httpClient.get<ClientKycCase[]>(query);
  }

  async reviewCase(
    kycCaseId: string,
    payload: {
      decision: ClientKycDecision;
      note: string;
    },
  ): Promise<ClientKycCase> {
    return httpClient.post<ClientKycCase>(API_ENDPOINTS.clientKycOps.review(kycCaseId), payload);
  }
}

export const clientKycOpsService = new ClientKycOpsService();
