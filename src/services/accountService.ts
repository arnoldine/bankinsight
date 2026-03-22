import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import { Account } from '../../types';

interface AccountApiModel {
  id: string;
  customerId?: string | null;
  branchId?: string | null;
  type?: string | null;
  currency?: string | null;
  balance?: number;
  lienAmount?: number;
  status?: string | null;
  productCode?: string | null;
  lastTransDate?: string | null;
  createdAt?: string | null;
}

const normalizeAccountType = (value?: string | null): Account['type'] => {
  const normalized = (value || 'SAVINGS').trim().toUpperCase();
  if (normalized === 'CURRENT') return 'CURRENT';
  if (normalized === 'FIXED_DEPOSIT') return 'FIXED_DEPOSIT';
  return 'SAVINGS';
};

const normalizeAccountStatus = (value?: string | null): Account['status'] => {
  const normalized = (value || 'ACTIVE').trim().toUpperCase();
  if (normalized === 'DORMANT') return 'DORMANT';
  if (normalized === 'FROZEN' || normalized === 'INACTIVE') return 'FROZEN';
  return 'ACTIVE';
};

const normalizeCurrency = (value?: string | null): Account['currency'] => {
  return (value || 'GHS').trim().toUpperCase() === 'USD' ? 'USD' : 'GHS';
};

const mapAccount = (account: AccountApiModel): Account => ({
  id: account.id,
  cif: account.customerId || '',
  branchId: account.branchId || 'BR001',
  type: normalizeAccountType(account.type),
  currency: normalizeCurrency(account.currency),
  balance: Number(account.balance || 0),
  lienAmount: Number(account.lienAmount || 0),
  status: normalizeAccountStatus(account.status),
  productCode: account.productCode || '',
  lastTransDate: account.lastTransDate || account.createdAt || new Date().toISOString(),
});

class AccountService {
  async getAccounts(): Promise<Account[]> {
    const accounts = await httpClient.get<AccountApiModel[]>(API_ENDPOINTS.accounts.list);
    return accounts.map(mapAccount);
  }

  async createAccount(data: { customerId: string; branchId?: string; type: string; currency?: string; productCode?: string }): Promise<Account> {
    const created = await httpClient.post<AccountApiModel>(API_ENDPOINTS.accounts.create, data);
    return mapAccount(created);
  }
}

export const accountService = new AccountService();
