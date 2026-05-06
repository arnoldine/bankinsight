import { useEffect, useMemo, useState } from 'react';
import {
  microfinanceService,
  type AccountSearchItem,
  type CollectorAssignment,
  type FieldCollectionBatch,
  type FieldCollectionBatchLine,
  type MicrofinanceLoanPolicy,
  type MicrofinanceSummary,
} from '../services/microfinanceService';

const money = (value: number) => new Intl.NumberFormat('en-GH', { style: 'currency', currency: 'GHS' }).format(value || 0);
const dateText = (value?: string | null) => value ? new Date(value).toLocaleString() : 'Not scheduled';

const emptyAssignment = {
  staffId: '',
  customerId: '',
  routeCode: '',
  meetingDay: '',
  collectionFrequency: 'DAILY',
  targetDepositAccountId: '',
  targetLoanId: '',
  isPrimaryCollector: true,
};

const emptyBatch = {
  staffId: '',
  businessDate: new Date().toISOString().slice(0, 10),
  routeCode: '',
  collectionType: 'SAVINGS',
  openingFloat: 0,
  currency: 'GHS',
  notes: '',
};

const emptyCollection = {
  assignmentId: '',
  customerId: '',
  targetAccountId: '',
  targetLoanId: '',
  collectionType: 'SAVINGS',
  amount: 0,
  currency: 'GHS',
  narrative: '',
  externalReference: '',
};

