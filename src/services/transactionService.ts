import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';
import { BulkPaymentBatch, BulkPaymentBatchItem, ChequeBookInventory, ChequeBookLeaf, ChequeClearingItem, Transaction } from '../../types';

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

interface BulkPaymentItemApiModel {
  id: string;
  accountId?: string | null;
  transactionType?: string | null;
  amount?: number;
  narration?: string | null;
  tellerId?: string | null;
  clientReference?: string | null;
  status?: string | null;
  postedTransactionId?: string | null;
  errorMessage?: string | null;
  processedAt?: string | null;
}

interface BulkPaymentBatchApiModel {
  id: string;
  batchReference?: string | null;
  status?: string | null;
  currency?: string | null;
  narration?: string | null;
  totalAmount?: number;
  processedAmount?: number;
  itemCount?: number;
  processedCount?: number;
  failedCount?: number;
  createdAt?: string | null;
  processedAt?: string | null;
  items?: BulkPaymentItemApiModel[] | null;
}

interface ChequeItemApiModel {
  id: string;
  accountId?: string | null;
  transactionType?: string | null;
  chequeNumber?: string | null;
  drawerName?: string | null;
  drawerAccountNumber?: string | null;
  presentingBankCode?: string | null;
  draweeBankCode?: string | null;
  clearingChannel?: string | null;
  bogRegulatoryClass?: string | null;
  isOtherBankCheque?: boolean | null;
  amount?: number;
  currency?: string | null;
  status?: string | null;
  holdDays?: number;
  lodgedAt?: string | null;
  clearingDate?: string | null;
  clearedAt?: string | null;
  postedTransactionId?: string | null;
  returnReason?: string | null;
  failureReason?: string | null;
  narration?: string | null;
}

interface ChequeBookLeafApiModel {
  id: string;
  serialNumber?: number;
  chequeNumber?: string | null;
  status?: string | null;
  accountId?: string | null;
  usedTransactionId?: string | null;
  usedAt?: string | null;
  cancelReason?: string | null;
}

interface ChequeBookInventoryApiModel {
  id: string;
  bookReference?: string | null;
  branchId?: string | null;
  seriesPrefix?: string | null;
  startSerialNumber?: number;
  endSerialNumber?: number;
  leafCount?: number;
  availableLeafCount?: number;
  usedLeafCount?: number;
  cancelledLeafCount?: number;
  status?: string | null;
  accountId?: string | null;
  customerId?: string | null;
  issuedAt?: string | null;
  issuedBy?: string | null;
  remarks?: string | null;
  createdAt?: string | null;
  leaves?: ChequeBookLeafApiModel[] | null;
}

type NormalizedTransactionType = Transaction['type'];
type BulkStatus = BulkPaymentBatch['status'];
type ChequeStatus = ChequeClearingItem['status'];
type ChequeBookStatus = ChequeBookInventory['status'];

const normalizeTransactionType = (value?: string | null): NormalizedTransactionType => {
  const normalized = (value || 'DEPOSIT').trim().toUpperCase();
  if (normalized === 'WITHDRAWAL') return 'WITHDRAWAL';
  if (normalized === 'TRANSFER') return 'TRANSFER';
  if (normalized === 'LOAN_REPAYMENT') return 'LOAN_REPAYMENT';
  return 'DEPOSIT';
};

const normalizeTransactionStatus = (value?: string | null): Transaction['status'] => {
  const normalized = (value || 'POSTED').trim().toUpperCase();
  if (normalized === 'PENDING' || normalized === 'PENDING_APPROVAL') return 'PENDING';
  if (normalized === 'REJECTED' || normalized === 'FAILED') return 'REJECTED';
  return 'POSTED';
};

const normalizeBulkStatus = (value?: string | null): BulkStatus => {
  const normalized = (value || 'PROCESSING').trim().toUpperCase();
  if (normalized === 'COMPLETED') return 'COMPLETED';
  if (normalized === 'FAILED') return 'FAILED';
  if (normalized === 'PARTIAL') return 'PARTIAL';
  if (normalized === 'POSTED') return 'POSTED';
  if (normalized === 'PENDING') return 'PENDING';
  return 'PROCESSING';
};

