import React, { useState, useEffect } from 'react';
import { Customer, Account, Transaction } from '../types';
import {
  DollarSign, CreditCard, Banknote, FileText, AlertCircle, CheckCircle,
  ShieldAlert, Lock, Printer, Plus, X, User, Calendar, RefreshCw, Home
} from 'lucide-react';

interface TellerFormsProps {
  customers: Customer[];
  accounts: Account[];
  onPostTransaction: (transactionData: any) => Promise<any>;
}

interface FormData {
  customerId: string;
  customerName: string;
  ghanaCard: string;
  accountId: string;
  accountType: string;
  balance: number;
  transactionType: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER';
  transactionMethod: 'CASH' | 'CHEQUE';
  amount: number;
  narration: string;
  chequeNumber?: string;
  chequeBank?: string;
  toAccountId?: string;
  tellerId: string;
}

interface ValidationError {
  field: string;
  message: string;
}

interface ReceiptData {
  transactionId: string;
  reference: string;
  amount: number;
  fees: number;
  newBalance: number;
  narration: string;
  timestamp: string;
}

/**
 * Comprehensive Teller Forms Component
 * Handles deposits, withdrawals, transfers with BOG compliance
 */
export const TellerForms: React.FC<TellerFormsProps> = ({
  customers,
  accounts,
  onPostTransaction
}) => {
  const [activeForm, setActiveForm] = useState<'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'CHEQUE'>('DEPOSIT');
  const [formData, setFormData] = useState<FormData>({
    customerId: '',
    customerName: '',
    ghanaCard: '',
    accountId: '',
    accountType: '',
    balance: 0,
    transactionType: 'DEPOSIT',
    transactionMethod: 'CASH',
    amount: 0,
    narration: '',
    tellerId: 'TLR001'
  });

  const [validationErrors, setValidationErrors] = useState<ValidationError[]>([]);
  const [loading, setLoading] = useState(false);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [showReceipt, setShowReceipt] = useState(false);
  const [receiptData, setReceiptData] = useState<ReceiptData | null>(null);
  const [availableMargin, setAvailableMargin] = useState<number>(0);
  const [dailyLimit, setDailyLimit] = useState<number>(0);

  // Load daily limit based on customer KYC level
  useEffect(() => {
    if (formData.customerId) {
      const customer = customers.find(c => c.id === formData.customerId);
      if (customer) {
        setFormData(prev => ({ ...prev, customerName: customer.name, ghanaCard: customer.ghanaCard }));
        
        // Set daily limits based on KYC tier
        const limits: Record<string, number> = {
          'Tier 1': 5000,
          'Tier 2': 50000,
          'Tier 3': 500000
        };
        setDailyLimit(limits[customer.kycLevel] || 5000);
        
        // Set available margin based on credit scoring (simulated)
        const margin = customer.riskRating === 'High' ? 10000 : customer.riskRating === 'Medium' ? 50000 : 100000;
        setAvailableMargin(margin);
      }
    }
  }, [formData.customerId, customers]);

  // Load account details when account is selected
  useEffect(() => {
    if (formData.accountId) {
      const account = accounts.find(a => a.id === formData.accountId);
      if (account) {
        setFormData(prev => ({
          ...prev,
          accountType: account.type,
          balance: account.balance,
          customerId: account.cif
        }));
      }
    }
  }, [formData.accountId, accounts]);

  // Validation logic
  const validateForm = (): boolean => {
    const errors: ValidationError[] = [];

    if (!formData.customerId) errors.push({ field: 'customerId', message: 'Customer is required' });
    if (!formData.ghanaCard) errors.push({ field: 'ghanaCard', message: 'Ghana Card is required for BOG compliance' });
    if (!formData.accountId) errors.push({ field: 'accountId', message: 'Account is required' });
    if (formData.amount <= 0) errors.push({ field: 'amount', message: 'Amount must be greater than 0' });

    // BOG Compliance: KYC limit validation
    if (formData.amount > dailyLimit) {
      errors.push({
        field: 'amount',
        message: `Amount exceeds KYC daily limit of GHS ${dailyLimit.toLocaleString()}`
      });
    }

    // Check available balance for withdrawals
    if ((formData.transactionType === 'WITHDRAWAL' || formData.transactionType === 'TRANSFER') && 
        formData.amount > formData.balance) {
      errors.push({
        field: 'amount',
        message: `Insufficient funds. Available: GHS ${formData.balance.toLocaleString()}`
      });
    }

    // Cheque-specific validation
    if (formData.transactionMethod === 'CHEQUE') {
      if (!formData.chequeNumber) errors.push({ field: 'chequeNumber', message: 'Cheque number is required' });
      if (!formData.chequeBank) errors.push({ field: 'chequeBank', message: 'Cheque bank is required' });
    }

    // Transfer-specific validation
    if (formData.transactionType === 'TRANSFER') {
      if (!formData.toAccountId) errors.push({ field: 'toAccountId', message: 'Destination account is required' });
    }

    setValidationErrors(errors);
    return errors.length === 0;
  };

  const handleInputChange = (field: string, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    setValidationErrors(validationErrors.filter(e => e.field !== field));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    setLoading(true);
    setSuccessMsg(null);
    setErrorMsg(null);

    try {
      const transactionData = {
        accountId: formData.accountId,
        customerId: formData.customerId,
        amount: formData.amount,
        type: formData.transactionType,
        depositMethod: formData.transactionMethod,
        withdrawalMethod: formData.transactionMethod,
        narration: formData.narration,
        tellerId: formData.tellerId,
        customerGhanaCard: formData.ghanaCard,
        chequeNumber: formData.chequeNumber,
        chequeBank: formData.chequeBank,
        toAccountId: formData.toAccountId
      };

      const response = await onPostTransaction(transactionData);

      if (response.success) {
        setReceiptData({
          transactionId: response.transactionId,
          reference: response.reference,
          amount: response.amount,
          fees: response.appliedFees || 0,
          newBalance: response.newBalance,
          narration: response.narration,
          timestamp: new Date().toLocaleString()
        });
        setShowReceipt(true);
        setSuccessMsg(`Transaction posted successfully (Ref: ${response.reference})`);
        
        // Reset form
        setTimeout(() => {
          setFormData({
            customerId: '',
            customerName: '',
            ghanaCard: '',
            accountId: '',
            accountType: '',
            balance: 0,
            transactionType: 'DEPOSIT',
            transactionMethod: 'CASH',
            amount: 0,
            narration: '',
            tellerId: 'TLR001'
          });
        }, 1500);
      } else {
        setErrorMsg(response.message || 'Transaction failed');
      }
    } catch (error: any) {
      setErrorMsg(error.message || 'An error occurred while processing the transaction');
    } finally {
      setLoading(false);
    }
  };

  const handlePrintReceipt = () => {
    window.print();
  };

  // Form Selection Tab
  const FormTab = ({ label, icon, type }: { label: string; icon: React.ReactNode; type: any }) => (
    <button
      onClick={() => {
        setActiveForm(type);
        setFormData(prev => ({ ...prev, transactionType: type }));
      }}
      className={`flex items-center gap-2 px-4 py-2 rounded transition ${
        activeForm === type
          ? 'bg-blue-600 text-white shadow-md'
          : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
      }`}
    >
      {icon}
      <span className="font-medium">{label}</span>
    </button>
  );

  return (
    <div className="bg-white rounded-lg shadow-lg p-6 max-h-[90vh] overflow-y-auto">
      {/* Header */}
      <div className="mb-6 border-b pb-4">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <Banknote size={32} className="text-blue-600" />
            <div>
              <h2 className="text-2xl font-bold text-gray-800">Teller Transaction Forms</h2>
              <p className="text-sm text-gray-500">BOG-Compliant Banking Operations</p>
            </div>
          </div>
          <div className="text-right text-sm text-gray-600">
            <p>Teller ID: <span className="font-mono font-bold">{formData.tellerId}</span></p>
            <p>{new Date().toLocaleDateString()}</p>
          </div>
        </div>

        {/* Form Selection Tabs */}
        <div className="flex gap-3 flex-wrap">
          <FormTab label="Cash Deposit" icon={<Plus size={18} />} type="DEPOSIT" />
          <FormTab label="Withdrawal" icon={<X size={18} />} type="WITHDRAWAL" />
          <FormTab label="Transfer" icon={<RefreshCw size={18} />} type="TRANSFER" />
          <FormTab label="Cheque" icon={<FileText size={18} />} type="CHEQUE" />
        </div>
      </div>

      {/* Alert Messages */}
      {validationErrors.length > 0 && (
        <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg">
          <div className="flex items-start gap-3">
            <AlertCircle size={20} className="text-red-600 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <p className="font-semibold text-red-900 mb-2">Validation Errors:</p>
              {validationErrors.map((error, idx) => (
                <p key={idx} className="text-sm text-red-700">• {error.message}</p>
              ))}
            </div>
          </div>
        </div>
      )}

      {errorMsg && (
        <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg flex items-start gap-3">
          <AlertCircle size={20} className="text-red-600 flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-semibold text-red-900">Error</p>
            <p className="text-sm text-red-700">{errorMsg}</p>
          </div>
        </div>
      )}

      {successMsg && (
        <div className="mb-4 p-4 bg-green-50 border border-green-200 rounded-lg flex items-start gap-3">
          <CheckCircle size={20} className="text-green-600 flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-semibold text-green-900">Success</p>
            <p className="text-sm text-green-700">{successMsg}</p>
          </div>
        </div>
      )}

      {/* Main Form */}
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Customer ID Verification (BOG Compliance) */}
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-start gap-3 mb-4">
            <ShieldAlert size={20} className="text-yellow-700 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <h3 className="font-semibold text-yellow-900">BOG Customer ID Verification Required</h3>
              <p className="text-sm text-yellow-700">All transactions must include customer Ghana Card verification</p>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Customer Selection */}
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                <User size={16} className="inline mr-2" />
                Customer (CIF)
              </label>
              <select
                value={formData.customerId}
                onChange={(e) => handleInputChange('customerId', e.target.value)}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              >
                <option value="">-- Select Customer --</option>
                {customers.map(c => (
                  <option key={c.id} value={c.id}>
                    {c.name} ({c.id}) - {c.kycLevel}
                  </option>
                ))}
              </select>
            </div>

            {/* Ghana Card Verification */}
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                <Lock size={16} className="inline mr-2" />
                Ghana Card (Verification)
              </label>
              <input
                type="text"
                value={formData.ghanaCard}
                onChange={(e) => handleInputChange('ghanaCard', e.target.value)}
                placeholder="GHA-XXXXXXXX-X"
                className={`w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent ${
                  validationErrors.find(e => e.field === 'ghanaCard') ? 'border-red-500' : 'border-gray-300'
                }`}
              />
              {formData.ghanaCard && formData.ghanaCard === customers.find(c => c.id === formData.customerId)?.ghanaCard && (
                <p className="text-xs text-green-600 mt-1">✓ Ghana Card verified</p>
              )}
            </div>
          </div>
        </div>

        {/* Account & Balance Information */}
        {formData.customerId && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <h3 className="font-semibold text-blue-900 mb-4">Account Information</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {/* Account Selection */}
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-2">Account</label>
                <select
                  value={formData.accountId}
                  onChange={(e) => handleInputChange('accountId', e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                >
                  <option value="">-- Select Account --</option>
                  {accounts
                    .filter(a => a.cif === formData.customerId)
                    .map(a => (
                      <option key={a.id} value={a.id}>
                        {a.id} ({a.type}) - {a.currency}
                      </option>
                    ))}
                </select>
              </div>

              {/* Current Balance */}
              {formData.accountId && (
                <>
                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-2">
                      <Home size={16} className="inline mr-2" />
                      Current Balance
                    </label>
                    <div className="px-4 py-2 bg-white border border-gray-300 rounded-lg font-bold text-green-600">
                      GHS {formData.balance.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                    </div>
                  </div>

                  {/* Daily Limit */}
                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-2">
                      <Calendar size={16} className="inline mr-2" />
                      Daily Limit ({formData.customerId ? customers.find(c => c.id === formData.customerId)?.kycLevel : 'N/A'})
                    </label>
                    <div className="px-4 py-2 bg-white border border-gray-300 rounded-lg font-bold text-blue-600">
                      GHS {dailyLimit.toLocaleString()}
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>
        )}

        {/* Transaction Details */}
        <div className="border rounded-lg p-4">
          <h3 className="font-semibold text-gray-800 mb-4">
            {activeForm === 'DEPOSIT' && '💰 Deposit Details'}
            {activeForm === 'WITHDRAWAL' && '🏧 Withdrawal Details'}
            {activeForm === 'TRANSFER' && '↔️ Transfer Details'}
            {activeForm === 'CHEQUE' && '📋 Cheque Details'}
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            {/* Transaction Method */}
            {activeForm !== 'TRANSFER' && (
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-2">Payment Method</label>
                <select
                  value={formData.transactionMethod}
                  onChange={(e) => handleInputChange('transactionMethod', e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                >
                  <option value="CASH">Cash</option>
                  <option value="CHEQUE">Cheque</option>
                </select>
              </div>
            )}

            {/* Amount */}
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                <DollarSign size={16} className="inline mr-2" />
                Amount (GHS)
              </label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={formData.amount}
                onChange={(e) => handleInputChange('amount', parseFloat(e.target.value))}
                placeholder="0.00"
                className={`w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent font-bold text-lg ${
                  validationErrors.find(e => e.field === 'amount') ? 'border-red-500' : 'border-gray-300'
                }`}
              />
            </div>
          </div>

          {/* Cheque Details */}
          {formData.transactionMethod === 'CHEQUE' && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-2">Cheque Number</label>
                <input
                  type="text"
                  value={formData.chequeNumber || ''}
                  onChange={(e) => handleInputChange('chequeNumber', e.target.value)}
                  placeholder="e.g., 123456"
                  className={`w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent ${
                    validationErrors.find(e => e.field === 'chequeNumber') ? 'border-red-500' : 'border-gray-300'
                  }`}
                />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-2">Bank Name</label>
                <input
                  type="text"
                  value={formData.chequeBank || ''}
                  onChange={(e) => handleInputChange('chequeBank', e.target.value)}
                  placeholder="e.g., GCB Bank"
                  className={`w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent ${
                    validationErrors.find(e => e.field === 'chequeBank') ? 'border-red-500' : 'border-gray-300'
                  }`}
                />
              </div>
            </div>
          )}

          {/* Transfer Details */}
          {activeForm === 'TRANSFER' && (
            <div className="mb-4">
              <label className="block text-sm font-semibold text-gray-700 mb-2">Destination Account</label>
              <select
                value={formData.toAccountId || ''}
                onChange={(e) => handleInputChange('toAccountId', e.target.value)}
                className={`w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent ${
                  validationErrors.find(e => e.field === 'toAccountId') ? 'border-red-500' : 'border-gray-300'
                }`}
              >
                <option value="">-- Select Destination Account --</option>
                {accounts
                  .filter(a => a.id !== formData.accountId)
                  .map(a => (
                    <option key={a.id} value={a.id}>
                      {a.id} ({a.cif}) - {a.type}
                    </option>
                  ))}
              </select>
            </div>
          )}

          {/* Narration/Reference */}
          <div>
            <label className="block text-sm font-semibold text-gray-700 mb-2">
              <FileText size={16} className="inline mr-2" />
              Narration/Reference
            </label>
            <textarea
              value={formData.narration}
              onChange={(e) => handleInputChange('narration', e.target.value)}
              placeholder="Enter transaction details/memo..."
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              rows={2}
            />
          </div>
        </div>

        {/* Submit Button */}
        <div className="flex gap-4">
          <button
            type="submit"
            disabled={loading}
            className="flex-1 px-6 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-bold rounded-lg transition flex items-center justify-center gap-2 shadow-md"
          >
            {loading ? (
              <>
                <RefreshCw size={20} className="animate-spin" />
                Processing...
              </>
            ) : (
              <>
                <CheckCircle size={20} />
                Submit Transaction
              </>
            )}
          </button>
          {showReceipt && receiptData && (
            <button
              type="button"
              onClick={handlePrintReceipt}
              className="px-6 py-3 bg-green-600 hover:bg-green-700 text-white font-bold rounded-lg transition flex items-center gap-2 shadow-md"
            >
              <Printer size={20} />
              Print Receipt
            </button>
          )}
        </div>
      </form>

      {/* Receipt Modal */}
      {showReceipt && receiptData && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg shadow-2xl max-w-md w-full p-6">
            <div className="text-center mb-6">
              <CheckCircle size={48} className="text-green-600 mx-auto mb-4" />
              <h3 className="text-2xl font-bold text-green-600">Transaction Successful</h3>
              <p className="text-gray-600 text-sm mt-2">Receipt Details</p>
            </div>

            <div className="space-y-3 bg-gray-50 p-4 rounded-lg mb-6 font-mono text-sm">
              <div className="flex justify-between">
                <span>Transaction ID:</span>
                <span className="font-bold">{receiptData.transactionId}</span>
              </div>
              <div className="flex justify-between">
                <span>Reference:</span>
                <span className="font-bold">{receiptData.reference}</span>
              </div>
              <div className="border-t pt-2 flex justify-between">
                <span>Amount:</span>
                <span className="font-bold">GHS {receiptData.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}</span>
              </div>
              <div className="flex justify-between">
                <span>Fees:</span>
                <span>GHS {receiptData.fees.toLocaleString(undefined, { minimumFractionDigits: 2 })}</span>
              </div>
              <div className="border-t pt-2 flex justify-between text-green-600 font-bold text-lg">
                <span>New Balance:</span>
                <span>GHS {receiptData.newBalance.toLocaleString(undefined, { minimumFractionDigits: 2 })}</span>
              </div>
              <div className="flex justify-between text-xs">
                <span>Time:</span>
                <span>{receiptData.timestamp}</span>
              </div>
            </div>

            <button
              onClick={() => setShowReceipt(false)}
              className="w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-semibold transition"
            >
              Close Receipt
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default TellerForms;
