import React, { useEffect, useMemo, useState } from 'react';
import { AlertCircle, CheckCircle, Clock, Download, RefreshCw, Send, ShieldCheck, UploadCloud } from 'lucide-react';
import { RegulatoryReturnDTO, reportService } from '../src/services/reportService';
import { adminService, OrassReadiness } from '../src/services/adminService';
import { getUiErrorMessage } from '../src/utils/errorUtils';

type ReturnTypeOption = {
  id: string;
  name: string;
  frequency: string;
};

const reportTypes: ReturnTypeOption[] = [
  { id: 'DailyPosition', name: 'Daily Position Report', frequency: 'Daily' },
  { id: 'MonthlyReturn1', name: 'Monthly Return 1 (Deposits)', frequency: 'Monthly' },
  { id: 'MonthlyReturn2', name: 'Monthly Return 2 (Loans)', frequency: 'Monthly' },
  { id: 'MonthlyReturn3', name: 'Monthly Return 3 (Off-BS)', frequency: 'Monthly' },
  { id: 'PrudentialReturn', name: 'Prudential Return', frequency: 'Quarterly' },
  { id: 'LargeExposure', name: 'Large Exposure Report', frequency: 'Monthly' },
];

export default function RegulatoryReports() {
  const [returns, setReturns] = useState<RegulatoryReturnDTO[]>([]);
  const [readiness, setReadiness] = useState<OrassReadiness | null>(null);
  const [selectedReturnType, setSelectedReturnType] = useState<string>('DailyPosition');
  const [loading, setLoading] = useState(true);
  const [actionStatus, setActionStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);

  const loadData = async () => {
    setLoading(true);
    setActionStatus(null);
    try {
      const [returnData, readinessData] = await Promise.all([
        reportService.getRegulatoryReturns().catch(() => []),
        adminService.getOrassReadiness().catch(() => null),
      ]);

      setReturns(returnData || []);
      setReadiness(readinessData);
    } catch (error) {
      setActionStatus({ tone: 'error', message: getUiErrorMessage(error, 'Unable to load regulatory reporting posture.') });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const filteredReturns = useMemo(() => {
    return returns.filter((item) => !selectedReturnType || item.returnType === selectedReturnType);
  }, [returns, selectedReturnType]);

  const metrics = useMemo(() => {
    return {
      total: returns.length,
      ready: returns.filter((item) => item.isReadyForSubmission).length,
      warnings: returns.filter((item) => item.validationStatus === 'WARNING').length,
      errors: returns.filter((item) => item.validationStatus === 'ERROR').length,
    };
  }, [returns]);

  const handleSubmit = async (returnId: number) => {
    setBusyId(returnId);
    try {
      await reportService.submitRegulatoryReturn(returnId);
      setActionStatus({ tone: 'success', message: `Regulatory return ${returnId} submitted successfully.` });
      await loadData();
    } catch (error) {
      setActionStatus({ tone: 'error', message: getUiErrorMessage(error, 'Unable to submit the selected regulatory return.') });
    } finally {
      setBusyId(null);
    }
  };

  const handleRefresh = async () => {
    await loadData();
  };

  return (
    <div className="space-y-6">
      <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <h2 className="flex items-center gap-2 text-2xl font-bold text-slate-900">
              <UploadCloud className="h-6 w-6 text-red-600" />
              Bank of Ghana Regulatory Reporting
            </h2>
            <p className="mt-2 text-sm text-slate-600">
              Review prepared returns, validation posture, and ORASS readiness from live backend data instead of sample submissions.
            </p>
          </div>
          <button
            type="button"
            onClick={() => void handleRefresh()}
            className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50"
          >
            <RefreshCw className="h-4 w-4" />
            Refresh
          </button>
        </div>
      </div>

      {actionStatus && (
        <div className={`rounded-2xl border px-4 py-3 text-sm ${actionStatus.tone === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-rose-200 bg-rose-50 text-rose-800'}`}>
          {actionStatus.message}
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Tracked Returns" value={String(metrics.total)} icon={<Clock className="h-5 w-5 text-slate-700" />} />
        <MetricCard label="Ready to Submit" value={String(metrics.ready)} icon={<CheckCircle className="h-5 w-5 text-emerald-700" />} />
        <MetricCard label="Validation Warnings" value={String(metrics.warnings)} icon={<AlertCircle className="h-5 w-5 text-amber-700" />} />
        <MetricCard label="Validation Errors" value={String(metrics.errors)} icon={<AlertCircle className="h-5 w-5 text-rose-700" />} />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
            <div>
              <h3 className="text-lg font-semibold text-slate-900">Prepared Returns</h3>
              <p className="mt-1 text-sm text-slate-500">Filter and action the live regulatory packages prepared by the reporting engine.</p>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Return Type</label>
              <select
                value={selectedReturnType}
                onChange={(event) => setSelectedReturnType(event.target.value)}
                className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm md:w-72"
              >
                {reportTypes.map((type) => (
                  <option key={type.id} value={type.id}>{type.name}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="mt-5 space-y-3">
            {loading ? (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500">
                Loading regulatory returns...
              </div>
            ) : filteredReturns.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500">
                No prepared returns were found for the selected return type.
              </div>
            ) : (
              filteredReturns.map((item) => (
                <div key={item.id} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                  <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                    <div>
                      <div className="flex items-center gap-2">
                        <div className="font-semibold text-slate-900">{item.returnType}</div>
                        <StatusPill label={item.submissionStatus} tone={item.submissionStatus === 'Submitted' ? 'green' : item.validationStatus === 'ERROR' ? 'red' : item.validationStatus === 'WARNING' ? 'amber' : 'blue'} />
                      </div>
                      <div className="mt-2 text-sm text-slate-600">
                        Return date: {new Date(item.returnDate).toLocaleDateString()} | Records: {item.totalRecords}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        Validation: {item.validationStatus} {item.bogReferenceNumber ? `| BoG Ref: ${item.bogReferenceNumber}` : ''}
                      </div>
                      {item.validationErrors?.length > 0 && (
                        <div className="mt-3 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800">
                          {item.validationErrors.join(' | ')}
                        </div>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      <button type="button" className="inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">
                        <Download className="h-4 w-4" />
                        Review
                      </button>
                      <button
                        type="button"
                        disabled={!item.isReadyForSubmission || busyId === item.id}
                        onClick={() => void handleSubmit(item.id)}
                        className="inline-flex items-center gap-2 rounded-xl bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {busyId === item.id ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                        Submit
                      </button>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center gap-3">
            <ShieldCheck className="h-5 w-5 text-slate-700" />
            <h3 className="text-lg font-semibold text-slate-900">ORASS Readiness</h3>
          </div>
          <div className="mt-5 space-y-3 text-sm text-slate-700">
            {readiness ? (
              <>
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                  <div className="font-semibold text-slate-900">Profile</div>
                  <div className="mt-1">{readiness.profileConfigured ? 'Configured' : 'Incomplete'}</div>
                </div>
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                  <div className="font-semibold text-slate-900">Submission Mode</div>
                  <div className="mt-1">{readiness.submissionMode}</div>
                </div>
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                  <div className="font-semibold text-slate-900">Ready Returns</div>
                  <div className="mt-1">{readiness.returnsReadyForSubmission}</div>
                </div>
                {readiness.missingRequirements?.length > 0 && (
                  <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-4 text-amber-900">
                    {readiness.missingRequirements.join(' | ')}
                  </div>
                )}
              </>
            ) : (
              <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500">
                ORASS readiness data is not available in this environment.
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function MetricCard({ label, value, icon }: { label: string; value: string; icon: React.ReactNode }) {
  return (
    <div className="rounded-[24px] border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{label}</div>
          <div className="mt-2 text-2xl font-bold text-slate-900">{value}</div>
        </div>
        <div className="rounded-2xl bg-slate-100 p-3">{icon}</div>
      </div>
    </div>
  );
}

function StatusPill({ label, tone }: { label: string; tone: 'green' | 'amber' | 'red' | 'blue' }) {
  const classes = {
    green: 'bg-emerald-100 text-emerald-800',
    amber: 'bg-amber-100 text-amber-800',
    red: 'bg-rose-100 text-rose-800',
    blue: 'bg-sky-100 text-sky-800',
  } as const;

  return <span className={`rounded-full px-2 py-1 text-[11px] font-semibold ${classes[tone]}`}>{label}</span>;
}
