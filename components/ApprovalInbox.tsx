import React, { useMemo, useState } from 'react';
import { ApprovalRequest, StaffUser } from '../types';
import { CheckCircle, XCircle, Clock, User, DollarSign, FileText, Landmark, Calendar, Shield, Search, Filter, Inbox } from 'lucide-react';

interface ApprovalInboxProps {
    requests: ApprovalRequest[];
    currentUser: StaffUser;
    onApprove: (id: string, workflowStep: number) => Promise<void>;
    onReject: (id: string) => Promise<void>;
}

const money = (value: number) => value.toLocaleString('en-US', { style: 'currency', currency: 'GHS' });

const ApprovalInbox: React.FC<ApprovalInboxProps> = ({ requests, currentUser, onApprove, onReject }) => {
    const [searchQuery, setSearchQuery] = useState('');
    const [typeFilter, setTypeFilter] = useState('ALL');
    const [activeView, setActiveView] = useState<'pending' | 'history'>('pending');

    const requestTypes = useMemo(
        () => ['ALL', ...Array.from(new Set(requests.map((request) => request.type).filter(Boolean)))],
        [requests],
    );

    const filteredRequests = useMemo(() => {
        const query = searchQuery.trim().toLowerCase();

        return requests.filter((request) => {
            const matchesType = typeFilter === 'ALL' || request.type === typeFilter;
            const matchesSearch = !query || [
                request.id,
                request.type,
                request.description,
                request.requesterName,
                request.payload?.loanDetails?.customerName,
                request.payload?.loanDetails?.productName,
                request.payload?.loanDetails?.customerId,
                request.payload?.loanDetails?.loanId,
            ].some((value) => String(value || '').toLowerCase().includes(query));

            return matchesType && matchesSearch;
        });
    }, [requests, searchQuery, typeFilter]);

    const pendingRequests = filteredRequests.filter((request) => request.status === 'PENDING');
    const historyRequests = filteredRequests.filter((request) => request.status !== 'PENDING');
    const renderedRequests = activeView === 'pending' ? pendingRequests : historyRequests;
    const overduePending = pendingRequests.filter((request) => {
        const requestDate = new Date(request.requestDate).getTime();
        return Number.isFinite(requestDate) && (Date.now() - requestDate) > 24 * 60 * 60 * 1000;
    }).length;
    const highValuePending = pendingRequests.filter(
        (request) => Number(request.amount || request.payload?.loanDetails?.principal || 0) >= 50000,
    ).length;

    return (
        <div className="flex flex-col h-full rounded-[28px] border border-slate-200 bg-[linear-gradient(180deg,#f8fafc,#eef2f7)] overflow-hidden">
            <div className="border-b border-slate-200 bg-white/90 px-6 py-5">
                <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
                    <div>
                        <h1 className="text-xl font-bold text-slate-900 flex items-center gap-2">
                            <CheckCircle className="text-emerald-600" size={24} />
                            Approval Inbox
                        </h1>
                        <p className="text-slate-500 text-sm mt-1">Maker-checker authorization queue for credit, transactions, and operational exceptions.</p>
                    </div>
                    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                        <div className="rounded-2xl border border-blue-100 bg-blue-50 px-4 py-3">
                            <p className="text-xs uppercase tracking-[0.18em] text-blue-600">Pending</p>
                            <p className="mt-1 text-2xl font-bold text-slate-900">{pendingRequests.length}</p>
                        </div>
                        <div className="rounded-2xl border border-amber-100 bg-amber-50 px-4 py-3">
                            <p className="text-xs uppercase tracking-[0.18em] text-amber-600">Over 24h</p>
                            <p className="mt-1 text-2xl font-bold text-slate-900">{overduePending}</p>
                        </div>
                        <div className="rounded-2xl border border-rose-100 bg-rose-50 px-4 py-3">
                            <p className="text-xs uppercase tracking-[0.18em] text-rose-600">High Value</p>
                            <p className="mt-1 text-2xl font-bold text-slate-900">{highValuePending}</p>
                        </div>
                    </div>
                </div>

                <div className="mt-5 grid grid-cols-1 gap-3 xl:grid-cols-[1fr_220px_auto]">
                    <label className="relative block">
                        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                        <input
                            value={searchQuery}
                            onChange={(event) => setSearchQuery(event.target.value)}
                            placeholder="Search request ID, requester, customer, product, or description"
                            className="w-full rounded-2xl border border-slate-200 bg-slate-50 py-3 pl-10 pr-4 text-sm text-slate-900 outline-none transition focus:border-blue-400 focus:bg-white"
                        />
                    </label>
                    <label className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50 px-3">
                        <Filter className="h-4 w-4 text-slate-400" />
                        <select
                            value={typeFilter}
                            onChange={(event) => setTypeFilter(event.target.value)}
                            className="w-full bg-transparent py-3 text-sm text-slate-700 outline-none"
                        >
                            {requestTypes.map((type) => (
                                <option key={type} value={type}>
                                    {type === 'ALL' ? 'All request types' : type.replace(/_/g, ' ')}
                                </option>
                            ))}
                        </select>
                    </label>
                    <div className="flex rounded-2xl border border-slate-200 bg-slate-50 p-1">
                        <button
                            onClick={() => setActiveView('pending')}
                            className={`rounded-xl px-4 py-2 text-sm font-semibold transition ${activeView === 'pending' ? 'bg-slate-900 text-white' : 'text-slate-600'}`}
                        >
                            Pending
                        </button>
                        <button
                            onClick={() => setActiveView('history')}
                            className={`rounded-xl px-4 py-2 text-sm font-semibold transition ${activeView === 'history' ? 'bg-slate-900 text-white' : 'text-slate-600'}`}
                        >
                            History
                        </button>
                    </div>
                </div>
            </div>

            <div className="flex-1 overflow-y-auto p-6 space-y-6">
                {activeView === 'pending' ? (
                    <div>
                        <h3 className="text-sm font-bold text-gray-500 uppercase tracking-wider mb-4">Pending Authorization</h3>
                        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                            {renderedRequests.length === 0 && (
                                <div className="col-span-2 p-10 text-center bg-white rounded-2xl border border-dashed border-slate-300 text-slate-400">
                                    <Inbox className="mx-auto mb-3 h-10 w-10 text-slate-300" />
                                    No pending requests match the current filters.
                                </div>
                            )}
                            {renderedRequests.map(req => {
                                const loanDetails = req.payload.loanDetails;
                                return (
                                    <div key={req.id} className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 hover:shadow-md transition-shadow">
                                        <div className="flex justify-between items-start mb-3 gap-3">
                                            <div className="flex items-center gap-2 min-w-0">
                                                <div className={`p-2 rounded-lg ${req.type === 'TRANSACTION_LIMIT' ? 'bg-orange-100 text-orange-600' : 'bg-purple-100 text-purple-600'}`}>
                                                    {req.type === 'TRANSACTION_LIMIT' ? <DollarSign size={20}/> : <FileText size={20}/>}
                                                </div>
                                                <div className="min-w-0">
                                                    <h4 className="font-bold text-gray-800 text-sm truncate">{req.type.replace(/_/g, ' ')}</h4>
                                                    <span className="text-xs text-gray-500 font-mono">{req.id}</span>
                                                </div>
                                            </div>
                                            <span className="bg-yellow-100 text-yellow-800 text-xs px-2 py-1 rounded font-bold flex items-center gap-1 whitespace-nowrap">
                                                <Clock size={12} /> Pending
                                            </span>
                                        </div>

                                        <p className="text-sm text-gray-700 mb-4">{req.description}</p>

                                        {loanDetails && (
                                            <div className="mb-4 rounded-xl border border-blue-100 bg-blue-50 p-4">
                                                <div className="grid grid-cols-1 gap-3 text-sm md:grid-cols-2">
                                                    <div>
                                                        <p className="text-xs font-bold uppercase tracking-wide text-blue-500">Customer</p>
                                                        <p className="font-semibold text-gray-900">{loanDetails.customerName || loanDetails.customerId}</p>
                                                        <p className="text-xs text-gray-500">CIF: {loanDetails.customerId}</p>
                                                    </div>
                                                    <div>
                                                        <p className="text-xs font-bold uppercase tracking-wide text-blue-500">Product</p>
                                                        <p className="font-semibold text-gray-900">{loanDetails.productName || loanDetails.productCode || 'Loan Product'}</p>
                                                        <p className="text-xs text-gray-500">Loan ID: {loanDetails.loanId}</p>
                                                    </div>
                                                    <div className="flex items-start gap-2">
                                                        <DollarSign size={16} className="mt-0.5 text-blue-500" />
                                                        <div>
                                                            <p className="text-xs font-bold uppercase tracking-wide text-blue-500">Principal</p>
                                                            <p className="font-semibold text-gray-900">{money(loanDetails.principal)}</p>
                                                        </div>
                                                    </div>
                                                    <div className="flex items-start gap-2">
                                                        <Landmark size={16} className="mt-0.5 text-blue-500" />
                                                        <div>
                                                            <p className="text-xs font-bold uppercase tracking-wide text-blue-500">Outstanding</p>
                                                            <p className="font-semibold text-gray-900">{money(loanDetails.outstandingBalance ?? loanDetails.principal)}</p>
                                                        </div>
                                                    </div>
                                                    <div className="flex items-start gap-2">
                                                        <Calendar size={16} className="mt-0.5 text-blue-500" />
                                                        <div>
                                                            <p className="text-xs font-bold uppercase tracking-wide text-blue-500">Terms</p>
                                                            <p className="font-semibold text-gray-900">{loanDetails.rate}% for {loanDetails.termMonths} months</p>
                                                        </div>
                                                    </div>
                                                    <div className="flex items-start gap-2">
                                                        <Shield size={16} className="mt-0.5 text-blue-500" />
                                                        <div>
                                                            <p className="text-xs font-bold uppercase tracking-wide text-blue-500">Risk / Security</p>
                                                            <p className="font-semibold text-gray-900">PAR {loanDetails.parBucket || '0'}</p>
                                                            <p className="text-xs text-gray-500">Collateral: {loanDetails.collateralType || 'Not captured'}</p>
                                                            {loanDetails.collateralValue !== undefined && (
                                                                <p className="text-xs text-gray-500">Collateral Value: {money(loanDetails.collateralValue)}</p>
                                                            )}
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        )}

                                        <div className="mb-4 grid grid-cols-1 gap-2 rounded-xl bg-gray-50 p-3 text-xs text-gray-500 md:grid-cols-2">
                                            <div className="flex items-center gap-1">
                                                <User size={12}/> Request by: <span className="font-bold text-gray-700">{req.requesterName}</span>
                                            </div>
                                            <div className="text-right md:text-left">Raised: {new Date(req.requestDate).toLocaleString()}</div>
                                            {loanDetails?.appliedAt && <div>Applied: {new Date(loanDetails.appliedAt).toLocaleString()}</div>}
                                            {loanDetails?.status && <div className="text-right md:text-left">Loan Status: <span className="font-bold text-gray-700">{loanDetails.status}</span></div>}
                                        </div>

                                        {req.amount !== undefined && (
                                            <div className="mb-4">
                                                <span className="text-xs text-gray-500 uppercase font-bold">Amount</span>
                                                <div className="text-lg font-bold text-gray-900 font-mono">
                                                    {money(req.amount)}
                                                </div>
                                            </div>
                                        )}

                                        <div className="flex gap-3 pt-3 border-t border-gray-100">
                                            <button
                                                onClick={() => onReject(req.id)}
                                                className="flex-1 py-2 border border-red-200 text-red-600 rounded-xl hover:bg-red-50 font-medium text-sm flex items-center justify-center gap-2"
                                            >
                                                <XCircle size={16}/> Reject
                                            </button>
                                            <button
                                                onClick={() => onApprove(req.id, req.payload.currentStep ?? 0)}
                                                className="flex-1 py-2 bg-green-600 text-white rounded-xl hover:bg-green-700 font-medium text-sm flex items-center justify-center gap-2 shadow-sm"
                                            >
                                                <CheckCircle size={16}/> Approve
                                            </button>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                ) : (
                    <div>
                        <h3 className="text-sm font-bold text-gray-500 uppercase tracking-wider mb-4">Action History</h3>
                        <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
                            <table className="w-full text-sm text-left">
                                <thead className="bg-gray-50 text-gray-600 font-medium border-b border-gray-200">
                                    <tr>
                                        <th className="p-4">Request ID</th>
                                        <th className="p-4">Type</th>
                                        <th className="p-4">Description</th>
                                        <th className="p-4">Requester</th>
                                        <th className="p-4 text-center">Status</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-gray-100">
                                    {renderedRequests.map(req => (
                                        <tr key={req.id} className="hover:bg-gray-50">
                                            <td className="p-4 font-mono text-xs text-gray-500">{req.id}</td>
                                            <td className="p-4 text-gray-800 font-medium">{req.type.replace(/_/g, ' ')}</td>
                                            <td className="p-4 text-gray-600">{req.description}</td>
                                            <td className="p-4 text-gray-600 text-xs">{req.requesterName}</td>
                                            <td className="p-4 text-center">
                                                <span className={`px-2 py-1 rounded text-[10px] font-bold uppercase ${
                                                    req.status === 'APPROVED' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                                                }`}>
                                                    {req.status}
                                                </span>
                                            </td>
                                        </tr>
                                    ))}
                                    {renderedRequests.length === 0 && (
                                        <tr><td colSpan={5} className="p-8 text-center text-gray-400">No approval history matches the current filters.</td></tr>
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default ApprovalInbox;
