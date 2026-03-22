import React, { useState } from 'react';
import { AlertTriangle, CheckCircle, Loader2, AlertCircle } from 'lucide-react';
import { assessLoanPenalty } from '../services/api';

interface PenaltyPanelProps {
  loanId: string;
  outstandingBalance: number;
  daysPastDue: number;
  onPenaltyAssessed?: () => void;
}

interface PenaltyAssessmentResult {
  loanId: string;
  penaltyAmount: number;
  penaltyRate: number;
  daysPastDue: number;
  outstandingBalance: number;
  reason: string;
  assessedAt: string;
}

const PENALTY_RATES = [
  { rate: 1.0, label: '1% - Minor Delay', minDays: 0 },
  { rate: 2.5, label: '2.5% - Moderate Delay', minDays: 15 },
  { rate: 5.0, label: '5% - Significant Delay', minDays: 30 },
  { rate: 10.0, label: '10% - Extended Delay', minDays: 60 },
];

const PenaltyPanel: React.FC<PenaltyPanelProps> = ({ loanId, outstandingBalance, daysPastDue, onPenaltyAssessed }) => {
  const [penaltyRate, setPenaltyRate] = useState(2.5);
  const [reason, setReason] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<PenaltyAssessmentResult | null>(null);
  const [error, setError] = useState('');

  const calculatePenaltyAmount = (rate: number) => {
    return Math.round((outstandingBalance * rate / 100) * 100) / 100;
  };

  const handleAssessPenalty = async () => {
    if (!loanId || outstandingBalance <= 0) {
      setError('Invalid loan or outstanding balance');
      return;
    }

    setIsLoading(true);
    setError('');
    setResult(null);

    try {
      const res = await assessLoanPenalty(loanId, {
        penaltyRate,
        reason: reason || `Late payment penalty (${daysPastDue} days overdue)`,
        clientReference: `PEN-${Date.now()}`,
      });

      setResult(res);
      setReason('');
      onPenaltyAssessed?.();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to assess penalty');
    } finally {
      setIsLoading(false);
    }
  };

  const suggestedRate = PENALTY_RATES.find(p => daysPastDue >= p.minDays)?.rate || 2.5;

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm">
      <div className="flex items-center gap-2 mb-6">
        <AlertTriangle className="w-5 h-5 text-amber-600" />
        <h3 className="text-lg font-semibold text-gray-900">Penalty Assessment</h3>
      </div>

      {daysPastDue > 0 && (
        <div className="mb-6 p-4 bg-amber-50 border border-amber-200 rounded-lg">
          <p className="text-sm font-semibold text-amber-900">⚠️ Loan is {daysPastDue} days past due</p>
          <p className="text-xs text-amber-700 mt-1">Suggested penalty rate: {suggestedRate}%</p>
        </div>
      )}

      <div className="space-y-4">
        {/* Loan Status */}
        <div className="grid grid-cols-2 gap-4 p-4 bg-gray-50 rounded-lg border border-gray-200">
          <div>
            <p className="text-xs text-gray-600">Outstanding Balance</p>
            <p className="text-lg font-semibold text-gray-900">GHS {outstandingBalance.toFixed(2)}</p>
          </div>
          <div>
            <p className="text-xs text-gray-600">Days Past Due</p>
            <p className="text-lg font-semibold text-gray-900">{daysPastDue}</p>
          </div>
        </div>

        {/* Penalty Rate Selection */}
        <div>
          <div className="flex items-center justify-between mb-2">
            <label className="block text-sm font-medium text-gray-700">Penalty Rate (%)</label>
            <span className="text-sm font-semibold text-amber-600">
              Calculated: GHS {calculatePenaltyAmount(penaltyRate).toFixed(2)}
            </span>
          </div>
          <div className="grid grid-cols-2 gap-2">
            {PENALTY_RATES.map(pr => (
              <button
                key={pr.rate}
                onClick={() => setPenaltyRate(pr.rate)}
                className={`p-2 rounded-lg border text-sm font-medium transition ${
                  penaltyRate === pr.rate
                    ? 'bg-amber-100 border-amber-400 text-amber-900'
                    : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50'
                }`}
              >
                <div className="font-semibold">{pr.rate}%</div>
                <div className="text-xs mt-1">{pr.label}</div>
              </button>
            ))}
          </div>
        </div>

        {/* Custom Rate */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Or Enter Custom Rate</label>
          <input
            type="number"
            value={penaltyRate}
            onChange={(e) => setPenaltyRate(parseFloat(e.target.value) || 0)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500"
            step="0.1"
            min="0"
            max="100"
          />
        </div>

        {/* Reason */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Reason (Optional)</label>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Explain the reason for penalty assessment..."
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500"
            rows={2}
          />
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
                <p className="font-semibold text-green-900">Penalty Assessed</p>
                <p className="text-green-700 text-xs mt-1">
                  Amount: GHS {result.penaltyAmount.toFixed(2)} 
                  <br />
                  New Outstanding: GHS {result.outstandingBalance.toFixed(2)}
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Action Button */}
        <button
          onClick={handleAssessPenalty}
          disabled={isLoading || penaltyRate <= 0 || !loanId}
          className="w-full bg-amber-600 hover:bg-amber-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-lg transition flex items-center justify-center gap-2"
        >
          {isLoading && <Loader2 className="w-4 h-4 animate-spin" />}
          {isLoading ? 'Assessing...' : 'Assess Penalty'}
        </button>
      </div>
    </div>
  );
};

export default PenaltyPanel;
