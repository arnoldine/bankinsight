import React, { useState } from 'react';
import { TrendingDown, CheckCircle, Loader2, RefreshCw, AlertCircle } from 'lucide-react';
import { classifyLoan } from '../services/api';

interface NPLPanelProps {
  loanId: string;
  onClassificationComplete?: () => void;
}

interface ClassificationResult {
  loanId: string;
  bogTier: string;
  daysPastDue: number;
  outstandingPrincipal: number;
  outstandingInterest: number;
  provisioningAmount: number;
  provisioningRate: number;
  evaluationDate: string;
}

const BOG_TIERS = [
  {
    tier: 'Current',
    dpd: '0',
    provisionRate: '1%',
    description: 'No arrears',
    color: 'bg-green-50 border-green-200',
    textColor: 'text-green-700',
    badgeColor: 'bg-green-100 text-green-800',
  },
  {
    tier: 'Oversight',
    dpd: '1-30',
    provisionRate: '5%',
    description: 'Up to 1 month overdue',
    color: 'bg-blue-50 border-blue-200',
    textColor: 'text-blue-700',
    badgeColor: 'bg-blue-100 text-blue-800',
  },
  {
    tier: 'Substandard',
    dpd: '31-90',
    provisionRate: '25%',
    description: '1-3 months overdue',
    color: 'bg-yellow-50 border-yellow-200',
    textColor: 'text-yellow-700',
    badgeColor: 'bg-yellow-100 text-yellow-800',
  },
  {
    tier: 'Doubtful',
    dpd: '91-180',
    provisionRate: '50%',
    description: '3-6 months overdue',
    color: 'bg-orange-50 border-orange-200',
    textColor: 'text-orange-700',
    badgeColor: 'bg-orange-100 text-orange-800',
  },
  {
    tier: 'Loss',
    dpd: '181+',
    provisionRate: '100%',
    description: 'Over 6 months overdue',
    color: 'bg-red-50 border-red-200',
    textColor: 'text-red-700',
    badgeColor: 'bg-red-100 text-red-800',
  },
];

