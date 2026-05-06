import { useEffect, useMemo, useState } from 'react';
import { platformEnhancementService, type ReconciliationException, type ReconciliationHubSummary } from '../services/platformEnhancementService';

const money = (amount: number, currency = 'GHS') =>
  new Intl.NumberFormat('en-GH', { style: 'currency', currency }).format(amount || 0);

const dateText = (value?: string | null) => value ? new Date(value).toLocaleString() : 'Not set';

export default function ReconciliationSettlementHub() {
  const [data, setData] = useState<ReconciliationHubSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedExceptionId, setSelectedExceptionId] = useState('');
  const [settlementForm, setSettlementForm] = useState({
    reconciliationExceptionId: '',
    instructionType: 'MANUAL_SETTLEMENT',
    currency: 'GHS',
    amount: '',
    settlementAccount: '',
    counterparty: '',
    dueAt: '',
    notes: '',
  });

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const summary = await platformEnhancementService.getReconciliationSummary();
      setData(summary);
      const preferredExceptionId = selectedExceptionId || summary.exceptions[0]?.id || '';
      setSelectedExceptionId(preferredExceptionId);
      setSettlementForm((current) => ({
        ...current,
        reconciliationExceptionId: current.reconciliationExceptionId || preferredExceptionId,
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load reconciliation hub.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const selectedException = useMemo(
    () => data?.exceptions.find((item) => item.id === settlementForm.reconciliationExceptionId) ?? null,
    [data, settlementForm.reconciliationExceptionId],
  );

  const resolve = async (item: ReconciliationException) => {
    await platformEnhancementService.updateReconciliationException(item.id, {
      status: 'RESOLVED',
      workflowStage: 'CLOSED',
      resolutionCode: 'MANUAL_CLEAR',
      detail: 'Resolved from reconciliation hub.',
    });
    await load();
  };

  const retry = async (item: ReconciliationException) => {
    await platformEnhancementService.retryReconciliationException(item.id, {
      detail: `Retry initiated for ${item.reference} from reconciliation hub.`,
    });
    await load();
  };

  const createSettlementInstruction = async () => {
    if (!settlementForm.reconciliationExceptionId || !settlementForm.amount) {
      return;
    }

    await platformEnhancementService.createSettlementInstruction({
      reconciliationExceptionId: settlementForm.reconciliationExceptionId,
      instructionType: settlementForm.instructionType,
      currency: settlementForm.currency,
      amount: Number(settlementForm.amount),
      settlementAccount: settlementForm.settlementAccount || undefined,
      counterparty: settlementForm.counterparty || undefined,
      dueAt: settlementForm.dueAt || undefined,
      notes: settlementForm.notes || undefined,
    });

    setSettlementForm((current) => ({
      ...current,
      amount: '',
      settlementAccount: '',
      counterparty: '',
      dueAt: '',
      notes: '',
    }));

    await load();
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Reconciliation and Settlement</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950">Break management desk</h1>
            <p className="mt-2 text-sm text-slate-600">Consolidate treasury, branch transfer, and cash-control breaks into one operational queue with retry and settlement controls.</p>
          </div>
          <button type="button" onClick={() => void load()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800">
            Refresh
          </button>
        </div>
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading reconciliation queue...</div>
      ) : data && (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {data.metrics.map((metric) => (
              <div key={metric.key} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <p className="text-sm text-slate-500">{metric.label}</p>
                <p className="mt-2 text-3xl font-semibold text-slate-950">{metric.value}</p>
              </div>
            ))}
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-950">Open exceptions</h2>
              <div className="mt-4 space-y-3">
                {data.exceptions.map((item) => (
                  <div key={item.id} className="rounded-2xl border border-slate-200 p-4">
                    <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">{item.category}</span>
                          <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-semibold text-slate-600">{item.severity}</span>
                          <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-semibold text-amber-800">{item.workflowStage || 'OPEN'}</span>
                        </div>
                        <p className="mt-2 text-sm font-semibold text-slate-950">{item.summary}</p>
                        <p className="mt-1 text-sm text-slate-600">{item.detail}</p>
                        <p className="mt-2 text-xs text-slate-500">{item.reference} • {money(item.amount, item.currency)}</p>
                        <div className="mt-2 flex flex-wrap gap-3 text-xs text-slate-500">
                          <span>Status: {item.status}</span>
                          <span>Retries: {item.retryCount}</span>
                          <span>Last attempt: {dateText(item.lastAttemptAt)}</span>
                          <span>Resolution: {item.resolutionCode || 'Pending'}</span>
                        </div>
                      </div>
                      <div className="flex flex-wrap items-center gap-2">
                        {item.status !== 'RESOLVED' && (
                          <>
                            <button type="button" onClick={() => void retry(item)} className="rounded-full border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 hover:border-slate-400">
                              Retry
                            </button>
                            <button type="button" onClick={() => void resolve(item)} className="rounded-full bg-slate-950 px-3 py-2 text-xs font-semibold text-white hover:bg-slate-800">
                              Mark Resolved
                            </button>
                          </>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Settlement instruction</h2>
                <div className="mt-4 space-y-3">
                  <select
                    value={settlementForm.reconciliationExceptionId}
                    onChange={(e) => setSettlementForm({ ...settlementForm, reconciliationExceptionId: e.target.value })}
                    className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm"
                  >
                    {data.exceptions.map((item) => (
                      <option key={item.id} value={item.id}>{item.reference} / {item.category}</option>
                    ))}
                  </select>
                  <select
                    value={settlementForm.instructionType}
                    onChange={(e) => setSettlementForm({ ...settlementForm, instructionType: e.target.value })}
                    className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm"
                  >
                    <option value="MANUAL_SETTLEMENT">Manual settlement</option>
                    <option value="NOSTRO_FUNDING">Nostro funding</option>
                    <option value="REVERSAL_POSTING">Reversal posting</option>
                    <option value="COUNTERPARTY_FOLLOW_UP">Counterparty follow-up</option>
                  </select>
                  <input value={settlementForm.amount} onChange={(e) => setSettlementForm({ ...settlementForm, amount: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Amount" />
                  <input value={settlementForm.settlementAccount} onChange={(e) => setSettlementForm({ ...settlementForm, settlementAccount: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Settlement account" />
                  <input value={settlementForm.counterparty} onChange={(e) => setSettlementForm({ ...settlementForm, counterparty: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Counterparty" />
                  <input value={settlementForm.dueAt} onChange={(e) => setSettlementForm({ ...settlementForm, dueAt: e.target.value })} type="datetime-local" className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" />
                  <textarea value={settlementForm.notes} onChange={(e) => setSettlementForm({ ...settlementForm, notes: e.target.value })} className="min-h-[90px] w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Settlement notes" />
                  {selectedException && (
                    <p className="text-xs text-slate-500">
                      Selected break: {selectedException.reference} / {money(selectedException.amount, selectedException.currency)}
                    </p>
                  )}
                  <button type="button" onClick={() => void createSettlementInstruction()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800">
                    Create settlement instruction
                  </button>
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Settlement queue</h2>
                <div className="mt-4 space-y-3">
                  {data.settlementInstructions.map((item) => (
                    <div key={item.id} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{item.instructionType}</p>
                          <p className="text-xs text-slate-500">{money(item.amount, item.currency)} / {item.counterparty || 'Internal counterparty'}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.status}</span>
                      </div>
                      <div className="mt-2 space-y-1 text-xs text-slate-500">
                        <p>Settlement account: {item.settlementAccount || 'Not supplied'}</p>
                        <p>Due: {dateText(item.dueAt)}</p>
                        <p>Completed: {dateText(item.completedAt)}</p>
                        {item.notes && <p>Notes: {item.notes}</p>}
                      </div>
                    </div>
                  ))}
                  {data.settlementInstructions.length === 0 && (
                    <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-5 text-sm text-slate-500">
                      No settlement instructions created yet.
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
