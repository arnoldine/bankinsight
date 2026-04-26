import { apiClient } from "./apiClient";

export interface AccountSummary {
  id: string;
  customerId?: string | null;
  branchId?: string | null;
  type?: string | null;
  currency?: string | null;
  balance?: number;
  status?: string | null;
}

export async function getAccounts(): Promise<AccountSummary[]> {
  return apiClient.get<AccountSummary[]>("/accounts");
}
