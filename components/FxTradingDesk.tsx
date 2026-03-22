import React, { useState, useEffect } from 'react';
import { PlusCircle, CheckCircle, XCircle, Clock, ArrowRight } from 'lucide-react';

interface FxTrade {
  id: number;
  dealNumber: string;
  tradeDate: string;
  direction: string;
  baseCurrency: string;
  baseAmount: number;
  counterCurrency: string;
  counterAmount: number;
  exchangeRate: number;
  status: string;
  initiatedByName: string;
  approvedByName?: string;
  profitLoss?: number;
}

export default function FxTradingDesk() {
  const [trades, setTrades] = useState<FxTrade[]>([]);
  const [pendingTrades, setPendingTrades] = useState<FxTrade[]>([]);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    tradeDate: new Date().toISOString().split('T')[0],
    valueDate: new Date().toISOString().split('T')[0],
    tradeType: 'Spot',
    direction: 'Buy',
    baseCurrency: 'USD',
    baseAmount: 10000,
    counterCurrency: 'GHS',
    counterAmount: 115000,
    exchangeRate: 11.5
  });

  const fetchTrades = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      
      const [allResponse, pendingResponse] = await Promise.all([
        fetch('http://localhost:5176/api/fxtrading', {
          headers: { Authorization: `Bearer ${token}` }
        }),
        fetch('http://localhost:5176/api/fxtrading/pending', {
          headers: { Authorization: `Bearer ${token}` }
        })
      ]);

      if (allResponse.ok) {
        const data = await allResponse.json();
        setTrades(data.slice(0, 20)); // Last 20 trades
      }

      if (pendingResponse.ok) {
        const data = await pendingResponse.json();
        setPendingTrades(data);
      }
    } catch (error) {
      console.error('Error fetching trades:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTrades();
    const interval = setInterval(fetchTrades, 30000);
    return () => clearInterval(interval);
  }, []);

  const handleCreateTrade = async () => {
    try {
      const token = localStorage.getItem('token');
      const response = await fetch('http://localhost:5176/api/fxtrading', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(form)
      });
      if (response.ok) {
        setShowForm(false);
        fetchTrades();
      }
    } catch (error) {
      console.error('Error creating trade:', error);
    }
  };

  const handleApproveTrade = async (tradeId: number) => {
    try {
      const token = localStorage.getItem('token');
      const response = await fetch('http://localhost:5176/api/fxtrading/approve', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ tradeId, approved: true })
      });
      if (response.ok) {
        fetchTrades();
      }
    } catch (error) {
      console.error('Error approving trade:', error);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'settled': return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'confirmed': return <Clock className="w-5 h-5 text-blue-600" />;
      case 'cancelled': return <XCircle className="w-5 h-5 text-red-600" />;
      case 'pending': return <Clock className="w-5 h-5 text-yellow-600" />;
      default: return null;
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">FX Trading Desk</h2>
          <p className="text-gray-600">Execute and track foreign exchange transactions</p>
        </div>
        <button
          onClick={() => setShowForm(!showForm)}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 flex items-center"
        >
          <PlusCircle className="w-5 h-5 mr-2" />
          New Trade
        </button>
      </div>

      {/* Trade Form */}
      {showForm && (
        <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
          <h3 className="text-lg font-semibold mb-4">Create FX Trade</h3>
          <div className="grid grid-cols-3 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Trade Type</label>
              <select
                value={form.tradeType}
                onChange={(e) => setForm({ ...form, tradeType: e.target.value })}
                className="w-full border border-gray-300 rounded px-3 py-2"
              >
                <option>Spot</option>
                <option>Forward</option>
                <option>Swap</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Direction</label>
              <select
                value={form.direction}
                onChange={(e) => setForm({ ...form, direction: e.target.value })}
                className="w-full border border-gray-300 rounded px-3 py-2"
              >
                <option>Buy</option>
                <option>Sell</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Rate</label>
              <input
                type="number"
                step="0.001"
                value={form.exchangeRate}
                onChange={(e) => setForm({ ...form, exchangeRate: parseFloat(e.target.value) })}
                className="w-full border border-gray-300 rounded px-3 py-2"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Buy Currency</label>
              <div className="flex gap-2">
                <input type="text" maxLength={3} value={form.baseCurrency} onChange={(e) => setForm({ ...form, baseCurrency: e.target.value })} className="w-20 border border-gray-300 rounded px-3 py-2" />
                <input type="number" value={form.baseAmount} onChange={(e) => setForm({ ...form, baseAmount: parseFloat(e.target.value) })} className="flex-1 border border-gray-300 rounded px-3 py-2" />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Sell Currency</label>
              <div className="flex gap-2">
                <input type="text" maxLength={3} value={form.counterCurrency} onChange={(e) => setForm({ ...form, counterCurrency: e.target.value })} className="w-20 border border-gray-300 rounded px-3 py-2" />
                <input type="number" value={form.counterAmount} onChange={(e) => setForm({ ...form, counterAmount: parseFloat(e.target.value) })} className="flex-1 border border-gray-300 rounded px-3 py-2" />
              </div>
            </div>
          </div>

          <button onClick={handleCreateTrade} className="w-full bg-green-600 text-white py-2 rounded-lg hover:bg-green-700">
            Execute Trade
          </button>
        </div>
      )}

      {/* Pending Approvals */}
      {pendingTrades.length > 0 && (
        <div className="bg-yellow-50 rounded-lg shadow p-6 border border-yellow-200">
          <h3 className="text-lg font-semibold mb-4 flex items-center">
            <Clock className="w-5 h-5 mr-2 text-yellow-600" />
            Pending Approvals ({pendingTrades.length})
          </h3>
          <div className="space-y-2">
            {pendingTrades.map((trade) => (
              <div key={trade.id} className="flex items-center justify-between bg-white p-3 rounded border border-yellow-200">
                <div className="flex-1">
                  <p className="font-semibold text-gray-900">
                    {trade.direction} {trade.baseAmount.toLocaleString()} {trade.baseCurrency} @ {trade.exchangeRate}
                  </p>
                  <p className="text-sm text-gray-600">{trade.dealNumber} - Initiated: {new Date(trade.tradeDate).toLocaleDateString()}</p>
                </div>
                <button
                  onClick={() => handleApproveTrade(trade.id)}
                  className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 text-sm font-semibold"
                >
                  Approve
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Trade History */}
      <div className="bg-white rounded-lg shadow overflow-hidden border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-semibold">Recent Trades</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Deal</th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Trade</th>
                <th className="px-6 py-3 text-center text-sm font-semibold text-gray-900">Rate</th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">P&L</th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Status</th>
              </tr>
            </thead>
            <tbody>
              {trades.map((trade) => (
                <tr key={trade.id} className="border-b border-gray-200 hover:bg-gray-50">
                  <td className="px-6 py-4 font-medium text-gray-900">{trade.dealNumber}</td>
                  <td className="px-6 py-4 text-gray-900">
                    {trade.direction} {trade.baseAmount.toLocaleString()} {trade.baseCurrency} for {trade.counterAmount.toLocaleString()} {trade.counterCurrency}
                  </td>
                  <td className="px-6 py-4 text-center font-semibold text-gray-900">{trade.exchangeRate.toFixed(4)}</td>
                  <td className="px-6 py-4 text-right">
                    {trade.profitLoss ? (
                      <span className={trade.profitLoss >= 0 ? 'text-green-600 font-semibold' : 'text-red-600 font-semibold'}>
                        ₵{trade.profitLoss.toLocaleString('en-US', {maximumFractionDigits: 0})}
                      </span>
                    ) : '-'}
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      {getStatusIcon(trade.status)}
                      <span className="text-sm font-semibold text-gray-900">{trade.status}</span>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