const normalizeChequeStatus = (value?: string | null): ChequeStatus => {
  const normalized = (value || 'LODGED').trim().toUpperCase();
  if (normalized === 'PENDING_CLEARING') return 'PENDING_CLEARING';
  if (normalized === 'CLEARED') return 'CLEARED';
  if (normalized === 'PAID') return 'PAID';
  if (normalized === 'RETURNED') return 'RETURNED';
  if (normalized === 'FAILED') return 'FAILED';
  return 'LODGED';
};

const normalizeChequeBookStatus = (value?: string | null): ChequeBookStatus => {
  const normalized = (value || 'IN_STOCK').trim().toUpperCase();
  if (normalized === 'ISSUED') return 'ISSUED';
  if (normalized === 'ACTIVE') return 'ACTIVE';
  if (normalized === 'EXHAUSTED') return 'EXHAUSTED';
  return 'IN_STOCK';
};

const mapTransaction = (transaction: TransactionApiModel): Transaction => ({
  id: transaction.id,
  accountId: transaction.accountId || '',
  type: normalizeTransactionType(transaction.type),
  amount: Number(transaction.amount || 0),
  narration: transaction.narration || '',
  date: transaction.date || new Date().toISOString(),
  tellerId: transaction.tellerId || '',
  status: normalizeTransactionStatus(transaction.status),
  reference: transaction.reference || '',
});

const mapBulkItem = (item: BulkPaymentItemApiModel): BulkPaymentBatchItem => ({
  id: item.id,
  accountId: item.accountId || '',
  transactionType: normalizeTransactionType(item.transactionType),
  amount: Number(item.amount || 0),
  narration: item.narration || '',
  tellerId: item.tellerId || '',
  clientReference: item.clientReference || '',
  status: normalizeBulkStatus(item.status),
  postedTransactionId: item.postedTransactionId || '',
  errorMessage: item.errorMessage || '',
  processedAt: item.processedAt || undefined,
});

const mapBulkBatch = (batch: BulkPaymentBatchApiModel): BulkPaymentBatch => ({
  id: batch.id,
  batchReference: batch.batchReference || '',
  status: normalizeBulkStatus(batch.status),
  currency: batch.currency || 'GHS',
  narration: batch.narration || '',
  totalAmount: Number(batch.totalAmount || 0),
  processedAmount: Number(batch.processedAmount || 0),
  itemCount: Number(batch.itemCount || 0),
  processedCount: Number(batch.processedCount || 0),
  failedCount: Number(batch.failedCount || 0),
  createdAt: batch.createdAt || new Date().toISOString(),
  processedAt: batch.processedAt || undefined,
  items: (batch.items || []).map(mapBulkItem),
});

const mapChequeItem = (item: ChequeItemApiModel): ChequeClearingItem => ({
  id: item.id,
  accountId: item.accountId || '',
  transactionType: normalizeTransactionType(item.transactionType) as ChequeClearingItem['transactionType'],
  chequeNumber: item.chequeNumber || '',
  drawerName: item.drawerName || '',
  drawerAccountNumber: item.drawerAccountNumber || '',
  presentingBankCode: item.presentingBankCode || '',
  draweeBankCode: item.draweeBankCode || '',
  clearingChannel: item.clearingChannel || '',
  bogRegulatoryClass: item.bogRegulatoryClass || '',
  isOtherBankCheque: Boolean(item.isOtherBankCheque),
  amount: Number(item.amount || 0),
  currency: item.currency || 'GHS',
  status: normalizeChequeStatus(item.status),
  holdDays: Number(item.holdDays || 0),
  lodgedAt: item.lodgedAt || new Date().toISOString(),
  clearingDate: item.clearingDate || '',
  clearedAt: item.clearedAt || undefined,
  postedTransactionId: item.postedTransactionId || '',
  returnReason: item.returnReason || '',
  failureReason: item.failureReason || '',
  narration: item.narration || '',
});

const mapChequeBookLeaf = (leaf: ChequeBookLeafApiModel): ChequeBookLeaf => ({
  id: leaf.id,
  serialNumber: Number(leaf.serialNumber || 0),
  chequeNumber: leaf.chequeNumber || '',
  status: ((leaf.status || 'AVAILABLE').trim().toUpperCase() as ChequeBookLeaf['status']),
  accountId: leaf.accountId || '',
  usedTransactionId: leaf.usedTransactionId || '',
  usedAt: leaf.usedAt || undefined,
  cancelReason: leaf.cancelReason || '',
});

