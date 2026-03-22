// components/BranchManagerDashboard.tsx
// Specialized dashboard view for Branch Manager role

import React from 'react';
import { PermissionGuard } from './PermissionGuard';
import StatCard from './StatCard';
import { Users, CheckCircle, TrendingUp, AlertTriangle, Zap, DollarSign } from 'lucide-react';

interface BranchManagerDashboardProps {
  metrics: any[];
  branchName?: string;
  staffCount?: number;
  approvalsNeeded?: number;
  portfolioGrowth?: number;
}

/**
 * Branch Manager-specific dashboard showing approvals and branch oversight
 */
export const BranchManagerDashboard: React.FC<BranchManagerDashboardProps> = ({
  metrics,
  branchName = 'Main Branch',
  staffCount = 0,
  approvalsNeeded = 0,
  portfolioGrowth = 0
}) => {
  return (
    <PermissionGuard permission="LOAN_APPROVE" fallback={<div>Access denied</div>}>
      <div className="space-y-6">
        {/* Branch Overview Stats */}
        <div className="bg-gradient-to-r from-purple-50 to-blue-50 border border-purple-200 rounded-lg p-4 mb-6">
          <h2 className="text-xl font-bold text-gray-900">{branchName}</h2>
          <p className="text-sm text-gray-600">Branch Manager Dashboard</p>
        </div>

        {/* Key Metrics - Branch Manager Focus */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            metric={{
              label: 'Staff Members',
              value: staffCount,
              percentage: '+0%',
              trend: 'neutral'
            }}
            icon={<Users size={24} />}
            color="blue"
          />
          <StatCard
            metric={{
              label: 'Pending Approvals',
              value: approvalsNeeded,
              percentage: '+12%',
              trend: 'up'
            }}
            icon={<CheckCircle size={24} />}
            color="orange"
          />
          <StatCard
            metric={{
              label: 'Portfolio Growth',
              value: portfolioGrowth,
              percentage: '+1.2%',
              trend: 'up'
            }}
            icon={<TrendingUp size={24} />}
            color="green"
          />
          <StatCard
            metric={{
              label: 'Risk Alerts',
              value: 2,
              percentage: '+0%',
              trend: 'neutral'
            }}
            icon={<AlertTriangle size={24} />}
            color="red"
          />
        </div>

        {/* Approval Management */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <CheckCircle size={18} className="text-purple-600" />
              Pending Approvals
            </h3>
            <div className="space-y-3">
              {approvalsNeeded === 0 ? (
                <div className="p-4 bg-green-50 border border-green-200 rounded-lg text-center">
                  <p className="text-green-800 font-medium">All requests approved!</p>
                </div>
              ) : (
                <>
                  <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg hover:bg-amber-100 cursor-pointer transition-colors">
                    <div className="flex justify-between items-center">
                      <div>
                        <p className="font-medium text-gray-900">Loan Request - Acct #001234</p>
                        <p className="text-xs text-gray-600 mt-1">Amount: ₦2,500,000 | Duration: 24 months</p>
                      </div>
                      <button className="px-3 py-1 bg-blue-500 hover:bg-blue-600 text-white rounded text-xs font-medium">
                        Review
                      </button>
                    </div>
                  </div>
                  <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg hover:bg-amber-100 cursor-pointer transition-colors">
                    <div className="flex justify-between items-center">
                      <div>
                        <p className="font-medium text-gray-900">Overdraft Request - Acct #005678</p>
                        <p className="text-xs text-gray-600 mt-1">Amount: ₦500,000 | Reason: Working Capital</p>
                      </div>
                      <button className="px-3 py-1 bg-blue-500 hover:bg-blue-600 text-white rounded text-xs font-medium">
                        Review
                      </button>
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>

          {/* Branch Performance */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <Zap size={18} className="text-blue-600" />
              Branch Performance
            </h3>
            <div className="space-y-4">
              <div>
                <div className="flex justify-between items-center mb-2">
                  <span className="text-sm font-medium text-gray-700">Loan Portfolio</span>
                  <span className="text-sm text-gray-600">85%</span>
                </div>
                <div className="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
                  <div className="h-full bg-blue-500" style={{ width: '85%' }}></div>
                </div>
              </div>
              <div>
                <div className="flex justify-between items-center mb-2">
                  <span className="text-sm font-medium text-gray-700">Deposits Target</span>
                  <span className="text-sm text-gray-600">72%</span>
                </div>
                <div className="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
                  <div className="h-full bg-green-500" style={{ width: '72%' }}></div>
                </div>
              </div>
              <div>
                <div className="flex justify-between items-center mb-2">
                  <span className="text-sm font-medium text-gray-700">Operational Efficiency</span>
                  <span className="text-sm text-gray-600">91%</span>
                </div>
                <div className="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
                  <div className="h-full bg-purple-500" style={{ width: '91%' }}></div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Staff Management */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">Staff Overview</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="p-4 bg-blue-50 rounded-lg border border-blue-200">
              <p className="text-xs text-blue-700 font-semibold mb-2">TELLERS</p>
              <p className="text-2xl font-bold text-blue-900">4</p>
              <p className="text-xs text-blue-600 mt-2">Active</p>
            </div>
            <div className="p-4 bg-purple-50 rounded-lg border border-purple-200">
              <p className="text-xs text-purple-700 font-semibold mb-2">MANAGERS</p>
              <p className="text-2xl font-bold text-purple-900">2</p>
              <p className="text-xs text-purple-600 mt-2">Supervisory</p>
            </div>
            <div className="p-4 bg-green-50 rounded-lg border border-green-200">
              <p className="text-xs text-green-700 font-semibold mb-2">LOAN OFFICERS</p>
              <p className="text-2xl font-bold text-green-900">3</p>
              <p className="text-xs text-green-600 mt-2">Active</p>
            </div>
            <div className="p-4 bg-orange-50 rounded-lg border border-orange-200">
              <p className="text-xs text-orange-700 font-semibold mb-2">COMPLIANCE</p>
              <p className="text-2xl font-bold text-orange-900">1</p>
              <p className="text-xs text-orange-600 mt-2">Officer</p>
            </div>
          </div>
        </div>

        {/* Branch Manager Features */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 flex items-start gap-3">
          <Zap className="text-blue-600 flex-shrink-0 mt-0.5" size={18} />
          <div>
            <h4 className="font-semibold text-blue-900">Branch Manager Features</h4>
            <p className="text-sm text-blue-800 mt-1">
              You have access to loan approvals, staff management, and branch performance monitoring.
              Use the Approvals section for maker-checker workflows and access detailed staff audit trails.
            </p>
          </div>
        </div>
      </div>
    </PermissionGuard>
  );
};

export default BranchManagerDashboard;
