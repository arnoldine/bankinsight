import React, { useState, useMemo } from 'react';
import { BookOpen, FileText, CheckCircle, AlertTriangle, Calculator, TrendingUp, ArrowRightLeft, Lock } from 'lucide-react';
import { GLAccount, JournalEntry, Transaction } from '../types';
import PermissionGuard from './PermissionGuard';

interface AccountantScreenProps {
  glAccounts: GLAccount[];
  journalEntries: JournalEntry[];
  transactions: Transaction[];
  onPostJournal?: (entry: any) => void;
  onReconcile?: (accountCode: string) => void;
}

const AccountantScreen: React.FC<AccountantScreenProps> = ({
  glAccounts,
  journalEntries,
  transactions,
  onPostJournal,
  onReconcile
}) => {
  const [activeTab, setActiveTab] = useState<'journal' | 'reconcile' | 'reports' | 'closing'>('journal');
  const [journalDate, setJournalDate] = useState(new Date().toISOString().split('T')[0]);
  const [journalRef, setJournalRef] = useState('');
  const [journalDesc, setJournalDesc] = useState('');
  const [journalLines, setJournalLines] = useState([
    { accountCode: '', debit: '', credit: '', narration: '' },
    { accountCode: '', debit: '', credit: '', narration: '' }
  ]);

  const metrics = useMemo(() => {
    const postedToday = journalEntries.filter(j => 
      j.date === new Date().toISOString().split('T')[0]
    ).length;
    
    const pendingReconciliations = 5; // Mock
    const totalDebits = glAccounts.filter(a => a.category === 'ASSET' || a.category === 'EXPENSE')
      .reduce((sum, a) => sum + Math.abs(a.balance), 0);
    const totalCredits = glAccounts.filter(a => a.category === 'LIABILITY' || a.category === 'INCOME')
      .reduce((sum, a) => sum + Math.abs(a.balance), 0);
    const inBalance = Math.abs(totalDebits - totalCredits) < 0.01;

    return {
      postedToday,
      pendingReconciliations,
      totalDebits,
      totalCredits,
      inBalance
    };
  }, [glAccounts, journalEntries]);

  const addJournalLine = () => {
    setJournalLines([...journalLines, { accountCode: '', debit: '', credit: '', narration: '' }]);
  };

  const updateJournalLine = (index: number, field: string, value: string) => {
    const updated = [...journalLines];
    updated[index] = { ...updated[index], [field]: value };
    setJournalLines(updated);
  };

  const removeJournalLine = (index: number) => {
    if (journalLines.length > 2) {
      setJournalLines(journalLines.filter((_, i) => i !== index));
    }
  };

  const calculateTotals = () => {
    const totalDebit = journalLines.reduce((sum, line) => sum + (parseFloat(line.debit) || 0), 0);
    const totalCredit = journalLines.reduce((sum, line) => sum + (parseFloat(line.credit) || 0), 0);
    const balanced = Math.abs(totalDebit - totalCredit) < 0.01 && totalDebit > 0;
    return { totalDebit, totalCredit, balanced };
  };

  const handlePostJournal = (e: React.FormEvent) => {
    e.preventDefault();
    const { balanced } = calculateTotals();
    
    if (!balanced) {
      alert('Journal entry must be balanced!');
      return;
    }

    if (onPostJournal) {
      onPostJournal({
        date: journalDate,
        reference: journalRef,
        description: journalDesc,
        lines: journalLines.filter(l => l.accountCode && (l.debit || l.credit))
      });
      
      // Reset form
      setJournalRef('');
      setJournalDesc('');
      setJournalLines([
        { accountCode: '', debit: '', credit: '', narration: '' },
        { accountCode: '', debit: '', credit: '', narration: '' }
      ]);
    }
  };

  const { totalDebit, totalCredit, balanced } = calculateTotals();

  return (
    <PermissionGuard permission={['GL_READ', 'GL_POST']} fallback={<div className="p-6 text-red-600">Access denied - Accounting permissions required</div>}>
      <div className="h-full flex flex-col bg-slate-50">
        {/* Header */}
        <div className="bg-white border-b border-slate-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Accountant</h1>
              <p className="text-sm text-slate-600 mt-1">GL Posting, Reconciliation & Financial Control</p>
            </div>
            <div className="flex items-center gap-4">
              <div className="text-right">
                <p className="text-xs text-slate-600">Trial Balance Status</p>
                <div className="flex items-center gap-2 mt-1">
                  {metrics.inBalance ? (
                    <>
                      <CheckCircle className="w-5 h-5 text-green-600" />
                      <span className="font-semibold text-green-600">Balanced</span>
                    </>
                  ) : (
                    <>
                      <AlertTriangle className="w-5 h-5 text-red-600" />
                      <span className="font-semibold text-red-600">Out of Balance</span>
                    </>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Metrics Cards */}
        <div className="px-6 py-4">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Posted Today</p>
                  <p className="text-2xl font-bold text-slate-900 mt-1">{metrics.postedToday}</p>
                </div>
                <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                  <FileText className="w-6 h-6 text-blue-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Pending Reconciliation</p>
                  <p className="text-2xl font-bold text-amber-600 mt-1">{metrics.pendingReconciliations}</p>
                </div>
                <div className="w-12 h-12 bg-amber-100 rounded-lg flex items-center justify-center">
                  <AlertTriangle className="w-6 h-6 text-amber-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Total Debits</p>
                  <p className="text-xl font-bold text-slate-900 mt-1">GHS {metrics.totalDebits.toFixed(2)}</p>
                </div>
                <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                  <TrendingUp className="w-6 h-6 text-green-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Total Credits</p>
                  <p className="text-xl font-bold text-slate-900 mt-1">GHS {metrics.totalCredits.toFixed(2)}</p>
                </div>
                <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
                  <ArrowRightLeft className="w-6 h-6 text-purple-600" />
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="px-6">
          <div className="flex gap-2 border-b border-slate-200">
            {[
              { id: 'journal', label: 'Journal Entry', icon: FileText },
              { id: 'reconcile', label: 'Reconciliation', icon: CheckCircle },
              { id: 'reports', label: 'Financial Reports', icon: BookOpen },
              { id: 'closing', label: 'Period Closing', icon: Lock }
            ].map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as any)}
                className={`flex items-center gap-2 px-4 py-3 font-medium transition-colors ${
                  activeTab === tab.id
                    ? 'text-blue-600 border-b-2 border-blue-600'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                <tab.icon className="w-4 h-4" />
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        {/* Content Area */}
        <div className="flex-1 overflow-auto px-6 py-4">
          {activeTab === 'journal' && (
            <div className="max-w-5xl mx-auto">
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <div className="mb-6">
                  <h3 className="text-lg font-semibold text-slate-900 mb-2">New Journal Entry</h3>
                  <p className="text-sm text-slate-600">Manual GL posting with automatic balancing check</p>
                </div>

                <form onSubmit={handlePostJournal} className="space-y-6">
                  <div className="grid grid-cols-3 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">Date</label>
                      <input
                        type="date"
                        value={journalDate}
                        onChange={(e) => setJournalDate(e.target.value)}
                        className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">Reference</label>
                      <input
                        type="text"
                        value={journalRef}
                        onChange={(e) => setJournalRef(e.target.value)}
                        className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        placeholder="JE-2024-001"
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
                      <input
                        type="text"
                        value={journalDesc}
                        onChange={(e) => setJournalDesc(e.target.value)}
                        className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        placeholder="Accrual entry"
                        required
                      />
                    </div>
                  </div>

                  <div>
                    <div className="flex items-center justify-between mb-3">
                      <h4 className="font-semibold text-slate-900">Journal Lines</h4>
                      <button
                        type="button"
                        onClick={addJournalLine}
                        className="text-sm text-blue-600 hover:text-blue-700 font-medium"
                      >
                        + Add Line
                      </button>
                    </div>

                    <div className="space-y-2">
                      {journalLines.map((line, index) => (
                        <div key={index} className="grid grid-cols-12 gap-2 items-center">
                          <div className="col-span-4">
                            <select
                              value={line.accountCode}
                              onChange={(e) => updateJournalLine(index, 'accountCode', e.target.value)}
                              className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm"
                              required
                            >
                              <option value="">Select GL Account...</option>
                              {glAccounts.map(acc => (
                                <option key={acc.code} value={acc.code}>
                                  {acc.code} - {acc.name}
                                </option>
                              ))}
                            </select>
                          </div>
                          <div className="col-span-3">
                            <input
                              type="number"
                              value={line.debit}
                              onChange={(e) => {
                                updateJournalLine(index, 'debit', e.target.value);
                                updateJournalLine(index, 'credit', '');
                              }}
                              className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm"
                              placeholder="Debit"
                              step="0.01"
                              disabled={!!line.credit}
                            />
                          </div>
                          <div className="col-span-3">
                            <input
                              type="number"
                              value={line.credit}
                              onChange={(e) => {
                                updateJournalLine(index, 'credit', e.target.value);
                                updateJournalLine(index, 'debit', '');
                              }}
                              className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm"
                              placeholder="Credit"
                              step="0.01"
                              disabled={!!line.debit}
                            />
                          </div>
                          <div className="col-span-2 flex justify-end">
                            {journalLines.length > 2 && (
                              <button
                                type="button"
                                onClick={() => removeJournalLine(index)}
                                className="text-red-600 hover:text-red-700"
                              >
                                Remove
                              </button>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>

                    <div className="mt-4 p-4 bg-slate-50 rounded-lg border border-slate-200">
                      <div className="flex items-center justify-between text-sm">
                        <div>
                          <span className="font-medium text-slate-700">Total Debits:</span>
                          <span className="ml-2 font-semibold text-slate-900">GHS {totalDebit.toFixed(2)}</span>
                        </div>
                        <div>
                          <span className="font-medium text-slate-700">Total Credits:</span>
                          <span className="ml-2 font-semibold text-slate-900">GHS {totalCredit.toFixed(2)}</span>
                        </div>
                        <div>
                          {balanced ? (
                            <span className="flex items-center gap-2 text-green-600 font-semibold">
                              <CheckCircle className="w-4 h-4" />
                              Balanced
                            </span>
                          ) : (
                            <span className="flex items-center gap-2 text-red-600 font-semibold">
                              <AlertTriangle className="w-4 h-4" />
                              Out of Balance
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="pt-4 border-t border-slate-200">
                    <button
                      type="submit"
                      disabled={!balanced}
                      className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-slate-300 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-lg transition-colors flex items-center justify-center gap-2"
                    >
                      <FileText className="w-5 h-5" />
                      Post Journal Entry
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {activeTab === 'reconcile' && (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <h3 className="font-semibold text-slate-900 mb-4">Accounts Requiring Reconciliation</h3>
                <div className="space-y-2">
                  {glAccounts.filter(a => a.category === 'ASSET' || a.category === 'LIABILITY').slice(0, 5).map(account => (
                    <div key={account.code} className="p-3 bg-slate-50 rounded-lg border border-slate-200 hover:border-slate-300 transition-colors">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="font-medium text-slate-900">{account.code} - {account.name}</p>
                          <p className="text-sm text-slate-600">Balance: GHS {account.balance.toFixed(2)}</p>
                        </div>
                        <button
                          onClick={() => onReconcile && onReconcile(account.code)}
                          className="px-3 py-1 bg-blue-600 hover:bg-blue-700 text-white text-sm rounded"
                        >
                          Reconcile
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <h3 className="font-semibold text-slate-900 mb-4">Recent Reconciliations</h3>
                <div className="space-y-2">
                  <div className="p-3 bg-green-50 rounded-lg border border-green-200">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="font-medium text-slate-900">1000 - Cash in Vault</p>
                        <p className="text-sm text-slate-600">Reconciled today</p>
                      </div>
                      <CheckCircle className="w-5 h-5 text-green-600" />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'reports' && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <div className="bg-white rounded-lg border border-slate-200 p-6 hover:border-blue-300 transition-colors cursor-pointer">
                <BookOpen className="w-10 h-10 text-blue-600 mb-3" />
                <h3 className="font-semibold text-slate-900 mb-2">Trial Balance</h3>
                <p className="text-sm text-slate-600">Current period trial balance report</p>
              </div>
              <div className="bg-white rounded-lg border border-slate-200 p-6 hover:border-blue-300 transition-colors cursor-pointer">
                <FileText className="w-10 h-10 text-green-600 mb-3" />
                <h3 className="font-semibold text-slate-900 mb-2">Income Statement</h3>
                <p className="text-sm text-slate-600">Profit & loss statement</p>
              </div>
              <div className="bg-white rounded-lg border border-slate-200 p-6 hover:border-blue-300 transition-colors cursor-pointer">
                <Calculator className="w-10 h-10 text-purple-600 mb-3" />
                <h3 className="font-semibold text-slate-900 mb-2">Balance Sheet</h3>
                <p className="text-sm text-slate-600">Statement of financial position</p>
              </div>
            </div>
          )}

          {activeTab === 'closing' && (
            <div className="max-w-2xl mx-auto">
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <div className="flex items-start gap-4 mb-6">
                  <div className="w-12 h-12 bg-red-100 rounded-lg flex items-center justify-center">
                    <Lock className="w-6 h-6 text-red-600" />
                  </div>
                  <div>
                    <h3 className="text-lg font-semibold text-slate-900 mb-2">Period Closing</h3>
                    <p className="text-sm text-slate-600">Close accounting period and lock transactions</p>
                  </div>
                </div>

                <div className="space-y-4">
                  <div className="p-4 bg-amber-50 border border-amber-200 rounded-lg">
                    <p className="text-sm text-amber-800">
                      <AlertTriangle className="w-4 h-4 inline mr-2" />
                      Period closing is irreversible. Ensure all transactions are posted and reconciled.
                    </p>
                  </div>

                  <div className="grid grid-cols-2 gap-4 p-4 bg-slate-50 rounded-lg">
                    <div>
                      <p className="text-xs text-slate-600 mb-1">Unreconciled Accounts</p>
                      <p className="text-2xl font-bold text-slate-900">{metrics.pendingReconciliations}</p>
                    </div>
                    <div>
                      <p className="text-xs text-slate-600 mb-1">Trial Balance</p>
                      <p className={`text-2xl font-bold ${metrics.inBalance ? 'text-green-600' : 'text-red-600'}`}>
                        {metrics.inBalance ? 'Balanced' : 'Out of Balance'}
                      </p>
                    </div>
                  </div>

                  <button
                    disabled={!metrics.inBalance || metrics.pendingReconciliations > 0}
                    className="w-full bg-red-600 hover:bg-red-700 disabled:bg-slate-300 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-lg transition-colors"
                  >
                    Close Current Period
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </PermissionGuard>
  );
};

export default AccountantScreen;