export default function MicrofinanceOperationsHub() {
  const [summary, setSummary] = useState<MicrofinanceSummary | null>(null);
  const [loanPolicies, setLoanPolicies] = useState<MicrofinanceLoanPolicy[]>([]);
  const [selectedBatchId, setSelectedBatchId] = useState<string | null>(null);
  const [selectedBatch, setSelectedBatch] = useState<FieldCollectionBatch | null>(null);
  const [customerQuery, setCustomerQuery] = useState('');
  const [accountQuery, setAccountQuery] = useState('');
  const [customerSearchResults, setCustomerSearchResults] = useState<Array<{ customerId: string; customerName: string; branchId?: string | null; phoneNumber?: string | null }>>([]);
  const [accountSearchResults, setAccountSearchResults] = useState<AccountSearchItem[]>([]);
  const [assignmentDraft, setAssignmentDraft] = useState(emptyAssignment);
  const [batchDraft, setBatchDraft] = useState(emptyBatch);
  const [collectionDraft, setCollectionDraft] = useState(emptyCollection);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  const load = async (preferredBatchId?: string | null) => {
    setIsLoading(true);
    setError(null);
    try {
      const [summaryData, policies] = await Promise.all([
        microfinanceService.getSummary(),
        microfinanceService.getLoanPolicies(),
      ]);
      setSummary(summaryData);
      setLoanPolicies(policies);

      const batchIdToUse = preferredBatchId ?? selectedBatchId ?? summaryData.openBatches[0]?.id ?? null;
      setSelectedBatchId(batchIdToUse);
      const batch = summaryData.openBatches.find((item) => item.id === batchIdToUse) ?? null;
      setSelectedBatch(batch);
      if (batch) {
        setCollectionDraft((current) => ({
          ...current,
          currency: batch.currency || 'GHS',
        }));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load microfinance operations.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  useEffect(() => {
    const handle = window.setTimeout(async () => {
      if (!customerQuery.trim()) {
        setCustomerSearchResults([]);
        return;
      }

      try {
        setCustomerSearchResults(await microfinanceService.searchCustomers(customerQuery));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to search customers.');
      }
    }, 300);

    return () => window.clearTimeout(handle);
  }, [customerQuery]);

  useEffect(() => {
    const handle = window.setTimeout(async () => {
      if (!accountQuery.trim() && !assignmentDraft.customerId.trim() && !collectionDraft.customerId.trim()) {
        setAccountSearchResults([]);
        return;
      }

      try {
        setAccountSearchResults(await microfinanceService.searchAccounts(
          accountQuery,
          assignmentDraft.customerId || collectionDraft.customerId || undefined,
        ));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to search accounts.');
      }
    }, 300);

    return () => window.clearTimeout(handle);
  }, [accountQuery, assignmentDraft.customerId, collectionDraft.customerId]);

  const selectedAlerts = useMemo(() => {
    if (!summary) {
      return [];
    }
    return summary.compulsorySavingsAlerts.slice(0, 8);
  }, [summary]);

  const selectedPolicy = useMemo(() => {
    if (!collectionDraft.targetLoanId) {
      return null;
    }
    return loanPolicies.find((item) => item.loanProductId === collectionDraft.targetLoanId || item.loanProductCode === collectionDraft.targetLoanId) ?? null;
  }, [collectionDraft.targetLoanId, loanPolicies]);

  const resetAssignment = () => {
    setAssignmentDraft(emptyAssignment);
    setCustomerQuery('');
    setAccountQuery('');
    setCustomerSearchResults([]);
    setAccountSearchResults([]);
  };

  const resetCollection = () => {
    setCollectionDraft({
      ...emptyCollection,
      currency: selectedBatch?.currency || 'GHS',
    });
    setAccountQuery('');
    setAccountSearchResults([]);
  };

  const saveAssignment = async () => {
    setIsSubmitting(true);
    setError(null);
    setStatusMessage(null);
    try {
      await microfinanceService.upsertAssignment({
        staffId: assignmentDraft.staffId,
        customerId: assignmentDraft.customerId,
        routeCode: assignmentDraft.routeCode || undefined,
        meetingDay: assignmentDraft.meetingDay || undefined,
        collectionFrequency: assignmentDraft.collectionFrequency,
        targetDepositAccountId: assignmentDraft.targetDepositAccountId || undefined,
        targetLoanId: assignmentDraft.targetLoanId || undefined,
        isPrimaryCollector: assignmentDraft.isPrimaryCollector,
      });
      resetAssignment();
      setStatusMessage('Collector assignment saved.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save collector assignment.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const openBatch = async () => {
    setIsSubmitting(true);
    setError(null);
    setStatusMessage(null);
    try {
      const batch = await microfinanceService.openBatch({
        staffId: batchDraft.staffId,
        businessDate: batchDraft.businessDate || undefined,
        routeCode: batchDraft.routeCode || undefined,
        collectionType: batchDraft.collectionType,
        openingFloat: Number(batchDraft.openingFloat) || 0,
        currency: batchDraft.currency || 'GHS',
        notes: batchDraft.notes || undefined,
      });
      setStatusMessage('Field batch opened.');
      setSelectedBatchId(batch.id);
      await load(batch.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to open field collection batch.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const recordCollection = async () => {
    if (!selectedBatchId) {
      setError('Open or select a field batch before posting collections.');
      return;
    }

    setIsSubmitting(true);
    setError(null);
    setStatusMessage(null);
    try {
      await microfinanceService.recordCollection(selectedBatchId, {
        assignmentId: collectionDraft.assignmentId || undefined,
        customerId: collectionDraft.customerId,
        targetAccountId: collectionDraft.targetAccountId || undefined,
        targetLoanId: collectionDraft.targetLoanId || undefined,
        collectionType: collectionDraft.collectionType,
        amount: Number(collectionDraft.amount),
        currency: collectionDraft.currency || selectedBatch?.currency || 'GHS',
        narrative: collectionDraft.narrative || undefined,
        externalReference: collectionDraft.externalReference || undefined,
      });
      setStatusMessage('Collection posted to the active field batch.');
      resetCollection();
      await load(selectedBatchId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to record field collection.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const submitBatch = async () => {
    if (!selectedBatchId) {
      return;
    }

    setIsSubmitting(true);
    setError(null);
    setStatusMessage(null);
    try {
      await microfinanceService.submitBatch(selectedBatchId, {});
      setStatusMessage('Field batch submitted for settlement.');
      await load(selectedBatchId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit field batch.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const settleBatch = async () => {
    if (!selectedBatchId || !selectedBatch) {
      return;
    }

    setIsSubmitting(true);
    setError(null);
    setStatusMessage(null);
    try {
      await microfinanceService.settleBatch(selectedBatchId, {
        settledAmount: selectedBatch.collectedAmount,
        settlementReference: `SET-${Date.now()}`,
        notes: 'Settled from BankInsight microfinance operations.',
      });
      setStatusMessage('Field batch settled successfully.');
      await load(selectedBatchId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to settle field batch.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectAssignmentCustomer = (assignment: CollectorAssignment) => {
    setAssignmentDraft((current) => ({
      ...current,
      customerId: assignment.customerId,
      targetDepositAccountId: assignment.targetDepositAccountId || current.targetDepositAccountId,
      targetLoanId: assignment.targetLoanId || current.targetLoanId,
    }));
    setCollectionDraft((current) => ({
      ...current,
      assignmentId: assignment.id,
      customerId: assignment.customerId,
      targetAccountId: assignment.targetDepositAccountId || current.targetAccountId,
      targetLoanId: assignment.targetLoanId || current.targetLoanId,
      collectionType: assignment.targetLoanId ? 'LOAN_REPAYMENT' : current.collectionType,
    }));
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Microfinance Operations</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950">Collector, susu, and field collections control</h1>
            <p className="mt-2 text-sm text-slate-600">Run daily collections, assign field officers, monitor compulsory savings pressure, and settle collector batches cleanly.</p>
          </div>
          <button type="button" onClick={() => void load(selectedBatchId)} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800">
            Refresh
          </button>
        </div>
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}
      {statusMessage && <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{statusMessage}</div>}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading microfinance operations...</div>
      ) : summary && (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {summary.metrics.map((metric) => (
              <div key={metric.key} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <p className="text-sm text-slate-500">{metric.label}</p>
                <p className="mt-2 text-3xl font-semibold text-slate-950">{metric.value}</p>
                {metric.subtitle && <p className="mt-1 text-xs text-slate-500">{metric.subtitle}</p>}
              </div>
            ))}
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <h2 className="text-lg font-semibold text-slate-950">Collector assignments</h2>
                    <p className="mt-1 text-sm text-slate-600">Assign field staff to customers, route codes, and the right savings or loan target.</p>
                  </div>
                  <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{summary.activeAssignments.length} active</span>
                </div>

                <div className="mt-4 grid gap-3 md:grid-cols-2">
                  <select value={assignmentDraft.staffId} onChange={(event) => setAssignmentDraft((current) => ({ ...current, staffId: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700">
                    <option value="">Select field staff</option>
                    {summary.fieldStaff.map((staff) => (
                      <option key={staff.staffId} value={staff.staffId}>{staff.name} {staff.branchId ? `(${staff.branchId})` : ''}</option>
                    ))}
                  </select>
                  <input value={customerQuery} onChange={(event) => setCustomerQuery(event.target.value)} placeholder="Search customer" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                  <input value={assignmentDraft.routeCode} onChange={(event) => setAssignmentDraft((current) => ({ ...current, routeCode: event.target.value }))} placeholder="Route code" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                  <input value={assignmentDraft.meetingDay} onChange={(event) => setAssignmentDraft((current) => ({ ...current, meetingDay: event.target.value }))} placeholder="Meeting day or market day" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                  <select value={assignmentDraft.collectionFrequency} onChange={(event) => setAssignmentDraft((current) => ({ ...current, collectionFrequency: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700">
                    <option value="DAILY">Daily</option>
                    <option value="WEEKLY">Weekly</option>
                    <option value="BIWEEKLY">Biweekly</option>
                    <option value="MONTHLY">Monthly</option>
                  </select>
                  <input value={accountQuery} onChange={(event) => setAccountQuery(event.target.value)} placeholder="Search savings account" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                </div>

                {customerSearchResults.length > 0 && (
                  <div className="mt-4 rounded-2xl border border-slate-200 p-3">
                    <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Customer matches</p>
                    <div className="mt-2 flex flex-wrap gap-2">
                      {customerSearchResults.slice(0, 8).map((item) => (
                        <button
                          key={item.customerId}
                          type="button"
                          onClick={() => {
                            setAssignmentDraft((current) => ({ ...current, customerId: item.customerId }));
                            setCollectionDraft((current) => ({ ...current, customerId: item.customerId }));
                            setCustomerQuery(item.customerName);
                          }}
                          className="rounded-full border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                        >
                          {item.customerName} ({item.customerId})
                        </button>
                      ))}
                    </div>
                  </div>
                )}

                {accountSearchResults.length > 0 && (
                  <div className="mt-4 rounded-2xl border border-slate-200 p-3">
                    <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Account matches</p>
                    <div className="mt-2 space-y-2">
                      {accountSearchResults.slice(0, 6).map((item) => (
                        <button
                          key={item.accountId}
                          type="button"
                          onClick={() => {
                            setAssignmentDraft((current) => ({
                              ...current,
                              customerId: item.customerId,
                              targetDepositAccountId: item.accountId,
                            }));
                            setCollectionDraft((current) => ({
                              ...current,
                              customerId: item.customerId,
                              targetAccountId: item.accountId,
                              currency: item.currency || current.currency,
                            }));
                            setAccountQuery(item.accountNumber);
                          }}
                          className="flex w-full items-center justify-between rounded-2xl border border-slate-200 px-3 py-2 text-left text-xs hover:bg-slate-50"
                        >
                          <span>
                            <span className="font-semibold text-slate-900">{item.customerName}</span>
                            <span className="ml-2 text-slate-500">{item.accountNumber}</span>
                          </span>
                          <span className="font-semibold text-slate-700">{money(item.availableBalance)}</span>
                        </button>
                      ))}
                    </div>
                  </div>
                )}

                <div className="mt-4 flex flex-wrap gap-3">
                  <button type="button" disabled={isSubmitting || !assignmentDraft.staffId || !assignmentDraft.customerId} onClick={() => void saveAssignment()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">
                    Save assignment
                  </button>
                  <button type="button" onClick={resetAssignment} className="rounded-full border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">
                    Clear
                  </button>
                </div>

                <div className="mt-6 space-y-3">
                  {summary.activeAssignments.slice(0, 10).map((item) => (
                    <div key={item.id} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{item.customerName}</p>
                          <p className="text-xs text-slate-500">{item.staffName} • {item.collectionFrequency} • {item.routeCode || 'No route code'}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.status}</span>
                      </div>
                      <div className="mt-3 grid gap-2 text-xs text-slate-600 md:grid-cols-3">
                        <div>Next collection: <span className="font-semibold text-slate-900">{dateText(item.nextCollectionDate)}</span></div>
                        <div>Deposit target: <span className="font-semibold text-slate-900">{item.targetDepositAccountId || 'Not linked'}</span></div>
                        <div>Loan target: <span className="font-semibold text-slate-900">{item.targetLoanId || 'Savings only'}</span></div>
                      </div>
                      <div className="mt-3">
                        <button type="button" onClick={() => selectAssignmentCustomer(item)} className="rounded-full border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50">
                          Use for collection
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <h2 className="text-lg font-semibold text-slate-950">Active field batch</h2>
                    <p className="mt-1 text-sm text-slate-600">Open a collector day-sheet, post savings or loan collections, then submit and settle the batch.</p>
                  </div>
                  <select value={selectedBatchId ?? ''} onChange={(event) => {
                    const nextBatchId = event.target.value || null;
                    setSelectedBatchId(nextBatchId);
                    setSelectedBatch(summary.openBatches.find((item) => item.id === nextBatchId) ?? null);
                  }} className="rounded-full border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700">
                    <option value="">Select batch</option>
                    {summary.openBatches.map((item) => (
                      <option key={item.id} value={item.id}>{item.staffName} • {item.businessDate} • {item.status}</option>
                    ))}
                  </select>
                </div>

                <div className="mt-4 grid gap-3 md:grid-cols-3">
                  <select value={batchDraft.staffId} onChange={(event) => setBatchDraft((current) => ({ ...current, staffId: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700">
                    <option value="">Select field staff</option>
                    {summary.fieldStaff.map((staff) => (
                      <option key={staff.staffId} value={staff.staffId}>{staff.name}</option>
                    ))}
                  </select>
                  <input type="date" value={batchDraft.businessDate} onChange={(event) => setBatchDraft((current) => ({ ...current, businessDate: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                  <input value={batchDraft.routeCode} onChange={(event) => setBatchDraft((current) => ({ ...current, routeCode: event.target.value }))} placeholder="Route code" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                  <select value={batchDraft.collectionType} onChange={(event) => setBatchDraft((current) => ({ ...current, collectionType: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700">
                    <option value="SAVINGS">Savings collection</option>
                    <option value="LOAN_REPAYMENT">Loan repayment</option>
                    <option value="COMPULSORY_SAVINGS">Compulsory savings</option>
                    <option value="MIXED">Mixed route</option>
                  </select>
                  <input type="number" min="0" step="0.01" value={batchDraft.openingFloat} onChange={(event) => setBatchDraft((current) => ({ ...current, openingFloat: Number(event.target.value) }))} placeholder="Opening float" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                  <input value={batchDraft.currency} onChange={(event) => setBatchDraft((current) => ({ ...current, currency: event.target.value.toUpperCase() }))} placeholder="Currency" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                </div>
                <textarea value={batchDraft.notes} onChange={(event) => setBatchDraft((current) => ({ ...current, notes: event.target.value }))} rows={2} placeholder="Batch notes" className="mt-3 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                <div className="mt-4">
                  <button type="button" disabled={isSubmitting || !batchDraft.staffId} onClick={() => void openBatch()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">
                    Open field batch
                  </button>
                </div>

                {selectedBatch && (
                  <div className="mt-6 rounded-2xl border border-slate-200 p-4">
                    <div className="grid gap-3 md:grid-cols-4 text-sm">
                      <div><span className="text-slate-500">Staff</span><div className="font-semibold text-slate-950">{selectedBatch.staffName}</div></div>
                      <div><span className="text-slate-500">Status</span><div className="font-semibold text-slate-950">{selectedBatch.status}</div></div>
                      <div><span className="text-slate-500">Collected</span><div className="font-semibold text-slate-950">{money(selectedBatch.collectedAmount)}</div></div>
                      <div><span className="text-slate-500">Variance</span><div className="font-semibold text-slate-950">{money(selectedBatch.varianceAmount)}</div></div>
                    </div>

                    <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                      <input value={collectionDraft.customerId} onChange={(event) => setCollectionDraft((current) => ({ ...current, customerId: event.target.value }))} placeholder="Customer ID" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                      <input value={collectionDraft.targetAccountId} onChange={(event) => setCollectionDraft((current) => ({ ...current, targetAccountId: event.target.value }))} placeholder="Target savings account" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                      <input value={collectionDraft.targetLoanId} onChange={(event) => setCollectionDraft((current) => ({ ...current, targetLoanId: event.target.value }))} placeholder="Target loan ID" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                      <select value={collectionDraft.collectionType} onChange={(event) => setCollectionDraft((current) => ({ ...current, collectionType: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700">
                        <option value="SAVINGS">Savings</option>
                        <option value="COMPULSORY_SAVINGS">Compulsory savings</option>
                        <option value="LOAN_REPAYMENT">Loan repayment</option>
                      </select>
                      <input type="number" min="0" step="0.01" value={collectionDraft.amount} onChange={(event) => setCollectionDraft((current) => ({ ...current, amount: Number(event.target.value) }))} placeholder="Amount" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                      <input value={collectionDraft.externalReference} onChange={(event) => setCollectionDraft((current) => ({ ...current, externalReference: event.target.value }))} placeholder="Receipt or external reference" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />
                    </div>
                    <input value={collectionDraft.narrative} onChange={(event) => setCollectionDraft((current) => ({ ...current, narrative: event.target.value }))} placeholder="Narration" className="mt-3 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" />

                    {selectedPolicy && (
                      <div className="mt-3 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-xs text-amber-800">
                        Loan policy: {selectedPolicy.loanProductName} • {selectedPolicy.repaymentFrequency}
                        {selectedPolicy.requiresCompulsorySavings && selectedPolicy.minimumSavingsToLoanRatio
                          ? ` • Compulsory savings ratio ${(selectedPolicy.minimumSavingsToLoanRatio * 100).toFixed(1)}%`
                          : ''}
                      </div>
                    )}

                    <div className="mt-4 flex flex-wrap gap-3">
                      <button type="button" disabled={isSubmitting || !collectionDraft.customerId || collectionDraft.amount <= 0} onClick={() => void recordCollection()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">
                        Post collection
                      </button>
                      <button type="button" disabled={isSubmitting || selectedBatch.lines.length === 0} onClick={() => void submitBatch()} className="rounded-full border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60">
                        Submit batch
                      </button>
                      <button type="button" disabled={isSubmitting || selectedBatch.status !== 'SUBMITTED'} onClick={() => void settleBatch()} className="rounded-full border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-semibold text-emerald-700 hover:bg-emerald-100 disabled:cursor-not-allowed disabled:opacity-60">
                        Settle batch
                      </button>
                    </div>

                    <div className="mt-5 space-y-2">
                      {selectedBatch.lines.length === 0 ? (
                        <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-5 text-sm text-slate-500">No collections have been posted to this batch yet.</div>
                      ) : selectedBatch.lines.map((line: FieldCollectionBatchLine) => (
                        <div key={line.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-slate-200 px-4 py-3 text-sm">
                          <div>
                            <div className="font-semibold text-slate-950">{line.customerName}</div>
                            <div className="text-xs text-slate-500">{line.collectionType} • {line.targetLoanId || line.targetAccountId || 'Manual target'} • {dateText(line.collectedAtUtc)}</div>
                          </div>
                          <div className="text-right">
                            <div className="font-semibold text-slate-950">{money(line.amount)}</div>
                            <div className="text-xs text-slate-500">{line.status}</div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>

            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Compulsory savings watchlist</h2>
                <p className="mt-1 text-sm text-slate-600">Highlight customers whose current savings discipline may block new microcredit disbursements.</p>
                <div className="mt-4 space-y-3">
                  {selectedAlerts.length === 0 ? (
                    <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-5 text-sm text-slate-500">No compulsory savings shortfalls are currently active.</div>
                  ) : selectedAlerts.map((item) => (
                    <div key={`${item.customerId}-${item.loanProductName}`} className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
                      <p className="text-sm font-semibold text-slate-950">{item.customerName}</p>
                      <p className="mt-1 text-xs text-amber-900">{item.loanProductName}</p>
                      <div className="mt-3 grid gap-2 text-xs text-amber-900">
                        <div>Required: <span className="font-semibold">{money(item.requiredAmount)}</span></div>
                        <div>Current: <span className="font-semibold">{money(item.currentAmount)}</span></div>
                        <div>Shortfall: <span className="font-semibold">{money(item.shortfallAmount)}</span></div>
                      </div>
                      <p className="mt-3 text-xs text-amber-900">{item.recommendation}</p>
                    </div>
                  ))}
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Loan policies for microcredit</h2>
                <p className="mt-1 text-sm text-slate-600">Show which loan products enforce compulsory savings and how repayment cadence is configured.</p>
                <div className="mt-4 space-y-3">
                  {loanPolicies.length === 0 ? (
                    <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-5 text-sm text-slate-500">No microfinance loan policies are available yet.</div>
                  ) : loanPolicies.map((item) => (
                    <div key={item.loanProductId} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{item.loanProductName}</p>
                          <p className="text-xs text-slate-500">{item.loanProductCode}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.repaymentFrequency}</span>
                      </div>
                      <p className="mt-3 text-xs text-slate-600">
                        {item.requiresCompulsorySavings
                          ? `Compulsory savings required at ${(Number(item.minimumSavingsToLoanRatio || 0) * 100).toFixed(1)}% of principal.`
                          : 'No compulsory savings rule is enforced on this product.'}
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
