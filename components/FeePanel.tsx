import React, { useState } from 'react';
import { DollarSign, AlertCircle, CheckCircle, Loader2 } from 'lucide-react';
import { assessAccountFee } from '../services/api';

interface FeePanelProps {
  accountId: string;
  accountBalance: number;
  onFeeAssessed?: () => void;
}

interface FeeAssessmentResult {
  transactionId: string;
  accountId: string;
  feeCode: string;
  amount: number;
  narration: string;
  postedAt: string;
}

const FEE_CODES = [
  { code: 'MONTHLY_MAINT', label: 'Monthly Maintenance', defaultAmount: 5.00 },
  { code: 'ANNUAL_MAINT', label: 'Annual Maintenance', defaultAmount: 50.00 },
  { code: 'OVERDRAFT_FEE', label: 'Overdraft Fee', defaultAmount: 25.00 },
  { code: 'CHECK_RETURN', label: 'Check Return Fee', defaultAmount: 15.00 },
  { code: 'LOW_BALANCE', label: 'Low Balance Fee', defaultAmount: 3.00 },
];

const FeePanel: React.FC<FeePanelProps> = ({ accountId, accountBalance, onFeeAssessed }) => {
  const [feeCode, setFeeCode] = useState('MONTHLY_MAINT');
  const [amount, setAmount] = useState(5.00);
  const [narration, setNarration] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<FeeAssessmentResult | null>(null);
  const [error, setError] = useState('');

  const handleFeeCodeChange = (code: string) => {
    setFeeCode(code);
    const selected = FEE_CODES.find(f => f.code === code);
    if (selected) setAmount(selected.defaultAmount);
  };

  const handleAssessFee = async () => {
    if (!accountId || amount <= 0) {
      setError('Account ID and valid amount required');
      return;
    }

    if (accountBalance < amount) {
      setError(`Insufficient balance. Available: ${accountBalance.toFixed(2)}`);
      return;
    }

    setIsLoading(true);
    setError('');
    setResult(null);

    try {
      const res = await assessAccountFee({
        feeCode,
        amount,
        accountId,
        narration: narration || `Fee: ${FEE_CODES.find(f => f.code === feeCode)?.label}`,
        clientReference: `FEE-${Date.now()}`,
      });

      setResult(res);
      setAmount(FEE_CODES.find(f => f.code === feeCode)?.defaultAmount || 0);
      setNarration('');
      onFeeAssessed?.();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to assess fee');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm">
      <div className="flex items-center gap-2 mb-6">
        <DollarSign className="w-5 h-5 text-blue-600" />
        <h3 className="text-lg font-semibold text-gray-900">Fee Assessment</h3>
      </div>

      <div className="space-y-4">
        {/* Fee Code Selection */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Fee Type</label>
          <select
            value={feeCode}
            onChange={(e) => handleFeeCodeChange(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {FEE_CODES.map(fee => (
              <option key={fee.code} value={fee.code}>
                {fee.label} (GHS {fee.defaultAmount.toFixed(2)})
              </option>
            ))}
          </select>
        </div>

        {/* Amount */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Amount (GHS)</label>
          <input
            type="number"
            value={amount}
            onChange={(e) => setAmount(parseFloat(e.target.value) || 0)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            step="0.01"
            min="0"
          />
        </div>

        {/* Narration */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Narration (Optional)</label>
          <input
            type="text"
            value={narration}
            onChange={(e) => setNarration(e.target.value)}
            placeholder="Fee description..."
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Balance Check */}
        <div className="p-3 bg-gray-50 rounded-lg border border-gray-200">
          <p className="text-sm text-gray-600">
            Account Balance: <span className="font-semibold text-gray-900">GHS {accountBalance.toFixed(2)}</span>
          </p>
          {amount > accountBalance && (
            <p className="text-xs text-red-600 mt-1">⚠️ Insufficient available balance</p>
          )}
        </div>

        {/* Error Message */}
        {error && (
          <div className="p-3 bg-red-50 border border-red-200 rounded-lg flex items-start gap-2">
            <AlertCircle className="w-4 h-4 text-red-600 mt-0.5 flex-shrink-0" />
            <p className="text-sm text-red-600">{error}</p>
          </div>
        )}

        {/* Result */}
        {result && (
          <div className="p-3 bg-green-50 border border-green-200 rounded-lg">
            <div className="flex items-start gap-2">
              <CheckCircle className="w-4 h-4 text-green-600 mt-0.5 flex-shrink-0" />
              <div className="text-sm">
                <p className="font-semibold text-green-900">Fee Assessed Successfully</p>
                <p className="text-green-700 text-xs mt-1">
                  Transaction ID: {result.transactionId}
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Action Button */}
        <button
          onClick={handleAssessFee}
          disabled={isLoading || amount <= 0 || !accountId}
          className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-lg transition flex items-center justify-center gap-2"
        >
          {isLoading && <Loader2 className="w-4 h-4 animate-spin" />}
          {isLoading ? 'Assessing...' : 'Assess Fee'}
        </button>
      </div>
    </div>
  );
};

export default FeePanel;
