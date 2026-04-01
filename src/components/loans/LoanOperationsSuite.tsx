import React, { useEffect, useMemo, useState } from 'react';
import { useLoans } from '../../hooks/useApi';
import { loanService, Loan, LoanClassificationResult, LoanPenaltyResult, LoanProductDefinition } from '../../services/loanService';
import { authService } from '../../services/authService';
import { Permissions } from '../../../lib/Permissions';
import { Account, Customer } from '../../../types';

interface Props {
  loans: Loan[];
  customers?: Customer[];
  accounts?: Account[];
  onReload: () => Promise<void>;
}

const formatCurrency = (value?: number, currency = 'GHS') =>
  new Intl.NumberFormat('en-GH', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value || 0);

const loanOptionLabel = (loan: Loan, customerName?: string) =>
  `${loan.id} | ${customerName || loan.cif} | ${loan.productName || loan.productCode || 'Loan'} | ${loan.status}`;

const accountOptionLabel = (account: Account) =>
  `${account.id} | ${account.type} | ${formatCurrency(account.balance, account.currency)} | ${account.cif}`;

export default function LoanOperationsSuite({ loans, customers = [], accounts = [], onReload }: Props) {
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

  const currentUser = authService.getUser();
  const isSuperAdmin = currentUser?.role === 'Administrator';
  const canConfigureProducts = isSuperAdmin || Boolean(currentUser?.permissions?.includes(Permissions.Loans.ConfigureProducts));
  const availableTabs = (canConfigureProducts
    ? ['products', 'workflow', 'servicing', 'credit', 'repayment', 'delinquency', 'postings', 'pnl', 'balancesheet']
    : ['workflow', 'servicing', 'credit', 'repayment', 'delinquency', 'postings', 'pnl', 'balancesheet']) as Array<'products' | 'workflow' | 'servicing' | 'credit' | 'repayment' | 'delinquency' | 'postings' | 'pnl' | 'balancesheet'>;
  const [active, setActive] = useState<'products' | 'workflow' | 'servicing' | 'credit' | 'repayment' | 'delinquency' | 'postings' | 'pnl' | 'balancesheet'>(canConfigureProducts ? 'products' : 'workflow');
  const [loanId, setLoanId] = useState('');
  const [customerId, setCustomerId] = useState('');
  const [creditResult, setCreditResult] = useState<any>(null);
  const [delinquency, setDelinquency] = useState<any>(null);
  const [postings, setPostings] = useState<any[]>([]);
  const [pnl, setPnl] = useState<any>(null);
  const [balanceSheet, setBalanceSheet] = useState<any>(null);
  const [loanProducts, setLoanProducts] = useState<LoanProductDefinition[]>([]);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [penaltyResult, setPenaltyResult] = useState<LoanPenaltyResult | null>(null);
  const [classificationResult, setClassificationResult] = useState<LoanClassificationResult | null>(null);

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
    customerId: '',
    loanProductId: 'LP_CONS_MONTHLY',
    principal: 900,
  });

  const [repaymentForm, setRepaymentForm] = useState({
    loanId: '',
    accountId: '',
    amount: 100,
  });
  const [servicingForm, setServicingForm] = useState({
    loanId: '',
    servicingAccountId: '',
    collateralAccountId: '',
    clientReference: '',
    penaltyRate: 5,
    reason: 'Past due penalty',
  });

  const activeLoanOptions = useMemo(() => loans.filter(l => l.status !== 'CLOSED'), [loans]);
  const approvedLoanOptions = useMemo(() => loans.filter(l => ['APPROVED', 'ACTIVE', 'DISBURSED'].includes(String(l.status || '').toUpperCase())), [loans]);
  const customerNameById = useMemo(() => new Map(customers.map((customer) => [customer.id, customer.name])), [customers]);
  const selectedWorkflowProduct = useMemo(
    () => loanProducts.find(product => product.id === workflowForm.loanProductId) || null,
    [loanProducts, workflowForm.loanProductId],
  );
  const selectedWorkflowCustomer = useMemo(
    () => customers.find(customer => customer.id === workflowForm.customerId) || null,
    [customers, workflowForm.customerId],
  );
  const selectedServicingLoan = useMemo(
    () => loans.find(loan => loan.id === servicingForm.loanId) || null,
    [loans, servicingForm.loanId],
  );
  const selectedRepaymentLoan = useMemo(
    () => loans.find(loan => loan.id === repaymentForm.loanId) || null,
    [loans, repaymentForm.loanId],
  );
  const relatedAccounts = useMemo(
    () => (selectedServicingLoan ? accounts.filter(account => account.cif === selectedServicingLoan.cif) : []),
    [accounts, selectedServicingLoan],
  );
  const repaymentAccounts = useMemo(
    () => (selectedRepaymentLoan ? accounts.filter(account => account.cif === selectedRepaymentLoan.cif) : []),
    [accounts, selectedRepaymentLoan],
  );

  useEffect(() => {
    const loadLoanProducts = async () => {
      try {
        const data = await loanService.getLoanProducts();
        const nextProducts = Array.isArray(data) ? data : [];
        setLoanProducts(nextProducts);
        if (nextProducts.length > 0 && !nextProducts.some(product => product.id === workflowForm.loanProductId)) {
          setWorkflowForm(current => ({ ...current, loanProductId: nextProducts[0].id }));
        }
      } catch {
        setLoanProducts([]);
      }
    };

    void loadLoanProducts();
  }, [workflowForm.loanProductId]);

  useEffect(() => {
    if (!canConfigureProducts && active === 'products') {
      setActive('workflow');
    }
  }, [active, canConfigureProducts]);

  useEffect(() => {
    if (!workflowForm.customerId && customers.length > 0) {
      setWorkflowForm(current => ({ ...current, customerId: customers[0].id }));
    }
    if (!customerId && customers.length > 0) {
      setCustomerId(customers[0].id);
    }
  }, [customerId, customers, workflowForm.customerId]);

  useEffect(() => {
    if (repaymentAccounts.length > 0 && !repaymentAccounts.some(account => account.id === repaymentForm.accountId)) {
      setRepaymentForm(current => ({ ...current, accountId: repaymentAccounts[0].id }));
    }
  }, [repaymentAccounts, repaymentForm.accountId]);

  useEffect(() => {
    if (relatedAccounts.length > 0 && !relatedAccounts.some(account => account.id === servicingForm.servicingAccountId)) {
      setServicingForm(current => ({ ...current, servicingAccountId: relatedAccounts[0].id }));
    }
  }, [relatedAccounts, servicingForm.servicingAccountId]);

  const runProductConfig = async () => {
    await loanService.configureLoanProduct(productForm as any);
    await onReload();
    const nextProducts = await loanService.getLoanProducts().catch(() => loanProducts);
    setLoanProducts(Array.isArray(nextProducts) ? nextProducts : loanProducts);
    setStatusMessage('Loan product definition saved. Product parameters are now enforced in workflow and origination.');
  };

  const runWorkflow = async () => {
    const applied = await applyLoan({ ...workflowForm, clientReference: `WEB-${Date.now()}` } as any);
    await appraiseLoan({ loanId: applied.id, decision: 'Reviewed', notes: 'Appraisal complete' });
    await approveLoan({ loanId: applied.id, decisionNotes: 'Maker-checker approved' });
    await loanService.disburseLoan({ loanId: applied.id, clientReference: `WEB-DSB-${Date.now()}` });
    await onReload();
    setLoanId(applied.id);
    setStatusMessage(`Workflow complete for ${applied.id}. Product policy was enforced from ${workflowForm.loanProductId}.`);
  };

  const runCredit = async () => {
    const res = await checkCredit({ customerId, loanId: loanId || undefined });
    setCreditResult(res);
  };

  const runRepayment = async () => {
    await loanService.repayLoanUnified({ ...repaymentForm, clientReference: `WEB-RPY-${Date.now()}` } as any);
    await onReload();
    setStatusMessage('Repayment posted.');
  };

  const runDisbursement = async () => {
    if (!servicingForm.loanId) return;
    await loanService.disburseLoan({
      loanId: servicingForm.loanId,
      clientReference: servicingForm.clientReference || `WEB-DSB-${Date.now()}`,
      servicingAccountId: servicingForm.servicingAccountId || undefined,
      collateralAccountId: servicingForm.collateralAccountId || undefined,
    });
    await onReload();
    setStatusMessage(`Loan ${servicingForm.loanId} disbursed successfully.`);
  };

  const runPenaltyAssessment = async () => {
    if (!servicingForm.loanId) return;
    const result = await loanService.assessPenalty(servicingForm.loanId, {
      penaltyRate: Number(servicingForm.penaltyRate),
      reason: servicingForm.reason,
      clientReference: servicingForm.clientReference || undefined,
    });
    setPenaltyResult(result);
    setStatusMessage(`Penalty assessed for ${servicingForm.loanId}.`);
    await onReload();
  };

  const runClassification = async () => {
    if (!servicingForm.loanId) return;
    const result = await loanService.classifyLoan(servicingForm.loanId);
    setClassificationResult(result);
    setStatusMessage(`Classification completed for ${servicingForm.loanId}.`);
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
        {availableTabs.map((tab) => (
          <button
            key={tab}
            onClick={() => setActive(tab as any)}
            className={`px-3 py-1 rounded text-sm border ${active === tab ? 'bg-blue-600 text-white border-blue-600' : 'bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-700'}`}
          >
            {tab === 'pnl' ? 'P&L' : tab === 'balancesheet' ? 'Balance Sheet' : tab === 'servicing' ? 'Servicing' : tab.charAt(0).toUpperCase() + tab.slice(1)}
          </button>
        ))}
      </div>

      {error && <div className="text-red-600 text-sm">{error}</div>}
      {statusMessage && <div className="text-sm text-blue-700 dark:text-blue-300">{statusMessage}</div>}

      {active === 'products' && canConfigureProducts && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Loan Product Definition</h4>
          <div className="text-xs text-slate-500 dark:text-slate-400">Super-admin business governance flow. Pricing, tenor, repayment frequency, and interest method are edited here and enforced in every other loan workflow.</div>
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
            <select className="px-3 py-2 rounded border" value={workflowForm.customerId} onChange={e => setWorkflowForm({ ...workflowForm, customerId: e.target.value })}>
              <option value="">Select customer</option>
              {customers.map(customer => <option key={customer.id} value={customer.id}>{customer.name} | {customer.id}</option>)}
            </select>
            <select className="px-3 py-2 rounded border" value={workflowForm.loanProductId} onChange={e => setWorkflowForm({ ...workflowForm, loanProductId: e.target.value })}>
              {loanProducts.map(product => <option key={product.id} value={product.id}>{product.name}</option>)}
            </select>
            <input className="px-3 py-2 rounded border" type="number" value={workflowForm.principal} onChange={e => setWorkflowForm({ ...workflowForm, principal: Number(e.target.value) })} placeholder="Principal" />
          </div>
          {selectedWorkflowCustomer && <div className="rounded border p-3 text-sm text-slate-600 dark:text-slate-300">Borrower: <span className="font-semibold text-slate-900 dark:text-white">{selectedWorkflowCustomer.name}</span> | CIF {selectedWorkflowCustomer.id}</div>}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3 text-sm">
            <div className="p-2 border rounded"><div className="text-gray-500">Annual Rate</div><div className="font-semibold">{selectedWorkflowProduct?.annualInterestRate ?? 0}%</div></div>
            <div className="p-2 border rounded"><div className="text-gray-500">Term</div><div className="font-semibold">{selectedWorkflowProduct?.termInPeriods ?? 0} periods</div></div>
            <div className="p-2 border rounded"><div className="text-gray-500">Repayment</div><div className="font-semibold">{selectedWorkflowProduct?.repaymentFrequency ?? 'N/A'}</div></div>
            <div className="p-2 border rounded"><div className="text-gray-500">Interest Method</div><div className="font-semibold">{selectedWorkflowProduct?.interestMethod ?? 'N/A'}</div></div>
          </div>
          <div className="text-xs text-slate-500 dark:text-slate-400">Product parameters are read-only here and managed only from the Product Definition tab.</div>
          <button disabled={loading || !workflowForm.loanProductId || !workflowForm.customerId} onClick={runWorkflow} className="px-4 py-2 bg-blue-600 text-white rounded">Run Apply {'->'} Appraise {'->'} Approve {'->'} Disburse</button>
          {loanId && <div className="text-sm">Latest Loan ID: <span className="font-mono">{loanId}</span></div>}
        </div>
      )}

      {active === 'servicing' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-4">
          <h4 className="font-semibold">Disbursement and Servicing Controls</h4>
          <div className="text-xs text-slate-500 dark:text-slate-400">
            Operations desk for approved facilities. Use servicing and collateral accounts during disbursement, then run penalty and classification controls as the loan seasons.
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <select className="px-3 py-2 rounded border" value={servicingForm.loanId} onChange={e => setServicingForm({ ...servicingForm, loanId: e.target.value })}>
              <option value="">Select Loan</option>
              {approvedLoanOptions.map(loan => (
                <option key={loan.id} value={loan.id}>{loanOptionLabel(loan, customerNameById.get(loan.cif))}</option>
              ))}
            </select>
            <input className="px-3 py-2 rounded border" value={servicingForm.clientReference} onChange={e => setServicingForm({ ...servicingForm, clientReference: e.target.value })} placeholder="Client reference" />
            <select className="px-3 py-2 rounded border" value={servicingForm.servicingAccountId} onChange={e => setServicingForm({ ...servicingForm, servicingAccountId: e.target.value })}>
              <option value="">Select servicing account</option>
              {relatedAccounts.map(account => <option key={account.id} value={account.id}>{accountOptionLabel(account)}</option>)}
            </select>
            <select className="px-3 py-2 rounded border" value={servicingForm.collateralAccountId} onChange={e => setServicingForm({ ...servicingForm, collateralAccountId: e.target.value })}>
              <option value="">Select collateral account</option>
              {relatedAccounts.map(account => <option key={account.id} value={account.id}>{accountOptionLabel(account)}</option>)}
            </select>
            <input className="px-3 py-2 rounded border" type="number" min="0" step="0.01" value={servicingForm.penaltyRate} onChange={e => setServicingForm({ ...servicingForm, penaltyRate: Number(e.target.value) })} placeholder="Penalty rate" />
            <input className="px-3 py-2 rounded border" value={servicingForm.reason} onChange={e => setServicingForm({ ...servicingForm, reason: e.target.value })} placeholder="Penalty reason" />
          </div>

          {selectedServicingLoan && (
            <div className="grid grid-cols-1 md:grid-cols-4 gap-3 text-sm">
              <div className="p-3 border rounded"><div className="text-gray-500">Status</div><div className="font-semibold">{selectedServicingLoan.status}</div></div>
              <div className="p-3 border rounded"><div className="text-gray-500">Principal</div><div className="font-semibold">{Number(selectedServicingLoan.principal || 0).toFixed(2)}</div></div>
              <div className="p-3 border rounded"><div className="text-gray-500">Outstanding</div><div className="font-semibold">{Number(selectedServicingLoan.outstandingBalance || 0).toFixed(2)}</div></div>
              <div className="p-3 border rounded"><div className="text-gray-500">PAR</div><div className="font-semibold">{selectedServicingLoan.parBucket || '0'}</div></div>
            </div>
          )}

          <div className="flex flex-wrap gap-2">
            <button disabled={loading || !servicingForm.loanId} onClick={runDisbursement} className="px-4 py-2 bg-green-600 text-white rounded disabled:opacity-60">Disburse Loan</button>
            <button disabled={loading || !servicingForm.loanId} onClick={runPenaltyAssessment} className="px-4 py-2 bg-amber-500 text-white rounded disabled:opacity-60">Assess Penalty</button>
            <button disabled={loading || !servicingForm.loanId} onClick={runClassification} className="px-4 py-2 bg-blue-600 text-white rounded disabled:opacity-60">Classify Loan</button>
          </div>

          {(penaltyResult || classificationResult) && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
              <div className="p-3 border rounded">
                <div className="font-medium mb-2">Penalty Result</div>
                {penaltyResult ? (
                  <div className="space-y-1">
                    <div>Amount: <span className="font-semibold">{Number(penaltyResult.penaltyAmount || 0).toFixed(2)}</span></div>
                    <div>Rate: <span className="font-semibold">{penaltyResult.penaltyRate}%</span></div>
                    <div>Days Past Due: <span className="font-semibold">{penaltyResult.daysPastDue}</span></div>
                  </div>
                ) : (
                  <div className="text-slate-500 dark:text-slate-400">No penalty assessment loaded yet.</div>
                )}
              </div>
              <div className="p-3 border rounded">
                <div className="font-medium mb-2">Classification Result</div>
                {classificationResult ? (
                  <div className="space-y-1">
                    <div>BoG Tier: <span className="font-semibold">{classificationResult.bogTier}</span></div>
                    <div>Provisioning Amount: <span className="font-semibold">{Number(classificationResult.provisioningAmount || 0).toFixed(2)}</span></div>
                    <div>Provisioning Rate: <span className="font-semibold">{Number(classificationResult.provisioningRate || 0) * 100}%</span></div>
                  </div>
                ) : (
                  <div className="text-slate-500 dark:text-slate-400">No classification loaded yet.</div>
                )}
              </div>
            </div>
          )}
        </div>
      )}

      {active === 'credit' && (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold">Credit Bureau Inquiry</h4>
          <div className="flex gap-2">
            <select className="px-3 py-2 rounded border" value={customerId} onChange={e => setCustomerId(e.target.value)}>
              <option value="">Select customer</option>
              {customers.map(customer => <option key={customer.id} value={customer.id}>{customer.name} | {customer.id}</option>)}
            </select>
            <select className="px-3 py-2 rounded border" value={loanId} onChange={e => setLoanId(e.target.value)}>
              <option value="">Select loan (optional)</option>
              {activeLoanOptions.filter(loan => !customerId || loan.cif === customerId).map(loan => <option key={loan.id} value={loan.id}>{loanOptionLabel(loan, customerNameById.get(loan.cif))}</option>)}
            </select>
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
              {activeLoanOptions.map(loan => <option key={loan.id} value={loan.id}>{loanOptionLabel(loan, customerNameById.get(loan.cif))}</option>)}
            </select>
            <select className="px-3 py-2 rounded border" value={repaymentForm.accountId} onChange={e => setRepaymentForm({ ...repaymentForm, accountId: e.target.value })}>
              <option value="">Select settlement account</option>
              {repaymentAccounts.map(account => <option key={account.id} value={account.id}>{accountOptionLabel(account)}</option>)}
            </select>
            <input className="px-3 py-2 rounded border" type="number" value={repaymentForm.amount} onChange={e => setRepaymentForm({ ...repaymentForm, amount: Number(e.target.value) })} placeholder="Amount" />
          </div>
          <button disabled={loading || !repaymentForm.loanId || !repaymentForm.accountId} onClick={runRepayment} className="px-4 py-2 bg-blue-600 text-white rounded">Post Repayment</button>
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
            <select className="px-3 py-2 rounded border" value={loanId} onChange={e => setLoanId(e.target.value)}>
              <option value="">Select loan</option>
              {activeLoanOptions.map(loan => <option key={loan.id} value={loan.id}>{loanOptionLabel(loan, customerNameById.get(loan.cif))}</option>)}
            </select>
            <button disabled={loading || !loanId} onClick={loadPostings} className="px-4 py-2 bg-blue-600 text-white rounded">Load GL Postings</button>
          </div>
          <div className="space-y-2">
            {postings.map((entry) => (
              <div key={entry.journalId} className="border rounded p-3 text-sm">
                <div className="font-medium">{entry.journalId} - {entry.reference}</div>
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
