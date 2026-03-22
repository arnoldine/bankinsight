import type { BankingOSProcessDefinition } from './contracts';

export interface BankingOSProcessPackResponse {
  productName: string;
  version: number;
  lifecycleEnvelope: string[];
  processes: BankingOSProcessDefinition[];
}

export async function fetchBankingOSProcessPack(baseUrl = '/api/bankingos'): Promise<BankingOSProcessPackResponse> {
  const response = await fetch(`${baseUrl}/process-pack`, {
    credentials: 'include'
  });

  if (!response.ok) {
    throw new Error(`Failed to load BankingOS process pack (${response.status})`);
  }

  return response.json();
}

export async function fetchBankingOSProcess(code: string, baseUrl = '/api/bankingos'): Promise<BankingOSProcessDefinition> {
  const response = await fetch(`${baseUrl}/processes/${encodeURIComponent(code)}`, {
    credentials: 'include'
  });

  if (!response.ok) {
    throw new Error(`Failed to load BankingOS process '${code}' (${response.status})`);
  }

  return response.json();
}
