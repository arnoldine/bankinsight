export type LifecycleEnvelope =
  | 'Draft'
  | 'Submitted'
  | 'Validation Pending'
  | 'Under Review'
  | 'Approved'
  | 'Rejected'
  | 'Returned for Correction'
  | 'Ready for Execution'
  | 'Executed'
  | 'Posted'
  | 'Completed'
  | 'Exception'
  | 'Reversed'
  | 'Archived';

export interface BankingOSActionDefinition {
  actionCode: string;
  displayLabel: string;
  permissionCode: string;
  requiresApproval: boolean;
  outcomeMapping: Record<string, string>;
}

export interface BankingOSFormBinding {
  formCode: string;
  version?: number;
  mode: 'entry' | 'review' | 'summary';
}

export interface BankingOSScreenSchema {
  layout: 'single-column' | 'two-column' | 'review-workbench' | 'queue-detail';
  regions: Array<'header' | 'timeline' | 'form' | 'summary' | 'documents' | 'checklist' | 'actions' | 'history'>;
  badges?: string[];
}

export interface BankingOSStageDefinition {
  stageCode: string;
  displayName: string;
  actorRole: string;
  lifecycleState: LifecycleEnvelope;
  formBinding?: BankingOSFormBinding;
  screenSchema: BankingOSScreenSchema;
  actions: BankingOSActionDefinition[];
}

export interface BankingOSProcessDefinition {
  code: string;
  name: string;
  module: string;
  entityType: string;
  triggerType: 'Manual' | 'Event' | 'Api';
  stages: BankingOSStageDefinition[];
}

export interface BankingOSQueueCard {
  instanceId: string;
  processCode: string;
  stageCode: string;
  primaryLabel: string;
  secondaryLabel?: string;
  assigneeLabel?: string;
  riskFlags?: string[];
  availableActions: BankingOSActionDefinition[];
}
