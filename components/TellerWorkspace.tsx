/**
 * Enhanced Teller Workspace
 * Integrates TellerForms with BOG-compliant transaction posting, ledger engine integration
 * and comprehensive customer service operations
 */

import React, { useState, useCallback } from 'react';
import { Customer, Account, Transaction } from '../types';
import TellerForms from './TellerForms';
import PermissionGuard from './PermissionGuard';
import useLedger from '../src/hooks/useLedger';
import { Banknote, FileText, Users, TrendingUp, AlertCircle, CheckCircle } from 'lucide-react';

interface TellerWorkspaceProps {
  customers: Customer[];
  accounts: Account[];
  transactions: Transaction[];
}

/**
 * TellerWorkspace - Complete teller transaction processing with BOG compliance
 */
export const TellerWorkspace: React.FC<TellerWorkspaceProps> = ({
  customers,
  accounts,
  transactions
}) => {
  const [activeTab, setActiveTab] = useState<'forms' | 'ledger' | 'accounts' | 'reports'>('forms');
  const [selectedAccountId, setSelectedAccountId] = useState<string | null>(null);
  
  const {
    postDeposit,
    postWithdrawal,
    postTransfer,
    postCheque,
    getAccountBalance,
    validateBogCompliance,
    checkSuspiciousActivity,
    loading,
    error,
    success,
    result
  } = useLedger();

  // Integrated transaction posting handler
  const handlePostTransaction = useCallback(async (transactionData: any) => {
    try {
      // Validate BOG compliance first
      const compliance = await validateBogCompliance(
        transactionData.customerId,
        transactionData.amount
      );

      if (!compliance.compliant) {
        return {
          success: false,
          message: compliance.message
        };
      }

      // Check for suspicious activity
      const suspiciousCheck = await checkSuspiciousActivity(
        transactionData.accountId,
        transactionData.amount
      );

      if (suspiciousCheck.isSuspicious && suspiciousCheck.riskScore > 80) {
        return {
          success: false,
          message: `Transaction flagged as suspicious (Risk Score: ${suspiciousCheck.riskScore}). Requires manager approval.`
        };
      }

      // Post transaction based on type
      let result;
      const baseRequest = {
        accountId: transactionData.accountId,
        customerId: transactionData.customerId,
        amount: transactionData.amount,
        narration: transactionData.narration,
        tellerId: transactionData.tellerId,
        customerGhanaCard: transactionData.customerGhanaCard,
        branchId: transactionData.branchId
      };

      if (transactionData.type === 'DEPOSIT') {
        result = await postDeposit({
          ...baseRequest,
          depositMethod: transactionData.transactionMethod || 'CASH',
          chequeNumber: transactionData.chequeNumber,
          chequeBank: transactionData.chequeBank
        });
      } else if (transactionData.type === 'WITHDRAWAL') {
        result = await postWithdrawal({
          ...baseRequest,
          withdrawalMethod: transactionData.transactionMethod || 'CASH',
          chequeNumber: transactionData.chequeNumber
        });
      } else if (transactionData.type === 'TRANSFER') {
        result = await postTransfer({
          fromAccountId: transactionData.accountId,
          toAccountId: transactionData.toAccountId,
          customerId: transactionData.customerId,
          amount: transactionData.amount,
          narration: transactionData.narration,
          tellerId: transactionData.tellerId,
          customerGhanaCard: transactionData.customerGhanaCard,
          branchId: transactionData.branchId
        });
      } else if (transactionData.type === 'CHEQUE') {
        result = await postCheque({
          ...baseRequest,
          chequeNumber: transactionData.chequeNumber || '',
          chequeBank: transactionData.chequeBank || '',
          transactionType: transactionData.transactionMethod === 'DEPOSIT' ? 'DEPOSIT' : 'WITHDRAWAL'
        });
      }

      return result || { success: false, message: 'Transaction type not recognized' };
    } catch (error: any) {
      return {
        success: false,
        message: error.message || 'An error occurred while processing the transaction'
      };
    }
  }, [postDeposit, postWithdrawal, postTransfer, postCheque, validateBogCompliance, checkSuspiciousActivity]);

  // Daily metrics
  const dailyMetrics = {
    transactionsProcessed: transactions.filter(
      t => new Date(t.date).toDateString() === new Date().toDateString()
    ).length,
    totalAmount: transactions
      .filter(t => new Date(t.date).toDateString() === new Date().toDateString())
      .reduce((sum, t) => sum + t.amount, 0),
    depositsCount: transactions.filter(
      t => t.type === 'DEPOSIT' && new Date(t.date).toDateString() === new Date().toDateString()
    ).length,
    withdrawalsCount: transactions.filter(
      t => t.type === 'WITHDRAWAL' && new Date(t.date).toDateString() === new Date().toDateString()
    ).length
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-800 rounded-lg shadow-lg p-6 text-white">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Banknote size={40} />
            <div>
              <h1 className="text-3xl font-bold">Teller Operations</h1>
              <p className="text-blue-100">BOG-Compliant Transaction Processing</p>
            </div>
          </div>
          <div className="text-right text-sm">
            <p className="text-blue-100">Today's Volume</p>
            <p className="text-3xl font-bold">GHS {dailyMetrics.totalAmount.toLocaleString()}</p>
          </div>
        </div>
      </div>

      {/* Daily Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4 border-l-4 border-blue-600">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-600 text-sm">Transactions Today</p>
              <p className="text-3xl font-bold text-blue-600">{dailyMetrics.transactionsProcessed}</p>
            </div>
            <FileText size={28} className="text-blue-600 opacity-20" />
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-4 border-l-4 border-green-600">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-600 text-sm">Deposits</p>
              <p className="text-3xl font-bold text-green-600">{dailyMetrics.depositsCount}</p>
            </div>
            <CheckCircle size={28} className="text-green-600 opacity-20" />
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-4 border-l-4 border-red-600">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-600 text-sm">Withdrawals</p>
              <p className="text-3xl font-bold text-red-600">{dailyMetrics.withdrawalsCount}</p>
            </div>
            <AlertCircle size={28} className="text-red-600 opacity-20" />
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-4 border-l-4 border-purple-600">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-600 text-sm">Total Volume</p>
              <p className="text-2xl font-bold text-purple-600">
                {(dailyMetrics.totalAmount / 1000).toFixed(1)}K
              </p>
            </div>
            <TrendingUp size={28} className="text-purple-600 opacity-20" />
          </div>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex gap-2 bg-white rounded-lg shadow p-2">
        <button
          onClick={() => setActiveTab('forms')}
          className={`flex-1 px-4 py-2 rounded font-semibold transition ${
            activeTab === 'forms'
              ? 'bg-blue-600 text-white shadow'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          Transaction Forms
        </button>
        <button
          onClick={() => setActiveTab('ledger')}
          className={`flex-1 px-4 py-2 rounded font-semibold transition ${
            activeTab === 'ledger'
              ? 'bg-blue-600 text-white shadow'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          Ledger Entries
        </button>
        <button
          onClick={() => setActiveTab('accounts')}
          className={`flex-1 px-4 py-2 rounded font-semibold transition ${
            activeTab === 'accounts'
              ? 'bg-blue-600 text-white shadow'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          Accounts
        </button>
        <button
          onClick={() => setActiveTab('reports')}
          className={`flex-1 px-4 py-2 rounded font-semibold transition ${
            activeTab === 'reports'
              ? 'bg-blue-600 text-white shadow'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          Reports
        </button>
      </div>

      {/* Tab Content */}
      <PermissionGuard permission="TELLER_POST">
        {activeTab === 'forms' && (
          <TellerForms
            customers={customers}
            accounts={accounts}
            onPostTransaction={handlePostTransaction}
          />
        )}

        {activeTab === 'ledger' && (
          <div className="bg-white rounded-lg shadow-lg p-6">
            <h3 className="text-xl font-bold mb-4">Ledger Entries</h3>
            <div className="text-gray-600 text-center py-8">
              <FileText size={48} className="mx-auto mb-4 opacity-20" />
              <p>Select an account from the Accounts tab to view ledger entries</p>
            </div>
          </div>
        )}

        {activeTab === 'accounts' && (
          <div className="bg-white rounded-lg shadow-lg p-6">
            <h3 className="text-xl font-bold mb-4">Accounts</h3>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2 px-4 font-semibold">Account ID</th>
                    <th className="text-left py-2 px-4 font-semibold">Customer</th>
                    <th className="text-left py-2 px-4 font-semibold">Type</th>
                    <th className="text-right py-2 px-4 font-semibold">Balance</th>
                    <th className="text-center py-2 px-4 font-semibold">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {accounts.slice(0, 10).map(account => (
                    <tr key={account.id} className="border-b hover:bg-gray-50">
                      <td className="py-2 px-4 font-mono text-sm">{account.id}</td>
                      <td className="py-2 px-4">
                        {customers.find(c => c.id === account.cif)?.name || account.cif}
                      </td>
                      <td className="py-2 px-4">{account.type}</td>
                      <td className="py-2 px-4 text-right font-bold text-green-600">
                        GHS {account.balance.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </td>
                      <td className="py-2 px-4 text-center">
                        <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
                          account.status === 'ACTIVE'
                            ? 'bg-green-100 text-green-800'
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {account.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === 'reports' && (
          <div className="bg-white rounded-lg shadow-lg p-6">
            <h3 className="text-xl font-bold mb-4">Daily Reports</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="bg-blue-50 p-4 rounded-lg">
                <h4 className="font-semibold text-blue-900 mb-2">Transaction Summary</h4>
                <ul className="text-sm space-y-2 text-blue-800">
                  <li>Total Transactions: {dailyMetrics.transactionsProcessed}</li>
                  <li>Deposits: {dailyMetrics.depositsCount}</li>
                  <li>Withdrawals: {dailyMetrics.withdrawalsCount}</li>
                  <li>Total Volume: GHS {dailyMetrics.totalAmount.toLocaleString()}</li>
                </ul>
              </div>
              <div className="bg-yellow-50 p-4 rounded-lg">
                <h4 className="font-semibold text-yellow-900 mb-2">Compliance Status</h4>
                <ul className="text-sm space-y-2 text-yellow-800">
                  <li>✓ BOG Customer ID Verification: Active</li>
                  <li>✓ KYC Limit Validation: Enabled</li>
                  <li>✓ Suspicious Activity Monitoring: Active</li>
                  <li>✓ Audit Trail: Recording</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </PermissionGuard>
    </div>
  );
};

export default TellerWorkspace;
