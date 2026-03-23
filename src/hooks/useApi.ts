import { useState, useEffect, useCallback } from 'react';
import { authService, User } from '../services/authService';
import { reportService, CustomerSegmentationDTO, ProductAnalyticsDTO, BalanceSheetDTO, IncomeStatementDTO, DailyPositionReportDTO, RegulatoryReturnDTO, LargeExposureReportDTO, PrudentialReturnDTO, ReportRunDTO, EnterpriseReportCatalogItem, EnterpriseReportExecutionRequest, EnterpriseReportExecutionResponse, EnterpriseReportHistoryItem, ReportFavoriteItem, ReportFilterPresetItem, CrbDataQualityDashboardDTO, EnterpriseReportDownloadResult } from '../services/reportService';
import { treasuryService, TreasuryPosition, FxRate, FxTrade, Investment, RiskMetric } from '../services/treasuryService';
import { vaultService, BranchVaultDto, VaultCountRequest, VaultTransactionRequest } from '../services/vaultService';
import { adminService } from '../services/adminService';
import { loanService, Loan, DisburseLoanRequest, LoanRepayRequest, LoanScheduleDto } from '../services/loanService';
import { glService, GlAccount, JournalEntry, CreateGlAccountRequest, PostJournalEntryRequest } from '../services/glService';
import { investmentService } from '../services/investmentService';
import { StaffUser, Role, Branch, SystemConfig, RegulatoryChartSeedResponse } from '../../types';
import { ApiError } from '../services/httpClient';

// Authentication Hook
export function useAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticating, setIsAuthenticating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const checkAuth = async () => {
      const currentUser = authService.getUser();
      const isValid = await authService.validateToken();

      if (currentUser && isValid) {
        setUser(currentUser);
        setIsAuthenticated(true);
      } else {
        authService.clearAuth();
        setIsAuthenticated(false);
      }

      setIsLoading(false);
    };

    checkAuth();
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    try {
      setError(null);
      setIsAuthenticating(true);
      const response = await authService.login({ email, password });
      if (response.token && response.user) {
        setUser(response.user);
        setIsAuthenticated(true);
      } else {
        setUser(null);
        setIsAuthenticated(false);
      }
      return response;
    } catch (err) {
      const message = err instanceof ApiError
        ? err.data?.message || err.data?.error || err.message || 'Login failed'
        : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setIsAuthenticating(false);
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await authService.logout();
      setUser(null);
      setIsAuthenticated(false);
    } catch (err) {
      console.error('Logout error:', err);
    }
  }, []);

  const verifyMfa = useCallback(async (mfaToken: string, code: string) => {
    try {
      setError(null);
      setIsAuthenticating(true);
      const response = await authService.verifyMfa({ mfaToken, code });
      if (response.token && response.user) {
        setUser(response.user);
        setIsAuthenticated(true);
      } else {
        setUser(null);
        setIsAuthenticated(false);
      }
      return response;
    } catch (err) {
      const message = err instanceof ApiError
        ? err.data?.message || err.data?.error || err.message || 'Verification failed'
        : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setIsAuthenticating(false);
    }
  }, []);

  const resendMfa = useCallback(async (mfaToken: string) => {
    try {
      setError(null);
      setIsAuthenticating(true);
      return await authService.resendMfa({ mfaToken });
    } catch (err) {
      const message = err instanceof ApiError
        ? err.data?.message || err.data?.error || err.message || 'Could not resend verification code'
        : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setIsAuthenticating(false);
    }
  }, []);

  const syncAuthState = useCallback(() => {
    const currentUser = authService.getUser();
    const token = authService.getToken();
    setUser(currentUser);
    setIsAuthenticated(!!token && !!currentUser);
  }, []);

  return { user, isAuthenticated, isLoading, isAuthenticating, error, login, verifyMfa, resendMfa, logout, syncAuthState };
}

