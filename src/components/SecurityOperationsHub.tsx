import React, { useEffect, useMemo, useState } from 'react';
import {
  Activity,
  AlertTriangle,
  Ban,
  ChevronRight,
  Lock,
  Plus,
  RefreshCw,
  Search,
  Server,
  Shield,
  ShieldAlert,
  Unlock,
} from 'lucide-react';
import { Branch, IrregularTransaction, SecurityAlert, SecurityDevice, SecuritySummary, StaffUser } from '../../types';
import { securityService } from '../services/securityService';
import { adminService } from '../services/adminService';
import { ApiError } from '../services/httpClient';
import { Permissions } from '../../lib/Permissions';

type DeviceDraft = {
  deviceId: string;
  name: string;
  branchId: string;
  assignedStaffId: string;
  serialNumber: string;
  ipAddress: string;
  softwareVersion: string;
  minimumSupportedVersion: string;
  notes: string;
};

type SecurityTab = 'overview' | 'terminal-setup' | 'device-management' | 'monitoring';

const defaultDraft: DeviceDraft = {
  deviceId: '',
  name: '',
  branchId: '',
  assignedStaffId: '',
  serialNumber: '',
  ipAddress: '',
  softwareVersion: '2.0.0',
  minimumSupportedVersion: '2.0.0',
  notes: '',
};

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.data?.message || error.data?.error || error.message || fallback;
  }

  return error instanceof Error ? error.message : fallback;
}

