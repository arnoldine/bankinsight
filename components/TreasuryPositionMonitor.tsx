import React, { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown, ArrowRight, DollarSign } from 'lucide-react';

interface TreasuryPosition {
  id: number;
  positionDate: string;
  currency: string;
  openingBalance: number;
  deposits: number;
  withdrawals: number;
  closingBalance: number;
  vaultBalance: number | null;
  nostroBalance: number | null;
  positionStatus: string;
}

interface PositionSummary {
  currency: string;
  currentBalance: number;
  exposureLimit: number;
  utilizationPercent: number;
  status: string;
}

export default function TreasuryPositionMonitor() {
  const [positions, setPositions] = useState<TreasuryPosition[]>([]);
  const [summary, setSummary] = useState<PositionSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedCurrency, setSelectedCurrency] = useState('GHS');

  const fetchPositions = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      
      // Fetch summary
      const summaryResponse = await fetch('http://localhost:5176/api/treasuryposition/summary', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (summaryResponse.ok) {
        const summaryData = await summaryResponse.json();
        setSummary(summaryData);
      }

      // Fetch positions for selected currency
      const positionsResponse = await fetch('http://localhost:5176/api/treasuryposition', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (positionsResponse.ok) {
        const positionsData = await positionsResponse.json();
        setPositions(positionsData.filter(p => p.currency === selectedCurrency));
      }
    } catch (error) {
      console.error('Error fetching positions:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPositions();
    const interval = setInterval(fetchPositions, 30000);
    return () => clearInterval(interval);
  }, [selectedCurrency]);

  const getStatusColor = (status: string) => {
    switch (status.toUpperCase()) {
      case 'NORMAL': return 'bg-green-100 text-green-800';
      case 'NEGATIVE': return 'bg-red-100 text-red-800';
      case 'OVER LIMIT': return 'bg-red-100 text-red-800';
      case 'RECONCILED': return 'bg-blue-100 text-blue-800';
      default: return 'bg-yellow-100 text-yellow-800';
    }
  };

  const currentPosition = positions[positions.length - 1];

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900">Treasury Position Monitor</h2>
        <p className="text-gray-600">Track daily cash positions across currencies</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        {summary.map((pos) => (
          <button
            key={pos.currency}
            onClick={() => setSelectedCurrency(pos.currency)}
            className={`rounded-lg shadow p-4 border text-left transition ${
              selectedCurrency === pos.currency
                ? 'bg-blue-50 border-blue-300 ring-2 ring-blue-400'
                : 'bg-white border-gray-200 hover:border-gray-300'
            }`}
          >
            <p className="text-sm text-gray-600">{pos.currency}</p>
            <p className="text-xl font-bold text-gray-900 mt-1">
              {pos.currentBalance < 0 ? '-' : ''}₵{Math.abs(pos.currentBalance).toLocaleString('en-US', {maximumFractionDigits: 0})}
            </p>
            <div className="mt-2 flex items-center justify-between">
              <div className="w-full bg-gray-200 rounded-full h-2 mr-2">
                <div
                  className={`h-2 rounded-full ${pos.status === 'Normal' ? 'bg-green-500' : 'bg-red-500'}`}
                  style={{ width: `${Math.min((Math.abs(pos.currentBalance) / pos.exposureLimit) * 100, 100)}%` }}
                />
              </div>
              <span className="text-xs font-semibold text-gray-600">{pos.utilizationPercent.toFixed(0)}%</span>
            </div>
          </button>
        ))}
      </div>

      {/* Current Position Details */}
      {currentPosition && (
        <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
          <div className="flex justify-between items-start mb-6">
            <div>
              <h3 className="text-lg font-semibold">Position for {selectedCurrency}</h3>
              <p className="text-sm text-gray-600">
                {new Date(currentPosition.positionDate).toLocaleDateString()}
              </p>
            </div>
            <span className={`px-3 py-1 rounded-full text-sm font-semibold ${getStatusColor(currentPosition.positionStatus)}`}>
              {currentPosition.positionStatus}
            </span>
          </div>

          <div className="grid grid-cols-3 gap-4 mb-6">
            <div className="p-4 bg-gray-50 rounded-lg border border-gray-200">
              <p className="text-sm text-gray-600">Opening Balance</p>
              <p className="text-2xl font-bold text-gray-900 mt-1">
                ₵{currentPosition.openingBalance.toLocaleString('en-US', {maximumFractionDigits: 0})}
              </p>
            </div>
            <div className="p-4 bg-blue-50 rounded-lg border border-blue-200">
              <p className="text-sm text-gray-600">Closing Balance</p>
              <p className="text-2xl font-bold text-blue-900 mt-1">
                ₵{currentPosition.closingBalance.toLocaleString('en-US', {maximumFractionDigits: 0})}
              </p>
            </div>
            <div className="p-4 bg-green-50 rounded-lg border border-green-200">
              <p className="text-sm text-gray-600">Net Change</p>
              <div className="flex items-end mt-1">
                {currentPosition.closingBalance >= currentPosition.openingBalance ? (
                  <TrendingUp className="w-5 h-5 text-green-600 mr-1" />
                ) : (
                  <TrendingDown className="w-5 h-5 text-red-600 mr-1" />
                )}
                <p className="text-2xl font-bold text-green-900">
                  ₵{Math.abs(currentPosition.closingBalance - currentPosition.openingBalance).toLocaleString('en-US', {maximumFractionDigits: 0})}
                </p>
              </div>
            </div>
          </div>

          {/* Cash Flow Details */}
          <div className="space-y-2">
            <div className="flex justify-between items-center py-2 border-b border-gray-200">
              <span className="text-gray-600">Deposits</span>
              <span className="text-green-600 font-semibold">+₵{currentPosition.deposits.toLocaleString('en-US', {maximumFractionDigits: 0})}</span>
            </div>
            <div className="flex justify-between items-center py-2 border-b border-gray-200">
              <span className="text-gray-600">Withdrawals</span>
              <span className="text-red-600 font-semibold">-₵{currentPosition.withdrawals.toLocaleString('en-US', {maximumFractionDigits: 0})}</span>
            </div>
            {currentPosition.vaultBalance !== null && (
              <div className="flex justify-between items-center py-2 border-b border-gray-200">
                <span className="text-gray-600">Vault Balance</span>
                <span className="text-gray-900 font-semibold">₵{currentPosition.vaultBalance.toLocaleString('en-US', {maximumFractionDigits: 0})}</span>
              </div>
            )}
            {currentPosition.nostroBalance !== null && (
              <div className="flex justify-between items-center py-2">
                <span className="text-gray-600">Nostro Balance</span>
                <span className="text-gray-900 font-semibold">₵{currentPosition.nostroBalance.toLocaleString('en-US', {maximumFractionDigits: 0})}</span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Position History */}
      {positions.length > 0 && (
        <div className="bg-white rounded-lg shadow overflow-hidden border border-gray-200">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-semibold">Position History ({selectedCurrency})</h3>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Date</th>
                  <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Opening</th>
                  <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Deposits</th>
                  <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Withdrawals</th>
                  <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Closing</th>
                  <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Status</th>
                </tr>
              </thead>
              <tbody>
                {positions.map((pos) => (
                  <tr key={pos.id} className="border-b border-gray-200 hover:bg-gray-50">
                    <td className="px-6 py-4 text-gray-900">{new Date(pos.positionDate).toLocaleDateString()}</td>
                    <td className="px-6 py-4 text-right text-gray-900">
                      ₵{pos.openingBalance.toLocaleString('en-US', {maximumFractionDigits: 0})}
                    </td>
                    <td className="px-6 py-4 text-right text-green-600">
                      ₵{pos.deposits.toLocaleString('en-US', {maximumFractionDigits: 0})}
                    </td>
                    <td className="px-6 py-4 text-right text-red-600">
                      ₵{pos.withdrawals.toLocaleString('en-US', {maximumFractionDigits: 0})}
                    </td>
                    <td className="px-6 py-4 text-right font-semibold text-gray-900">
                      ₵{pos.closingBalance.toLocaleString('en-US', {maximumFractionDigits: 0})}
                    </td>
                    <td className="px-6 py-4">
                      <span className={`px-2 py-1 rounded text-xs font-semibold ${getStatusColor(pos.positionStatus)}`}>
                        {pos.positionStatus}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
