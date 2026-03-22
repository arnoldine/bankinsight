import React, { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown, AlertTriangle, CheckCircle, RefreshCw } from 'lucide-react';

interface RiskMetric {
  id: number;
  metricType: string;
  metricValue: number;
  threshold: number;
  thresholdBreached: boolean;
  currency: string | null;
  status: string;
  calculatedAt: string;
}

interface RiskDashboardData {
  asOfDate: string;
  vaRValue: number;
  vaRThreshold: number;
  vaRBreached: boolean;
  lcrValue: number;
  lcrThreshold: number;
  lcrBreached: boolean;
  currencyExposure: Record<string, number>;
  recentAlerts: RiskMetric[];
}

const formatMoney = (value: number) => `GHS ${value.toLocaleString('en-US', { maximumFractionDigits: 0 })}`;

export default function RiskDashboard() {
  const [dashboard, setDashboard] = useState<RiskDashboardData | null>(null);
  const [loading, setLoading] = useState(false);

  const fetchDashboard = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      const response = await fetch('http://localhost:5176/api/riskanalytics/dashboard', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        setDashboard(data);
      }
    } catch (error) {
      console.error('Error fetching risk dashboard:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboard();
    const interval = setInterval(fetchDashboard, 60000);
    return () => clearInterval(interval);
  }, []);

  const getBreachStatus = (breached: boolean) => {
    return breached ? (
      <div className="flex items-center text-red-600">
        <AlertTriangle className="w-5 h-5 mr-2" />
        <span className="font-semibold">BREACHED</span>
      </div>
    ) : (
      <div className="flex items-center text-green-600">
        <CheckCircle className="w-5 h-5 mr-2" />
        <span className="font-semibold">NORMAL</span>
      </div>
    );
  };

  return (
    <div className="space-y-6">
      <div className="rounded-[28px] bg-[linear-gradient(135deg,#0f172a,#1e293b,#334155)] p-6 text-white shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.22em] text-white/60">Risk monitoring</p>
            <h2 className="mt-2 text-2xl font-bold">Real-time risk metrics, exposure monitoring, and alert watchlist.</h2>
          </div>
          <button
            onClick={fetchDashboard}
            disabled={loading}
            className="inline-flex items-center gap-2 rounded-2xl bg-white/10 px-4 py-3 text-sm font-semibold text-white transition hover:bg-white/20 disabled:opacity-60"
          >
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh Risk Data
          </button>
        </div>
      </div>

      {dashboard && (
        <>
          <div className="grid grid-cols-1 gap-4 xl:grid-cols-3">
            <div className={`rounded-2xl shadow p-6 border ${dashboard.vaRBreached ? 'bg-red-50 border-red-200' : 'bg-green-50 border-green-200'}`}>
              <div className="flex justify-between items-start mb-4">
                <div>
                  <p className="text-sm text-gray-600">Value at Risk (95% CI, 1-Day)</p>
                  <p className="text-3xl font-bold text-gray-900 mt-2">{formatMoney(dashboard.vaRValue)}</p>
                </div>
                {dashboard.vaRBreached ? <TrendingDown className="w-8 h-8 text-red-600" /> : <TrendingUp className="w-8 h-8 text-green-600" />}
              </div>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Threshold:</span>
                  <span className="font-semibold">{formatMoney(dashboard.vaRThreshold)}</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div className={`h-2 rounded-full ${dashboard.vaRBreached ? 'bg-red-600' : 'bg-green-600'}`} style={{ width: `${Math.min((dashboard.vaRValue / dashboard.vaRThreshold) * 100, 100)}%` }} />
                </div>
              </div>
              <div className="mt-4">{getBreachStatus(dashboard.vaRBreached)}</div>
            </div>

            <div className={`rounded-2xl shadow p-6 border ${dashboard.lcrBreached ? 'bg-red-50 border-red-200' : 'bg-blue-50 border-blue-200'}`}>
              <div className="flex justify-between items-start mb-4">
                <div>
                  <p className="text-sm text-gray-600">Liquidity Coverage Ratio (LCR)</p>
                  <p className="text-3xl font-bold text-gray-900 mt-2">{dashboard.lcrValue.toFixed(1)}%</p>
                </div>
                <TrendingUp className={`w-8 h-8 ${dashboard.lcrBreached ? 'text-red-600' : 'text-blue-600'}`} />
              </div>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Regulatory Min:</span>
                  <span className="font-semibold">{dashboard.lcrThreshold}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div className={`h-2 rounded-full ${dashboard.lcrBreached ? 'bg-red-600' : 'bg-blue-600'}`} style={{ width: `${Math.min((dashboard.lcrValue / dashboard.lcrThreshold) * 100, 100)}%` }} />
                </div>
              </div>
              <div className="mt-4">{getBreachStatus(dashboard.lcrBreached)}</div>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
              <p className="text-sm text-slate-500">Alert Watchlist</p>
              <p className="mt-2 text-3xl font-bold text-slate-900">{dashboard.recentAlerts.length}</p>
              <p className="mt-2 text-sm text-slate-500">Recent threshold or concentration alerts awaiting review.</p>
              <div className="mt-4 text-xs text-slate-500">As of {new Date(dashboard.asOfDate).toLocaleString()}</div>
            </div>
          </div>

          <div className="bg-white rounded-2xl shadow p-6 border border-gray-200">
            <h3 className="text-lg font-semibold mb-4">Currency Exposure</h3>
            <div className="space-y-3">
              {Object.entries(dashboard.currencyExposure).map(([currency, exposure]) => (
                <div key={currency}>
                  <div className="flex justify-between items-center mb-1">
                    <span className="font-medium text-gray-900">{currency}</span>
                    <span className="text-gray-600">{formatMoney(Number(exposure))}</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div className="h-2 rounded-full bg-indigo-600" style={{ width: `${Math.min((Math.abs(Number(exposure)) / 10000000) * 100, 100)}%` }} />
                  </div>
                </div>
              ))}
            </div>
          </div>

          {dashboard.recentAlerts.length > 0 && (
            <div className="bg-white rounded-2xl shadow p-6 border border-gray-200">
              <h3 className="text-lg font-semibold mb-4">Recent Alerts</h3>
              <div className="space-y-3">
                {dashboard.recentAlerts.map((alert) => (
                  <div key={alert.id} className="flex items-start p-3 bg-amber-50 border border-amber-200 rounded-2xl">
                    <AlertTriangle className="w-5 h-5 text-amber-600 mr-3 mt-0.5 flex-shrink-0" />
                    <div className="flex-1">
                      <p className="font-semibold text-gray-900">{alert.metricType}</p>
                      <p className="text-sm text-gray-600">
                        Value: {alert.metricValue.toFixed(2)} | Threshold: {alert.threshold.toFixed(2)}
                        {alert.currency && ` | ${alert.currency}`}
                      </p>
                      <p className="text-xs text-gray-500 mt-1">{new Date(alert.calculatedAt).toLocaleString()}</p>
                    </div>
                    <span className={`px-2 py-1 rounded text-xs font-semibold ${alert.status === 'Escalated' ? 'bg-red-100 text-red-800' : 'bg-yellow-100 text-yellow-800'}`}>
                      {alert.status}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}

      {loading && (
        <div className="text-center py-8">
          <p className="text-gray-600">Loading risk metrics...</p>
        </div>
      )}
    </div>
  );
}
