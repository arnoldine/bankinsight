import React, { useState, useMemo } from 'react';
import { Settings2, Eye, EyeOff, Receipt, AlertTriangle, ShieldAlert } from 'lucide-react';
import FeePanel from './FeePanel';
import PenaltyPanel from './PenaltyPanel';
import NPLPanel from './NPLPanel';
import { Account, Loan } from '../types';

interface OperationsHubProps {
  accounts: Account[];
  loans: Loan[];
  onOperationComplete?: () => void;
  initialTab?: 'fees' | 'penalties' | 'npl';
}

const OperationsHub: React.FC<OperationsHubProps> = ({ accounts, loans, onOperationComplete, initialTab = 'fees' }) => {
  const [activeTab, setActiveTab] = useState<'fees' | 'penalties' | 'npl'>(initialTab);
  const [selectedAccountId, setSelectedAccountId] = useState<string>('');
  const [selectedLoanId, setSelectedLoanId] = useState<string>('');
  const [showDetails, setShowDetails] = useState(false);

  React.useEffect(() => {
    setActiveTab(initialTab);
  }, [initialTab]);

  const selectedAccount = useMemo(() => accounts.find(a => a.id === selectedAccountId), [accounts, selectedAccountId]);
  const selectedLoan = useMemo(() => loans.find(l => l.id === selectedLoanId), [loans, selectedLoanId]);
  const activeLoans = loans.filter((loan) => loan.status === 'ACTIVE');
  const loansInArrears = activeLoans.filter((loan) => !['0', 'PAR_0', '', undefined, null].includes(loan.parBucket as any)).length;

  const tabConfig = [
    { id: 'fees' as const, label: 'Fee Assessment', icon: Receipt },
    { id: 'penalties' as const, label: 'Penalties', icon: AlertTriangle },
    { id: 'npl' as const, label: 'NPL Classification', icon: ShieldAlert },
  ];

  return (
    <div className="rounded-[28px] border border-slate-200 bg-[linear-gradient(180deg,#f8fafc,#eef2f7)] p-6 shadow-sm">
      <div className="rounded-[28px] bg-[linear-gradient(135deg,#0f172a,#1e293b,#334155)] p-6 text-white mb-6">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.22em] text-white/60">Operations</p>
            <h2 className="mt-2 text-2xl font-bold">Assess fees, penalties, and NPL classifications.</h2>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="rounded-2xl bg-white/10 px-4 py-3"><p className="text-xs uppercase tracking-[0.18em] text-white/70">Accounts</p><p className="mt-1 text-xl font-bold">{accounts.length}</p></div>
            <div className="rounded-2xl bg-white/10 px-4 py-3"><p className="text-xs uppercase tracking-[0.18em] text-white/70">Active loans</p><p className="mt-1 text-xl font-bold">{activeLoans.length}</p></div>
            <div className="rounded-2xl bg-white/10 px-4 py-3"><p className="text-xs uppercase tracking-[0.18em] text-white/70">Arrears watch</p><p className="mt-1 text-xl font-bold">{loansInArrears}</p></div>
          </div>
        </div>
      </div>

      <div className="flex items-center gap-3 mb-6">
        <Settings2 className="w-6 h-6 text-slate-700" />
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Operations Hub</h2>
          <p className="text-sm text-slate-600">Manage fees, penalties, and NPL classifications.</p>
        </div>
      </div>

      <div className="flex gap-2 mb-6 border-b border-slate-300 overflow-x-auto">
        {tabConfig.map(tab => {
          const Icon = tab.icon;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-4 py-2 font-medium text-sm transition flex items-center gap-2 whitespace-nowrap ${
                activeTab === tab.id
                  ? 'text-slate-900 border-b-2 border-slate-900'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              <Icon className="w-4 h-4" /> {tab.label}
            </button>
          );
        })}
      </div>

      <div className="space-y-6">
        {activeTab === 'fees' && (
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-2">Select Account</label>
              <select
                value={selectedAccountId}
                onChange={(e) => setSelectedAccountId(e.target.value)}
                className="w-full px-4 py-3 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">-- Choose an account --</option>
                {accounts.map(acc => (
                  <option key={acc.id} value={acc.id}>
                    {acc.id} - {acc.cif} (Balance: GHS {(acc.balance - acc.lienAmount).toFixed(2)})
                  </option>
                ))}
              </select>
            </div>

            {selectedAccount && (
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                <div className="lg:col-span-2">
                  <FeePanel accountId={selectedAccount.id} accountBalance={selectedAccount.balance - selectedAccount.lienAmount} onFeeAssessed={onOperationComplete} />
                </div>
                <div className="space-y-4">
                  <div className="bg-white rounded-2xl border border-slate-200 p-4">
                    <p className="text-xs font-semibold text-slate-600 uppercase mb-3">Account Details</p>
                    {showDetails ? (
                      <div className="space-y-2 text-sm">
                        <p><span className="font-medium">ID:</span> {selectedAccount.id}</p>
                        <p><span className="font-medium">Customer:</span> {selectedAccount.cif}</p>
                        <p><span className="font-medium">Type:</span> {selectedAccount.type}</p>
                        <p><span className="font-medium">Status:</span> {selectedAccount.status}</p>
                        <p><span className="font-medium">Balance:</span> GHS {selectedAccount.balance.toFixed(2)}</p>
                        <p><span className="font-medium">Lien:</span> GHS {selectedAccount.lienAmount.toFixed(2)}</p>
                        <p><span className="font-medium">Available:</span> GHS {(selectedAccount.balance - selectedAccount.lienAmount).toFixed(2)}</p>
                      </div>
                    ) : (
                      <p className="text-sm text-slate-600">Balance: GHS {selectedAccount.balance.toFixed(2)}</p>
                    )}
                    <button onClick={() => setShowDetails(!showDetails)} className="mt-3 w-full text-xs text-slate-600 hover:text-slate-900 font-medium flex items-center justify-center gap-1">
                      {showDetails ? <EyeOff className="w-3 h-3" /> : <Eye className="w-3 h-3" />}
                      {showDetails ? 'Hide' : 'Show'} Details
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}

        {activeTab === 'penalties' && (
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-2">Select Loan</label>
              <select
                value={selectedLoanId}
                onChange={(e) => setSelectedLoanId(e.target.value)}
                className="w-full px-4 py-3 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-amber-500"
              >
                <option value="">-- Choose a loan --</option>
                {activeLoans.map(loan => (
                  <option key={loan.id} value={loan.id}>
                    {loan.id} - {loan.cif} (Outstanding: GHS {loan.outstandingBalance.toFixed(2)})
                  </option>
                ))}
              </select>
            </div>

            {selectedLoan && (
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                <div className="lg:col-span-2">
                  <PenaltyPanel loanId={selectedLoan.id} outstandingBalance={selectedLoan.outstandingBalance} daysPastDue={0} onPenaltyAssessed={onOperationComplete} />
                </div>
                <div className="space-y-4">
                  <div className="bg-white rounded-2xl border border-slate-200 p-4">
                    <p className="text-xs font-semibold text-slate-600 uppercase mb-3">Loan Details</p>
                    {showDetails ? (
                      <div className="space-y-2 text-sm">
                        <p><span className="font-medium">ID:</span> {selectedLoan.id}</p>
                        <p><span className="font-medium">Customer:</span> {selectedLoan.cif}</p>
                        <p><span className="font-medium">Principal:</span> GHS {selectedLoan.principal.toFixed(2)}</p>
                        <p><span className="font-medium">Rate:</span> {selectedLoan.rate}% p.a.</p>
                        <p><span className="font-medium">Term:</span> {selectedLoan.termMonths} months</p>
                        <p><span className="font-medium">Status:</span> {selectedLoan.status}</p>
                        <p><span className="font-medium">Outstanding:</span> GHS {selectedLoan.outstandingBalance.toFixed(2)}</p>
                      </div>
                    ) : (
                      <p className="text-sm text-slate-600">Outstanding: GHS {selectedLoan.outstandingBalance.toFixed(2)}</p>
                    )}
                    <button onClick={() => setShowDetails(!showDetails)} className="mt-3 w-full text-xs text-slate-600 hover:text-slate-900 font-medium flex items-center justify-center gap-1">
                      {showDetails ? <EyeOff className="w-3 h-3" /> : <Eye className="w-3 h-3" />}
                      {showDetails ? 'Hide' : 'Show'} Details
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}

        {activeTab === 'npl' && (
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-2">Select Loan for Classification</label>
              <select
                value={selectedLoanId}
                onChange={(e) => setSelectedLoanId(e.target.value)}
                className="w-full px-4 py-3 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-red-500"
              >
                <option value="">-- Choose a loan --</option>
                {activeLoans.map(loan => (
                  <option key={loan.id} value={loan.id}>
                    {loan.id} - {loan.cif} (Outstanding: GHS {loan.outstandingBalance.toFixed(2)})
                  </option>
                ))}
              </select>
            </div>

            {selectedLoan && (
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                <div className="lg:col-span-2">
                  <NPLPanel loanId={selectedLoan.id} onClassificationComplete={onOperationComplete} />
                </div>
                <div className="space-y-4">
                  <div className="bg-white rounded-2xl border border-slate-200 p-4">
                    <p className="text-xs font-semibold text-slate-600 uppercase mb-3">Loan Details</p>
                    {showDetails ? (
                      <div className="space-y-2 text-sm">
                        <p><span className="font-medium">ID:</span> {selectedLoan.id}</p>
                        <p><span className="font-medium">Customer:</span> {selectedLoan.cif}</p>
                        <p><span className="font-medium">Principal:</span> GHS {selectedLoan.principal.toFixed(2)}</p>
                        <p><span className="font-medium">Status:</span> {selectedLoan.status}</p>
                        <p><span className="font-medium">PAR Bucket:</span> {selectedLoan.parBucket}</p>
                        <p><span className="font-medium">Outstanding:</span> GHS {selectedLoan.outstandingBalance.toFixed(2)}</p>
                      </div>
                    ) : (
                      <div className="space-y-1 text-sm">
                        <p className="text-slate-600">Outstanding: GHS {selectedLoan.outstandingBalance.toFixed(2)}</p>
                        <p className="text-slate-600">PAR Bucket: {selectedLoan.parBucket}</p>
                      </div>
                    )}
                    <button onClick={() => setShowDetails(!showDetails)} className="mt-3 w-full text-xs text-slate-600 hover:text-slate-900 font-medium flex items-center justify-center gap-1">
                      {showDetails ? <EyeOff className="w-3 h-3" /> : <Eye className="w-3 h-3" />}
                      {showDetails ? 'Hide' : 'Show'} Details
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default OperationsHub;
