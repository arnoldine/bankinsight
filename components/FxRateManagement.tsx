import React, { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown, RefreshCw, AlertCircle } from 'lucide-react';

interface FxRate {
  id: number;
  baseCurrency: string;
  targetCurrency: string;
  buyRate: number;
  sellRate: number;
  midRate: number;
  rateDate: string;
  source: string;
  isActive: boolean;
}

interface CreateRateForm {
  baseCurrency: string;
  targetCurrency: string;
  buyRate: number;
  sellRate: number;
}

export default function FxRateManagement() {
  const [rates, setRates] = useState<FxRate[]>([]);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateRateForm>({
    baseCurrency: 'GHS',
    targetCurrency: 'USD',
    buyRate: 11.50,
    sellRate: 11.60
  });
  const [fromCurrency, setFromCurrency] = useState('GHS');
  const [toCurrency, setToCurrency] = useState('USD');
  const [convertAmount, setConvertAmount] = useState(1000);
  const [convertResult, setConvertResult] = useState<number | null>(null);

  const fetchRates = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      const response = await fetch('http://localhost:5176/api/fxrate', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        setRates(data);
      }
    } catch (error) {
      console.error('Error fetching rates:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRates();
    const interval = setInterval(fetchRates, 30000); // Refresh every 30s
    return () => clearInterval(interval);
  }, []);

  const handleAddRate = async () => {
    try {
      const token = localStorage.getItem('token');
      const payload = {
        ...form,
        midRate: (form.buyRate + form.sellRate) / 2,
        rateDate: new Date().toISOString(),
        source: 'Manual'
      };
      const response = await fetch('http://localhost:5176/api/fxrate', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });
      if (response.ok) {
        setShowForm(false);
        fetchRates();
        setForm({ baseCurrency: 'GHS', targetCurrency: 'USD', buyRate: 11.50, sellRate: 11.60 });
      }
    } catch (error) {
      console.error('Error adding rate:', error);
    }
  };

  const handleConvert = async () => {
    try {
      const token = localStorage.getItem('token');
      const response = await fetch(
        `http://localhost:5176/api/fxrate/convert?amount=${convertAmount}&fromCurrency=${fromCurrency}&toCurrency=${toCurrency}`,
        { headers: { Authorization: `Bearer ${token}` } }
      );
      if (response.ok) {
        const result = await response.json();
        setConvertResult(result);
      }
    } catch (error) {
      console.error('Error in conversion:', error);
    }
  };

  const currencies = ['GHS', 'USD', 'EUR', 'GBP', 'NGN', 'XOF'];
  const spread = (rate: FxRate) => ((rate.sellRate - rate.buyRate) / rate.midRate * 100).toFixed(2);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">FX Rates Manager</h2>
          <p className="text-gray-600">Manage foreign exchange rates with Bank of Ghana integration</p>
        </div>
        <button
          onClick={() => setShowForm(!showForm)}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
        >
          Add Rate
        </button>
      </div>

      {/* Add Form */}
      {showForm && (
        <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
          <h3 className="text-lg font-semibold mb-4">Add New FX Rate</h3>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Base Currency</label>
              <select
                value={form.baseCurrency}
                onChange={(e) => setForm({ ...form, baseCurrency: e.target.value })}
                className="w-full border border-gray-300 rounded px-3 py-2"
              >
                {currencies.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Target Currency</label>
              <select
                value={form.targetCurrency}
                onChange={(e) => setForm({ ...form, targetCurrency: e.target.value })}
                className="w-full border border-gray-300 rounded px-3 py-2"
              >
                {currencies.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Buy Rate</label>
              <input
                type="number"
                step="0.001"
                value={form.buyRate}
                onChange={(e) => setForm({ ...form, buyRate: parseFloat(e.target.value) })}
                className="w-full border border-gray-300 rounded px-3 py-2"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Sell Rate</label>
              <input
                type="number"
                step="0.001"
                value={form.sellRate}
                onChange={(e) => setForm({ ...form, sellRate: parseFloat(e.target.value) })}
                className="w-full border border-gray-300 rounded px-3 py-2"
              />
            </div>
          </div>
          <button
            onClick={handleAddRate}
            className="mt-4 w-full bg-green-600 text-white py-2 rounded-lg hover:bg-green-700"
          >
            Save Rate
          </button>
        </div>
      )}

      {/* Currency Converter */}
      <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-lg shadow p-6 border border-blue-200">
        <h3 className="text-lg font-semibold mb-4">Quick Currency Converter</h3>
        <div className="grid grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">From</label>
            <select
              value={fromCurrency}
              onChange={(e) => setFromCurrency(e.target.value)}
              className="w-full border border-gray-300 rounded px-3 py-2"
            >
              {currencies.map(c => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Amount</label>
            <input
              type="number"
              value={convertAmount}
              onChange={(e) => setConvertAmount(parseFloat(e.target.value))}
              className="w-full border border-gray-300 rounded px-3 py-2"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">To</label>
            <select
              value={toCurrency}
              onChange={(e) => setToCurrency(e.target.value)}
              className="w-full border border-gray-300 rounded px-3 py-2"
            >
              {currencies.map(c => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">&nbsp;</label>
            <button
              onClick={handleConvert}
              className="w-full bg-indigo-600 text-white py-2 rounded hover:bg-indigo-700"
            >
              Convert
            </button>
          </div>
        </div>
        {convertResult !== null && (
          <div className="mt-4 p-4 bg-white rounded border border-indigo-200">
            <p className="text-center text-lg font-semibold text-indigo-600">
              {convertAmount} {fromCurrency} = {convertResult.toFixed(2)} {toCurrency}
            </p>
          </div>
        )}
      </div>

      {/* Active Rates Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
          <h3 className="text-lg font-semibold">Active FX Rates</h3>
          <button
            onClick={fetchRates}
            disabled={loading}
            className="p-2 hover:bg-gray-100 rounded"
          >
            <RefreshCw className={`w-5 h-5 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Pair</th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Buy</th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Mid</th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Sell</th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Spread %</th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Source</th>
              </tr>
            </thead>
            <tbody>
              {rates.map((rate) => (
                <tr key={rate.id} className="border-b border-gray-200 hover:bg-gray-50">
                  <td className="px-6 py-4 font-medium text-gray-900">
                    {rate.baseCurrency}/{rate.targetCurrency}
                  </td>
                  <td className="px-6 py-4 text-right text-gray-900">{rate.buyRate.toFixed(4)}</td>
                  <td className="px-6 py-4 text-right font-semibold text-indigo-600">
                    {rate.midRate.toFixed(4)}
                  </td>
                  <td className="px-6 py-4 text-right text-gray-900">{rate.sellRate.toFixed(4)}</td>
                  <td className="px-6 py-4 text-right text-gray-900">{spread(rate)}%</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{rate.source}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {rates.length === 0 && (
          <div className="px-6 py-8 text-center text-gray-500">
            No FX rates loaded. Click "Add Rate" to create your first rate.
          </div>
        )}
      </div>
    </div>
  );
}
