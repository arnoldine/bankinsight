import React, { useEffect, useMemo, useState } from 'react';
import { BulkPaymentBatch, ChequeBookInventory, ChequeClearingItem, Transaction } from '../types';
import { ArrowDown, ArrowUp, ArrowUpDown, BadgeAlert, CircleDollarSign, Clock3, Landmark, RefreshCw, Search, WalletCards, X } from 'lucide-react';

interface TransactionExplorerProps {
  transactions: Transaction[];
  bulkBatches?: BulkPaymentBatch[];
  chequeItems?: ChequeClearingItem[];
  chequeBooks?: ChequeBookInventory[];
  onCreateBulkPaymentBatch?: (payload: {
    currency: string;
    narration?: string;
    items: Array<{
      accountId: string;
      transactionType: Transaction['type'];
      amount: number;
      narration?: string;
      tellerId?: string;
      clientReference?: string;
    }>;
  }) => Promise<void>;
  onReturnCheque?: (itemId: string, reason: string) => Promise<void>;
  onRefreshPayments?: () => Promise<void>;
  onCreateChequeBookStock?: (payload: {
    branchId: string;
    seriesPrefix: string;
    startSerialNumber: number;
    leafCount: number;
    remarks?: string;
  }) => Promise<void>;
  onIssueChequeBook?: (bookId: string, payload: { accountId: string; remarks?: string }) => Promise<void>;
}

type SortField = 'date' | 'amount' | 'id' | 'accountId';
type SortDirection = 'asc' | 'desc';
type PaymentTab = 'transactions' | 'bulk' | 'cheques';
type BulkDraftItem = { accountId: string; transactionType: Transaction['type']; amount: string; narration: string; tellerId: string };

const emptyBulkItem = (): BulkDraftItem => ({ accountId: '', transactionType: 'DEPOSIT', amount: '', narration: '', tellerId: '' });

const formatMoney = (value: number, currency = 'GHS') =>
  new Intl.NumberFormat('en-GH', { style: 'currency', currency, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value || 0);

const statusTone = (status: string) => {
  const normalized = status.toUpperCase();
  if (['POSTED', 'COMPLETED', 'CLEARED', 'PAID'].includes(normalized)) return 'bg-emerald-50 text-emerald-700 border-emerald-200';
  if (['FAILED', 'REJECTED', 'RETURNED'].includes(normalized)) return 'bg-rose-50 text-rose-700 border-rose-200';
  if (['PARTIAL', 'PENDING', 'PENDING_CLEARING', 'LODGED', 'PROCESSING'].includes(normalized)) return 'bg-amber-50 text-amber-700 border-amber-200';
  return 'bg-slate-100 text-slate-700 border-slate-200';
};

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
      <div className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-500">{label}</div>
      <div className="mt-1 text-sm font-semibold text-slate-900">{value}</div>
    </div>
  );
}

