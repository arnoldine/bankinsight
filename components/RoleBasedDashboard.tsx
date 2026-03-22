// components/RoleBasedDashboard.tsx
// Component that renders role-specific dashboard views

import React from 'react';
import { JWTPayload, decodeJWT, getPermissionsFromToken } from '../lib/jwtUtils';
import { Landmark, DollarSign, FileText, Users, CheckCircle, BarChart3, Lock, ShieldAlert } from 'lucide-react';

interface RoleBasedDashboardProps {
  token?: string | null;
  children?: React.ReactNode;
}

/**
 * Displays role-specific dashboard content and welcome message
 */
export const RoleBasedDashboard: React.FC<RoleBasedDashboardProps> = ({ token, children }) => {
  const authToken = token || (typeof window !== 'undefined' ? localStorage.getItem('bankinsight_token') : null);
  
  if (!authToken) {
    return null;
  }

  const payload = decodeJWT(authToken);
  const permissions = getPermissionsFromToken(authToken);
  
  if (!payload || !permissions) {
    return null;
  }

  const roleId = payload.role_id;
  const email = payload.email;

  // Determine role type and show role-specific content
  const getRoleInfo = () => {
    if (permissions.includes('SYSTEM_ADMIN')) {
      return {
        role: 'Administrator',
        description: 'Full system access - manage users, roles, configuration',
        icon: Users,
        color: 'from-blue-600 to-blue-700',
        features: [
          'User & Role Management',
          'System Configuration',
          'All Reports & Analytics',
          'Security & Audit Logs',
          'Approval Management'
        ]
      };
    }

    if (permissions.includes('LOAN_APPROVE')) {
      return {
        role: 'Branch Manager',
        description: 'Branch oversight and loan approvals',
        icon: CheckCircle,
        color: 'from-purple-600 to-purple-700',
        features: [
          'Loan Approvals',
          'Account Management',
          'Client View',
          'Approval Workflows',
          'Branch Reports'
        ]
      };
    }

    if (permissions.includes('POST_TRANSACTION')) {
      return {
        role: 'Teller',
        description: 'Front desk cash and transaction operations',
        icon: Landmark,
        color: 'from-green-600 to-green-700',
        features: [
          'Post Deposits/Withdrawals',
          'View Accounts',
          'Process Transactions',
          'GL Posting',
          'Daily Reconciliation'
        ]
      };
    }

    if (permissions.includes('TELLER_TRANSACTION')) {
      return {
        role: 'Teller',
        description: 'Front desk operations and transactions',
        icon: Landmark,
        color: 'from-green-600 to-green-700',
        features: [
          'Customer Transactions',
          'Account Views',
          'Cash Handling',
          'GL Posting',
          'Daily Reports'
        ]
      };
    }

    if (permissions.includes('REPORT_VIEW')) {
      return {
        role: 'Analyst',
        description: 'Reporting and analytics access',
        icon: BarChart3,
        color: 'from-orange-600 to-orange-700',
        features: [
          'Financial Reports',
          'Risk Analytics',
          'Regulatory Reports',
          'Data Analysis',
          'Report Scheduling'
        ]
      };
    }

    return {
      role: 'User',
      description: `Limited access based on permissions`,
      icon: Lock,
      color: 'from-gray-600 to-gray-700',
      features: [
        `${permissions.length} total permissions`,
        'Role-based features',
        'Permission-gated access',
        'Audit logging',
        'Security monitoring'
      ]
    };
  };

  const roleInfo = getRoleInfo();
  const RoleIcon = roleInfo.icon;

  return (
    <div className="bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 rounded-lg p-6 mb-6 border border-slate-200 dark:border-slate-700">
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-4">
          <div className={`bg-gradient-to-br ${roleInfo.color} p-3 rounded-lg text-white`}>
            <RoleIcon size={24} />
          </div>
          <div>
            <h2 className="text-2xl font-bold text-slate-900 dark:text-white">
              {roleInfo.role}
            </h2>
            <p className="text-slate-600 dark:text-slate-400 text-sm">
              {email} • {roleInfo.description}
            </p>
          </div>
        </div>
        <div className="text-right">
          <div className="text-3xl font-bold text-slate-900 dark:text-white">
            {permissions.length}
          </div>
          <div className="text-xs text-slate-600 dark:text-slate-400">
            Active Permissions
          </div>
        </div>
      </div>

      {/* Feature Grid */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-3 mt-4">
        {roleInfo.features.map((feature, idx) => (
          <div key={idx} className="flex items-center gap-2 bg-white dark:bg-slate-800 p-3 rounded-lg border border-slate-200 dark:border-slate-700 shadow-sm">
            <div className={`w-2 h-2 rounded-full bg-gradient-to-r ${roleInfo.color}`} />
            <span className="text-xs font-medium text-slate-700 dark:text-slate-300">
              {feature}
            </span>
          </div>
        ))}
      </div>

      {/* Permission List */}
      <div className="mt-4 pt-4 border-t border-slate-200 dark:border-slate-700">
        <p className="text-xs font-semibold text-slate-700 dark:text-slate-300 mb-3 flex items-center gap-2">
          <ShieldAlert size={14} />
          Your Permissions
        </p>
        <div className="grid grid-cols-3 md:grid-cols-6 gap-2">
          {permissions.slice(0, 12).map((perm) => (
            <span key={perm} className="text-xs bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300 px-2 py-1 rounded font-mono truncate">
              {perm}
            </span>
          ))}
          {permissions.length > 12 && (
            <span className="text-xs bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300 px-2 py-1 rounded font-mono">
              +{permissions.length - 12} more
            </span>
          )}
        </div>
      </div>

      {children}
    </div>
  );
};

export default RoleBasedDashboard;
