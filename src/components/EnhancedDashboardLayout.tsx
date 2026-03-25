import React, { Suspense, lazy, useState, useMemo, useEffect } from 'react';
import { User } from '../services/authService';
import { MenuItem as AppMenuItem, Role, UILayout, Workflow } from '../../types';
import { localRegistryService } from '../services/localRegistryService';
import {
  Menu,
  X,
  LogOut,
  LayoutDashboard,
  TrendingUp,
  FileText,
  Briefcase,
  Users,
  UserPlus,
  Settings as SettingsIcon,
  AlertCircle,
  RefreshCw,
  Search,
  Archive,
  DollarSign,
  Plus,
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
  ChevronDown,
  Boxes,
  Zap,
  TrendingDown,
  Activity,
} from 'lucide-react';
import { Permissions } from '../../lib/Permissions';
import { ProtectedRoute } from './ProtectedRoute';
import { useAdmin, useLoans, useGl, useInvestments } from '../hooks/useApi';
import { customerService } from '../services/customerService';
import { accountService } from '../services/accountService';
import { transactionService } from '../services/transactionService';
import { approvalService } from '../services/approvalService';
import { getDefaultRoute } from '../../lib/permissionUtils';
import { groupLendingService } from '../services/groupLendingService';

// Import missing screens
import ReportingHub from './ReportingHub';
import TreasuryManagementHub from './TreasuryManagementHub';
import VaultManagementHub from './VaultManagementHub';
import LoanManagementHub from './LoanManagementHub';
import ExtensibilityTestPage from './DynamicForms/ExtensibilityTestPage';
import TaskInbox from './TaskInbox';
import BankingOSControlCenter from './BankingOSControlCenter';
import Settings from '../../components/Settings';
import TellerTerminal from '../../components/TellerTerminal';
import TellerNotesScreen from '../../components/TellerNotesScreen';
import CashOpsNotesScreen from '../../components/CashOpsNotesScreen';
import TransactionExplorer from '../../components/TransactionExplorer';
import StatementVerification from '../../components/StatementVerification';
import AccountingEngine from '../../components/AccountingEngine';
import OperationsHub from '../../components/OperationsHub';
import ApprovalInbox from '../../components/ApprovalInbox';
import ProductDesigner from '../../components/ProductDesigner';
import EodConsole from '../../components/EodConsole';
import AuditTrail from '../../components/AuditTrail';
import SecurityOperationsHub from './SecurityOperationsHub';
import GroupLendingHub from './group-lending/GroupLendingHub';
import ClientManager from '../../components/ClientManager';
import FeePanel from '../../components/FeePanel';
import PenaltyPanel from '../../components/PenaltyPanel';
import NPLPanel from '../../components/NPLPanel';
import RiskDashboard from '../../components/RiskDashboard';
import InvestmentPortfolio from '../../components/InvestmentPortfolio';
import FxRateManagement from '../../components/FxRateManagement';
import FxTradingDesk from '../../components/FxTradingDesk';

interface MenuItem {
  id: string;
  label: string;
  icon: React.ReactNode;
  permission?: string;
  subItems?: MenuItem[];
}

interface MenuGroup {
  label: string;
  items: MenuItem[];
}


function ScreenLoadingFallback() {
  return (
    <div className="flex min-h-[320px] items-center justify-center p-8">
      <div className="glass-card w-full max-w-md rounded-[28px] border border-white/70 p-8 text-center shadow-soft dark:border-white/10">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-brand-600 text-white shadow-soft">
          <RefreshCw className="h-7 w-7 animate-spin" />
        </div>
        <h3 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Loading Screen</h3>
        <p className="mt-2 text-sm font-medium text-slate-600 dark:text-slate-300">Loading operational data, permissions, and screen content.</p>
      </div>
    </div>
  );
}

interface DashboardLayoutProps {
  user: User;
  onLogout: () => void;
  error?: string | null;
  onErrorDismiss?: () => void;
}

