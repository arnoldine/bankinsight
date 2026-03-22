// components/TellerDashboard.tsx
// Specialized dashboard view for Teller role

import React from 'react';
import { PermissionGuard } from './PermissionGuard';
import StatCard from './StatCard';
import { Landmark, BadgeCheck, AlertCircle, TrendingUp, Clock, DollarSign } from 'lucide-react';

interface TellerDashboardProps {
  metrics: any[];
  accountsCount?: number;
  todayTransactions?: number;
  pendingItems?: number;
}

/**
 * Teller-specific dashboard showing only transaction and account operations
 */
export const TellerDashboard: React.FC<TellerDashboardProps> = ({
  metrics,
  accountsCount = 0,
  todayTransactions = 0,
  pendingItems = 0
}) => {
  return (
    <PermissionGuard permission="TELLER_POST" fallback={<div>Access denied</div>}>
      <div className="space-y-6">
        {/* Quick Stats - Teller Operations Only */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            metric={{
              label: 'Active Accounts',
              value: accountsCount,
              percentage: '+2.5%',
              trend: 'neutral'
            }}
            icon={<Landmark size={24} />}
            color="blue"
          />
          <StatCard
            metric={{
              label: "Today's Transactions",
              value: todayTransactions,
              percentage: '+15%',
              trend: 'up'
            }}
            icon={<TrendingUp size={24} />}
            color="green"
          />
          <StatCard
            metric={{
              label: 'Pending GL Post',
              value: pendingItems,
              percentage: '+8%',
              trend: 'neutral'
            }}
            icon={<Clock size={24} />}
            color="orange"
          />
          <StatCard
            metric={{
              label: 'Suspense Account',
              value: 0,
              percentage: '0%',
              trend: 'neutral'
            }}
            icon={<AlertCircle size={24} />}
            color="red"
          />
        </div>

        {/* Teller Operations Quick Links */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <Landmark size={18} className="text-blue-600" />
              Quick Operations
            </h3>
            <div className="space-y-3">
              <button className="w-full p-3 text-left bg-blue-50 hover:bg-blue-100 text-blue-900 rounded-lg border border-blue-200 transition-colors flex items-center justify-between">
                <span className="font-medium">New Deposit</span>
                <span className="text-xs bg-blue-200 px-2 py-1 rounded">Ctrl+D</span>
              </button>
              <button className="w-full p-3 text-left bg-green-50 hover:bg-green-100 text-green-900 rounded-lg border border-green-200 transition-colors flex items-center justify-between">
                <span className="font-medium">Cash Withdrawal</span>
                <span className="text-xs bg-green-200 px-2 py-1 rounded">Ctrl+W</span>
              </button>
              <button className="w-full p-3 text-left bg-purple-50 hover:bg-purple-100 text-purple-900 rounded-lg border border-purple-200 transition-colors flex items-center justify-between">
                <span className="font-medium">Fund Transfer</span>
                <span className="text-xs bg-purple-200 px-2 py-1 rounded">Ctrl+T</span>
              </button>
              <button className="w-full p-3 text-left bg-orange-50 hover:bg-orange-100 text-orange-900 rounded-lg border border-orange-200 transition-colors flex items-center justify-between">
                <span className="font-medium">Post GL Entry</span>
                <span className="text-xs bg-orange-200 px-2 py-1 rounded">Ctrl+G</span>
              </button>
            </div>
          </div>

          {/* Daily Reconciliation */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <BadgeCheck size={18} className="text-green-600" />
              Daily Reconciliation
            </h3>
            <div className="space-y-3">
              <div className="p-3 bg-gray-50 rounded-lg border border-gray-200 flex justify-between items-center">
                <span className="text-sm text-gray-700">Cash Position</span>
                <span className="font-medium text-gray-900">Due 5:00 PM</span>
              </div>
              <div className="p-3 bg-gray-50 rounded-lg border border-gray-200 flex justify-between items-center">
                <span className="text-sm text-gray-700">Cheque Book Balance</span>
                <span className="font-medium text-gray-900">Due 5:15 PM</span>
              </div>
              <div className="p-3 bg-gray-50 rounded-lg border border-gray-200 flex justify-between items-center">
                <span className="text-sm text-gray-700">Vault Count</span>
                <span className="font-medium text-gray-900">Due 5:30 PM</span>
              </div>
              <button className="w-full mt-2 p-2 bg-green-500 hover:bg-green-600 text-white rounded-lg font-medium transition-colors">
                Submit Reconciliation
              </button>
            </div>
          </div>
        </div>

        {/* Restricted Features Warning */}
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 flex items-start gap-3">
          <AlertCircle className="text-amber-600 flex-shrink-0 mt-0.5" size={18} />
          <div>
            <h4 className="font-semibold text-amber-900">Limited Access</h4>
            <p className="text-sm text-amber-800 mt-1">
              As a Teller, you have access to transaction posting and GL operations only.
              Administrative and reporting features require elevated permissions.
              Contact your Branch Manager for additional access.
            </p>
          </div>
        </div>
      </div>
    </PermissionGuard>
  );
};

export default TellerDashboard;
