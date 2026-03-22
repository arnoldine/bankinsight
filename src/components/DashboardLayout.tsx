import React, { useState, useMemo } from 'react';
import { User } from '../services/authService';
import {
  Menu,
  X,
  LogOut,
  LayoutDashboard,
  TrendingUp,
  FileText,
  Briefcase,
  Users,
  Settings as SettingsIcon,
  AlertCircle,
  RefreshCw,
  Archive,
  DollarSign,
  Settings2,
  Landmark,
  ArrowRightLeft,
  FileCheck,
  Calculator,
  Headphones,
  Shield,
  ClipboardList,
  CheckCircle,
  Package,
  Moon,
  History,
  CheckSquare,
} from 'lucide-react';
import ReportingHub from './ReportingHub';
import TreasuryManagementHub from './TreasuryManagementHub';
import VaultManagementHub from './VaultManagementHub';
import LoanManagementHub from './LoanManagementHub';
import ExtensibilityTestPage from './DynamicForms/ExtensibilityTestPage';
import Settings from '../../components/Settings';
import TellerTerminal from '../../components/TellerTerminal';
import TransactionExplorer from '../../components/TransactionExplorer';
import StatementVerification from '../../components/StatementVerification';
import AccountingEngine from '../../components/AccountingEngine';
import OperationsHub from '../../components/OperationsHub';
import LoanOfficerScreen from '../../components/LoanOfficerScreen';
import AccountantScreen from '../../components/AccountantScreen';
import CustomerServiceScreen from '../../components/CustomerServiceScreen';
import ComplianceOfficerScreen from '../../components/ComplianceOfficerScreen';
import ApprovalInbox from '../../components/ApprovalInbox';
import ProductDesigner from '../../components/ProductDesigner';
import EodConsole from '../../components/EodConsole';
import AuditTrail from '../../components/AuditTrail';
import DevelopmentTasks from '../../components/DevelopmentTasks';
import GroupManager from '../../components/GroupManager';
import ClientManager from '../../components/ClientManager';
import { useTreasury, useReports } from '../hooks/useApi';
import { hasPermission } from '../../lib/jwtUtils';

interface DashboardLayoutProps {
  user: User;
  onLogout: () => void;
  error?: string | null;
  onErrorDismiss?: () => void;
}

