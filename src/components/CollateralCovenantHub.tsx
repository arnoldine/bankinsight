import { useEffect, useState } from 'react';
import { platformEnhancementService, type CollateralManagementSummary } from '../services/platformEnhancementService';

const money = (amount: number) => new Intl.NumberFormat('en-GH', { style: 'currency', currency: 'GHS' }).format(amount || 0);

export default function CollateralCovenantHub() {
  const [data, setData] = useState<CollateralManagementSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      setData(await platformEnhancementService.getCollateralSummary());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load collateral management.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const markCovenantReviewed = async (id: string) => {
    await platformEnhancementService.updateCovenantRecord(id, {
      status: 'SATISFIED',
      lastReviewedAt: new Date().toISOString(),
      detail: 'Reviewed and satisfied in collateral hub.',
    });
    await load();
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Collateral and Covenant Management</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950">Security perfection and covenant oversight</h1>
            <p className="mt-2 text-sm text-slate-600">Track valuation expiry, document custody, perfection status, and borrower covenant compliance.</p>
          </div>
          <button type="button" onClick={() => void load()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800">
            Refresh
          </button>
        </div>
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading collateral controls...</div>
      ) : data && (
        <>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">Valuations expiring in 30 days</p>
              <p className="mt-2 text-3xl font-semibold text-slate-950">{data.expiringValuationsCount}</p>
            </div>
            <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">Overdue covenants</p>
              <p className="mt-2 text-3xl font-semibold text-slate-950">{data.overdueCovenantsCount}</p>
            </div>
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-950">Collateral register</h2>
              <div className="mt-4 space-y-3">
                {data.collateralItems.map((item) => (
                  <div key={item.id} className="rounded-2xl border border-slate-200 p-4">
                    <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                      <div>
                        <p className="text-sm font-semibold text-slate-950">{item.customerName}</p>
                        <p className="text-xs text-slate-500">{item.loanId} • {item.collateralType}</p>
                        <p className="mt-2 text-sm text-slate-600">{item.description}</p>
                      </div>
                      <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.perfectionStatus}</span>
                    </div>
                    <div className="mt-3 grid gap-3 md:grid-cols-2">
                      <p className="text-sm text-slate-600">Registered: {money(item.registeredValue)}</p>
                      <p className="text-sm text-slate-600">Current valuation: {money(item.currentValuation)}</p>
                      <p className="text-sm text-slate-600">Valuation expiry: {item.valuationExpiryDate ? new Date(item.valuationExpiryDate).toLocaleDateString() : 'N/A'}</p>
                      <p className="text-sm text-slate-600">Custody: {item.custodyLocation || 'Unspecified'}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-950">Covenants</h2>
              <div className="mt-4 space-y-3">
                {data.covenants.map((item) => (
                  <div key={item.id} className="rounded-2xl border border-slate-200 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-950">{item.name}</p>
                        <p className="text-xs text-slate-500">{item.loanId} • {item.covenantType}</p>
                      </div>
                      <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.status}</span>
                    </div>
                    <p className="mt-2 text-sm text-slate-600">{item.detail}</p>
                    <div className="mt-3 flex items-center justify-between gap-3">
                      <p className="text-xs text-slate-500">Due: {item.dueDate ? new Date(item.dueDate).toLocaleDateString() : 'N/A'}</p>
                      {item.status !== 'SATISFIED' && (
                        <button type="button" onClick={() => void markCovenantReviewed(item.id)} className="rounded-full bg-slate-950 px-3 py-2 text-xs font-semibold text-white hover:bg-slate-800">
                          Mark reviewed
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
