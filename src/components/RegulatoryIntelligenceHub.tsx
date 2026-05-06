import { useEffect, useState } from 'react';
import { platformEnhancementService, type AssignableStaffItem, type RegulatoryIntelligenceSummary } from '../services/platformEnhancementService';

const dateText = (value?: string | null) => (value ? new Date(value).toLocaleString() : 'Not available');

export default function RegulatoryIntelligenceHub() {
  const [data, setData] = useState<RegulatoryIntelligenceSummary | null>(null);
  const [staff, setStaff] = useState<AssignableStaffItem[]>([]);
  const [ownerDrafts, setOwnerDrafts] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [summary, assignableStaff] = await Promise.all([
        platformEnhancementService.getRegulatoryIntelligenceSummary(),
        platformEnhancementService.getAssignableRelationshipStaff(),
      ]);
      setData(summary);
      setStaff(assignableStaff);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load regulatory intelligence.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const updateVarianceStatus = async (reference: string, returnType: string, reopen = false) => {
    const resolutionNote = window.prompt(reopen ? 'Add a short reopen note' : 'Add a short resolution note');
    if (resolutionNote === null) {
      return;
    }

    try {
      if (reopen) {
        await platformEnhancementService.reopenRegulatoryVariance({ reference, returnType, resolutionNote });
      } else {
        await platformEnhancementService.resolveRegulatoryVariance({ reference, returnType, resolutionNote });
      }
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update regulatory variance.');
    }
  };

  const assignVariance = async (reference: string, returnType: string) => {
    try {
      await platformEnhancementService.assignRegulatoryVariance({
        reference,
        returnType,
        ownerUserId: ownerDrafts[reference] || undefined,
        assignmentNote: 'Variance assignment updated from BankInsight.',
      });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to assign regulatory variance.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Regulatory Intelligence</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950">ORASS readiness and variance workbench</h1>
            <p className="mt-2 text-sm text-slate-600">See regulator submission readiness, assignment state, acknowledgement history, and unresolved return variances in one place.</p>
          </div>
          <button type="button" onClick={() => void load()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800">
            Refresh
          </button>
        </div>
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading regulatory intelligence...</div>
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

          <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Readiness profile</h2>
                <div className="mt-4 space-y-2 text-sm text-slate-600">
                  <p>Profile configured: <span className="font-semibold text-slate-950">{data.readiness.profileConfigured ? 'Yes' : 'No'}</span></p>
                  <p>Ready for submission: <span className="font-semibold text-slate-950">{data.readiness.readyForSubmission ? 'Yes' : 'No'}</span></p>
                  <p>Mode: <span className="font-semibold text-slate-950">{data.readiness.submissionMode}</span></p>
                  <p>Source report code: <span className="font-semibold text-slate-950">{data.readiness.sourceReportCode}</span></p>
                  <p>Last prepared: <span className="font-semibold text-slate-950">{dateText(data.readiness.lastPreparedReturnDate)}</span></p>
                  <p>Last submission: <span className="font-semibold text-slate-950">{dateText(data.readiness.lastSubmissionAt)}</span></p>
                </div>
                {data.readiness.missingRequirements.length > 0 && (
                  <div className="mt-4 rounded-2xl border border-amber-200 bg-amber-50 p-4">
                    <p className="text-sm font-semibold text-amber-900">Missing requirements</p>
                    <ul className="mt-2 list-disc pl-5 text-sm text-amber-800">
                      {data.readiness.missingRequirements.map((item) => <li key={item}>{item}</li>)}
                    </ul>
                  </div>
                )}
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Open variances</h2>
                <div className="mt-4 space-y-3">
                  {data.variances.map((item, index) => (
                    <div key={`${item.reference}-${index}`} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{item.title}</p>
                          <p className="text-xs text-slate-500">{item.returnType} • Ref {item.reference}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.severity}</span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">{item.detail}</p>
                      <p className="mt-2 text-xs text-slate-500">{item.actionHint}</p>
                      <div className="mt-2 flex flex-wrap gap-2 text-xs text-slate-500">
                        <span className="rounded-full bg-slate-100 px-2.5 py-1 font-semibold text-slate-600">{item.resolutionStatus}</span>
                        <span>Owner: {item.ownerName || 'Unassigned'}</span>
                        {item.assignedByName && <span>Assigned by: {item.assignedByName}</span>}
                        {item.assignedAt && <span>Assigned: {dateText(item.assignedAt)}</span>}
                        {item.resolutionNote && <span>Note: {item.resolutionNote}</span>}
                      </div>
                      <div className="mt-3 flex flex-wrap items-center gap-2">
                        <select
                          value={ownerDrafts[item.reference] ?? item.ownerUserId ?? ''}
                          onChange={(event) => setOwnerDrafts((current) => ({ ...current, [item.reference]: event.target.value }))}
                          className="rounded-full border border-slate-200 px-3 py-2 text-xs text-slate-700"
                        >
                          <option value="">Unassigned</option>
                          {staff.map((person) => (
                            <option key={person.userId} value={person.userId}>{person.name}</option>
                          ))}
                        </select>
                        <button type="button" onClick={() => void assignVariance(item.reference, item.returnType)} className="rounded-full border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50">
                          Assign
                        </button>
                        {item.resolutionStatus === 'RESOLVED' ? (
                          <button type="button" onClick={() => void updateVarianceStatus(item.reference, item.returnType, true)} className="rounded-full border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50">
                            Reopen
                          </button>
                        ) : (
                          <button type="button" onClick={() => void updateVarianceStatus(item.reference, item.returnType)} className="rounded-full bg-slate-950 px-3 py-1.5 text-xs font-semibold text-white hover:bg-slate-800">
                            Mark resolved
                          </button>
                        )}
                      </div>
                      {item.events.length > 0 && (
                        <div className="mt-4 rounded-2xl bg-slate-50 p-4">
                          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">History</p>
                          <div className="mt-3 space-y-2">
                            {item.events.slice(0, 4).map((event, eventIndex) => (
                              <div key={`${item.reference}-event-${eventIndex}`} className="text-xs text-slate-600">
                                <span className="font-semibold text-slate-900">{event.eventType}</span>
                                {' '}• {event.performedByName || 'System'} • {dateText(event.createdAt)}
                                <div className="mt-1 text-slate-500">{event.detail}</div>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                  {data.variances.length === 0 && (
                    <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-5 text-sm text-slate-500">
                      No open regulatory variances.
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Submission queue</h2>
                <div className="mt-4 space-y-3">
                  {data.queue.map((item) => (
                    <div key={item.id} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{item.returnType}</p>
                          <p className="text-xs text-slate-500">{item.returnDate} • {item.reportingPeriodStart} to {item.reportingPeriodEnd}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.submissionStatus}</span>
                      </div>
                      <div className="mt-2 space-y-1 text-xs text-slate-500">
                        <p>Records: {item.totalRecords.toLocaleString()} • Validation: {item.validationStatus}</p>
                        <p>Ready: {item.isReadyForSubmission ? 'Yes' : 'No'}</p>
                        {item.validationMessages[0] && <p>Message: {item.validationMessages[0]}</p>}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Submission history</h2>
                <div className="mt-4 space-y-3">
                  {data.history.map((item) => (
                    <div key={item.id} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{item.returnType}</p>
                          <p className="text-xs text-slate-500">{item.submittedBy} • {item.submissionDate || item.returnDate}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.acknowledgementStatus}</span>
                      </div>
                      <div className="mt-2 space-y-1 text-xs text-slate-500">
                        <p>Transport: {item.transportStatus}</p>
                        <p>Reference: {item.bogReferenceNumber || 'Pending regulator ref'}</p>
                        {(item.transportMessage || item.validationMessages[0]) && <p>Message: {item.transportMessage || item.validationMessages[0]}</p>}
                      </div>
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
