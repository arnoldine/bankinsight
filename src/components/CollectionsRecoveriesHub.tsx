import { useEffect, useState } from 'react';
import { platformEnhancementService, type CollectionCase } from '../services/platformEnhancementService';

const money = (amount: number) => new Intl.NumberFormat('en-GH', { style: 'currency', currency: 'GHS' }).format(amount || 0);

export default function CollectionsRecoveriesHub() {
  const [cases, setCases] = useState<CollectionCase[]>([]);
  const [selectedCaseId, setSelectedCaseId] = useState('');
  const [notes, setNotes] = useState('');
  const [actionType, setActionType] = useState('PROMISE_TO_PAY');
  const [actionAmount, setActionAmount] = useState('');
  const [assignedAgency, setAssignedAgency] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadCases = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await platformEnhancementService.getCollectionCases();
      setCases(data);
      setSelectedCaseId((current) => current || data[0]?.id || '');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load collection cases.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadCases();
  }, []);

  const selectedCase = cases.find((item) => item.id === selectedCaseId) ?? null;

  const logFollowUp = async () => {
    if (!selectedCase) return;
    await platformEnhancementService.updateCollectionCase(selectedCase.id, {
      eventType: 'FOLLOW_UP',
      detail: notes || 'Follow-up logged from recoveries hub.',
      status: selectedCase.status,
      priority: selectedCase.priority,
      recoveryStage: selectedCase.recoveryStage,
      notes,
    });
    setNotes('');
    await loadCases();
  };

  const runRecoveryAction = async () => {
    if (!selectedCase) return;
    await platformEnhancementService.executeCollectionAction(selectedCase.id, {
      actionType,
      detail: notes || `Recovery action ${actionType} logged from recoveries hub.`,
      promiseToPayAmount: actionType === 'PROMISE_TO_PAY' ? Number(actionAmount || selectedCase.amountInArrears) : undefined,
      settlementAmount: actionType === 'SETTLEMENT_OFFER' ? Number(actionAmount || selectedCase.amountInArrears * 0.85) : undefined,
      assignedAgency: actionType === 'ASSIGN_AGENCY' ? assignedAgency : undefined,
      writeOffReason: actionType === 'WRITE_OFF_RECOMMENDATION' ? notes || 'Write-off recommended from recoveries hub.' : undefined,
    });
    setNotes('');
    setActionAmount('');
    setAssignedAgency('');
    await loadCases();
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Collections and Recoveries</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950">Delinquency workbench</h1>
            <p className="mt-2 text-sm text-slate-600">Prioritize arrears, capture promises to pay, and keep recovery actions auditable.</p>
          </div>
          <button
            type="button"
            onClick={() => void loadCases()}
            className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            Refresh
          </button>
        </div>
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading recoveries queue...</div>
      ) : (
        <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
          <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="text-lg font-semibold text-slate-950">Queue</h2>
            <div className="mt-4 space-y-3">
              {cases.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => setSelectedCaseId(item.id)}
                  className={`w-full rounded-2xl border p-4 text-left transition ${selectedCaseId === item.id ? 'border-slate-900 bg-slate-50' : 'border-slate-200 hover:border-slate-300'}`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-950">{item.customerName}</p>
                      <p className="text-xs text-slate-500">{item.loanId}</p>
                    </div>
                    <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.priority}</span>
                  </div>
                  <p className="mt-3 text-sm text-slate-700">{item.delinquencyDays} day(s) overdue</p>
                  <p className="text-xs text-slate-500">Arrears: {money(item.amountInArrears)}</p>
                </button>
              ))}
            </div>
          </div>

          <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
            {!selectedCase ? (
              <div className="text-sm text-slate-500">Select a case to review its recovery details.</div>
            ) : (
              <div className="space-y-6">
                <div>
                  <div className="flex items-center justify-between gap-4">
                    <div>
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Case {selectedCase.id}</p>
                      <h2 className="mt-2 text-xl font-semibold text-slate-950">{selectedCase.customerName}</h2>
                    </div>
                    <span className="rounded-full bg-slate-950 px-3 py-1 text-xs font-semibold text-white">{selectedCase.recoveryStage}</span>
                  </div>
                  <div className="mt-4 grid gap-4 md:grid-cols-2">
                    <div className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Outstanding balance</p>
                      <p className="mt-2 text-lg font-semibold text-slate-950">{money(selectedCase.outstandingBalance)}</p>
                    </div>
                    <div className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Amount in arrears</p>
                        <p className="mt-2 text-lg font-semibold text-slate-950">{money(selectedCase.amountInArrears)}</p>
                    </div>
                  </div>
                  <div className="mt-4 grid gap-4 md:grid-cols-2">
                    <div className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Recovery strategy</p>
                      <p className="mt-2 text-sm font-semibold text-slate-950">{selectedCase.recoveryStrategy || 'Unassigned'}</p>
                    </div>
                    <div className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Legal status</p>
                      <p className="mt-2 text-sm font-semibold text-slate-950">{selectedCase.legalStatus || 'Not started'}</p>
                    </div>
                  </div>
                  <div className="mt-4 grid gap-4 md:grid-cols-3">
                    <div className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Approval status</p>
                      <p className="mt-2 text-sm font-semibold text-slate-950">{selectedCase.approvalStatus || 'Not required'}</p>
                    </div>
                    <div className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Assigned agency</p>
                      <p className="mt-2 text-sm font-semibold text-slate-950">{selectedCase.assignedAgency || 'Internal team'}</p>
                    </div>
                    <div className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Repossession status</p>
                      <p className="mt-2 text-sm font-semibold text-slate-950">{selectedCase.repossessionStatus || 'Not started'}</p>
                    </div>
                  </div>
                </div>

                <div className="rounded-2xl border border-slate-200 p-4">
                  <label className="text-sm font-semibold text-slate-800">Recovery note</label>
                  <textarea
                    value={notes}
                    onChange={(event) => setNotes(event.target.value)}
                    className="mt-3 min-h-[120px] w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-900 outline-none focus:border-slate-400"
                    placeholder="Capture call outcome, promise to pay, or next-step instructions..."
                  />
                  <div className="mt-4 flex justify-end">
                    <button
                      type="button"
                      onClick={() => void logFollowUp()}
                      className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-800"
                    >
                      Log follow-up
                    </button>
                  </div>
                </div>

                <div className="rounded-2xl border border-slate-200 p-4">
                  <label className="text-sm font-semibold text-slate-800">Recovery action</label>
                  <div className="mt-3 grid gap-3 md:grid-cols-3">
                    <select value={actionType} onChange={(event) => setActionType(event.target.value)} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-900">
                      <option value="PROMISE_TO_PAY">Promise to Pay</option>
                      <option value="SETTLEMENT_OFFER">Settlement Offer</option>
                      <option value="LEGAL_REFERRAL">Legal Referral</option>
                      <option value="REPOSSESSION_REVIEW">Repossession Review</option>
                      <option value="WRITE_OFF_RECOMMENDATION">Write-Off Recommendation</option>
                      <option value="ASSIGN_AGENCY">Assign Agency</option>
                    </select>
                    <input
                      value={actionAmount}
                      onChange={(event) => setActionAmount(event.target.value)}
                      className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-900"
                      placeholder="Action amount (optional)"
                    />
                    <input
                      value={assignedAgency}
                      onChange={(event) => setAssignedAgency(event.target.value)}
                      className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-900"
                      placeholder="Agency (for assignment)"
                    />
                    <button
                      type="button"
                      onClick={() => void runRecoveryAction()}
                      className="rounded-full bg-amber-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-amber-500"
                    >
                      Run action
                    </button>
                  </div>
                </div>

                <div>
                  <h3 className="text-sm font-semibold text-slate-800">Recent recovery actions</h3>
                  <div className="mt-3 space-y-3">
                    {selectedCase.events.map((item, index) => (
                      <div key={`${item.eventType}-${index}`} className="rounded-2xl border border-slate-200 p-4">
                        <div className="flex items-center justify-between gap-3">
                          <p className="text-sm font-semibold text-slate-950">{item.eventType}</p>
                          <p className="text-xs text-slate-500">{new Date(item.createdAt).toLocaleString()}</p>
                        </div>
                        <p className="mt-2 text-sm text-slate-600">{item.detail}</p>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
