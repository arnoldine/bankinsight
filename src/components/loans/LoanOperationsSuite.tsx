import React, { useMemo, useState } from 'react';
import { useLoans } from '../../hooks/useApi';
import { loanService, Loan } from '../../services/loanService';

interface Props {
  loans: Loan[];
  onReload: () => Promise<void>;
}

export default function LoanOperationsSuite({ loans, onReload }: Props) {
  const {
    loading,
    error,
    applyLoan,
    appraiseLoan,
    approveLoan,
    checkCredit,
    getDelinquencyDashboard,
    getProfitabilityReport,
    getBalanceSheetReport,
    getGlPostings,
  } = useLoans();

  const [active, setActive] = useState<'products' | 'workflow' | 'credit' | 'repayment' | 'delinquency' | 'postings' | 'pnl' | 'balancesheet'>('products');

  const [loanId, setLoanId] = useState('');
  const [customerId, setCustomerId] = useState('CUST0001');
  const [creditResult, setCreditResult] = useState<any>(null);
  const [delinquency, setDelinquency] = useState<any>(null);
  const [postings, setPostings] = useState<any[]>([]);
  const [pnl, setPnl] = useState<any>(null);
  const [balanceSheet, setBalanceSheet] = useState<any>(null);

  const [productForm, setProductForm] = useState({
    id: 'LP_CONS_MONTHLY_EXT',
    code: 'CONS_MONTHLY_EXT',
    name: 'Monthly Consumer Loans (Extended)',
    productType: 'MonthlyConsumerLoan',
    interestMethod: 'ReducingBalance',
    repaymentFrequency: 'Monthly',
    termInPeriods: 12,
    annualInterestRate: 22,
    minAmount: 200,
    maxAmount: 150000,
  });

  const [workflowForm, setWorkflowForm] = useState({
    customerId: 'CUST0001',
    loanProductId: 'LP_CONS_MONTHLY',
    principal: 900,
    annualInterestRate: 22,
    termInPeriods: 6,
    interestMethod: 'ReducingBalance',
    repaymentFrequency: 'Monthly',
    scheduleType: 'Monthly',
  });

  const [repaymentForm, setRepaymentForm] = useState({
    loanId: '',
    accountId: 'ACC0001',
    amount: 100,
  });

  const activeLoanOptions = useMemo(() => loans.filter(l => l.status !== 'CLOSED'), [loans]);

  const runProductConfig = async () => {
    await loanService.configureLoanProduct(productForm as any);
    await onReload();
    alert('Loan product configuration saved.');
  };

  const runWorkflow = async () => {
    const applied = await applyLoan({ ...workflowForm, clientReference: `WEB-${Date.now()}` } as any);
    await appraiseLoan({ loanId: applied.id, decision: 'Reviewed', notes: 'Appraisal complete' });
    await approveLoan({ loanId: applied.id, decisionNotes: 'Maker-checker approved' });
    await loanService.disburseLoan({ loanId: applied.id, clientReference: `WEB-DSB-${Date.now()}` });
    await onReload();
    setLoanId(applied.id);
    alert(`Workflow complete for ${applied.id}`);
  };

  const runCredit = async () => {
    const res = await checkCredit({ customerId, loanId: loanId || undefined });
    setCreditResult(res);
  };

  const runRepayment = async () => {
    await loanService.repayLoanUnified({ ...repaymentForm, clientReference: `WEB-RPY-${Date.now()}` } as any);
    await onReload();
    alert('Repayment posted');
  };

  const loadDelinquency = async () => {
    const res = await getDelinquencyDashboard();
    setDelinquency(res);
  };

  const loadPostings = async () => {
    if (!loanId) return;
    const res = await getGlPostings(loanId);
    setPostings(res);
  };

  const loadPnl = async () => {
    const toDate = new Date().toISOString().slice(0, 10);
    const fromDate = new Date(Date.now() - 1000 * 60 * 60 * 24 * 30).toISOString().slice(0, 10);
    const res = await getProfitabilityReport(fromDate, toDate);
    setPnl(res);
  };

  const loadBalanceSheet = async () => {
    const asOf = new Date().toISOString().slice(0, 10);
    const res = await getBalanceSheetReport(asOf);
    setBalanceSheet(res);
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        {['products', 'workflow', 'credit', 'repayment', 'delinquency', 'postings', 'pnl', 'balancesheet'].map((tab) => (
          <button
            key={tab}
            onClick={() => setActive(tab as any)}
            className={`px-3 py-1 rounded text-sm border ${active === tab ? 'bg-blue-600 text-white border-blue-600' : 'bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-700'}`}
          >
            {tab === 'pnl' ? 'P&L' : tab === 'balancesheet' ? 'Balance Sheet' : tab.charAt(0).toUpperCase() + tab.slice(1)}
          </button>
        ))}
      </div>

      {error && <div className="text-red-600 text-sm">{error}</div>}

      {active === 'products' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Loan Product Configuration</h4>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <input className="px-3 py-2 rounded border" value={productForm.id} onChange={e => setProductForm({ ...productForm, id: e.target.value })} placeholder="Product ID" />
            <input className="px-3 py-2 rounded border" value={productForm.code} onChange={e => setProductForm({ ...productForm, code: e.target.value })} placeholder="Code" />
            <input className="px-3 py-2 rounded border md:col-span-2" value={productForm.name} onChange={e => setProductForm({ ...productForm, name: e.target.value })} placeholder="Name" />
            <select className="px-3 py-2 rounded border" value={productForm.productType} onChange={e => setProductForm({ ...productForm, productType: e.target.value as any })}>
              <option value="DigitalLoan30Days">Digital 30 Days</option>
              <option value="WeeklyGroupLoan">Weekly Group</option>
              <option value="MonthlyConsumerLoan">Monthly Consumer</option>
              <option value="MonthlyBusinessLoan">Monthly Business</option>
            </select>
            <select className="px-3 py-2 rounded border" value={productForm.interestMethod} onChange={e => setProductForm({ ...productForm, interestMethod: e.target.value as any })}>
              <option value="Flat">Flat</option>
              <option value="ReducingBalance">Reducing Balance</option>
            </select>
            <button disabled={loading} onClick={runProductConfig} className="px-4 py-2 bg-blue-600 text-white rounded">Save Product</button>
          </div>
        </div>
      )}

      {active === 'workflow' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Application + Approval Workflow</h4>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <input className="px-3 py-2 rounded border" value={workflowForm.customerId} onChange={e => setWorkflowForm({ ...workflowForm, customerId: e.target.value })} placeholder="Customer ID" />
            <input className="px-3 py-2 rounded border" value={workflowForm.loanProductId} onChange={e => setWorkflowForm({ ...workflowForm, loanProductId: e.target.value })} placeholder="Loan Product ID" />
            <input className="px-3 py-2 rounded border" type="number" value={workflowForm.principal} onChange={e => setWorkflowForm({ ...workflowForm, principal: Number(e.target.value) })} placeholder="Principal" />
          </div>
          <button disabled={loading} onClick={runWorkflow} className="px-4 py-2 bg-blue-600 text-white rounded">Run Apply → Appraise → Approve → Disburse</button>
          {loanId && <div className="text-sm">Latest Loan ID: <span className="font-mono">{loanId}</span></div>}
        </div>
      )}

      {active === 'credit' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Credit Bureau Inquiry</h4>
          <div className="flex gap-2">
            <input className="px-3 py-2 rounded border" value={customerId} onChange={e => setCustomerId(e.target.value)} placeholder="Customer ID" />
            <input className="px-3 py-2 rounded border" value={loanId} onChange={e => setLoanId(e.target.value)} placeholder="Loan ID (optional)" />
            <button disabled={loading} onClick={runCredit} className="px-4 py-2 bg-blue-600 text-white rounded">Check Credit</button>
          </div>
          {creditResult && (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
              <div className="p-2 border rounded"><div className="text-gray-500">Score</div><div className="font-semibold">{creditResult.score}</div></div>
              <div className="p-2 border rounded"><div className="text-gray-500">Risk Grade</div><div className="font-semibold">{creditResult.riskGrade}</div></div>
              <div className="p-2 border rounded"><div className="text-gray-500">Decision</div><div className="font-semibold">{creditResult.decision}</div></div>
              <div className="p-2 border rounded"><div className="text-gray-500">Provider</div><div className="font-semibold">{creditResult.providerName}</div></div>
            </div>
          )}
        </div>
      )}

      {active === 'repayment' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Repayment Entry</h4>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <select className="px-3 py-2 rounded border" value={repaymentForm.loanId} onChange={e => setRepaymentForm({ ...repaymentForm, loanId: e.target.value })}>
              <option value="">Select Loan</option>
              {activeLoanOptions.map(l => <option key={l.id} value={l.id}>{l.id}</option>)}
            </select>
            <input className="px-3 py-2 rounded border" value={repaymentForm.accountId} onChange={e => setRepaymentForm({ ...repaymentForm, accountId: e.target.value })} placeholder="Account ID" />
            <input className="px-3 py-2 rounded border" type="number" value={repaymentForm.amount} onChange={e => setRepaymentForm({ ...repaymentForm, amount: Number(e.target.value) })} placeholder="Amount" />
          </div>
          <button disabled={loading || !repaymentForm.loanId} onClick={runRepayment} className="px-4 py-2 bg-blue-600 text-white rounded">Post Repayment</button>
        </div>
      )}

      {active === 'delinquency' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Delinquency Dashboard</h4>
          <button disabled={loading} onClick={loadDelinquency} className="px-4 py-2 bg-blue-600 text-white rounded">Load Dashboard</button>
          {delinquency && (
            <div className="grid grid-cols-2 md:grid-cols-5 gap-3 text-sm">
              <div className="p-2 border rounded"><div>Total Active</div><div className="font-semibold">{delinquency.totalActiveLoans}</div></div>
              <div className="p-2 border rounded"><div>Non-Accrual</div><div className="font-semibold">{delinquency.nonAccrualLoans}</div></div>
              <div className="p-2 border rounded"><div>PAR 30</div><div className="font-semibold">{delinquency.portfolioAtRisk30}%</div></div>
              <div className="p-2 border rounded"><div>PAR 90</div><div className="font-semibold">{delinquency.portfolioAtRisk90}%</div></div>
              <div className="p-2 border rounded"><div>Aging</div><div className="font-semibold">{Object.entries(delinquency.agingBuckets || {}).map(([k, v]) => `${k}:${v}`).join(' | ')}</div></div>
            </div>
          )}
        </div>
      )}

      {active === 'postings' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Loan Accounting Postings Viewer</h4>
          <div className="flex gap-2">
            <input className="px-3 py-2 rounded border" value={loanId} onChange={e => setLoanId(e.target.value)} placeholder="Loan ID" />
            <button disabled={loading || !loanId} onClick={loadPostings} className="px-4 py-2 bg-blue-600 text-white rounded">Load GL Postings</button>
          </div>
          <div className="space-y-2">
            {postings.map((entry) => (
              <div key={entry.journalId} className="border rounded p-3 text-sm">
                <div className="font-medium">{entry.journalId} • {entry.reference}</div>
                {(entry.lines || []).map((line: any, index: number) => (
                  <div key={index} className="text-gray-600 dark:text-slate-300">{line.accountCode} | Dr {line.debit} | Cr {line.credit}</div>
                ))}
              </div>
            ))}
          </div>
        </div>
      )}

      {active === 'pnl' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Loan P&L Dashboard Cards</h4>
          <button disabled={loading} onClick={loadPnl} className="px-4 py-2 bg-blue-600 text-white rounded">Load P&L</button>
          {pnl && (
            <div className="grid grid-cols-1 md:grid-cols-5 gap-3 text-sm">
              <div className="p-2 border rounded"><div>Interest Income</div><div className="font-semibold">{(pnl.branchLevel?.[0]?.interestIncome ?? 0).toFixed(2)}</div></div>
              <div className="p-2 border rounded"><div>Processing Fee</div><div className="font-semibold">{(pnl.branchLevel?.[0]?.processingFeeIncome ?? 0).toFixed(2)}</div></div>
              <div className="p-2 border rounded"><div>Penalty Income</div><div className="font-semibold">{(pnl.branchLevel?.[0]?.penaltyIncome ?? 0).toFixed(2)}</div></div>
              <div className="p-2 border rounded"><div>Impairment Expense</div><div className="font-semibold">{(pnl.branchLevel?.[0]?.impairmentExpense ?? 0).toFixed(2)}</div></div>
              <div className="p-2 border rounded"><div>Recovery Income</div><div className="font-semibold">{(pnl.branchLevel?.[0]?.recoveryIncome ?? 0).toFixed(2)}</div></div>
            </div>
          )}
        </div>
      )}

      {active === 'balancesheet' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Loan Balance Sheet Cards</h4>
          <button disabled={loading} onClick={loadBalanceSheet} className="px-4 py-2 bg-blue-600 text-white rounded">Load Balance Sheet</button>
          {balanceSheet && (
            <div className="grid grid-cols-1 md:grid-cols-5 gap-3 text-sm">
              <div className="p-2 border rounded"><div>Gross Portfolio</div><div className="font-semibold">{balanceSheet.total.grossLoanPortfolio?.toFixed(2)}</div></div>
              <div className="p-2 border rounded"><div>Interest Receivable</div><div className="font-semibold">{balanceSheet.total.accruedInterestReceivable?.toFixed(2)}</div></div>
              <div className="p-2 border rounded"><div>Penalty Receivable</div><div className="font-semibold">{balanceSheet.total.accruedPenaltyReceivable?.toFixed(2)}</div></div>
              <div className="p-2 border rounded"><div>Impairment Allowance</div><div className="font-semibold">{balanceSheet.total.impairmentAllowance?.toFixed(2)}</div></div>
              <div className="p-2 border rounded"><div>Net Portfolio</div><div className="font-semibold">{balanceSheet.total.netLoanPortfolio?.toFixed(2)}</div></div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
