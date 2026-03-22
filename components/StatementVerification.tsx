import React, { useMemo, useState } from 'react';
import { Account, Transaction, Customer } from '../types';
import { Search, FileText, CheckCircle, Printer, Download, Calendar, Filter } from 'lucide-react';

interface StatementVerificationProps {
    accounts: Account[];
    transactions: Transaction[];
    customers: Customer[];
}

const formatCurrency = (value: number, currency = 'GHS') =>
    new Intl.NumberFormat('en-GH', { style: 'currency', currency, minimumFractionDigits: 2 }).format(value || 0);

const StatementVerification: React.FC<StatementVerificationProps> = ({ accounts, transactions, customers }) => {
    const [accountNum, setAccountNum] = useState('');
    const [dateFrom, setDateFrom] = useState('');
    const [dateTo, setDateTo] = useState('');
    const [reportData, setReportData] = useState<{ account: Account, customer: Customer, txns: Transaction[], runningBalance: number[] } | null>(null);
    const [error, setError] = useState('');

    const totalTransactions = reportData?.txns.length || 0;
    const totalCredits = useMemo(() => reportData?.txns.filter((tx) => tx.type === 'DEPOSIT' || tx.type === 'LOAN_REPAYMENT').reduce((sum, tx) => sum + tx.amount, 0) || 0, [reportData]);
    const totalDebits = useMemo(() => reportData?.txns.filter((tx) => tx.type !== 'DEPOSIT' && tx.type !== 'LOAN_REPAYMENT').reduce((sum, tx) => sum + tx.amount, 0) || 0, [reportData]);

    const handleGenerate = () => {
        setError('');
        setReportData(null);

        const acc = accounts.find(a => a.id === accountNum);
        if (!acc) {
            setError('Account not found.');
            return;
        }

        const cust = customers.find(c => c.id === acc.cif);
        if (!cust) {
            setError('Customer data missing.');
            return;
        }

        let filteredTxns = transactions.filter(t => t.accountId === acc.id);

        if (dateFrom) {
            filteredTxns = filteredTxns.filter(t => new Date(t.date) >= new Date(dateFrom));
        }
        if (dateTo) {
            filteredTxns = filteredTxns.filter(t => new Date(t.date) <= new Date(dateTo));
        }

        filteredTxns.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

        let currentRun = 0;
        const runningBalances = filteredTxns.map(t => {
            const amount = t.type === 'DEPOSIT' || t.type === 'LOAN_REPAYMENT' ? t.amount : -t.amount;
            currentRun += amount;
            return currentRun;
        });

        setReportData({ account: acc, customer: cust, txns: filteredTxns, runningBalance: runningBalances });
    };

    return (
        <div className="flex flex-col h-full rounded-[28px] border border-slate-200 bg-[linear-gradient(180deg,#f8fafc,#eef2f7)] overflow-hidden">
            <div className="border-b border-slate-200 bg-white px-6 py-5">
                <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
                    <div>
                        <h2 className="text-xl font-bold text-gray-800 flex items-center gap-2 mb-1">
                            <FileText className="text-blue-600" /> Statement Verification
                        </h2>
                        <p className="text-sm text-slate-500">Generate an account statement view with transaction filters and verification-ready presentation.</p>
                    </div>
                    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                        <div className="rounded-2xl border border-blue-100 bg-blue-50 px-4 py-3"><p className="text-xs uppercase tracking-[0.18em] text-blue-600">Transactions</p><p className="mt-1 text-2xl font-bold text-slate-900">{totalTransactions}</p></div>
                        <div className="rounded-2xl border border-emerald-100 bg-emerald-50 px-4 py-3"><p className="text-xs uppercase tracking-[0.18em] text-emerald-600">Credits</p><p className="mt-1 text-lg font-bold text-slate-900">{formatCurrency(totalCredits, reportData?.account.currency || 'GHS')}</p></div>
                        <div className="rounded-2xl border border-rose-100 bg-rose-50 px-4 py-3"><p className="text-xs uppercase tracking-[0.18em] text-rose-600">Debits</p><p className="mt-1 text-lg font-bold text-slate-900">{formatCurrency(totalDebits, reportData?.account.currency || 'GHS')}</p></div>
                    </div>
                </div>

                <div className="mt-5 grid grid-cols-1 gap-4 xl:grid-cols-[1.2fr_repeat(2,180px)_auto] xl:items-end">
                    <div>
                        <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Account Number</label>
                        <div className="relative">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
                            <input
                                type="text"
                                value={accountNum}
                                onChange={e => setAccountNum(e.target.value)}
                                className="w-full pl-9 pr-4 py-3 border border-gray-300 rounded-2xl focus:ring-2 focus:ring-blue-500 outline-none"
                                placeholder="e.g. 201000..."
                            />
                        </div>
                    </div>
                    <div>
                        <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Start Date</label>
                        <input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)} className="w-full border p-3 rounded-2xl text-sm" />
                    </div>
                    <div>
                        <label className="block text-xs font-bold text-gray-500 uppercase mb-1">End Date</label>
                        <input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)} className="w-full border p-3 rounded-2xl text-sm" />
                    </div>
                    <button onClick={handleGenerate} className="px-6 py-3 bg-blue-600 text-white font-bold rounded-2xl hover:bg-blue-700">Generate</button>
                </div>
                {error && <p className="text-red-500 text-sm mt-3">{error}</p>}
            </div>

            {reportData ? (
                <div className="flex-1 overflow-y-auto p-8 bg-gray-50/50">
                    <div className="max-w-5xl mx-auto bg-white p-10 shadow-lg border border-gray-200 min-h-[800px] rounded-[28px]">
                        <div className="flex flex-col gap-4 border-b-2 border-gray-800 pb-6 mb-6 lg:flex-row lg:items-start lg:justify-between">
                            <div>
                                <h1 className="text-2xl font-bold text-gray-900">Official Statement</h1>
                                <p className="text-gray-500">BankInsight Systems</p>
                                <p className="text-xs text-gray-400 mt-1">Generated: {new Date().toLocaleString()}</p>
                            </div>
                            <div className="flex flex-wrap gap-3 lg:justify-end">
                                <div className="inline-flex items-center gap-2 px-3 py-1 bg-green-100 text-green-800 rounded-full text-xs font-bold">
                                    <CheckCircle size={14} /> SYSTEM VERIFIED
                                </div>
                                <div className="inline-flex items-center gap-2 px-3 py-1 bg-slate-100 text-slate-700 rounded-full text-xs font-bold">
                                    <Filter size={14} /> {dateFrom || dateTo ? 'Filtered Range' : 'Full History'}
                                </div>
                            </div>
                        </div>

                        <div className="grid grid-cols-1 gap-4 mb-8 md:grid-cols-4">
                            <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-500">Account</p><p className="mt-2 font-bold text-slate-900 font-mono">{reportData.account.id}</p></div>
                            <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-500">Customer</p><p className="mt-2 font-bold text-slate-900">{reportData.customer.name}</p></div>
                            <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-500">Current Balance</p><p className="mt-2 font-bold text-slate-900">{formatCurrency(reportData.account.balance, reportData.account.currency)}</p></div>
                            <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-500">Period Volume</p><p className="mt-2 font-bold text-slate-900">{totalTransactions} entries</p></div>
                        </div>

                        <div className="grid grid-cols-1 gap-8 mb-8 md:grid-cols-2">
                            <div>
                                <h3 className="text-xs font-bold text-gray-400 uppercase">Account Holder</h3>
                                <p className="font-bold text-lg">{reportData.customer.name}</p>
                                <p className="text-sm text-gray-600">{reportData.customer.digitalAddress}</p>
                                <p className="text-sm text-gray-600">CIF: {reportData.customer.id}</p>
                            </div>
                            <div className="text-left md:text-right">
                                <h3 className="text-xs font-bold text-gray-400 uppercase">Statement Window</h3>
                                <p className="font-bold text-lg">{dateFrom || 'Start of history'} to {dateTo || 'Today'}</p>
                                <p className="text-sm text-gray-600">{reportData.account.type}</p>
                                <p className="text-sm font-bold text-blue-600">Bal: {formatCurrency(reportData.account.balance, reportData.account.currency)}</p>
                            </div>
                        </div>

                        <table className="w-full text-sm text-left mb-8">
                            <thead className="bg-gray-100 text-gray-600 font-bold uppercase text-xs border-y border-gray-300">
                                <tr>
                                    <th className="py-3 px-2">Date</th>
                                    <th className="py-3 px-2">Reference</th>
                                    <th className="py-3 px-2 w-1/2">Narration</th>
                                    <th className="py-3 px-2 text-right">Amount</th>
                                    <th className="py-3 px-2 text-right">Running Balance</th>
                                </tr>
                            </thead>
                            <tbody className="font-mono">
                                {reportData.txns.map((tx, idx) => (
                                    <tr key={tx.id} className="border-b border-gray-100">
                                        <td className="py-3 px-2 text-gray-600">{new Date(tx.date).toLocaleDateString()}</td>
                                        <td className="py-3 px-2 text-blue-600">{tx.id.split('-')[1] || tx.id}</td>
                                        <td className="py-3 px-2 text-gray-800 font-sans">{tx.narration}</td>
                                        <td className={`py-3 px-2 text-right font-bold ${tx.type === 'DEPOSIT' || tx.type === 'LOAN_REPAYMENT' ? 'text-green-700' : 'text-red-700'}`}>
                                            {tx.type === 'DEPOSIT' || tx.type === 'LOAN_REPAYMENT' ? '+' : '-'}{tx.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                                        </td>
                                        <td className="py-3 px-2 text-right text-slate-700">{formatCurrency(reportData.runningBalance[idx] || 0, reportData.account.currency)}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>

                        <div className="border-t-2 border-gray-800 pt-4 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                            <p className="text-xs text-gray-400">This is a computer generated document prepared for statement verification.</p>
                            <div className="flex gap-4">
                                <button className="flex items-center gap-2 text-sm font-bold text-gray-600 hover:text-black">
                                    <Printer size={16} /> Print
                                </button>
                                <button className="flex items-center gap-2 text-sm font-bold text-blue-600 hover:text-blue-800">
                                    <Download size={16} /> Download PDF
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            ) : (
                <div className="flex-1 flex flex-col items-center justify-center text-gray-400 p-10">
                    <FileText size={64} className="mb-4 opacity-20" />
                    <p className="text-lg font-semibold text-slate-700">Enter details above to generate statement verification.</p>
                    <p className="mt-2 text-sm text-slate-500">Use the account number and optional date filters to prepare a printable verification view.</p>
                </div>
            )}
        </div>
    );
};

export default StatementVerification;
