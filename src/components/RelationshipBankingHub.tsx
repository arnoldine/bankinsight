import { useEffect, useState } from 'react';
import {
  platformEnhancementService,
  type RelationshipBankingSummary,
  type RelationshipPortfolioDetail,
} from '../services/platformEnhancementService';

const money = (value: number) => new Intl.NumberFormat('en-GH', { style: 'currency', currency: 'GHS' }).format(value || 0);
const dateText = (value: string) => new Date(value).toLocaleString();

export default function RelationshipBankingHub() {
  const [data, setData] = useState<RelationshipBankingSummary | null>(null);
  const [detail, setDetail] = useState<RelationshipPortfolioDetail | null>(null);
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(null);
  const [ownerDrafts, setOwnerDrafts] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      setData(await platformEnhancementService.getRelationshipBankingSummary());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load relationship banking summary.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  useEffect(() => {
    if (!selectedCustomerId) {
      setDetail(null);
      return;
    }

    const loadDetail = async () => {
      setIsDetailLoading(true);
      try {
        setDetail(await platformEnhancementService.getRelationshipPortfolioDetail(selectedCustomerId));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load relationship portfolio detail.');
      } finally {
        setIsDetailLoading(false);
      }
    };

    void loadDetail();
  }, [selectedCustomerId]);

  const assignOwner = async (customerId: string) => {
    try {
      await platformEnhancementService.assignRelationshipOwner({
        customerId,
        ownerUserId: ownerDrafts[customerId] || undefined,
        assignmentNote: 'Relationship owner updated from BankInsight.',
      });
      await load();
      if (selectedCustomerId === customerId) {
        setDetail(await platformEnhancementService.getRelationshipPortfolioDetail(customerId));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to assign relationship owner.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Relationship Banking</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950">Customer relationship command center</h1>
            <p className="mt-2 text-sm text-slate-600">Track relationship value, ownership, profitability, and service pressure across priority customers.</p>
          </div>
          <button type="button" onClick={() => void load()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800">
            Refresh
          </button>
        </div>
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading relationship banking view...</div>
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

          <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Top relationships</h2>
                <div className="mt-4 space-y-3">
                  {data.topRelationships.map((item) => (
                    <div key={item.customerId} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <button type="button" onClick={() => setSelectedCustomerId(item.customerId)} className="text-left">
                          <p className="text-sm font-semibold text-slate-950">{item.customerName}</p>
                          <p className="text-xs text-slate-500">{item.customerId} • {item.segment}</p>
                        </button>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.riskSummary}</span>
                      </div>
                      <div className="mt-3 grid gap-3 text-xs text-slate-600 md:grid-cols-3">
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Deposits: <span className="font-semibold text-slate-900">{money(item.depositBalance)}</span></div>
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Investments: <span className="font-semibold text-slate-900">{money(item.investmentBalance)}</span></div>
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Loan exposure: <span className="font-semibold text-slate-900">{money(item.loanExposure)}</span></div>
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Relationship value: <span className="font-semibold text-slate-900">{money(item.estimatedRelationshipValue)}</span></div>
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Annual revenue: <span className="font-semibold text-slate-900">{money(item.estimatedAnnualRevenue)}</span></div>
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Accounts / Loans / Inv: <span className="font-semibold text-slate-900">{item.activeAccountCount}/{item.activeLoanCount}/{item.activeInvestmentCount}</span></div>
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Complaints / Links: <span className="font-semibold text-slate-900">{item.openComplaintCount}/{item.householdOrGroupLinks}</span></div>
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Owner: <span className="font-semibold text-slate-900">{item.relationshipOwner}</span></div>
                        <div className="rounded-xl bg-slate-50 px-3 py-2">Last engagement: <span className="font-semibold text-slate-900">{item.lastEngagementAt ? dateText(item.lastEngagementAt) : 'No recent signal'}</span></div>
                      </div>
                      <div className="mt-3 flex flex-wrap items-center gap-2">
                        <select
                          value={ownerDrafts[item.customerId] ?? item.relationshipOwnerUserId ?? ''}
                          onChange={(event) => setOwnerDrafts((current) => ({ ...current, [item.customerId]: event.target.value }))}
                          className="rounded-full border border-slate-200 px-3 py-2 text-xs text-slate-700"
                        >
                          <option value="">Unassigned</option>
                          {data.assignableStaff.map((staff) => (
                            <option key={staff.userId} value={staff.userId}>{staff.name}</option>
                          ))}
                        </select>
                        <button type="button" onClick={() => void assignOwner(item.customerId)} className="rounded-full border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50">
                          Update owner
                        </button>
                        <button type="button" onClick={() => setSelectedCustomerId(item.customerId)} className="rounded-full bg-slate-950 px-3 py-2 text-xs font-semibold text-white hover:bg-slate-800">
                          Drill through
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Manager performance</h2>
                <div className="mt-4 space-y-3">
                  {data.managerPerformance.map((item) => (
                    <div key={item.relationshipOwner} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-center justify-between gap-3">
                        <p className="text-sm font-semibold text-slate-950">{item.relationshipOwner}</p>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.customerCount} customers</span>
                      </div>
                      <div className="mt-3 grid gap-2 text-xs text-slate-600 md:grid-cols-2">
                        <div>Deposits: <span className="font-semibold text-slate-900">{money(item.depositBalance)}</span></div>
                        <div>Exposure: <span className="font-semibold text-slate-900">{money(item.loanExposure)}</span></div>
                        <div>Revenue: <span className="font-semibold text-slate-900">{money(item.estimatedAnnualRevenue)}</span></div>
                        <div>Complaints / High risk: <span className="font-semibold text-slate-900">{item.openComplaintCount}/{item.highRiskRelationships}</span></div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-950">{detail ? `${detail.customerName} portfolio detail` : 'Recent engagement timeline'}</h2>
              {isDetailLoading ? (
                <div className="mt-4 text-sm text-slate-500">Loading relationship detail...</div>
              ) : detail ? (
                <div className="mt-4 space-y-4">
                  <div className="grid gap-3 md:grid-cols-2">
                    {detail.productBreakdown.map((item) => (
                      <div key={item.category} className="rounded-2xl border border-slate-200 p-4 text-sm text-slate-600">
                        <p className="font-semibold text-slate-950">{item.category}</p>
                        <p className="mt-2">Count: {item.count}</p>
                        <p>Balance: {money(item.balance)}</p>
                        <p>Contribution: {money(item.contribution)}</p>
                      </div>
                    ))}
                  </div>
                  <div className="space-y-3">
                    {detail.recentEngagements.map((item, index) => (
                      <div key={`${item.customerId}-${item.source}-${index}`} className="rounded-2xl border border-slate-200 p-4">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="text-sm font-semibold text-slate-950">{item.title}</p>
                            <p className="text-xs text-slate-500">{item.customerName} • {item.source}</p>
                          </div>
                          <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.severity}</span>
                        </div>
                        <p className="mt-2 text-sm text-slate-600">{item.detail}</p>
                        <p className="mt-2 text-xs text-slate-500">{dateText(item.occurredAt)}</p>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <div className="mt-4 space-y-3">
                  {data.recentEngagements.map((item, index) => (
                    <div key={`${item.customerId}-${item.source}-${index}`} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{item.title}</p>
                          <p className="text-xs text-slate-500">{item.customerName} • {item.source}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{item.severity}</span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">{item.detail}</p>
                      <p className="mt-2 text-xs text-slate-500">{dateText(item.occurredAt)}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
