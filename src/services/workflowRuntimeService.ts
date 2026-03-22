import { API_ENDPOINTS } from './apiConfig';
import { httpClient } from './httpClient';

export interface StartWorkflowProcessRequest {
  entityType: string;
  entityId: string;
  correlationId?: string;
  payloadJson?: string;
}

export interface StartWorkflowProcessResponse {
  instanceId: string;
  status: string;
}

class WorkflowRuntimeService {
  async startProcess(request: StartWorkflowProcessRequest, processCode?: string): Promise<StartWorkflowProcessResponse> {
    return httpClient.post<StartWorkflowProcessResponse>(
      API_ENDPOINTS.workflowRuntime.start(processCode),
      request
    );
  }
}

export const workflowRuntimeService = new WorkflowRuntimeService();
