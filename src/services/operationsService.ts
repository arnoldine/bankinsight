import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface EodStatusResponse {
  businessDate: string;
  mode: string;
  status: string;
  pendingTransactions: number;
  draftJournalEntries: number;
  activeLoans: number;
  manualSteps: string[];
  schedulerEnabled: boolean;
  schedulerTimeUtc: string;
  lastSchedulerRunDate?: string | null;
  nextScheduledRunUtc?: string | null;
}

export interface EodStepResultResponse {
  stepId: string;
  status: 'SUCCESS' | 'WARNING' | 'ERROR';
  message: string;
  businessDate: string;
  details?: unknown;
}

class OperationsService {
  async getEodStatus(): Promise<EodStatusResponse> {
    return httpClient.get<EodStatusResponse>(API_ENDPOINTS.operations.eodStatus);
  }

  async runEodStep(stepId: string, businessDate?: string): Promise<EodStepResultResponse> {
    return httpClient.post<EodStepResultResponse>(API_ENDPOINTS.operations.runEodStep, {
      stepId,
      businessDate: businessDate || undefined,
    });
  }
}

export const operationsService = new OperationsService();
