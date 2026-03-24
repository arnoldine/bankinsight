import React, { useEffect, useMemo, useState } from 'react';
import { Branch, MenuItem, RegulatoryChartSeedResponse, StaffUser, SystemConfig, UILayout, Workflow } from '../types';
import {
    Activity,
    GitBranch,
    Landmark,
    Loader2,
    Lock,
    Menu,
    Plus,
    RefreshCw,
    Save,
    Search,
    Server,
    Settings as SettingsIcon,
    Shield,
    Sliders,
    Users,
    X,
    UploadCloud,
} from 'lucide-react';
import MenuEditor from './MenuEditor';
import { useAdmin, useFetch } from '../src/hooks/useApi';
import { httpClient } from '../src/services/httpClient';
import { Permissions } from '../lib/Permissions';
import type { OrassQueueItem, OrassReadiness, OrassReconciliationResult, OrassSubmissionEvidence, OrassSubmissionHistoryItem } from '../src/services/adminService';

interface SettingsProps {
    workflows?: Workflow[];
    customForms?: UILayout[];
    menuItems?: MenuItem[];
    onSaveMenu?: (item: MenuItem) => void;
    onDeleteMenu?: (id: string) => void;
    currentUserId?: string;
    pageTargets?: Array<{ id: string; label: string; helper?: string }>;
}

type SettingsTab = 'USERS' | 'ROLES' | 'MENU' | 'CONFIG' | 'AUTH' | 'ORASS' | 'BRANCHES' | 'SYSTEM_INFO';

interface RoleRecord {
    id: string;
    name: string;
    description?: string;
    permissions: string[];
    isSystemRole?: boolean;
    isActive?: boolean;
    userCount?: number;
}

interface ProcessDefinitionRecord {
    id: string;
    code: string;
    name: string;
    module: string;
    entityType: string;
    triggerType: string;
    triggerEventType?: string | null;
    isSystemProcess: boolean;
    isActive: boolean;
}

interface ProcessDraft {
    code: string;
    name: string;
    module: string;
    entityType: string;
    triggerType: string;
    triggerEventType: string;
}

interface UserDraft {
    id: string;
    name: string;
    email: string;
    phone: string;
    roleId: string;
    branchId: string;
    status: string;
    password: string;
}

const defaultUserDraft: UserDraft = {
    id: '',
    name: '',
    email: '',
    phone: '',
    roleId: '',
    branchId: '',
    status: 'Active',
    password: '',
};

const defaultConfig: SystemConfig = {
    amlThreshold: 10000,
    kycLimits: {
        'Tier 1': { maxBalance: 5000, dailyLimit: 1000 },
        'Tier 2': { maxBalance: 20000, dailyLimit: 5000 },
        'Tier 3': { maxBalance: 100000, dailyLimit: 20000 },
    },
    auth: {
        enabled: false,
        provider: 'LOCAL',
        tenantId: '',
        clientId: '',
        ldapServer: '',
    },
    eodScheduler: {
        enabled: false,
        timeUtc: '23:00',
        lastRunDate: '',
    },
    orass: {
        enabled: false,
        institutionCode: '',
        submissionMode: 'TEST',
        endpointUrl: '',
        username: '',
        password: '',
        certificateAlias: '',
        sourceReportCode: 'REG-BOG-DBK-ORASS',
        autoSubmit: false,
        cutoffTimeUtc: '17:00',
        fallbackEmail: '',
        lastSubmissionAt: '',
    },
    dbProvider: 'MYSQL',
};

const defaultRoleDraft: RoleRecord = {
    id: '',
    name: '',
    description: '',
    permissions: [],
    isSystemRole: false,
    isActive: true,
    userCount: 0,
};

const defaultProcessDraft: ProcessDraft = {
    code: '',
    name: '',
    module: 'Operations',
    entityType: 'Transaction',
    triggerType: 'Manual',
    triggerEventType: '',
};

function mergeConfig(config: Partial<SystemConfig> | null | undefined): SystemConfig {
    if (!config) return defaultConfig;
    return {
        ...defaultConfig,
        ...config,
        kycLimits: {
            ...defaultConfig.kycLimits,
            ...(config.kycLimits || {}),
        },
        auth: {
            ...defaultConfig.auth,
            ...(config.auth || {}),
        },
        eodScheduler: {
            ...defaultConfig.eodScheduler,
            ...(config.eodScheduler || {}),
        },
        orass: {
            ...defaultConfig.orass,
            ...(config.orass || {}),
        },
    };
}

function startCase(value: string): string {
    return value
        .replace(/[._-]/g, ' ')
        .replace(/\b\w/g, (char) => char.toUpperCase());
}

function getUiErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof Error && error.message) {
        return error.message;
    }
    return fallback;
}

