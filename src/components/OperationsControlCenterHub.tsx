import { useEffect, useState } from 'react';
import { AlertTriangle, CheckCircle2, Clock3, ShieldAlert } from 'lucide-react';
import { platformEnhancementService, type OperationsControlSummary } from '../services/platformEnhancementService';

interface Props {
  onNavigate?: (tabId: string) => void;
}

const statusTone = (severity: string) => {
  switch (severity) {
    case 'HIGH':
      return 'border-rose-200 bg-rose-50 text-rose-700';
    case 'MEDIUM':
      return 'border-amber-200 bg-amber-50 text-amber-700';
    default:
      return 'border-emerald-200 bg-emerald-50 text-emerald-700';
  }
};

export default function OperationsControlCenterHub({ onNavigate }: Props) {
  const [data, setData] = useState<OperationsControlSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      setData(await platformEnhancementService.getOperationsControlSummary());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load operations control summary.');
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
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Operations Control Center</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950">Enterprise operations command view</h1>
            <p className="mt-2 max-w-3xl text-sm text-slate-600">
              Keep approvals, collections, security, cash control, and queue health in one operating picture.
            </p>
          </div>
          <button
            type="button"
            onClick={() => void load()}
            className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            Refresh
          </button>
        </div>
      </div>

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>
      )}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading operations intelligence...</div>
      ) : data && (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {data.metrics.map((metric) => (
              <div key={metric.key} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-medium text-slate-500">{metric.label}</p>
                    <p className="mt-2 text-3xl font-semibold text-slate-950">{metric.value}</p>
                    {metric.subtitle && <p className="mt-2 text-xs uppercase tracking-[0.18em] text-slate-400">{metric.subtitle}</p>}
                  </div>
                  <span className={`rounded-full border px-3 py-1 text-xs font-semibold ${statusTone(metric.severity)}`}>
                    {metric.severity}
                  </span>
                </div>
              </div>
            ))}
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Exception Queue</p>
                  <h2 className="mt-2 text-lg font-semibold text-slate-950">Actionable work items</h2>
                </div>
                <span className={`rounded-full px-3 py-1 text-xs font-semibold ${data.platformStatus === 'HEALTHY' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                  {data.platformStatus.replaceAll('_', ' ')}
                </span>
              </div>
              <div className="mt-4 space-y-3">
                {data.workItems.length === 0 ? (
                  <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-700">
                    All monitored queues are currently within healthy bounds.
                  </div>
                ) : data.workItems.map((item) => (
                  <button
                    type="button"
                    key={item.id}
                    onClick={() => item.routeHint && onNavigate?.(item.routeHint.replace(/^\//, ''))}
                    className="w-full rounded-2xl border border-slate-200 p-4 text-left transition hover:border-slate-300 hover:bg-slate-50"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">{item.category}</span>
                          <span className={`rounded-full border px-2 py-0.5 text-[11px] font-semibold ${statusTone(item.severity)}`}>{item.severity}</span>
                        </div>
                        <p className="mt-2 text-sm font-semibold text-slate-950">{item.title}</p>
                        <p className="mt-1 text-sm text-slate-600">{item.detail}</p>
                      </div>
                      <div className="text-right text-sm font-semibold text-slate-500">
                        {item.count ?? item.amount ?? ''}
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            </div>

            <div className="space-y-4">
              <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <div className="flex items-center gap-3">
                  <CheckCircle2 className="text-emerald-600" size={20} />
                  <div>
                    <p className="text-sm font-semibold text-slate-950">Business date</p>
                    <p className="text-sm text-slate-600">{data.businessDate}</p>
                  </div>
                </div>
              </div>
              <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <div className="flex items-center gap-3">
                  <Clock3 className="text-amber-600" size={20} />
                  <div>
                    <p className="text-sm font-semibold text-slate-950">Control discipline</p>
                    <p className="text-sm text-slate-600">Maker-checker queues, open incidents, and stale collections are prioritized here.</p>
                  </div>
                </div>
              </div>
              <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <div className="flex items-center gap-3">
                  <ShieldAlert className="text-rose-600" size={20} />
                  <div>
                    <p className="text-sm font-semibold text-slate-950">Security and resilience</p>
                    <p className="text-sm text-slate-600">Failed logins and WAF activity are surfaced alongside operational queues so risk is visible in context.</p>
                  </div>
                </div>
              </div>
              <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <div className="flex items-center gap-3">
                  <AlertTriangle className="text-slate-600" size={20} />
                  <div>
                    <p className="text-sm font-semibold text-slate-950">What good looks like</p>
                    <p className="text-sm text-slate-600">The goal is an exception-first desk: fewer blind lists, faster routing, clearer queue ownership.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
