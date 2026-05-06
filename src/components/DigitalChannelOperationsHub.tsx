import { useEffect, useState } from 'react';
import { platformEnhancementService, type DigitalChannelOperationsSummary } from '../services/platformEnhancementService';

const money = (value: number) => new Intl.NumberFormat('en-GH', { style: 'currency', currency: 'GHS' }).format(value || 0);
const dateText = (value: string) => new Date(value).toLocaleString();

export default function DigitalChannelOperationsHub() {
  const [data, setData] = useState<DigitalChannelOperationsSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      setData(await platformEnhancementService.getDigitalChannelOperationsSummary());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load digital channel operations.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Digital Channel Operations</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950">Channel health and service operations</h1>
            <p className="mt-2 text-sm text-slate-600">Supervise client sessions, complaints, KYC refresh workload, and channel usage trends from one supervisory desk.</p>
          </div>
          <button type="button" onClick={() => void load()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800">
            Refresh
          </button>
        </div>
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading digital channel operations...</div>
      ) : data && (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {data.metrics.map((metric) => (
              <div key={metric.key} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <p className="text-sm text-slate-500">{metric.label}</p>
                <p className="mt-2 text-3xl font-semibold text-slate-950">{metric.value}</p>
                {metric.subtitle && <p className="mt-1 text-xs text-slate-500">{metric.subtitle}</p>}
              </div>
            ))}
          </div>

          <div className="grid gap-6 xl:grid-cols-3">
            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-950">Channel mix</h2>
              <div className="mt-4 space-y-3">
                {data.channelMetrics.map((metric) => (
                  <div key={metric.channelName} className="rounded-2xl border border-slate-200 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-950">{metric.channelName}</p>
                        <p className="text-xs text-slate-500">{metric.transactionCount.toLocaleString()} transactions</p>
                      </div>
                      <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{metric.percentageOfTotal.toFixed(1)}%</span>
                    </div>
                    <p className="mt-2 text-sm text-slate-600">{money(metric.transactionVolume)}</p>
                  </div>
                ))}
              </div>
            </div>

            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-950">Session risk</h2>
              <div className="mt-4 space-y-3">
                {data.sessionRiskItems.map((item) => (
                  <div key={item.sessionId} className="rounded-2xl border border-slate-200 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-950">{item.customerName}</p>
                        <p className="text-xs text-slate-500">{item.customerId} • {item.ipAddress}</p>
                      </div>
                      <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.riskLabel}</span>
                    </div>
                    <div className="mt-2 space-y-1 text-xs text-slate-500">
                      <p>Last activity: {dateText(item.lastActivity)}</p>
                      <p>Expires: {dateText(item.expiresAt)}</p>
                      <p>Status: {item.isActive ? 'Active' : 'Inactive'}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Complaint queue</h2>
                <div className="mt-4 space-y-3">
                  {data.complaintQueue.map((item) => (
                    <div key={item.complaintId} className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-sm font-semibold text-slate-950">{item.reference}</p>
                      <p className="text-xs text-slate-500">{item.customerName} • {item.category} • {item.ownerTeam}</p>
                      <p className="mt-2 text-sm text-slate-600">{item.summary}</p>
                      <p className="mt-2 text-xs text-slate-500">SLA due: {dateText(item.slaDueAt)} / {item.status}</p>
                    </div>
                  ))}
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">KYC refresh queue</h2>
                <div className="mt-4 space-y-3">
                  {data.kycQueue.map((item) => (
                    <div key={item.kycCaseId} className="rounded-2xl border border-slate-200 p-4">
                      <p className="text-sm font-semibold text-slate-950">{item.reference}</p>
                      <p className="text-xs text-slate-500">{item.customerName} • {item.status}</p>
                      <p className="mt-2 text-sm text-slate-600">{item.reason}</p>
                      <p className="mt-2 text-xs text-slate-500">Submitted: {dateText(item.submittedAt)} {item.reviewerName ? `• Reviewer: ${item.reviewerName}` : ''}</p>
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