export default function Settings({
    workflows = [],
    customForms = [],
    menuItems = [],
    onSaveMenu,
    onDeleteMenu,
    currentUserId,
    pageTargets = [],
}: SettingsProps) {
    const [activeTab, setActiveTab] = useState<SettingsTab>('USERS');
    const [refreshKey, setRefreshKey] = useState(0);
    const [modalMessage, setModalMessage] = useState<string | null>(null);
    const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
    const [isCreatingUser, setIsCreatingUser] = useState(false);
    const [userDraft, setUserDraft] = useState<UserDraft>(defaultUserDraft);
    const [userDirty, setUserDirty] = useState(false);
    const [userSaving, setUserSaving] = useState(false);
    const [userBusyId, setUserBusyId] = useState<string | null>(null);
    const [userBusyAction, setUserBusyAction] = useState<'reset' | 'toggle-status' | null>(null);
    const [localConfig, setLocalConfig] = useState<SystemConfig>(defaultConfig);
    const [configDirty, setConfigDirty] = useState(false);
    const [roleSearch, setRoleSearch] = useState('');
    const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
    const [roleDraft, setRoleDraft] = useState<RoleRecord>(defaultRoleDraft);
    const [roleDirty, setRoleDirty] = useState(false);
    const [roleSaving, setRoleSaving] = useState(false);
    const [processDraft, setProcessDraft] = useState<ProcessDraft>(defaultProcessDraft);
    const [processSaving, setProcessSaving] = useState(false);
    const [selectedProcessId, setSelectedProcessId] = useState<string | null>(null);
    const [seedRegionCode, setSeedRegionCode] = useState('GH');
    const [seedingChart, setSeedingChart] = useState(false);
    const [seedResult, setSeedResult] = useState<RegulatoryChartSeedResponse | null>(null);
    const [submittingOrassReturnId, setSubmittingOrassReturnId] = useState<number | null>(null);
    const [selectedOrassHistoryId, setSelectedOrassHistoryId] = useState<number | null>(null);
    const [orassReconciling, setOrassReconciling] = useState(false);
    const [orassReconciliationResult, setOrassReconciliationResult] = useState<OrassReconciliationResult | null>(null);

    const admin = useAdmin();
    const refreshData = () => setRefreshKey((current) => current + 1);

    const needsUsers = activeTab === 'USERS' || activeTab === 'SYSTEM_INFO';
    const needsRoles = activeTab === 'USERS' || activeTab === 'ROLES' || activeTab === 'MENU' || activeTab === 'SYSTEM_INFO';
    const needsBranches = activeTab === 'BRANCHES' || activeTab === 'SYSTEM_INFO';
    const needsConfig = activeTab === 'CONFIG' || activeTab === 'AUTH' || activeTab === 'ORASS' || activeTab === 'SYSTEM_INFO';
    const needsOrassReadiness = activeTab === 'ORASS';

    const { data: usersData, loading: usersLoading, error: usersError } = useFetch(
        () => needsUsers ? admin.getUsers() : Promise.resolve([] as any[]),
        [refreshKey, needsUsers]
    );
    const { data: rolesData, loading: rolesLoading, error: rolesError } = useFetch(
        () => needsRoles ? (admin.getRoles() as Promise<RoleRecord[]>) : Promise.resolve([] as RoleRecord[]),
        [refreshKey, needsRoles]
    );
    const { data: branchesData, loading: branchesLoading, error: branchesError } = useFetch(
        () => needsBranches ? admin.getBranches() : Promise.resolve([] as Branch[]),
        [refreshKey, needsBranches]
    );
    const { data: configData, loading: configLoading, error: configError } = useFetch(
        () => needsConfig ? admin.getSystemConfig() : Promise.resolve(defaultConfig),
        [refreshKey, needsConfig]
    );
    const { data: processData, loading: processLoading, error: processError } = useFetch(
        () => needsProcesses ? httpClient.get<ProcessDefinitionRecord[]>('/WorkflowDefinition') : Promise.resolve([] as ProcessDefinitionRecord[]),
        [refreshKey, needsProcesses]
    );
    const { data: orassReadinessData, loading: orassLoading, error: orassError } = useFetch(
        () => needsOrassReadiness ? admin.getOrassReadiness() : Promise.resolve(null as OrassReadiness | null),
        [refreshKey, needsOrassReadiness]
    );
    const { data: orassQueueData, loading: orassQueueLoading, error: orassQueueError } = useFetch(
        () => needsOrassReadiness ? admin.getOrassQueue() : Promise.resolve([] as OrassQueueItem[]),
        [refreshKey, needsOrassReadiness]
    );
    const { data: orassHistoryData, loading: orassHistoryLoading, error: orassHistoryError } = useFetch(
        () => needsOrassReadiness ? admin.getOrassHistory() : Promise.resolve([] as OrassSubmissionHistoryItem[]),
        [refreshKey, needsOrassReadiness]
    );
    const { data: orassEvidenceData, loading: orassEvidenceLoading } = useFetch(
        () => needsOrassReadiness && selectedOrassHistoryId
            ? admin.getOrassEvidence(selectedOrassHistoryId)
            : Promise.resolve(null as OrassSubmissionEvidence | null),
        [refreshKey, needsOrassReadiness, selectedOrassHistoryId]
    );

    const users = (usersData || []) as StaffUser[];
    const roles = (rolesData || []) as RoleRecord[];
    const branches = branchesData || [];
    const systemConfig = configData;
    const processDefinitions = processData || [];

    useEffect(() => {
        if (!configDirty) {
            setLocalConfig(mergeConfig(systemConfig));
        }
    }, [systemConfig, configDirty]);

    useEffect(() => {
        if (isCreatingUser) {
            return;
        }

        if (!users.length) {
            setSelectedUserId(null);
            if (!userDirty) {
                setUserDraft(defaultUserDraft);
            }
            return;
        }

        if (!selectedUserId) {
            if (!userDirty) {
                setUserDraft(defaultUserDraft);
            }
            return;
        }

        const selected = users.find((user) => user.id === selectedUserId);
        if (!selected) {
            setSelectedUserId(null);
            if (!userDirty) {
                setUserDraft(defaultUserDraft);
            }
            return;
        }

        if (!userDirty) {
            setUserDraft({
                id: selected.id,
                name: selected.name || '',
                email: selected.email || '',
                phone: selected.phone || '',
                roleId: selected.roleId || '',
                branchId: selected.branchId || '',
                status: selected.status || 'Active',
                password: '',
            });
        }
    }, [users, selectedUserId, userDirty, isCreatingUser]);
    useEffect(() => {
        if (!roles.length) {
            setSelectedRoleId(null);
            if (!roleDirty) {
                setRoleDraft(defaultRoleDraft);
            }
            return;
        }

        const selected = roles.find((role) => role.id === selectedRoleId) || roles[0];
        if (!selectedRoleId) {
            setSelectedRoleId(selected.id);
        }
        if (!roleDirty) {
            setRoleDraft({
                id: selected.id,
                name: selected.name,
                description: selected.description || '',
                permissions: [...(selected.permissions || [])],
                isSystemRole: selected.isSystemRole,
                isActive: selected.isActive,
                userCount: selected.userCount,
            });
        }
    }, [roles, selectedRoleId, roleDirty]);

    useEffect(() => {
        if (!processDefinitions.length) {
            setSelectedProcessId(null);
            return;
        }

        if (!selectedProcessId || !processDefinitions.some((definition) => definition.id === selectedProcessId)) {
            setSelectedProcessId(processDefinitions[0].id);
        }
    }, [processDefinitions, selectedProcessId]);

    const tabLoadingMap: Record<SettingsTab, boolean> = {
        USERS: usersLoading || rolesLoading,
        ROLES: rolesLoading,
        PROCESS: processLoading,
        MENU: rolesLoading,
        CONFIG: configLoading,
        AUTH: configLoading,
        ORASS: configLoading,
        BRANCHES: branchesLoading,
        SYSTEM_INFO: usersLoading || rolesLoading || branchesLoading || configLoading,
    };
    tabLoadingMap.ORASS = configLoading || orassLoading;
    tabLoadingMap.ORASS = configLoading || orassLoading || orassQueueLoading || orassHistoryLoading;

    const tabErrorMap: Record<SettingsTab, string | null> = {
        USERS: usersError || rolesError,
        ROLES: rolesError,
        PROCESS: processError,
        MENU: rolesError,
        CONFIG: configError,
        AUTH: configError,
        ORASS: configError,
        BRANCHES: branchesError,
        SYSTEM_INFO: usersError || rolesError || branchesError || configError,
    };
    tabErrorMap.ORASS = configError || orassError;
    tabErrorMap.ORASS = configError || orassError || orassQueueError || orassHistoryError;

    const defaultErrorMessageMap: Record<SettingsTab, string> = {
        USERS: 'Failed to load users or roles.',
        ROLES: 'Failed to load roles and permissions.',
        PROCESS: 'Failed to load process definitions.',
        MENU: 'Failed to load role data for menu configuration.',
        CONFIG: 'Failed to load system configuration.',
        AUTH: 'Failed to load authentication settings.',
        ORASS: 'Failed to load ORASS submission settings.',
        BRANCHES: 'Failed to load branches.',
        SYSTEM_INFO: 'Failed to load system information.',
    };

    const isLoadingData = tabLoadingMap[activeTab];
    const rawLoadError = tabErrorMap[activeTab];
    const loadError = rawLoadError === 'Failed to load data' ? defaultErrorMessageMap[activeTab] : rawLoadError;
    const orassReadiness = orassReadinessData as OrassReadiness | null;
    const orassQueue = (orassQueueData || []) as OrassQueueItem[];
    const orassHistory = (orassHistoryData || []) as OrassSubmissionHistoryItem[];
    const orassEvidence = orassEvidenceData as OrassSubmissionEvidence | null;

    const roleNameById = useMemo(() => new Map(roles.map((role) => [role.id, role.name])), [roles]);
    const branchNameById = useMemo(() => new Map(branches.map((branch) => [branch.id, branch.name])), [branches]);
    const permissionCatalog = useMemo(() => {
        return Object.entries(Permissions).map(([moduleName, modulePermissions]) => ({
            module: startCase(moduleName),
            entries: Object.entries(modulePermissions).map(([permissionName, code]) => ({
                code,
                label: startCase(permissionName),
            })),
        }));
    }, []);
    const filteredPermissionCatalog = useMemo(() => {
        const query = roleSearch.trim().toLowerCase();
        if (!query) {
            return permissionCatalog;
        }

        return permissionCatalog
            .map((group) => ({
                ...group,
                entries: group.entries.filter((entry) => (
                    entry.code.toLowerCase().includes(query) ||
                    entry.label.toLowerCase().includes(query) ||
                    group.module.toLowerCase().includes(query)
                )),
            }))
            .filter((group) => group.entries.length > 0);
    }, [permissionCatalog, roleSearch]);

    const totalPermissionCount = useMemo(
        () => permissionCatalog.reduce((count, group) => count + group.entries.length, 0),
        [permissionCatalog]
    );

    const latestPermissionHighlights = useMemo(() => ([
        'reports.generate',
        'reports.approve',
        'reports.submit',
        'reports.configure',
        Permissions.Processes.Publish,
        Permissions.Tasks.Claim,
        Permissions.Tasks.Complete,
        Permissions.Audit.View,
    ]), []);

    const selectedProcess = processDefinitions.find((definition) => definition.id === selectedProcessId) || null;

    const tabs: Array<{ id: SettingsTab; label: string; icon: React.ReactNode }> = [
        { id: 'USERS', label: 'User Management', icon: <Users size={16} /> },
        { id: 'ROLES', label: 'Permissions', icon: <Shield size={16} /> },
        { id: 'AUTH', label: 'Authentication', icon: <Lock size={16} /> },
        { id: 'ORASS', label: 'ORASS Setup', icon: <UploadCloud size={16} /> },
        { id: 'MENU', label: 'Menu Config', icon: <Menu size={16} /> },
        { id: 'CONFIG', label: 'Configuration', icon: <Sliders size={16} /> },
        { id: 'BRANCHES', label: 'Branches', icon: <Landmark size={16} /> },
        { id: 'SYSTEM_INFO', label: 'System Info', icon: <Server size={16} /> },
    ];

    const handleSaveConfig = async () => {
        try {
            await admin.updateSystemConfig(localConfig);
            setConfigDirty(false);
            setModalMessage('System configuration updated.');
            refreshData();
        } catch (e: any) {
            setModalMessage(e?.message || 'Failed to update configuration.');
        }
    };

    const updateConfig = (updates: Partial<SystemConfig>) => {
        setConfigDirty(true);
        setLocalConfig((current) => ({ ...current, ...updates }));
    };

    const updateAuthConfig = (updates: Partial<SystemConfig['auth']>) => {
        setConfigDirty(true);
        setLocalConfig((current) => ({
            ...current,
            auth: { ...current.auth, ...updates },
        }));
    };

    const updateOrassConfig = (updates: Partial<SystemConfig['orass']>) => {
        setConfigDirty(true);
        setLocalConfig((current) => ({
            ...current,
            orass: { ...current.orass, ...updates },
        }));
    };

    const handleSubmitOrassReturn = async (returnId: number) => {
        setSubmittingOrassReturnId(returnId);
        try {
            const result = await admin.submitOrassReturn(returnId);
            setModalMessage(`ORASS return ${result.id} submitted with reference ${result.bogReferenceNumber || 'pending reference allocation'}.`);
            setConfigDirty(false);
            refreshData();
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, 'Unable to submit ORASS return.'));
        } finally {
            setSubmittingOrassReturnId(null);
        }
    };

    const handleReconcileOrassAcknowledgements = async () => {
        setOrassReconciling(true);
        try {
            const result = await admin.reconcileOrassAcknowledgements();
            setOrassReconciliationResult(result);
            setModalMessage(
                `ORASS reconciliation scanned ${result.scannedCount} submission(s) and updated ${result.updatedCount}. ${result.pendingCount} still pending.`
            );
            refreshData();
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, 'Unable to reconcile ORASS acknowledgements.'));
        } finally {
            setOrassReconciling(false);
        }
    };

    const startNewRole = () => {
        setSelectedRoleId(null);
        setRoleDraft(defaultRoleDraft);
        setRoleDirty(true);
    };

    const selectRole = (role: RoleRecord) => {
        setSelectedRoleId(role.id);
        setRoleDraft({
            id: role.id,
            name: role.name,
            description: role.description || '',
            permissions: [...(role.permissions || [])],
            isSystemRole: role.isSystemRole,
            isActive: role.isActive,
            userCount: role.userCount,
        });
        setRoleDirty(false);
    };

    const togglePermission = (permissionCode: string) => {
        setRoleDirty(true);
        setRoleDraft((current) => ({
            ...current,
            permissions: current.permissions.includes(permissionCode)
                ? current.permissions.filter((permission) => permission !== permissionCode)
                : [...current.permissions, permissionCode].sort(),
        }));
    };

    const handleSaveRole = async () => {
        if (!roleDraft.name.trim()) {
            setModalMessage('Role name is required.');
            return;
        }

        setRoleSaving(true);
        try {
            const payload = {
                name: roleDraft.name.trim(),
                description: roleDraft.description?.trim() || '',
                permissions: roleDraft.permissions,
            };

            const savedRole = selectedRoleId
                ? await admin.updateRole(selectedRoleId, payload as any)
                : await admin.createRole(payload as any);

            const normalizedRole = savedRole as unknown as RoleRecord;
            setRoleDraft({
                id: normalizedRole.id,
                name: normalizedRole.name,
                description: normalizedRole.description || '',
                permissions: [...(normalizedRole.permissions || roleDraft.permissions)],
                isSystemRole: normalizedRole.isSystemRole,
                isActive: normalizedRole.isActive,
                userCount: normalizedRole.userCount,
            });
            setSelectedRoleId(normalizedRole.id);
            setRoleDirty(false);
            setModalMessage(selectedRoleId ? 'Role permissions updated.' : 'New role created.');
            refreshData();
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, 'Unable to save role changes.'));
        } finally {
            setRoleSaving(false);
        }
    };

    const handleCreateProcess = async () => {
        if (!processDraft.code.trim() || !processDraft.name.trim()) {
            setModalMessage('Process code and name are required.');
            return;
        }

        setProcessSaving(true);
        try {
            const created = await httpClient.post<ProcessDefinitionRecord>('/WorkflowDefinition', {
                code: processDraft.code.trim(),
                name: processDraft.name.trim(),
                module: processDraft.module.trim(),
                entityType: processDraft.entityType.trim(),
                triggerType: processDraft.triggerType.trim(),
                triggerEventType: processDraft.triggerEventType.trim() || null,
            });

            setProcessDraft(defaultProcessDraft);
            setSelectedProcessId(created.id);
            setModalMessage('Process definition created.');
            refreshData();
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, 'Unable to create process definition.'));
        } finally {
            setProcessSaving(false);
        }
    };

    const handleSeedRegulatoryChart = async () => {
        setSeedingChart(true);
        try {
            const result = await admin.seedRegulatoryChartOfAccounts(seedRegionCode.trim().toUpperCase() || 'GH');
            setSeedResult(result);
            setModalMessage(
                result.insertedCount > 0
                    ? `Regulatory chart seeded successfully. ${result.insertedCount} accounts were added.`
                    : `Regulatory chart already aligned. ${result.existingCount} standard accounts are already present.`
            );
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, 'Unable to seed regulatory chart of accounts.'));
        } finally {
            setSeedingChart(false);
        }
    };
    const closeUserModal = () => {
        setIsCreatingUser(false);
        setSelectedUserId(null);
        setUserDraft(defaultUserDraft);
        setUserDirty(false);
    };

    useEffect(() => {
        if (!selectedUserId && !isCreatingUser) {
            return;
        }

        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                closeUserModal();
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [selectedUserId, isCreatingUser]);

    const selectUser = (user: StaffUser) => {
        setIsCreatingUser(false);
        setSelectedUserId(user.id);
        setUserDraft({
            id: user.id,
            name: user.name || '',
            email: user.email || '',
            phone: user.phone || '',
            roleId: user.roleId || '',
            branchId: user.branchId || '',
            status: user.status || 'Active',
            password: '',
        });
        setUserDirty(false);
    };

    const startNewUser = () => {
        setIsCreatingUser(true);
        setSelectedUserId(null);
        setUserDraft({
            ...defaultUserDraft,
            status: 'Active',
            password: 'password123',
        });
        setUserDirty(false);
    };

    const handleSaveUser = async () => {
        if (!userDraft.name.trim() || !userDraft.email.trim()) {
            setModalMessage('Name and email are required for user accounts.');
            return;
        }

        if (isCreatingUser && !userDraft.password.trim()) {
            setModalMessage('A temporary password is required when creating a user.');
            return;
        }

        setUserSaving(true);
        try {
            const trimmedName = userDraft.name.trim();
            const trimmedEmail = userDraft.email.trim();
            const trimmedPhone = userDraft.phone.trim();
            const avatarInitials = trimmedName
                .split(/\s+/)
                .filter(Boolean)
                .slice(0, 2)
                .map((part) => part[0]?.toUpperCase() || '')
                .join('');

            if (isCreatingUser) {
                const createdUser = await admin.createUser({
                    name: trimmedName,
                    email: trimmedEmail,
                    phone: trimmedPhone,
                    roleId: userDraft.roleId || undefined,
                    branchId: userDraft.branchId || undefined,
                    password: userDraft.password.trim(),
                    avatarInitials,
                });

                if (userDraft.status === 'Inactive') {
                    await admin.updateUser(createdUser.id, { status: 'Inactive' });
                    createdUser.status = 'Inactive';
                }

                setIsCreatingUser(false);
                setSelectedUserId(createdUser.id);
                setUserDraft({
                    id: createdUser.id,
                    name: createdUser.name || trimmedName,
                    email: createdUser.email || trimmedEmail,
                    phone: createdUser.phone || trimmedPhone,
                    roleId: createdUser.roleId || userDraft.roleId,
                    branchId: createdUser.branchId || userDraft.branchId,
                    status: createdUser.status || userDraft.status,
                    password: '',
                });
                setUserDirty(false);
                setModalMessage('User account created successfully.');
                closeUserModal();
            } else {
                if (!userDraft.id) {
                    setModalMessage('Select a user account to update.');
                    return;
                }

                await admin.updateUser(userDraft.id, {
                    name: trimmedName,
                    email: trimmedEmail,
                    phone: trimmedPhone,
                    roleId: userDraft.roleId || '',
                    branchId: userDraft.branchId || '',
                    status: userDraft.status as StaffUser['status'],
                    password: userDraft.password.trim() || undefined,
                });
                setUserDraft((current) => ({ ...current, password: '' }));
                setUserDirty(false);
                setModalMessage('User account updated successfully.');
                closeUserModal();
            }

            refreshData();
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, isCreatingUser ? 'Unable to create user account.' : 'Unable to update user account.'));
        } finally {
            setUserSaving(false);
        }
    };

    const handleResetUserPassword = async (user: StaffUser) => {
        setUserBusyId(user.id);
        setUserBusyAction('reset');
        try {
            await admin.resetPassword(user.id);
            setModalMessage(`Password reset for ${user.name || user.email}. Temporary password: password123`);
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, 'Unable to reset the user password.'));
        } finally {
            setUserBusyId(null);
            setUserBusyAction(null);
        }
    };

    const handleToggleUserStatus = async (user: StaffUser) => {
        const nextStatus = user.status === 'Inactive' ? 'Active' : 'Inactive';
        setUserBusyId(user.id);
        setUserBusyAction('toggle-status');
        try {
            await admin.updateUser(user.id, { status: nextStatus });
            if (selectedUserId === user.id) {
                setUserDraft((current) => ({ ...current, status: nextStatus }));
                setUserDirty(false);
            }
            setModalMessage(nextStatus === 'Inactive' ? `${user.name || user.email} has been disabled.` : `${user.name || user.email} has been re-enabled.`);
            refreshData();
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, 'Unable to update the user status.'));
        } finally {
            setUserBusyId(null);
            setUserBusyAction(null);
        }
    };

    const handleDeleteUser = async (user: StaffUser) => {
        if (!window.confirm(`Delete ${user.name || user.email}? This removes the user account from the platform.`)) {
            return;
        }

        setUserBusyId(user.id);
        setUserBusyAction(null);
        try {
            await admin.deleteUser(user.id);
            if (selectedUserId === user.id) {
                setSelectedUserId(null);
                setUserDraft(defaultUserDraft);
                setUserDirty(false);
            }
            setModalMessage(`${user.name || user.email} was deleted.`);
            refreshData();
        } catch (error) {
            setModalMessage(getUiErrorMessage(error, 'Unable to delete the user account.'));
        } finally {
            setUserBusyId(null);
        }
    };

    const renderUsers = () => (
        <>
            <div className="space-y-4">
                <div className="flex items-center justify-between">
                    <div>
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white">Staff Directory</h3>
                    <p className="text-sm text-gray-500 dark:text-slate-400">Create new staff accounts, update existing profiles, reset passwords, disable access, and delete users from one screen.</p>
                    </div>
                    <div className="flex items-center gap-2">
                        <button onClick={refreshData} className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700">
                            Refresh
                        </button>
                        <button onClick={startNewUser} className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
                            <Plus size={16} className="mr-2 inline-block" /> New User
                        </button>
                    </div>
                </div>

                <div className="overflow-x-auto rounded-lg border border-gray-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
                    <table className="w-full text-sm">
                        <thead className="bg-gray-50 text-left text-gray-500 dark:bg-slate-900 dark:text-slate-400">
                            <tr>
                                <th className="p-3">Name</th>
                                <th className="p-3">Email</th>
                                <th className="p-3">Role</th>
                                <th className="p-3">Branch</th>
                                <th className="p-3">Status</th>
                                <th className="p-3 text-right">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {users.map((user) => {
                                const isBusy = userBusyId === user.id;
                                const roleLabel = roleNameById.get(user.roleId) || user.roleName || 'Unassigned';
                                const branchLabel = branchNameById.get(user.branchId) || user.branchId || 'N/A';
                                const canDisable = user.status !== 'Inactive';

                                return (
                                    <tr key={user.id} className="border-t border-gray-100 dark:border-slate-700">
                                        <td className="p-3 font-medium text-gray-900 dark:text-white">{user.name}</td>
                                        <td className="p-3 text-gray-600 dark:text-slate-300">{user.email}</td>
                                        <td className="p-3 text-gray-600 dark:text-slate-300">{roleLabel}</td>
                                        <td className="p-3 text-gray-600 dark:text-slate-300">{branchLabel}</td>
                                        <td className="p-3 text-gray-600 dark:text-slate-300">{user.status || 'Unknown'}</td>
                                        <td className="p-3">
                                            <div className="flex flex-wrap justify-end gap-2">
                                                <button
                                                    onClick={() => selectUser(user)}
                                                    className="rounded-md border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
                                                >
                                                    Edit
                                                </button>
                                                <button
                                                    onClick={() => handleResetUserPassword(user)}
                                                    disabled={isBusy}
                                                    className="rounded-md border border-amber-200 px-3 py-1.5 text-xs font-medium text-amber-700 hover:bg-amber-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-amber-900/60 dark:text-amber-300 dark:hover:bg-amber-950/30"
                                                >
                                                    {isBusy && userBusyAction === 'reset' ? 'Resetting...' : 'Reset Password'}
                                                </button>
                                                <button
                                                    onClick={() => handleToggleUserStatus(user)}
                                                    disabled={isBusy}
                                                    className={`rounded-md px-3 py-1.5 text-xs font-medium disabled:cursor-not-allowed disabled:opacity-60 ${canDisable ? 'border border-red-200 text-red-700 hover:bg-red-50 dark:border-red-900/60 dark:text-red-300 dark:hover:bg-red-950/30' : 'border border-emerald-200 text-emerald-700 hover:bg-emerald-50 dark:border-emerald-900/60 dark:text-emerald-300 dark:hover:bg-emerald-950/30'}`}
                                                >
                                                    {isBusy && userBusyAction === 'toggle-status' ? 'Saving...' : canDisable ? 'Disable' : 'Re-enable'}
                                                </button>
                                                <button
                                                    onClick={() => handleDeleteUser(user)}
                                                    disabled={isBusy}
                                                    className="rounded-md border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
                                                >
                                                    Delete
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
            </div>

            {(selectedUserId || isCreatingUser) && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm" onClick={closeUserModal}>
                    <div className="w-full max-w-3xl rounded-xl bg-white shadow-2xl dark:bg-slate-800" onClick={(event) => event.stopPropagation()}>
                        <div className="flex items-start justify-between gap-4 border-b border-gray-200 px-6 py-4 dark:border-slate-700">
                            <div>
                                <h3 className="text-xl font-bold text-gray-900 dark:text-white">{isCreatingUser ? 'Create User' : 'Update User'}</h3>
                                <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">{isCreatingUser ? 'Enter the new user details, set a temporary password, and save the account.' : 'Update profile details, change the assigned role or branch, and optionally set a new password.'}</p>
                            </div>
                            <button
                                onClick={closeUserModal}
                                className="rounded-md p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-600 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                                aria-label="Close user form"
                            >
                                <X size={18} />
                            </button>
                        </div>

                        <div className="space-y-5 px-6 py-5">
                            <div className="grid gap-4 md:grid-cols-2">
                                <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                                    Full Name
                                    <input
                                        value={userDraft.name}
                                        onChange={(e) => {
                                            setUserDirty(true);
                                            setUserDraft((current) => ({ ...current, name: e.target.value }));
                                        }}
                                        className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                                    />
                                </label>
                                <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                                    Email Address
                                    <input
                                        value={userDraft.email}
                                        onChange={(e) => {
                                            setUserDirty(true);
                                            setUserDraft((current) => ({ ...current, email: e.target.value }));
                                        }}
                                        className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                                    />
                                </label>
                                <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                                    Phone Number
                                    <input
                                        value={userDraft.phone}
                                        onChange={(e) => {
                                            setUserDirty(true);
                                            setUserDraft((current) => ({ ...current, phone: e.target.value }));
                                        }}
                                        className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                                    />
                                </label>
                                <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                                    Status
                                    <select
                                        value={userDraft.status}
                                        onChange={(e) => {
                                            setUserDirty(true);
                                            setUserDraft((current) => ({ ...current, status: e.target.value }));
                                        }}
                                        className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                                    >
                                        <option value="Active">Active</option>
                                        <option value="Inactive">Inactive</option>
                                    </select>
                                </label>
                                <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                                    Role
                                    <select
                                        value={userDraft.roleId}
                                        onChange={(e) => {
                                            setUserDirty(true);
                                            setUserDraft((current) => ({ ...current, roleId: e.target.value }));
                                        }}
                                        className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                                    >
                                        <option value="">Unassigned</option>
                                        {roles.map((role) => (
                                            <option key={role.id} value={role.id}>{role.name}</option>
                                        ))}
                                    </select>
                                </label>
                                <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                                    Branch
                                    <select
                                        value={userDraft.branchId}
                                        onChange={(e) => {
                                            setUserDirty(true);
                                            setUserDraft((current) => ({ ...current, branchId: e.target.value }));
                                        }}
                                        className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                                    >
                                        <option value="">Unassigned</option>
                                        {branches.map((branch) => (
                                            <option key={branch.id} value={branch.id}>{branch.name}</option>
                                        ))}
                                    </select>
                                </label>
                                <label className="text-sm font-medium text-gray-700 dark:text-slate-300 md:col-span-2">
                                    {isCreatingUser ? 'Temporary Password' : 'New Password (Optional)'}
                                    <input
                                        type="password"
                                        value={userDraft.password}
                                        onChange={(e) => {
                                            setUserDirty(true);
                                            setUserDraft((current) => ({ ...current, password: e.target.value }));
                                        }}
                                        className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                                        placeholder={isCreatingUser ? 'password123' : 'Leave blank to keep the current password'}
                                    />
                                </label>
                            </div>

                            <div className="grid gap-3 md:grid-cols-3">
                                <InfoCard label="Mode" value={isCreatingUser ? 'New User' : 'Edit User'} />
                                <InfoCard label="User ID" value={userDraft.id || 'Pending creation'} />
                                <InfoCard label="Assigned Role" value={roleNameById.get(userDraft.roleId) || 'Unassigned'} />
                            </div>

                            <div className="rounded-lg border border-dashed border-gray-300 bg-gray-50 p-4 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                                {isCreatingUser
                                    ? 'New accounts require a temporary password. The default suggested password is password123, but you can replace it before saving.'
                                    : 'Use this form to update user details or change the assigned role. Leave the password field empty if you do not want to change it.'}
                            </div>
                        </div>

                        <div className="flex items-center justify-end gap-3 border-t border-gray-200 px-6 py-4 dark:border-slate-700">
                            <button
                                onClick={closeUserModal}
                                className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
                            >
                                Cancel
                            </button>
                            <button
                                onClick={handleSaveUser}
                                disabled={userSaving}
                                className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
                            >
                                {userSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save size={16} />}
                                {isCreatingUser ? 'Create User' : 'Save Changes'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </>
    );const renderRoles = () => (
        <div className="grid gap-6 xl:grid-cols-[320px_minmax(0,1fr)]">
            <div className="rounded-lg border border-gray-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div className="flex items-center justify-between border-b border-gray-200 px-4 py-3 dark:border-slate-700">
                    <div>
                        <h3 className="font-bold text-gray-900 dark:text-white">Roles</h3>
                                        <p className="text-xs text-gray-500 dark:text-slate-400">Manage permission bundles for each module.</p>
                    </div>
                    <div className="flex items-center gap-2">
                        <button onClick={refreshData} className="rounded-md p-2 text-gray-500 hover:bg-gray-100 dark:text-slate-300 dark:hover:bg-slate-700">
                            <RefreshCw size={16} />
                        </button>
                        <button onClick={startNewRole} className="rounded-md bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700">
                            <Plus size={16} className="inline-block" /> New
                        </button>
                    </div>
                </div>
                <div className="space-y-2 p-3">
                    {roles.map((role) => (
                        <button
                            key={role.id}
                            onClick={() => selectRole(role)}
                            className={`w-full rounded-lg border p-3 text-left transition ${selectedRoleId === role.id && !roleDirty ? 'border-blue-500 bg-blue-50 dark:border-blue-400 dark:bg-blue-950/40' : 'border-gray-200 hover:border-blue-200 hover:bg-gray-50 dark:border-slate-700 dark:hover:bg-slate-700/60'}`}
                        >
                            <div className="flex items-center justify-between gap-3">
                                <span className="font-medium text-gray-900 dark:text-white">{role.name}</span>
                                <span className="rounded-full bg-gray-100 px-2 py-1 text-xs text-gray-600 dark:bg-slate-700 dark:text-slate-300">
                                    {(role.permissions || []).length} perms
                                </span>
                            </div>
                            <p className="mt-1 text-xs text-gray-500 dark:text-slate-400">{role.description || 'No description provided.'}</p>
                            <div className="mt-2 flex gap-2 text-[11px] text-gray-500 dark:text-slate-400">
                                <span>{role.userCount || 0} users</span>
                                <span>{role.isSystemRole ? 'System role' : 'Custom role'}</span>
                            </div>
                        </button>
                    ))}
                </div>
            </div>

            <div className="space-y-4 rounded-lg border border-gray-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white">Permission Mapping</h3>
                        <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Edit a role, search the permission catalog, and save changes back to the API.</p>
                    </div>
                    <button
                        onClick={handleSaveRole}
                        disabled={roleSaving}
                        className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {roleSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save size={16} />}
                        Save Role
                    </button>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Role Name
                        <input
                            value={roleDraft.name}
                            onChange={(e) => {
                                setRoleDirty(true);
                                setRoleDraft((current) => ({ ...current, name: e.target.value }));
                            }}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="Operations Supervisor"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Description
                        <input
                            value={roleDraft.description || ''}
                            onChange={(e) => {
                                setRoleDirty(true);
                                setRoleDraft((current) => ({ ...current, description: e.target.value }));
                            }}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="Who should use this role"
                        />
                    </label>
                </div>

                <div className="grid gap-3 md:grid-cols-3">
                    <InfoCard label="Assigned Permissions" value={String(roleDraft.permissions.length)} />
                    <InfoCard label="Users With Role" value={String(roleDraft.userCount || 0)} />
                    <InfoCard label="Role Type" value={roleDraft.isSystemRole ? 'System' : selectedRoleId ? 'Existing' : 'Draft'} />
                </div>

                <div className="rounded-2xl border border-blue-200 bg-blue-50 px-4 py-4 text-sm text-blue-900 dark:border-blue-900/60 dark:bg-blue-950/30 dark:text-blue-100">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                        <div>
                            <div className="font-semibold">Permission catalog synced with current platform modules</div>
                            <div className="mt-1 text-xs text-blue-800/80 dark:text-blue-200/80">
                                Settings now includes the latest reporting, process, workflow, and audit permissions exposed by the platform.
                            </div>
                        </div>
                        <div className="rounded-full bg-white/80 px-3 py-1 text-xs font-semibold uppercase tracking-[0.14em] text-blue-700 dark:bg-slate-900/60 dark:text-blue-200">
                            {totalPermissionCount} permissions
                        </div>
                    </div>
                    <div className="mt-3 flex flex-wrap gap-2">
                        {latestPermissionHighlights.map((code) => (
                            <span key={code} className="rounded-full border border-blue-200 bg-white px-2.5 py-1 text-[11px] font-medium text-blue-700 dark:border-blue-900/60 dark:bg-slate-900/60 dark:text-blue-200">
                                {code}
                            </span>
                        ))}
                    </div>
                </div>

                <div className="relative">
                    <Search className="pointer-events-none absolute left-3 top-3.5 h-4 w-4 text-gray-400" />
                    <input
                        value={roleSearch}
                        onChange={(e) => setRoleSearch(e.target.value)}
                        className="w-full rounded-lg border border-gray-300 bg-white py-2.5 pl-10 pr-4 text-sm dark:border-slate-600 dark:bg-slate-900"
                        placeholder="Search permissions by module, action, or code"
                    />
                </div>

                <div className="grid gap-4 lg:grid-cols-2">
                    {filteredPermissionCatalog.map((group) => (
                        <div key={group.module} className="rounded-lg border border-gray-200 dark:border-slate-700">
                            <div className="border-b border-gray-200 px-4 py-3 dark:border-slate-700">
                                <div className="flex items-center justify-between gap-3">
                                    <h4 className="font-semibold text-gray-900 dark:text-white">{group.module}</h4>
                                    <span className="text-xs text-gray-500 dark:text-slate-400">
                                        {group.entries.filter((entry) => roleDraft.permissions.includes(entry.code)).length}/{group.entries.length}
                                    </span>
                                </div>
                            </div>
                            <div className="space-y-2 p-4">
                                {group.entries.map((entry) => (
                                    <label key={entry.code} className="flex cursor-pointer items-start gap-3 rounded-md border border-transparent px-2 py-1 hover:bg-gray-50 dark:hover:bg-slate-700/60">
                                        <input
                                            type="checkbox"
                                            checked={roleDraft.permissions.includes(entry.code)}
                                            onChange={() => togglePermission(entry.code)}
                                            className="mt-1 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                                        />
                                        <span>
                                            <span className="block text-sm font-medium text-gray-800 dark:text-slate-100">{entry.label}</span>
                                            <span className="block text-xs text-gray-500 dark:text-slate-400">{entry.code}</span>
                                        </span>
                                    </label>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );

    const renderProcess = () => (
        <div className="grid gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
            <div className="rounded-lg border border-gray-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div className="flex items-center justify-between border-b border-gray-200 px-4 py-3 dark:border-slate-700">
                    <div>
                        <h3 className="font-bold text-gray-900 dark:text-white">Definitions</h3>
                        <p className="text-xs text-gray-500 dark:text-slate-400">Live process definitions from the workflow engine.</p>
                    </div>
                    <button onClick={refreshData} className="rounded-md p-2 text-gray-500 hover:bg-gray-100 dark:text-slate-300 dark:hover:bg-slate-700">
                        <RefreshCw size={16} />
                    </button>
                </div>
                <div className="space-y-2 p-3">
                    {processDefinitions.map((definition) => (
                        <button
                            key={definition.id}
                            onClick={() => setSelectedProcessId(definition.id)}
                            className={`w-full rounded-lg border p-3 text-left transition ${selectedProcessId === definition.id ? 'border-blue-500 bg-blue-50 dark:border-blue-400 dark:bg-blue-950/40' : 'border-gray-200 hover:border-blue-200 hover:bg-gray-50 dark:border-slate-700 dark:hover:bg-slate-700/60'}`}
                        >
                            <div className="flex items-start justify-between gap-3">
                                <div>
                                    <div className="font-medium text-gray-900 dark:text-white">{definition.name}</div>
                                    <div className="mt-1 text-xs text-gray-500 dark:text-slate-400">{definition.code}</div>
                                </div>
                                <span className={`rounded-full px-2 py-1 text-[11px] ${definition.isActive ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300' : 'bg-gray-100 text-gray-600 dark:bg-slate-700 dark:text-slate-300'}`}>
                                    {definition.isActive ? 'Active' : 'Inactive'}
                                </span>
                            </div>
                            <div className="mt-2 flex flex-wrap gap-2 text-[11px] text-gray-500 dark:text-slate-400">
                                <span>{definition.module}</span>
                                <span>{definition.entityType}</span>
                                <span>{definition.triggerType}</span>
                            </div>
                        </button>
                    ))}
                </div>
            </div>

            <div className="space-y-4 rounded-lg border border-gray-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white">Process Designer</h3>
                        <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Register new workflow definitions and review the operational metadata already in the engine.</p>
                    </div>
                    <button
                        onClick={handleCreateProcess}
                        disabled={processSaving}
                        className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {processSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus size={16} />}
                        Create Definition
                    </button>
                </div>

                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Process Code
                        <input
                            value={processDraft.code}
                            onChange={(e) => setProcessDraft((current) => ({ ...current, code: e.target.value.toUpperCase().replace(/\s+/g, '_') }))}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="ACCOUNT_REVIEW"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Process Name
                        <input
                            value={processDraft.name}
                            onChange={(e) => setProcessDraft((current) => ({ ...current, name: e.target.value }))}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="Account Review"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Module
                        <input
                            value={processDraft.module}
                            onChange={(e) => setProcessDraft((current) => ({ ...current, module: e.target.value }))}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="Operations"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Entity Type
                        <input
                            value={processDraft.entityType}
                            onChange={(e) => setProcessDraft((current) => ({ ...current, entityType: e.target.value }))}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="Transaction"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Trigger Type
                        <select
                            value={processDraft.triggerType}
                            onChange={(e) => setProcessDraft((current) => ({ ...current, triggerType: e.target.value }))}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                        >
                            <option value="Manual">Manual</option>
                            <option value="Event">Event</option>
                            <option value="Scheduled">Scheduled</option>
                        </select>
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Trigger Event
                        <input
                            value={processDraft.triggerEventType}
                            onChange={(e) => setProcessDraft((current) => ({ ...current, triggerEventType: e.target.value }))}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="transaction.submitted"
                        />
                    </label>
                </div>

                <div className="grid gap-3 md:grid-cols-3">
                    <InfoCard label="Definitions" value={String(processDefinitions.length)} />
                    <InfoCard label="Active" value={String(processDefinitions.filter((definition) => definition.isActive).length)} />
                    <InfoCard label="System" value={String(processDefinitions.filter((definition) => definition.isSystemProcess).length)} />
                </div>

                {selectedProcess ? (
                    <div className="rounded-lg border border-gray-200 p-5 dark:border-slate-700">
                        <div className="flex items-center justify-between gap-4">
                            <div>
                                <h4 className="text-lg font-semibold text-gray-900 dark:text-white">{selectedProcess.name}</h4>
                                <p className="text-sm text-gray-500 dark:text-slate-400">{selectedProcess.code}</p>
                            </div>
                            <div className="flex gap-2">
                                <StatusPill label={selectedProcess.isActive ? 'Active' : 'Inactive'} tone={selectedProcess.isActive ? 'green' : 'slate'} />
                                <StatusPill label={selectedProcess.isSystemProcess ? 'System' : 'Custom'} tone={selectedProcess.isSystemProcess ? 'amber' : 'blue'} />
                            </div>
                        </div>
                        <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
                            <MetricTile label="Module" value={selectedProcess.module} />
                            <MetricTile label="Entity" value={selectedProcess.entityType} />
                            <MetricTile label="Trigger" value={selectedProcess.triggerType} />
                            <MetricTile label="Event" value={selectedProcess.triggerEventType || 'N/A'} />
                        </div>
                    </div>
                ) : (
                    <div className="rounded-lg border border-dashed border-gray-300 bg-gray-50 p-6 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                        Select a process definition to inspect its metadata.
                    </div>
                )}
            </div>
        </div>
    );

    const renderBranches = () => (
        <div className="overflow-x-auto rounded-lg border border-gray-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <table className="w-full text-sm">
                <thead className="bg-gray-50 text-left text-gray-500 dark:bg-slate-900 dark:text-slate-400">
                    <tr>
                        <th className="p-3">Name</th>
                        <th className="p-3">Code</th>
                        <th className="p-3">Location</th>
                        <th className="p-3">Status</th>
                    </tr>
                </thead>
                <tbody>
                    {branches.map((branch: Branch) => (
                        <tr key={branch.id} className="border-t border-gray-100 dark:border-slate-700">
                            <td className="p-3 font-medium text-gray-900 dark:text-white">{branch.name}</td>
                            <td className="p-3 text-gray-600 dark:text-slate-300">{branch.code}</td>
                            <td className="p-3 text-gray-600 dark:text-slate-300">{branch.location}</td>
                            <td className="p-3 text-gray-600 dark:text-slate-300">{branch.status}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );

    const renderConfig = () => (
        <div className="space-y-6">
            <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div>
                    <h3 className="text-lg font-bold text-gray-900 dark:text-white">Transaction Limits</h3>
                    <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Background loading will not block this form.</p>
                </div>
                <div className="mt-6 grid gap-4 md:grid-cols-2">
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        AML Threshold
                        <input
                            type="number"
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            value={localConfig.amlThreshold}
                            onChange={(e) => updateConfig({ amlThreshold: Number(e.target.value) })}
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Database Provider
                        <select
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            value={localConfig.dbProvider}
                            onChange={(e) => updateConfig({ dbProvider: e.target.value as SystemConfig['dbProvider'] })}
                        >
                            <option value="MYSQL">MySQL</option>
                            <option value="SQLSERVER">SQL Server</option>
                            <option value="POSTGRES">PostgreSQL</option>
                        </select>
                    </label>
                </div>

                <div className="mt-6 rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-slate-700 dark:bg-slate-900/40">
                    <div className="flex items-center justify-between gap-4">
                        <div>
                            <h4 className="text-sm font-semibold text-gray-900 dark:text-white">End of Day Scheduler</h4>
                            <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Run the supported EOD batch automatically once per business date using UTC time.</p>
                        </div>
                        <label className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-slate-300">
                            <input
                                type="checkbox"
                                checked={localConfig.eodScheduler.enabled}
                                onChange={(e) => updateConfig({ eodScheduler: { ...localConfig.eodScheduler, enabled: e.target.checked } })}
                            />
                            Enable Scheduler
                        </label>
                    </div>
                    <div className="mt-4 grid gap-4 md:grid-cols-2">
                        <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                            Run Time (UTC)
                            <input
                                type="time"
                                className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                                value={localConfig.eodScheduler.timeUtc}
                                onChange={(e) => updateConfig({ eodScheduler: { ...localConfig.eodScheduler, timeUtc: e.target.value } })}
                            />
                        </label>
                        <div className="rounded-lg border border-dashed border-gray-300 bg-white p-3 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-950/40 dark:text-slate-300">
                            <div><span className="font-semibold text-gray-900 dark:text-white">Last Run Date:</span> {localConfig.eodScheduler.lastRunDate || 'Not yet scheduled'}</div>
                            <div className="mt-2">The scheduler runs live-supported steps: pre-validation, loan accrual batch, GL close readiness, and date rollover.</div>
                        </div>
                    </div>
                </div>
                <button onClick={handleSaveConfig} className="mt-6 inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
                    <Save size={16} /> Save Configuration
                </button>
            </div>

            <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white">Regulatory Chart of Accounts</h3>
                        <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Seed the standard chart for the regulator in your region and safely top up any missing account codes.</p>
                    </div>
                    <StatusPill label="GL Setup" tone="blue" />
                </div>

                <div className="mt-6 grid gap-4 lg:grid-cols-[minmax(0,240px)_1fr]">
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Region Code
                        <input
                            value={seedRegionCode}
                            onChange={(e) => setSeedRegionCode(e.target.value.toUpperCase())}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="GH"
                            maxLength={4}
                        />
                    </label>
                    <div className="rounded-lg border border-dashed border-gray-300 bg-gray-50 p-4 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                        Use <span className="font-semibold text-gray-900 dark:text-white">GH</span> to apply the Bank of Ghana baseline chart. The seed action is idempotent, so existing standard accounts are left untouched and only missing codes are inserted.
                    </div>
                </div>

                <div className="mt-6 flex flex-wrap items-center gap-3">
                    <button
                        onClick={handleSeedRegulatoryChart}
                        disabled={seedingChart}
                        className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {seedingChart ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw size={16} />}
                        Auto-Seed Standard Chart
                    </button>
                    {seedResult && (
                        <span className="text-sm text-gray-600 dark:text-slate-300">
                            Last run: {seedResult.standardName} with {seedResult.totalStandardAccounts} standard accounts.
                        </span>
                    )}
                </div>

                {seedResult && (
                    <div className="mt-6 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                        <InfoCard label="Region" value={seedResult.regionCode} />
                        <InfoCard label="Inserted" value={String(seedResult.insertedCount)} />
                        <InfoCard label="Existing" value={String(seedResult.existingCount)} />
                        <InfoCard label="Standard Size" value={String(seedResult.totalStandardAccounts)} />
                    </div>
                )}

                {seedResult?.insertedCodes?.length ? (
                    <div className="mt-4 rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800 dark:border-green-900/50 dark:bg-green-950/30 dark:text-green-200">
                        Added account codes: {seedResult.insertedCodes.join(', ')}
                    </div>
                ) : seedResult ? (
                    <div className="mt-4 rounded-lg border border-blue-200 bg-blue-50 p-4 text-sm text-blue-800 dark:border-blue-900/50 dark:bg-blue-950/30 dark:text-blue-200">
                        No new codes were needed. This environment already matches the selected regulatory baseline.
                    </div>
                ) : null}
            </div>
        </div>
    );
const renderAuth = () => (
        <div className="space-y-4 rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <h3 className="text-lg font-bold text-gray-900 dark:text-white">Authentication</h3>
            <label className="flex items-center gap-3 text-sm text-gray-700 dark:text-slate-300">
                <input
                    type="checkbox"
                    checked={localConfig.auth.enabled}
                    onChange={(e) => updateAuthConfig({ enabled: e.target.checked })}
                />
                Enable SSO Authentication
            </label>
            <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                Provider
                <select
                    className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                    value={localConfig.auth.provider}
                    onChange={(e) => updateAuthConfig({ provider: e.target.value as SystemConfig['auth']['provider'] })}
                >
                    <option value="LOCAL">Local</option>
                    <option value="AZURE_AD">Azure AD</option>
                    <option value="LDAP">LDAP</option>
                </select>
            </label>
            <button onClick={handleSaveConfig} className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
                <Save size={16} /> Save Authentication
            </button>
        </div>
    );

    const renderOrass = () => (
        <div className="space-y-6">
            <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white">ORASS Data Submission</h3>
                        <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Configure the supervisory return submission profile, transport endpoint, and fallback operations for Bank of Ghana ORASS processing.</p>
                    </div>
                    <StatusPill label={localConfig.orass.enabled ? 'Enabled' : 'Disabled'} tone={localConfig.orass.enabled ? 'green' : 'slate'} />
                </div>

                <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    <label className="flex items-center gap-3 rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 text-sm font-medium text-gray-700 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                        <input
                            type="checkbox"
                            checked={localConfig.orass.enabled}
                            onChange={(e) => updateOrassConfig({ enabled: e.target.checked })}
                        />
                        Enable ORASS submission profile
                    </label>
                    <label className="flex items-center gap-3 rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 text-sm font-medium text-gray-700 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                        <input
                            type="checkbox"
                            checked={localConfig.orass.autoSubmit}
                            onChange={(e) => updateOrassConfig({ autoSubmit: e.target.checked })}
                        />
                        Auto-submit approved ORASS packages
                    </label>
                    <div className="rounded-lg border border-dashed border-gray-300 bg-white p-4 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-950/40 dark:text-slate-300">
                        <div><span className="font-semibold text-gray-900 dark:text-white">Last Submission:</span> {localConfig.orass.lastSubmissionAt || 'Not recorded'}</div>
                        <div className="mt-2">Keep this profile in <span className="font-semibold">TEST</span> until endpoint, credentials, and acknowledgements are validated with the regulator.</div>
                    </div>
                </div>

                <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Institution Code
                        <input
                            value={localConfig.orass.institutionCode}
                            onChange={(e) => updateOrassConfig({ institutionCode: e.target.value.toUpperCase() })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="BOG institution code"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Submission Mode
                        <select
                            value={localConfig.orass.submissionMode}
                            onChange={(e) => updateOrassConfig({ submissionMode: e.target.value as SystemConfig['orass']['submissionMode'] })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                        >
                            <option value="TEST">Test</option>
                            <option value="PRODUCTION">Production</option>
                        </select>
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Source Report Code
                        <input
                            value={localConfig.orass.sourceReportCode}
                            onChange={(e) => updateOrassConfig({ sourceReportCode: e.target.value.toUpperCase() })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="REG-BOG-DBK-ORASS"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300 md:col-span-2 xl:col-span-3">
                        ORASS Endpoint URL
                        <input
                            value={localConfig.orass.endpointUrl}
                            onChange={(e) => updateOrassConfig({ endpointUrl: e.target.value })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="https://..."
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Service Username
                        <input
                            value={localConfig.orass.username || ''}
                            onChange={(e) => updateOrassConfig({ username: e.target.value })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="orass-user"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Service Password
                        <input
                            type="password"
                            value={localConfig.orass.password || ''}
                            onChange={(e) => updateOrassConfig({ password: e.target.value })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="Stored in config"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Certificate Alias
                        <input
                            value={localConfig.orass.certificateAlias || ''}
                            onChange={(e) => updateOrassConfig({ certificateAlias: e.target.value })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="client-cert-alias"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300">
                        Cutoff Time (UTC)
                        <input
                            type="time"
                            value={localConfig.orass.cutoffTimeUtc}
                            onChange={(e) => updateOrassConfig({ cutoffTimeUtc: e.target.value })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                        />
                    </label>
                    <label className="text-sm font-medium text-gray-700 dark:text-slate-300 md:col-span-2">
                        Fallback Escalation Email
                        <input
                            type="email"
                            value={localConfig.orass.fallbackEmail || ''}
                            onChange={(e) => updateOrassConfig({ fallbackEmail: e.target.value })}
                            className="mt-1 w-full rounded border border-gray-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-900"
                            placeholder="regulatory.ops@institution.com"
                        />
                    </label>
                </div>

                <div className="mt-6 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-200">
                    Store only the setup profile here. Actual ORASS transport, acknowledgement parsing, and evidence archiving still need backend workflow support before auto-submission should be relied on.
                </div>

                <button onClick={handleSaveConfig} className="mt-6 inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
                    <Save size={16} /> Save ORASS Setup
                </button>
            </div>

            <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white">Submission Readiness</h3>
                        <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Readiness checks compare the saved ORASS profile against queued regulatory returns and operational prerequisites.</p>
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                        <button
                            onClick={handleReconcileOrassAcknowledgements}
                            disabled={orassReconciling}
                            className={`rounded-md px-3 py-2 text-sm font-medium text-white ${
                                orassReconciling
                                    ? 'cursor-not-allowed bg-slate-500'
                                    : 'bg-blue-600 hover:bg-blue-700'
                            }`}
                        >
                            {orassReconciling ? 'Reconciling...' : 'Reconcile Acknowledgements'}
                        </button>
                        <button onClick={refreshData} className="rounded-md border border-gray-200 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700">
                            Refresh
                        </button>
                    </div>
                </div>

                {orassReadiness ? (
                    <>
                        <div className="mt-6 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                            <InfoCard label="Profile" value={orassReadiness.profileConfigured ? 'Configured' : 'Incomplete'} />
                            <InfoCard label="Ready Returns" value={String(orassReadiness.returnsReadyForSubmission)} />
                            <InfoCard label="Pending Queue" value={String(orassReadiness.pendingReturns)} />
                            <InfoCard label="Mode" value={orassReadiness.submissionMode} />
                        </div>

                        <div className="mt-6 grid gap-4 lg:grid-cols-2">
                            <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-slate-700 dark:bg-slate-900/40">
                                <h4 className="text-sm font-semibold text-gray-900 dark:text-white">Blocking Requirements</h4>
                                {orassReadiness.missingRequirements.length ? (
                                    <ul className="mt-3 space-y-2 text-sm text-red-700 dark:text-red-300">
                                        {orassReadiness.missingRequirements.map((item) => (
                                            <li key={item}>- {item}</li>
                                        ))}
                                    </ul>
                                ) : (
                                    <p className="mt-3 text-sm text-emerald-700 dark:text-emerald-300">No blocking setup gaps detected for the saved profile.</p>
                                )}
                            </div>

                            <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-slate-700 dark:bg-slate-900/40">
                                <h4 className="text-sm font-semibold text-gray-900 dark:text-white">Operational Notes</h4>
                                <ul className="mt-3 space-y-2 text-sm text-gray-700 dark:text-slate-300">
                                    {(orassReadiness.notes || []).map((item) => (
                                        <li key={item}>- {item}</li>
                                    ))}
                                </ul>
                                <div className="mt-4 text-sm text-gray-600 dark:text-slate-400">
                                    <div><span className="font-semibold text-gray-900 dark:text-white">Source Report:</span> {orassReadiness.sourceReportCode || 'Not set'}</div>
                                    <div className="mt-1"><span className="font-semibold text-gray-900 dark:text-white">Last Prepared Return:</span> {orassReadiness.lastPreparedReturnDate || 'Not available'}</div>
                                    <div className="mt-1"><span className="font-semibold text-gray-900 dark:text-white">Last Submission:</span> {orassReadiness.lastSubmissionAt || 'Not recorded'}</div>
                                </div>
                            </div>
                        </div>

                        {orassReconciliationResult ? (
                            <div className="mt-6 rounded-lg border border-blue-200 bg-blue-50 p-4 dark:border-blue-900/50 dark:bg-blue-950/20">
                                <div className="flex flex-wrap items-center justify-between gap-3">
                                    <h4 className="text-sm font-semibold text-gray-900 dark:text-white">Latest Reconciliation Run</h4>
                                    <StatusPill
                                        label={orassReconciliationResult.updatedCount > 0 ? 'Updated' : 'No Changes'}
                                        tone={orassReconciliationResult.updatedCount > 0 ? 'green' : 'blue'}
                                    />
                                </div>
                                <div className="mt-3 grid gap-3 md:grid-cols-4">
                                    <MetricTile label="Scanned" value={String(orassReconciliationResult.scannedCount)} />
                                    <MetricTile label="Updated" value={String(orassReconciliationResult.updatedCount)} />
                                    <MetricTile label="Pending" value={String(orassReconciliationResult.pendingCount)} />
                                    <MetricTile label="Mode" value={orassReconciliationResult.executionMode} />
                                </div>
                                <div className="mt-3 text-xs text-gray-600 dark:text-slate-400">
                                    Executed at: {orassReconciliationResult.executedAt}
                                </div>
                                {orassReconciliationResult.notes.length ? (
                                    <ul className="mt-3 space-y-1 text-xs text-gray-700 dark:text-slate-300">
                                        {orassReconciliationResult.notes.map((note) => (
                                            <li key={note}>- {note}</li>
                                        ))}
                                    </ul>
                                ) : null}
                            </div>
                        ) : null}
                    </>
                ) : (
                    <div className="mt-6 rounded-lg border border-dashed border-gray-300 bg-gray-50 p-6 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                        Readiness checks will appear here once the ORASS tab finishes loading its configuration and queue posture.
                    </div>
                )}
            </div>

            <div className="grid gap-6 xl:grid-cols-2">
                <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                    <div className="flex items-start justify-between gap-4">
                        <div>
                            <h3 className="text-lg font-bold text-gray-900 dark:text-white">Submission Queue</h3>
                            <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Approved ORASS packages can be submitted here. Pending and rejected items stay visible for operational follow-up.</p>
                        </div>
                        <StatusPill
                            label={orassQueue.some((item) => item.isReadyForSubmission) ? 'Action Required' : 'Monitoring'}
                            tone={orassQueue.some((item) => item.isReadyForSubmission) ? 'amber' : 'blue'}
                        />
                    </div>

                    {orassQueue.length ? (
                        <div className="mt-6 space-y-3">
                            {orassQueue.map((item) => (
                                <div key={item.id} className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-slate-700 dark:bg-slate-900/40">
                                    <div className="flex flex-wrap items-start justify-between gap-3">
                                        <div>
                                            <div className="flex flex-wrap items-center gap-2">
                                                <h4 className="text-sm font-semibold text-gray-900 dark:text-white">{item.returnType}</h4>
                                                <StatusPill
                                                    label={item.submissionStatus}
                                                    tone={item.isReadyForSubmission ? 'green' : item.submissionStatus === 'Rejected' ? 'amber' : 'slate'}
                                                />
                                                <StatusPill
                                                    label={item.validationStatus}
                                                    tone={item.validationStatus === 'ERROR' ? 'amber' : item.validationStatus === 'WARNING' ? 'blue' : 'green'}
                                                />
                                            </div>
                                            <div className="mt-2 text-sm text-gray-600 dark:text-slate-400">
                                                <div>Return date: {item.returnDate}</div>
                                                <div>Reporting period: {item.reportingPeriodStart || 'N/A'} to {item.reportingPeriodEnd || 'N/A'}</div>
                                                <div>Total records: {item.totalRecords}</div>
                                            </div>
                                        </div>

                                        <button
                                            onClick={() => handleSubmitOrassReturn(item.id)}
                                            disabled={!item.isReadyForSubmission || submittingOrassReturnId === item.id}
                                            className={`inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-white ${
                                                !item.isReadyForSubmission
                                                    ? 'cursor-not-allowed bg-gray-400 dark:bg-slate-600'
                                                    : 'bg-blue-600 hover:bg-blue-700'
                                            }`}
                                        >
                                            {submittingOrassReturnId === item.id ? <Loader2 className="h-4 w-4 animate-spin" /> : <UploadCloud size={16} />}
                                            Submit
                                        </button>
                                    </div>

                                    {item.validationMessages.length ? (
                                        <ul className="mt-3 space-y-1 text-xs text-gray-600 dark:text-slate-400">
                                            {item.validationMessages.slice(0, 3).map((message) => (
                                                <li key={`${item.id}-${message}`}>- {message}</li>
                                            ))}
                                        </ul>
                                    ) : (
                                        <p className="mt-3 text-xs text-emerald-700 dark:text-emerald-300">No validation issues captured for this return.</p>
                                    )}
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="mt-6 rounded-lg border border-dashed border-gray-300 bg-gray-50 p-6 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                            No ORASS queue items currently match the configured source report code.
                        </div>
                    )}
                </div>

                <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                    <div className="flex items-start justify-between gap-4">
                        <div>
                            <h3 className="text-lg font-bold text-gray-900 dark:text-white">Submission History</h3>
                            <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">Latest ORASS submissions, references, and reviewer notes captured on the underlying regulatory return.</p>
                        </div>
                        <MetricTile label="Tracked Submissions" value={String(orassHistory.length)} />
                    </div>

                    {orassHistory.length ? (
                        <div className="mt-6 space-y-3">
                            {orassHistory.map((item) => (
                                <div key={item.id} className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-slate-700 dark:bg-slate-900/40">
                                    <div className="flex flex-wrap items-start justify-between gap-3">
                                        <div>
                                            <div className="flex flex-wrap items-center gap-2">
                                                <h4 className="text-sm font-semibold text-gray-900 dark:text-white">{item.returnType}</h4>
                                                <StatusPill
                                                    label={item.submissionStatus}
                                                    tone={item.submissionStatus === 'Submitted' || item.submissionStatus === 'Accepted' ? 'green' : 'amber'}
                                                />
                                                <StatusPill
                                                    label={item.transportStatus}
                                                    tone={item.transportStatus === 'FAILED' ? 'amber' : item.transportStatus === 'SIMULATED_SENT' ? 'blue' : 'green'}
                                                />
                                                <StatusPill
                                                    label={item.acknowledgementStatus}
                                                    tone={item.acknowledgementStatus === 'REJECTED' ? 'amber' : item.acknowledgementStatus === 'ACCEPTED' ? 'green' : 'blue'}
                                                />
                                            </div>
                                            <div className="mt-2 text-sm text-gray-600 dark:text-slate-400">
                                                <div>Return date: {item.returnDate}</div>
                                                <div>Submitted at: {item.submissionDate || 'Not recorded'}</div>
                                                <div>Submitted by: {item.submittedBy || 'System'}</div>
                                                <div>Reference: {item.bogReferenceNumber || 'Pending'}</div>
                                                <div>Acknowledgement ref: {item.acknowledgementReference || 'Not captured'}</div>
                                                <div>Acknowledged at: {item.acknowledgedAt || 'Pending'}</div>
                                            </div>
                                        </div>
                                        <button
                                            onClick={() => setSelectedOrassHistoryId(item.id)}
                                            className={`rounded-md border px-3 py-2 text-sm font-medium ${
                                                selectedOrassHistoryId === item.id
                                                    ? 'border-blue-600 bg-blue-50 text-blue-700 dark:border-blue-400 dark:bg-blue-950/30 dark:text-blue-200'
                                                    : 'border-gray-200 text-gray-700 hover:bg-gray-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700'
                                            }`}
                                        >
                                            View Evidence
                                        </button>
                                    </div>

                                    {item.transportMessage ? (
                                        <p className="mt-3 text-xs text-gray-600 dark:text-slate-400">{item.transportMessage}</p>
                                    ) : null}

                                    {item.validationMessages.length ? (
                                        <ul className="mt-3 space-y-1 text-xs text-gray-600 dark:text-slate-400">
                                            {item.validationMessages.slice(0, 3).map((message) => (
                                                <li key={`${item.id}-history-${message}`}>- {message}</li>
                                            ))}
                                        </ul>
                                    ) : null}
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="mt-6 rounded-lg border border-dashed border-gray-300 bg-gray-50 p-6 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                            Submission history will appear here after the first ORASS package is sent through the managed queue.
                        </div>
                    )}

                    <div className="mt-6 rounded-lg border border-dashed border-gray-300 bg-gray-50 p-4 text-sm text-gray-600 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-300">
                        {selectedOrassHistoryId && orassEvidence ? (
                            <>
                                <div className="flex flex-wrap items-center justify-between gap-3">
                                    <h4 className="text-sm font-semibold text-gray-900 dark:text-white">Evidence for Return #{selectedOrassHistoryId}</h4>
                                    <StatusPill
                                        label={orassEvidence.transportStatus}
                                        tone={orassEvidence.transportStatus === 'FAILED' ? 'amber' : orassEvidence.transportStatus === 'SIMULATED_SENT' ? 'blue' : 'green'}
                                    />
                                </div>
                                <div className="mt-3 grid gap-3 md:grid-cols-2">
                                    <MetricTile label="Transmission ID" value={orassEvidence.transmissionId} />
                                    <MetricTile label="Mode" value={orassEvidence.submissionMode} />
                                    <MetricTile label="Provider Status" value={orassEvidence.providerStatusCode || 'N/A'} />
                                    <MetricTile label="Payload Hash" value={orassEvidence.payloadHash || 'Not captured'} />
                                </div>
                                <div className="mt-3 text-xs text-gray-600 dark:text-slate-400">
                                    <div>Endpoint: {orassEvidence.endpointUrl || 'Not set'}</div>
                                    <div>Submitted at: {orassEvidence.submittedAt || 'Not recorded'}</div>
                                    <div>Acknowledged at: {orassEvidence.acknowledgedAt || 'Pending'}</div>
                                    <div>Transport message: {orassEvidence.transportMessage || 'Not available'}</div>
                                </div>
                                {orassEvidence.notes.length ? (
                                    <ul className="mt-3 space-y-1 text-xs text-gray-600 dark:text-slate-400">
                                        {orassEvidence.notes.map((note) => (
                                            <li key={note}>- {note}</li>
                                        ))}
                                    </ul>
                                ) : null}
                            </>
                        ) : selectedOrassHistoryId && orassEvidenceLoading ? (
                            <div className="flex items-center gap-2">
                                <Loader2 className="h-4 w-4 animate-spin" />
                                Loading ORASS evidence...
                            </div>
                        ) : (
                            <div>Select a submission record to inspect transport evidence and acknowledgement metadata.</div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );

    const renderUnavailableFeature = (title: string, description: string) => (
        <div className="rounded-lg border border-dashed border-gray-300 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <h3 className="text-lg font-bold text-gray-900 dark:text-white">{title}</h3>
            <p className="mt-2 text-sm text-gray-600 dark:text-slate-300">{description}</p>
        </div>
    );

    const renderSystemInfo = () => (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <InfoCard label="Users" value={String(users.length)} />
            <InfoCard label="Roles" value={String(roles.length)} />
            <InfoCard label="Branches" value={String(branches.length)} />
            <InfoCard label="Database" value={localConfig.dbProvider} />
        </div>
    );

    const renderContent = () => {
        switch (activeTab) {
            case 'USERS':
                return renderUsers();
            case 'ROLES':
                return renderRoles();
            case 'AUTH':
                return renderAuth();
            case 'ORASS':
                return renderOrass();
            case 'MENU':
                return onSaveMenu && onDeleteMenu
                    ? <MenuEditor menuItems={menuItems} workflows={workflows} forms={customForms} roles={roles as any} onSaveMenu={onSaveMenu} onDeleteMenu={onDeleteMenu} pageTargets={pageTargets} currentUserId={currentUserId} />
                    : renderUnavailableFeature('Menu Config is unavailable', 'Menu editing is not connected in the current dashboard route. This prevents the tab from crashing while the parent wiring is still incomplete.');
            case 'CONFIG':
                return renderConfig();
            case 'BRANCHES':
                return renderBranches();
            case 'SYSTEM_INFO':
                return renderSystemInfo();
            default:
                return renderUsers();
        }
    };

    return (
        <div className="flex h-full flex-col overflow-hidden rounded-xl bg-gray-100 dark:bg-slate-900">
            <div className="border-b border-gray-200 bg-white px-6 py-4 dark:border-slate-700 dark:bg-slate-800">
                <div className="flex items-center gap-3">
                    <div className="rounded-lg bg-blue-100 p-2 text-blue-700">
                        <SettingsIcon size={24} />
                    </div>
                    <div>
                        <h1 className="text-xl font-bold text-gray-900 dark:text-white">System Administration</h1>
                        <p className="text-xs text-gray-500 dark:text-slate-400">Configure users, permissions, workflows, and platform settings without blocking the screen on startup.</p>
                    </div>
                </div>

                <div className="mt-4 flex gap-6 overflow-x-auto">
                    {tabs.map((tab) => (
                        <button
                            key={tab.id}
                            onClick={() => setActiveTab(tab.id)}
                            className={`flex items-center gap-2 whitespace-nowrap border-b-2 pb-2 text-sm font-medium ${activeTab === tab.id ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-500 dark:text-slate-400'}`}
                        >
                            {tab.icon}
                            {tab.label}
                        </button>
                    ))}
                </div>

                {(isLoadingData || loadError || configDirty || roleDirty) && (
                    <div className={`mt-4 rounded-lg border px-4 py-3 text-sm ${loadError ? 'border-red-200 bg-red-50 text-red-700' : configDirty || roleDirty ? 'border-amber-200 bg-amber-50 text-amber-700' : 'border-blue-200 bg-blue-50 text-blue-700'}`}>
                        <div className="flex items-center justify-between gap-3">
                            <div className="flex items-center gap-2">
                                {isLoadingData ? <Loader2 className="h-4 w-4 animate-spin" /> : <Activity size={16} />}
                                <span>
                                    {loadError || (configDirty
                                        ? 'You have unsaved configuration changes. Background refresh will not overwrite them.'
                                        : roleDirty
                                            ? 'You have unsaved role changes. Save them before switching to another role.'
                                            : 'Loading data for this tab in the background...')}
                                </span>
                            </div>
                            <button onClick={refreshData} className="font-medium hover:underline">
                                Retry
                            </button>
                        </div>
                    </div>
                )}
            </div>

            <div className="flex-1 overflow-auto p-6">{renderContent()}</div>

            {modalMessage && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
                    <div className="w-96 rounded-lg bg-white p-6 shadow-xl dark:bg-slate-800">
                        <div className="mb-4 text-center text-gray-800 dark:text-slate-100">{modalMessage}</div>
                        <button onClick={() => setModalMessage(null)} className="w-full rounded bg-blue-600 px-4 py-2 text-sm font-bold text-white">
                            OK
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}

function InfoCard({ label, value }: { label: string; value: string }) {
    return (
        <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <p className="text-sm text-gray-500 dark:text-slate-400">{label}</p>
            <p className="mt-2 text-2xl font-bold text-gray-900 dark:text-white">{value}</p>
        </div>
    );
}

function MetricTile({ label, value }: { label: string; value: string }) {
    return (
        <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-slate-700 dark:bg-slate-900/40">
            <p className="text-xs uppercase tracking-wide text-gray-500 dark:text-slate-400">{label}</p>
            <p className="mt-2 text-sm font-semibold text-gray-900 dark:text-white">{value}</p>
        </div>
    );
}

function StatusPill({ label, tone }: { label: string; tone: 'green' | 'blue' | 'amber' | 'slate' }) {
    const toneClasses = {
        green: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
        blue: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
        amber: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
        slate: 'bg-gray-100 text-gray-700 dark:bg-slate-700 dark:text-slate-300',
    };

    return <span className={`rounded-full px-2 py-1 text-xs font-medium ${toneClasses[tone]}`}>{label}</span>;
}








