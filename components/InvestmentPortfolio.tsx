import React, { useState, useEffect } from 'react';
import { PlusCircle, Trash2, CheckCircle, XCircle } from 'lucide-react';

interface Investment {
  id: number;
  investmentNumber: string;
  instrument: string;
  currency: string;
  principalAmount: number;
  interestRate: number;
  placementDate: string;
  maturityDate: string;
  status: string;
  daysToMaturity: number;
  accruedInterest: number;
  maturityValue: number;
}

interface Portfolio {
  totalInvestments: number;
  totalPrincipal: number;
  totalAccruedInterest: number;
  totalMaturityValue: number;
  averageYield: number;
  byType: Record<string, number>;
  byCurrency: Record<string, number>;
}

export default function InvestmentPortfolio() {
  const [investments, setInvestments] = useState<Investment[]>([]);
  const [portfolio, setPortfolio] = useState<Portfolio | null>(null);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);

  const fetchPortfolio = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      const response = await fetch('http://localhost:5176/api/investment/portfolio', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        setPortfolio(data);
        setInvestments(data.maturityCalendar || []);
      }
    } catch (error) {
      console.error('Error fetching portfolio:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPortfolio();
    const interval = setInterval(fetchPortfolio, 60000);
    return () => clearInterval(interval);
  }, []);

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'bg-green-100 text-green-800';
      case 'matured': return 'bg-gray-100 text-gray-800';
      case 'liquidated': return 'bg-red-100 text-red-800';
      default: return 'bg-yellow-100 text-yellow-800';
    }
  };

  const getMaturityWarning = (daysToMaturity: number) => {
    if (daysToMaturity < 0) return <span className="text-red-600 font-semibold">Matured</span>;
    if (daysToMaturity <= 7) return <span className="text-red-600 font-semibold">{daysToMaturity} days</span>;
    if (daysToMaturity <= 30) return <span className="text-yellow-600">{daysToMaturity} days</span>;
    return <span className="text-green-600">{daysToMaturity} days</span>;
  };

  return (
    <div className="space-y-6">
      {/* Portfolio Summary Cards */}
      {portfolio && (
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div className="bg-gradient-to-br from-blue-50 to-blue-100 rounded-lg shadow p-4 border border-blue-200">
            <p className="text-sm text-gray-600">Total Investments</p>
            <p className="text-2xl font-bold text-blue-900">{portfolio.totalInvestments}</p>
          </div>
          <div className="bg-gradient-to-br from-green-50 to-green-100 rounded-lg shadow p-4 border border-green-200">
            <p className="text-sm text-gray-600">Principal</p>
            <p className="text-2xl font-bold text-green-900">₵{portfolio.totalPrincipal.toLocaleString('en-US', {maximumFractionDigits: 2})}</p>
          </div>
          <div className="bg-gradient-to-br from-purple-50 to-purple-100 rounded-lg shadow p-4 border border-purple-200">
            <p className="text-sm text-gray-600">Accrued Interest</p>
            <p className="text-2xl font-bold text-purple-900">₵{portfolio.totalAccruedInterest.toLocaleString('en-US', {maximumFractionDigits: 2})}</p>
          </div>
          <div className="bg-gradient-to-br from-indigo-50 to-indigo-100 rounded-lg shadow p-4 border border-indigo-200">
            <p className="text-sm text-gray-600">Maturity Value</p>
            <p className="text-2xl font-bold text-indigo-900">₵{portfolio.totalMaturityValue.toLocaleString('en-US', {maximumFractionDigits: 2})}</p>
          </div>
          <div className="bg-gradient-to-br from-orange-50 to-orange-100 rounded-lg shadow p-4 border border-orange-200">
            <p className="text-sm text-gray-600">Avg. Yield</p>
            <p className="text-2xl font-bold text-orange-900">{portfolio.averageYield.toFixed(2)}%</p>
          </div>
        </div>
      )}

      {/* Investment Type Breakdown */}
      {portfolio && (
        <div className="grid grid-cols-2 gap-4">
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <h3 className="text-lg font-semibold mb-4">By Investment Type</h3>
            <div className="space-y-2">
              {Object.entries(portfolio.byType).map(([type, amount]) => (
                <div key={type} className="flex justify-between items-center">
                  <span className="text-gray-700">{type}</span>
                  <span className="font-semibold text-gray-900">₵{Number(amount).toLocaleString('en-US', {maximumFractionDigits: 0})}</span>
                </div>
              ))}
            </div>
          </div>
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <h3 className="text-lg font-semibold mb-4">By Currency</h3>
            <div className="space-y-2">
              {Object.entries(portfolio.byCurrency).map(([currency, amount]) => (
                <div key={currency} className="flex justify-between items-center">
                  <span className="text-gray-700">{currency}</span>
                  <span className="font-semibold text-gray-900">{currency} {Number(amount).toLocaleString('en-US', {maximumFractionDigits: 0})}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Maturity Calendar */}
      <div className="bg-white rounded-lg shadow overflow-hidden border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-semibold">Next 30 Days Maturity</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Investment</th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Instrument</th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Principal</th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900">Interest</th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900">Maturity</th>
                <th className="px-6 py-3 text-center text-sm font-semibold text-gray-900">Days Left</th>
              </tr>
            </thead>
            <tbody>
              {investments.map((inv) => (
                <tr key={inv.id} className="border-b border-gray-200 hover:bg-gray-50">
                  <td className="px-6 py-4 font-medium text-gray-900">{inv.investmentNumber}</td>
                  <td className="px-6 py-4 text-gray-600">{inv.instrument}</td>
                  <td className="px-6 py-4 text-right text-gray-900">
                    {inv.currency} {inv.principalAmount.toLocaleString('en-US', {maximumFractionDigits: 2})}
                  </td>
                  <td className="px-6 py-4 text-right text-green-600 font-semibold">
                    {inv.accruedInterest.toLocaleString('en-US', {maximumFractionDigits: 2})}
                  </td>
                  <td className="px-6 py-4 text-gray-600">{new Date(inv.maturityDate).toLocaleDateString()}</td>
                  <td className="px-6 py-4 text-center">
                    {getMaturityWarning(inv.daysToMaturity)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {investments.length === 0 && (
          <div className="px-6 py-8 text-center text-gray-500">
            No investments maturing in the next 30 days.
          </div>
        )}
      </div>
    </div>
  );
}
