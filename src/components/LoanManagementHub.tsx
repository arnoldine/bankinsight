import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  CheckCircle2,
  Clock3,
  PlusCircle,
  RefreshCw,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Wallet,
  X,
} from 'lucide-react';
import { Customer } from '../../types';
import { Can } from '../../components/Can';
import { Permissions } from '../../lib/Permissions';
import { useLoans } from '../hooks/useApi';
import LoanOperationsSuite from './loans/LoanOperationsSuite';
import { loanService, Loan, LoanProductDefinition, LoanScheduleDto } from '../services/loanService';

interface LoanManagementHubProps {
  loans?: Loan[];
  customers?: Customer[];
  onDisburseLoan?: (data: any) => void;
  onRepayLoan?: (loanId: string, data: any) => void;
  initialTab?: 'portfolio' | 'origination' | 'repayment' | 'schedule' | 'operations';
}

type LoanTab = 'portfolio' | 'origination' | 'repayment' | 'schedule' | 'operations';
type BannerTone = 'success' | 'error' | 'info';

type BannerState = {
  tone: BannerTone;
  text: string;
} | null;

const FALLBACK_PRODUCT_OPTIONS: LoanProductDefinition[] = [
  {
    id: 'LP_CONS_MONTHLY',
    code: 'CONS_MONTHLY',
    name: 'Monthly Consumer Loans',
    productType: 'MonthlyConsumerLoan',
    repaymentFrequency: 'Monthly',
    interestMethod: 'Flat',
    annualInterestRate: 22,
    termInPeriods: 18,
    minAmount: 200,
    maxAmount: 100000,
    isActive: true,
  },
  {
    id: 'LP_BIZ_MONTHLY',
    code: 'BIZ_MONTHLY',
    name: 'Monthly Business Loans',
    productType: 'MonthlyBusinessLoan',
    repaymentFrequency: 'Monthly',
    interestMethod: 'ReducingBalance',
    annualInterestRate: 18,
    termInPeriods: 24,
    minAmount: 500,
    maxAmount: 250000,
    isActive: true,
  },
  {
    id: 'LP_GROUP_WEEKLY',
    code: 'GROUP_WEEKLY',
    name: 'Weekly Group Loans',
    productType: 'WeeklyGroupLoan',
    repaymentFrequency: 'Weekly',
    interestMethod: 'ReducingBalance',
    annualInterestRate: 20,
    termInPeriods: 24,
    minAmount: 100,
    maxAmount: 50000,
    isActive: true,
  },
];

const formatCurrency = (value?: number) =>
  new Intl.NumberFormat('en-GH', {
    style: 'currency',
    currency: 'GHS',
    minimumFractionDigits: 2,
  }).format(value || 0);

const normalizeStatus = (status?: string) => String(status || '').toUpperCase();

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error && typeof error === 'object') {
    const maybe = error as { data?: { message?: string; error?: string }; message?: string };
    return maybe.data?.message || maybe.data?.error || maybe.message || fallback;
  }
  return fallback;
};

const badgeTone = (status?: string) => {
  const normalized = normalizeStatus(status);
  if (['ACTIVE', 'APPROVED', 'DISBURSED', 'CURRENT'].includes(normalized)) {
    return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300';
  }
  if (['PENDING', 'PENDING_APPROVAL', 'APPRAISED'].includes(normalized)) {
    return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300';
  }
  if (['WRITTEN_OFF', 'CLOSED'].includes(normalized)) {
    return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300';
  }
  return 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-300';
};

const bannerToneClass = (tone: BannerTone) =>
  ({
    success: 'border-green-200 bg-green-50 text-green-800 dark:border-green-900/60 dark:bg-green-950/30 dark:text-green-200',
    error: 'border-rose-200 bg-rose-50 text-rose-800 dark:border-rose-900/60 dark:bg-rose-950/30 dark:text-rose-200',
    info: 'border-blue-200 bg-blue-50 text-blue-800 dark:border-blue-900/60 dark:bg-blue-950/30 dark:text-blue-200',
  })[tone];

