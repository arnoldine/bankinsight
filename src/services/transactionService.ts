import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import { Transaction } from '../../types';

interface TransactionApiModel {
  id: string;
  accountId?: string | null;
  type?: string | null;
  amount?: number;
  narration?: string | null;
  date?: string | null;
  tellerId?: string | null;
  status?: string | null;
  reference?: string | null;
}

const normalizeType = (value?: string | null): Transaction['type'] => {
  const normalized = (value || 'DEPOSIT').trim().toUpperCase();
  if (normalized === 'WITHDRAWAL') return 'WITHDRAWAL';
  if (normalized === 'TRANSFER') return 'TRANSFER';
  if (normalized === 'LOAN_REPAYMENT') return 'LOAN_REPAYMENT';
  return 'DEPOSIT';
};

const normalizeStatus = (value?: string | null): Transaction['status'] => {
  const normalized = (value || 'POSTED').trim().toUpperCase();
  if (normalized === 'PENDING' || normalized === 'PENDING_APPROVAL') return 'PENDING';
  if (normalized === 'REJECTED' || normalized === 'FAILED') return 'REJECTED';
  return 'POSTED';
};

const mapTransaction = (transaction: TransactionApiModel): Transaction => ({
  id: transaction.id,
  accountId: transaction.accountId || '',
  type: normalizeType(transaction.type),
  amount: Number(transaction.amount || 0),
  narration: transaction.narration || '',
  date: transaction.date || new Date().toISOString(),
  tellerId: transaction.tellerId || '',
  status: normalizeStatus(transaction.status),
  reference: transaction.reference || '',
});

class TransactionService {
  async getTransactions(): Promise<Transaction[]> {
    const transactions = await httpClient.get<TransactionApiModel[]>(API_ENDPOINTS.transactions.list);
    return transactions.map(mapTransaction);
  }

  async createTransaction(data: {
    accountId: string;
    type: Transaction['type'];
    amount: number;
    narration?: string;
    tellerId: string;
    clientReference?: string;
  }): Promise<Transaction> {
    const created = await httpClient.post<TransactionApiModel>(API_ENDPOINTS.transactions.list, data);
    return mapTransaction(created);
  }
}

export const transactionService = new TransactionService();
