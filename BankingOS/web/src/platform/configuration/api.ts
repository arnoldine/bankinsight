import type { BankingOSFormDefinition, BankingOSThemeDefinition } from './contracts';

export interface BankingOSSeedField {
  id: string;
  label: string;
  type: string;
  required: boolean;
  options?: string[];
}

export interface BankingOSSeedForm extends BankingOSFormDefinition {
  version: number;
  layout: Record<string, unknown>;
  fields: BankingOSSeedField[];
}

export interface BankingOSThemePackResponse {
  themes: Array<BankingOSThemeDefinition & { tokens: Record<string, string> }>;
}

export async function fetchBankingOSForms(baseUrl = '/api/bankingos'): Promise<BankingOSSeedForm[]> {
  const response = await fetch(`${baseUrl}/forms`, {
    credentials: 'include'
  });

  if (!response.ok) {
    throw new Error(`Failed to load BankingOS forms (${response.status})`);
  }

  return response.json();
}

export async function fetchBankingOSThemes(baseUrl = '/api/bankingos'): Promise<BankingOSThemePackResponse> {
  const response = await fetch(`${baseUrl}/themes`, {
    credentials: 'include'
  });

  if (!response.ok) {
    throw new Error(`Failed to load BankingOS themes (${response.status})`);
  }

  return response.json();
}
