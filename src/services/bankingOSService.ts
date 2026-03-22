import { httpClient } from './httpClient';

export interface BankingOSProcessStageSummary {
  stageCode: string;
  displayName: string;
  actorRole: string;
  formCode?: string;
  actions: string[];
  screen?: BankingOSStageScreenSchema | null;
}

export interface BankingOSProcessSummary {
  code: string;
  name: string;
  module: string;
  entityType: string;
  triggerType: string;
  version: number;
  status: string;
  stages: BankingOSProcessStageSummary[];
}

export interface BankingOSProcessPack {
  productName: string;
  version: number;
  lifecycleEnvelope: string[];
  processes: BankingOSProcessSummary[];
}

export interface BankingOSSeedField {
  id: string;
  label: string;
  type: string;
  required: boolean;
  options?: string[];
}

export interface BankingOSProcessCatalogItem {
  code: string;
  name: string;
  module: string;
  entityType: string;
  triggerType: string;
  version: number;
  status: string;
  isSeeded: boolean;
  isPublished: boolean;
  stageCount: number;
}

export interface BankingOSSeedForm {
  code: string;
  name: string;
  module: string;
  version: number;
  status: string;
  layout: Record<string, unknown>;
  fields: BankingOSSeedField[];
}

export interface BankingOSTheme {
  code: string;
  name: string;
  version: number;
  status: string;
  tokens: Record<string, string>;
}

export interface BankingOSThemePack {
  themes: BankingOSTheme[];
}

export interface BankingOSThemeCatalogItem {
  code: string;
  name: string;
  version: number;
  status: string;
  isSeeded: boolean;
  isPublished: boolean;
  tokenCount: number;
}

export interface BankingOSPublishBundle {
  code: string;
  name: string;
  status: string;
  releaseChannel: string;
  requiresApproval: boolean;
  processes: string[];
  forms: string[];
  themes: string[];
  notes: string;
  lastAction: string;
  lastActionBy: string;
  lastActionAtUtc: string;
}

export interface BankingOSFormCatalogItem {
  code: string;
  name: string;
  module: string;
  version: number;
  status: string;
  isSeeded: boolean;
  isPublished: boolean;
  fieldCount: number;
}

export interface BankingOSProductGroupRules {
  minMembersRequired: number;
  maxMembersAllowed: number;
  minWeeks?: number | null;
  maxWeeks?: number | null;
  requiresCompulsorySavings: boolean;
  minSavingsToLoanRatio?: number | null;
  requiresGroupApprovalMeeting: boolean;
  requiresJointLiability: boolean;
  allowTopUp: boolean;
  allowReschedule: boolean;
  maxCycleNumber?: number | null;
  cycleIncrementRulesJson?: string | null;
  defaultRepaymentFrequency: string;
  defaultInterestMethod: string;
  penaltyPolicyJson?: string | null;
  attendanceRuleJson?: string | null;
  eligibilityRuleJson?: string | null;
  meetingCollectionRuleJson?: string | null;
  allocationOrderJson?: string | null;
  accountingProfileJson?: string | null;
  disclosureTemplate?: string | null;
}

export interface BankingOSProductEligibilityRules {
  requiresKycComplete: boolean;
  blockOnSevereArrears: boolean;
  maxAllowedExposure?: number | null;
  minMembershipDays?: number | null;
  minAttendanceRate?: number | null;
  requireCreditBureauCheck: boolean;
  creditBureauProvider?: string | null;
  minimumCreditScore?: number | null;
  ruleJson?: string | null;
}

export interface BankingOSProductConfiguration {
  id: string;
  name: string;
  description?: string | null;
  type: string;
  currency: string;
  interestRate?: number | null;
  interestMethod?: string | null;
  minAmount?: number | null;
  maxAmount?: number | null;
  minTerm?: number | null;
  maxTerm?: number | null;
  defaultTerm?: number | null;
  status: string;
  lendingMethodology: string;
  isGroupLoanEnabled: boolean;
  supportsJointLiability: boolean;
  requiresCenter: boolean;
  requiresGroup: boolean;
  defaultRepaymentFrequency: string;
  allowedRepaymentFrequencies: string[];
  supportsWeeklyRepayment: boolean;
  minimumGroupSize?: number | null;
  maximumGroupSize?: number | null;
  requiresCompulsorySavings: boolean;
  minimumSavingsToLoanRatio?: number | null;
  requiresGroupApprovalMeeting: boolean;
  usesMemberLevelUnderwriting: boolean;
  usesGroupLevelApproval: boolean;
  loanCyclePolicyType?: string | null;
  maxCycleNumber?: number | null;
  graduatedCycleLimitRulesJson?: string | null;
  attendanceRuleType?: string | null;
  arrearsEligibilityRuleType?: string | null;
  groupGuaranteePolicyType?: string | null;
  meetingCollectionMode?: string | null;
  allowBatchDisbursement: boolean;
  allowMemberLevelDisbursementAdjustment: boolean;
  allowTopUpWithinGroup: boolean;
  allowRescheduleWithinGroup: boolean;
  groupPenaltyPolicy?: string | null;
  groupDelinquencyPolicy?: string | null;
  groupOfficerAssignmentMode?: string | null;
  groupRules?: BankingOSProductGroupRules | null;
  eligibilityRules?: BankingOSProductEligibilityRules | null;
}

