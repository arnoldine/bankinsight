import React, { useState } from 'react';
import { FileText, BarChart3, Zap, TrendingUp } from 'lucide-react';
import ReportCatalog from './ReportCatalog';
import AnalyticsDashboard from './AnalyticsDashboard';
import FinancialStatements from './FinancialStatements';
import RegulatoryReports from './RegulatoryReports';

interface ReportingTab {
  id: string;
  label: string;
  icon: React.ReactNode;
  component: React.ReactNode;
}

export default function ReportingHub() {
  const [activeTab, setActiveTab] = useState('catalog');

  const tabs: ReportingTab[] = [
    {
      id: 'catalog',
      label: 'Report Catalog',
      icon: <FileText className="w-5 h-5" />,
      component: <ReportCatalog />
    },
    {
      id: 'regulatory',
      label: 'Regulatory Reports',
      icon: <Zap className="w-5 h-5" />,
      component: <RegulatoryReports />
    },
    {
      id: 'financial',
      label: 'Financial Statements',
      icon: <BarChart3 className="w-5 h-5" />,
      component: <FinancialStatements />
    },
    {
      id: 'analytics',
      label: 'Analytics',
      icon: <TrendingUp className="w-5 h-5" />,
      component: <AnalyticsDashboard />
    }
  ];

  return (
    <div className="space-y-6">
      {/* Tabs */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div className="flex overflow-x-auto border-b border-gray-200">
          {tabs.map(tab => (
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
      </div>

      {/* Content */}
      <div>
        {tabs.find(tab => tab.id === activeTab)?.component}
      </div>

      {/* Footer Info */}
      <div className="bg-gradient-to-r from-blue-50 to-purple-50 rounded-lg border border-blue-200 p-6">
        <h3 className="font-semibold text-gray-900 mb-2">📊 Reporting System</h3>
        <ul className="text-sm text-gray-700 space-y-1">
          <li>✓ Real-time report generation and scheduling</li>
          <li>✓ Bank of Ghana compliance reporting (Daily Position, Monthly Returns 1-3, Prudential, Large Exposure)</li>
          <li>✓ Financial statements (Balance Sheet, Income Statement, Cash Flow, Trial Balance)</li>
          <li>✓ Advanced analytics (Customer segmentation, Transaction trends, Product analytics, Channel analytics, Staff productivity)</li>
          <li>✓ Export to multiple formats (Excel, PDF, CSV)</li>
          <li>✓ Automated report scheduling and email delivery</li>
        </ul>
      </div>
    </div>
  );
}
