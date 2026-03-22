
import React, { useState, useMemo } from 'react';
import { Transaction, Loan, Account, GLAccount, Branch, Customer } from '../types';
import { Download, Filter, Calendar, Search, FileText, Printer, FileSpreadsheet, ChevronDown, Table, RefreshCw, ShieldAlert } from 'lucide-react';

interface BIReportsProps {
    transactions: Transaction[];
    loans: Loan[];
    accounts: Account[];
    glAccounts: GLAccount[];
    branches: Branch[];
    customers: Customer[];
}

type ReportType = 'TRANSACTIONS' | 'LOANS' | 'ACCOUNTS' | 'GL_TRIAL_BALANCE';

const BIReports: React.FC<BIReportsProps> = ({ transactions, loans, accounts, glAccounts, branches, customers }) => {
    const [reportType, setReportType] = useState<ReportType>('TRANSACTIONS');
    const [dateFrom, setDateFrom] = useState('');
    const [dateTo, setDateTo] = useState('');
    const [selectedBranch, setSelectedBranch] = useState('ALL');
    const [searchTerm, setSearchTerm] = useState('');
    const [statusFilter, setStatusFilter] = useState('ALL');

    // --- DATA FILTERING ENGINE ---
    const reportData = useMemo(() => {
        let data: any[] = [];

        switch (reportType) {
            case 'TRANSACTIONS':
                data = transactions.map(t => {
                    const acc = accounts.find(a => a.id === t.accountId);
                    return {
                        ...t,
                        branchId: acc?.branchId || 'Unknown',
                        customerName: customers.find(c => c.id === acc?.cif)?.name || 'N/A'
                    };
                });
                if (dateFrom) data = data.filter(d => new Date(d.date) >= new Date(dateFrom));
                if (dateTo) data = data.filter(d => new Date(d.date) <= new Date(dateTo));
                if (selectedBranch !== 'ALL') data = data.filter(d => d.branchId === selectedBranch);
                if (statusFilter !== 'ALL') data = data.filter(d => d.status === statusFilter);
                if (searchTerm) data = data.filter(d => 
                    d.id.toLowerCase().includes(searchTerm.toLowerCase()) || 
                    d.accountId.includes(searchTerm) || 
                    d.narration.toLowerCase().includes(searchTerm.toLowerCase())
                );
                return data.sort((a,b) => new Date(b.date).getTime() - new Date(a.date).getTime());

            case 'LOANS':
                data = loans.map(l => {
                    const branchId = accounts.find(a => a.cif === l.cif)?.branchId || 'BR001'; // Infer branch from CIF
                    return {
                        ...l,
                        branchId,
                        customerName: customers.find(c => c.id === l.cif)?.name || (l.groupId ? `Group: ${l.groupId}` : 'N/A')
                    };
                });
                if (dateFrom) data = data.filter(d => new Date(d.disbursementDate) >= new Date(dateFrom));
                if (dateTo) data = data.filter(d => new Date(d.disbursementDate) <= new Date(dateTo));
                // Note: Branch filtering on loans is inferred in this mock
                if (statusFilter !== 'ALL') data = data.filter(d => d.status === statusFilter);
                if (searchTerm) data = data.filter(d => 
                    d.id.toLowerCase().includes(searchTerm.toLowerCase()) || 
                    d.cif?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                    d.customerName.toLowerCase().includes(searchTerm.toLowerCase())
                );
                return data;

            case 'ACCOUNTS':
                data = accounts.map(a => ({
                    ...a,
                    customerName: customers.find(c => c.id === a.cif)?.name || 'N/A'
                }));
                if (selectedBranch !== 'ALL') data = data.filter(d => d.branchId === selectedBranch);
                if (statusFilter !== 'ALL') data = data.filter(d => d.status === statusFilter);
                if (searchTerm) data = data.filter(d => 
                    d.id.includes(searchTerm) || 
                    d.cif.toLowerCase().includes(searchTerm.toLowerCase()) ||
                    d.customerName.toLowerCase().includes(searchTerm.toLowerCase())
                );
                return data;

            case 'GL_TRIAL_BALANCE':
                data = glAccounts.filter(g => !g.isHeader);
                if (searchTerm) data = data.filter(d => d.name.toLowerCase().includes(searchTerm.toLowerCase()) || d.code.includes(searchTerm));
                return data.sort((a,b) => a.code.localeCompare(b.code));

            default:
                return [];
        }
    }, [reportType, transactions, loans, accounts, glAccounts, customers, dateFrom, dateTo, selectedBranch, statusFilter, searchTerm]);

    // --- EXPORT HANDLERS ---
    const handleExport = (format: 'CSV' | 'PDF' | 'XLSX') => {
        if (reportData.length === 0) {
            alert("No data to export");
            return;
        }

        if (format === 'CSV') {
            const headers = Object.keys(reportData[0]).join(',');
            const rows = reportData.map(row => Object.values(row).map(v => `"${v}"`).join(','));
            const csvContent = [headers, ...rows].join('\n');
            const blob = new Blob([csvContent], { type: 'text/csv' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `BankInsight_${reportType}_${new Date().toISOString().split('T')[0]}.csv`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        } else {
            alert(`${format} export simulation started. File will download shortly.`);
        }
    };

    return (
        <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            {/* Header / Toolbar */}
            <div className="p-6 border-b border-gray-200 bg-gray-50">
                <div className="flex justify-between items-start mb-6">
                    <div>
                        <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
                            <FileText className="text-blue-600" /> Business Intelligence Reports
                        </h2>
                        <p className="text-sm text-gray-500 mt-1">Generate and export system-wide operational reports.</p>
                    </div>
                    <div className="flex gap-2">
                        <button onClick={() => handleExport('CSV')} className="flex items-center gap-2 px-3 py-2 bg-white border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-100">
                            <FileSpreadsheet size={16} /> CSV
                        </button>
                        <button onClick={() => handleExport('PDF')} className="flex items-center gap-2 px-3 py-2 bg-white border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-100">
                            <Printer size={16} /> PDF
                        </button>
                        <button onClick={() => handleExport('XLSX')} className="flex items-center gap-2 px-3 py-2 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700 shadow-sm">
                            <Download size={16} /> Excel
                        </button>
                    </div>
                </div>

                {/* Report Selector Tabs */}
                <div className="flex gap-1 bg-gray-200 p-1 rounded-lg mb-6 w-fit flex-wrap">
                    {[
                        { id: 'TRANSACTIONS', label: 'Transactions' },
                        { id: 'LOANS', label: 'Loan Portfolio' },
                        { id: 'ACCOUNTS', label: 'Deposit Accounts' },
                        { id: 'GL_TRIAL_BALANCE', label: 'Trial Balance' }
                    ].map(type => (
                        <button
                            key={type.id}
                            onClick={() => { setReportType(type.id as ReportType); setSearchTerm(''); }}
                            className={`px-4 py-1.5 text-sm font-bold rounded-md transition-all ${
                                reportType === type.id 
                                ? 'bg-white text-blue-700 shadow-sm' 
                                : 'text-gray-600 hover:text-gray-900 hover:bg-gray-300'
                            }`}
                        >
                            {type.label}
                        </button>
                    ))}
                </div>

                {/* Dynamic Filters */}
                <div className="grid grid-cols-1 md:grid-cols-4 lg:grid-cols-5 gap-4">
                    <div className="col-span-1 md:col-span-2 relative">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
                        <input 
                            type="text" 
                            placeholder="Search records..." 
                            value={searchTerm}
                            onChange={e => setSearchTerm(e.target.value)}
                            className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                    </div>

                    {reportType !== 'GL_TRIAL_BALANCE' && (
                        <>
                            <div className="col-span-1">
                                <div className="relative">
                                    <Filter className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
                                    <select 
                                        className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm bg-white appearance-none focus:outline-none focus:ring-2 focus:ring-blue-500"
                                        value={statusFilter}
                                        onChange={e => setStatusFilter(e.target.value)}
                                    >
                                        <option value="ALL">All Statuses</option>
                                        <option value="ACTIVE">Active</option>
                                        <option value="PENDING">Pending</option>
                                        <option value="CLOSED">Closed/Paid</option>
                                        <option value="FROZEN">Frozen</option>
                                    </select>
                                    <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none" size={14} />
                                </div>
                            </div>

                            {reportType !== 'LOANS' && (
                                <div className="col-span-1">
                                    <div className="relative">
                                        <select 
                                            className="w-full px-4 py-2 border border-gray-300 rounded-lg text-sm bg-white appearance-none focus:outline-none focus:ring-2 focus:ring-blue-500"
                                            value={selectedBranch}
                                            onChange={e => setSelectedBranch(e.target.value)}
                                        >
                                            <option value="ALL">All Branches</option>
                                            {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                                        </select>
                                        <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none" size={14} />
                                    </div>
                                </div>
                            )}

                            <div className="col-span-1 flex gap-2 items-center bg-white border border-gray-300 rounded-lg px-2">
                                <Calendar size={16} className="text-gray-400 shrink-0"/>
                                <input 
                                    type="date" 
                                    className="w-full py-1.5 text-sm outline-none"
                                    value={dateFrom}
                                    onChange={e => setDateFrom(e.target.value)}
                                />
                            </div>
                        </>
                    )}
                </div>
            </div>

            {/* Results Table */}
            <div className="flex-1 overflow-auto">
                <table className="w-full text-left text-sm border-collapse">
                    <thead className="bg-gray-100 text-gray-600 font-bold text-xs uppercase sticky top-0 z-10 border-b border-gray-300">
                        {reportType === 'TRANSACTIONS' && (
                            <tr>
                                <th className="p-4">Date</th>
                                <th className="p-4">Txn ID</th>
                                <th className="p-4">Account</th>
                                <th className="p-4">Branch</th>
                                <th className="p-4">Type</th>
                                <th className="p-4 text-right">Amount</th>
                                <th className="p-4 text-center">Status</th>
                            </tr>
                        )}
                        {reportType === 'LOANS' && (
                            <tr>
                                <th className="p-4">Loan ID</th>
                                <th className="p-4">Customer</th>
                                <th className="p-4">Product</th>
                                <th className="p-4 text-right">Principal</th>
                                <th className="p-4 text-right">Outstanding</th>
                                <th className="p-4 text-center">Par Bucket</th>
                                <th className="p-4 text-center">Status</th>
                            </tr>
                        )}
                        {reportType === 'ACCOUNTS' && (
                            <tr>
                                <th className="p-4">Account No</th>
                                <th className="p-4">Customer Name</th>
                                <th className="p-4">Product</th>
                                <th className="p-4">Branch</th>
                                <th className="p-4 text-right">Balance</th>
                                <th className="p-4 text-center">Status</th>
                            </tr>
                        )}
                        {reportType === 'GL_TRIAL_BALANCE' && (
                            <tr>
                                <th className="p-4">GL Code</th>
                                <th className="p-4">Account Name</th>
                                <th className="p-4">Category</th>
                                <th className="p-4">Currency</th>
                                <th className="p-4 text-right">Balance</th>
                            </tr>
                        )}
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {reportData.map((row: any, idx) => (
                            <tr key={idx} className="hover:bg-blue-50 transition-colors">
                                {reportType === 'TRANSACTIONS' && (
                                    <>
                                        <td className="p-4 text-gray-600 whitespace-nowrap">{new Date(row.date).toLocaleString()}</td>
                                        <td className="p-4 font-mono text-xs text-blue-600">{row.id}</td>
                                        <td className="p-4">
                                            <div className="font-medium text-gray-900">{row.customerName}</div>
                                            <div className="text-xs text-gray-500 font-mono">{row.accountId}</div>
                                        </td>
                                        <td className="p-4 text-gray-600 text-xs">{row.branchId}</td>
                                        <td className="p-4"><span className="px-2 py-0.5 bg-gray-100 rounded text-xs font-bold">{row.type}</span></td>
                                        <td className="p-4 text-right font-mono font-medium">{row.amount.toLocaleString(undefined, {minimumFractionDigits: 2})}</td>
                                        <td className="p-4 text-center"><span className={`text-[10px] font-bold px-2 py-1 rounded ${row.status === 'POSTED' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'}`}>{row.status}</span></td>
                                    </>
                                )}
                                {reportType === 'LOANS' && (
                                    <>
                                        <td className="p-4 font-mono text-blue-600">{row.id}</td>
                                        <td className="p-4">
                                            <div className="font-medium text-gray-900">{row.customerName}</div>
                                            <div className="text-xs text-gray-500">{row.cif}</div>
                                        </td>
                                        <td className="p-4 text-gray-600">{row.productName}</td>
                                        <td className="p-4 text-right font-mono">{row.principal.toLocaleString()}</td>
                                        <td className="p-4 text-right font-mono font-bold">{row.outstandingBalance.toLocaleString()}</td>
                                        <td className="p-4 text-center">
                                            <span className={`px-2 py-0.5 rounded text-xs font-bold ${row.parBucket === '0' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                                                {row.parBucket === '0' ? 'Standard' : row.parBucket}
                                            </span>
                                        </td>
                                        <td className="p-4 text-center"><span className="px-2 py-0.5 bg-gray-100 rounded text-xs font-bold">{row.status}</span></td>
                                    </>
                                )}
                                {reportType === 'ACCOUNTS' && (
                                    <>
                                        <td className="p-4 font-mono text-blue-600">{row.id}</td>
                                        <td className="p-4 font-medium text-gray-900">{row.customerName}</td>
                                        <td className="p-4 text-gray-600">{row.productCode}</td>
                                        <td className="p-4 text-gray-500 text-xs">{row.branchId}</td>
                                        <td className="p-4 text-right font-mono font-bold text-gray-800">{row.balance.toLocaleString(undefined, {minimumFractionDigits: 2})} {row.currency}</td>
                                        <td className="p-4 text-center"><span className="px-2 py-0.5 bg-green-50 text-green-700 border border-green-200 rounded text-xs font-bold">{row.status}</span></td>
                                    </>
                                )}
                                {reportType === 'GL_TRIAL_BALANCE' && (
                                    <>
                                        <td className="p-4 font-mono text-purple-600 font-bold">{row.code}</td>
                                        <td className="p-4 font-medium text-gray-800">{row.name}</td>
                                        <td className="p-4"><span className="px-2 py-0.5 bg-gray-100 text-gray-600 rounded text-xs uppercase font-bold">{row.category}</span></td>
                                        <td className="p-4 text-gray-500 text-xs">{row.currency}</td>
                                        <td className={`p-4 text-right font-mono font-bold ${row.balance < 0 ? 'text-red-600' : 'text-gray-900'}`}>{row.balance.toLocaleString(undefined, {minimumFractionDigits: 2})}</td>
                                    </>
                                )}
                            </tr>
                        ))}
                    </tbody>
                </table>
                {reportData.length === 0 && (
                    <div className="flex flex-col items-center justify-center h-64 text-gray-400">
                        <RefreshCw size={32} className="mb-2 opacity-20" />
                        <p>No records found matching filters.</p>
                    </div>
                )}
            </div>
            
            <div className="p-3 border-t border-gray-200 bg-gray-50 text-xs text-gray-500 flex justify-between">
                <span>Total Records: {reportData.length}</span>
                <span>Generated: {new Date().toLocaleString()}</span>
            </div>
        </div>
    );
};

export default BIReports;
