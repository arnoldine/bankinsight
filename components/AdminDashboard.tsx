// components/AdminDashboard.tsx
// Specialized dashboard view for Admin role

import React from 'react';
import { PermissionGuard } from './PermissionGuard';
import StatCard from './StatCard';
import { PieChart, Globe, Shield, TrendingUp, AlertTriangle, Database } from 'lucide-react';

interface AdminDashboardProps {
  metrics: any[];
  totalUsers?: number;
  activeRoles?: number;
  systemHealth?: number;
  securityAlerts?: number;
}

/**
 * Admin-specific dashboard showing system administration and configuration
 */
export const AdminDashboard: React.FC<AdminDashboardProps> = ({
  metrics,
  totalUsers = 0,
  activeRoles = 0,
  systemHealth = 100,
  securityAlerts = 0
}) => {
  return (
    <PermissionGuard permission="SYSTEM_ADMIN" fallback={<div>Access denied - Admin only</div>}>
      <div className="space-y-6">
        {/* Admin Header */}
        <div className="bg-gradient-to-r from-red-50 to-orange-50 border border-red-200 rounded-lg p-4 mb-6">
          <h2 className="text-xl font-bold text-gray-900">System Administration</h2>
          <p className="text-sm text-gray-600 flex items-center gap-2 mt-1">
            <Shield size={14} className="text-red-600" />
            Full system access and configuration
          </p>
        </div>

        {/* System Metrics */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            metric={{
              label: 'Total Users',
              value: totalUsers,
              percentage: '+12%',
              trend: 'up'
            }}
            icon={<Globe size={24} />}
            color="blue"
          />
          <StatCard
            metric={{
              label: 'Active Roles',
              value: activeRoles,
              percentage: '+0%',
              trend: 'neutral'
            }}
            icon={<Shield size={24} />}
            color="purple"
          />
          <StatCard
            metric={{
              label: 'System Health',
              value: systemHealth,
              percentage: '+2%',
              trend: 'up'
            }}
            icon={<TrendingUp size={24} />}
            color="green"
          />
          <StatCard
            metric={{
              label: 'Security Alerts',
              value: securityAlerts,
              percentage: '0%',
              trend: 'neutral'
            }}
            icon={<AlertTriangle size={24} />}
            color="red"
          />
        </div>

        {/* Administrative Panels */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* User & Role Management */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <Shield size={18} className="text-blue-600" />
              User & Role Management
            </h3>
            <div className="space-y-3">
              <a href="#users" className="block p-3 bg-blue-50 hover:bg-blue-100 text-blue-900 rounded-lg border border-blue-200 transition-colors">
                <div className="font-medium flex justify-between items-center">
                  <span>Manage Users</span>
                  <span className="text-xs bg-blue-200 px-2 py-1 rounded">5</span>
                </div>
                <p className="text-xs text-blue-700 mt-1">Create, edit, delete users and assign roles</p>
              </a>
              <a href="#roles" className="block p-3 bg-purple-50 hover:bg-purple-100 text-purple-900 rounded-lg border border-purple-200 transition-colors">
                <div className="font-medium flex justify-between items-center">
                  <span>Configure Roles</span>
                  <span className="text-xs bg-purple-200 px-2 py-1 rounded">{activeRoles}</span>
                </div>
                <p className="text-xs text-purple-700 mt-1">Define roles and assign permissions</p>
              </a>
              <a href="#permissions" className="block p-3 bg-green-50 hover:bg-green-100 text-green-900 rounded-lg border border-green-200 transition-colors">
                <div className="font-medium flex justify-between items-center">
                  <span>Permission Mapping</span>
                  <span className="text-xs bg-green-200 px-2 py-1 rounded">62</span>
                </div>
                <p className="text-xs text-green-700 mt-1">Review and modify system permissions</p>
              </a>
            </div>
          </div>

          {/* System Configuration */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <Database size={18} className="text-orange-600" />
              System Configuration
            </h3>
            <div className="space-y-3">
              <a href="#config" className="block p-3 bg-orange-50 hover:bg-orange-100 text-orange-900 rounded-lg border border-orange-200 transition-colors">
                <div className="font-medium flex justify-between items-center">
                  <span>System Settings</span>
                  <span className="text-xs bg-orange-200 px-2 py-1 rounded">Active</span>
                </div>
                <p className="text-xs text-orange-700 mt-1">Database, authentication, and business parameters</p>
              </a>
              <a href="#security" className="block p-3 bg-red-50 hover:bg-red-100 text-red-900 rounded-lg border border-red-200 transition-colors">
                <div className="font-medium flex justify-between items-center">
                  <span>Security Settings</span>
                  <span className="text-xs bg-red-200 px-2 py-1 rounded">Configured</span>
                </div>
                <p className="text-xs text-red-700 mt-1">IP whitelist, MFA, and audit logging</p>
              </a>
              <a href="#backup" className="block p-3 bg-teal-50 hover:bg-teal-100 text-teal-900 rounded-lg border border-teal-200 transition-colors">
                <div className="font-medium flex justify-between items-center">
                  <span>Backup & Recovery</span>
                  <span className="text-xs bg-teal-200 px-2 py-1 rounded">Daily</span>
                </div>
                <p className="text-xs text-teal-700 mt-1">Database backups and disaster recovery</p>
              </a>
            </div>
          </div>
        </div>

        {/* Security & Compliance */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4">Security Posture</h3>
            <div className="space-y-4">
              <div className="flex items-center justify-between p-3 bg-green-50 rounded-lg border border-green-200">
                <span className="font-medium text-gray-700">API Security</span>
                <span className="text-green-600 font-semibold">✓ Enabled</span>
              </div>
              <div className="flex items-center justify-between p-3 bg-green-50 rounded-lg border border-green-200">
                <span className="font-medium text-gray-700">Audit Logging</span>
                <span className="text-green-600 font-semibold">✓ Active</span>
              </div>
              <div className="flex items-center justify-between p-3 bg-green-50 rounded-lg border border-green-200">
                <span className="font-medium text-gray-700">IP Whitelist</span>
                <span className="text-green-600 font-semibold">✓ Configured</span>
              </div>
              <div className="flex items-center justify-between p-3 bg-amber-50 rounded-lg border border-amber-200">
                <span className="font-medium text-gray-700">MFA Enforcement</span>
                <span className="text-amber-600 font-semibold">⚠ Optional</span>
              </div>
            </div>
          </div>

          {/* Recent Admin Activity */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h3 className="font-semibold text-gray-900 mb-4">Recent Admin Activity</h3>
            <div className="space-y-3 text-sm">
              <div className="p-2 bg-blue-50 rounded border-l-2 border-blue-600">
                <p className="font-medium text-gray-800">User john_doe role updated to TELLER</p>
                <p className="text-gray-600 text-xs mt-1">2 minutes ago</p>
              </div>
              <div className="p-2 bg-purple-50 rounded border-l-2 border-purple-600">
                <p className="font-medium text-gray-800">New role COMPLIANCE_OFFICER created</p>
                <p className="text-gray-600 text-xs mt-1">1 hour ago</p>
              </div>
              <div className="p-2 bg-green-50 rounded border-l-2 border-green-600">
                <p className="font-medium text-gray-800">System config updated: AML threshold</p>
                <p className="text-gray-600 text-xs mt-1">3 hours ago</p>
              </div>
              <div className="p-2 bg-orange-50 rounded border-l-2 border-orange-600">
                <p className="font-medium text-gray-800">Backup completed successfully</p>
                <p className="text-gray-600 text-xs mt-1">Yesterday at 2:00 AM</p>
              </div>
            </div>
          </div>
        </div>

        {/* Admin Privileges Info */}
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
          <Shield className="text-red-600 flex-shrink-0 mt-0.5" size={18} />
          <div>
            <h4 className="font-semibold text-red-900">Administrator Privileges</h4>
            <p className="text-sm text-red-800 mt-1">
              You have full system access and can manage all users, roles, and configurations.
              All administrative actions are logged for audit purposes. Exercise permissions responsibly.
            </p>
          </div>
        </div>
      </div>
    </PermissionGuard>
  );
};

export default AdminDashboard;