export default function SecurityOperationsHub({
  userPermissions,
}: {
  userPermissions: string[];
}) {
  const [activeTab, setActiveTab] = useState<SecurityTab>('overview');
  const [summary, setSummary] = useState<SecuritySummary | null>(null);
  const [devices, setDevices] = useState<SecurityDevice[]>([]);
  const [alerts, setAlerts] = useState<SecurityAlert[]>([]);
  const [irregularities, setIrregularities] = useState<IrregularTransaction[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [users, setUsers] = useState<StaffUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [busyDeviceId, setBusyDeviceId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [draft, setDraft] = useState<DeviceDraft>(defaultDraft);

  const canManageDevices = userPermissions.includes(Permissions.Users.Manage);

  const tabs: Array<{ id: SecurityTab; label: string; helper: string }> = [
    { id: 'overview', label: 'Overview', helper: 'Security posture and quick actions' },
    { id: 'terminal-setup', label: 'Terminal Setup', helper: 'Register and update cash terminals' },
    { id: 'device-management', label: 'Device Management', helper: 'Block, isolate, search, and review devices' },
    { id: 'monitoring', label: 'Monitoring', helper: 'Irregular transactions and security alerts' },
  ];

  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [summaryData, devicesData, alertsData, irregularData, branchesData, usersData] = await Promise.all([
        securityService.getSummary(),
        securityService.getDevices(),
        securityService.getAlerts(),
        securityService.getIrregularTransactions(),
        adminService.getBranches().catch(() => [] as Branch[]),
        adminService.getUsers().catch(() => [] as StaffUser[]),
      ]);

      setSummary(summaryData);
      setDevices(devicesData);
      setAlerts(alertsData);
      setIrregularities(irregularData);
      setBranches(branchesData);
      setUsers(usersData);
      setDraft((current) => ({
        ...current,
        minimumSupportedVersion: current.minimumSupportedVersion || summaryData.minimumSupportedVersion,
      }));
    } catch (loadError) {
      setError(getErrorMessage(loadError, 'Failed to load security operations data.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const filteredDevices = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return devices;
    }

    return devices.filter((device) => {
      const haystack = [
        device.id,
        device.name,
        device.status,
        device.softwareStatus,
        device.branchName,
        device.assignedStaffName,
        device.softwareVersion,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();

      return haystack.includes(query);
    });
  }, [devices, search]);

  const resetDraft = () => {
    setDraft({
      ...defaultDraft,
      minimumSupportedVersion: summary?.minimumSupportedVersion || defaultDraft.minimumSupportedVersion,
      softwareVersion: summary?.minimumSupportedVersion || defaultDraft.softwareVersion,
    });
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    setNotice(null);

    try {
      await securityService.registerDevice({
        deviceId: draft.deviceId,
        name: draft.name,
        branchId: draft.branchId || undefined,
        assignedStaffId: draft.assignedStaffId || undefined,
        serialNumber: draft.serialNumber || undefined,
        ipAddress: draft.ipAddress || undefined,
        softwareVersion: draft.softwareVersion,
        minimumSupportedVersion: draft.minimumSupportedVersion || undefined,
        notes: draft.notes || undefined,
        deviceType: 'CASH_TERMINAL',
      });

      setNotice(`Cash terminal ${draft.deviceId} saved successfully.`);
      resetDraft();
      await loadData();
      setActiveTab('device-management');
    } catch (submitError) {
      setError(getErrorMessage(submitError, 'Unable to save device setup.'));
    } finally {
      setSubmitting(false);
    }
  };

  const loadDeviceIntoForm = (device: SecurityDevice) => {
    setDraft({
      deviceId: device.id,
      name: device.name,
      branchId: device.branchId || '',
      assignedStaffId: device.assignedStaffId || '',
      serialNumber: device.serialNumber || '',
      ipAddress: device.ipAddress || '',
      softwareVersion: device.softwareVersion,
      minimumSupportedVersion: device.minimumSupportedVersion,
      notes: device.notes || '',
    });
    setNotice(`Loaded ${device.name} into terminal setup.`);
    setActiveTab('terminal-setup');
  };
  const handleDeviceAction = async (deviceId: string, action: 'BLOCK' | 'UNBLOCK' | 'ISOLATE' | 'FLAG') => {
    setBusyDeviceId(deviceId);
    setError(null);
    setNotice(null);

    try {
      await securityService.executeDeviceAction(deviceId, action);
      setNotice(`${action.charAt(0)}${action.slice(1).toLowerCase()} action applied to ${deviceId}.`);
      await loadData();
    } catch (actionError) {
      setError(getErrorMessage(actionError, `Unable to ${action.toLowerCase()} device.`));
    } finally {
      setBusyDeviceId(null);
    }
  };

  const handleScanOutdated = async () => {
    setSubmitting(true);
    setError(null);
    setNotice(null);

    try {
      const result = await securityService.scanOutdatedDevices();
      setNotice(`Outdated software scan complete. ${result.outdatedCount} devices are outdated and ${result.flaggedCount} were newly flagged.`);
      await loadData();
      setActiveTab('device-management');
    } catch (scanError) {
      setError(getErrorMessage(scanError, 'Unable to scan for outdated devices.'));
    } finally {
      setSubmitting(false);
    }
  };

  const renderStatusPill = (label: string, tone: 'green' | 'amber' | 'red' | 'slate' | 'blue') => {
    const tones = {
      green: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
      amber: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
      red: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
      slate: 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300',
      blue: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
    };

    return <span className={`rounded-full px-2 py-1 text-xs font-semibold ${tones[tone]}`}>{label}</span>;
  };

  const metrics = [
    { label: 'Registered Devices', value: summary?.registeredDevices ?? 0, icon: <Server className="h-5 w-5 text-blue-600 dark:text-blue-300" /> },
    { label: 'Active Devices', value: summary?.activeDevices ?? 0, icon: <Activity className="h-5 w-5 text-emerald-600 dark:text-emerald-300" /> },
    { label: 'Blocked', value: summary?.blockedDevices ?? 0, icon: <Ban className="h-5 w-5 text-red-600 dark:text-red-300" /> },
    { label: 'Isolated', value: summary?.isolatedDevices ?? 0, icon: <Lock className="h-5 w-5 text-amber-600 dark:text-amber-300" /> },
    { label: 'Outdated Software', value: summary?.outdatedDevices ?? 0, icon: <AlertTriangle className="h-5 w-5 text-orange-600 dark:text-orange-300" /> },
    { label: 'Irregular Activity', value: summary?.irregularActivityCount ?? 0, icon: <ShieldAlert className="h-5 w-5 text-purple-600 dark:text-purple-300" /> },
  ];

  const renderOverview = () => (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
        {metrics.map((card) => (
          <div key={card.label} className="screen-stat p-5">
            <div className="flex items-center justify-between">
              {card.icon}
              {loading && <RefreshCw className="h-4 w-4 animate-spin text-slate-400" />}
            </div>
            <div className="mt-4 text-2xl font-bold text-slate-900 dark:text-white">{card.value}</div>
            <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{card.label}</div>
          </div>
        ))}
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="screen-panel p-6">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">System Snapshot</h3>
              <p className="text-sm text-slate-500 dark:text-slate-400">Review device posture and monitoring priorities from one screen.</p>
            </div>
            {renderStatusPill(`Min ${summary?.minimumSupportedVersion || '2.0.0'}`, 'blue')}
          </div>

          <div className="mt-5 grid gap-4 md:grid-cols-3">
            <button onClick={() => setActiveTab('terminal-setup')} className="group rounded-[22px] border border-slate-200/80 bg-slate-50/85 p-5 text-left transition hover:-translate-y-0.5 hover:border-teal-300 hover:bg-white dark:border-slate-700 dark:bg-slate-800/55 dark:hover:border-teal-500/40 dark:hover:bg-slate-800">
              <div className="flex items-center justify-between">
                <Plus className="h-5 w-5 text-teal-700 dark:text-teal-300" />
                <ChevronRight className="h-4 w-4 text-slate-400 transition group-hover:translate-x-0.5" />
              </div>
              <div className="mt-4 font-semibold text-slate-900 dark:text-white">Terminal Setup</div>
              <div className="mt-1 text-sm text-slate-500 dark:text-slate-400">Register a new cash terminal or update an existing workstation.</div>
            </button>
            <button onClick={() => setActiveTab('device-management')} className="group rounded-[22px] border border-slate-200/80 bg-slate-50/85 p-5 text-left transition hover:-translate-y-0.5 hover:border-teal-300 hover:bg-white dark:border-slate-700 dark:bg-slate-800/55 dark:hover:border-teal-500/40 dark:hover:bg-slate-800">
              <div className="flex items-center justify-between">
                <Server className="h-5 w-5 text-teal-700 dark:text-teal-300" />
                <ChevronRight className="h-4 w-4 text-slate-400 transition group-hover:translate-x-0.5" />
              </div>
              <div className="mt-4 font-semibold text-slate-900 dark:text-white">Device Management</div>
              <div className="mt-1 text-sm text-slate-500 dark:text-slate-400">Search devices, review versions, and block or isolate risky endpoints.</div>
            </button>
            <button onClick={() => setActiveTab('monitoring')} className="group rounded-[22px] border border-slate-200/80 bg-slate-50/85 p-5 text-left transition hover:-translate-y-0.5 hover:border-teal-300 hover:bg-white dark:border-slate-700 dark:bg-slate-800/55 dark:hover:border-teal-500/40 dark:hover:bg-slate-800">
              <div className="flex items-center justify-between">
                <ShieldAlert className="h-5 w-5 text-teal-700 dark:text-teal-300" />
                <ChevronRight className="h-4 w-4 text-slate-400 transition group-hover:translate-x-0.5" />
              </div>
              <div className="mt-4 font-semibold text-slate-900 dark:text-white">Monitoring</div>
              <div className="mt-1 text-sm text-slate-500 dark:text-slate-400">Review irregular activity findings and security alerts in one stream.</div>
            </button>
          </div>
        </div>

        <div className="screen-panel p-6">
          <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Priority Queue</h3>
          <div className="mt-5 space-y-3">
            <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
              <div className="flex items-center justify-between gap-3">
                <span className="font-medium text-slate-900 dark:text-white">Outdated terminal software</span>
                {renderStatusPill(`${summary?.outdatedDevices ?? 0} devices`, (summary?.outdatedDevices ?? 0) > 0 ? 'amber' : 'green')}
              </div>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Run the version review to flag workstations still below the supported release.</p>
            </div>
            <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
              <div className="flex items-center justify-between gap-3">
                <span className="font-medium text-slate-900 dark:text-white">Irregular transaction findings</span>
                {renderStatusPill(`${irregularities.length} findings`, irregularities.length > 0 ? 'amber' : 'green')}
              </div>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Look for unusual amounts, off-hours activity, and rapid repeat patterns.</p>
            </div>
            <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
              <div className="flex items-center justify-between gap-3">
                <span className="font-medium text-slate-900 dark:text-white">Security alert volume</span>
                {renderStatusPill(`${alerts.length} recent`, alerts.length > 0 ? 'amber' : 'green')}
              </div>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Audit-backed failed sign-in and large transaction alerts are flowing normally.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );

  const renderTerminalSetup = () => (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
      <form onSubmit={handleSubmit} className="space-y-5 rounded-[28px] border border-white/60 bg-white/88 p-6 shadow-[0_28px_80px_-48px_rgba(15,23,42,0.45)] backdrop-blur dark:border-white/10 dark:bg-slate-900/78">
        <div className="flex items-center justify-between gap-3">
          <div>
            <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Cash Terminal Setup</h3>
            <p className="text-sm text-slate-500 dark:text-slate-400">Register a teller workstation or update one that is already deployed.</p>
          </div>
          {renderStatusPill(`Min ${summary?.minimumSupportedVersion || draft.minimumSupportedVersion}`, 'blue')}
        </div>

        <div className="grid gap-4 lg:grid-cols-2">
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Device ID
            <input value={draft.deviceId} onChange={(event) => setDraft((current) => ({ ...current, deviceId: event.target.value.toUpperCase() }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500" placeholder="TERM-HO-01" />
          </label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Display Name
            <input value={draft.name} onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500" placeholder="Head Office Counter 1" />
          </label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Branch
            <select value={draft.branchId} onChange={(event) => setDraft((current) => ({ ...current, branchId: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500">
              <option value="">Select branch</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>{branch.name}</option>
              ))}
            </select>
          </label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Assigned Staff
            <select value={draft.assignedStaffId} onChange={(event) => setDraft((current) => ({ ...current, assignedStaffId: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500">
              <option value="">Select staff</option>
              {users.map((user) => (
                <option key={user.id} value={user.id}>{user.name}</option>
              ))}
            </select>
          </label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Software Version
            <input value={draft.softwareVersion} onChange={(event) => setDraft((current) => ({ ...current, softwareVersion: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500" placeholder="2.0.0" />
          </label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Minimum Supported
            <input value={draft.minimumSupportedVersion} onChange={(event) => setDraft((current) => ({ ...current, minimumSupportedVersion: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500" placeholder="2.0.0" />
          </label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Serial Number
            <input value={draft.serialNumber} onChange={(event) => setDraft((current) => ({ ...current, serialNumber: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500" />
          </label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            IP Address
            <input value={draft.ipAddress} onChange={(event) => setDraft((current) => ({ ...current, ipAddress: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500" placeholder="10.0.0.25" />
          </label>
        </div>

        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
          Notes
          <textarea value={draft.notes} onChange={(event) => setDraft((current) => ({ ...current, notes: event.target.value }))} rows={4} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white dark:placeholder:text-slate-500" placeholder="Counter terminal for cash deposits and withdrawals" />
        </label>

        <div className="flex flex-wrap gap-3">
          <button disabled={submitting || !canManageDevices} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
            {submitting ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
            Save Terminal
          </button>
          <button type="button" onClick={resetDraft} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900/70 dark:text-slate-200 dark:hover:bg-slate-800">
            Reset Form
          </button>
        </div>
      </form>

      <div className="space-y-4 rounded-[28px] border border-white/60 bg-white/88 p-6 shadow-[0_28px_80px_-48px_rgba(15,23,42,0.45)] backdrop-blur dark:border-white/10 dark:bg-slate-900/78">
        <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Setup Guide</h3>
        <div className="space-y-3 text-sm text-slate-500 dark:text-slate-400">
          <div className="rounded-[22px] border border-slate-200/80 bg-slate-50/85 p-4 dark:border-slate-700/80 dark:bg-slate-800/55">
            <div className="font-medium text-slate-900 dark:text-white">1. Identify the workstation</div>
            <div className="mt-1">Use a branch-specific terminal code so it is easy to locate during incident response.</div>
          </div>
          <div className="rounded-[22px] border border-slate-200/80 bg-slate-50/85 p-4 dark:border-slate-700/80 dark:bg-slate-800/55">
            <div className="font-medium text-slate-900 dark:text-white">2. Assign a responsible operator</div>
            <div className="mt-1">Link the terminal to the active teller or branch custodian for accountability.</div>
          </div>
          <div className="rounded-[22px] border border-slate-200/80 bg-slate-50/85 p-4 dark:border-slate-700/80 dark:bg-slate-800/55">
            <div className="font-medium text-slate-900 dark:text-white">3. Confirm version posture</div>
            <div className="mt-1">Keep the installed version at or above the supported release before enabling live cash operations.</div>
          </div>
        </div>
      </div>
    </div>
  );

  const renderDeviceManagement = () => (
    <div className="space-y-6">
      <div className="rounded-[28px] border border-white/60 bg-white/88 p-6 shadow-[0_28px_80px_-48px_rgba(15,23,42,0.45)] backdrop-blur dark:border-white/10 dark:bg-slate-900/78">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Managed Devices</h3>
            <p className="text-sm text-slate-500 dark:text-slate-400">Search, review versions, and apply response actions from the device management screen.</p>
          </div>
          <div className="flex flex-wrap gap-3">
            <div className="relative w-full max-w-sm">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search device, staff, branch, status..." className="w-full rounded-2xl border border-slate-300/90 bg-white py-3 pl-10 pr-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" />
            </div>
            {canManageDevices && (
              <button onClick={handleScanOutdated} disabled={submitting} className="inline-flex items-center gap-2 rounded-2xl bg-amber-400 px-4 py-3 text-sm font-semibold text-slate-950 transition hover:bg-amber-300 disabled:opacity-60">
                <ShieldAlert className="h-4 w-4" />
                Scan Outdated Software
              </button>
            )}
          </div>
        </div>

        <div className="mt-5 overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-slate-50/90 text-left text-[11px] uppercase tracking-[0.18em] text-slate-500 dark:bg-slate-800/80 dark:text-slate-400">
              <tr>
                <th className="px-4 py-3">Terminal</th>
                <th className="px-4 py-3">Assigned</th>
                <th className="px-4 py-3">Version</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredDevices.length === 0 ? (
                <tr>
                  <td colSpan={5} className="py-8 text-center text-slate-500 dark:text-slate-400">No devices registered yet.</td>
                </tr>
              ) : filteredDevices.map((device) => (
                <tr key={device.id} className="border-t border-slate-200/80 dark:border-slate-700/80">
                  <td className="px-4 py-4 align-top">
                    <div className="font-medium text-slate-900 dark:text-white">{device.name}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">{device.id} | {device.branchName || device.branchId || 'Unassigned branch'}</div>
                    {device.blockReason && <div className="mt-1 text-xs text-amber-600 dark:text-amber-300">{device.blockReason}</div>}
                  </td>
                  <td className="px-4 py-4 align-top text-slate-600 dark:text-slate-300">
                    <div>{device.assignedStaffName || device.assignedStaffId || 'No staff linked'}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">{device.ipAddress || 'No IP captured'}</div>
                  </td>
                  <td className="px-4 py-4 align-top">
                    <div className="font-medium text-slate-900 dark:text-white">{device.softwareVersion}</div>
                    <div className="mt-1">{renderStatusPill(device.softwareStatus, device.softwareStatus === 'OUTDATED' ? 'amber' : 'green')}</div>
                  </td>
                  <td className="px-4 py-4 align-top">{renderStatusPill(device.status, device.status === 'ACTIVE' ? 'green' : device.status === 'FLAGGED' ? 'amber' : 'red')}</td>
                  <td className="px-4 py-4 align-top">
                    <div className="flex flex-wrap gap-2">
                      <button onClick={() => loadDeviceIntoForm(device)} className="rounded-2xl border border-slate-300 bg-white px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900/70 dark:text-slate-200 dark:hover:bg-slate-800">Open Setup</button>
                      {device.status === 'BLOCKED' ? (
                        <button disabled={busyDeviceId === device.id || !canManageDevices} onClick={() => handleDeviceAction(device.id, 'UNBLOCK')} className="inline-flex items-center gap-1 rounded-2xl bg-emerald-600 px-3 py-2 text-xs font-semibold text-white transition hover:bg-emerald-700 disabled:opacity-60">
                          <Unlock className="h-3.5 w-3.5" /> Unblock
                        </button>
                      ) : (
                        <button disabled={busyDeviceId === device.id || !canManageDevices} onClick={() => handleDeviceAction(device.id, 'BLOCK')} className="inline-flex items-center gap-1 rounded-2xl bg-rose-600 px-3 py-2 text-xs font-semibold text-white transition hover:bg-rose-700 disabled:opacity-60">
                          <Ban className="h-3.5 w-3.5" /> Block
                        </button>
                      )}
                      <button disabled={busyDeviceId === device.id || !canManageDevices} onClick={() => handleDeviceAction(device.id, 'ISOLATE')} className="inline-flex items-center gap-1 rounded-2xl bg-amber-400 px-3 py-2 text-xs font-semibold text-slate-950 transition hover:bg-amber-300 disabled:opacity-60">
                        <Lock className="h-3.5 w-3.5" /> Isolate
                      </button>
                      {device.softwareStatus === 'OUTDATED' && (
                        <button disabled={busyDeviceId === device.id || !canManageDevices} onClick={() => handleDeviceAction(device.id, 'FLAG')} className="inline-flex items-center gap-1 rounded-2xl bg-slate-950 px-3 py-2 text-xs font-semibold text-white transition hover:bg-slate-800 disabled:opacity-60 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">
                          <AlertTriangle className="h-3.5 w-3.5" /> Flag
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );

  const renderMonitoring = () => (
    <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
      <div className="rounded-[28px] border border-white/60 bg-white/85 p-6 shadow-[0_28px_80px_-48px_rgba(15,23,42,0.45)] backdrop-blur dark:border-white/10 dark:bg-slate-900/75">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Irregular Transaction Monitor</h3>
            <p className="text-sm text-slate-500 dark:text-slate-400">Review backend-detected activity spikes, unusual terminal behavior, and follow-up triggers.</p>
          </div>
          <div className="rounded-full bg-rose-100 px-3 py-1 text-xs font-semibold text-rose-700 dark:bg-rose-500/10 dark:text-rose-200">
            {irregularities.length} flagged
          </div>
        </div>

        <div className="mt-5 space-y-3">
          {irregularities.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
              No irregular transaction patterns detected right now.
            </div>
          ) : irregularities.slice(0, 12).map((item) => (
            <div key={item.id} className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/55">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <div className="text-sm font-semibold text-slate-900 dark:text-white">{item.summary}</div>
                  <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{item.reference || item.transactionId || item.type || 'Transaction anomaly'}</div>
                </div>
                <span className="rounded-full bg-amber-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-amber-700 dark:bg-amber-500/10 dark:text-amber-200">
                  {item.severity || 'review'}
                </span>
              </div>
              <p className="mt-3 text-sm text-slate-600 dark:text-slate-300">
                {item.customerName
                  ? `${item.customerName} triggered a ${item.type.toLowerCase()} worth ${item.amount.toLocaleString()} with risk score ${item.riskScore}.`
                  : `This ${item.type.toLowerCase()} was highlighted for investigation with risk score ${item.riskScore}.`}
              </p>
              <div className="mt-3 text-xs text-slate-500 dark:text-slate-400">{item.detectedAt ? new Date(item.detectedAt).toLocaleString() : 'Timestamp unavailable'}</div>
            </div>
          ))}
        </div>
      </div>

      <div className="rounded-[28px] border border-white/60 bg-white/85 p-6 shadow-[0_28px_80px_-48px_rgba(15,23,42,0.45)] backdrop-blur dark:border-white/10 dark:bg-slate-900/75">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Security Alerts</h3>
            <p className="text-sm text-slate-500 dark:text-slate-400">Escalations from device health checks, policy violations, and operator actions.</p>
          </div>
          <div className="rounded-full bg-slate-900 px-3 py-1 text-xs font-semibold text-white dark:bg-white dark:text-slate-900">
            {alerts.length} total
          </div>
        </div>

        <div className="mt-5 space-y-3">
          {alerts.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
              No security alerts have been raised yet.
            </div>
          ) : alerts.slice(0, 12).map((alert) => (
            <div key={alert.id} className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/55">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <div className="text-sm font-semibold text-slate-900 dark:text-white">{alert.action.replace(/_/g, ' ')}</div>
                  <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{alert.entityId || alert.entityType || 'Platform-wide alert'}</div>
                </div>
                <span className="rounded-full bg-slate-200 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-700 dark:bg-slate-700 dark:text-slate-200">
                  {alert.status || 'info'}
                </span>
              </div>
              <p className="mt-3 text-sm text-slate-600 dark:text-slate-300">{alert.description}</p>
              <div className="mt-3 text-xs text-slate-500 dark:text-slate-400">{alert.createdAt ? new Date(alert.createdAt).toLocaleString() : 'Timestamp unavailable'}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );

  const renderActiveTab = () => {
    switch (activeTab) {
      case 'terminal-setup':
        return renderTerminalSetup();
      case 'device-management':
        return renderDeviceManagement();
      case 'monitoring':
        return renderMonitoring();
      default:
        return renderOverview();
    }
  };

  return (
    <div className="space-y-6 text-slate-900 dark:text-white">
      <section className="screen-hero p-6 sm:p-8">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-3xl">
            <div className="screen-badge inline-flex items-center gap-2">
              <Shield className="h-3.5 w-3.5" />
              Security operations
            </div>
            <h1 className="mt-4 text-3xl font-heading font-bold tracking-tight text-slate-950 dark:text-white sm:text-4xl">Security Operations</h1>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">
              Manage terminal rollout, device governance, and transaction surveillance with clear action paths.
            </p>
          </div>

          <button onClick={loadData} disabled={loading} className="screen-button-secondary inline-flex items-center justify-center gap-2 disabled:cursor-not-allowed disabled:opacity-60">
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh data
          </button>
        </div>
      </section>

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-200">
          {error}
        </div>
      )}

      {notice && (
        <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-200">
          {notice}
        </div>
      )}

      <section className="screen-panel p-3">
        <div className="flex flex-wrap gap-2">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-4 py-2.5 text-sm font-medium transition ${
                activeTab === tab.id
                  ? 'screen-tab screen-tab-active'
                  : 'screen-tab'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </section>

      {renderActiveTab()}
    </div>
  );
}