const NPLPanel: React.FC<NPLPanelProps> = ({ loanId, onClassificationComplete }) => {
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<ClassificationResult | null>(null);
  const [error, setError] = useState('');

  const handleClassify = async () => {
    if (!loanId) {
      setError('Loan ID required');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      const res = await classifyLoan(loanId);
      setResult(res);
      onClassificationComplete?.();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to classify loan');
    } finally {
      setIsLoading(false);
    }
  };

  const getTierInfo = (tier: string) => BOG_TIERS.find(t => t.tier === tier);

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm">
      <div className="flex items-center gap-2 mb-6">
        <TrendingDown className="w-5 h-5 text-red-600" />
        <h3 className="text-lg font-semibold text-gray-900">NPL Classification & Provisioning</h3>
      </div>

      {!result ? (
        <div className="space-y-4">
          <p className="text-sm text-gray-600">
            Evaluate this loan against Bank of Ghana (BoG) classification standards. This will calculate the provision amount based on days past due.
          </p>

          {/* BoG Tier Reference */}
          <div className="space-y-2">
            <p className="text-xs font-semibold text-gray-700 uppercase">BoG Classification Tiers</p>
            <div className="grid grid-cols-1 gap-2">
              {BOG_TIERS.map(tier => (
                <div key={tier.tier} className={`p-3 rounded-lg border ${tier.color}`}>
                  <div className="flex items-center justify-between">
                    <div>
                      <p className={`font-semibold ${tier.textColor}`}>{tier.tier}</p>
                      <p className="text-xs text-gray-600 mt-1">{tier.description}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-xs text-gray-600">DPD: <span className="font-semibold">{tier.dpd}</span></p>
                      <p className="text-xs text-gray-600 mt-1">Provision: <span className="font-semibold">{tier.provisionRate}</span></p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg flex items-start gap-2">
              <AlertCircle className="w-4 h-4 text-red-600 mt-0.5 flex-shrink-0" />
              <p className="text-sm text-red-600">{error}</p>
            </div>
          )}

          {/* Action Button */}
          <button
            onClick={handleClassify}
            disabled={isLoading || !loanId}
            className="w-full bg-red-600 hover:bg-red-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-lg transition flex items-center justify-center gap-2"
          >
            {isLoading && <Loader2 className="w-4 h-4 animate-spin" />}
            {isLoading ? 'Classifying...' : 'Classify & Calculate Provision'}
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {/* Classification Result */}
          <div className={`p-4 rounded-lg border ${getTierInfo(result.bogTier)?.color}`}>
            <div className="flex items-start justify-between">
              <div>
                <p className="text-xs text-gray-600 mb-1">BoG Classification Tier</p>
                <p className={`text-2xl font-bold ${getTierInfo(result.bogTier)?.textColor}`}>
                  {result.bogTier}
                </p>
              </div>
              <div className="text-right">
                <p className="text-xs text-gray-600 mb-1">Days Past Due</p>
                <p className="text-2xl font-bold text-gray-900">{result.daysPastDue}</p>
              </div>
            </div>
          </div>

          {/* Financial Details */}
          <div className="grid grid-cols-2 gap-3">
            <div className="p-3 bg-gray-50 rounded-lg border border-gray-200">
              <p className="text-xs text-gray-600 mb-1">Outstanding Principal</p>
              <p className="text-lg font-semibold text-gray-900">GHS {result.outstandingPrincipal.toFixed(2)}</p>
            </div>
            <div className="p-3 bg-gray-50 rounded-lg border border-gray-200">
              <p className="text-xs text-gray-600 mb-1">Outstanding Interest</p>
              <p className="text-lg font-semibold text-gray-900">GHS {result.outstandingInterest.toFixed(2)}</p>
            </div>
          </div>

          {/* Provisioning Information */}
          <div className="p-4 bg-blue-50 rounded-lg border border-blue-200">
            <p className="text-xs text-blue-600 font-semibold mb-3">Provisioning Calculation</p>
            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span className="text-blue-700">Total Exposure:</span>
                <span className="font-semibold text-blue-900">
                  GHS {(result.outstandingPrincipal + result.outstandingInterest).toFixed(2)}
                </span>
              </div>
              <div className="flex items-center justify-between text-sm border-t border-blue-200 pt-2">
                <span className="text-blue-700">Provision Rate ({result.bogTier}):</span>
                <span className="font-semibold text-blue-900">{(result.provisioningRate * 100).toFixed(2)}%</span>
              </div>
              <div className="flex items-center justify-between text-sm border-t border-blue-200 pt-2">
                <span className="text-blue-700 font-semibold">Required Provision:</span>
                <span className="text-lg font-bold text-blue-900">
                  GHS {result.provisioningAmount.toFixed(2)}
                </span>
              </div>
            </div>
          </div>

          {/* Success Message */}
          <div className="p-3 bg-green-50 border border-green-200 rounded-lg flex items-start gap-2">
            <CheckCircle className="w-4 h-4 text-green-600 mt-0.5 flex-shrink-0" />
            <div className="text-sm">
              <p className="font-semibold text-green-900">Classification Complete</p>
              <p className="text-green-700 text-xs mt-1">
                Evaluated on: {new Date(result.evaluationDate).toLocaleString()}
              </p>
            </div>
          </div>

          {/* Re-evaluate Button */}
          <button
            onClick={() => {
              setResult(null);
              setError('');
            }}
            className="w-full bg-gray-600 hover:bg-gray-700 text-white font-medium py-2 px-4 rounded-lg transition flex items-center justify-center gap-2"
          >
            <RefreshCw className="w-4 h-4" />
            Re-evaluate
          </button>
        </div>
      )}
    </div>
  );
};

export default NPLPanel;
