import React, { useEffect, useMemo, useState } from 'react';
import { AlertCircle, CheckCircle2, Clock3, RefreshCw, Search, ShieldCheck, XCircle } from 'lucide-react';
import { ApiError } from '../services/httpClient';
import {
  ClientKycCase,
  ClientKycDecision,
  clientKycOpsService,
} from '../services/clientKycOpsService';

const FILTERS = ['ALL', 'SUBMITTED', 'UNDER_REVIEW', 'APPROVED', 'REJECTED'] as const;

const statusTone = (status: string) => {
  switch (status.toUpperCase()) {
    case 'APPROVED':
      return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300';
    case 'REJECTED':
      return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300';
    case 'UNDER_REVIEW':
      return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300';
    default:
      return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300';
  }
};

const formatDate = (value?: string | null) =>
  value ? new Date(value).toLocaleString() : 'Not available';

export default function ClientKycReviewQueue({
  canReview,
}: {
  canReview: boolean;
}) {
  const [statusFilter, setStatusFilter] = useState<(typeof FILTERS)[number]>('ALL');
  const [search, setSearch] = useState('');
  const [cases, setCases] = useState<ClientKycCase[]>([]);
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);
  const [decision, setDecision] = useState<ClientKycDecision>('UNDER_REVIEW');
  const [note, setNote] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const loadQueue = async (nextFilter = statusFilter) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await clientKycOpsService.getQueue(nextFilter);
      setCases(data);
      setSelectedCaseId((current) => {
        if (current && data.some((item) => item.id === current)) {
          return current;
        }
        return data[0]?.id ?? null;
      });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to load the KYC review queue.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadQueue(statusFilter);
  }, [statusFilter]);

  const filteredCases = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return cases;
    }

    return cases.filter((item) =>
      [item.reference, item.customerId, item.customerName, item.reason, item.summary]
        .filter(Boolean)
        .some((value) => value.toLowerCase().includes(query)),
    );
  }, [cases, search]);

  const selectedCase = filteredCases.find((item) => item.id === selectedCaseId) ?? filteredCases[0] ?? null;

  useEffect(() => {
    if (selectedCase && selectedCase.id !== selectedCaseId) {
      setSelectedCaseId(selectedCase.id);
    }
  }, [selectedCase, selectedCaseId]);

  useEffect(() => {
    if (!selectedCase) {
      setDecision('UNDER_REVIEW');
      setNote('');
      return;
    }

    const suggestedDecision =
      selectedCase.status === 'SUBMITTED' ? 'UNDER_REVIEW' :
      selectedCase.status === 'UNDER_REVIEW' ? 'APPROVED' :
      selectedCase.status === 'APPROVED' ? 'APPROVED' :
      'REJECTED';

    setDecision(suggestedDecision);
    setNote(selectedCase.decisionNote ?? '');
  }, [selectedCase?.id]);

  const queueSummary = useMemo(() => {
    return {
      total: cases.length,
      submitted: cases.filter((item) => item.status === 'SUBMITTED').length,
      underReview: cases.filter((item) => item.status === 'UNDER_REVIEW').length,
      approved: cases.filter((item) => item.status === 'APPROVED').length,
      rejected: cases.filter((item) => item.status === 'REJECTED').length,
    };
  }, [cases]);

  const handleSubmitDecision = async () => {
    if (!selectedCase || !canReview || note.trim().length < 5) {
      return;
    }

    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const updated = await clientKycOpsService.reviewCase(selectedCase.id, {
        decision,
        note: note.trim(),
      });

      setCases((current) => current.map((item) => item.id === updated.id ? updated : item));
      setSuccessMessage(`KYC case ${updated.reference} updated to ${updated.status.replace('_', ' ')}.`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to submit the KYC review decision.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-full space-y-6 p-4 sm:p-6">
      <section className="dashboard-sheen rounded-2xl border border-slate-200 p-6 text-slate-900 shadow-soft">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <p className="mb-1 font-accent text-[11px] uppercase tracking-[0.24em] text-slate-500">Client Channel KYC Ops</p>
            <h2 className="text-3xl font-heading font-semibold tracking-[-0.04em] text-slate-950">Customer KYC Review Queue</h2>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">
              Review customer-submitted KYC refresh cases, verify supporting context, and record a decision with a durable case note.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <button
              onClick={() => loadQueue()}
              className="inline-flex items-center gap-2 rounded-full border border-white/80 bg-white/80 px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-white"
            >
              <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
              Refresh queue
            </button>
            <div className="rounded-full bg-slate-950 px-4 py-2.5 text-sm font-medium text-white dark:bg-white dark:text-slate-950">
              {queueSummary.total} active case{queueSummary.total === 1 ? '' : 's'}
            </div>
          </div>
        </div>
      </section>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
        {[
          { label: 'Submitted', value: queueSummary.submitted, icon: <Clock3 className="h-5 w-5 text-blue-600" /> },
          { label: 'Under Review', value: queueSummary.underReview, icon: <RefreshCw className="h-5 w-5 text-amber-600" /> },
          { label: 'Approved', value: queueSummary.approved, icon: <CheckCircle2 className="h-5 w-5 text-green-600" /> },
          { label: 'Rejected', value: queueSummary.rejected, icon: <XCircle className="h-5 w-5 text-red-600" /> },
          { label: 'Decision Rights', value: canReview ? 'Enabled' : 'Read only', icon: <ShieldCheck className="h-5 w-5 text-slate-700" /> },
        ].map((card) => (
          <div key={card.label} className="glass-card rounded-[24px] border border-white/70 p-5">
            <div className="flex items-center justify-between">
              <p className="text-[11px] font-accent uppercase tracking-[0.2em] text-slate-500">{card.label}</p>
              {card.icon}
            </div>
            <p className="mt-3 text-2xl font-heading font-bold tracking-[-0.04em] text-slate-950">{card.value}</p>
          </div>
        ))}
      </section>

      {error && (
        <div className="flex items-start gap-3 rounded-2xl border border-danger-100 bg-danger-50 p-4 shadow-sm">
          <AlertCircle className="mt-0.5 h-5 w-5 text-red-600" />
          <p className="text-sm font-medium text-red-800">{error}</p>
        </div>
      )}

      {successMessage && (
        <div className="flex items-start gap-3 rounded-2xl border border-green-100 bg-green-50 p-4 shadow-sm">
          <CheckCircle2 className="mt-0.5 h-5 w-5 text-green-600" />
          <p className="text-sm font-medium text-green-800">{successMessage}</p>
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.1fr_1.4fr]">
        <section className="glass-card rounded-[28px] border border-white/70 p-5">
          <div className="flex flex-col gap-3 border-b border-slate-200 pb-4">
            <div className="flex flex-wrap gap-2">
              {FILTERS.map((filter) => (
                <button
                  key={filter}
                  onClick={() => setStatusFilter(filter)}
                  className={`rounded-full px-3.5 py-2 text-xs font-semibold transition ${
                    statusFilter === filter
                      ? 'bg-slate-950 text-white dark:bg-white dark:text-slate-950'
                      : 'bg-white/80 text-slate-600 hover:bg-white'
                  }`}
                >
                  {filter.replace('_', ' ')}
                </button>
              ))}
            </div>
            <label className="flex items-center gap-3 rounded-[20px] border border-slate-200 bg-white/80 px-4 py-3">
              <Search className="h-4 w-4 text-slate-400" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search by customer, case reference, or reason"
                className="w-full bg-transparent text-sm text-slate-900 outline-none placeholder:text-slate-400"
              />
            </label>
          </div>

          <div className="mt-4 space-y-3">
            {isLoading ? (
              <div className="rounded-[24px] border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
                Loading KYC cases...
              </div>
            ) : filteredCases.length === 0 ? (
              <div className="rounded-[24px] border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
                No KYC cases match the current filter.
              </div>
            ) : (
              filteredCases.map((item) => (
                <button
                  key={item.id}
                  onClick={() => setSelectedCaseId(item.id)}
                  className={`w-full rounded-[24px] border px-4 py-4 text-left transition ${
                    selectedCase?.id === item.id
                      ? 'border-brand-300 bg-brand-50/70 shadow-soft'
                      : 'border-white/70 bg-white/70 hover:bg-white'
                  }`}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="text-sm font-semibold text-slate-950">{item.customerName}</p>
                      <p className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500">{item.reference}</p>
                    </div>
                    <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold ${statusTone(item.status)}`}>
                      {item.status.replace('_', ' ')}
                    </span>
                  </div>
                  <p className="mt-3 text-sm text-slate-600">{item.reason}</p>
                  <p className="mt-2 line-clamp-2 text-xs leading-5 text-slate-500">{item.summary}</p>
                  <div className="mt-3 flex items-center justify-between text-xs text-slate-500">
                    <span>{item.customerId}</span>
                    <span>{formatDate(item.submittedAt)}</span>
                  </div>
                </button>
              ))
            )}
          </div>
        </section>

        <section className="glass-card rounded-[28px] border border-white/70 p-6">
          {!selectedCase ? (
            <div className="flex min-h-[320px] items-center justify-center rounded-[24px] border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
              Select a KYC case to review its customer context, evidence history, and decision log.
            </div>
          ) : (
            <div className="space-y-6">
              <div className="flex flex-col gap-4 border-b border-slate-200 pb-5 lg:flex-row lg:items-start lg:justify-between">
                <div>
                  <p className="text-[11px] font-accent uppercase tracking-[0.24em] text-slate-500">Selected KYC Case</p>
                  <h3 className="mt-2 text-2xl font-heading font-semibold tracking-[-0.04em] text-slate-950">{selectedCase.customerName}</h3>
                  <p className="mt-2 text-sm text-slate-600">
                    {selectedCase.customerId} · {selectedCase.reference}
                  </p>
                </div>
                <span className={`w-fit rounded-full px-3 py-1.5 text-xs font-semibold ${statusTone(selectedCase.status)}`}>
                  {selectedCase.status.replace('_', ' ')}
                </span>
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div className="rounded-[24px] border border-white/70 bg-white/60 p-4">
                  <p className="text-[11px] font-accent uppercase tracking-[0.22em] text-slate-500">Reason</p>
                  <p className="mt-2 text-sm font-medium text-slate-900">{selectedCase.reason}</p>
                </div>
                <div className="rounded-[24px] border border-white/70 bg-white/60 p-4">
                  <p className="text-[11px] font-accent uppercase tracking-[0.22em] text-slate-500">Submitted</p>
                  <p className="mt-2 text-sm font-medium text-slate-900">{formatDate(selectedCase.submittedAt)}</p>
                  <p className="mt-1 text-xs text-slate-500">
                    {selectedCase.reviewedAt ? `Last reviewed ${formatDate(selectedCase.reviewedAt)}` : 'Awaiting first review'}
                  </p>
                </div>
              </div>

              <div className="rounded-[24px] border border-white/70 bg-white/60 p-4">
                <p className="text-[11px] font-accent uppercase tracking-[0.22em] text-slate-500">Customer Summary</p>
                <p className="mt-2 text-sm leading-6 text-slate-700">{selectedCase.summary}</p>
              </div>

              <div className="rounded-[24px] border border-white/70 bg-white/60 p-4">
                <div className="flex items-center justify-between">
                  <p className="text-[11px] font-accent uppercase tracking-[0.22em] text-slate-500">Case Timeline</p>
                  <p className="text-xs text-slate-500">{selectedCase.events.length} event{selectedCase.events.length === 1 ? '' : 's'}</p>
                </div>
                <div className="mt-4 space-y-3">
                  {selectedCase.events.map((event) => (
                    <div key={event.id} className="rounded-[20px] border border-slate-200 bg-white/80 px-4 py-3">
                      <div className="flex flex-wrap items-center justify-between gap-3">
                        <p className="text-sm font-semibold text-slate-900">{event.title}</p>
                        <span className={`rounded-full px-2 py-1 text-[11px] font-semibold ${statusTone(event.eventType)}`}>
                          {event.eventType.replace('_', ' ')}
                        </span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">{event.description}</p>
                      <p className="mt-2 text-xs text-slate-500">
                        {event.actorName || 'System'} · {formatDate(event.createdAt)}
                      </p>
                    </div>
                  ))}
                </div>
              </div>

              <div className="rounded-[24px] border border-white/70 bg-slate-950 p-5 text-white">
                <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                  <div>
                    <p className="text-[11px] font-accent uppercase tracking-[0.22em] text-white/70">Review Action</p>
                    <p className="mt-1 text-sm text-white/80">
                      Record a review decision. This action is written to the KYC case timeline and visible for audit.
                    </p>
                  </div>
                  {!canReview && (
                    <span className="rounded-full border border-white/20 px-3 py-1 text-xs font-semibold text-white/80">
                      Read-only access
                    </span>
                  )}
                </div>
                <div className="mt-5 grid grid-cols-1 gap-4 md:grid-cols-[220px_1fr]">
                  <label className="space-y-2">
                    <span className="text-xs font-medium text-white/80">Decision</span>
                    <select
                      value={decision}
                      onChange={(event) => setDecision(event.target.value as ClientKycDecision)}
                      disabled={!canReview || isSubmitting}
                      className="w-full rounded-2xl border border-white/15 bg-white/10 px-4 py-3 text-sm text-white outline-none"
                    >
                      <option value="UNDER_REVIEW">Under review</option>
                      <option value="APPROVED">Approve</option>
                      <option value="REJECTED">Reject</option>
                    </select>
                  </label>
                  <label className="space-y-2">
                    <span className="text-xs font-medium text-white/80">Decision note</span>
                    <textarea
                      value={note}
                      onChange={(event) => setNote(event.target.value)}
                      disabled={!canReview || isSubmitting}
                      rows={5}
                      placeholder="Explain what was checked, any missing items, and the outcome for the customer."
                      className="w-full rounded-2xl border border-white/15 bg-white/10 px-4 py-3 text-sm text-white outline-none placeholder:text-white/40"
                    />
                  </label>
                </div>
                <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
                  <p className="text-xs text-white/60">
                    Reviewer: {selectedCase.reviewerName || 'No reviewer recorded yet'}
                  </p>
                  <button
                    onClick={handleSubmitDecision}
                    disabled={!canReview || isSubmitting || note.trim().length < 5}
                    className="rounded-full bg-white px-5 py-2.5 text-sm font-semibold text-slate-950 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:bg-white/50"
                  >
                    {isSubmitting ? 'Saving decision...' : 'Save review decision'}
                  </button>
                </div>
              </div>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