export default function EnhancedDashboardLayout({
  user,
  onLogout,
  error,
  onErrorDismiss,
}: DashboardLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [activeTab, setActiveTab] = useState(() => {
    return user?.permissions ? getDefaultRoute(user.permissions) : 'dashboard';
  });
  const [expandedMenus, setExpandedMenus] = useState<Set<string>>(new Set());
  const [menuQuery, setMenuQuery] = useState('');
  const userPermissions = user?.permissions ?? [];
  const hasPermission = (permission: string) => userPermissions.includes(permission);

    // Initialize API hooks
  const { getProducts, createProduct, updateProduct, getAuditLogs, getRoles } = useAdmin();
  const { getLoans, disburseLoan } = useLoans();
  const { getAccounts: getGlAccounts, createAccount: createGlAccount, getJournalEntries, postJournalEntry } = useGl();
  const { getInvestments, createInvestment, liquidateInvestment, getFixedDeposits, createFixedDeposit } = useInvestments();

  // Data state for various components
  const [customers, setCustomers] = useState<any[]>([]);
  const [accounts, setAccounts] = useState<any[]>([]);
  const [transactions, setTransactions] = useState<any[]>([]);
  const [loans, setLoans] = useState<any[]>([]);
  const [products, setProducts] = useState<any[]>([]);
  const [glAccounts, setGlAccounts] = useState<any[]>([]);
  const [journalEntries, setJournalEntries] = useState<any[]>([]);
  const [groups, setGroups] = useState<any[]>([]);
  const [centers, setCenters] = useState<any[]>([]);
  const [groupApplications, setGroupApplications] = useState<any[]>([]);
  const [groupMeetings, setGroupMeetings] = useState<any[]>([]);
  const [groupPortfolioSummary, setGroupPortfolioSummary] = useState<any | null>(null);
  const [groupParReport, setGroupParReport] = useState<any[]>([]);
  const [groupOfficerPerformance, setGroupOfficerPerformance] = useState<any[]>([]);
  const [groupCycleAnalysis, setGroupCycleAnalysis] = useState<any[]>([]);
  const [groupDelinquencyReport, setGroupDelinquencyReport] = useState<any[]>([]);
  const [groupMeetingCollectionsReport, setGroupMeetingCollectionsReport] = useState<any[]>([]);
  const [investments, setInvestments] = useState<any[]>([]);
  const [fixedDeposits, setFixedDeposits] = useState<any[]>([]);
  const [auditLogs, setAuditLogs] = useState<any[]>([]);
  const [approvals, setApprovals] = useState<any[]>([]);
  const [customForms, setCustomForms] = useState<UILayout[]>([]);
  const [menuItems, setMenuItems] = useState<AppMenuItem[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [isLoadingData, setIsLoadingData] = useState(false);
  const environmentLabel = (import.meta.env.MODE || 'production').toUpperCase();
  const apiBaseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5176/api';
  const commandDate = new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
  const commandTime = new Date().toLocaleTimeString('en-US', {
    hour: '2-digit',
    minute: '2-digit',
  });

  const loadCustomers = async () => {
    const customersData = await customerService.getCustomers();
    setCustomers(customersData || []);
    return customersData;
  };

  const loadRetailAccounts = async () => {
    const accountData = await accountService.getAccounts();
    setAccounts(accountData || []);
    return accountData;
  };

  const loadTransactions = async () => {
    const transactionData = await transactionService.getTransactions();
    setTransactions(transactionData || []);
    return transactionData;
  };

  const loadLoans = async () => {
    const loansData = await getLoans();
    setLoans(loansData || []);
    return loansData;
  };

  const loadGlAccounts = async () => {
    const glData = await getGlAccounts();
    setGlAccounts(glData || []);
    return glData;
  };

  const loadJournalEntries = async () => {
    const entries = await getJournalEntries();
    setJournalEntries(entries || []);
    return entries;
  };

  const loadGroups = async () => {
    const data = await groupLendingService.getGroups();
    setGroups(data || []);
    return data;
  };

  const loadCenters = async () => {
    const data = await groupLendingService.getCenters();
    setCenters(data || []);
    return data;
  };

  const loadGroupReports = async () => {
    const [summary, par, officer, cycle, delinquency, meetingCollections] = await Promise.all([
      groupLendingService.getPortfolioSummary(),
      groupLendingService.getParReport(),
      groupLendingService.getOfficerPerformance(),
      groupLendingService.getCycleAnalysis(),
      groupLendingService.getDelinquencyReport(),
      groupLendingService.getMeetingCollectionsReport(),
    ]);
    setGroupPortfolioSummary(summary || null);
    setGroupParReport((par as any[]) || []);
    setGroupOfficerPerformance((officer as any[]) || []);
    setGroupCycleAnalysis((cycle as any[]) || []);
    setGroupDelinquencyReport((delinquency as any[]) || []);
    setGroupMeetingCollectionsReport((meetingCollections as any[]) || []);
  };
  const loadProducts = async () => {
    const productsData = await getProducts();
    setProducts(productsData || []);
    return productsData;
  };

  const loadInvestments = async () => {
    const investmentsData = await getInvestments();
    setInvestments(investmentsData || []);
    return investmentsData;
  };

  const loadFixedDeposits = async () => {
    const fdData = await getFixedDeposits();
    setFixedDeposits(fdData || []);
    return fdData;
  };

  const loadAuditLogs = async () => {
    const logs = await getAuditLogs(100);
    setAuditLogs(logs || []);
    return logs;
  };

  const loadApprovals = async () => {
    const approvalData = await approvalService.getApprovals();
    setApprovals(approvalData || []);
    return approvalData;
  };

  const loadRoles = async () => {
    const roleData = await getRoles();
    setRoles((roleData || []) as Role[]);
    return roleData;
  };

  useEffect(() => {
    setCustomForms(localRegistryService.getForms());
    setMenuItems(localRegistryService.getMenuItems());
  }, []);

  useEffect(() => {
    const screenLabel = activeTab
      .split('-')
      .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
      .join(' ');
    document.title = `${screenLabel || 'Dashboard'} | BankInsight`;
  }, [activeTab]);


  // Fetch initial data on mount
  useEffect(() => {
    const fetchData = async () => {
      setIsLoadingData(true);
      try {
        const loaders: Array<Promise<unknown>> = [];

        if (hasPermission(Permissions.Customers.View)) {
          loaders.push(
            loadCustomers().catch((err) => console.error('Failed to load customers:', err)),
          );
        }

        if (hasPermission(Permissions.Accounts.View)) {
          loaders.push(
            loadRetailAccounts().catch((err) => console.error('Failed to load retail accounts:', err)),
          );
        }

        if (hasPermission(Permissions.Transactions.View) || hasPermission(Permissions.Transactions.Post)) {
          loaders.push(loadTransactions().catch((err) => console.error('Failed to load transactions:', err)));
        }

        if (hasPermission(Permissions.Loans.View)) {
          loaders.push(loadLoans().catch((err) => console.error('Failed to load loans:', err)));
        }

        if (hasPermission(Permissions.GeneralLedger.View)) {
          loaders.push(
            loadGlAccounts().catch((err) => console.error('Failed to load GL accounts:', err)),
            loadJournalEntries().catch((err) => console.error('Failed to load journal entries:', err)),
          );
        }

        if (hasPermission(Permissions.Roles.View)) {
          loaders.push(
            loadProducts().catch((err) => console.error('Failed to load products:', err)),
            loadRoles().catch((err) => console.error('Failed to load roles:', err)),
          );
        }

        if (hasPermission(Permissions.Accounts.View)) {
          loaders.push(
            loadInvestments().catch((err) => console.error('Failed to load investments:', err)),
            loadFixedDeposits().catch((err) => console.error('Failed to load fixed deposits:', err)),
          );
        }

        if (hasPermission(Permissions.Audit.View)) {
          loaders.push(loadAuditLogs().catch((err) => console.error('Failed to load audit logs:', err)));
        }

        if (hasPermission(Permissions.Loans.Approve)) {
          loaders.push(loadApprovals().catch((err) => console.error('Failed to load approvals:', err)));
        }

        if (hasPermission(Permissions.GroupLending?.View || 'group-lending.view')) {
          loaders.push(
            loadGroups().catch((err) => console.error('Failed to load lending groups:', err)),
            loadCenters().catch((err) => console.error('Failed to load lending centers:', err)),
          );
        }

        if (hasPermission(Permissions.GroupLending?.ViewReports || 'group-lending.reports.view')) {
          loaders.push(loadGroupReports().catch((err) => console.error('Failed to load group lending reports:', err)));
        }

        await Promise.all(loaders);
      } catch (err) {
        console.error('Error fetching dashboard data:', err);
      } finally {
        setIsLoadingData(false);
      }
    };

    fetchData();
  }, [userPermissions, getLoans, getGlAccounts, getJournalEntries, getProducts, getInvestments, getFixedDeposits, getAuditLogs]);

  useEffect(() => {
    if (activeTab !== 'group-lending') {
      return;
    }

    loadGroups().catch((err) => console.error('Failed to refresh lending groups:', err));
    loadCenters().catch((err) => console.error('Failed to refresh lending centers:', err));
    loadGroupReports().catch((err) => console.error('Failed to refresh group lending reports:', err));
  }, [activeTab]);

  // Helper functions for component actions
  const handleCreateCustomer = async (data: any) => {
    const createdCustomer = await customerService.createCustomer(data);
    await loadCustomers();
    return createdCustomer;
  };

  const handleUpdateCustomer = async (id: string, data: any) => {
    const updatedCustomer = await customerService.updateCustomer(id, data);
    await loadCustomers();
    return updatedCustomer;
  };

  const handleCreateAccount = async (customerId: string, productCode: string, currency: string) => {
    const createdAccount = await accountService.createAccount({
      customerId,
      branchId: '001',
      type: 'SAVINGS',
      currency,
      productCode,
    });
    await loadRetailAccounts();
    return createdAccount.id;
  };

  const handleTransaction = async (accountId: string, type: any, amount: number, narration: string, tellerId: string) => {
    const transaction = await transactionService.createTransaction({
      accountId,
      type,
      amount,
      narration,
      tellerId: user.id || tellerId || 'STF1123',
      clientReference: `${type}-${Date.now()}`,
    });

    await Promise.all([
      loadTransactions(),
      loadRetailAccounts(),
      hasPermission(Permissions.Loans.View) ? loadLoans() : Promise.resolve(),
      hasPermission(Permissions.Audit.View) ? loadAuditLogs() : Promise.resolve(),
    ]);

    return {
      success: true,
      id: transaction.id,
      message: transaction.reference || 'Transaction posted successfully',
      status: transaction.status === 'PENDING' ? 'PENDING_APPROVAL' as const : 'POSTED' as const,
    };
  };
  const handleApproveRequest = async (id: string, workflowStep: number) => {
    await approvalService.approveApproval(id, workflowStep + 1);
    await loadApprovals();
  };

  const handleRejectRequest = async (id: string) => {
    await approvalService.rejectApproval(id);
    await loadApprovals();
  };

  const handlePostJournal = async (data: any) => {
    await postJournalEntry({
      description: data.description,
      reference: data.reference,
      postedBy: user.name || user.email,
      lines: (data.lines || []).map((line: any) => ({
        accountCode: line.code,
        debit: Number(line.debit || 0),
        credit: Number(line.credit || 0),
      })),
    });

    const [entries, accountsData] = await Promise.all([getJournalEntries(), getGlAccounts()]);
    setJournalEntries(entries || []);
    setGlAccounts(accountsData || []);
  };

  const handleCreateGlAccount = async (data: any) => {
    await createGlAccount(data);
    const accountsData = await getGlAccounts();
    setGlAccounts(accountsData || []);
  };

  const refreshAccounting = async () => {
    const [accountsData, entries] = await Promise.all([getGlAccounts(), getJournalEntries()]);
    setGlAccounts(accountsData || []);
    setJournalEntries(entries || []);
  };

  const handleCreateProduct = async (data: any) => {
    try {
      await createProduct(data);
      // Refresh products
      const productsData = await getProducts();
      setProducts(productsData || []);
    } catch (err) {
      console.error('Failed to create product:', err);
    }
  };

  const handleUpdateProduct = async (id: string, data: any) => {
    try {
      await updateProduct(id, data);
      // Refresh products
      const productsData = await getProducts();
      setProducts(productsData || []);
    } catch (err) {
      console.error('Failed to update product:', err);
    }
  };

  const handleCreateInvestment = async (data: any) => {
    try {
      await createInvestment(data);
      const investmentsData = await getInvestments();
      setInvestments(investmentsData || []);
    } catch (err) {
      console.error('Failed to create investment:', err);
    }
  };

  const handleCreateFixedDeposit = async (data: any) => {
    try {
      await createFixedDeposit(data);
      const fdData = await getFixedDeposits();
      setFixedDeposits(fdData || []);
    } catch (err) {
      console.error('Failed to create fixed deposit:', err);
    }
  };

  const handleLiquidateInvestment = async (id: string) => {
    try {
      await liquidateInvestment(id);
      const investmentsData = await getInvestments();
      setInvestments(investmentsData || []);
    } catch (err) {
      console.error('Failed to liquidate investment:', err);
    }
  };

  const handleDisburseLoan = async (data: any) => {
    try {
      await disburseLoan(data);
      // Refresh loans
      const loansData = await getLoans();
      setLoans(loansData || []);
    } catch (err) {
      console.error('Failed to disburse loan:', err);
    }
  };

  const upsertGroupApplication = (application: any) => {
    setGroupApplications((current) => {
      const existing = current.find((item) => item.id === application.id);
      if (!existing) {
        return [application, ...current];
      }

      return current.map((item) => item.id === application.id ? application : item);
    });
  };

  const upsertGroupMeeting = (meeting: any) => {
    setGroupMeetings((current) => {
      const existing = current.find((item) => item.id === meeting.id);
      if (!existing) {
        return [meeting, ...current];
      }

      return current.map((item) => item.id === meeting.id ? meeting : item);
    });
  };

  const handleCreateGroup = async (data: any) => {
    await groupLendingService.createGroup(data);
    await loadGroups();
    await loadGroupReports();
  };

  const handleAddMember = async (groupId: string, payload: any) => {
    await groupLendingService.addMember(groupId, payload);
    await loadGroups();
    await loadGroupReports();
  };

  const handleRemoveMember = async (groupId: string, memberId: string) => {
    await groupLendingService.removeMember(groupId, memberId);
    await loadGroups();
    await loadGroupReports();
  };

  const handleCreateCenter = async (data: any) => {
    await groupLendingService.createCenter(data);
    await loadCenters();
  };

  const handleCreateGroupApplication = async (data: any) => {
    const application = await groupLendingService.createApplication(data);
    upsertGroupApplication(application);
    await loadGroupReports();
  };

  const handleSubmitGroupApplication = async (id: string) => {
    const application = await groupLendingService.submitApplication(id);
    upsertGroupApplication(application);
  };

  const handleApproveGroupApplication = async (id: string) => {
    const application = await groupLendingService.approveApplication(id, { decisionNotes: 'Approved from operations screen' });
    upsertGroupApplication(application);
    await loadGroupReports();
  };

  const handleRejectGroupApplication = async (id: string) => {
    const application = await groupLendingService.rejectApplication(id, 'Rejected from operations screen');
    upsertGroupApplication(application);
  };

  const handleDisburseGroupApplication = async (id: string) => {
    const application = await groupLendingService.disburseApplication(id, { disbursementDate: new Date().toISOString().slice(0, 10) });
    upsertGroupApplication(application);
    await Promise.all([loadLoans(), loadGroupReports(), loadGroups()]);
  };

  const handleCreateGroupMeeting = async (data: any) => {
    const meeting = await groupLendingService.createMeeting(data);
    upsertGroupMeeting(meeting);
  };

  const handleSaveForm = (layout: UILayout) => {
    setCustomForms(localRegistryService.saveForm(layout, { id: user.id, name: user.name, role: user.role }));
  };

  const handleSaveMenu = (item: AppMenuItem) => {
    setMenuItems(localRegistryService.saveMenuItem(item));
  };

  const handleDeleteMenu = (id: string) => {
    setMenuItems(localRegistryService.deleteMenuItem(id));
  };

  const currentRoleIds = useMemo(() => {
    const match = roles.find((role) => role.name === user.role || role.id === user.role);
    return [user.role, match?.id].filter(Boolean) as string[];
  }, [roles, user.role]);

  const customMenuGroup: MenuGroup | null = useMemo(() => {
    const visibleMenuItems = menuItems.filter((item) => !item.requiredRoleIds?.length || item.requiredRoleIds.some((roleId) => currentRoleIds.includes(roleId)));
    if (!visibleMenuItems.length) {
      return null;
    }

    return {
      label: 'CUSTOM TOOLS',
      items: visibleMenuItems.map((item) => ({
        id: item.type === 'FORM' ? `form-designer:${item.targetId}` : item.targetId || item.id,
        label: item.label,
        icon: <LayoutDashboard size={18} />,
      })),
    };
  }, [currentRoleIds, menuItems]);

  // Define menu structure with groups and submenus
  const menuGroups: MenuGroup[] = [
    {
      label: 'CORE OPERATIONS',
      items: [
        {
          id: 'dashboard',
          label: 'Dashboard',
          icon: <LayoutDashboard size={18} />,
        },
        {
          id: 'clients',
          label: 'Clients',
          icon: <Users size={18} />,
          permission: Permissions.Customers.View,
          subItems: [
            { id: 'clients-list', label: 'Client List', icon: <Users size={16} />, permission: Permissions.Customers.View },
            { id: 'clients-onboard', label: 'Onboarding', icon: <UserPlus size={16} />, permission: Permissions.Customers.Create },
          ],
        },
        {
          id: 'accounts',
          label: 'Account Opening',
          icon: <FileText size={18} />,
          permission: Permissions.Accounts.Open,
          subItems: [
            { id: 'accounts-list', label: 'Client Accounts', icon: <FileText size={16} />, permission: Permissions.Accounts.View },
            { id: 'accounts-create', label: 'New Account Flow', icon: <Plus size={16} />, permission: Permissions.Accounts.Open },
          ],
        },
        {
          id: 'group-lending',
          label: 'Group Lending',
          icon: <Users size={18} />,
          permission: Permissions.GroupLending.View,
        },
        {
          id: 'teller',
          label: 'Teller',
          icon: <Landmark size={18} />,
          permission: Permissions.Transactions.Post,
            subItems: [
              { id: 'teller-deposit', label: 'Cash Deposits', icon: <DollarSign size={16} />, permission: Permissions.Transactions.Post },
              { id: 'teller-withdrawal', label: 'Cash Withdrawals', icon: <TrendingDown size={16} />, permission: Permissions.Transactions.Post },
              { id: 'teller-transfers', label: 'Transfers', icon: <ArrowRightLeft size={16} />, permission: Permissions.Transactions.Post },
              { id: 'teller-notes', label: 'Teller Notes', icon: <FileText size={16} />, permission: Permissions.Transactions.Post },
            ],
          },
        {
          id: 'transactions',
          label: 'Transactions',
          icon: <ArrowRightLeft size={18} />,
          permission: Permissions.Accounts.View,
        },
        {
          id: 'bankingos',
          label: 'BankingOS',
          icon: <Boxes size={18} />,
          permission: Permissions.Processes.View,
        },
      ],
    },
    {
      label: 'LOAN MANAGEMENT',
      items: [
        {
          id: 'loans',
          label: 'Loans',
          icon: <DollarSign size={18} />,
          permission: Permissions.Loans.View,
          subItems: [
            { id: 'loans-pipeline', label: 'Pipeline', icon: <Briefcase size={16} />, permission: Permissions.Loans.View },
            { id: 'loans-portfolio', label: 'Portfolio', icon: <TrendingUp size={16} />, permission: Permissions.Loans.View },
          ],
        },
        {
          id: 'approvals',
          label: 'Approvals',
          icon: <CheckCircle size={18} />,
          permission: Permissions.Loans.Approve,
        },
      ],
    },
    {
      label: 'FINANCIAL MANAGEMENT',
      items: [
        {
          id: 'accounting',
          label: 'Accounting',
          icon: <Calculator size={18} />,
          permission: Permissions.GeneralLedger.View,
          subItems: [
            { id: 'accounting-je', label: 'Journal Entries', icon: <FileText size={16} />, permission: Permissions.GeneralLedger.Post },
            { id: 'accounting-reconcile', label: 'Reconciliation', icon: <CheckCircle size={16} />, permission: Permissions.GeneralLedger.Post },
            { id: 'accounting-gl', label: 'GL Accounts', icon: <Briefcase size={16} />, permission: Permissions.GeneralLedger.View },
          ],
        },
        {
          id: 'statements',
          label: 'Statements',
          icon: <FileCheck size={18} />,
          permission: Permissions.Accounts.View,
        },
        {
          id: 'treasury',
          label: 'Treasury',
          icon: <TrendingUp size={18} />,
          permission: Permissions.Accounts.View,
          subItems: [
            { id: 'treasury-position', label: 'Positions', icon: <TrendingUp size={16} />, permission: Permissions.Accounts.View },
            { id: 'treasury-fx', label: 'FX Management', icon: <DollarSign size={16} />, permission: Permissions.Accounts.View },
            { id: 'treasury-investments', label: 'Investments', icon: <TrendingUp size={16} />, permission: Permissions.Accounts.View },
          ],
        },
          {
            id: 'vault',
            label: 'Vault',
            icon: <Archive size={18} />,
            permission: Permissions.Accounts.View,
            subItems: [
              { id: 'vault', label: 'Cash Operations', icon: <Archive size={16} />, permission: Permissions.Accounts.View },
              { id: 'cash-ops-notes', label: 'Cash Notes', icon: <FileText size={16} />, permission: Permissions.Accounts.View },
            ],
          },
      ],
    },
    {
      label: 'OPERATIONS & RISK',
      items: [
        {
          id: 'operations',
          label: 'Operations',
          icon: <Settings2 size={18} />,
          permission: Permissions.Accounts.View,
          subItems: [
            { id: 'operations-fees', label: 'Fees', icon: <DollarSign size={16} />, permission: Permissions.Accounts.View },
            { id: 'operations-penalties', label: 'Penalties', icon: <AlertCircle size={16} />, permission: Permissions.Accounts.View },
            { id: 'operations-npl', label: 'NPL Management', icon: <TrendingDown size={16} />, permission: Permissions.Accounts.View },
          ],
        },
        {
          id: 'reporting',
          label: 'Reporting',
          icon: <FileText size={18} />,
          permission: Permissions.Reports.View,
        },
      ],
    },

    {
      label: 'SYSTEM',
      items: [
        {
          id: 'products',
          label: 'Products',
          icon: <Package size={18} />,
          permission: Permissions.Roles.View,
        },
        {
          id: 'settings',
          label: 'Settings',
          icon: <SettingsIcon size={18} />,
          permission: Permissions.Roles.View,
        },
        {
          id: 'eod',
          label: 'End of Day',
          icon: <Moon size={18} />,
          permission: Permissions.Roles.View,
        },
        {
          id: 'audit',
          label: 'Audit Trail',
          icon: <History size={18} />,
          permission: Permissions.Roles.View,
        },
        {
          id: 'security-ops',
          label: 'Security Ops',
          icon: <Shield size={18} />,
          permission: Permissions.Audit.View,
        },
        {
          id: 'extensibility',
          label: 'Platform Tools',
          icon: <Zap size={18} />,
          permission: Permissions.Roles.View,
        },
      ],
    },
  ];

  // Filter menu items based on permissions
  const filteredMenuGroups = useMemo(() => {
    const query = menuQuery.trim().toLowerCase();
    const groups = customMenuGroup ? [...menuGroups, customMenuGroup] : menuGroups;

    return groups
      .map((group) => ({
        ...group,
        items: group.items
          .filter((item) => !item.permission || hasPermission(item.permission))
          .map((item) => {
            const visibleSubItems = item.subItems?.filter((sub) => !sub.permission || hasPermission(sub.permission)) || [];
            if (!query) {
              return { ...item, subItems: visibleSubItems };
            }

            const itemMatches = item.label.toLowerCase().includes(query);
            const matchingSubItems = visibleSubItems.filter((sub) => sub.label.toLowerCase().includes(query));

            if (itemMatches || matchingSubItems.length > 0) {
              return {
                ...item,
                subItems: itemMatches ? visibleSubItems : matchingSubItems,
              };
            }

            return null;
          })
          .filter(Boolean) as MenuItem[],
      }))
      .filter((group) => group.items.length > 0);
  }, [customMenuGroup, menuQuery, userPermissions]);

  const activeMenuMeta = useMemo(() => {
    if (activeTab.startsWith('form-designer') || activeTab === 'process-designer') {
      return {
        label: 'BankingOS Config Studio',
        icon: <Boxes className="w-6 h-6 text-brand-500" />,
      };
    }

    for (const group of filteredMenuGroups) {
      const item = group.items.find((entry) => entry.id === activeTab);
      if (item) {
        return {
          label: item.label,
          icon: <span className="text-brand-500">{item.icon}</span>,
        };
      }

      const subItem = group.items
        .flatMap((entry) => entry.subItems || [])
        .find((entry) => entry.id === activeTab);

      if (subItem) {
        return {
          label: subItem.label,
          icon: <span className="text-brand-500">{subItem.icon}</span>,
        };
      }
    }

    return {
      label: 'Dashboard',
      icon: <LayoutDashboard className="w-6 h-6 text-brand-500" />,
    };
  }, [activeTab, filteredMenuGroups]);

  const workspaceShortcuts = useMemo(() => {
    const preferredIds = ['dashboard', 'bankingos', 'clients-onboard', 'loans-pipeline', 'group-lending', 'approvals', 'reporting'];
    const allItems = filteredMenuGroups.flatMap((group) => [
      ...group.items,
      ...group.items.flatMap((item) => item.subItems || []),
    ]);

    return preferredIds
      .map((id) => allItems.find((item) => item.id === id))
      .filter(Boolean)
      .slice(0, 5) as MenuItem[];
  }, [filteredMenuGroups]);

  const commandStatusCards = useMemo(() => ([
    {
      label: 'Environment',
      value: environmentLabel,
      tone: environmentLabel === 'PRODUCTION' ? 'green' : 'amber',
    },
    {
        label: 'Screen',
      value: activeMenuMeta.label,
      tone: 'blue',
    },
    {
      label: 'API',
      value: apiBaseUrl.replace(/^https?:\/\//, ''),
      tone: 'slate',
    },
    {
      label: 'Session',
      value: user.role || 'Authenticated',
      tone: 'green',
    },
  ]), [activeMenuMeta.label, apiBaseUrl, environmentLabel, user.role]);

  const toggleMenu = (menuId: string) => {
    const newExpanded = new Set(expandedMenus);
    if (newExpanded.has(menuId)) {
      newExpanded.delete(menuId);
    } else {
      newExpanded.add(menuId);
    }
    setExpandedMenus(newExpanded);
  };

  const handleMenuClick = (itemId: string) => {
    setActiveTab(itemId);
  };

  // Render screen content based on active tab
  const renderScreenContent = () => {
    // Show loading indicator while data is being fetched
    if (isLoadingData && activeTab !== 'dashboard') {
      return <ScreenLoadingFallback />;
    }

    if (activeTab.startsWith('form-designer:') || activeTab === 'form-designer' || activeTab === 'process-designer') {
      return (
        <ProtectedRoute requiredPermission={Permissions.Processes.View} userPermissions={userPermissions}>
          <NavigationLanding
            title="Design tooling has moved to BankingOS"
            description="Form Designer and Process Designer are now maintained only in BankingOS so configuration stays in one governed platform workspace."
            primaryActionLabel="Open BankingOS"
            onPrimaryAction={() => setActiveTab('bankingos')}
            secondaryActionLabel="Open Task Inbox"
            onSecondaryAction={() => setActiveTab('task-inbox')}
          />
        </ProtectedRoute>
      );
    }

    switch (activeTab) {
      case 'dashboard':
        return (
          <DashboardView
            user={user}
            onNavigate={setActiveTab}
            customers={customers}
            accounts={accounts}
            loans={loans}
            transactions={transactions}
            auditLogs={auditLogs}
            isLoading={isLoadingData}
          />
        );
      case 'clients-list':
      case 'clients':
        return (
          <ProtectedRoute requiredPermission={Permissions.Customers.View} userPermissions={userPermissions}>
            <ClientManager 
              key={activeTab}
              customers={customers} 
              accounts={accounts} 
              loans={loans} 
              transactions={transactions} 
              products={products} 
              onCreateCustomer={handleCreateCustomer} 
              onUpdateCustomer={handleUpdateCustomer} 
              onCreateAccount={handleCreateAccount}
              initialView="LIST"
            />
          </ProtectedRoute>
        );
      case 'clients-onboard':
        return (
          <ProtectedRoute requiredPermission={Permissions.Customers.Create} userPermissions={userPermissions}>
            <ClientManager 
              key={activeTab}
              customers={customers} 
              accounts={accounts} 
              loans={loans} 
              transactions={transactions} 
              products={products} 
              onCreateCustomer={handleCreateCustomer} 
              onUpdateCustomer={handleUpdateCustomer} 
              onCreateAccount={handleCreateAccount}
              initialView="CREATE"
            />
          </ProtectedRoute>
        );
      case 'accounts-list':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <NavigationLanding
              title="Accounts are organized by client"
                description="Manage accounts from the client profile. Open the client list to review customer accounts, or go to statements for account-level history and verification."
              primaryActionLabel="Open Client List"
              onPrimaryAction={() => setActiveTab('clients-list')}
              secondaryActionLabel="Open Statements"
              onSecondaryAction={() => setActiveTab('statements')}
            />
          </ProtectedRoute>
        );
      case 'accounts-create':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.Open} userPermissions={userPermissions}>
            <NavigationLanding
              title="Create an account from onboarding"
                description="New accounts are opened from the client profile so the customer, KYC profile, and product selection stay together in one flow."
              primaryActionLabel="Start Onboarding"
              onPrimaryAction={() => setActiveTab('clients-onboard')}
              secondaryActionLabel="View Clients"
              onSecondaryAction={() => setActiveTab('clients-list')}
            />
          </ProtectedRoute>
        );
      case 'task-inbox':
        return <TaskInbox />;
      case 'bankingos':
        return (
          <ProtectedRoute requiredPermission={Permissions.Processes.View} userPermissions={userPermissions}>
            <BankingOSControlCenter />
          </ProtectedRoute>
        );
      case 'groups':
      case 'group-lending':
        return (
          <ProtectedRoute requiredPermission={Permissions.GroupLending.View} userPermissions={userPermissions}>
            <GroupLendingHub
              groups={groups}
              centers={centers}
              products={products}
              customers={customers}
              applications={groupApplications}
              meetings={groupMeetings}
              portfolioSummary={groupPortfolioSummary}
              parReport={groupParReport}
              officerPerformance={groupOfficerPerformance}
              cycleAnalysis={groupCycleAnalysis}
              delinquencyReport={groupDelinquencyReport}
              meetingCollectionsReport={groupMeetingCollectionsReport}
              onCreateGroup={handleCreateGroup}
              onAddMember={handleAddMember}
              onCreateCenter={handleCreateCenter}
              onCreateApplication={handleCreateGroupApplication}
              onSubmitApplication={handleSubmitGroupApplication}
              onApproveApplication={handleApproveGroupApplication}
              onRejectApplication={handleRejectGroupApplication}
              onDisburseApplication={handleDisburseGroupApplication}
              onCreateMeeting={handleCreateGroupMeeting}
            />
          </ProtectedRoute>
        );
      case 'teller':
      case 'teller-deposit':
          return (
            <ProtectedRoute requiredPermission={Permissions.Transactions.Post} userPermissions={userPermissions}>
              <TellerTerminal 
                key={activeTab}
                accounts={accounts}
                customers={customers}
                tellerId={user.id}
                onTransaction={handleTransaction}
                onOpenNotes={() => setActiveTab('teller-notes')}
                initialTransactionType="DEPOSIT"
              />
            </ProtectedRoute>
          );
      case 'teller-withdrawal':
        return (
          <ProtectedRoute requiredPermission={Permissions.Transactions.Post} userPermissions={userPermissions}>
              <TellerTerminal 
                key={activeTab}
                accounts={accounts}
                customers={customers}
                tellerId={user.id}
                onTransaction={handleTransaction}
                onOpenNotes={() => setActiveTab('teller-notes')}
                initialTransactionType="WITHDRAWAL"
              />
            </ProtectedRoute>
          );
      case 'teller-transfers':
        return (
          <ProtectedRoute requiredPermission={Permissions.Transactions.Post} userPermissions={userPermissions}>
              <TellerTerminal 
                key={activeTab}
                accounts={accounts}
                customers={customers}
                tellerId={user.id}
                onTransaction={handleTransaction}
                onOpenNotes={() => setActiveTab('teller-notes')}
                initialTransactionType="TRANSFER"
              />
            </ProtectedRoute>
          );
      case 'teller-notes':
          return (
            <ProtectedRoute requiredPermission={Permissions.Transactions.Post} userPermissions={userPermissions}>
              <TellerNotesScreen accounts={accounts} customers={customers} />
            </ProtectedRoute>
          );
      case 'transactions':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <TransactionExplorer transactions={transactions} />
          </ProtectedRoute>
        );
      case 'statements':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <StatementVerification 
              accounts={accounts} 
              transactions={transactions} 
              customers={customers} 
            />
          </ProtectedRoute>
        );
      case 'accounting':
      case 'accounting-gl':
        return (
          <ProtectedRoute requiredPermission={Permissions.GeneralLedger.View} userPermissions={userPermissions}>
            <AccountingEngine 
              key={activeTab}
              accounts={glAccounts} 
              journalEntries={journalEntries} 
              onPostJournal={handlePostJournal} 
              onCreateAccount={handleCreateGlAccount}
              initialView="COA"
              canPostJournal={hasPermission(Permissions.GeneralLedger.Post)}
              canManageAccounts={hasPermission(Permissions.GeneralLedger.Post)}
              onRefresh={refreshAccounting}
              isLoading={isLoadingData}
            />
          </ProtectedRoute>
        );
      case 'accounting-je':
        return (
          <ProtectedRoute requiredPermission={Permissions.GeneralLedger.Post} userPermissions={userPermissions}>
            <AccountingEngine 
              key={activeTab}
              accounts={glAccounts} 
              journalEntries={journalEntries} 
              onPostJournal={handlePostJournal} 
              onCreateAccount={handleCreateGlAccount}
              initialView="JV"
              canPostJournal={hasPermission(Permissions.GeneralLedger.Post)}
              canManageAccounts={hasPermission(Permissions.GeneralLedger.Post)}
              onRefresh={refreshAccounting}
              isLoading={isLoadingData}
            />
          </ProtectedRoute>
        );
      case 'accounting-reconcile':
        return (
          <ProtectedRoute requiredPermission={Permissions.GeneralLedger.View} userPermissions={userPermissions}>
            <AccountingEngine 
              key={activeTab}
              accounts={glAccounts} 
              journalEntries={journalEntries} 
              onPostJournal={handlePostJournal} 
              onCreateAccount={handleCreateGlAccount}
              initialView="LEDGER"
              canPostJournal={hasPermission(Permissions.GeneralLedger.Post)}
              canManageAccounts={hasPermission(Permissions.GeneralLedger.Post)}
              onRefresh={refreshAccounting}
              isLoading={isLoadingData}
            />
          </ProtectedRoute>
        );
      case 'loans':
      case 'loans-portfolio':
        return (
          <ProtectedRoute requiredPermission={Permissions.Loans.View} userPermissions={userPermissions}>
            <LoanManagementHub 
              key={activeTab}
              loans={loans}
              customers={customers}
              onDisburseLoan={handleDisburseLoan}
              onRepayLoan={(id, data) => console.log('Repay loan:', id, data)}
              initialTab="portfolio"
            />
          </ProtectedRoute>
        );
      case 'loans-pipeline':
        return (
          <ProtectedRoute requiredPermission={Permissions.Loans.View} userPermissions={userPermissions}>
            <LoanManagementHub 
              key={activeTab}
              loans={loans}
              customers={customers}
              onDisburseLoan={handleDisburseLoan}
              onRepayLoan={(id, data) => console.log('Repay loan:', id, data)}
              initialTab="origination"
            />
          </ProtectedRoute>
        );
      case 'loans-approvals':
        return (
          <ProtectedRoute requiredPermission={Permissions.Loans.View} userPermissions={userPermissions}>
            <NavigationLanding
              title="Loan approvals live in the approvals inbox"
                description="Credit approval is handled from the approvals inbox so decisions, audit history, and escalations stay in one place."
              primaryActionLabel="Open Approvals"
              onPrimaryAction={() => setActiveTab('approvals')}
              secondaryActionLabel="Open Portfolio"
              onSecondaryAction={() => setActiveTab('loans-portfolio')}
            />
          </ProtectedRoute>
        );
      case 'approvals':
        return (
          <ProtectedRoute requiredPermission={Permissions.Loans.Approve} userPermissions={userPermissions}>
            <ApprovalInbox 
              requests={approvals} 
              currentUser={{ 
                id: user?.id || '', 
                email: user?.email || '', 
                name: user?.name || '', 
                roleId: '', 
                branchId: '', 
                isActive: true 
              } as any} 
              onApprove={handleApproveRequest} 
              onReject={handleRejectRequest} 
            />
          </ProtectedRoute>
        );
      case 'vault':
          return (
            <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
              <VaultManagementHub onOpenNotes={() => setActiveTab('cash-ops-notes')} />
            </ProtectedRoute>
          );
      case 'cash-ops-notes':
          return (
            <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
              <CashOpsNotesScreen />
            </ProtectedRoute>
          );
      case 'treasury':
      case 'treasury-position':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <TreasuryManagementHub 
              key={activeTab}
              investments={investments}
              fixedDeposits={fixedDeposits}
              onCreateInvestment={handleCreateInvestment}
              onCreateFixedDeposit={handleCreateFixedDeposit}
              onLiquidateInvestment={handleLiquidateInvestment}
              initialTab="positions"
            />
          </ProtectedRoute>
        );
      case 'treasury-fx':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <TreasuryManagementHub 
              key={activeTab}
              investments={investments}
              fixedDeposits={fixedDeposits}
              onCreateInvestment={handleCreateInvestment}
              onCreateFixedDeposit={handleCreateFixedDeposit}
              onLiquidateInvestment={handleLiquidateInvestment}
              initialTab="trades"
            />
          </ProtectedRoute>
        );
      case 'treasury-investments':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <TreasuryManagementHub 
              key={activeTab}
              investments={investments}
              fixedDeposits={fixedDeposits}
              onCreateInvestment={handleCreateInvestment}
              onCreateFixedDeposit={handleCreateFixedDeposit}
              onLiquidateInvestment={handleLiquidateInvestment}
              initialTab="investments"
            />
          </ProtectedRoute>
        );
      case 'operations':
      case 'operations-fees':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <OperationsHub key={activeTab} accounts={accounts} loans={loans} initialTab="fees" />
          </ProtectedRoute>
        );
      case 'operations-penalties':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <OperationsHub key={activeTab} accounts={accounts} loans={loans} initialTab="penalties" />
          </ProtectedRoute>
        );
      case 'operations-npl':
        return (
          <ProtectedRoute requiredPermission={Permissions.Accounts.View} userPermissions={userPermissions}>
            <OperationsHub key={activeTab} accounts={accounts} loans={loans} initialTab="npl" />
          </ProtectedRoute>
        );
      case 'reporting':
        return (
          <ProtectedRoute requiredPermission={Permissions.Reports.View} userPermissions={userPermissions}>
            <ReportingHub />
          </ProtectedRoute>
        );
      case 'loanofficer':
        return (
          <ProtectedRoute requiredPermission={Permissions.Loans.View} userPermissions={userPermissions}>
            <NavigationLanding
              title="Loan officer work happens in the loan pipeline"
              description="Use the production loan workbench for origination, appraisal, approvals, and disbursement instead of the legacy role-specific screen."
              primaryActionLabel="Open Loan Pipeline"
              onPrimaryAction={() => setActiveTab('loans-pipeline')}
              secondaryActionLabel="Open Portfolio"
              onSecondaryAction={() => setActiveTab('loans-portfolio')}
            />
          </ProtectedRoute>
        );
      case 'accountant':
        return (
          <ProtectedRoute requiredPermission={Permissions.GeneralLedger.Post} userPermissions={userPermissions}>
            <NavigationLanding
              title="Accounting operations run through the accounting engine"
              description="Use the core accounting workspace for journals, reconciliations, and ledger review so finance actions stay in the governed production flow."
              primaryActionLabel="Open Accounting Engine"
              onPrimaryAction={() => setActiveTab('accounting')}
              secondaryActionLabel="Open Audit Trail"
              onSecondaryAction={() => setActiveTab('audit')}
            />
          </ProtectedRoute>
        );
      case 'customerservice':
        return (
          <ProtectedRoute requiredPermission={Permissions.Customers.View} userPermissions={userPermissions}>
            <NavigationLanding
              title="Customer service runs from client and statement workspaces"
              description="Use the client manager and statement verification screens for production customer servicing instead of the legacy prototype support dashboard."
              primaryActionLabel="Open Client List"
              onPrimaryAction={() => setActiveTab('clients-list')}
              secondaryActionLabel="Open Statements"
              onSecondaryAction={() => setActiveTab('statements')}
            />
          </ProtectedRoute>
        );
      case 'compliance':
        return (
          <ProtectedRoute requiredPermission={Permissions.Audit.View} userPermissions={userPermissions}>
            <NavigationLanding
              title="Compliance monitoring has moved to governed operations screens"
              description="Use Security Ops, reporting, and onboarding review queues for production compliance work instead of the legacy prototype dashboard."
              primaryActionLabel="Open Security Ops"
              onPrimaryAction={() => setActiveTab('security-ops')}
              secondaryActionLabel="Open Reporting"
              onSecondaryAction={() => setActiveTab('reporting')}
            />
          </ProtectedRoute>
        );
      case 'products':
        return <ProductDesigner 
          products={products} 
          onCreateProduct={handleCreateProduct} 
          onUpdateProduct={handleUpdateProduct} 
        />;
      case 'eod':
        return <EodConsole 
          businessDate={new Date().toISOString().split('T')[0]} 
          onRunStep={async (step) => console.log('Run EOD step:', step)} 
          isLive={true} 
        />;
      case 'audit':
        return <AuditTrail logs={auditLogs} />;
      case 'security-ops':
        return (
          <ProtectedRoute requiredPermission={Permissions.Audit.View} userPermissions={userPermissions}>
            <SecurityOperationsHub userPermissions={userPermissions} />
          </ProtectedRoute>
        );
      case 'extensibility':
        return (
          <ProtectedRoute requiredPermission={Permissions.Roles.View} userPermissions={userPermissions}>
            <NavigationLanding
              title="Platform configuration lives in BankingOS"
              description="Developer sandboxes and draft extensibility tooling are not exposed in production. Use BankingOS for governed configuration and release management."
              primaryActionLabel="Open BankingOS"
              onPrimaryAction={() => setActiveTab('bankingos')}
              secondaryActionLabel="Open Settings"
              onSecondaryAction={() => setActiveTab('settings')}
            />
          </ProtectedRoute>
        );
      case 'settings':
        return (
          <ProtectedRoute requiredPermission={Permissions.Roles.View} userPermissions={userPermissions}>
            <Settings workflows={[]} customForms={customForms} menuItems={menuItems} onSaveMenu={handleSaveMenu} onDeleteMenu={handleDeleteMenu} currentUserId={user.id} pageTargets={[{ id: 'bankingos', label: 'BankingOS Config Studio', helper: 'Open the BankingOS workspace for forms, processes, themes, and governed releases' }, { id: 'security-ops', label: 'Security Ops', helper: 'Open device governance and monitoring' }]} />
          </ProtectedRoute>
        );
      default:
        return (
          <DashboardView
            user={user}
            onNavigate={setActiveTab}
            customers={customers}
            accounts={accounts}
            loans={loans}
            transactions={transactions}
            auditLogs={auditLogs}
            isLoading={isLoadingData}
          />
        );
    }
  };

  return (
    <div className="app-shell simple-screen flex h-screen overflow-hidden bg-slate-100 p-2 text-slate-900">
      {/* Enhanced Professional Sidebar */}
      <div
        className={`${sidebarOpen ? 'w-72' : 'w-20'
          } glass-card flex flex-col border-r border-slate-200 bg-white transition-all duration-300 rounded-lg`}
      >
        {/* Logo Section */}
        <div className="border-b border-slate-200 px-5 py-5">
          <div className="flex items-center gap-3">
            <div className="status-orb flex h-11 w-11 items-center justify-center rounded-lg bg-brand-600 shadow-soft">
              <Landmark className="w-6 h-6 text-white" />
            </div>
            {sidebarOpen && (
              <div>
                <span className="font-heading font-bold text-lg text-slate-900 dark:text-white block leading-tight">
                  BankInsight
                </span>
                <span className="text-xs text-slate-500">Operations Platform</span>
              </div>
            )}
          </div>
        </div>

        {/* Navigation with Groups */}
        <div className="px-3 pt-4">
          {sidebarOpen && (
            <label className="relative block">
              <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                type="text"
                value={menuQuery}
                onChange={(event) => setMenuQuery(event.target.value)}
                placeholder="Search screens"
                className="screen-input py-3 pl-11 pr-4"
              />
            </label>
          )}
        </div>
        <nav className="flex-1 overflow-y-auto px-3 py-4 space-y-6">
          {filteredMenuGroups.map((group) => (
            <div key={group.label}>
              {sidebarOpen && (
                <div className="mb-3 px-3 font-accent text-[10px] font-bold uppercase tracking-[0.30em] text-slate-400 dark:text-slate-500">
                  {group.label}
                </div>
              )}

              <div className="space-y-1">
                {group.items.map((item) => (
                  <div key={item.id}>
                    <button
                      onClick={() => {
                        if (item.subItems && item.subItems.length > 0) {
                          toggleMenu(item.id);
                        } else {
                          handleMenuClick(item.id);
                        }
                      }}
                      className={`group w-full flex items-center gap-3 rounded-2xl px-3.5 py-3 text-sm font-medium transition-all ${
                        activeTab === item.id || activeTab.startsWith(item.id + '-')
                          ? 'bg-brand-600 text-white shadow-sm'
                          : 'text-slate-700 dark:text-slate-300 hover:bg-white/70 dark:hover:bg-white/5 hover:text-brand-700 dark:hover:text-white'
                      }`}
                    >
                      <span className={`flex-shrink-0 ${activeTab === item.id || activeTab.startsWith(item.id + '-') ? '' : 'group-hover:scale-110 transition-transform'}`}>
                        {item.icon}
                      </span>
                      {sidebarOpen && (
                        <>
                          <span className="flex-1 text-left">{item.label}</span>
                          {item.subItems && item.subItems.length > 0 && (
                            <ChevronDown
                              size={16}
                              className={`transition-transform duration-200 ${expandedMenus.has(item.id) ? 'rotate-180' : ''}`}
                            />
                          )}
                        </>
                      )}
                    </button>

                    {/* Submenus */}
                    {sidebarOpen && item.subItems && expandedMenus.has(item.id) && (
                      <div className="ml-4 mt-2 space-y-1 border-l border-slate-200/90 py-1 pl-4 dark:border-drk-700/80">
                        {item.subItems.map((subItem) => (
                          <button
                            key={subItem.id}
                            onClick={() => handleMenuClick(subItem.id)}
                            className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs transition-all font-medium ${
                              activeTab === subItem.id
                                ? 'bg-white/95 text-brand-700 shadow-sm dark:bg-white/10 dark:text-brand-300 font-semibold'
                                : 'text-slate-600 dark:text-slate-400 hover:bg-white/70 dark:hover:bg-white/5 hover:text-slate-900 dark:hover:text-white'
                            }`}
                          >
                            {subItem.icon}
                            <span>{subItem.label}</span>
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          ))}
        </nav>

        {/* User Section */}
        <div className="border-t border-white/70 bg-white/35 p-4 dark:border-white/10 dark:bg-black/10">
          <div className={`${sidebarOpen ? 'space-y-3' : 'space-y-2'}`}>
            {sidebarOpen && (
              <div className="rounded-[24px] border border-white/80 bg-white/82 px-3.5 py-3 dark:border-white/10 dark:bg-white/5">
                <p className="font-semibold text-slate-900 dark:text-white truncate text-sm">{user.name}</p>
                <p className="text-slate-500 dark:text-slate-400 text-xs truncate">{user.email}</p>
              </div>
            )}
            <button
              onClick={onLogout}
              className="w-full flex items-center justify-center gap-2 rounded-[24px] border border-danger-100 bg-danger-50/90 px-4 py-3 text-sm font-medium text-danger-600 transition-colors hover:bg-danger-100 dark:border-danger-500/20 dark:bg-danger-500/10 dark:text-danger-300 dark:hover:bg-danger-500/20"
            >
              <LogOut className="w-4 h-4" />
              {sidebarOpen && <span>Logout</span>}
            </button>
          </div>
        </div>

        {/* Toggle Button */}
        <button
          onClick={() => setSidebarOpen(!sidebarOpen)}
          className="border-t border-white/70 p-4 text-slate-600 transition-colors hover:bg-white/70 hover:text-brand-700 dark:border-white/10 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-white"
        >
          {sidebarOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
        </button>
      </div>

      {/* Main Content */}
      <div className="simple-screen flex-1 flex flex-col overflow-hidden pl-3">
        {/* Modern Header */}
        <div className="glass-card mb-3 rounded-lg border border-slate-200 px-6 py-4 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <h1 className="flex items-center gap-3 text-[1.7rem] font-heading font-bold tracking-[-0.04em] text-slate-950 dark:text-white">
                {activeMenuMeta.icon}
                {activeMenuMeta.label}
              </h1>
              <p className="mt-1.5 flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400">
                <span className="w-1.5 h-1.5 bg-green-500 rounded-full"></span>
                {commandDate} at {commandTime}
              </p>
            </div>
            <div className="flex items-center gap-3">
              <div className="hidden xl:flex flex-wrap items-center gap-2">
                {workspaceShortcuts.map((item) => (
                  <button
                    key={item.id}
                    onClick={() => handleMenuClick(item.id)}
                    className={`rounded-full px-3.5 py-2.5 text-xs font-semibold transition ${activeTab === item.id ? 'bg-slate-950 text-white dark:bg-white dark:text-slate-950' : 'bg-white/75 text-slate-600 hover:bg-white dark:bg-drk-850/75 dark:text-slate-300 dark:hover:bg-drk-800'}`}
                  >
                    {item.label}
                  </button>
                ))}
              </div>
              <button
                onClick={() => {
                  if (activeTab === 'group-lending' || activeTab === 'groups') {
                    loadGroups().catch((err) => console.error('Failed to refresh lending groups:', err));
                    loadCenters().catch((err) => console.error('Failed to refresh lending centers:', err));
                    loadGroupReports().catch((err) => console.error('Failed to refresh group lending reports:', err));
                    return;
                  }
                  window.location.reload();
                }}
                className="rounded-[22px] border border-white/80 bg-white/80 p-2.5 transition-colors hover:bg-white dark:border-white/10 dark:bg-white/5 dark:hover:bg-white/10"
              >
                <RefreshCw className="w-5 h-5 text-slate-600 dark:text-slate-400" />
              </button>
            </div>
          </div>
        </div>

        <div className="mb-3 grid grid-cols-1 gap-3 px-1 md:grid-cols-2 xl:grid-cols-4">
          {commandStatusCards.map((card) => (
            <div key={card.label} className="glass-card flex items-center justify-between rounded-[22px] border border-white/70 px-4 py-3 shadow-soft dark:border-white/10">
              <div>
                <p className="text-[11px] font-accent uppercase tracking-[0.24em] text-slate-500 dark:text-slate-400">{card.label}</p>
                <p className="mt-1 break-all text-sm font-semibold text-slate-900 dark:text-white">{card.value}</p>
              </div>
              <span className={`status-orb h-3 w-3 rounded-full ${
                card.tone === 'green'
                  ? 'bg-green-500'
                  : card.tone === 'amber'
                    ? 'bg-amber-500'
                    : card.tone === 'blue'
                      ? 'bg-blue-500'
                      : 'bg-slate-400'
              }`} />
            </div>
          ))}
        </div>

        {/* Error Alert */}
        {error && (
          <div className="mx-6 mt-4 flex items-start gap-3 rounded-2xl border border-danger-100 bg-danger-50 p-4 shadow-sm dark:border-danger-500/20 dark:bg-danger-500/10">
            <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <p className="text-sm text-red-800 dark:text-red-200 font-medium">{error}</p>
            </div>
            <button
              onClick={onErrorDismiss}
              className="text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-auto rounded-[28px]">
          {renderScreenContent()}
        </div>
      </div>
    </div>
  );
}

/**
 * Dashboard View Component
 */
function NavigationLanding({
  title,
  description,
  primaryActionLabel,
  onPrimaryAction,
  secondaryActionLabel,
  onSecondaryAction,
}: {
  title: string;
  description: string;
  primaryActionLabel: string;
  onPrimaryAction: () => void;
  secondaryActionLabel?: string;
  onSecondaryAction?: () => void;
}) {
  return (
    <div className="flex h-full items-center justify-center p-8">
      <div className="glass-card max-w-2xl rounded-[30px] border border-white/70 p-8 shadow-medium dark:border-white/10">
        <h2 className="text-[1.85rem] font-heading font-bold tracking-[-0.04em] text-slate-950 dark:text-white">{title}</h2>
        <p className="mt-3 text-sm leading-6 text-slate-600 dark:text-slate-300">{description}</p>
        <div className="mt-6 flex flex-wrap gap-3">
          <button onClick={onPrimaryAction} className="rounded-full bg-brand-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-brand-700">
            {primaryActionLabel}
          </button>
          {secondaryActionLabel && onSecondaryAction && (
            <button onClick={onSecondaryAction} className="rounded-full border border-slate-300 px-5 py-2.5 text-sm font-medium text-slate-700 hover:bg-white dark:border-white/10 dark:text-slate-200 dark:hover:bg-white/10">
              {secondaryActionLabel}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
function DashboardView({
  user,
  onNavigate,
  customers,
  accounts,
  loans,
  transactions,
  auditLogs,
  isLoading,
}: {
  user: User;
  onNavigate: (id: string) => void;
  customers: any[];
  accounts: any[];
  loans: any[];
  transactions: any[];
  auditLogs: any[];
  isLoading: boolean;
}) {
  const processFlowSteps = [
    {
      step: '01',
      icon: <Users size={20} />,
      label: 'Onboard Client',
      description: 'Capture KYC profile and customer details.',
      color: 'bg-brand-600',
      id: 'clients-onboard',
    },
    {
      step: '02',
      icon: <FileText size={20} />,
      label: 'Open Account',
      description: 'Create savings, current, or product-linked accounts.',
      color: 'bg-brand-600',
      id: 'accounts-create',
    },
    {
      step: '03',
      icon: <ArrowRightLeft size={20} />,
      label: 'Post Transactions',
      description: 'Process deposits, withdrawals, and transfers.',
      color: 'bg-brand-700',
      id: 'teller-deposit',
    },
    {
      step: '04',
      icon: <DollarSign size={20} />,
      label: 'Process Loans',
      description: 'Run origination and manage loan pipeline activities.',
      color: 'bg-brand-600',
      id: 'loans-pipeline',
    },
    {
      step: '05',
      icon: <CheckCircle size={20} />,
      label: 'Review Approvals',
      description: 'Complete credit and operational approvals.',
      color: 'bg-brand-700',
      id: 'approvals',
    },
    {
      step: '06',
      icon: <TrendingUp size={20} />,
      label: 'Generate Reports',
      description: 'Review operational, compliance, and financial outputs.',
      color: 'bg-slate-700',
      id: 'reporting',
    },
  ];

  const currencyFormatter = new Intl.NumberFormat('en-GH', {
    style: 'currency',
    currency: 'GHS',
    maximumFractionDigits: 0,
  });

  const activeLoanCount = loans.filter((loan) => {
    const status = String(loan.status || '').toUpperCase();
    return ['ACTIVE', 'APPROVED', 'DISBURSED', 'CURRENT'].includes(status);
  }).length;

  const totalDeposits = accounts.reduce((sum, account) => sum + Number(account.balance || 0), 0);
  const todayKey = new Date().toISOString().slice(0, 10);
  const todaysTransactions = transactions.filter((transaction) => String(transaction.date || '').slice(0, 10) === todayKey);
  const exceptionAudits = auditLogs.filter((log) => {
    const status = String(log.status || '').toUpperCase();
    return status && status !== 'SUCCESS' && status !== 'COMPLETED';
  }).length;

  const transactionVolumeToday = todaysTransactions.reduce(
    (sum, transaction) => sum + Number(transaction.amount || 0),
    0,
  );

  const recentTransactions = [...transactions]
    .sort((a, b) => new Date(b.date || 0).getTime() - new Date(a.date || 0).getTime())
    .slice(0, 5);

  const recentAudits = [...auditLogs]
    .sort((a, b) => new Date(b.timestamp || b.date || 0).getTime() - new Date(a.timestamp || a.date || 0).getTime())
    .slice(0, 5);

  const metricCards = [
    {
      label: 'Active Clients',
      value: customers.length.toLocaleString(),
      helper: `${accounts.length} live accounts`,
      icon: <Users className="h-6 w-6 text-brand-600 dark:text-brand-400" />,
      tone: 'bg-brand-50 dark:bg-brand-500/10',
    },
    {
      label: 'Deposit Balances',
      value: currencyFormatter.format(totalDeposits),
      helper: `${activeLoanCount} active loans`,
      icon: <TrendingUp className="h-6 w-6 text-success-600 dark:text-success-400" />,
      tone: 'bg-success-50 dark:bg-success-500/10',
    },
    {
      label: "Today's Transactions",
      value: todaysTransactions.length.toLocaleString(),
      helper: `${currencyFormatter.format(transactionVolumeToday)} posted today`,
      icon: <ArrowRightLeft className="h-6 w-6 text-warning-600 dark:text-warning-400" />,
      tone: 'bg-warning-50 dark:bg-warning-500/10',
    },
    {
      label: 'Audit Exceptions',
      value: exceptionAudits.toLocaleString(),
      helper: `${auditLogs.length} recent audit events`,
      icon: <Shield className="h-6 w-6 text-warning-600 dark:text-warning-500" />,
      tone: 'bg-danger-50 dark:bg-danger-500/10',
    },
  ];

  const renderAuditStatus = (status: string) => {
    const normalized = String(status || 'UNKNOWN').toUpperCase();
    if (normalized === 'SUCCESS' || normalized === 'COMPLETED') {
      return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300';
    }
    if (normalized === 'PENDING') {
      return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300';
    }
    return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300';
  };

  return (
    <div className="min-h-full space-y-6 p-4 sm:p-6">
      <div className="dashboard-sheen rounded-2xl border border-slate-200 p-6 text-slate-900 shadow-soft">
        <div className="flex items-center justify-between gap-6">
          <div>
            <p className="mb-1 font-accent text-[11px] uppercase tracking-[0.24em] text-slate-500">Operations Overview</p>
            <h1 className="mb-2 text-3xl font-heading font-semibold text-slate-900 sm:text-4xl">{user.name}</h1>
            <p className="flex items-center gap-2 text-sm text-slate-600">
              <span className="h-2 w-2 animate-pulse rounded-full bg-green-400"></span>
              {customers.length} customers, {transactions.length} transactions, and {loans.length} loans in the active operating view
            </p>
          </div>
          <div className="hidden items-center gap-4 text-right md:flex">
            <div>
              <p className="mb-1 text-xs uppercase tracking-[0.14em] text-slate-500">Data status</p>
              <p className="text-base font-semibold text-slate-900">{isLoading ? 'Refreshing data services...' : 'Data services connected'}</p>
            </div>
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-slate-100">
              {isLoading ? (
                <RefreshCw size={28} className="animate-spin text-white" />
              ) : (
                <CheckCircle size={32} className="text-green-300" />
              )}
            </div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {metricCards.map((card) => (
          <div key={card.label} className="glass-card card-hover rounded-[28px] border border-white/70 p-6">
            <div className="mb-4 flex items-center justify-between">
              <div className={`flex h-12 w-12 items-center justify-center rounded-2xl ring-1 ring-white/50 dark:ring-white/10 ${card.tone}`}>
                {card.icon}
              </div>
              {isLoading && <RefreshCw className="h-4 w-4 animate-spin text-slate-400" />}
            </div>
            <p className="mb-1 text-sm font-medium text-slate-600 dark:text-slate-400">{card.label}</p>
            <p className="text-[1.85rem] font-heading font-bold tracking-[-0.04em] text-slate-950 dark:text-white">{card.value}</p>
            <p className="mt-2 text-xs uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">{card.helper}</p>
          </div>
        ))}
      </div>

      <div className="glass-card card-hover rounded-[28px] border border-white/70 p-6">
        <h2 className="mb-4 flex items-center gap-2 text-lg font-heading font-bold tracking-[-0.03em] text-slate-950 dark:text-white">
          <Zap className="h-5 w-5 text-brand-500" />
          Process Flow
        </h2>
        <p className="mb-4 text-sm text-slate-600 dark:text-slate-300">
          Follow the standard workflow from onboarding through reporting. Each step opens the corresponding operational screen.
        </p>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {processFlowSteps.map((action) => (
            <button
              key={action.id}
              onClick={() => onNavigate(action.id)}
              className={`${action.color} flex flex-col items-start gap-2 rounded-[24px] p-4 text-left text-white transition-all hover:-translate-y-0.5 hover:shadow-lg active:scale-95`}
            >
              <div className="flex w-full items-center justify-between">
                <span className="rounded-full border border-white/30 bg-white/10 px-2 py-1 text-[10px] font-accent uppercase tracking-[0.18em]">
                  Step {action.step}
                </span>
                {action.icon}
              </div>
              <span className="text-sm font-semibold">{action.label}</span>
              <span className="text-xs text-white/85">{action.description}</span>
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <div className="glass-card card-hover rounded-[28px] border border-white/70 p-6">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Recent Transactions</h2>
            <button onClick={() => onNavigate('transactions')} className="text-sm text-brand-600 hover:underline dark:text-brand-400">
              View all
            </button>
          </div>
          <div className="space-y-3">
            {recentTransactions.length === 0 ? (
              <p className="text-sm text-slate-500 dark:text-slate-400">No transaction activity has been posted yet.</p>
            ) : (
              recentTransactions.map((transaction) => (
                <div key={transaction.id} className="flex items-center justify-between rounded-[22px] border border-white/75 bg-white/55 px-4 py-3 dark:border-white/10 dark:bg-white/5">
                  <div>
                    <p className="font-medium text-slate-900 dark:text-white">{transaction.type}</p>
                    <p className="text-xs text-slate-500 dark:text-slate-400">
                      {transaction.accountId} | {transaction.date ? new Date(transaction.date).toLocaleString() : 'Pending timestamp'}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold text-slate-900 dark:text-white">
                      {new Intl.NumberFormat('en-GH', { style: 'currency', currency: transaction.currency || 'GHS' }).format(Number(transaction.amount || 0))}
                    </p>
                    <p className="text-xs text-slate-500 dark:text-slate-400">{transaction.status || 'POSTED'}</p>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        <div className="glass-card card-hover rounded-[28px] border border-white/70 p-6">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Recent Audit Activity</h2>
            <button onClick={() => onNavigate('audit')} className="text-sm text-brand-600 hover:underline dark:text-brand-400">
              View audit trail
            </button>
          </div>
          <div className="space-y-3">
            {recentAudits.length === 0 ? (
              <p className="text-sm text-slate-500 dark:text-slate-400">No audit events are available for this session.</p>
            ) : (
              recentAudits.map((log) => (
                <div key={log.id} className="rounded-[22px] border border-white/75 bg-white/55 px-4 py-3 dark:border-white/10 dark:bg-white/5">
                  <div className="flex items-center justify-between gap-4">
                    <div>
                      <p className="font-medium text-slate-900 dark:text-white">{log.action || 'System Activity'}</p>
                      <p className="text-xs text-slate-500 dark:text-slate-400">
                        {log.user || 'System'} | {log.module || 'Operations'}
                      </p>
                    </div>
                    <span className={`rounded-full px-2 py-1 text-xs font-semibold ${renderAuditStatus(log.status)}`}>
                      {log.status || 'UNKNOWN'}
                    </span>
                  </div>
                  <p className="mt-2 text-sm text-slate-600 dark:text-slate-300">{log.details || log.description || 'No detail provided.'}</p>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
// Export icon components that are missing
const UserPlus = (props: React.ComponentProps<typeof Users>) => <Users {...props} />;
const Plus = (props: React.ComponentProps<typeof CheckSquare>) => <CheckSquare {...props} />;

function MetricMini({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-[20px] border border-white/70 bg-white/70 px-4 py-3 dark:border-white/10 dark:bg-white/5">
      <p className="text-[11px] font-accent uppercase tracking-[0.22em] text-slate-500 dark:text-slate-400">{label}</p>
      <p className="mt-1 text-lg font-semibold text-slate-900 dark:text-white">{value}</p>
    </div>
  );
}

















































