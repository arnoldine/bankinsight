import React, { useState, useEffect } from 'react';
import { BarChart3, DollarSign, TrendingDown, PieChart, FileText, Download } from 'lucide-react';

interface FinancialStatement {
  items: Array<{ label: string; amount: number }>;
  total: number;
}

export default function FinancialStatements() {
  const [reportType, setReportType] = useState('balance-sheet');
  const [asOfDate, setAsOfDate] = useState(new Date().toISOString().split('T')[0]);
  const [loading, setLoading] = useState(false);
  const [balanceSheet, setBalanceSheet] = useState<any>(null);

  useEffect(() => {
    // In real implementation, fetch from API
    loadFinancialData();
  }, [reportType, asOfDate]);

  const loadFinancialData = async () => {
    setLoading(true);
    try {
      // Simulate API call
      const mockBalance = {
        asOfDate,
        assets: [
          { lineItem: 'Cash & Equivalents', amount: 5_000_000, percentage: 8 },
          { lineItem: 'Customer Deposits', amount: 28_500_000, percentage: 45 },
          { lineItem: 'Investment Securities', amount: 12_000_000, percentage: 19 },
          { lineItem: 'Loans & Advances', amount: 18_000_000, percentage: 28 }
        ],
        liabilities: [
          { lineItem: 'Customer Deposits', amount: 42_000_000, percentage: 75 },
          { lineItem: 'Borrowings', amount: 5_600_000, percentage: 10 }
        ],
        equity: [
          { lineItem: 'Share Capital', amount: 8_000_000, percentage: 14 },
          { lineItem: 'Retained Earnings', amount: 1_900_000, percentage: 1 }
        ],
        totalAssets: 63_500_000,
        totalLiabilities: 47_600_000,
        totalEquity: 9_900_000
      };
      setBalanceSheet(mockBalance);
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-GH', {
      style: 'currency',
      currency: 'GHS',
      minimumFractionDigits: 0
    }).format(amount);
  };

  const renderBalanceSheet = () => (
    <div className="space-y-6">
      {/* Assets */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Assets</h3>
        <div className="space-y-3">
          {balanceSheet?.assets.map((asset: any, idx: number) => (
            <div key={idx} className="flex items-center justify-between border-b border-gray-100 pb-3">
              <div className="flex-1">
                <p className="text-gray-700 font-medium">{asset.lineItem}</p>
                <div className="mt-1 bg-gray-100 rounded-full h-2">
                  <div className="bg-blue-600 h-2 rounded-full" style={{ width: `${asset.percentage}%` }}></div>
                </div>
              </div>
              <div className="text-right ml-4">
                <p className="text-gray-900 font-semibold">{formatCurrency(asset.amount)}</p>
                <p className="text-sm text-gray-600">{asset.percentage}%</p>
              </div>
            </div>
          ))}
        </div>
        <div className="mt-4 pt-4 border-t-2 border-gray-300 flex justify-between items-center">
          <p className="text-lg font-bold text-gray-900">Total Assets</p>
          <p className="text-2xl font-bold text-blue-600">{formatCurrency(balanceSheet?.totalAssets)}</p>
        </div>
      </div>

      {/* Liabilities */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Liabilities</h3>
        <div className="space-y-3">
          {balanceSheet?.liabilities.map((liability: any, idx: number) => (
            <div key={idx} className="flex items-center justify-between border-b border-gray-100 pb-3">
              <div className="flex-1">
                <p className="text-gray-700 font-medium">{liability.lineItem}</p>
                <div className="mt-1 bg-gray-100 rounded-full h-2">
                  <div className="bg-red-600 h-2 rounded-full" style={{ width: `${liability.percentage}%` }}></div>
                </div>
              </div>
              <div className="text-right ml-4">
                <p className="text-gray-900 font-semibold">{formatCurrency(liability.amount)}</p>
                <p className="text-sm text-gray-600">{liability.percentage}%</p>
              </div>
            </div>
          ))}
        </div>
        <div className="mt-4 pt-4 border-t-2 border-gray-300 flex justify-between items-center">
          <p className="text-lg font-bold text-gray-900">Total Liabilities</p>
          <p className="text-2xl font-bold text-red-600">{formatCurrency(balanceSheet?.totalLiabilities)}</p>
        </div>
      </div>

      {/* Equity */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Equity</h3>
        <div className="space-y-3">
          {balanceSheet?.equity.map((eq: any, idx: number) => (
            <div key={idx} className="flex items-center justify-between border-b border-gray-100 pb-3">
              <div className="flex-1">
                <p className="text-gray-700 font-medium">{eq.lineItem}</p>
                <div className="mt-1 bg-gray-100 rounded-full h-2">
                  <div className="bg-green-600 h-2 rounded-full" style={{ width: `${eq.percentage}%` }}></div>
                </div>
              </div>
              <div className="text-right ml-4">
                <p className="text-gray-900 font-semibold">{formatCurrency(eq.amount)}</p>
                <p className="text-sm text-gray-600">{eq.percentage}%</p>
              </div>
            </div>
          ))}
        </div>
        <div className="mt-4 pt-4 border-t-2 border-gray-300 flex justify-between items-center">
          <p className="text-lg font-bold text-gray-900">Total Equity</p>
          <p className="text-2xl font-bold text-green-600">{formatCurrency(balanceSheet?.totalEquity)}</p>
        </div>
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <BarChart3 className="w-6 h-6 text-blue-600" />
            Financial Statements
          </h2>
          <p className="text-gray-600 mt-1">Balance Sheet, Income Statement, Cash Flow</p>
        </div>
        <button className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg flex items-center gap-2">
          <Download className="w-4 h-4" />
          Export PDF
        </button>
      </div>

      {/* Controls */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-col md:flex-row gap-4 items-end">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Report Type</label>
            <select
              value={reportType}
              onChange={e => setReportType(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="balance-sheet">Balance Sheet (Statement of Financial Position)</option>
              <option value="income-statement">Income Statement (P&L)</option>
              <option value="cash-flow">Cash Flow Statement</option>
              <option value="trial-balance">Trial Balance</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">As of Date</label>
            <input
              type="date"
              value={asOfDate}
              onChange={e => setAsOfDate(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
      </div>

      {/* Report Content */}
      {loading ? (
        <div className="text-center py-12">
          <div className="animate-spin inline-block w-8 h-8 border-4 border-blue-200 border-t-blue-600 rounded-full"></div>
          <p className="mt-4 text-gray-600">Loading financial statement...</p>
        </div>
      ) : (
        <>
          {reportType === 'balance-sheet' && balanceSheet && renderBalanceSheet()}
          {reportType === 'income-statement' && (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <p className="text-center text-gray-600">Income Statement report coming soon</p>
            </div>
          )}
          {reportType === 'cash-flow' && (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <p className="text-center text-gray-600">Cash Flow Statement report coming soon</p>
            </div>
          )}
          {reportType === 'trial-balance' && (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <p className="text-center text-gray-600">Trial Balance report coming soon</p>
            </div>
          )}
        </>
      )}
    </div>
  );
}
