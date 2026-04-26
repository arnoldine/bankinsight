import React, { useEffect, useMemo, useState } from 'react';
import { Account, Customer, Loan } from '../../types';
import { digitalBankingService, DigitalBankingDashboard, DigitalBankingProduct, DigitalInvestmentPortfolio, DigitalLoanEligibility } from '../services/digitalBankingService';
import { loanService, LoanProductDefinition } from '../services/loanService';

type TabKey = 'overview' | 'savings' | 'investments' | 'lending';

interface Props {
  customers: Customer[];
  accounts: Account[];
  loans: Loan[];
  onRefreshAccounts: () => Promise<unknown>;
  onRefreshLoans: () => Promise<unknown>;
}

const cardClass = 'rounded-3xl border border-slate-200 bg-white p-5 shadow-sm';
const inputClass = 'w-full rounded-2xl border border-slate-200 px-3 py-2 text-sm focus:border-sky-500 focus:outline-none';
const buttonClass = 'rounded-2xl bg-sky-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-sky-700';

export default function DigitalBankingHub({ customers, accounts, loans, onRefreshAccounts, onRefreshLoans }: Props) {
  const [activeTab, setActiveTab] = useState<TabKey>('overview');
  const [dashboard, setDashboard] = useState<DigitalBankingDashboard | null>(null);
  const [products, setProducts] = useState<DigitalBankingProduct[]>([]);
  const [loanProducts, setLoanProducts] = useState<LoanProductDefinition[]>([]);
  const [portfolio, setPortfolio] = useState<DigitalInvestmentPortfolio | null>(null);
  const [eligibility, setEligibility] = useState<DigitalLoanEligibility | null>(null);
  const [selectedCustomerId, setSelectedCustomerId] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [savingsForm, setSavingsForm] = useState({
    productCode: '',
    fundingAccountId: '',
    initialDepositAmount: '',
  });
  const [investmentForm, setInvestmentForm] = useState({
    productCode: '',
    fundingAccountId: '',
    principal: '',
    rate: '',
    tenorDays: '',
    payoutOption: 'AT_MATURITY',
  });
  const [eligibilityForm, setEligibilityForm] = useState({
    loanProductId: '',
    principal: '',
  });
  const [loanApplicationForm, setLoanApplicationForm] = useState({
    loanProductId: '',
    principal: '',
    servicingAccountId: '',
    collateralAccountId: '',
  });

  const selectedCustomerAccounts = useMemo(
    () => accounts.filter((account) => account.cif === selectedCustomerId),
    [accounts, selectedCustomerId],
  );

  const customerLoans = useMemo(
    () => loans.filter((loan) => loan.cif === selectedCustomerId),
    [loans, selectedCustomerId],
  );

  const savingsProducts = useMemo(
    () => products.filter((product) => ['SAVINGS', 'CURRENT', 'FIXED_DEPOSIT'].includes(product.type.toUpperCase())),
    [products],
  );

  const refresh = async () => {
    const [dashboardResult, productResult, portfolioResult, loanProductResult] = await Promise.all([
      digitalBankingService.getDashboard(),
      digitalBankingService.getSavingsProducts(),
      digitalBankingService.getInvestmentPortfolio(),
      loanService.getLoanProducts(),
    ]);
    setDashboard(dashboardResult);
    setProducts(productResult);
    setPortfolio(portfolioResult);
    setLoanProducts(loanProductResult);
  };

  useEffect(() => {
    refresh().catch((err) => setError(err instanceof Error ? err.message : 'Failed to load digital banking workspace.'));
  }, []);

  const clearFeedback = () => {
    setMessage(null);
    setError(null);
  };

  const handleOpenSavings = async () => {
    clearFeedback();
    try {
      if (!selectedCustomerId || !savingsForm.productCode) {
        throw new Error('Select a customer and savings product first.');
      }

      await digitalBankingService.openSavingsAccount({
        customerId: selectedCustomerId,
        productCode: savingsForm.productCode,
        fundingAccountId: savingsForm.fundingAccountId || undefined,
        initialDepositAmount: Number(savingsForm.initialDepositAmount || 0),
      });

      await Promise.all([onRefreshAccounts(), refresh()]);
      setMessage('Digital savings account opened successfully.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to open digital savings account.');
    }
  };

  const handleCreateInvestment = async () => {
    clearFeedback();
    try {
      if (!selectedCustomerId) {
        throw new Error('Select a customer before creating a digital investment.');
      }

      await digitalBankingService.createInvestment({
        customerId: selectedCustomerId,
        fundingAccountId: investmentForm.fundingAccountId,
        productCode: investmentForm.productCode,
        principal: Number(investmentForm.principal),
        rate: Number(investmentForm.rate),
        tenorDays: Number(investmentForm.tenorDays),
        payoutOption: investmentForm.payoutOption,
      });

      await Promise.all([onRefreshAccounts(), refresh()]);
      setMessage('Digital investment placed successfully.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create digital investment.');
    }
  };

  const handleCheckEligibility = async () => {
    clearFeedback();
    try {
      if (!selectedCustomerId) {
        throw new Error('Select a customer first.');
      }

      const result = await digitalBankingService.checkLoanEligibility({
        customerId: selectedCustomerId,
        loanProductId: eligibilityForm.loanProductId || undefined,
        principal: eligibilityForm.principal ? Number(eligibilityForm.principal) : undefined,
      });
      setEligibility(result);
      setMessage('Digital loan eligibility refreshed.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to check digital loan eligibility.');
    }
  };

  const handleApplyLoan = async () => {
    clearFeedback();
    try {
      if (!selectedCustomerId) {
        throw new Error('Select a customer first.');
      }

      await digitalBankingService.applyLoan({
        customerId: selectedCustomerId,
        loanProductId: loanApplicationForm.loanProductId,
        principal: Number(loanApplicationForm.principal),
        servicingAccountId: loanApplicationForm.servicingAccountId || undefined,
        collateralAccountId: loanApplicationForm.collateralAccountId || undefined,
      });

      await Promise.all([onRefreshLoans(), refresh()]);
      setMessage('Digital loan application submitted.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to apply for digital loan.');
    }
  };

  const renderOverview = () => (
    <div className="grid gap-4 md:grid-cols-4">
      {[
        ['Savings Accounts', dashboard?.activeSavingsAccounts ?? 0],
        ['Savings Balance', (dashboard?.totalSavingsBalance ?? 0).toLocaleString()],
        ['Investment Profiles', dashboard?.activeInvestmentProfiles ?? 0],
        ['Loan Exposure', (dashboard?.totalLoanExposure ?? 0).toLocaleString()],
      ].map(([label, value]) => (
        <div key={String(label)} className={cardClass}>
          <p className="text-xs uppercase tracking-wide text-slate-500">{label}</p>
          <p className="mt-3 text-2xl font-bold text-slate-950">{value}</p>
        </div>
      ))}
    </div>
  );

  const renderSavings = () => (
    <div className="grid gap-5 lg:grid-cols-[1.1fr,0.9fr]">
      <div className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-950">Open Digital Savings</h3>
        <div className="mt-4 grid gap-3 md:grid-cols-2">
          <select className={inputClass} value={selectedCustomerId} onChange={(e) => setSelectedCustomerId(e.target.value)}>
            <option value="">Select customer</option>
            {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name} ({customer.id})</option>)}
          </select>
          <select className={inputClass} value={savingsForm.productCode} onChange={(e) => setSavingsForm((current) => ({ ...current, productCode: e.target.value }))}>
            <option value="">Select product</option>
            {savingsProducts.map((product) => <option key={product.productCode} value={product.productCode}>{product.name} ({product.type})</option>)}
          </select>
          <select className={inputClass} value={savingsForm.fundingAccountId} onChange={(e) => setSavingsForm((current) => ({ ...current, fundingAccountId: e.target.value }))}>
            <option value="">Funding account (optional)</option>
            {selectedCustomerAccounts.map((account) => <option key={account.id} value={account.id}>{account.id} · {account.type}</option>)}
          </select>
          <input className={inputClass} placeholder="Initial deposit" value={savingsForm.initialDepositAmount} onChange={(e) => setSavingsForm((current) => ({ ...current, initialDepositAmount: e.target.value }))} />
        </div>
        <button className={`${buttonClass} mt-4`} onClick={handleOpenSavings}>Open Savings Account</button>
      </div>
      <div className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-950">Customer Deposit Accounts</h3>
        <div className="mt-4 space-y-3">
          {selectedCustomerAccounts.length === 0 ? (
            <p className="text-sm text-slate-500">Select a customer to view linked deposit accounts.</p>
          ) : selectedCustomerAccounts.map((account) => (
            <div key={account.id} className="rounded-2xl border border-slate-200 p-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-slate-900">{account.id}</p>
                  <p className="text-sm text-slate-500">{account.type} · {account.currency}</p>
                </div>
                <p className="text-sm font-semibold text-slate-800">{account.balance.toLocaleString()}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );

  const renderInvestments = () => (
    <div className="grid gap-5 lg:grid-cols-[1.1fr,0.9fr]">
      <div className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-950">Create Digital Investment</h3>
        <div className="mt-4 grid gap-3 md:grid-cols-2">
          <select className={inputClass} value={selectedCustomerId} onChange={(e) => setSelectedCustomerId(e.target.value)}>
            <option value="">Select customer</option>
            {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name} ({customer.id})</option>)}
          </select>
          <select className={inputClass} value={investmentForm.productCode} onChange={(e) => setInvestmentForm((current) => ({ ...current, productCode: e.target.value }))}>
            <option value="">Select product</option>
            {savingsProducts.filter((product) => product.type.toUpperCase() === 'FIXED_DEPOSIT').map((product) => <option key={product.productCode} value={product.productCode}>{product.name}</option>)}
          </select>
          <select className={inputClass} value={investmentForm.fundingAccountId} onChange={(e) => setInvestmentForm((current) => ({ ...current, fundingAccountId: e.target.value }))}>
            <option value="">Funding account</option>
            {selectedCustomerAccounts.map((account) => <option key={account.id} value={account.id}>{account.id} · {account.type}</option>)}
          </select>
          <input className={inputClass} placeholder="Principal" value={investmentForm.principal} onChange={(e) => setInvestmentForm((current) => ({ ...current, principal: e.target.value }))} />
          <input className={inputClass} placeholder="Rate (%)" value={investmentForm.rate} onChange={(e) => setInvestmentForm((current) => ({ ...current, rate: e.target.value }))} />
          <input className={inputClass} placeholder="Tenor (days)" value={investmentForm.tenorDays} onChange={(e) => setInvestmentForm((current) => ({ ...current, tenorDays: e.target.value }))} />
        </div>
        <button className={`${buttonClass} mt-4`} onClick={handleCreateInvestment}>Place Investment</button>
      </div>
      <div className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-950">Digital Investment Portfolio</h3>
        <p className="mt-2 text-sm text-slate-500">
          Active: {portfolio?.activeProfiles ?? 0} · Principal: {(portfolio?.totalPrincipal ?? 0).toLocaleString()}
        </p>
        <div className="mt-4 space-y-3">
          {(portfolio?.items ?? []).slice(0, 6).map((item) => (
            <div key={item.id} className="rounded-2xl border border-slate-200 p-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-slate-900">{item.accountId}</p>
                  <p className="text-sm text-slate-500">{item.status} · {item.tenorDays} days · {item.rate}%</p>
                </div>
                <p className="text-sm font-semibold text-slate-800">{item.projectedMaturityValue.toLocaleString()}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );

  const renderLending = () => (
    <div className="grid gap-5 lg:grid-cols-[1.1fr,0.9fr]">
      <div className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-950">Digital Loan Eligibility</h3>
        <div className="mt-4 grid gap-3 md:grid-cols-2">
          <select className={inputClass} value={selectedCustomerId} onChange={(e) => setSelectedCustomerId(e.target.value)}>
            <option value="">Select customer</option>
            {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name} ({customer.id})</option>)}
          </select>
          <select className={inputClass} value={eligibilityForm.loanProductId} onChange={(e) => setEligibilityForm((current) => ({ ...current, loanProductId: e.target.value }))}>
            <option value="">Select loan product</option>
            {loanProducts.map((product) => <option key={product.id} value={product.id}>{product.name}</option>)}
          </select>
          <input className={inputClass} placeholder="Requested amount" value={eligibilityForm.principal} onChange={(e) => setEligibilityForm((current) => ({ ...current, principal: e.target.value }))} />
        </div>
        <button className={`${buttonClass} mt-4`} onClick={handleCheckEligibility}>Check Eligibility</button>
        {eligibility && (
          <div className="mt-4 rounded-2xl border border-slate-200 p-4">
            <p className="font-semibold text-slate-900">Decision: {eligibility.creditCheck.decision}</p>
            <p className="mt-1 text-sm text-slate-600">Score: {eligibility.creditCheck.score} · {eligibility.creditCheck.riskBand} · {eligibility.creditCheck.riskGrade}</p>
            {eligibility.reasons.length > 0 && (
              <ul className="mt-3 list-disc pl-5 text-sm text-amber-700">
                {eligibility.reasons.map((reason) => <li key={reason}>{reason}</li>)}
              </ul>
            )}
          </div>
        )}
      </div>
      <div className={cardClass}>
        <h3 className="text-lg font-semibold text-slate-950">Apply Digital Loan</h3>
        <div className="mt-4 grid gap-3">
          <select className={inputClass} value={loanApplicationForm.loanProductId} onChange={(e) => setLoanApplicationForm((current) => ({ ...current, loanProductId: e.target.value }))}>
            <option value="">Select loan product</option>
            {loanProducts.map((product) => <option key={product.id} value={product.id}>{product.name}</option>)}
          </select>
          <input className={inputClass} placeholder="Principal" value={loanApplicationForm.principal} onChange={(e) => setLoanApplicationForm((current) => ({ ...current, principal: e.target.value }))} />
          <select className={inputClass} value={loanApplicationForm.servicingAccountId} onChange={(e) => setLoanApplicationForm((current) => ({ ...current, servicingAccountId: e.target.value }))}>
            <option value="">Servicing account</option>
            {selectedCustomerAccounts.map((account) => <option key={account.id} value={account.id}>{account.id}</option>)}
          </select>
          <select className={inputClass} value={loanApplicationForm.collateralAccountId} onChange={(e) => setLoanApplicationForm((current) => ({ ...current, collateralAccountId: e.target.value }))}>
            <option value="">Collateral account (optional)</option>
            {selectedCustomerAccounts.map((account) => <option key={account.id} value={account.id}>{account.id}</option>)}
          </select>
        </div>
        <button className={`${buttonClass} mt-4`} onClick={handleApplyLoan}>Submit Digital Loan</button>
        <div className="mt-5 space-y-3">
          {customerLoans.slice(0, 5).map((loan) => (
            <div key={loan.id} className="rounded-2xl border border-slate-200 p-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-slate-900">{loan.id}</p>
                  <p className="text-sm text-slate-500">{loan.productName} · {loan.status}</p>
                </div>
                <p className="text-sm font-semibold text-slate-800">{loan.outstandingBalance?.toLocaleString?.() ?? 0}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 className="text-3xl font-bold text-slate-950">Digital Banking</h2>
          <p className="mt-1 text-sm text-slate-500">Manage digital savings, digital investments, and digital lending from one control surface.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {(['overview', 'savings', 'investments', 'lending'] as TabKey[]).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`rounded-full px-4 py-2 text-sm font-semibold transition ${activeTab === tab ? 'bg-slate-950 text-white' : 'bg-slate-100 text-slate-700 hover:bg-slate-200'}`}
            >
              {tab === 'overview' ? 'Overview' : tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {message && <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">{message}</div>}
      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">{error}</div>}

      {activeTab === 'overview' && renderOverview()}
      {activeTab === 'savings' && renderSavings()}
      {activeTab === 'investments' && renderInvestments()}
      {activeTab === 'lending' && renderLending()}
    </div>
  );
}
