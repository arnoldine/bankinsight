import { useMemo, useState } from 'react';
import { platformEnhancementService, type Customer360Response } from '../services/platformEnhancementService';

interface CustomerLite {
  id: string;
  name: string;
}

interface Props {
  customers: CustomerLite[];
}

const money = (amount: number, currency: string) =>
  new Intl.NumberFormat('en-GH', { style: 'currency', currency }).format(amount || 0);

export default function Customer360Hub({ customers }: Props) {
  const [customerId, setCustomerId] = useState(customers[0]?.id ?? '');
  const [data, setData] = useState<Customer360Response | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedCustomer = useMemo(
    () => customers.find((item) => item.id === customerId),
    [customerId, customers],
  );

  const load = async (id: string) => {
    if (!id) return;
    setIsLoading(true);
    setError(null);
    try {
      setData(await platformEnhancementService.getCustomer360(id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load customer 360.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Customer 360</p>
        <div className="mt-4 flex flex-col gap-3 lg:flex-row lg:items-end">
          <div className="flex-1">
            <label className="text-sm font-medium text-slate-700">Customer</label>
            <select
              value={customerId}
              onChange={(event) => setCustomerId(event.target.value)}
              className="mt-2 w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none focus:border-slate-400"
            >
              <option value="">Select a customer</option>
              {customers.slice(0, 200).map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.name} ({customer.id})
                </option>
              ))}
            </select>
          </div>
          <button
            type="button"
            onClick={() => void load(customerId)}
            disabled={!customerId || isLoading}
            className="rounded-full bg-slate-950 px-5 py-3 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-300"
          >
            {isLoading ? 'Loading...' : 'Open 360 View'}
          </button>
        </div>
        {selectedCustomer && <p className="mt-3 text-sm text-slate-500">Selected: {selectedCustomer.name}</p>}
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}

      {data && (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">Deposit Balances</p>
              <p className="mt-2 text-2xl font-semibold text-slate-950">{money(data.financialSummary.totalBalances, data.financialSummary.primaryCurrency)}</p>
            </div>
            <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">Loan Exposure</p>
              <p className="mt-2 text-2xl font-semibold text-slate-950">{money(data.financialSummary.totalOutstandingLoans, data.financialSummary.primaryCurrency)}</p>
            </div>
            <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">90-Day Deposits</p>
              <p className="mt-2 text-2xl font-semibold text-slate-950">{money(data.financialSummary.totalDeposits90Days, data.financialSummary.primaryCurrency)}</p>
            </div>
            <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">90-Day Withdrawals</p>
              <p className="mt-2 text-2xl font-semibold text-slate-950">{money(data.financialSummary.totalWithdrawals90Days, data.financialSummary.primaryCurrency)}</p>
            </div>
            <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">Est. Annual Revenue</p>
              <p className="mt-2 text-2xl font-semibold text-slate-950">{money(data.financialSummary.estimatedAnnualRevenue, data.financialSummary.primaryCurrency)}</p>
            </div>
            <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">Relationship Owner</p>
              <p className="mt-2 text-2xl font-semibold text-slate-950">{data.financialSummary.relationshipOwner}</p>
            </div>
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
            <div className="space-y-6">
              <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Customer profile</h2>
                <div className="mt-4 grid gap-4 md:grid-cols-2">
                  <div>
                    <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Identity</p>
                    <p className="mt-2 text-sm text-slate-700">{data.profile.name}</p>
                    <p className="text-sm text-slate-500">{data.profile.id}</p>
                    <p className="text-sm text-slate-500">{data.profile.ghanaCard || 'No Ghana Card recorded'}</p>
                  </div>
                  <div>
                    <p className="text-xs uppercase tracking-[0.2em] text-slate-400">KYC and risk</p>
                    <p className="mt-2 text-sm text-slate-700">KYC: {data.profile.kycLevel || 'N/A'}</p>
                    <p className="text-sm text-slate-500">Risk: {data.profile.riskRating || 'N/A'}</p>
                    <p className="text-sm text-slate-500">
                      Ready for loan origination: {data.profile.kycReadiness?.isReadyForLoanOrigination ? 'Yes' : 'No'}
                    </p>
                  </div>
                </div>
              </section>

              <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Accounts and facilities</h2>
                <div className="mt-4 grid gap-4 lg:grid-cols-2">
                  <div>
                    <h3 className="text-sm font-semibold text-slate-700">Accounts</h3>
                    <div className="mt-3 space-y-3">
                      {data.accounts.map((account) => (
                        <div key={account.id} className="rounded-2xl border border-slate-200 p-4">
                          <div className="flex items-center justify-between gap-3">
                            <div>
                              <p className="text-sm font-semibold text-slate-950">{account.id}</p>
                              <p className="text-sm text-slate-500">{account.productCode || 'General account'}</p>
                            </div>
                            <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{account.status}</span>
                          </div>
                          <p className="mt-3 text-sm text-slate-700">{money(account.balance, account.currency)}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                  <div>
                    <h3 className="text-sm font-semibold text-slate-700">Loans</h3>
                    <div className="mt-3 space-y-3">
                      {data.loans.map((loan) => (
                        <div key={loan.id} className="rounded-2xl border border-slate-200 p-4">
                          <div className="flex items-center justify-between gap-3">
                            <div>
                              <p className="text-sm font-semibold text-slate-950">{loan.id}</p>
                              <p className="text-sm text-slate-500">{loan.productCode || 'Loan facility'}</p>
                            </div>
                            <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{loan.status}</span>
                          </div>
                          <p className="mt-3 text-sm text-slate-700">Outstanding: {money(loan.outstandingBalance, data.financialSummary.primaryCurrency)}</p>
                          <p className="text-xs text-slate-500">PAR bucket: {loan.parBucket}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </section>
            </div>

            <div className="space-y-6">
              <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Recent transactions</h2>
                <div className="mt-4 space-y-3">
                  {data.recentTransactions.map((transaction) => (
                    <div key={transaction.id} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-center justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{transaction.type}</p>
                          <p className="text-xs text-slate-500">{transaction.accountId}</p>
                        </div>
                        <p className="text-sm font-semibold text-slate-700">{money(transaction.amount, transaction.currency)}</p>
                      </div>
                      <p className="mt-2 text-xs text-slate-500">{new Date(transaction.date).toLocaleString()}</p>
                    </div>
                  ))}
                </div>
              </section>

              <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Engagement timeline</h2>
                <div className="mt-4 space-y-3">
                  {data.engagementTimeline.map((item, index) => (
                    <div key={`${item.type}-${index}`} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-center justify-between gap-3">
                        <p className="text-sm font-semibold text-slate-950">{item.title}</p>
                        <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{item.type}</span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">{item.detail}</p>
                      <p className="mt-2 text-xs text-slate-500">{new Date(item.at).toLocaleString()}</p>
                    </div>
                  ))}
                </div>
              </section>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
