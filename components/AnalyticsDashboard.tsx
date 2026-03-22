import React, { useState } from 'react';
import { BarChart3, DollarSign, TrendingUp, PieChart, Calendar, Download } from 'lucide-react';

interface AnalyticsTab {
  id: string;
  label: string;
  icon: React.ReactNode;
}

const analyticsTabs: AnalyticsTab[] = [
  { id: 'customer-seg', label: 'Customer Segmentation', icon: <Users className="w-5 h-5" /> },
  { id: 'transactions', label: 'Transaction Trends', icon: <TrendingUp className="w-5 h-5" /> },
  { id: 'products', label: 'Product Analytics', icon: <PieChart className="w-5 h-5" /> },
  { id: 'channels', label: 'Channel Analytics', icon: <BarChart3 className="w-5 h-5" /> },
  { id: 'staff', label: 'Staff Productivity', icon: <TrendingUp className="w-5 h-5" /> }
];

import { Users } from 'lucide-react';

export default function AnalyticsDashboard() {
  const [activeTab, setActiveTab] = useState('customer-seg');
  const [dateRange, setDateRange] = useState({
    start: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    end: new Date().toISOString().split('T')[0]
  });
  const [loading, setLoading] = useState(false);

  const handleDateChange = (field: 'start' | 'end', value: string) => {
    setDateRange(prev => ({ ...prev, [field]: value }));
  };

  const renderCustomerSegmentation = () => (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {[
          { name: 'Inactive', count: 145, balance: '2.5M', color: 'bg-gray-100' },
          { name: 'Retail', count: 1203, balance: '45.2M', color: 'bg-blue-100' },
          { name: 'Mid-Market', count: 287, balance: '120.5M', color: 'bg-purple-100' },
          { name: 'VIP', count: 42, balance: '250.8M', color: 'bg-green-100' }
        ].map(segment => (
          <div key={segment.name} className={`${segment.color} rounded-lg p-4`}>
            <p className="text-sm text-gray-600 font-medium">{segment.name}</p>
            <p className="text-2xl font-bold text-gray-900 mt-2">{segment.count}</p>
            <p className="text-xs text-gray-600 mt-1">Balance: {segment.balance}</p>
            <div className="mt-3 w-full bg-gray-200 rounded-full h-2">
              <div className="bg-blue-600 h-2 rounded-full" style={{ width: '65%' }}></div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderTransactionTrends = () => (
    <div className="space-y-4">
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="font-semibold text-gray-900 mb-4">Daily Transaction Volume</h3>
        <div className="space-y-3">
          {[1, 2, 3, 4, 5].map(day => (
            <div key={day} className="flex items-end gap-3">
              <span className="text-sm text-gray-700 w-12">Day {day}</span>
              <div className="flex-1 bg-blue-100 h-16 rounded-lg relative">
                <div className="absolute top-0 left-0 right-0 bottom-0 bg-blue-600 rounded-lg" 
                     style={{ height: `${Math.random() * 100}%` }}></div>
              </div>
              <span className="text-sm text-gray-600 w-20">2.5K txn</span>
            </div>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-blue-50 rounded-lg p-4">
          <p className="text-sm text-gray-600">Total Transactions</p>
          <p className="text-2xl font-bold text-blue-600 mt-2">12,450</p>
        </div>
        <div className="bg-green-50 rounded-lg p-4">
          <p className="text-sm text-gray-600">Total Volume</p>
          <p className="text-2xl font-bold text-green-600 mt-2">₵45.2M</p>
        </div>
        <div className="bg-purple-50 rounded-lg p-4">
          <p className="text-sm text-gray-600">Avg Daily Volume</p>
          <p className="text-2xl font-bold text-purple-600 mt-2">₵1.5M</p>
        </div>
        <div className="bg-orange-50 rounded-lg p-4">
          <p className="text-sm text-gray-600">Peak Volume</p>
          <p className="text-2xl font-bold text-orange-600 mt-2">₵4.2M</p>
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
            <BarChart3 className="w-6 h-6 text-purple-600" />
            Analytics & Insights
          </h2>
          <p className="text-gray-600 mt-1">Business intelligence and customer analytics</p>
        </div>
        <button className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg flex items-center gap-2">
          <Download className="w-4 h-4" />
          Export
        </button>
      </div>

      {/* Date Range Selector */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-col md:flex-row gap-4 items-end">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
            <input
              type="date"
              value={dateRange.start}
              onChange={e => handleDateChange('start', e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">End Date</label>
            <input
              type="date"
              value={dateRange.end}
              onChange={e => handleDateChange('end', e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <button className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg">
            Apply
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div className="flex overflow-x-auto border-b border-gray-200">
          {analyticsTabs.map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2 px-4 py-3 whitespace-nowrap font-medium transition-colors ${
                activeTab === tab.id
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-gray-700 hover:text-gray-900'
              }`}
            >
              {tab.icon}
              {tab.label}
            </button>
          ))}
        </div>

        {/* Content */}
        <div className="p-6">
          {activeTab === 'customer-seg' && renderCustomerSegmentation()}
          {activeTab === 'transactions' && renderTransactionTrends()}
          {activeTab === 'products' && <div className="text-center text-gray-600 py-12">Product analytics coming soon</div>}
          {activeTab === 'channels' && <div className="text-center text-gray-600 py-12">Channel analytics coming soon</div>}
          {activeTab === 'staff' && <div className="text-center text-gray-600 py-12">Staff productivity analytics coming soon</div>}
        </div>
      </div>
    </div>
  );
}
