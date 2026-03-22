import React, { useState } from 'react';
import { BarChart3, TrendingUp, DollarSign, Settings } from 'lucide-react';
import FxRateManagement from './FxRateManagement';
import TreasuryPositionMonitor from './TreasuryPositionMonitor';
import FxTradingDesk from './FxTradingDesk';
import InvestmentPortfolio from './InvestmentPortfolio';
import RiskDashboard from './RiskDashboard';

type TreasuryTab = 'fxrates' | 'position' | 'trading' | 'investments' | 'risk';

interface TabConfig {
  id: TreasuryTab;
  label: string;
  icon: React.ReactNode;
  component: React.ReactNode;
}

export default function TreasuryManagementHub() {
  const [activeTab, setActiveTab] = useState<TreasuryTab>('position');

  const tabs: TabConfig[] = [
    {
      id: 'position',
      label: 'Cash Position',
      icon: <DollarSign className="w-5 h-5" />,
      component: <TreasuryPositionMonitor />
    },
    {
      id: 'fxrates',
      label: 'FX Rates',
      icon: <TrendingUp className="w-5 h-5" />,
      component: <FxRateManagement />
    },
    {
      id: 'trading',
      label: 'FX Trading',
      icon: <BarChart3 className="w-5 h-5" />,
      component: <FxTradingDesk />
    },
    {
      id: 'investments',
      label: 'Investments',
      icon: <TrendingUp className="w-5 h-5" />,
      component: <InvestmentPortfolio />
    },
    {
      id: 'risk',
      label: 'Risk Analytics',
      icon: <Settings className="w-5 h-5" />,
      component: <RiskDashboard />
    }
  ];

  return (
    <div className="h-full bg-gray-50 overflow-auto">
      {/* Header */}
      <div className="bg-white border-b border-gray-200 sticky top-0 z-40">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <h1 className="text-3xl font-bold text-gray-900">Treasury Management</h1>
          <p className="text-gray-600 mt-1">Comprehensive banking treasury operations hub</p>
        </div>

        {/* Tab Navigation */}
        <nav className="border-t border-gray-200">
          <div className="max-w-7xl mx-auto px-6 flex gap-1">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`px-4 py-4 border-b-2 font-medium text-sm flex items-center gap-2 transition ${
                  activeTab === tab.id
                    ? 'border-blue-600 text-blue-600'
                    : 'border-transparent text-gray-600 hover:text-gray-900 hover:border-gray-300'
                }`}
              >
                {tab.icon}
                {tab.label}
              </button>
            ))}
          </div>
        </nav>
      </div>

      {/* Tab Content */}
      <div className="max-w-7xl mx-auto px-6 py-8">
        {tabs.find((tab) => tab.id === activeTab)?.component}
      </div>

      {/* Footer */}
      <div className="bg-white border-t border-gray-200 mt-12">
        <div className="max-w-7xl mx-auto px-6 py-6">
          <div className="grid grid-cols-3 gap-8">
            <div>
              <h4 className="font-semibold text-gray-900 mb-2">Quick Tips</h4>
              <ul className="text-sm text-gray-600 space-y-1">
                <li>• Monitor cash positions daily</li>
                <li>• Review pending trades for approval</li>
                <li>• Track investment maturity dates</li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold text-gray-900 mb-2">Key Metrics</h4>
              <ul className="text-sm text-gray-600 space-y-1">
                <li>• Risk limits and breaches</li>
                <li>• Liquidity ratios (LCR/NSFR)</li>
                <li>• FX exposure by currency</li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold text-gray-900 mb-2">Integration</h4>
              <ul className="text-sm text-gray-600 space-y-1">
                <li>• Bank of Ghana rates daily</li>
                <li>• Automated accrual calculations</li>
                <li>• Real-time risk monitoring</li>
              </ul>
            </div>
          </div>
          <div className="mt-6 pt-6 border-t border-gray-200">
            <p className="text-sm text-gray-500">
              Treasury Management System v2.0 | Last updated: {new Date().toLocaleString()}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