// Reports Hook
export function useReports() {
  const [catalogLoading, setCatalogLoading] = useState(false);
  const [catalogError, setCatalogError] = useState<string | null>(null);

  const getReportMessage = (err: unknown, fallback: string) => {
    if (err instanceof ApiError) {
      return err.data?.message || err.data?.error || err.message || fallback;
    }

    return (err as Error).message || fallback;
  };

  const getReportCatalog = useCallback(async () => {
    try {
      setCatalogLoading(true);
      setCatalogError(null);
      return await reportService.getReportCatalog();
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load reports');
      setCatalogError(message);
      throw err;
    } finally {
      setCatalogLoading(false);
    }
  }, []);

  const getCustomerSegmentation = useCallback(async (asOfDate: string) => {
    try {
      setCatalogError(null);
      return await reportService.getCustomerSegmentation(asOfDate);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load segmentation');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getProductAnalytics = useCallback(async (asOfDate: string) => {
    try {
      setCatalogError(null);
      return await reportService.getProductAnalytics(asOfDate);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load product analytics');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getBalanceSheet = useCallback(async (asOfDate: string) => {
    try {
      setCatalogError(null);
      return await reportService.getBalanceSheet(asOfDate);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load balance sheet');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getIncomeStatement = useCallback(async (periodStart: string, periodEnd: string) => {
    try {
      setCatalogError(null);
      return await reportService.getIncomeStatement(periodStart, periodEnd);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load income statement');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getDailyPositionReport = useCallback(async (reportDate: string) => {
    try {
      setCatalogError(null);
      return await reportService.getDailyPositionReport(reportDate);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load daily position report');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getRegulatoryReturns = useCallback(async (returnType?: string) => {
    try {
      setCatalogError(null);
      return await reportService.getRegulatoryReturns(returnType);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load regulatory returns');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getPrudentialReturn = useCallback(async (asOfDate: string): Promise<PrudentialReturnDTO> => {
    try {
      setCatalogError(null);
      return await reportService.getPrudentialReturn(asOfDate);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load prudential return');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getLargeExposureReport = useCallback(async (asOfDate: string): Promise<LargeExposureReportDTO> => {
    try {
      setCatalogError(null);
      return await reportService.getLargeExposureReport(asOfDate);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load large exposure report');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getEnterpriseCatalog = useCallback(async (): Promise<EnterpriseReportCatalogItem[]> => {
    try {
      setCatalogError(null);
      return await reportService.getEnterpriseCatalog();
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load enterprise report catalog');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const executeEnterpriseReport = useCallback(async (reportCode: string, request: EnterpriseReportExecutionRequest): Promise<EnterpriseReportExecutionResponse> => {
    try {
      setCatalogError(null);
      return await reportService.executeEnterpriseReport(reportCode, request);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to execute enterprise report');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const exportEnterpriseReport = useCallback(async (reportCode: string, format: string, request: EnterpriseReportExecutionRequest): Promise<EnterpriseReportDownloadResult> => {
    try {
      setCatalogError(null);
      return await reportService.exportEnterpriseReport(reportCode, format, request);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to export report');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getEnterpriseHistory = useCallback(async (): Promise<EnterpriseReportHistoryItem[]> => {
    try {
      setCatalogError(null);
      return await reportService.getEnterpriseHistory();
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load report history');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getEnterpriseFavorites = useCallback(async (): Promise<ReportFavoriteItem[]> => {
    try {
      setCatalogError(null);
      return await reportService.getEnterpriseFavorites();
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load favorites');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const addEnterpriseFavorite = useCallback(async (reportCode: string): Promise<void> => {
    try {
      setCatalogError(null);
      await reportService.addEnterpriseFavorite(reportCode);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to save favorite');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const removeEnterpriseFavorite = useCallback(async (reportCode: string): Promise<void> => {
    try {
      setCatalogError(null);
      await reportService.removeEnterpriseFavorite(reportCode);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to remove favorite');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getEnterprisePresets = useCallback(async (reportCode: string): Promise<ReportFilterPresetItem[]> => {
    try {
      setCatalogError(null);
      return await reportService.getEnterprisePresets(reportCode);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load presets');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const saveEnterprisePreset = useCallback(async (reportCode: string, presetName: string, parameters: Record<string, string>): Promise<ReportFilterPresetItem> => {
    try {
      setCatalogError(null);
      return await reportService.saveEnterprisePreset(reportCode, presetName, parameters);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to save preset');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const deleteEnterprisePreset = useCallback(async (presetId: string): Promise<void> => {
    try {
      setCatalogError(null);
      await reportService.deleteEnterprisePreset(presetId);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to delete preset');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const getCrbDataQualityDashboard = useCallback(async (): Promise<CrbDataQualityDashboardDTO> => {
    try {
      setCatalogError(null);
      return await reportService.getCrbDataQualityDashboard();
    } catch (err) {
      const message = getReportMessage(err, 'Failed to load CRB data quality');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const generateReport = useCallback(async (reportId: number, parameters: Record<string, string>, format = 'JSON'): Promise<ReportRunDTO> => {
    try {
      setCatalogError(null);
      return await reportService.generateReport(reportId, parameters, format);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to generate report');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const approveRegulatoryReturn = useCallback(async (returnId: number): Promise<RegulatoryReturnDTO> => {
    try {
      setCatalogError(null);
      return await reportService.approveRegulatoryReturn(returnId);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to approve regulatory return');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const rejectRegulatoryReturn = useCallback(async (returnId: number, reason: string): Promise<RegulatoryReturnDTO> => {
    try {
      setCatalogError(null);
      return await reportService.rejectRegulatoryReturn(returnId, reason);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to reject regulatory return');
      setCatalogError(message);
      throw err;
    }
  }, []);

  const submitRegulatoryReturn = useCallback(async (returnId: number): Promise<RegulatoryReturnDTO> => {
    try {
      setCatalogError(null);
      return await reportService.submitRegulatoryReturn(returnId);
    } catch (err) {
      const message = getReportMessage(err, 'Failed to submit regulatory return');
      setCatalogError(message);
      throw err;
    }
  }, []);

  return {
    catalogLoading,
    catalogError,
    getReportCatalog,
    getCustomerSegmentation,
    getProductAnalytics,
    getBalanceSheet,
    getIncomeStatement,
    getDailyPositionReport,
    getRegulatoryReturns,
    getPrudentialReturn,
    getLargeExposureReport,
    getEnterpriseCatalog,
    executeEnterpriseReport,
    exportEnterpriseReport,
    getEnterpriseHistory,
    getEnterpriseFavorites,
    addEnterpriseFavorite,
    removeEnterpriseFavorite,
    getEnterprisePresets,
    saveEnterprisePreset,
    deleteEnterprisePreset,
    getCrbDataQualityDashboard,
    generateReport,
    approveRegulatoryReturn,
    rejectRegulatoryReturn,
    submitRegulatoryReturn,
  };
}

// Treasury Hook
export function useTreasury() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getTreasuryPositions = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      return await treasuryService.getTreasuryPositions();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load positions' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getFxRates = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      return await treasuryService.getFxRates();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load FX rates' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const createFxTrade = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await treasuryService.createFxTrade(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create trade' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getFxTrades = useCallback(async () => {
    try {
      setError(null);
      return await treasuryService.getFxTrades();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load trades' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const getInvestments = useCallback(async () => {
    try {
      setError(null);
      return await treasuryService.getInvestments();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load investments' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const createInvestment = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await treasuryService.createInvestment(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create investment' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getRiskMetrics = useCallback(async () => {
    try {
      setError(null);
      return await treasuryService.getRiskMetrics();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load risk metrics' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  return {
    loading,
    error,
    getTreasuryPositions,
    getFxRates,
    createFxTrade,
    getFxTrades,
    getInvestments,
    createInvestment,
    getRiskMetrics,
  };
}

// Vault Management Hook
export function useVault() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getBranchVaults = useCallback(async (branchId: string) => {
    try {
      setError(null);
      setLoading(true);
      return await vaultService.getBranchVaults(branchId);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load branch vaults' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getAllVaults = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      return await vaultService.getAllVaults();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load all vaults' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const recordVaultCount = useCallback(async (data: VaultCountRequest) => {
    try {
      setError(null);
      setLoading(true);
      return await vaultService.recordVaultCount(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to record vault count' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const processVaultTransaction = useCallback(async (data: VaultTransactionRequest) => {
    try {
      setError(null);
      setLoading(true);
      return await vaultService.processVaultTransaction(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to process vault transaction' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    loading,
    error,
    getAllVaults,
    getBranchVaults,
    recordVaultCount,
    processVaultTransaction,
  };
}

// Generic Data Fetching Hook
export function useFetch<T>(
  fetchFn: () => Promise<T>,
  dependencies: any[] = []
) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const load = async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await fetchFn();
        if (isMounted) {
          setData(result);
        }
      } catch (err) {
        if (isMounted) {
          const message = err instanceof ApiError
            ? err.data?.message || err.data?.error || err.message
            : (err as Error).message;
          setError(message || 'Failed to load data');
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    load();

    return () => {
      isMounted = false;
    };
  }, dependencies);

  return { data, loading, error };
}

// Admin Hook
export function useAdmin() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getUsers = useCallback(async () => {
    try {
      setError(null);
      return await adminService.getUsers();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load users' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const createUser = useCallback(async (data: Partial<StaffUser>) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.createUser(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create user' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);
  const updateUser = useCallback(async (id: string, data: Partial<StaffUser>) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.updateUser(id, data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to update user' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const updateUserRole = useCallback(async (id: string, roleId: string) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.updateUserRole(id, roleId);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to update user role' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const deleteUser = useCallback(async (id: string) => {
    try {
      setError(null);
      setLoading(true);
      await adminService.deleteUser(id);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to delete user' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const resetPassword = useCallback(async (id: string) => {
    try {
      setError(null);
      setLoading(true);
      await adminService.resetPassword(id);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to reset password' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getRoles = useCallback(async () => {
    try {
      setError(null);
      return await adminService.getRoles();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load roles' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const createRole = useCallback(async (data: Partial<Role>) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.createRole(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create role' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const updateRole = useCallback(async (id: string, data: Partial<Role>) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.updateRole(id, data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to update role' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getBranches = useCallback(async () => {
    try {
      setError(null);
      return await adminService.getBranches();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load branches' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const createBranch = useCallback(async (data: Partial<Branch>) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.createBranch(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create branch' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const updateBranch = useCallback(async (id: string, data: Partial<Branch>) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.updateBranch(id, data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to update branch' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const deleteBranch = useCallback(async (id: string) => {
    try {
      setError(null);
      setLoading(true);
      await adminService.deleteBranch(id);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to delete branch' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getSystemConfig = useCallback(async () => {
    try {
      setError(null);
      return await adminService.getSystemConfig();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load system config' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const updateSystemConfig = useCallback(async (config: SystemConfig) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.updateSystemConfig(config);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to update system config' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const seedRegulatoryChartOfAccounts = useCallback(async (regionCode: string = 'GH'): Promise<RegulatoryChartSeedResponse> => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.seedRegulatoryChartOfAccounts(regionCode);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to seed regulatory chart of accounts' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getPrivilegeLeases = useCallback(async (staffId: string) => {
    try {
      setError(null);
      return await adminService.getPrivilegeLeases(staffId);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load privilege leases' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const createPrivilegeLease = useCallback(async (data: {
    staffId: string;
    permission: string;
    reason: string;
    approvedBy: string;
    startsAt?: string;
    expiresAt: string;
  }) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.createPrivilegeLease(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create privilege lease' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const revokePrivilegeLease = useCallback(async (leaseId: string, revokedBy: string) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.revokePrivilegeLease(leaseId, revokedBy);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to revoke privilege lease' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getProducts = useCallback(async () => {
    try {
      setError(null);
      return await adminService.getProducts();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load products' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const createProduct = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.createProduct(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create product' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const updateProduct = useCallback(async (id: string, data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await adminService.updateProduct(id, data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to update product' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getAuditLogs = useCallback(async (limit?: number) => {
    try {
      setError(null);
      return await adminService.getAuditLogs(limit);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load audit logs' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  return {
    loading,
    error,
    getUsers,
    createUser,
    updateUser,
    updateUserRole,
    deleteUser,
    resetPassword,
    getRoles,
    createRole,
    updateRole,
    getBranches,
    createBranch,
    updateBranch,
    deleteBranch,
    getSystemConfig,
    updateSystemConfig,
    seedRegulatoryChartOfAccounts,
    getPrivilegeLeases,
    createPrivilegeLease,
    revokePrivilegeLease,
    getProducts,
    createProduct,
    updateProduct,
    getAuditLogs
  };
}

// Loans Hook
export function useLoans() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getLoans = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.getLoans();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load loans' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getLoan = useCallback(async (id: string) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.getLoan(id);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load loan details' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const disburseLoan = useCallback(async (data: DisburseLoanRequest) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.disburseLoan(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to disburse loan' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const repayLoan = useCallback(async (id: string, data: LoanRepayRequest) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.repayLoan(id, data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to repay loan' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getLoanSchedule = useCallback(async (id: string) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.getLoanSchedule(id);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load loan schedule' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const applyLoan = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.applyLoan(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to apply loan' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const appraiseLoan = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.appraiseLoan(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to appraise loan' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const approveLoan = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.approveLoan(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to approve loan' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const checkCredit = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.checkCredit(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to check credit' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getDelinquencyDashboard = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.getDelinquencyDashboard();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load delinquency dashboard' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getProfitabilityReport = useCallback(async (fromDate?: string, toDate?: string) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.getProfitabilityReport(fromDate, toDate);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load loan profitability report' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getBalanceSheetReport = useCallback(async (asOfDate?: string) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.getBalanceSheetReport(asOfDate);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load loan balance sheet report' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getGlPostings = useCallback(async (loanId: string) => {
    try {
      setError(null);
      setLoading(true);
      return await loanService.getGlPostings(loanId);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load loan GL postings' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    loading,
    error,
    getLoans,
    getLoan,
    disburseLoan,
    repayLoan,
    getLoanSchedule,
    applyLoan,
    appraiseLoan,
    approveLoan,
    checkCredit,
    getDelinquencyDashboard,
    getProfitabilityReport,
    getBalanceSheetReport,
    getGlPostings,
  };
}

// GL Hook
export function useGl() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getAccounts = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      return await glService.getAccounts();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to fetch GL accounts' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const createAccount = useCallback(async (data: CreateGlAccountRequest) => {
    try {
      setError(null);
      setLoading(true);
      return await glService.createAccount(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create GL account' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getJournalEntries = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      return await glService.getJournalEntries();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to fetch journal entries' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const postJournalEntry = useCallback(async (data: PostJournalEntryRequest) => {
    try {
      setError(null);
      setLoading(true);
      return await glService.postJournalEntry(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to post journal entry' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    loading,
    error,
    getAccounts,
    createAccount,
    getJournalEntries,
    postJournalEntry,
  };
}

// Investment Hook
export function useInvestments() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getInvestments = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      return await investmentService.getInvestments();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load investments' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const createInvestment = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await investmentService.createInvestment(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create investment' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const liquidateInvestment = useCallback(async (id: string) => {
    try {
      setError(null);
      setLoading(true);
      return await investmentService.liquidateInvestment(id);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to liquidate investment' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getFixedDeposits = useCallback(async () => {
    try {
      setError(null);
      return await investmentService.getFixedDeposits();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load fixed deposits' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  const createFixedDeposit = useCallback(async (data: any) => {
    try {
      setError(null);
      setLoading(true);
      return await investmentService.createFixedDeposit(data);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to create fixed deposit' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const renewFixedDeposit = useCallback(async (id: string, principal: number, tenure: number) => {
    try {
      setError(null);
      setLoading(true);
      return await investmentService.renewFixedDeposit(id, principal, tenure);
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to renew fixed deposit' : (err as Error).message;
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const getPortfolioSummary = useCallback(async () => {
    try {
      setError(null);
      return await investmentService.getPortfolioSummary();
    } catch (err) {
      const message = err instanceof ApiError ? 'Failed to load portfolio summary' : (err as Error).message;
      setError(message);
      throw err;
    }
  }, []);

  return {
    loading,
    error,
    getInvestments,
    createInvestment,
    liquidateInvestment,
    getFixedDeposits,
    createFixedDeposit,
    renewFixedDeposit,
    getPortfolioSummary,
  };
}