const TransactionExplorer: React.FC<TransactionExplorerProps> = ({
  transactions,
  bulkBatches = [],
  chequeItems = [],
  chequeBooks = [],
  onCreateBulkPaymentBatch,
  onReturnCheque,
  onRefreshPayments,
  onCreateChequeBookStock,
  onIssueChequeBook,
}) => {
  const [activeTab, setActiveTab] = useState<PaymentTab>('transactions');
  const [filters, setFilters] = useState({ accountId: '', tellerId: '', narration: '', type: 'ALL', status: 'ALL', startDate: '', endDate: '', minAmount: '', maxAmount: '' });
  const [sort, setSort] = useState<{ field: SortField; direction: SortDirection }>({ field: 'date', direction: 'desc' });
  const [lastUpdated, setLastUpdated] = useState(new Date());
  const [bulkNarration, setBulkNarration] = useState('');
  const [bulkCurrency, setBulkCurrency] = useState('GHS');
  const [bulkItems, setBulkItems] = useState<BulkDraftItem[]>([emptyBulkItem()]);
  const [bulkSubmitting, setBulkSubmitting] = useState(false);
  const [bulkMessage, setBulkMessage] = useState<string | null>(null);
  const [bulkError, setBulkError] = useState<string | null>(null);
  const [selectedBatchId, setSelectedBatchId] = useState<string | null>(null);
  const [returnReasonByCheque, setReturnReasonByCheque] = useState<Record<string, string>>({});
  const [submittingReturnId, setSubmittingReturnId] = useState<string | null>(null);
  const [chequeMessage, setChequeMessage] = useState<string | null>(null);
  const [chequeError, setChequeError] = useState<string | null>(null);
  const [chequeBookDraft, setChequeBookDraft] = useState({ branchId: 'BR001', seriesPrefix: 'GH', startSerialNumber: '', leafCount: '25', remarks: '' });
  const [issueAccountByBook, setIssueAccountByBook] = useState<Record<string, string>>({});
  const [chequeBookBusy, setChequeBookBusy] = useState<string | null>(null);

  useEffect(() => setLastUpdated(new Date()), [transactions, bulkBatches, chequeItems]);

  const handleSort = (field: SortField) => setSort((prev) => ({ field, direction: prev.field === field && prev.direction === 'desc' ? 'asc' : 'desc' }));

  const filteredTransactions = useMemo(() => {
    return transactions.filter((tx) => {
      const matchesAccount = tx.accountId.toLowerCase().includes(filters.accountId.toLowerCase());
      const matchesTeller = tx.tellerId.toLowerCase().includes(filters.tellerId.toLowerCase());
      const matchesNarration = tx.narration.toLowerCase().includes(filters.narration.toLowerCase());
      const matchesType = filters.type === 'ALL' || tx.type === filters.type;
      const matchesStatus = filters.status === 'ALL' || tx.status === filters.status;
      const txDate = new Date(tx.date).setHours(0, 0, 0, 0);
      const start = filters.startDate ? new Date(filters.startDate).setHours(0, 0, 0, 0) : null;
      const end = filters.endDate ? new Date(filters.endDate).setHours(0, 0, 0, 0) : null;
      const matchesDate = (!start || txDate >= start) && (!end || txDate <= end);
      const amount = tx.amount;
      const min = filters.minAmount ? parseFloat(filters.minAmount) : null;
      const max = filters.maxAmount ? parseFloat(filters.maxAmount) : null;
      const matchesAmount = (!min || amount >= min) && (!max || amount <= max);
      return matchesAccount && matchesTeller && matchesNarration && matchesType && matchesStatus && matchesDate && matchesAmount;
    }).sort((a, b) => {
      const dir = sort.direction === 'asc' ? 1 : -1;
      if (sort.field === 'date') return (new Date(a.date).getTime() - new Date(b.date).getTime()) * dir;
      if (sort.field === 'amount') return (a.amount - b.amount) * dir;
      if (sort.field === 'id') return a.id.localeCompare(b.id) * dir;
      return a.accountId.localeCompare(b.accountId) * dir;
    });
  }, [transactions, filters, sort]);

  const selectedBatch = useMemo(() => bulkBatches.find((batch) => batch.id === selectedBatchId) || bulkBatches[0] || null, [bulkBatches, selectedBatchId]);
  const paymentMetrics = useMemo(() => {
    const pendingCheques = chequeItems.filter((item) => ['LODGED', 'PENDING_CLEARING'].includes(item.status)).length;
    const failedBulkLines = bulkBatches.reduce((sum, batch) => sum + Number(batch.failedCount || 0), 0);
    const completedBatches = bulkBatches.filter((batch) => batch.status === 'COMPLETED').length;
    return [
      { label: 'Posted transactions', value: transactions.length.toLocaleString(), helper: `${formatMoney(transactions.reduce((sum, item) => sum + item.amount, 0))} booked`, icon: <CircleDollarSign className="h-5 w-5 text-blue-600" /> },
      { label: 'Bulk batches', value: bulkBatches.length.toLocaleString(), helper: `${completedBatches} completed`, icon: <WalletCards className="h-5 w-5 text-violet-600" /> },
      { label: 'Cheque queue', value: chequeItems.length.toLocaleString(), helper: `${pendingCheques} awaiting clearing`, icon: <Landmark className="h-5 w-5 text-emerald-600" /> },
      { label: 'Cheque books', value: chequeBooks.length.toLocaleString(), helper: `${chequeBooks.filter((book) => book.status === 'IN_STOCK').length} in stock`, icon: <BadgeAlert className="h-5 w-5 text-amber-600" /> },
    ];
  }, [transactions, bulkBatches, chequeItems, chequeBooks]);

  const clearFilters = () => setFilters({ accountId: '', tellerId: '', narration: '', type: 'ALL', status: 'ALL', startDate: '', endDate: '', minAmount: '', maxAmount: '' });
  const updateBulkItem = (index: number, patch: Partial<BulkDraftItem>) => setBulkItems((current) => current.map((item, itemIndex) => (itemIndex === index ? { ...item, ...patch } : item)));
  const addBulkItem = () => setBulkItems((current) => [...current, emptyBulkItem()]);
  const removeBulkItem = (index: number) => setBulkItems((current) => (current.length === 1 ? current : current.filter((_, itemIndex) => itemIndex !== index)));
  const resetBulkForm = () => { setBulkNarration(''); setBulkCurrency('GHS'); setBulkItems([emptyBulkItem()]); };

  const submitBulkBatch = async () => {
    if (!onCreateBulkPaymentBatch) return;
    const normalizedItems = bulkItems.map((item) => ({
      accountId: item.accountId.trim(),
      transactionType: item.transactionType,
      amount: Number(item.amount),
      narration: item.narration.trim(),
      tellerId: item.tellerId.trim(),
      clientReference: item.accountId.trim() ? `${item.transactionType}-${item.accountId.trim()}-${Date.now()}` : '',
    })).filter((item) => item.accountId && item.amount > 0);
    if (normalizedItems.length === 0) {
      setBulkError('Add at least one valid payment line before submitting the batch.');
      setBulkMessage(null);
      return;
    }
    setBulkSubmitting(true);
    setBulkError(null);
    setBulkMessage(null);
    try {
      await onCreateBulkPaymentBatch({ currency: bulkCurrency, narration: bulkNarration.trim(), items: normalizedItems });
      setBulkMessage(`Bulk batch submitted with ${normalizedItems.length} line${normalizedItems.length === 1 ? '' : 's'}.`);
      resetBulkForm();
    } catch (error: any) {
      setBulkError(error?.message || 'Bulk payment batch could not be submitted.');
    } finally {
      setBulkSubmitting(false);
    }
  };

  const handleChequeReturn = async (item: ChequeClearingItem) => {
    if (!onReturnCheque) return;
    const reason = (returnReasonByCheque[item.id] || '').trim();
    if (!reason) {
      setChequeError('Enter a return reason before sending the cheque back to the queue.');
      setChequeMessage(null);
      return;
    }
    setSubmittingReturnId(item.id);
    setChequeError(null);
    setChequeMessage(null);
    try {
      await onReturnCheque(item.id, reason);
      setChequeMessage(`Cheque ${item.chequeNumber} was returned successfully.`);
      setReturnReasonByCheque((current) => ({ ...current, [item.id]: '' }));
    } catch (error: any) {
      setChequeError(error?.message || 'Cheque return could not be completed.');
    } finally {
      setSubmittingReturnId(null);
    }
  };

  const submitChequeBookStock = async () => {
    if (!onCreateChequeBookStock) return;
    const startSerialNumber = Number(chequeBookDraft.startSerialNumber);
    const leafCount = Number(chequeBookDraft.leafCount);
    if (!chequeBookDraft.branchId.trim() || !chequeBookDraft.seriesPrefix.trim() || startSerialNumber <= 0 || leafCount <= 0) {
      setChequeError('Provide branch, prefix, start serial, and leaf count before stocking a cheque book.');
      setChequeMessage(null);
      return;
    }
    setChequeBookBusy('stock');
    try {
      await onCreateChequeBookStock({
        branchId: chequeBookDraft.branchId.trim(),
        seriesPrefix: chequeBookDraft.seriesPrefix.trim(),
        startSerialNumber,
        leafCount,
        remarks: chequeBookDraft.remarks.trim(),
      });
      setChequeMessage('Cheque-book stock received successfully.');
      setChequeError(null);
      setChequeBookDraft({ branchId: chequeBookDraft.branchId, seriesPrefix: chequeBookDraft.seriesPrefix, startSerialNumber: '', leafCount: '25', remarks: '' });
    } catch (error: any) {
      setChequeError(error?.message || 'Cheque-book stock could not be recorded.');
      setChequeMessage(null);
    } finally {
      setChequeBookBusy(null);
    }
  };

  const submitChequeBookIssue = async (book: ChequeBookInventory) => {
    if (!onIssueChequeBook) return;
    const accountId = (issueAccountByBook[book.id] || '').trim();
    if (!accountId) {
      setChequeError('Select or enter an account before issuing the cheque book.');
      setChequeMessage(null);
      return;
    }
    setChequeBookBusy(book.id);
    try {
      await onIssueChequeBook(book.id, { accountId });
      setChequeMessage(`Cheque book ${book.bookReference} issued to ${accountId}.`);
      setChequeError(null);
      setIssueAccountByBook((current) => ({ ...current, [book.id]: '' }));
    } catch (error: any) {
      setChequeError(error?.message || 'Cheque book could not be issued.');
      setChequeMessage(null);
    } finally {
      setChequeBookBusy(null);
    }
  };

  const SortIcon = ({ field }: { field: SortField }) => {
    if (sort.field !== field) return <ArrowUpDown size={14} className="text-slate-300" />;
    return sort.direction === 'asc' ? <ArrowUp size={14} className="text-blue-600" /> : <ArrowDown size={14} className="text-blue-600" />;
  };

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 flex flex-col h-full overflow-hidden">
      <div className="p-6 border-b border-gray-200 space-y-5">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div>
            <div className="flex items-center gap-3">
              <h2 className="text-xl font-bold text-gray-800 flex items-center gap-2"><RefreshCw className="text-blue-600" />Payments Workspace</h2>
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border bg-green-50 text-green-700 border-green-200"><Clock3 size={12} className="animate-pulse" />Live operations</div>
            </div>
            <p className="mt-2 text-sm text-slate-500">Monitor posted transactions, submit bulk batches, and manage cheque clearing in one operational view.</p>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-xs text-gray-500">Last update: <span className="font-mono text-gray-700">{lastUpdated.toLocaleTimeString()}</span></span>
            {onRefreshPayments && <button onClick={() => void onRefreshPayments()} className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"><RefreshCw size={14} />Refresh queues</button>}
          </div>
        </div>
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {paymentMetrics.map((metric) => <div key={metric.label} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4"><div className="flex items-center justify-between"><div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{metric.label}</div>{metric.icon}</div><div className="mt-3 text-2xl font-bold text-slate-900">{metric.value}</div><div className="mt-1 text-sm text-slate-500">{metric.helper}</div></div>)}
        </div>
        <div className="flex flex-wrap gap-2">
          {['transactions', 'bulk', 'cheques'].map((tab) => <button key={tab} onClick={() => setActiveTab(tab as PaymentTab)} className={`rounded-full px-4 py-2 text-sm font-semibold transition ${activeTab === tab ? 'bg-slate-950 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}>{tab === 'bulk' ? 'Bulk Payments' : tab === 'cheques' ? 'Cheque Clearing' : 'Transactions'}</button>)}
        </div>
      </div>
      <div className="flex-1 overflow-auto">
        {activeTab === 'transactions' && (
          <div className="p-6 space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg border border-gray-200">
              <div className="grid grid-cols-1 md:grid-cols-4 lg:grid-cols-6 gap-4 mb-4">
                <div><label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Account ID</label><input value={filters.accountId} onChange={(e) => setFilters({ ...filters, accountId: e.target.value })} placeholder="201..." className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none" /></div>
                <div><label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Teller ID</label><input value={filters.tellerId} onChange={(e) => setFilters({ ...filters, tellerId: e.target.value })} placeholder="TLR..." className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none" /></div>
                <div><label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Type</label><select value={filters.type} onChange={(e) => setFilters({ ...filters, type: e.target.value })} className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"><option value="ALL">All Types</option><option value="DEPOSIT">Deposit</option><option value="WITHDRAWAL">Withdrawal</option><option value="TRANSFER">Transfer</option><option value="LOAN_REPAYMENT">Loan Repayment</option></select></div>
                <div><label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Status</label><select value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value })} className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"><option value="ALL">All Statuses</option><option value="POSTED">Posted</option><option value="PENDING">Pending</option><option value="REJECTED">Rejected</option></select></div>
                <div className="md:col-span-2"><label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Amount Range</label><div className="flex gap-2"><input type="number" placeholder="Min" value={filters.minAmount} onChange={(e) => setFilters({ ...filters, minAmount: e.target.value })} className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none" /><input type="number" placeholder="Max" value={filters.maxAmount} onChange={(e) => setFilters({ ...filters, maxAmount: e.target.value })} className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none" /></div></div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-4 lg:grid-cols-6 gap-4">
                <div className="md:col-span-2"><label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Narration Search</label><div className="relative"><Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" /><input value={filters.narration} onChange={(e) => setFilters({ ...filters, narration: e.target.value })} placeholder="Search description..." className="w-full pl-9 pr-3 p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none" /></div></div>
                <div className="md:col-span-3"><label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Date Range</label><div className="flex gap-2"><input type="date" value={filters.startDate} onChange={(e) => setFilters({ ...filters, startDate: e.target.value })} className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none" /><span className="text-gray-400 self-center">-</span><input type="date" value={filters.endDate} onChange={(e) => setFilters({ ...filters, endDate: e.target.value })} className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none" /></div></div>
                <div className="flex items-end"><button onClick={clearFilters} className="w-full py-2 bg-white border border-gray-300 text-gray-600 rounded text-sm font-medium hover:bg-gray-100 flex items-center justify-center gap-1"><X size={14} />Clear Filters</button></div>
              </div>
            </div>
            <table className="w-full text-left text-sm">
              <thead className="bg-gray-100 text-gray-600 font-semibold sticky top-0 z-10 border-b border-gray-200">
                <tr>
                  <th className="p-4 cursor-pointer" onClick={() => handleSort('date')}><div className="flex items-center gap-1">Date <SortIcon field="date" /></div></th>
                  <th className="p-4 cursor-pointer" onClick={() => handleSort('id')}><div className="flex items-center gap-1">Txn ID <SortIcon field="id" /></div></th>
                  <th className="p-4 cursor-pointer" onClick={() => handleSort('accountId')}><div className="flex items-center gap-1">Account <SortIcon field="accountId" /></div></th>
                  <th className="p-4">Type</th>
                  <th className="p-4 text-right cursor-pointer" onClick={() => handleSort('amount')}><div className="flex items-center justify-end gap-1">Amount <SortIcon field="amount" /></div></th>
                  <th className="p-4">Narration</th>
                  <th className="p-4">Teller</th>
                  <th className="p-4 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filteredTransactions.map((tx) => (
                  <tr key={tx.id} className="hover:bg-blue-50 transition-colors">
                    <td className="p-4 text-gray-600 whitespace-nowrap">{new Date(tx.date).toLocaleString()}</td>
                    <td className="p-4 font-mono text-blue-600 text-xs">{tx.id}</td>
                    <td className="p-4 font-medium text-gray-800">{tx.accountId}</td>
                    <td className="p-4"><span className={`px-2 py-1 rounded text-xs font-bold ${tx.type === 'DEPOSIT' ? 'bg-green-100 text-green-700' : tx.type === 'WITHDRAWAL' ? 'bg-red-100 text-red-700' : 'bg-gray-100 text-gray-700'}`}>{tx.type}</span></td>
                    <td className="p-4 text-right font-mono font-medium">{formatMoney(tx.amount)}</td>
                    <td className="p-4 text-gray-500 text-xs max-w-xs truncate" title={tx.narration}>{tx.narration}</td>
                    <td className="p-4 text-gray-500 text-xs">{tx.tellerId}</td>
                    <td className="p-4 text-center"><span className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase border ${statusTone(tx.status)}`}>{tx.status}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
            {filteredTransactions.length === 0 && <div className="flex flex-col items-center justify-center h-48 text-gray-400"><Search size={48} className="mb-2 opacity-20" /><p>No transactions match your criteria.</p></div>}
          </div>
        )}
        {activeTab === 'bulk' && (
          <div className="grid gap-6 p-6 xl:grid-cols-[1.05fr_0.95fr]">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
              <div className="flex items-center justify-between gap-3"><div><div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Bulk settlement batch</div><h3 className="mt-2 text-lg font-bold text-slate-900">Create payment file</h3></div><button onClick={addBulkItem} className="rounded-lg bg-slate-950 px-3 py-2 text-sm font-semibold text-white hover:bg-slate-800">Add line</button></div>
              <div className="mt-4 grid gap-4 md:grid-cols-2">
                <label className="text-sm font-medium text-slate-700">Currency<select value={bulkCurrency} onChange={(e) => setBulkCurrency(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"><option value="GHS">GHS</option><option value="USD">USD</option></select></label>
                <label className="text-sm font-medium text-slate-700 md:col-span-2">Batch narration<input value={bulkNarration} onChange={(e) => setBulkNarration(e.target.value)} placeholder="Salary run / supplier payments / refunds" className="mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm" /></label>
              </div>
              <div className="mt-5 space-y-4">
                {bulkItems.map((item, index) => (
                  <div key={`bulk-item-${index}`} className="rounded-xl border border-slate-200 bg-white p-4">
                    <div className="mb-3 flex items-center justify-between"><div className="text-sm font-semibold text-slate-800">Line {index + 1}</div><button onClick={() => removeBulkItem(index)} className="text-sm text-rose-600 hover:text-rose-700">Remove</button></div>
                    <div className="grid gap-3 md:grid-cols-2">
                      <input value={item.accountId} onChange={(e) => updateBulkItem(index, { accountId: e.target.value })} placeholder="Account ID" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm" />
                      <select value={item.transactionType} onChange={(e) => updateBulkItem(index, { transactionType: e.target.value as Transaction['type'] })} className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm"><option value="DEPOSIT">Deposit</option><option value="WITHDRAWAL">Withdrawal</option><option value="TRANSFER">Transfer</option><option value="LOAN_REPAYMENT">Loan Repayment</option></select>
                      <input value={item.amount} onChange={(e) => updateBulkItem(index, { amount: e.target.value })} placeholder="Amount" type="number" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm" />
                      <input value={item.tellerId} onChange={(e) => updateBulkItem(index, { tellerId: e.target.value })} placeholder="Teller or operator ID" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm" />
                      <input value={item.narration} onChange={(e) => updateBulkItem(index, { narration: e.target.value })} placeholder="Line narration" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm md:col-span-2" />
                    </div>
                  </div>
                ))}
              </div>
              {(bulkMessage || bulkError) && <div className={`mt-4 rounded-xl border px-4 py-3 text-sm ${bulkError ? 'border-rose-200 bg-rose-50 text-rose-700' : 'border-emerald-200 bg-emerald-50 text-emerald-700'}`}>{bulkError || bulkMessage}</div>}
              <div className="mt-5 flex items-center justify-between gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3"><div className="text-sm text-slate-600">{bulkItems.filter((item) => item.accountId.trim() && Number(item.amount) > 0).length} valid line(s) ready for submission</div><button onClick={() => void submitBulkBatch()} disabled={bulkSubmitting || !onCreateBulkPaymentBatch} className="rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50">{bulkSubmitting ? 'Submitting...' : 'Submit batch'}</button></div>
            </div>
            <div className="space-y-4">
              <div className="rounded-2xl border border-slate-200 bg-white p-5">
                <div className="flex items-center justify-between"><div><div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Recent batches</div><h3 className="mt-2 text-lg font-bold text-slate-900">Monitor settlement outcomes</h3></div><div className="text-sm text-slate-500">{bulkBatches.length} batch(es)</div></div>
                <div className="mt-4 space-y-3">
                  {bulkBatches.length === 0 ? <div className="rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500">No bulk batches have been submitted yet.</div> : bulkBatches.map((batch) => (
                    <button key={batch.id} onClick={() => setSelectedBatchId(batch.id)} className={`w-full rounded-xl border px-4 py-4 text-left transition ${selectedBatch?.id === batch.id ? 'border-slate-900 bg-slate-950 text-white' : 'border-slate-200 bg-slate-50 hover:border-slate-300'}`}>
                      <div className="flex items-center justify-between gap-3"><div><div className={`font-semibold ${selectedBatch?.id === batch.id ? 'text-white' : 'text-slate-900'}`}>{batch.batchReference}</div><div className={`text-xs ${selectedBatch?.id === batch.id ? 'text-slate-300' : 'text-slate-500'}`}>{new Date(batch.createdAt).toLocaleString()}</div></div><span className={`rounded-full border px-2 py-1 text-[10px] font-bold uppercase ${selectedBatch?.id === batch.id ? 'border-white/15 bg-white/10 text-white' : statusTone(batch.status)}`}>{batch.status}</span></div>
                      <div className={`mt-2 text-sm ${selectedBatch?.id === batch.id ? 'text-slate-200' : 'text-slate-600'}`}>{batch.itemCount} lines • {formatMoney(batch.totalAmount, batch.currency)}</div>
                    </button>
                  ))}
                </div>
              </div>
              {selectedBatch && <div className="rounded-2xl border border-slate-200 bg-white p-5"><div className="flex items-center justify-between gap-3"><div><div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Batch detail</div><h3 className="mt-2 text-lg font-bold text-slate-900">{selectedBatch.batchReference}</h3></div><span className={`rounded-full border px-2 py-1 text-[10px] font-bold uppercase ${statusTone(selectedBatch.status)}`}>{selectedBatch.status}</span></div><div className="mt-4 grid gap-3 sm:grid-cols-3"><Metric label="Processed" value={`${selectedBatch.processedCount}/${selectedBatch.itemCount}`} /><Metric label="Failed lines" value={selectedBatch.failedCount.toString()} /><Metric label="Posted amount" value={formatMoney(selectedBatch.processedAmount, selectedBatch.currency)} /></div><div className="mt-4 space-y-3">{selectedBatch.items.map((item) => <div key={item.id} className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3"><div className="flex items-center justify-between gap-3"><div><div className="font-semibold text-slate-900">{item.accountId}</div><div className="text-xs text-slate-500">{item.transactionType} • {formatMoney(item.amount, selectedBatch.currency)}</div></div><span className={`rounded-full border px-2 py-1 text-[10px] font-bold uppercase ${statusTone(item.status)}`}>{item.status}</span></div><div className="mt-2 text-sm text-slate-600">{item.narration || 'No narration supplied.'}</div>{(item.postedTransactionId || item.errorMessage) && <div className="mt-2 text-xs text-slate-500">{item.postedTransactionId ? `Posted transaction: ${item.postedTransactionId}` : item.errorMessage}</div>}</div>)}</div></div>}
            </div>
          </div>
        )}
        {activeTab === 'cheques' && (
          <div className="p-6 space-y-4">
            {(chequeMessage || chequeError) && <div className={`rounded-xl border px-4 py-3 text-sm ${chequeError ? 'border-rose-200 bg-rose-50 text-rose-700' : 'border-emerald-200 bg-emerald-50 text-emerald-700'}`}>{chequeError || chequeMessage}</div>}
            <div className="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
                <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Inventory intake</div>
                <h3 className="mt-2 text-lg font-bold text-slate-900">Stock cheque books</h3>
                <div className="mt-4 grid gap-3 md:grid-cols-2">
                  <input value={chequeBookDraft.branchId} onChange={(e) => setChequeBookDraft({ ...chequeBookDraft, branchId: e.target.value })} placeholder="Branch ID" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm" />
                  <input value={chequeBookDraft.seriesPrefix} onChange={(e) => setChequeBookDraft({ ...chequeBookDraft, seriesPrefix: e.target.value.toUpperCase() })} placeholder="Series prefix" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm" />
                  <input type="number" value={chequeBookDraft.startSerialNumber} onChange={(e) => setChequeBookDraft({ ...chequeBookDraft, startSerialNumber: e.target.value })} placeholder="Start serial number" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm" />
                  <input type="number" value={chequeBookDraft.leafCount} onChange={(e) => setChequeBookDraft({ ...chequeBookDraft, leafCount: e.target.value })} placeholder="Leaf count" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm" />
                  <input value={chequeBookDraft.remarks} onChange={(e) => setChequeBookDraft({ ...chequeBookDraft, remarks: e.target.value })} placeholder="Remarks" className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm md:col-span-2" />
                </div>
                <button onClick={() => void submitChequeBookStock()} disabled={chequeBookBusy === 'stock' || !onCreateChequeBookStock} className="mt-4 rounded-lg bg-slate-950 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-50">
                  {chequeBookBusy === 'stock' ? 'Stocking...' : 'Record stock'}
                </button>
              </div>
              <div className="rounded-2xl border border-slate-200 bg-white p-5">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Cheque books</div>
                    <h3 className="mt-2 text-lg font-bold text-slate-900">Inventory and issuance</h3>
                  </div>
                  <div className="text-sm text-slate-500">{chequeBooks.length} book(s)</div>
                </div>
                <div className="mt-4 space-y-3">
                  {chequeBooks.length === 0 && <div className="rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500">No cheque books in inventory yet.</div>}
                  {chequeBooks.map((book) => (
                    <div key={book.id} className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-4">
                      <div className="flex items-center justify-between gap-3">
                        <div>
                          <div className="font-semibold text-slate-900">{book.bookReference}</div>
                          <div className="text-xs text-slate-500">{book.seriesPrefix}{book.startSerialNumber.toString().padStart(6, '0')} - {book.seriesPrefix}{book.endSerialNumber.toString().padStart(6, '0')}</div>
                        </div>
                        <span className={`rounded-full border px-2 py-1 text-[10px] font-bold uppercase ${statusTone(book.status)}`}>{book.status}</span>
                      </div>
                      <div className="mt-3 grid gap-3 sm:grid-cols-3">
                        <Metric label="Available" value={book.availableLeafCount.toString()} />
                        <Metric label="Used" value={book.usedLeafCount.toString()} />
                        <Metric label="Account" value={book.accountId || 'Not issued'} />
                      </div>
                      {book.status === 'IN_STOCK' && onIssueChequeBook && (
                        <div className="mt-3 flex gap-2">
                          <input value={issueAccountByBook[book.id] || ''} onChange={(e) => setIssueAccountByBook((current) => ({ ...current, [book.id]: e.target.value }))} placeholder="Issue to account ID" className="flex-1 rounded-lg border border-slate-300 px-3 py-2.5 text-sm" />
                          <button onClick={() => void submitChequeBookIssue(book)} disabled={chequeBookBusy === book.id} className="rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50">
                            {chequeBookBusy === book.id ? 'Issuing...' : 'Issue'}
                          </button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            </div>
            <div className="grid gap-4 lg:grid-cols-2">
              {chequeItems.sort((a, b) => new Date(b.lodgedAt).getTime() - new Date(a.lodgedAt).getTime()).map((item) => (
                <div key={item.id} className="rounded-2xl border border-slate-200 bg-white p-5">
                  <div className="flex items-start justify-between gap-3"><div><div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{item.isOtherBankCheque ? 'External cheque' : 'On-us cheque'}</div><h3 className="mt-2 text-lg font-bold text-slate-900">{item.chequeNumber}</h3><div className="mt-1 text-sm text-slate-500">{item.accountId} • {formatMoney(item.amount, item.currency)}</div></div><span className={`rounded-full border px-2 py-1 text-[10px] font-bold uppercase ${statusTone(item.status)}`}>{item.status}</span></div>
                  <div className="mt-4 grid gap-3 sm:grid-cols-2"><Metric label="Drawer" value={item.drawerName || 'Not supplied'} /><Metric label="Drawee bank" value={item.draweeBankCode || 'N/A'} /><Metric label="Channel" value={item.clearingChannel || 'GHIPSS'} /><Metric label="Clearing date" value={item.clearingDate || 'Immediate'} /></div>
                  <div className="mt-4 text-sm text-slate-600">{item.narration || 'No narration supplied.'}</div>
                  {['LODGED', 'PENDING_CLEARING'].includes(item.status) && onReturnCheque && <div className="mt-4 space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-4"><label className="block text-sm font-medium text-slate-700">Return reason<input value={returnReasonByCheque[item.id] || ''} onChange={(e) => setReturnReasonByCheque((current) => ({ ...current, [item.id]: e.target.value }))} placeholder="Dormant signature, clearing reject, stale instrument..." className="mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm" /></label><button onClick={() => void handleChequeReturn(item)} disabled={submittingReturnId === item.id} className="rounded-lg bg-rose-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-rose-700 disabled:opacity-50">{submittingReturnId === item.id ? 'Returning...' : 'Return cheque'}</button></div>}
                  {(item.failureReason || item.returnReason || item.postedTransactionId) && <div className="mt-4 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-xs text-slate-600">{item.postedTransactionId && <div>Posted transaction: {item.postedTransactionId}</div>}{item.returnReason && <div>Return reason: {item.returnReason}</div>}{item.failureReason && <div>Failure reason: {item.failureReason}</div>}</div>}
                </div>
              ))}
            </div>
            {chequeItems.length === 0 && <div className="rounded-xl border border-dashed border-slate-300 px-4 py-12 text-center text-sm text-slate-500">No cheque items are in the clearing queue yet.</div>}
          </div>
        )}
      </div>
    </div>
  );
};

export default TransactionExplorer;