export default function DashboardLayout({
  user,
  onLogout,
  error,
  onErrorDismiss,
}: DashboardLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [activeTab, setActiveTab] = useState('dashboard');

  // Get token for permission checks
  const token = localStorage.getItem('bankinsight_token') || '';

  // Define all menu items with permission requirements
  const allMenuItems = [
    { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard, permission: null },
    { id: 'clients', label: 'Clients', icon: Users, permission: 'ACCOUNT_READ' },
    { id: 'groups', label: 'Groups', icon: Users, permission: 'ACCOUNT_READ' },
    { id: 'loans', label: 'Loans', icon: DollarSign, permission: 'LOAN_READ' },
    { id: 'teller', label: 'Teller', icon: Landmark, permission: 'TELLER_POST' },
    { id: 'transactions', label: 'Transactions', icon: ArrowRightLeft, permission: 'ACCOUNT_READ' },
    { id: 'statements', label: 'Statements', icon: FileCheck, permission: 'ACCOUNT_READ' },
    { id: 'accounting', label: 'Accounting', icon: FileText, permission: 'GL_READ' },
    { id: 'vault', label: 'Vault', icon: Archive, permission: 'ACCOUNT_READ' },
    { id: 'treasury', label: 'Treasury', icon: TrendingUp, permission: 'ACCOUNT_READ' },
    { id: 'operations', label: 'Operations', icon: Settings2, permission: 'ACCOUNT_READ' },
    { id: 'reporting', label: 'Reports', icon: FileText, permission: 'REPORT_VIEW' },
    { id: 'loanofficer', label: 'Loan Officer', icon: ClipboardList, permission: 'LOAN_READ' },
    { id: 'accountant', label: 'Accountant', icon: Calculator, permission: 'GL_POST' },
    { id: 'customerservice', label: 'Customer Service', icon: Headphones, permission: 'ACCOUNT_READ' },
    { id: 'compliance', label: 'Compliance', icon: Shield, permission: 'AUDIT_READ' },
    { id: 'approvals', label: 'Approvals', icon: CheckCircle, permission: 'LOAN_APPROVE' },
    { id: 'products', label: 'Products', icon: Package, permission: 'SYSTEM_CONFIG' },
    { id: 'eod', label: 'End of Day', icon: Moon, permission: 'SYSTEM_CONFIG' },
    { id: 'extensibility', label: 'Extensibility Rules', icon: Settings2, permission: 'SYSTEM_CONFIG' },
    { id: 'settings', label: 'Settings', icon: SettingsIcon, permission: 'SYSTEM_CONFIG' },
    { id: 'audit', label: 'Audit Trail', icon: History, permission: 'SYSTEM_CONFIG' },
    { id: 'tasks', label: 'Dev Tasks', icon: CheckSquare, permission: null },
  ];

  // Filter menu items based on permissions
  const menuItems = useMemo(() => {
    return allMenuItems.filter(item => {
      if (!item.permission) return true; // No permission required
      return hasPermission(token, item.permission);
    });
  }, [token]);

  return (
    <div className="flex h-screen bg-gray-100 dark:bg-slate-900 text-gray-900 dark:text-white">
      {/* Sidebar */}
      <div
        className={`${sidebarOpen ? 'w-64' : 'w-20'
          } bg-white dark:bg-slate-800 border-r border-gray-200 dark:border-slate-700 transition-all duration-200 flex flex-col`}
      >
        {/* Logo */}
        <div className="p-4 border-b border-gray-200 dark:border-slate-700">
          <div className="flex items-center gap-3">
            <Briefcase className={`w-8 h-8 text-blue-400 ${!sidebarOpen && 'mx-auto'}`} />
            {sidebarOpen && <span className="font-bold text-lg">BankInsight</span>}
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 p-4 space-y-2">
          {menuItems.map((item) => {
            const Icon = item.icon;
            return (
              <button
                key={item.id}
                onClick={() => setActiveTab(item.id)}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${activeTab === item.id
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-500 dark:text-slate-400 hover:bg-gray-50 dark:bg-slate-700'
                  }`}
              >
                <Icon className="w-5 h-5" />
                {sidebarOpen && <span>{item.label}</span>}
              </button>
            );
          })}
        </nav>

        {/* User Section */}
        <div className="p-4 border-t border-gray-200 dark:border-slate-700">
          <div className={`${sidebarOpen ? 'space-y-3' : 'space-y-2'}`}>
            {sidebarOpen && (
              <div className="text-sm">
                <p className="font-semibold truncate">{user.name}</p>
                <p className="text-gray-500 dark:text-slate-400 text-xs truncate">{user.email}</p>
              </div>
            )}
            <button
              onClick={onLogout}
              className="w-full flex items-center gap-2 px-4 py-2 rounded-lg bg-red-600/20 text-red-400 hover:bg-red-600/30 transition-colors"
            >
              <LogOut className="w-5 h-5" />
              {sidebarOpen && <span>Logout</span>}
            </button>
          </div>
        </div>

        {/* Toggle Button */}
        <button
          onClick={() => setSidebarOpen(!sidebarOpen)}
          className="p-4 border-t border-gray-200 dark:border-slate-700 text-gray-500 dark:text-slate-400 hover:text-gray-900 dark:hover:text-gray-900 dark:hover:text-white transition-colors"
        >
          {sidebarOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
        </button>
      </div>

      {/* Main Content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Header */}
        <div className="bg-white dark:bg-slate-800 border-b border-gray-200 dark:border-slate-700 p-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold">
              {menuItems.find((m) => m.id === activeTab)?.label || 'Dashboard'}
            </h1>
            <div className="text-sm text-gray-500 dark:text-slate-400">
              {new Date().toLocaleDateString('en-US', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric',
              })}
            </div>
          </div>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="bg-red-900/20 border-l-4 border-red-500 p-4 m-4 rounded flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-red-400 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <p className="text-red-200">{error}</p>
            </div>
            <button
              onClick={onErrorDismiss}
              className="text-red-400 hover:text-red-300"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-auto">
          {activeTab === 'dashboard' && <DashboardView user={user} />}
          {activeTab === 'clients' && <div className="p-6">Client Manager - Coming Soon</div>}
          {activeTab === 'groups' && <div className="p-6">Group Manager - Coming Soon</div>}
          {activeTab === 'loans' && <LoanManagementHub />}
          {activeTab === 'teller' && <div className="p-6">Teller Terminal - Coming Soon</div>}
          {activeTab === 'transactions' && <div className="p-6">Transaction Explorer - Coming Soon</div>}
          {activeTab === 'statements' && <div className="p-6">Statement Verification - Coming Soon</div>}
          {activeTab === 'accounting' && <div className="p-6">Accounting Engine - Coming Soon</div>}
          {activeTab === 'vault' && <VaultManagementHub />}
          {activeTab === 'treasury' && <TreasuryManagementHub />}
          {activeTab === 'operations' && <div className="p-6">Operations Hub - Coming Soon</div>}
          {activeTab === 'reporting' && <ReportingHub />}
          {activeTab === 'loanofficer' && <div className="p-6">Loan Officer Screen - Coming Soon</div>}
          {activeTab === 'accountant' && <div className="p-6">Accountant Screen - Coming Soon</div>}
          {activeTab === 'customerservice' && <div className="p-6">Customer Service Screen - Coming Soon</div>}
          {activeTab === 'compliance' && <div className="p-6">Compliance Screen - Coming Soon</div>}
          {activeTab === 'approvals' && <div className="p-6">Approval Inbox - Coming Soon</div>}
          {activeTab === 'products' && <div className="p-6">Product Designer - Coming Soon</div>}
          {activeTab === 'eod' && <div className="p-6">End of Day Console - Coming Soon</div>}
          {activeTab === 'extensibility' && <ExtensibilityTestPage />}
          {activeTab === 'settings' && <Settings />}
          {activeTab === 'audit' && <div className="p-6">Audit Trail - Coming Soon</div>}
          {activeTab === 'tasks' && <div className="p-6">Development Tasks - Coming Soon</div>}
        </div>
      </div>
    </div>
  );
}

/**
 * Dashboard View Component
 */
function DashboardView({ user }: { user: User }) {
  const { getTreasuryPositions } = useTreasury();
  const { getReportCatalog } = useReports();
  const [loading, setLoading] = React.useState(true);
  const [stats, setStats] = React.useState({
    totalMarketValue: 0,
    activePositions: 0,
    totalReports: 0,
  });

  React.useEffect(() => {
    const loadStats = async () => {
      try {
        setLoading(true);
        const [positions, catalog] = await Promise.all([
          getTreasuryPositions(),
          getReportCatalog(),
        ]);

        const totalValue = Array.isArray(positions)
          ? positions.reduce((sum, p) => sum + (p.closingBalance || 0), 0)
          : 0;

        setStats({
          totalMarketValue: totalValue,
          activePositions: Array.isArray(positions) ? positions.length : 0,
          totalReports: Array.isArray(catalog) ? catalog.length : 0,
        });
      } catch (err) {
        console.error('Failed to load dashboard stats:', err);
      } finally {
        setLoading(false);
      }
    };

    loadStats();
  }, [getTreasuryPositions, getReportCatalog]);

  return (
    <div className="p-6 space-y-6">
      {/* Welcome Card */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-700 rounded-lg p-6 border border-blue-500/50">
        <h2 className="text-3xl font-bold mb-2">Welcome, {user.name}! 👋</h2>
        <p className="text-blue-100">
          You're logged in as <strong>{user.role}</strong>
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 p-4 rounded-lg flex flex-col justify-between">
          <p className="text-gray-500 dark:text-slate-400 text-sm">Total Market Value</p>
          <p className="text-2xl font-bold text-blue-400">
            {loading ? <RefreshCw className="w-5 h-5 animate-spin" /> : `$${stats.totalMarketValue.toLocaleString('en-US', { maximumFractionDigits: 0 })}`}
          </p>
        </div>
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 p-4 rounded-lg flex flex-col justify-between">
          <p className="text-gray-500 dark:text-slate-400 text-sm">Active Treasury Positions</p>
          <p className="text-2xl font-bold text-green-400">
            {loading ? <RefreshCw className="w-5 h-5 animate-spin" /> : stats.activePositions}
          </p>
        </div>
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 p-4 rounded-lg flex flex-col justify-between">
          <p className="text-gray-500 dark:text-slate-400 text-sm">Available Reports</p>
          <p className="text-2xl font-bold text-purple-400">
            {loading ? <RefreshCw className="w-5 h-5 animate-spin" /> : stats.totalReports}
          </p>
        </div>
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 p-4 rounded-lg flex flex-col justify-between">
          <p className="text-gray-500 dark:text-slate-400 text-sm">System Status</p>
          <p className="text-2xl font-bold text-green-400">Online</p>
        </div>
      </div>

      {/* Quick Features */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-6">
          <h3 className="text-xl font-bold mb-4">Available Features</h3>
          <ul className="space-y-2 text-gray-600 dark:text-slate-300">
            <li>✓ Advanced Reporting & Analytics</li>
            <li>✓ Treasury Management</li>
            <li>✓ Vault & Cash Management</li>
            <li>✓ User Management</li>
            <li>✓ Audit Trail & Compliance</li>
            <li>✓ Performance Monitoring</li>
            <li>✓ Rate Limiting & Security</li>
          </ul>
        </div>

        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-6">
          <h3 className="text-xl font-bold mb-4">System Information</h3>
          <ul className="space-y-2 text-gray-600 dark:text-slate-300">
            <li>
              <span className="text-gray-500 dark:text-slate-400">API Version:</span> 1.0.0
            </li>
            <li>
              <span className="text-gray-500 dark:text-slate-400">Phase:</span> 4 - Integration &
              Testing
            </li>
            <li>
              <span className="text-gray-500 dark:text-slate-400">Database:</span> PostgreSQL
            </li>
            <li>
              <span className="text-gray-500 dark:text-slate-400">Frontend:</span> React + TypeScript
            </li>
            <li>
              <span className="text-gray-500 dark:text-slate-400">Authentication:</span> JWT Bearer
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
}