export interface BankingOSProcessTask {
  id: string;
  status: string;
  assignedRoleCode?: string | null;
  assignedPermissionCode?: string | null;
  processInstance?: {
    id: string;
    entityType: string;
    entityId: string;
    status: string;
    processDefinitionVersion?: {
      processDefinition?: {
        code: string;
        name: string;
      };
    };
  };
  processStepDefinition?: {
    stepCode: string;
    stepName: string;
    stepType: string;
  };
}

export interface BankingOSTaskContext {
  taskId: string;
  taskStatus: string;
  stepCode: string;
  stepName: string;
  stepType: string;
  processCode: string;
  processName: string;
  entityType: string;
  entityId: string;
  stage?: BankingOSProcessStageSummary | null;
  form?: BankingOSSeedForm | null;
  allowedActions: string[];
  actions: BankingOSTaskActionDescriptor[];
  requiresClaim: boolean;
  requiredFieldIds: string[];
  validationRules: BankingOSFieldValidationRule[];
  screen?: BankingOSTaskScreenSchema | null;
  selectedProduct?: BankingOSProductLaunchOption | null;
  completionOutcome: string;
  rejectionAllowed: boolean;
}

export interface BankingOSTaskActionDescriptor {
  code: string;
  label: string;
  tone: string;
  requiresRemarks: boolean;
  isPrimary: boolean;
  isEnabled: boolean;
  disabledReason?: string | null;
}

export interface BankingOSFieldValidationRule {
  fieldId: string;
  label: string;
  required: boolean;
  message: string;
}

export interface BankingOSTaskScreenSchema {
  title: string;
  description: string;
  bannerTone: string;
  bannerMessage: string;
  sections: BankingOSTaskScreenSection[];
}

export interface BankingOSTaskScreenSection {
  id: string;
  title: string;
  kind: string;
  fieldIds: string[];
}

export interface BankingOSProductLaunchOption {
  productId: string;
  name: string;
  type: string;
  status: string;
  currency: string;
  interestRate?: number | null;
  minAmount?: number | null;
  maxAmount?: number | null;
  defaultTerm?: number | null;
  minTerm?: number | null;
  maxTerm?: number | null;
  defaultRepaymentFrequency: string;
  allowedRepaymentFrequencies: string[];
  eligibilityHints: string[];
}

export interface BankingOSLaunchContext {
  processCode: string;
  processName: string;
  entityType: string;
  primaryForm?: BankingOSSeedForm | null;
  productOptions: BankingOSProductLaunchOption[];
  validationHints: string[];
}

export interface BankingOSStageScreenSchema {
  title: string;
  description: string;
  bannerTone: string;
  bannerMessage: string;
  sections: BankingOSStageScreenSection[];
}

export interface BankingOSStageScreenSection {
  id: string;
  title: string;
  kind: string;
  fieldIds: string[];
}

class BankingOSService {
  async getProcessPack(): Promise<BankingOSProcessPack> {
    return httpClient.get<BankingOSProcessPack>('/bankingos/process-pack');
  }

  async getLaunchContext(processCode: string): Promise<BankingOSLaunchContext> {
    return httpClient.get<BankingOSLaunchContext>(`/bankingos/processes/${encodeURIComponent(processCode)}/launch-context`);
  }

  async launchProcess(processCode: string, request: { entityType: string; entityId: string; correlationId?: string; payloadJson?: string }): Promise<{ instanceId: string; status: string }> {
    return httpClient.post<{ instanceId: string; status: string }>(`/bankingos/processes/${encodeURIComponent(processCode)}/launch`, request);
  }

  async getForms(): Promise<BankingOSSeedForm[]> {
    return httpClient.get<BankingOSSeedForm[]>('/bankingos/forms');
  }

  async getProcessCatalog(): Promise<BankingOSProcessCatalogItem[]> {
    return httpClient.get<BankingOSProcessCatalogItem[]>('/bankingos/process-catalog');
  }

  async getProducts(): Promise<BankingOSProductConfiguration[]> {
    return httpClient.get<BankingOSProductConfiguration[]>('/bankingos/products');
  }

  async createProduct(product: BankingOSProductConfiguration): Promise<BankingOSProductConfiguration> {
    return httpClient.post<BankingOSProductConfiguration>('/bankingos/products', product);
  }

  async updateProduct(product: BankingOSProductConfiguration): Promise<BankingOSProductConfiguration> {
    return httpClient.put<BankingOSProductConfiguration>(`/bankingos/products/${encodeURIComponent(product.id)}`, product);
  }

  async getThemes(): Promise<BankingOSThemePack> {
    return httpClient.get<BankingOSThemePack>('/bankingos/themes');
  }