const mapChequeBook = (book: ChequeBookInventoryApiModel): ChequeBookInventory => ({
  id: book.id,
  bookReference: book.bookReference || '',
  branchId: book.branchId || '',
  seriesPrefix: book.seriesPrefix || '',
  startSerialNumber: Number(book.startSerialNumber || 0),
  endSerialNumber: Number(book.endSerialNumber || 0),
  leafCount: Number(book.leafCount || 0),
  availableLeafCount: Number(book.availableLeafCount || 0),
  usedLeafCount: Number(book.usedLeafCount || 0),
  cancelledLeafCount: Number(book.cancelledLeafCount || 0),
  status: normalizeChequeBookStatus(book.status),
  accountId: book.accountId || '',
  customerId: book.customerId || '',
  issuedAt: book.issuedAt || undefined,
  issuedBy: book.issuedBy || '',
  remarks: book.remarks || '',
  createdAt: book.createdAt || new Date().toISOString(),
  leaves: (book.leaves || []).map(mapChequeBookLeaf),
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

  async getBulkPaymentBatches(): Promise<BulkPaymentBatch[]> {
    const batches = await httpClient.get<BulkPaymentBatchApiModel[]>(API_ENDPOINTS.payments.bulk);
    return batches.map(mapBulkBatch);
  }

  async createBulkPaymentBatch(data: {
    currency: string;
    narration?: string;
    submittedBy?: string;
    items: Array<{
      accountId: string;
      transactionType: Transaction['type'];
      amount: number;
      narration?: string;
      tellerId?: string;
      clientReference?: string;
    }>;
  }): Promise<BulkPaymentBatch> {
    const created = await httpClient.post<BulkPaymentBatchApiModel>(API_ENDPOINTS.payments.bulk, data);
    return mapBulkBatch(created);
  }

  async getChequeItems(): Promise<ChequeClearingItem[]> {
    const cheques = await httpClient.get<ChequeItemApiModel[]>(API_ENDPOINTS.payments.cheques);
    return cheques.map(mapChequeItem);
  }

  async lodgeChequeDeposit(data: {
    accountId: string;
    chequeNumber: string;
    amount: number;
    currency: string;
    drawerName?: string;
    drawerAccountNumber?: string;
    presentingBankCode: string;
    draweeBankCode: string;
    isOtherBankCheque: boolean;
    clearingChannel: string;
    bogRegulatoryClass: string;
    tellerId?: string;
    narration?: string;
  }): Promise<ChequeClearingItem> {
    const created = await httpClient.post<ChequeItemApiModel>(API_ENDPOINTS.payments.chequeDeposits, data);
    return mapChequeItem(created);
  }

  async processChequeWithdrawal(data: {
    accountId: string;
    chequeNumber: string;
    amount: number;
    currency: string;
    tellerId: string;
    narration?: string;
  }): Promise<ChequeClearingItem> {
    const created = await httpClient.post<ChequeItemApiModel>(API_ENDPOINTS.payments.chequeWithdrawals, data);
    return mapChequeItem(created);
  }

  async returnCheque(itemId: string, reason: string): Promise<ChequeClearingItem> {
    const updated = await httpClient.post<ChequeItemApiModel>(API_ENDPOINTS.payments.chequeReturn(itemId), { reason });
    return mapChequeItem(updated);
  }

  async getChequeBooks(accountId?: string): Promise<ChequeBookInventory[]> {
    const endpoint = accountId
      ? `${API_ENDPOINTS.payments.chequeBooks}?accountId=${encodeURIComponent(accountId)}`
      : API_ENDPOINTS.payments.chequeBooks;
    const books = await httpClient.get<ChequeBookInventoryApiModel[]>(endpoint);
    return books.map(mapChequeBook);
  }

  async createChequeBookStock(data: {
    branchId: string;
    seriesPrefix: string;
    startSerialNumber: number;
    leafCount: number;
    remarks?: string;
  }): Promise<ChequeBookInventory> {
    const created = await httpClient.post<ChequeBookInventoryApiModel>(API_ENDPOINTS.payments.chequeBookStock, data);
    return mapChequeBook(created);
  }

  async issueChequeBook(bookId: string, data: { accountId: string; issuedBy?: string; remarks?: string }): Promise<ChequeBookInventory> {
    const updated = await httpClient.post<ChequeBookInventoryApiModel>(API_ENDPOINTS.payments.chequeBookIssue(bookId), data);
    return mapChequeBook(updated);
  }
}

export const transactionService = new TransactionService();
