import React from 'react';
import { AlertCircle, CheckCircle, DollarSign, Loader2 } from 'lucide-react';
import { feeService, ProductChargeAssessment } from '../src/services/feeService';

interface FeePanelProps {
  accountId: string;
  accountBalance: number;
  onFeeAssessed?: () => void;
}

interface FeeAssessmentResult {
  transactionId: string;
  accountId: string;
  chargeCode: string;
  chargeName: string;
  chargeType: 'FEE' | 'COMMISSION';
  amount: number;
  narration: string;
  postedAt: string;
}

const formatMoney = (value: number) => `GHS ${value.toFixed(2)}`;

const FeePanel: React.FC<FeePanelProps> = ({ accountId, accountBalance, onFeeAssessed }) => {
  const [charges, setCharges] = React.useState<ProductChargeAssessment[]>([]);
  const [chargeCode, setChargeCode] = React.useState('');
  const [amount, setAmount] = React.useState(0);
  const [narration, setNarration] = React.useState('');
  const [isLoading, setIsLoading] = React.useState(false);
  const [result, setResult] = React.useState<FeeAssessmentResult | null>(null);
  const [error, setError] = React.useState('');

  React.useEffect(() => {
    let active = true;
    if (!accountId) {
      setCharges([]);
      setChargeCode('');
      return;
    }

    feeService.getApplicableCharges(accountId, 'MANUAL')
      .then((items) => {
        if (!active) return;
        setCharges(items);
        if (items.length > 0) {
          setChargeCode(items[0].chargeCode);
          setAmount(Number(items[0].flatAmount || items[0].minimumAmount || 0));
        }
      })
      .catch((err) => {
        if (!active) return;
        setError(err instanceof Error ? err.message : 'Failed to load configured charges.');
      });

    return () => {
      active = false;
    };
  }, [accountId]);

  const selectedCharge = React.useMemo(
    () => charges.find((charge) => charge.chargeCode === chargeCode) || null,
    [charges, chargeCode],
  );

  const handleChargeCodeChange = (code: string) => {
    setChargeCode(code);
    const selected = charges.find((charge) => charge.chargeCode === code);
    if (selected) {
      setAmount(Number(selected.flatAmount || selected.minimumAmount || 0));
    }
  };

  const handleApplyCharge = async () => {
    if (!accountId || !selectedCharge) {
      setError('Select a configured charge before continuing.');
      return;
    }

    if (amount > 0 && accountBalance < amount) {
      setError(`Insufficient balance. Available: ${formatMoney(accountBalance)}`);
      return;
    }

    setIsLoading(true);
    setError('');
    setResult(null);

    try {
      const response = await feeService.applyAccountCharge({
        accountId,
        chargeCode,
        overrideAmount: amount > 0 ? amount : undefined,
        narration: narration || `${selectedCharge.chargeType}: ${selectedCharge.chargeName}`,
        clientReference: `CHG-${Date.now()}`,
      });

      setResult(response);
      setAmount(Number(selectedCharge.flatAmount || selectedCharge.minimumAmount || 0));
      setNarration('');
      onFeeAssessed?.();
    } catch (err: any) {
      setError(err?.data?.message || err?.message || 'Failed to apply charge.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm">
      <div className="flex items-center gap-2 mb-6">
        <DollarSign className="w-5 h-5 text-blue-600" />
        <h3 className="text-lg font-semibold text-gray-900">Product Charges</h3>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Configured Charge</label>
          <select
            value={chargeCode}
            onChange={(e) => handleChargeCodeChange(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Select a charge</option>
            {charges.map((charge) => (
              <option key={charge.chargeCode} value={charge.chargeCode}>
                {charge.chargeName} ({charge.chargeType})
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Amount Override (optional)</label>
          <input
            type="number"
            value={amount}
            onChange={(e) => setAmount(parseFloat(e.target.value) || 0)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            step="0.01"
            min="0"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Narration (Optional)</label>
          <input
            type="text"
            value={narration}
            onChange={(e) => setNarration(e.target.value)}
            placeholder="Charge description..."
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div className="p-3 bg-gray-50 rounded-lg border border-gray-200">
          <p className="text-sm text-gray-600">
            Account Balance: <span className="font-semibold text-gray-900">{formatMoney(accountBalance)}</span>
          </p>
          {selectedCharge && (
            <p className="mt-1 text-xs text-gray-500">
              {selectedCharge.calculationType === 'PERCENTAGE'
                ? `Configured at ${selectedCharge.rate ?? 0}% with min/max enforcement.`
                : `Configured flat amount: ${formatMoney(Number(selectedCharge.flatAmount || 0))}`}
            </p>
          )}
          {amount > 0 && amount > accountBalance && (
            <p className="text-xs text-red-600 mt-1">Insufficient available balance</p>
          )}
        </div>

        {error && (
          <div className="p-3 bg-red-50 border border-red-200 rounded-lg flex items-start gap-2">
            <AlertCircle className="w-4 h-4 text-red-600 mt-0.5 flex-shrink-0" />
            <p className="text-sm text-red-600">{error}</p>
          </div>
        )}

        {result && (
          <div className="p-3 bg-green-50 border border-green-200 rounded-lg">
            <div className="flex items-start gap-2">
              <CheckCircle className="w-4 h-4 text-green-600 mt-0.5 flex-shrink-0" />
              <div className="text-sm">
                <p className="font-semibold text-green-900">Charge applied successfully</p>
                <p className="text-green-700 text-xs mt-1">
                  {result.chargeName} posted as {result.transactionId}
                </p>
              </div>
            </div>
          </div>
        )}

        <button
          onClick={handleApplyCharge}
          disabled={isLoading || !chargeCode || !accountId}
          className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-lg transition flex items-center justify-center gap-2"
        >
          {isLoading && <Loader2 className="w-4 h-4 animate-spin" />}
          {isLoading ? 'Applying...' : 'Apply charge'}
        </button>
      </div>
    </div>
  );
};

export default FeePanel;