  async getThemeCatalog(): Promise<BankingOSThemeCatalogItem[]> {
    return httpClient.get<BankingOSThemeCatalogItem[]>('/bankingos/theme-catalog');
  }

  async getPublishBundles(): Promise<BankingOSPublishBundle[]> {
    return httpClient.get<BankingOSPublishBundle[]>('/bankingos/publish-bundles');
  }

  async submitPublishBundle(code: string, actor: string, notes?: string): Promise<BankingOSPublishBundle> {
    return httpClient.post<BankingOSPublishBundle>(`/bankingos/publish-bundles/${encodeURIComponent(code)}/submit`, {
      actor,
      notes
    });
  }

  async approvePublishBundle(code: string, actor: string, notes?: string): Promise<BankingOSPublishBundle> {
    return httpClient.post<BankingOSPublishBundle>(`/bankingos/publish-bundles/${encodeURIComponent(code)}/approve`, {
      actor,
      notes
    });
  }

  async rejectPublishBundle(code: string, actor: string, notes?: string): Promise<BankingOSPublishBundle> {
    return httpClient.post<BankingOSPublishBundle>(`/bankingos/publish-bundles/${encodeURIComponent(code)}/reject`, {
      actor,
      notes
    });
  }

  async promotePublishBundle(code: string, actor: string, notes?: string): Promise<BankingOSPublishBundle> {
    return httpClient.post<BankingOSPublishBundle>(`/bankingos/publish-bundles/${encodeURIComponent(code)}/promote`, {
      actor,
      notes
    });
  }

  async getFormCatalog(): Promise<BankingOSFormCatalogItem[]> {
    return httpClient.get<BankingOSFormCatalogItem[]>('/bankingos/form-catalog');
  }

  async saveFormDraft(form: BankingOSSeedForm): Promise<BankingOSSeedForm> {
    return httpClient.post<BankingOSSeedForm>('/bankingos/forms/drafts', {
      code: form.code,
      name: form.name,
      module: form.module,
      layout: form.layout,
      fields: form.fields
    });
  }

  async publishForm(code: string): Promise<BankingOSSeedForm> {
    return httpClient.post<BankingOSSeedForm>(`/bankingos/forms/${encodeURIComponent(code)}/publish`);
  }

  async saveProcessDraft(process: BankingOSProcessSummary): Promise<BankingOSProcessSummary> {
    return httpClient.post<BankingOSProcessSummary>('/bankingos/processes/drafts', {
      code: process.code,
      name: process.name,
      module: process.module,
      entityType: process.entityType,
      triggerType: process.triggerType,
      stages: process.stages
    });
  }

  async publishProcess(code: string): Promise<BankingOSProcessSummary> {
    return httpClient.post<BankingOSProcessSummary>(`/bankingos/processes/${encodeURIComponent(code)}/publish`);
  }

  async saveThemeDraft(theme: BankingOSTheme): Promise<BankingOSTheme> {
    return httpClient.post<BankingOSTheme>('/bankingos/themes/drafts', {
      code: theme.code,
      name: theme.name,
      tokens: theme.tokens
    });
  }

  async publishTheme(code: string): Promise<BankingOSTheme> {
    return httpClient.post<BankingOSTheme>(`/bankingos/themes/${encodeURIComponent(code)}/publish`);
  }

  async getMyTasks(): Promise<BankingOSProcessTask[]> {
    return httpClient.get<BankingOSProcessTask[]>('/WorkflowRuntime/tasks/my');
  }

  async getTaskContext(taskId: string): Promise<BankingOSTaskContext> {
    return httpClient.get<BankingOSTaskContext>(`/bankingos/tasks/${encodeURIComponent(taskId)}/context`);
  }

  async completeWorkbenchTask(taskId: string, payloadJson?: string, remarks?: string): Promise<void> {
    await httpClient.post(`/bankingos/tasks/${encodeURIComponent(taskId)}/complete`, {
      payloadJson,
      remarks
    });
  }

  async rejectWorkbenchTask(taskId: string, payloadJson?: string, remarks?: string): Promise<void> {
    await httpClient.post(`/bankingos/tasks/${encodeURIComponent(taskId)}/reject`, {
      payloadJson,
      remarks
    });
  }

  async getClaimableTasks(): Promise<BankingOSProcessTask[]> {
    return httpClient.get<BankingOSProcessTask[]>('/WorkflowRuntime/tasks/claimable');
  }

  async claimTask(taskId: string): Promise<void> {
    await httpClient.post(`/WorkflowRuntime/tasks/${taskId}/claim`);
  }

  async completeTask(taskId: string, outcome: string, payloadJson?: string): Promise<void> {
    await httpClient.post(`/WorkflowRuntime/tasks/${taskId}/complete`, {
      outcome,
      payloadJson
    });
  }

  async rejectTask(taskId: string, payloadJson?: string): Promise<void> {
    await httpClient.post(`/WorkflowRuntime/tasks/${taskId}/reject`, {
      outcome: 'Reject',
      payloadJson
    });
  }
}

export const bankingOSService = new BankingOSService();
