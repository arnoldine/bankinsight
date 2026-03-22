export interface BankingOSFormDefinition {
  code: string;
  name: string;
  module: string;
  status: 'Draft' | 'Published' | 'Archived';
  currentPublishedVersion?: number;
}

export interface BankingOSThemeDefinition {
  code: string;
  name: string;
  currentPublishedVersion?: number;
}

export interface BankingOSPublishBundle {
  code: string;
  name: string;
  forms: string[];
  themes: string[];
  processes: string[];
  requiresApproval: boolean;
}
