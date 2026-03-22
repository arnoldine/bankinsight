import React, { useState, useMemo } from 'react';
import { FileText, Users, TrendingUp, AlertCircle, CheckCircle, Clock, DollarSign, Calculator } from 'lucide-react';
import { Loan, Customer, Product } from '../types';
import PermissionGuard from './PermissionGuard';
import { useBankingSystem } from '../hooks/useBankingSystem';

interface LoanOfficerScreenProps {
  loans: Loan[];
  customers: Customer[];
  products: Product[];
  onDisburseLoan?: (loanId: string) => void;
  onAppraiseLoan?: (customerId: string, amount: number) => void;
}

const LoanOfficerScreen: React.FC<LoanOfficerScreenProps> = ({
  loans,
  customers,
  products,
  onDisburseLoan,
  onAppraiseLoan
}) => {
  const [activeTab, setActiveTab] = useState<'pipeline' | 'portfolio' | 'appraisal' | 'followup'>('pipeline');
  const [selectedCustomer, setSelectedCustomer] = useState('');
  const [loanAmount, setLoanAmount] = useState('');
  const [loanPurpose, setLoanPurpose] = useState('');
  const [selectedProduct, setSelectedProduct] = useState('');
  
  const { hasPermission } = useBankingSystem();

  // Calculate metrics
  const metrics = useMemo(() => {
    const activeLoans = loans.filter(l => l.status === 'ACTIVE');
    const pendingLoans = loans.filter(l => l.status === 'PENDING');
    const totalPortfolio = activeLoans.reduce((sum, l) => sum + l.outstandingBalance, 0);
    const avgLoanSize = activeLoans.length> 0 ? totalPortfolio / activeLoans.length : 0;

    return {
      activeLoans: activeLoans.length,
      pendingLoans: pendingLoans.length,
      totalPortfolio,
      avgLoanSize,
      loansDisbursedThisMonth: 12, // Mock data
      collectionRate: 94.5
    };
  }, [loans]);

  const handleSubmitAppraisal = (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedCustomer && loanAmount && onAppraiseLoan) {
      onAppraiseLoan(selectedCustomer, parseFloat(loanAmount));
      // Reset form
      setSelectedCustomer('');
      setLoanAmount('');
      setLoanPurpose('');
      setSelectedProduct('');
    }
  };

  return (
    <PermissionGuard permission={['LOAN_READ', 'LOAN_WRITE']} fallback={<div className="p-6 text-red-600">Access denied - Loan Officer permissions required</div>}>
      <div className="h-full flex flex-col bg-slate-50">
        {/* Header */}
        <div className="bg-white border-b border-slate-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Loan Officer</h1>
              <p className="text-sm text-slate-600 mt-1">Manage loan applications, portfolio, and customer relationships</p>
            </div>
            <div className="flex items-center gap-2">
              <div className="text-right">
                <p className="text-xs text-slate-600">Portfolio Outstanding</p>
                <p className="text-lg font-bold text-teal-600">GHS {metrics.totalPortfolio.toLocaleString()}</p>
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
                  <p className="text-xs text-slate-600 uppercase font-semibold">Active Loans</p>
                  <p className="text-2xl font-bold text-slate-900 mt-1">{metrics.activeLoans}</p>
                </div>
                <div className="w-12 h-12 bg-teal-100 rounded-lg flex items-center justify-center">
                  <FileText className="w-6 h-6 text-teal-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Pending Approvals</p>
                  <p className="text-2xl font-bold text-amber-600 mt-1">{metrics.pendingLoans}</p>
                </div>
                <div className="w-12 h-12 bg-amber-100 rounded-lg flex items-center justify-center">
                  <Clock className="w-6 h-6 text-amber-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Avg Loan Size</p>
                  <p className="text-2xl font-bold text-slate-900 mt-1">GHS {metrics.avgLoanSize.toFixed(0)}</p>
                </div>
                <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                  <Calculator className="w-6 h-6 text-blue-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-600 uppercase font-semibold">Collection Rate</p>
                  <p className="text-2xl font-bold text-green-600 mt-1">{metrics.collectionRate}%</p>
                </div>
                <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                  <TrendingUp className="w-6 h-6 text-green-600" />
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="px-6">
          <div className="flex gap-2 border-b border-slate-200">
            {[
              { id: 'pipeline', label: 'Loan Pipeline', icon: FileText },
              { id: 'portfolio', label: 'My Portfolio', icon: DollarSign },
              { id: 'appraisal', label: 'New Appraisal', icon: Calculator },
              { id: 'followup', label: 'Follow-ups', icon: AlertCircle }
            ].map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as any)}
                className={`flex items-center gap-2 px-4 py-3 font-medium transition-colors ${
                  activeTab === tab.id
                    ? 'text-teal-600 border-b-2 border-teal-600'
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
          {activeTab === 'pipeline' && (
            <div className="bg-white rounded-lg border border-slate-200">
              <div className="p-4 border-b border-slate-200">
                <h3 className="font-semibold text-slate-900">Loan Application Pipeline</h3>
              </div>
              <div className="divide-y divide-slate-200">
                {loans.filter(l => l.status === 'PENDING').map(loan => (
                  <div key={loan.id} className="p-4 hover:bg-slate-50 transition-colors">
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 bg-amber-100 rounded-full flex items-center justify-center">
                            <FileText className="w-5 h-5 text-amber-600" />
                          </div>
                          <div>
                            <p className="font-semibold text-slate-900">{loan.id}</p>
                            <p className="text-sm text-slate-600">Customer: {loan.cif}</p>
                          </div>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className="font-semibold text-slate-900">GHS {loan.principal.toLocaleString()}</p>
                        <p className="text-xs text-slate-600">{loan.termMonths} months @ {loan.rate}%</p>
                      </div>
                      <div className="ml-4">
                        <span className="px-3 py-1 bg-amber-100 text-amber-700 rounded-full text-xs font-medium">
                          Pending
                        </span>
                      </div>
                    </div>
                  </div>
                ))}
                {loans.filter(l => l.status === 'PENDING').length === 0 && (
                  <div className="p-8 text-center text-slate-500">
                    <Clock className="w-12 h-12 mx-auto mb-3 text-slate-300" />
                    <p>No pending applications</p>
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === 'portfolio' && (
            <div className="bg-white rounded-lg border border-slate-200">
              <div className="p-4 border-b border-slate-200">
                <h3 className="font-semibold text-slate-900">Active Loan Portfolio</h3>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      <th className="text-left p-3 text-xs font-semibold text-slate-600 uppercase">Loan ID</th>
                      <th className="text-left p-3 text-xs font-semibold text-slate-600 uppercase">Customer</th>
                      <th className="text-right p-3 text-xs font-semibold text-slate-600 uppercase">Principal</th>
                      <th className="text-right p-3 text-xs font-semibold text-slate-600 uppercase">Outstanding</th>
                      <th className="text-left p-3 text-xs font-semibold text-slate-600 uppercase">PAR</th>
                      <th className="text-center p-3 text-xs font-semibold text-slate-600 uppercase">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-200">
                    {loans.filter(l => l.status === 'ACTIVE').map(loan => (
                      <tr key={loan.id} className="hover:bg-slate-50">
                        <td className="p-3 text-sm font-medium text-slate-900">{loan.id}</td>
                        <td className="p-3 text-sm text-slate-600">{loan.cif}</td>
                        <td className="p-3 text-sm text-right text-slate-900">GHS {loan.principal.toLocaleString()}</td>
                        <td className="p-3 text-sm text-right font-semibold text-slate-900">GHS {loan.outstandingBalance.toLocaleString()}</td>
                        <td className="p-3 text-sm">
                          <span className={`px-2 py-1 rounded text-xs font-medium ${
                            loan.parBucket === '0' ? 'bg-green-100 text-green-700' :
                            loan.parBucket === '1-30' ? 'bg-yellow-100 text-yellow-700' :
                            'bg-red-100 text-red-700'
                          }`}>
                            {loan.parBucket}
                          </span>
                        </td>
                        <td className="p-3 text-center">
                          <span className="px-3 py-1 bg-green-100 text-green-700 rounded-full text-xs font-medium">
                            Active
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {activeTab === 'appraisal' && (
            <div className="max-w-3xl mx-auto">
              <div className="bg-white rounded-lg border border-slate-200 p-6">
                <div className="mb-6">
                  <h3 className="text-lg font-semibold text-slate-900 mb-2">New Loan Appraisal</h3>
                  <p className="text-sm text-slate-600">Conduct initial assessment for new loan application</p>
                </div>

                <form onSubmit={handleSubmitAppraisal} className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Customer</label>
                    <select
                      value={selectedCustomer}
                      onChange={(e) => setSelectedCustomer(e.target.value)}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-teal-500"
                      required
                    >
                      <option value="">Select customer...</option>
                      {customers.map(c => (
                        <option key={c.id} value={c.id}>{c.name} ({c.id})</option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Loan Product</label>
                    <select
                      value={selectedProduct}
                      onChange={(e) => setSelectedProduct(e.target.value)}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-teal-500"
                      required
                    >
                      <option value="">Select product...</option>
                      {products.filter(p => p.type === 'LOAN').map(p => (
                        <option key={p.id} value={p.id}>{p.name}</option>
                      ))}
                    </select>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">Requested Amount (GHS)</label>
                      <input
                        type="number"
                        value={loanAmount}
                        onChange={(e) => setLoanAmount(e.target.value)}
                        className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-teal-500"
                        placeholder="0.00"
                        step="0.01"
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">Purpose of Loan</label>
                      <select
                        value={loanPurpose}
                        onChange={(e) => setLoanPurpose(e.target.value)}
                        className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-teal-500"
                        required
                      >
                        <option value="">Select...</option>
                        <option value="BUSINESS">Business Expansion</option>
                        <option value="WORKING_CAPITAL">Working Capital</option>
                        <option value="EQUIPMENT">Equipment Purchase</option>
                        <option value="PERSONAL">Personal</option>
                        <option value="EDUCATION">Education</option>
                        <option value="AGRICULTURE">Agriculture</option>
                      </select>
                    </div>
                  </div>

                  <div className="pt-4 border-t border-slate-200">
                    {hasPermission('LOAN_APPROVE') ? (
                      <button
                        type="submit"
                        className="w-full bg-teal-600 hover:bg-teal-700 text-white font-semibold py-3 rounded-lg transition-colors flex items-center justify-center gap-2"
                      >
                        <CheckCircle className="w-5 h-5" />
                        Submit for Appraisal
                      </button>
                    ) : (
                      <div className="w-full bg-slate-100 text-slate-400 font-semibold py-3 rounded-lg flex items-center justify-center gap-2 cursor-not-allowed border border-slate-200" title="Missing LOAN_APPROVE permission">
                        <CheckCircle className="w-5 h-5" />
                        Submit for Appraisal
                      </div>
                    )}
                  </div>
                </form>
              </div>
            </div>
          )}

          {activeTab === 'followup' && (
            <div className="bg-white rounded-lg border border-slate-200 p-6">
              <div className="flex items-center gap-3 mb-4">
                <AlertCircle className="w-6 h-6 text-amber-600" />
                <div>
                  <h3 className="font-semibold text-slate-900">Required Follow-ups</h3>
                  <p className="text-sm text-slate-600">Customers requiring attention</p>
                </div>
              </div>
              <div className="space-y-3">
                <div className="p-4 bg-amber-50 border border-amber-200 rounded-lg">
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="font-medium text-slate-900">Late Payment - CUST0001</p>
                      <p className="text-sm text-slate-600 mt-1">15 days past due on loan LN0001</p>
                    </div>
                    <span className="px-2 py-1 bg-amber-200 text-amber-800 rounded text-xs font-medium">
                      High Priority
                    </span>
                  </div>
                </div>
                <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg">
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="font-medium text-slate-900">Document Collection - CUST0002</p>
                      <p className="text-sm text-slate-600 mt-1">Pending collateral verification documents</p>
                    </div>
                    <span className="px-2 py-1 bg-blue-200 text-blue-800 rounded text-xs font-medium">
                      Medium
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </PermissionGuard>
  );
};

export default LoanOfficerScreen;