export default function LoanManagementHub({
  loans: initialLoans = [],
  customers: initialCustomers = [],
  onDisburseLoan,
  onRepayLoan,
  initialTab = 'portfolio',
}: LoanManagementHubProps) {
  const {
    getLoans,
    repayLoan,
    getLoanSchedule,
    applyLoan,
    appraiseLoan,
    approveLoan,
    checkCredit,
    loading,
    error,
  } = useLoans();

  const [loans, setLoans] = useState<Loan[]>(initialLoans || []);
  const [loanProducts, setLoanProducts] = useState<LoanProductDefinition[]>(FALLBACK_PRODUCT_OPTIONS);
  const [customers] = useState(initialCustomers || []);
  const [activeTab, setActiveTab] = useState<LoanTab>(initialTab);
  const [selectedLoanId, setSelectedLoanId] = useState<string>('');
  const [selectedReviewLoanId, setSelectedReviewLoanId] = useState<string>('');
  const [schedule, setSchedule] = useState<LoanScheduleDto[]>([]);
  const [previewSchedule, setPreviewSchedule] = useState<any[]>([]);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [creditLoading, setCreditLoading] = useState(false);
  const [workflowBusy, setWorkflowBusy] = useState<string | null>(null);
  const [banner, setBanner] = useState<BannerState>(null);

  const [origCif, setOrigCif] = useState('');
  const [origProduct, setOrigProduct] = useState<string>('LP_CONS_MONTHLY');
  const [origPrincipal, setOrigPrincipal] = useState('');
  const [origCollateralType, setOrigCollateralType] = useState('');
  const [origCollateralValue, setOrigCollateralValue] = useState('');
  const [approvalNotes, setApprovalNotes] = useState('');

  const [repayAmount, setRepayAmount] = useState('');
  const [repayAccountId, setRepayAccountId] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [parFilter, setParFilter] = useState<'ALL' | 'CURRENT' | 'ARREARS'>('ALL');
  const [creditResult, setCreditResult] = useState<any>(null);

  useEffect(() => {
    setActiveTab(initialTab);
  }, [initialTab]);

  useEffect(() => {
    if (initialLoans && initialLoans.length > 0) {
      setLoans(initialLoans);
    } else {
      loadData();
    }
  }, [initialLoans]);

  const customerMap = useMemo(
    () => new Map(customers.map((customer) => [customer.id, customer.name])),
    [customers],
  );

  const selectedProductDefinition = useMemo(
    () => loanProducts.find((product) => product.id === origProduct) || loanProducts[0] || null,
    [loanProducts, origProduct],
  );

  const selectedLoan = useMemo(
    () => loans.find((loan) => loan.id === selectedLoanId) || null,
    [loans, selectedLoanId],
  );

  const selectedReviewLoan = useMemo(
    () => loans.find((loan) => loan.id === selectedReviewLoanId) || null,
    [loans, selectedReviewLoanId],
  );

  const resolveCustomerId = (input: string) => {
    const normalized = input.trim().toLowerCase();
    if (!normalized) {
      return null;
    }

    const exactMatch = customers.find((customer) => {
      const customerId = String(customer.id || '').trim().toLowerCase();
      const customerName = String(customer.name || '').trim().toLowerCase();
      const customerCif = String(customer.cif || '').trim().toLowerCase();
      return customerId === normalized || customerCif === normalized || customerName === normalized;
    });

    return exactMatch?.id || null;
  };

  const pendingReviewLoans = useMemo(
    () => loans.filter((loan) => ['PENDING', 'PENDING_APPROVAL', 'APPRAISED', 'APPROVED'].includes(normalizeStatus(loan.status))),
    [loans],
  );

  const activeLoans = useMemo(
    () => loans.filter((loan) => ['ACTIVE', 'APPROVED', 'DISBURSED', 'IN_ARREARS'].includes(normalizeStatus(loan.status))),
    [loans],
  );

  const totalPrincipal = useMemo(
    () => activeLoans.reduce((sum, loan) => sum + Number(loan.principal || 0), 0),
    [activeLoans],
  );

  const totalOutstanding = useMemo(
    () => activeLoans.reduce((sum, loan) => sum + Number(loan.outstandingBalance || 0), 0),
    [activeLoans],
  );

  const arrearsCount = useMemo(
    () => activeLoans.filter((loan) => {
      const bucket = normalizeStatus(loan.parBucket);
      return bucket !== '0' && bucket !== 'PAR_0';
    }).length,
    [activeLoans],
  );

  const collateralizedCount = useMemo(
    () => loans.filter((loan) => Boolean(loan.collateralType)).length,
    [loans],
  );

  const filteredLoans = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return loans.filter((loan) => {
      const customerName = String(customerMap.get(loan.cif) || '').toLowerCase();
      const matchesSearch = !query || [
        loan.id,
        loan.cif,
        loan.productName,
        loan.productCode,
        loan.status,
        loan.parBucket,
        loan.collateralType,
        customerName,
      ].some((value) => String(value || '').toLowerCase().includes(query));

      const matchesStatus = statusFilter === 'ALL' || normalizeStatus(loan.status) === statusFilter;
      const parBucket = normalizeStatus(loan.parBucket);
      const isInArrears = parBucket !== '0' && parBucket !== 'PAR_0';
      const matchesPar = parFilter === 'ALL' || (parFilter === 'ARREARS' ? isInArrears : !isInArrears);

      return matchesSearch && matchesStatus && matchesPar;
    });
  }, [customerMap, loans, parFilter, searchQuery, statusFilter]);

  const upcomingInstallments = useMemo(
    () => schedule.filter((line) => normalizeStatus(line.status) !== 'PAID').slice(0, 3),
    [schedule],
  );

  const loadData = async () => {
    try {
      const [data, productCatalog] = await Promise.all([
        getLoans(),
        loanService.getLoanProducts().catch(() => FALLBACK_PRODUCT_OPTIONS),
      ]);
      const nextLoans = Array.isArray(data) ? data : [];
      const nextProducts = Array.isArray(productCatalog) && productCatalog.length > 0 ? productCatalog : FALLBACK_PRODUCT_OPTIONS;
      setLoans(nextLoans);
      setLoanProducts(nextProducts);

      if (!nextProducts.some((product) => product.id === origProduct)) {
        setOrigProduct(nextProducts[0]?.id || 'LP_CONS_MONTHLY');
      }

      if (!selectedLoanId && nextLoans.length > 0) {
        setSelectedLoanId(nextLoans[0].id);
      }

      if (!selectedReviewLoanId) {
        const nextPending = nextLoans.find((loan) => ['PENDING', 'PENDING_APPROVAL', 'APPRAISED', 'APPROVED'].includes(normalizeStatus(loan.status)));
        if (nextPending) {
          setSelectedReviewLoanId(nextPending.id);
        }
      }
    } catch (loadError) {
      setBanner({ tone: 'error', text: getErrorMessage(loadError, 'Failed to load loan portfolio.') });
    }
  };

  const resetOriginationForm = () => {
    setOrigCif('');
    setOrigProduct(loanProducts[0]?.id || 'LP_CONS_MONTHLY');
    setOrigPrincipal('');
    setOrigCollateralType('');
    setOrigCollateralValue('');
    setPreviewSchedule([]);
    setCreditResult(null);
  };

  const loadSchedule = async (loanId: string) => {
    try {
      const data = await getLoanSchedule(loanId);
      setSchedule(Array.isArray(data) ? data : []);
    } catch (scheduleError) {
      setBanner({ tone: 'error', text: getErrorMessage(scheduleError, 'Failed to load amortization schedule.') });
    }
  };

  const handleTabChange = async (tab: LoanTab) => {
    setActiveTab(tab);
    setBanner(null);
    if ((tab === 'schedule' || tab === 'repayment') && selectedLoanId) {
      await loadSchedule(selectedLoanId);
    }
  };

  const handlePreviewSchedule = async () => {
    if (!origPrincipal || !selectedProductDefinition) {
      setBanner({ tone: 'error', text: 'Select a product and principal before previewing the schedule.' });
      return;
    }

    setPreviewLoading(true);
    setBanner(null);
    try {
      const result = await loanService.generateSchedule({
        loanProductId: selectedProductDefinition.id,
        principal: Number(origPrincipal),
      });

      setPreviewSchedule(Array.isArray(result?.lines) ? result.lines : []);
      setBanner({ tone: 'info', text: 'Repayment preview generated from the selected product definition.' });
    } catch (previewError) {
      setBanner({ tone: 'error', text: getErrorMessage(previewError, 'Unable to preview repayment schedule.') });
    } finally {
      setPreviewLoading(false);
    }
  };

  const handleCreditCheck = async () => {
    const customerId = resolveCustomerId(origCif);
    if (!customerId) {
      setBanner({ tone: 'error', text: 'Select a customer before running a credit check.' });
      return;
    }

    setCreditLoading(true);
    setBanner(null);
    try {
      const result = await checkCredit({ customerId });
      setCreditResult(result);
      setBanner({ tone: 'info', text: `Credit check completed with ${result.decision || 'a review decision'}.` });
    } catch (creditError) {
      setBanner({ tone: 'error', text: getErrorMessage(creditError, 'Unable to run credit check.') });
    } finally {
      setCreditLoading(false);
    }
  };

  const handleSubmitApplication = async (event: React.FormEvent) => {
    event.preventDefault();
    const customerId = resolveCustomerId(origCif);
    if (!customerId) {
      setBanner({ tone: 'error', text: 'Select a valid customer ID before submitting the application.' });
      return;
    }

    setWorkflowBusy('apply');
    setBanner(null);

    try {
      const createdLoan = await applyLoan({
        customerId,
        loanProductId: origProduct,
        principal: Number(origPrincipal),
        clientReference: `WEB-APP-${Date.now()}`,
      });

      setBanner({ tone: 'success', text: `Loan application ${createdLoan.id} submitted into the review queue.` });
      setSelectedReviewLoanId(createdLoan.id);
      setSelectedLoanId(createdLoan.id);
      resetOriginationForm();
      await loadData();
    } catch (applyError) {
      setBanner({ tone: 'error', text: getErrorMessage(applyError, 'Unable to submit loan application.') });
    } finally {
      setWorkflowBusy(null);
    }
  };

  const handleReviewAction = async (action: 'appraise' | 'approve' | 'disburse') => {
    if (!selectedReviewLoanId) {
      setBanner({ tone: 'error', text: 'Select a queued application first.' });
      return;
    }

    setWorkflowBusy(action);
    setBanner(null);
    try {
      if (action === 'appraise') {
        await appraiseLoan({
          loanId: selectedReviewLoanId,
          decision: 'Reviewed',
          notes: approvalNotes || 'Loan reviewed in credit workbench',
        });
      } else if (action === 'approve') {
        await approveLoan({
          loanId: selectedReviewLoanId,
          decisionNotes: approvalNotes || 'Approved in loan management workbench',
        });
      } else {
        const request = {
          loanId: selectedReviewLoanId,
          clientReference: `WEB-DSB-${Date.now()}`,
        };
        await loanService.disburseLoan(request);
        onDisburseLoan?.(request);
      }

      setBanner({
        tone: 'success',
        text:
          action === 'appraise'
            ? `Application ${selectedReviewLoanId} moved through appraisal.`
            : action === 'approve'
              ? `Application ${selectedReviewLoanId} approved successfully.`
              : `Loan ${selectedReviewLoanId} disbursed successfully.`,
      });
      setApprovalNotes('');
      await loadData();
    } catch (workflowError) {
      setBanner({ tone: 'error', text: getErrorMessage(workflowError, `Unable to ${action} the selected loan.`) });
    } finally {
      setWorkflowBusy(null);
    }
  };

  const handleRepay = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedLoanId) {
      setBanner({ tone: 'error', text: 'Select a loan before posting a repayment.' });
      return;
    }

    try {
      const request = {
        amount: Number(repayAmount),
        accountId: repayAccountId,
      };

      await repayLoan(selectedLoanId, request);
      onRepayLoan?.(selectedLoanId, request);
      setRepayAmount('');
      setRepayAccountId('');
      setBanner({ tone: 'success', text: `Repayment posted successfully for ${selectedLoanId}.` });
      await loadData();
      await loadSchedule(selectedLoanId);
    } catch (repaymentError) {
      setBanner({ tone: 'error', text: getErrorMessage(repaymentError, 'Unable to post repayment.') });
    }
  };

  const clearPortfolioFilters = () => {
    setSearchQuery('');
    setStatusFilter('ALL');
    setParFilter('ALL');
  };

  return (
    <div className="simple-screen p-6 space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 dark:text-white">Loan Management</h2>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Production workbench for origination, credit review, disbursement, repayment, and portfolio supervision.
          </p>
        </div>
        <button
          onClick={loadData}
          disabled={loading}
          className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
        >
          <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
          Refresh portfolio
        </button>
      </div>

      {banner && (
        <div className={`rounded-2xl border px-4 py-3 text-sm ${bannerToneClass(banner.tone)}`}>
          {banner.text}
        </div>
      )}

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800 dark:border-rose-900/60 dark:bg-rose-950/30 dark:text-rose-200">
          {error}
        </div>
      )}

      <div className="screen-hero p-6">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
          <div className="screen-stat px-4 py-3">
            <p className="text-xs text-slate-500">Active loans</p>
            <p className="mt-1 text-2xl font-semibold text-slate-900 dark:text-white">{activeLoans.length}</p>
          </div>
          <div className="screen-stat px-4 py-3">
            <p className="text-xs text-slate-500">Pipeline queue</p>
            <p className="mt-1 text-2xl font-semibold text-slate-900 dark:text-white">{pendingReviewLoans.length}</p>
          </div>
          <div className="screen-stat px-4 py-3">
            <p className="text-xs text-slate-500">Gross principal</p>
            <p className="mt-1 text-xl font-semibold text-slate-900 dark:text-white">{formatCurrency(totalPrincipal)}</p>
          </div>
          <div className="screen-stat px-4 py-3">
            <p className="text-xs text-slate-500">Outstanding</p>
            <p className="mt-1 text-xl font-semibold text-slate-900 dark:text-white">{formatCurrency(totalOutstanding)}</p>
          </div>
          <div className="screen-stat px-4 py-3">
            <p className="text-xs text-slate-500">Arrears / collateral</p>
            <p className="mt-1 text-xl font-semibold text-slate-900 dark:text-white">{arrearsCount} / {collateralizedCount}</p>
          </div>
        </div>
      </div>

      <div className="flex gap-4 overflow-x-auto border-b border-slate-200 dark:border-slate-700">
        {[
          ['portfolio', 'Portfolio'],
          ['origination', 'Origination'],
          ['repayment', 'Repayment'],
          ['schedule', 'Schedule'],
          ['operations', 'Operations'],
        ].map(([tabId, label]) => (
          <button
            key={tabId}
            onClick={() => handleTabChange(tabId as LoanTab)}
            className={`border-b-2 px-4 py-2 text-sm font-medium whitespace-nowrap ${
              activeTab === tabId
                ? 'border-blue-500 text-blue-600 dark:text-blue-300'
                : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {activeTab === 'portfolio' && (
        <div className="space-y-4">
          <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
              <div className="flex-1">
                <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Search portfolio</label>
                <div className="relative">
                  <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <input
                    type="text"
                    value={searchQuery}
                    onChange={(event) => setSearchQuery(event.target.value)}
                    placeholder="Client name, CIF, loan ID, product, or collateral"
                    className="w-full rounded-xl border border-slate-200 bg-slate-50 py-3 pl-10 pr-10 text-sm text-slate-900 outline-none transition focus:border-blue-400 focus:bg-white dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                  />
                  {searchQuery && (
                    <button type="button" onClick={() => setSearchQuery('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600" aria-label="Clear search">
                      <X className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3 lg:w-[470px]">
                <div>
                  <label className="mb-2 flex items-center gap-2 text-sm font-medium text-slate-600 dark:text-slate-300">
                    <SlidersHorizontal className="h-4 w-4" /> Status
                  </label>
                  <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3 py-3 text-sm dark:border-slate-700 dark:bg-slate-900">
                    <option value="ALL">All statuses</option>
                    <option value="ACTIVE">Active</option>
                    <option value="PENDING_APPROVAL">Pending approval</option>
                    <option value="APPROVED">Approved</option>
                    <option value="DISBURSED">Disbursed</option>
                    <option value="CLOSED">Closed</option>
                    <option value="WRITTEN_OFF">Written off</option>
                  </select>
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Repayment health</label>
                  <select value={parFilter} onChange={(event) => setParFilter(event.target.value as 'ALL' | 'CURRENT' | 'ARREARS')} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3 py-3 text-sm dark:border-slate-700 dark:bg-slate-900">
                    <option value="ALL">All buckets</option>
                    <option value="CURRENT">Current only</option>
                    <option value="ARREARS">In arrears</option>
                  </select>
                </div>
                <button type="button" onClick={clearPortfolioFilters} className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700">
                  Clear filters
                </button>
              </div>
            </div>
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.55fr_0.95fr]">
            <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
              <div className="border-b border-slate-200 px-5 py-4 text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                Showing <span className="font-semibold text-slate-900 dark:text-white">{filteredLoans.length}</span> of {loans.length} loans
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-left">
                  <thead className="bg-slate-50 text-xs uppercase text-slate-500 dark:bg-slate-900/50 dark:text-slate-400">
                    <tr>
                      <th className="px-5 py-4 font-medium">Customer</th>
                      <th className="px-5 py-4 font-medium">Loan</th>
                      <th className="px-5 py-4 font-medium text-right">Outstanding</th>
                      <th className="px-5 py-4 font-medium">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-200 dark:divide-slate-700/60">
                    {filteredLoans.map((loan) => (
                      <tr
                        key={loan.id}
                        onClick={() => setSelectedLoanId(loan.id)}
                        className={`cursor-pointer transition hover:bg-slate-50 dark:hover:bg-slate-700/30 ${
                          selectedLoanId === loan.id ? 'bg-blue-50/70 dark:bg-blue-900/10' : ''
                        }`}
                      >
                        <td className="px-5 py-4">
                          <div className="font-medium text-slate-900 dark:text-white">{customerMap.get(loan.cif) || loan.cif}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">CIF: {loan.cif}</div>
                        </td>
                        <td className="px-5 py-4">
                          <div className="font-mono text-blue-600 dark:text-blue-300">{loan.id}</div>
                          <div className="text-sm text-slate-600 dark:text-slate-300">{loan.productName || loan.productCode || 'Loan Product'}</div>
                        </td>
                        <td className="px-5 py-4 text-right font-mono text-slate-700 dark:text-slate-200">{formatCurrency(loan.outstandingBalance || 0)}</td>
                        <td className="px-5 py-4">
                          <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${badgeTone(loan.status)}`}>{loan.status}</span>
                          <div className={`mt-1 text-xs ${normalizeStatus(loan.parBucket) === '0' || normalizeStatus(loan.parBucket) === 'PAR_0' ? 'text-slate-500 dark:text-slate-400' : 'text-rose-600 dark:text-rose-300'}`}>
                            PAR {loan.parBucket || '0'}
                          </div>
                        </td>
                      </tr>
                    ))}
                    {filteredLoans.length === 0 && (
                      <tr>
                        <td colSpan={4} className="px-6 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
                          No loans matched the current search and filter combination.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-800">
              {selectedLoan ? (
                <div className="space-y-5">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h3 className="text-lg font-semibold text-slate-900 dark:text-white">Loan detail</h3>
                      <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{selectedLoan.id}</p>
                    </div>
                    <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${badgeTone(selectedLoan.status)}`}>
                      {selectedLoan.status}
                    </span>
                  </div>

                  <div className="grid gap-3 sm:grid-cols-2">
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Customer</div>
                      <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{customerMap.get(selectedLoan.cif) || selectedLoan.cif}</div>
                    </div>
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Outstanding</div>
                      <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{formatCurrency(selectedLoan.outstandingBalance || 0)}</div>
                    </div>
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Pricing</div>
                      <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{selectedLoan.rate}% for {selectedLoan.termMonths} months</div>
                    </div>
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Security</div>
                      <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">
                        {selectedLoan.collateralType ? `${selectedLoan.collateralType} · ${formatCurrency(selectedLoan.collateralValue || 0)}` : 'No collateral captured'}
                      </div>
                    </div>
                  </div>

                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-900/60">
                    <div className="flex items-center gap-2 text-sm font-semibold text-slate-800 dark:text-slate-100">
                      <Clock3 className="h-4 w-4" />
                      Portfolio controls
                    </div>
                    <div className="mt-3 flex flex-wrap gap-2">
                      <button type="button" onClick={() => handleTabChange('schedule')} className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200">
                        View schedule
                      </button>
                      <button type="button" onClick={() => handleTabChange('repayment')} className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200">
                        Post repayment
                      </button>
                      <button type="button" onClick={() => { setSelectedReviewLoanId(selectedLoan.id); setActiveTab('origination'); }} className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200">
                        Open review queue
                      </button>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="flex h-full items-center justify-center text-sm text-slate-500 dark:text-slate-400">
                  Select a loan to inspect its operational details.
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {activeTab === 'origination' && (
        <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
          <div className="space-y-6">
            <div className="rounded-2xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800">
              <div className="flex items-center gap-2">
                <PlusCircle className="h-5 w-5 text-blue-600 dark:text-blue-300" />
                <h3 className="text-lg font-semibold text-slate-900 dark:text-white">Origination workspace</h3>
              </div>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
                Capture a new application, preview the repayment plan, run a bureau check, and then hand it into the review queue.
              </p>

              <form onSubmit={handleSubmitApplication} className="mt-5 space-y-4">
                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Customer CIF / Customer ID</label>
                  <input
                    list="loan-customer-cifs"
                    type="text"
                    value={origCif}
                    onChange={(event) => setOrigCif(event.target.value)}
                    placeholder="Search or paste CIF"
                    className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900"
                    required
                  />
                  <datalist id="loan-customer-cifs">
                    {customers.map((customer) => (
                      <option key={customer.id} value={customer.id}>{customer.name}</option>
                    ))}
                  </datalist>
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Loan product</label>
                  <select value={origProduct} onChange={(event) => setOrigProduct(event.target.value)} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
                    {loanProducts.map((product) => (
                      <option key={product.id} value={product.id}>{product.name}</option>
                    ))}
                  </select>
                </div>

                <div className="grid gap-4 md:grid-cols-3">
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Principal</label>
                    <input type="number" min="100" value={origPrincipal} onChange={(event) => setOrigPrincipal(event.target.value)} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900" required />
                  </div>
                  <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
                    <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Annual rate (%)</div>
                    <div className="mt-1 text-sm font-semibold text-slate-900 dark:text-white">{selectedProductDefinition?.annualInterestRate ?? 0}%</div>
                    <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">Controlled from Product Definition.</div>
                  </div>
                  <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
                    <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Term and schedule</div>
                    <div className="mt-1 text-sm font-semibold text-slate-900 dark:text-white">
                      {selectedProductDefinition?.termInPeriods ?? 0} periods · {selectedProductDefinition?.repaymentFrequency || 'N/A'}
                    </div>
                    <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{selectedProductDefinition?.interestMethod || 'N/A'} interest</div>
                  </div>
                </div>

                <div className="grid gap-4 md:grid-cols-3">
                  <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
                    <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Minimum amount</div>
                    <div className="mt-1 text-sm font-semibold text-slate-900 dark:text-white">{formatCurrency(selectedProductDefinition?.minAmount ?? 0)}</div>
                  </div>
                  <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
                    <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Maximum amount</div>
                    <div className="mt-1 text-sm font-semibold text-slate-900 dark:text-white">{formatCurrency(selectedProductDefinition?.maxAmount ?? 0)}</div>
                  </div>
                  <div className="rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 dark:border-blue-900/60 dark:bg-blue-950/30">
                    <div className="text-xs font-medium text-blue-700 dark:text-blue-300">Product governance</div>
                    <div className="mt-1 text-sm font-semibold text-slate-900 dark:text-white">Parameters are enforced from Product Definition.</div>
                    <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">Origination can only choose a product and submit facility-specific details.</div>
                  </div>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Collateral type</label>
                    <input type="text" value={origCollateralType} onChange={(event) => setOrigCollateralType(event.target.value)} placeholder="Optional" className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900" />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Collateral value</label>
                    <input type="number" min="0" step="0.01" value={origCollateralValue} onChange={(event) => setOrigCollateralValue(event.target.value)} placeholder="Optional" className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900" />
                  </div>
                </div>

                <div className="flex flex-wrap gap-3">
                  <button type="button" onClick={handlePreviewSchedule} disabled={previewLoading} className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
                    <Clock3 className="h-4 w-4" />
                    {previewLoading ? 'Generating preview...' : 'Preview schedule'}
                  </button>
                  <button type="button" onClick={handleCreditCheck} disabled={creditLoading} className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
                    <ShieldCheck className="h-4 w-4" />
                    {creditLoading ? 'Running credit check...' : 'Run credit check'}
                  </button>
                  <Can permission={Permissions.Loans.Disburse}>
                    <button type="submit" disabled={workflowBusy === 'apply'} className="inline-flex items-center gap-2 rounded-xl bg-blue-600 px-4 py-3 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60">
                      <CheckCircle2 className="h-4 w-4" />
                      {workflowBusy === 'apply' ? 'Submitting...' : 'Submit application'}
                    </button>
                  </Can>
                </div>
              </form>
            </div>

            <div className="grid gap-6 xl:grid-cols-2">
              <div className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-800">
                <h4 className="text-base font-semibold text-slate-900 dark:text-white">Schedule preview</h4>
                <div className="mt-4 space-y-3">
                  {previewSchedule.length > 0 ? previewSchedule.slice(0, 5).map((line: any) => (
                    <div key={`${line.period}-${line.dueDate}`} className="rounded-xl border border-slate-200 px-4 py-3 dark:border-slate-700">
                      <div className="flex items-center justify-between gap-3">
                        <div>
                          <div className="text-sm font-medium text-slate-900 dark:text-white">Period {line.period}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">{String(line.dueDate)}</div>
                        </div>
                        <div className="text-right text-sm">
                          <div className="font-medium text-slate-900 dark:text-white">{formatCurrency(Number(line.installment ?? line.total ?? 0))}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">Closing {formatCurrency(Number(line.closingBalance ?? line.balance ?? 0))}</div>
                        </div>
                      </div>
                    </div>
                  )) : (
                    <div className="rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                      Generate a preview to validate repayment affordability before submission.
                    </div>
                  )}
                </div>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-800">
                <h4 className="text-base font-semibold text-slate-900 dark:text-white">Credit decision snapshot</h4>
                {creditResult ? (
                  <div className="mt-4 grid gap-3 sm:grid-cols-2">
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Score</div>
                      <div className="mt-1 text-lg font-semibold text-slate-900 dark:text-white">{creditResult.score}</div>
                    </div>
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Decision</div>
                      <div className="mt-1 text-lg font-semibold text-slate-900 dark:text-white">{creditResult.decision}</div>
                    </div>
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Risk grade</div>
                      <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{creditResult.riskGrade}</div>
                    </div>
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Provider</div>
                      <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{creditResult.providerName}</div>
                    </div>
                  </div>
                ) : (
                  <div className="mt-4 rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                    Run a credit check to capture a bureau-backed decision before approval.
                  </div>
                )}
              </div>
            </div>
          </div>

          <div className="space-y-6">
            <div className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-800">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <h3 className="text-lg font-semibold text-slate-900 dark:text-white">Review queue</h3>
                  <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Maker-checker oriented queue for appraisal, approval, and disbursement.</p>
                </div>
                <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700 dark:bg-slate-700 dark:text-slate-200">
                  {pendingReviewLoans.length} queued
                </span>
              </div>

              <div className="mt-4 space-y-3">
                {pendingReviewLoans.length > 0 ? pendingReviewLoans.map((loan) => (
                  <button
                    key={loan.id}
                    type="button"
                    onClick={() => setSelectedReviewLoanId(loan.id)}
                    className={`w-full rounded-xl border px-4 py-3 text-left transition ${
                      selectedReviewLoanId === loan.id
                        ? 'border-blue-300 bg-blue-50 dark:border-blue-800 dark:bg-blue-900/15'
                        : 'border-slate-200 bg-white hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:hover:bg-slate-800'
                    }`}
                  >
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <div className="font-medium text-slate-900 dark:text-white">{loan.id}</div>
                        <div className="text-xs text-slate-500 dark:text-slate-400">{customerMap.get(loan.cif) || loan.cif}</div>
                      </div>
                      <span className={`inline-flex rounded-full px-2 py-1 text-[11px] font-semibold ${badgeTone(loan.status)}`}>{loan.status}</span>
                    </div>
                    <div className="mt-2 text-xs text-slate-500 dark:text-slate-400">
                      {loan.productName || loan.productCode} · {formatCurrency(loan.principal)}
                    </div>
                  </button>
                )) : (
                  <div className="rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                    No queued loan applications need review right now.
                  </div>
                )}
              </div>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-800">
              {selectedReviewLoan ? (
                <div className="space-y-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h4 className="text-base font-semibold text-slate-900 dark:text-white">Selected application</h4>
                      <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{selectedReviewLoan.id}</p>
                    </div>
                    <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${badgeTone(selectedReviewLoan.status)}`}>
                      {selectedReviewLoan.status}
                    </span>
                  </div>

                  <div className="grid gap-3 sm:grid-cols-2">
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Customer</div>
                      <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{customerMap.get(selectedReviewLoan.cif) || selectedReviewLoan.cif}</div>
                    </div>
                    <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                      <div className="text-xs text-slate-500">Facility</div>
                      <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{formatCurrency(selectedReviewLoan.principal)}</div>
                    </div>
                  </div>

                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Decision notes</label>
                    <textarea
                      value={approvalNotes}
                      onChange={(event) => setApprovalNotes(event.target.value)}
                      rows={4}
                      className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm dark:border-slate-700 dark:bg-slate-900"
                      placeholder="Capture appraisal summary, approval reason, or disbursement note"
                    />
                  </div>

                  <div className="flex flex-wrap gap-3">
                    <Can permission={Permissions.Loans.Approve}>
                      <button onClick={() => handleReviewAction('appraise')} disabled={workflowBusy === 'appraise'} className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
                        {workflowBusy === 'appraise' ? 'Appraising...' : 'Appraise'}
                      </button>
                    </Can>
                    <Can permission={Permissions.Loans.Approve}>
                      <button onClick={() => handleReviewAction('approve')} disabled={workflowBusy === 'approve'} className="rounded-xl bg-amber-500 px-4 py-3 text-sm font-medium text-white hover:bg-amber-600 disabled:opacity-60">
                        {workflowBusy === 'approve' ? 'Approving...' : 'Approve'}
                      </button>
                    </Can>
                    <Can permission={Permissions.Loans.Disburse}>
                      <button onClick={() => handleReviewAction('disburse')} disabled={workflowBusy === 'disburse'} className="rounded-xl bg-green-600 px-4 py-3 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-60">
                        {workflowBusy === 'disburse' ? 'Disbursing...' : 'Disburse'}
                      </button>
                    </Can>
                  </div>
                </div>
              ) : (
                <div className="py-10 text-center text-sm text-slate-500 dark:text-slate-400">
                  Select an item from the review queue to continue appraisal, approval, or disbursement.
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {activeTab === 'repayment' && (
        <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
          <div className="rounded-2xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800">
            <h3 className="text-lg font-semibold text-slate-900 dark:text-white">Repayment posting</h3>
            <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Select the live facility, confirm the source account, and post a controlled repayment.</p>

            <form onSubmit={handleRepay} className="mt-5 space-y-4">
              <div>
                <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Loan</label>
                <select
                  value={selectedLoanId}
                  onChange={async (event) => {
                    setSelectedLoanId(event.target.value);
                    if (event.target.value) {
                      await loadSchedule(event.target.value);
                    }
                  }}
                  className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900"
                  required
                >
                  <option value="">Select loan</option>
                  {activeLoans.map((loan) => (
                    <option key={loan.id} value={loan.id}>{loan.id} - {customerMap.get(loan.cif) || loan.cif}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Debit account ID</label>
                <input type="text" value={repayAccountId} onChange={(event) => setRepayAccountId(event.target.value)} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900" required />
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Amount</label>
                <input type="number" min="0.01" step="0.01" value={repayAmount} onChange={(event) => setRepayAmount(event.target.value)} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900" required />
              </div>
              <button type="submit" disabled={loading} className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-green-600 px-4 py-3 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-60">
                <Wallet className="h-4 w-4" />
                Submit repayment
              </button>
            </form>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800">
            <h3 className="text-lg font-semibold text-slate-900 dark:text-white">Selected facility context</h3>
            {selectedLoan ? (
              <div className="mt-4 space-y-4">
                <div className="grid gap-3 sm:grid-cols-3">
                  <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                    <div className="text-xs text-slate-500">Customer</div>
                    <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{customerMap.get(selectedLoan.cif) || selectedLoan.cif}</div>
                  </div>
                  <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                    <div className="text-xs text-slate-500">Outstanding</div>
                    <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{formatCurrency(selectedLoan.outstandingBalance || 0)}</div>
                  </div>
                  <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700">
                    <div className="text-xs text-slate-500">PAR</div>
                    <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{selectedLoan.parBucket || '0'}</div>
                  </div>
                </div>

                <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-900/60">
                  <div className="text-sm font-semibold text-slate-900 dark:text-white">Upcoming installments</div>
                  <div className="mt-3 space-y-3">
                    {upcomingInstallments.length > 0 ? upcomingInstallments.map((line) => (
                      <div key={`${line.period}-${line.dueDate}`} className="flex items-center justify-between gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-800">
                        <div>
                          <div className="text-sm font-medium text-slate-900 dark:text-white">Period {line.period}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">{String(line.dueDate)}</div>
                        </div>
                        <div className="text-right">
                          <div className="text-sm font-medium text-slate-900 dark:text-white">{formatCurrency(line.total)}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">{line.status}</div>
                        </div>
                      </div>
                    )) : (
                      <div className="rounded-xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                        Select a loan to load its repayment schedule context.
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ) : (
              <div className="mt-4 text-sm text-slate-500 dark:text-slate-400">Choose a loan on the left to display repayment context.</div>
            )}
          </div>
        </div>
      )}

      {activeTab === 'schedule' && (
        <div className="space-y-4">
          <div className="max-w-xl rounded-2xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800">
            <label className="mb-2 block text-sm font-medium text-slate-600 dark:text-slate-300">Loan</label>
            <select
              value={selectedLoanId}
              onChange={async (event) => {
                setSelectedLoanId(event.target.value);
                if (event.target.value) {
                  await loadSchedule(event.target.value);
                }
              }}
              className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900"
            >
              <option value="">Select loan</option>
              {loans.map((loan) => (
                <option key={loan.id} value={loan.id}>{loan.id} - {customerMap.get(loan.cif) || loan.cif}</option>
              ))}
            </select>
          </div>
          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
            <table className="w-full text-sm text-left">
              <thead className="border-b border-slate-200 bg-slate-50 text-slate-500 dark:border-slate-700 dark:bg-slate-900/50 dark:text-slate-400">
                <tr>
                  <th className="p-4">Period</th>
                  <th className="p-4">Due Date</th>
                  <th className="p-4 text-right">Principal</th>
                  <th className="p-4 text-right">Interest</th>
                  <th className="p-4 text-right">Installment</th>
                  <th className="p-4 text-right">Balance</th>
                  <th className="p-4">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-700/60">
                {schedule.map((line) => (
                  <tr key={`${line.period}-${line.dueDate}`}>
                    <td className="p-4">{line.period}</td>
                    <td className="p-4">{String(line.dueDate)}</td>
                    <td className="p-4 text-right">{formatCurrency(line.principal)}</td>
                    <td className="p-4 text-right">{formatCurrency(line.interest)}</td>
                    <td className="p-4 text-right">{formatCurrency(line.total)}</td>
                    <td className="p-4 text-right">{formatCurrency(line.balance)}</td>
                    <td className="p-4">{line.status}</td>
                  </tr>
                ))}
                {schedule.length === 0 && (
                  <tr>
                    <td colSpan={7} className="p-6 text-center text-slate-500 dark:text-slate-400">
                      Select a loan to view its amortization schedule.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === 'operations' && <LoanOperationsSuite loans={loans} onReload={loadData} />}
    </div>
  );
}
