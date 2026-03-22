
import React, { useState, useMemo, useEffect } from 'react';
import { Transaction } from '../types';
import { Search, Filter, ArrowUpDown, ArrowUp, ArrowDown, Download, RefreshCw, X, Wifi } from 'lucide-react';

interface TransactionExplorerProps {
  transactions: Transaction[];
}

type SortField = 'date' | 'amount' | 'id' | 'accountId';
type SortDirection = 'asc' | 'desc';

const TransactionExplorer: React.FC<TransactionExplorerProps> = ({ transactions }) => {
  const [filters, setFilters] = useState({
    accountId: '',
    tellerId: '',
    narration: '',
    type: 'ALL',
    status: 'ALL',
    startDate: '',
    endDate: '',
    minAmount: '',
    maxAmount: ''
  });

  const [sort, setSort] = useState<{ field: SortField; direction: SortDirection }>({
    field: 'date',
    direction: 'desc'
  });

  const [isLive, setIsLive] = useState(true);
  const [lastUpdated, setLastUpdated] = useState(new Date());

  // Effect to update timestamp when transactions prop changes (Live Feed)
  useEffect(() => {
    setLastUpdated(new Date());
  }, [transactions]);

  const handleSort = (field: SortField) => {
    setSort(prev => ({
      field,
      direction: prev.field === field && prev.direction === 'desc' ? 'asc' : 'desc'
    }));
  };

  const filteredTransactions = useMemo(() => {
    return transactions.filter(tx => {
      const matchesAccount = tx.accountId.toLowerCase().includes(filters.accountId.toLowerCase());
      const matchesTeller = tx.tellerId.toLowerCase().includes(filters.tellerId.toLowerCase());
      const matchesNarration = tx.narration.toLowerCase().includes(filters.narration.toLowerCase());
      const matchesType = filters.type === 'ALL' || tx.type === filters.type;
      const matchesStatus = filters.status === 'ALL' || tx.status === filters.status;
      
      const txDate = new Date(tx.date).setHours(0,0,0,0);
      const start = filters.startDate ? new Date(filters.startDate).setHours(0,0,0,0) : null;
      const end = filters.endDate ? new Date(filters.endDate).setHours(0,0,0,0) : null;
      
      const matchesDate = (!start || txDate >= start) && (!end || txDate <= end);
      
      const amount = tx.amount;
      const min = filters.minAmount ? parseFloat(filters.minAmount) : null;
      const max = filters.maxAmount ? parseFloat(filters.maxAmount) : null;
      const matchesAmount = (!min || amount >= min) && (!max || amount <= max);

      return matchesAccount && matchesTeller && matchesNarration && matchesType && matchesStatus && matchesDate && matchesAmount;
    }).sort((a, b) => {
      const dir = sort.direction === 'asc' ? 1 : -1;
      if (sort.field === 'date') return (new Date(a.date).getTime() - new Date(b.date).getTime()) * dir;
      if (sort.field === 'amount') return (a.amount - b.amount) * dir;
      if (sort.field === 'id') return a.id.localeCompare(b.id) * dir;
      if (sort.field === 'accountId') return a.accountId.localeCompare(b.accountId) * dir;
      return 0;
    });
  }, [transactions, filters, sort]);

  const clearFilters = () => {
    setFilters({
        accountId: '',
        tellerId: '',
        narration: '',
        type: 'ALL',
        status: 'ALL',
        startDate: '',
        endDate: '',
        minAmount: '',
        maxAmount: ''
    });
  };

  const SortIcon = ({ field }: { field: SortField }) => {
    if (sort.field !== field) return <ArrowUpDown size={14} className="text-gray-300" />;
    return sort.direction === 'asc' ? <ArrowUp size={14} className="text-blue-600" /> : <ArrowDown size={14} className="text-blue-600" />;
  };

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 flex flex-col h-full overflow-hidden">
      {/* Header */}
      <div className="p-6 border-b border-gray-200">
        <div className="flex justify-between items-center mb-4">
          <div className="flex items-center gap-3">
            <h2 className="text-xl font-bold text-gray-800 flex items-center gap-2">
                <RefreshCw className="text-blue-600" /> Transaction Explorer
            </h2>
            <div className={`flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${isLive ? 'bg-green-50 text-green-700 border-green-200' : 'bg-gray-100 text-gray-500 border-gray-200'}`}>
                <Wifi size={12} className={isLive ? "animate-pulse" : ""} />
                {isLive ? 'Live Feed' : 'Offline'}
            </div>
          </div>
          <div className="flex items-center gap-4">
             <span className="text-xs text-gray-500">
                 Last update: <span className="font-mono text-gray-700">{lastUpdated.toLocaleTimeString()}</span>
             </span>
             <div className="text-sm text-gray-500 bg-gray-100 px-3 py-1 rounded-md">
                Showing {filteredTransactions.length} of {transactions.length}
            </div>
          </div>
        </div>

        {/* Filters */}
        <div className="bg-gray-50 p-4 rounded-lg border border-gray-200">
            <div className="grid grid-cols-1 md:grid-cols-4 lg:grid-cols-6 gap-4 mb-4">
                <div className="md:col-span-1">
                    <label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Account ID</label>
                    <input 
                        type="text" 
                        value={filters.accountId}
                        onChange={e => setFilters({...filters, accountId: e.target.value})}
                        placeholder="201..."
                        className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                    />
                </div>
                <div className="md:col-span-1">
                    <label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Teller ID</label>
                    <input 
                        type="text" 
                        value={filters.tellerId}
                        onChange={e => setFilters({...filters, tellerId: e.target.value})}
                        placeholder="TLR..."
                        className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                    />
                </div>
                <div className="md:col-span-1">
                    <label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Type</label>
                    <select 
                        value={filters.type}
                        onChange={e => setFilters({...filters, type: e.target.value})}
                        className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                    >
                        <option value="ALL">All Types</option>
                        <option value="DEPOSIT">Deposit</option>
                        <option value="WITHDRAWAL">Withdrawal</option>
                        <option value="TRANSFER">Transfer</option>
                        <option value="LOAN_REPAYMENT">Loan Repayment</option>
                    </select>
                </div>
                <div className="md:col-span-1">
                    <label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Status</label>
                    <select 
                        value={filters.status}
                        onChange={e => setFilters({...filters, status: e.target.value})}
                        className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                    >
                        <option value="ALL">All Statuses</option>
                        <option value="POSTED">Posted</option>
                        <option value="PENDING">Pending</option>
                        <option value="REJECTED">Rejected</option>
                    </select>
                </div>
                <div className="md:col-span-2">
                    <label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Amount Range</label>
                    <div className="flex gap-2">
                        <input 
                            type="number" 
                            placeholder="Min"
                            value={filters.minAmount}
                            onChange={e => setFilters({...filters, minAmount: e.target.value})}
                            className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                        />
                        <input 
                            type="number" 
                            placeholder="Max"
                            value={filters.maxAmount}
                            onChange={e => setFilters({...filters, maxAmount: e.target.value})}
                            className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                        />
                    </div>
                </div>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-4 lg:grid-cols-6 gap-4">
                <div className="md:col-span-2">
                    <label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Narration Search</label>
                    <div className="relative">
                        <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                        <input 
                            type="text" 
                            value={filters.narration}
                            onChange={e => setFilters({...filters, narration: e.target.value})}
                            placeholder="Search description..."
                            className="w-full pl-9 pr-3 p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                        />
                    </div>
                </div>
                <div className="md:col-span-3">
                     <label className="block text-xs font-semibold text-gray-500 uppercase mb-1">Date Range</label>
                     <div className="flex gap-2">
                        <input 
                            type="date" 
                            value={filters.startDate}
                            onChange={e => setFilters({...filters, startDate: e.target.value})}
                            className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                        />
                        <span className="text-gray-400 self-center">-</span>
                        <input 
                            type="date" 
                            value={filters.endDate}
                            onChange={e => setFilters({...filters, endDate: e.target.value})}
                            className="w-full p-2 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 outline-none"
                        />
                    </div>
                </div>
                <div className="md:col-span-1 flex items-end">
                    <button 
                        onClick={clearFilters}
                        className="w-full py-2 bg-white border border-gray-300 text-gray-600 rounded text-sm font-medium hover:bg-gray-100 flex items-center justify-center gap-1"
                    >
                        <X size={14} /> Clear Filters
                    </button>
                </div>
            </div>
        </div>
      </div>

      {/* Table */}
      <div className="flex-1 overflow-auto">
        <table className="w-full text-left text-sm">
            <thead className="bg-gray-100 text-gray-600 font-semibold sticky top-0 z-10 border-b border-gray-200">
                <tr>
                    <th 
                        className="p-4 cursor-pointer hover:bg-gray-200 transition-colors select-none"
                        onClick={() => handleSort('date')}
                    >
                        <div className="flex items-center gap-1">Date <SortIcon field="date" /></div>
                    </th>
                    <th 
                        className="p-4 cursor-pointer hover:bg-gray-200 transition-colors select-none"
                        onClick={() => handleSort('id')}
                    >
                        <div className="flex items-center gap-1">Txn ID <SortIcon field="id" /></div>
                    </th>
                    <th 
                        className="p-4 cursor-pointer hover:bg-gray-200 transition-colors select-none"
                        onClick={() => handleSort('accountId')}
                    >
                        <div className="flex items-center gap-1">Account <SortIcon field="accountId" /></div>
                    </th>
                    <th className="p-4">Type</th>
                    <th 
                        className="p-4 text-right cursor-pointer hover:bg-gray-200 transition-colors select-none"
                        onClick={() => handleSort('amount')}
                    >
                        <div className="flex items-center justify-end gap-1">Amount <SortIcon field="amount" /></div>
                    </th>
                    <th className="p-4">Narration</th>
                    <th className="p-4">Teller</th>
                    <th className="p-4 text-center">Status</th>
                </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
                {filteredTransactions.map(tx => (
                    <tr key={tx.id} className="hover:bg-blue-50 group transition-colors animate-in fade-in slide-in-from-top-1 duration-300">
                        <td className="p-4 text-gray-600 whitespace-nowrap">
                            {new Date(tx.date).toLocaleString()}
                        </td>
                        <td className="p-4 font-mono text-blue-600 text-xs">{tx.id}</td>
                        <td className="p-4 font-medium text-gray-800">{tx.accountId}</td>
                        <td className="p-4">
                            <span className={`px-2 py-1 rounded text-xs font-bold ${
                                tx.type === 'DEPOSIT' ? 'bg-green-100 text-green-700' :
                                tx.type === 'WITHDRAWAL' ? 'bg-red-100 text-red-700' :
                                'bg-gray-100 text-gray-700'
                            }`}>
                                {tx.type}
                            </span>
                        </td>
                        <td className="p-4 text-right font-mono font-medium">
                            {tx.amount.toLocaleString(undefined, {minimumFractionDigits: 2})}
                        </td>
                        <td className="p-4 text-gray-500 text-xs max-w-xs truncate" title={tx.narration}>
                            {tx.narration}
                        </td>
                         <td className="p-4 text-gray-500 text-xs">
                            {tx.tellerId}
                        </td>
                        <td className="p-4 text-center">
                            <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase border ${
                                tx.status === 'POSTED' ? 'bg-green-50 text-green-700 border-green-200' : 
                                tx.status === 'REJECTED' ? 'bg-red-50 text-red-700 border-red-200' :
                                'bg-yellow-50 text-yellow-700 border-yellow-200'
                            }`}>
                                {tx.status}
                            </span>
                        </td>
                    </tr>
                ))}
            </tbody>
        </table>
        {filteredTransactions.length === 0 && (
             <div className="flex flex-col items-center justify-center h-48 text-gray-400">
                <Search size={48} className="mb-2 opacity-20" />
                <p>No transactions match your criteria.</p>
            </div>
        )}
      </div>
    </div>
  );
};

export default TransactionExplorer;
