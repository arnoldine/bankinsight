import React, { useState, useEffect, useMemo } from 'react';
import { useLoans } from '../hooks/useApi';
import { Loan, DisburseLoanRequest, LoanRepayRequest, LoanScheduleDto } from '../services/loanService';
import {
    AlertTriangle,
    PlusCircle,
    RefreshCw,
    Search,
    SlidersHorizontal,
    X,
} from 'lucide-react';
import { Customer } from '../../types';
import LoanOperationsSuite from './loans/LoanOperationsSuite';
import { Can } from '../../components/Can';
import { Permissions } from '../../lib/Permissions';

interface LoanManagementHubProps {
  loans?: Loan[];
  customers?: Customer[];
  onDisburseLoan?: (data: any) => void;
  onRepayLoan?: (loanId: string, data: any) => void;
  initialTab?: 'portfolio' | 'origination' | 'repayment' | 'schedule' | 'operations';
}

const formatCurrency = (value?: number) =>
  new Intl.NumberFormat('en-GH', {
    style: 'currency',
    currency: 'GHS',
    minimumFractionDigits: 2,
  }).format(value || 0);

const normalizeStatus = (status?: string) => String(status || '').toUpperCase();

export default function LoanManagementHub({ loans: initialLoans = [], customers: initialCustomers = [], onDisburseLoan, onRepayLoan, initialTab = 'portfolio' }: LoanManagementHubProps) {
    const { getLoans, disburseLoan, repayLoan, getLoanSchedule, loading, error } = useLoans();
    const [loans, setLoans] = useState<Loan[]>(initialLoans || []);
    const [customers] = useState(initialCustomers || []);
    const [activeTab, setActiveTab] = useState<'portfolio' | 'origination' | 'repayment' | 'schedule' | 'operations'>(initialTab);

    const [selectedLoanId, setSelectedLoanId] = useState<string>('');
    const [schedule, setSchedule] = useState<LoanScheduleDto[]>([]);

    const [origCif, setOrigCif] = useState('');
    const [origProduct, setOrigProduct] = useState('PERSONAL_LOAN');
    const [origPrincipal, setOrigPrincipal] = useState('');
    const [origRate, setOrigRate] = useState('15');
    const [origTerm, setOrigTerm] = useState('12');

    const [repayAmount, setRepayAmount] = useState('');
    const [repayAccountId, setRepayAccountId] = useState('');
    const [origCollateralType, setOrigCollateralType] = useState('');
    const [origCollateralValue, setOrigCollateralValue] = useState('');
    const [searchQuery, setSearchQuery] = useState('');
    const [statusFilter, setStatusFilter] = useState('ALL');
    const [parFilter, setParFilter] = useState<'ALL' | 'CURRENT' | 'ARREARS'>('ALL');

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

    const loadData = async () => {
        try {
            const data = await getLoans();
            setLoans(Array.isArray(data) ? data : []);
        } catch (err) {
            console.error(err);
        }
    };

    const handleDisburse = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const req: DisburseLoanRequest = {
                cif: origCif,
                productCode: origProduct,
                principal: parseFloat(origPrincipal),
                rate: parseFloat(origRate),
                termMonths: parseInt(origTerm, 10)
            };
            await disburseLoan(req);
            if (onDisburseLoan) {
                onDisburseLoan(req);
            }
            alert('Loan disbursed successfully');
            setOrigCif('');
            setOrigPrincipal('');
            loadData();
            setActiveTab('portfolio');
        } catch (err: any) {
            alert(err.message || 'Failed to disburse loan');
        }
    };

    const handleRepay = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!selectedLoanId) {
            alert('Please select a loan');
            return;
        }
        try {
            const req: LoanRepayRequest = {
                amount: parseFloat(repayAmount),
                accountId: repayAccountId
            };
            await repayLoan(selectedLoanId, req);
            alert('Repayment successful');
            setRepayAmount('');
            setRepayAccountId('');
            loadData();
            if (activeTab === 'schedule') {
                loadSchedule(selectedLoanId);
            }
            if (onRepayLoan) {
                onRepayLoan(selectedLoanId, req);
            }
        } catch (err: any) {
            alert(err.message || 'Failed to process repayment');
        }
    };

    const loadSchedule = async (loanId: string) => {
        try {
            const data = await getLoanSchedule(loanId);
            setSchedule(Array.isArray(data) ? data : []);
        } catch (err: any) {
            alert(err.message || 'Failed to load schedule');
        }
    };

    const handleTabChange = (tab: 'portfolio' | 'origination' | 'repayment' | 'schedule' | 'operations') => {
        setActiveTab(tab);
        if (tab === 'schedule' && selectedLoanId) {
            loadSchedule(selectedLoanId);
        }
    };

    const activeLoans = loans.filter((loan) => ['ACTIVE', 'APPROVED', 'DISBURSED', 'IN_ARREARS'].includes(normalizeStatus(loan.status)));
    const totalPrincipal = activeLoans.reduce((sum, loan) => sum + Number(loan.principal || 0), 0);
    const totalOutstanding = activeLoans.reduce((sum, loan) => sum + Number(loan.outstandingBalance || 0), 0);
    const arrearsCount = activeLoans.filter((loan) => normalizeStatus(loan.parBucket) !== '0' && normalizeStatus(loan.parBucket) !== 'PAR_0').length;
    const pendingApprovals = loans.filter((loan) => ['PENDING', 'PENDING_APPROVAL'].includes(normalizeStatus(loan.status))).length;
    const collateralizedCount = loans.filter((loan) => Boolean(loan.collateralType)).length;

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

    const clearPortfolioFilters = () => {
        setSearchQuery('');
        setStatusFilter('ALL');
        setParFilter('ALL');
    };

    const statusBadge = (loan: Loan) => {
        const status = normalizeStatus(loan.status);
        if (status === 'ACTIVE' || status === 'APPROVED' || status === 'DISBURSED') {
            return 'bg-green-100 text-green-700';
        }
        if (status === 'PENDING_APPROVAL' || status === 'PENDING') {
            return 'bg-yellow-100 text-yellow-700';
        }
        if (status === 'WRITTEN_OFF' || status === 'CLOSED') {
            return 'bg-slate-100 text-slate-600';
        }
        return 'bg-red-100 text-red-700';
    };

    return (
        <div className="simple-screen p-6 space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                    <h2 className="text-2xl font-bold mb-2">Loan Management (Originations & Portfolio)</h2>
                    <p className="text-gray-500 dark:text-slate-400">Manage loan origination, monitoring, scheduling, and repayments.</p>
                </div>
                <button
                    onClick={loadData}
                    disabled={loading}
                    className="px-4 py-2 bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 hover:bg-gray-50 dark:hover:bg-slate-700 rounded-lg flex items-center gap-2 transition-colors disabled:opacity-50"
                >
                    <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
                    Refresh
                </button>
            </div>

            {error && (
                <div className="p-4 bg-red-900/20 border border-red-500/50 rounded-lg flex items-start gap-3">
                    <AlertTriangle className="w-5 h-5 text-red-400 flex-shrink-0 mt-0.5" />
                    <p className="text-red-200">{error}</p>
                </div>
            )}

            <div className="screen-hero p-6">
                <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
                    <div>
                        <p className="text-xs font-semibold text-slate-500">Loan operations</p>
                        <h3 className="mt-2 text-2xl font-semibold text-slate-900">Origination, monitoring, repayment, and control review.</h3>
                    </div>
                    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                        <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">Pending approval</p><p className="mt-1 text-xl font-semibold text-slate-900">{pendingApprovals}</p></div>
                        <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">In arrears</p><p className="mt-1 text-xl font-semibold text-slate-900">{arrearsCount}</p></div>
                        <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">Collateralized</p><p className="mt-1 text-xl font-semibold text-slate-900">{collateralizedCount}</p></div>
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 p-4 rounded-lg">
                    <p className="text-gray-500 dark:text-slate-400 text-sm">Active Loans</p>
                    <p className="text-2xl font-bold text-blue-600">{activeLoans.length}</p>
                </div>
                <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 p-4 rounded-lg">
                    <p className="text-gray-500 dark:text-slate-400 text-sm">Total Principal</p>
                    <p className="text-2xl font-bold text-green-600">{formatCurrency(totalPrincipal)}</p>
                </div>
                <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 p-4 rounded-lg">
                    <p className="text-gray-500 dark:text-slate-400 text-sm">Outstanding Balance</p>
                    <p className="text-2xl font-bold text-purple-600">{formatCurrency(totalOutstanding)}</p>
                </div>
                <div className={`bg-white dark:bg-slate-800 border ${arrearsCount > 0 ? 'border-red-300' : 'border-gray-200 dark:border-slate-700'} p-4 rounded-lg`}>
                    <p className="text-gray-500 dark:text-slate-400 text-sm">Loans in Arrears</p>
                    <p className={`text-2xl font-bold ${arrearsCount > 0 ? 'text-red-600' : 'text-green-600'}`}>{arrearsCount}</p>
                </div>
            </div>

            <div className="flex gap-4 border-b border-gray-200 dark:border-slate-700 overflow-x-auto">
                <button onClick={() => handleTabChange('portfolio')} className={`px-4 py-2 font-medium transition-colors border-b-2 whitespace-nowrap ${activeTab === 'portfolio' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-200'}`}>Portfolio Overview</button>
                <button onClick={() => handleTabChange('origination')} className={`px-4 py-2 font-medium transition-colors border-b-2 whitespace-nowrap ${activeTab === 'origination' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-200'}`}>Originate/Disburse</button>
                <button onClick={() => handleTabChange('repayment')} className={`px-4 py-2 font-medium transition-colors border-b-2 whitespace-nowrap ${activeTab === 'repayment' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-200'}`}>Process Repayment</button>
                <button onClick={() => handleTabChange('schedule')} className={`px-4 py-2 font-medium transition-colors border-b-2 whitespace-nowrap ${activeTab === 'schedule' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-200'}`}>Amortization Schedule</button>
                <button onClick={() => handleTabChange('operations')} className={`px-4 py-2 font-medium transition-colors border-b-2 whitespace-nowrap ${activeTab === 'operations' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-200'}`}>Compliance & Operations</button>
            </div>

            {activeTab === 'portfolio' && (
                <div className="space-y-4">
                    <div className="rounded-xl border border-gray-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
                            <div className="flex-1">
                                <label className="mb-2 block text-sm font-medium text-gray-600 dark:text-slate-300">Search clients, CIF, loan ID, or product</label>
                                <div className="relative">
                                    <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
                                    <input
                                        type="text"
                                        value={searchQuery}
                                        onChange={(e) => setSearchQuery(e.target.value)}
                                        placeholder="Try client name, CIF, loan ID, or product"
                                        className="w-full rounded-lg border border-gray-200 bg-gray-50 py-3 pl-10 pr-10 text-sm text-gray-900 outline-none transition focus:border-blue-400 focus:bg-white dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                                    />
                                    {searchQuery && (
                                        <button type="button" onClick={() => setSearchQuery('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600" aria-label="Clear search">
                                            <X className="h-4 w-4" />
                                        </button>
                                    )}
                                </div>
                            </div>
                            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3 lg:w-[460px]">
                                <div>
                                    <label className="mb-2 flex items-center gap-2 text-sm font-medium text-gray-600 dark:text-slate-300"><SlidersHorizontal className="h-4 w-4" /> Status</label>
                                    <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="w-full rounded-lg border border-gray-200 bg-gray-50 px-3 py-3 text-sm dark:border-slate-700 dark:bg-slate-900">
                                        <option value="ALL">All Statuses</option>
                                        <option value="ACTIVE">Active</option>
                                        <option value="PENDING_APPROVAL">Pending Approval</option>
                                        <option value="APPROVED">Approved</option>
                                        <option value="CLOSED">Closed</option>
                                        <option value="WRITTEN_OFF">Written Off</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="mb-2 block text-sm font-medium text-gray-600 dark:text-slate-300">PAR Bucket</label>
                                    <select value={parFilter} onChange={(e) => setParFilter(e.target.value as 'ALL' | 'CURRENT' | 'ARREARS')} className="w-full rounded-lg border border-gray-200 bg-gray-50 px-3 py-3 text-sm dark:border-slate-700 dark:bg-slate-900">
                                        <option value="ALL">All PAR States</option>
                                        <option value="CURRENT">Current Only</option>
                                        <option value="ARREARS">In Arrears</option>
                                    </select>
                                </div>
                                <button type="button" onClick={clearPortfolioFilters} className="rounded-lg border border-gray-200 bg-white px-4 py-3 text-sm font-medium text-gray-700 transition hover:bg-gray-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700">
                                    Clear Filters
                                </button>
                            </div>
                        </div>
                        <div className="mt-4 flex flex-wrap items-center gap-3 text-sm text-gray-500 dark:text-slate-400">
                            <span>Showing <span className="font-semibold text-gray-900 dark:text-white">{filteredLoans.length}</span> of {loans.length} loans</span>
                            {searchQuery && <span className="rounded-full bg-blue-50 px-3 py-1 text-blue-700">Search: {searchQuery}</span>}
                            {statusFilter !== 'ALL' && <span className="rounded-full bg-amber-50 px-3 py-1 text-amber-700">Status: {statusFilter.replace('_', ' ')}</span>}
                            {parFilter !== 'ALL' && <span className="rounded-full bg-rose-50 px-3 py-1 text-rose-700">PAR: {parFilter}</span>}
                        </div>
                    </div>

                    <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg overflow-hidden">
                        <div className="overflow-x-auto">
                            <table className="w-full text-left">
                                <thead className="text-xs uppercase text-gray-500 dark:text-slate-400 border-b border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800">
                                    <tr>
                                        <th className="px-6 py-4 font-medium">Customer</th>
                                        <th className="px-6 py-4 font-medium">Loan / Product</th>
                                        <th className="px-6 py-4 font-medium text-right">Principal</th>
                                        <th className="px-6 py-4 font-medium text-right">Outstanding</th>
                                        <th className="px-6 py-4 font-medium">Terms / Security</th>
                                        <th className="px-6 py-4 font-medium">Status / PAR</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-200 dark:divide-slate-700/50">
                                    {filteredLoans.map((loan) => (
                                        <tr key={loan.id} className="hover:bg-gray-50 dark:hover:bg-slate-700/30 transition-colors">
                                            <td className="px-6 py-4">
                                                <div className="font-medium text-gray-900 dark:text-white">{customerMap.get(loan.cif) || loan.cif}</div>
                                                <div className="text-sm text-gray-500 dark:text-slate-400">CIF: {loan.cif}</div>
                                            </td>
                                            <td className="px-6 py-4">
                                                <div className="font-mono text-blue-600 dark:text-blue-400">{loan.id}</div>
                                                <div className="text-sm text-gray-600 dark:text-slate-300">{loan.productName || loan.productCode || 'Loan Product'}</div>
                                            </td>
                                            <td className="px-6 py-4 font-mono text-right text-gray-700 dark:text-slate-300">{formatCurrency(loan.principal)}</td>
                                            <td className="px-6 py-4 font-mono text-right text-green-600 dark:text-green-400">{formatCurrency(loan.outstandingBalance || 0)}</td>
                                            <td className="px-6 py-4 text-sm text-gray-600 dark:text-slate-300">
                                                <div>{loan.rate}% / {loan.termMonths} months</div>
                                                <div className="text-xs text-gray-500 dark:text-slate-400">
                                                    {loan.collateralType ? `Collateral: ${loan.collateralType}` : 'Collateral not captured'}
                                                    {loan.collateralValue !== undefined ? ` | ${formatCurrency(loan.collateralValue)}` : ''}
                                                </div>
                                            </td>
                                            <td className="px-6 py-4">
                                                <span className={`inline-flex items-center px-2 py-1 rounded text-xs font-medium ${statusBadge(loan)}`}>{loan.status}</span>
                                                <div className={`mt-1 text-xs ${normalizeStatus(loan.parBucket) === '0' || normalizeStatus(loan.parBucket) === 'PAR_0' ? 'text-gray-500 dark:text-slate-400' : 'text-red-500'}`}>
                                                    PAR {loan.parBucket || '0'}
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                    {filteredLoans.length === 0 && !loading && (
                                        <tr>
                                            <td colSpan={6} className="px-6 py-10 text-center text-gray-500 dark:text-slate-400">
                                                <div className="mx-auto max-w-md space-y-2">
                                                    <p className="text-base font-medium text-gray-700 dark:text-slate-200">No loans matched your search.</p>
                                                    <p>Try a client name, CIF, loan ID, or clear the status and PAR filters.</p>
                                                </div>
                                            </td>
                                        </tr>
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            )}

            {activeTab === 'origination' && (
                <div className="max-w-2xl bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-6">
                    <h3 className="text-lg font-bold mb-4 flex items-center gap-2">
                        <PlusCircle className="w-5 h-5 text-blue-400" /> Disburse New Loan
                    </h3>
                    <form onSubmit={handleDisburse} className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Customer CIF</label>
                            <input list="loan-customer-cifs" type="text" value={origCif} onChange={(e) => setOrigCif(e.target.value)} placeholder="Search or paste CIF" className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" required />
                            <datalist id="loan-customer-cifs">
                                {customers.map((customer) => (
                                    <option key={customer.id} value={customer.id}>{customer.name}</option>
                                ))}
                            </datalist>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Product</label>
                            <select value={origProduct} onChange={(e) => setOrigProduct(e.target.value)} className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2">
                                <option value="PERSONAL_LOAN">Personal Loan</option>
                                <option value="BUSINESS_LOAN">Business Loan</option>
                                <option value="MORTGAGE">Mortgage</option>
                            </select>
                        </div>
                        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                            <div>
                                <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Principal Amount</label>
                                <input type="number" min="100" value={origPrincipal} onChange={(e) => setOrigPrincipal(e.target.value)} className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" required />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Term (Months)</label>
                                <input type="number" min="1" value={origTerm} onChange={(e) => setOrigTerm(e.target.value)} className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" required />
                            </div>
                        </div>
                        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                            <div>
                                <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Interest Rate (%)</label>
                                <input type="number" step="0.1" value={origRate} onChange={(e) => setOrigRate(e.target.value)} className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" required />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Collateral Type</label>
                                <input type="text" value={origCollateralType} onChange={(e) => setOrigCollateralType(e.target.value)} placeholder="Optional" className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" />
                            </div>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Collateral Value</label>
                            <input type="number" min="0" step="0.01" value={origCollateralValue} onChange={(e) => setOrigCollateralValue(e.target.value)} placeholder="Optional" className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" />
                        </div>
                        <Can permission={Permissions.Loans.Disburse}>
                            <button type="submit" disabled={loading} className="w-full py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors mt-4">Approve & Disburse</button>
                        </Can>
                    </form>
                </div>
            )}

            {activeTab === 'repayment' && (
                <div className="max-w-xl bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-6">
                    <h3 className="text-lg font-bold mb-4">Process Repayment</h3>
                    <form onSubmit={handleRepay} className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Loan</label>
                            <select value={selectedLoanId} onChange={(e) => setSelectedLoanId(e.target.value)} className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" required>
                                <option value="">Select loan</option>
                                {loans.map((loan) => (
                                    <option key={loan.id} value={loan.id}>{loan.id} - {customerMap.get(loan.cif) || loan.cif}</option>
                                ))}
                            </select>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Debit Account ID</label>
                            <input type="text" value={repayAccountId} onChange={(e) => setRepayAccountId(e.target.value)} className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" required />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Amount</label>
                            <input type="number" min="0.01" step="0.01" value={repayAmount} onChange={(e) => setRepayAmount(e.target.value)} className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2" required />
                        </div>
                        <button type="submit" disabled={loading} className="w-full py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg font-medium transition-colors">Submit Repayment</button>
                    </form>
                </div>
            )}

            {activeTab === 'schedule' && (
                <div className="space-y-4">
                    <div className="max-w-xl bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-6">
                        <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">Loan</label>
                        <select value={selectedLoanId} onChange={(e) => { setSelectedLoanId(e.target.value); if (e.target.value) loadSchedule(e.target.value); }} className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2">
                            <option value="">Select loan</option>
                            {loans.map((loan) => (
                                <option key={loan.id} value={loan.id}>{loan.id} - {customerMap.get(loan.cif) || loan.cif}</option>
                            ))}
                        </select>
                    </div>
                    <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg overflow-hidden">
                        <table className="w-full text-sm text-left">
                            <thead className="bg-gray-50 text-gray-500 border-b border-gray-200 dark:border-slate-700">
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
                            <tbody className="divide-y divide-gray-100 dark:divide-slate-700/50">
                                {schedule.map((line) => (
                                    <tr key={`${line.period}-${line.dueDate}`}>
                                        <td className="p-4">{line.period}</td>
                                        <td className="p-4">{line.dueDate}</td>
                                        <td className="p-4 text-right">{formatCurrency(line.principal)}</td>
                                        <td className="p-4 text-right">{formatCurrency(line.interest)}</td>
                                        <td className="p-4 text-right">{formatCurrency(line.total)}</td>
                                        <td className="p-4 text-right">{formatCurrency(line.balance)}</td>
                                        <td className="p-4">{line.status}</td>
                                    </tr>
                                ))}
                                {schedule.length === 0 && (
                                    <tr><td colSpan={7} className="p-6 text-center text-gray-400">Select a loan to view its amortization schedule.</td></tr>
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








