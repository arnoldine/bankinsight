import { useState, useEffect } from 'react';
import { buildApiBaseUrl } from '../lib/configStore';
import { Permissions } from '../lib/Permissions';
import { Customer, Account, Loan, GLAccount, JournalEntry, AuditLog, Transaction, StaffUser, Group, Branch, Product, ApprovalRequest, Workflow, Role, SystemConfig } from '../types';
import { authService } from '../src/services/authService';

export const useBankingSystem = () => {
    const [currentUser, setCurrentUser] = useState<StaffUser | null>(null);
    const [authError, setAuthError] = useState<string | null>(null);
    const [systemConfig, setSystemConfig] = useState<SystemConfig | null>(null);
    const [authToken, setAuthToken] = useState<string | null>(() => authService.getToken() || localStorage.getItem('bankinsight_token'));

    const [businessDate, setBusinessDate] = useState(new Date().toISOString().split('T')[0]);
    const [branches, setBranches] = useState<Branch[]>([]);
    const [customers, setCustomers] = useState<Customer[]>([]);
    const [groups, setGroups] = useState<Group[]>([]);
    const [accounts, setAccounts] = useState<Account[]>([]);
    const [loans, setLoans] = useState<Loan[]>([]);
    const [glAccounts, setGlAccounts] = useState<GLAccount[]>([]);
    const [journalEntries, setJournalEntries] = useState<JournalEntry[]>([]);
    const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]);
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [products, setProducts] = useState<Product[]>([]);
    const [roles, setRoles] = useState<Role[]>([]);
    const [staff, setStaff] = useState<StaffUser[]>([]);
    const [approvalRequests, setApprovalRequests] = useState<ApprovalRequest[]>([]);
    const [workflows, setWorkflows] = useState<Workflow[]>([]);
    const [devTasks, setDevTasks] = useState<any[]>([]);

    // Fetch helper
    const apiFetch = async (endpoint: string, options?: RequestInit) => {
        const response = await fetch(`${buildApiBaseUrl()}${endpoint}`, {
            ...options,
            headers: {
                'Content-Type': 'application/json',
                ...(authToken ? { Authorization: `Bearer ${authToken}` } : {}),
                ...options?.headers,
            }
        });

        if (!response.ok) {
            if (response.status === 401) {
                logout();
                throw new Error("Session expired. Please log in again.");
            }
            if (response.status === 403) {
                throw new Error("Access Denied: You lack sufficient permissions for this action.");
            }
            let errMessage = 'API Error';
            try {
                const err = await response.json();
                errMessage = err.message || errMessage;
            } catch (e) {
                // Ignore parse error on non-JSON error responses
            }
            throw new Error(errMessage);
        }

        if (response.status === 204) {
            return null;
        }

        const text = await response.text();
        try {
            return text ? JSON.parse(text) : null;
        } catch (e) {
            return text;
        }
    };

    const login = async (email: string, password: string) => {
        try {
            const data = await apiFetch('/auth/login', {
                method: 'POST',
                body: JSON.stringify({ email, password })
            });
            setCurrentUser(data.user);
            setAuthToken(data.token);
            localStorage.setItem('bankinsight_token', data.token);
            setAuthError(null);
        } catch (err: any) {
            setAuthError(err.message);
        }
    };

    const logout = () => {
        setCurrentUser(null);
        setAuthToken(null);
        localStorage.removeItem('bankinsight_token');
    };

    const hasPermission = (permission: string): boolean => {
        if (!currentUser || !currentUser.permissions) return false;
        return currentUser.permissions.includes(permission);
    };

    // Setup initial fetch on mount
    useEffect(() => {
        if (!currentUser) return;
        const loadData = async () => {
            try {
                const promises = [];
                promises.push(apiFetch('/users').then(setStaff).catch(() => []));
                promises.push(apiFetch('/config').then((configData) => {
                    // Parse DB config keys into a SystemConfig object
                    const parsedConfig = configData.reduce((acc: any, item: any) => {
                        try {
                            acc[item.key] = JSON.parse(item.value);
                        } catch {
                            acc[item.key] = item.value;
                        }
                        return acc;
                    }, { amlThreshold: 10000, dbProvider: 'MYSQL', auth: { enabled: false, provider: 'LOCAL', tenantId: '', clientId: '', ldapServer: '' } });
                    setSystemConfig(parsedConfig);
                }).catch(() => []));
                
                // Conditionally fetch data based on user permissions to prevent overfetching vulnerabilities
                if (hasPermission(Permissions.Accounts.View)) {
                    promises.push(apiFetch('/customers').then(setCustomers).catch(() => []));
                    promises.push(apiFetch('/accounts').then(setAccounts).catch(() => []));
                    promises.push(apiFetch('/transactions').then(setTransactions).catch(() => []));
                    promises.push(apiFetch('/groups').then(setGroups).catch(() => []));
                }
                
                if (hasPermission(Permissions.Loans.View)) {
                    promises.push(apiFetch('/loans').then(setLoans).catch(() => []));
                }
                
                if (hasPermission(Permissions.GeneralLedger.View)) {
                    promises.push(apiFetch('/gl/accounts').then(setGlAccounts).catch(() => []));
                    promises.push(apiFetch('/gl/journals').then(setJournalEntries).catch(() => []));
                }
                
                if (hasPermission(Permissions.Roles.View)) {
                    promises.push(apiFetch('/products').then(setProducts).catch(() => []));
                    promises.push(apiFetch('/roles').then(setRoles).catch(() => []));
                    promises.push(apiFetch('/workflows').then(setWorkflows).catch(() => []));
                }
                
                if (hasPermission(Permissions.Loans.Approve) || hasPermission(Permissions.Workflow.Approve)) {
                    promises.push(apiFetch('/approvals').then(setApprovalRequests).catch(() => []));
                }

                await Promise.all(promises);


            } catch (err) {
                console.error('Failed to load initial data', err);
            }
        };
        loadData();
    }, [currentUser]);

    const processTellerTransaction = async (accountId: string, type: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'LOAN_REPAYMENT', amount: number, narration: string, tellerId: string) => {
        try {
            const res = await apiFetch('/transactions', {
                method: 'POST',
                body: JSON.stringify({ account_id: accountId, type, amount, narration, teller_id: tellerId })
            });

            // Optimistic update
            setTransactions(prev => [res, ...prev]);
            setAccounts(prev => prev.map(a =>
                a.id === accountId
                    ? { ...a, balance: type === 'WITHDRAWAL' ? Number(a.balance) - Number(amount) : Number(a.balance) + Number(amount) }
                    : a
            ));

            return { success: true, id: res.id, message: 'Transaction Posted', status: 'POSTED' as const };
        } catch (e: any) {
            throw e;
        }
    };

    const createCustomer = async (customerData: Omit<Customer, 'id'>) => {
        try {
            const res = await apiFetch('/customers', {
                method: 'POST',
                body: JSON.stringify(customerData)
            });
            setCustomers(prev => [res, ...prev]);
            return res.id;
        } catch (e: any) { throw e; }
    };

    const updateCustomer = async (id: string, updates: Partial<Customer>) => {
        try {
            const res = await apiFetch(`/customers/${id}`, {
                method: 'PUT',
                body: JSON.stringify(updates)
            });
            setCustomers(prev => prev.map(c => c.id === id ? res : c));
        } catch (e: any) { throw e; }
    };

    const createAccount = async (cif: string, productCode: string, currency: 'GHS' | 'USD' = 'GHS') => {
        try {
            const res = await apiFetch('/accounts', {
                method: 'POST',
                body: JSON.stringify({ customer_id: cif, product_code: productCode, currency, type: productCode.startsWith('SA') ? 'SAVINGS' : 'CURRENT' })
            });
            setAccounts(prev => [res, ...prev]);
            return res.id;
        } catch (e: any) { throw e; }
    };

    // Stubs for remaining local-only functions to prevent compilation errors
    // Disburse Loan
    const disburseLoan = async (loanData: any) => {
        try {
            const res = await apiFetch('/loans/disburse', {
                method: 'POST',
                body: JSON.stringify(loanData)
            });
            setLoans(prev => [res, ...prev]);
            // Optimistic account balance update (since loan drops into account)
            setAccounts(prev => prev.map(a =>
                a.cif === loanData.cif
                    ? { ...a, balance: Number(a.balance) + Number(loanData.principal) }
                    : a
            ));
            return res.id;
        } catch (e: any) { throw e; }
    };

    // GL Accounting
    const postGL = async (journalData: any) => {
        try {
            const res = await apiFetch('/gl/journals', {
                method: 'POST',
                body: JSON.stringify(journalData)
            });
            setJournalEntries(prev => [res, ...prev]);
            // Very simplified optimistic update for UI
            return { success: true };
        } catch (e: any) { throw e; }
    };

    const createGLAccount = async (glData: any) => {
        try {
            const res = await apiFetch('/gl/accounts', {
                method: 'POST',
                body: JSON.stringify(glData)
            });
            setGlAccounts(prev => [...prev, res].sort((a, b) => a.code.localeCompare(b.code)));
            return res.code;
        } catch (e: any) { throw e; }
    };
    const executeStoredProcedure = () => ({ success: true, output: '' });
    const runEndOfDay = () => "";
    const runEodStep = async () => ({ status: 'OK', message: 'EOD Complete' });
    const approveRequest = async (id: string, workflowStep: number) => {
        try {
            const res = await apiFetch(`/approvals/${id}`, {
                method: 'PUT',
                body: JSON.stringify({ status: 'APPROVED', current_step: workflowStep + 1 })
            });
            setApprovalRequests(prev => prev.map(a => a.id === id ? res : a));
        } catch (e: any) { throw e; }
    };

    const rejectRequest = async (id: string) => {
        try {
            const res = await apiFetch(`/approvals/${id}`, {
                method: 'PUT',
                body: JSON.stringify({ status: 'REJECTED' })
            });
            setApprovalRequests(prev => prev.map(a => a.id === id ? res : a));
        } catch (e: any) { throw e; }
    };
    const createRole = async (roleData: any) => {
        try {
            const res = await apiFetch('/roles', {
                method: 'POST',
                body: JSON.stringify(roleData)
            });
            setRoles(prev => [...prev, res]);
            return res.id;
        } catch (e: any) { throw e; }
    };

    const updateUserRole = async (userId: string, roleId: string) => {
        try {
            const res = await apiFetch(`/users/${userId}`, {
                method: 'PUT',
                body: JSON.stringify({ role_id: roleId })
            });
            setStaff(prev => prev.map(s => s.id === userId ? { ...s, roleId } : s));
        } catch (e: any) { throw e; }
    };

    const createStaff = async (staffData: Omit<StaffUser, 'id' | 'roleName' | 'lastLogin'>) => {
        try {
            const res = await apiFetch('/users', {
                method: 'POST',
                body: JSON.stringify(staffData)
            });
            setStaff(prev => [res, ...prev]);
            return res.id;
        } catch (e: any) { throw e; }
    };

    const resetUserPassword = async (userId: string) => {
        try {
            await apiFetch(`/users/${userId}`, {
                method: 'PUT',
                body: JSON.stringify({ password: 'password123' })
            });
        } catch (e: any) { throw e; }
    };

    const changeMyPassword = () => { };

    const updateSystemConfig = async (configData: any) => {
        try {
            // Flatten configData for the backend array structure
            const configItems = Object.entries(configData).map(([key, value]) => ({
                key,
                value: typeof value === 'object' ? JSON.stringify(value) : String(value)
            }));

            await apiFetch('/config', {
                method: 'POST',
                body: JSON.stringify(configItems)
            });
            // Keep local state up to date
            setSystemConfig(configData);
        } catch (e: any) { throw e; }
    };

    const createWorkflow = async (workflowData: Workflow) => {
        try {
            const res = await apiFetch('/workflows', {
                method: 'POST',
                body: JSON.stringify(workflowData)
            });
            setWorkflows(prev => [...prev, res]);
            return res.id;
        } catch (e: any) { throw e; }
    };

    const updateWorkflow = async (id: string, updates: Partial<Workflow>) => {
        try {
            const res = await apiFetch(`/workflows/${id}`, {
                method: 'PUT',
                body: JSON.stringify(updates)
            });
            setWorkflows(prev => prev.map(w => w.id === id ? res : w));
        } catch (e: any) { throw e; }
    };
    const createProduct = async (productData: any) => {
        try {
            const res = await apiFetch('/products', {
                method: 'POST',
                body: JSON.stringify(productData)
            });
            setProducts(prev => [...prev, res]);
            return res.id;
        } catch (e: any) { throw e; }
    };

    const updateProduct = async (id: string, updates: any) => {
        try {
            const res = await apiFetch(`/products/${id}`, {
                method: 'PUT',
                body: JSON.stringify(updates)
            });
            setProducts(prev => prev.map(p => p.id === id ? res : p));
        } catch (e: any) { throw e; }
    };

    const createGroup = async (groupData: any) => {
        try {
            const res = await apiFetch('/groups', {
                method: 'POST',
                body: JSON.stringify(groupData)
            });
            setGroups(prev => [res, ...prev]);
            return res.id;
        } catch (e: any) { throw e; }
    };

    const addMemberToGroup = async (groupId: string, customerId: string) => {
        try {
            await apiFetch(`/groups/${groupId}/members`, {
                method: 'POST',
                body: JSON.stringify({ customerId })
            });
            setGroups(prev => prev.map(g => g.id === groupId ? { ...g, members: [...g.members, customerId] } : g));
        } catch (e: any) { throw e; }
    };

    const removeMemberFromGroup = async (groupId: string, customerId: string) => {
        try {
            await apiFetch(`/groups/${groupId}/members/${customerId}`, {
                method: 'DELETE'
            });
            setGroups(prev => prev.map(g => g.id === groupId ? { ...g, members: g.members.filter(m => m !== customerId) } : g));
        } catch (e: any) { throw e; }
    };

    const addAuditLog = () => { };
    const addDevTask = () => { };
    const updateDevTaskStatus = () => { };
    const deleteDevTask = () => { };

    return {
        currentUser, login, logout, hasPermission, authError, businessDate, branches, customers,
        groups, products, accounts, loans, glAccounts, journalEntries, auditLogs, transactions,
        roles, staff, approvalRequests, workflows, systemConfig, devTasks, processTellerTransaction,
        createCustomer, updateCustomer, createGroup, addMemberToGroup, removeMemberFromGroup,
        createAccount, disburseLoan, addAuditLog, setLoans, postGL, createGLAccount,
        executeStoredProcedure, runEndOfDay, runEodStep, approveRequest, rejectRequest,
        createRole, updateUserRole, createStaff, resetUserPassword, changeMyPassword,
        updateSystemConfig, createWorkflow, updateWorkflow, createProduct, updateProduct,
        addDevTask, updateDevTaskStatus, deleteDevTask
    };
};
