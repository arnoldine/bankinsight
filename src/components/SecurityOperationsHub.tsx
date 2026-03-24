import React, { useEffect, useMemo, useState } from 'react';
import { Activity, AlertTriangle, Clock3, Globe, Laptop, Plus, RefreshCw, Search, Shield, ShieldAlert } from 'lucide-react';
import { Branch, FailedLoginAttempt, IrregularTransaction, SecurityAlert, SecurityDevice, SecuritySession, SecuritySummary, StaffUser } from '../../types';
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

type SecurityTab = 'overview' | 'registry' | 'investigations' | 'setup';

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

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof ApiError) {
    return error.data?.message || error.data?.error || error.message || fallback;
  }
  return error instanceof Error ? error.message : fallback;
};

const formatDateTime = (value?: string) => (value ? new Date(value).toLocaleString() : 'Not available');

const pillTone = (tone: 'green' | 'amber' | 'red' | 'slate' | 'blue') =>
  ({
    green: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
    amber: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
    red: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
    slate: 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300',
    blue: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
  })[tone];

const renderPill = (label: string, tone: 'green' | 'amber' | 'red' | 'slate' | 'blue') => (
  <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.16em] ${pillTone(tone)}`}>{label}</span>
);

const lifecycleTone = (device: SecurityDevice): 'green' | 'amber' | 'red' | 'slate' | 'blue' => {
  if (device.lifecycleState === 'NEW_OBSERVED') return 'blue';
  if (device.lifecycleState === 'SUSPICIOUS') return 'amber';
  if (device.lifecycleState === 'RESTRICTED' || device.lifecycleState === 'REVOKED') return 'red';
  if (device.lifecycleState === 'MONITORED') return 'slate';
  return 'green';
};

const riskTone = (risk: SecurityDevice['riskLevel']): 'green' | 'amber' | 'red' => risk === 'HIGH' ? 'red' : risk === 'MEDIUM' ? 'amber' : 'green';

export default function SecurityOperationsHub({ userPermissions }: { userPermissions: string[] }) {
  const [activeTab, setActiveTab] = useState<SecurityTab>('overview');
  const [summary, setSummary] = useState<SecuritySummary | null>(null);
  const [devices, setDevices] = useState<SecurityDevice[]>([]);
  const [alerts, setAlerts] = useState<SecurityAlert[]>([]);
  const [irregularities, setIrregularities] = useState<IrregularTransaction[]>([]);
  const [failedLogins, setFailedLogins] = useState<FailedLoginAttempt[]>([]);
  const [sessions, setSessions] = useState<SecuritySession[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [users, setUsers] = useState<StaffUser[]>([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [busyDeviceId, setBusyDeviceId] = useState<string | null>(null);
  const [draft, setDraft] = useState<DeviceDraft>(defaultDraft);
  const canManageDevices = userPermissions.includes(Permissions.Users.Manage);

  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [summaryData, devicesData, alertsData, irregularData, failedData, sessionData, branchData, userData] = await Promise.all([
        securityService.getSummary(),
        securityService.getDevices(),
        securityService.getAlerts(),
        securityService.getIrregularTransactions(),
        securityService.getFailedLogins().catch(() => [] as FailedLoginAttempt[]),
        securityService.getSessions().catch(() => [] as SecuritySession[]),
        adminService.getBranches().catch(() => [] as Branch[]),
        adminService.getUsers().catch(() => [] as StaffUser[]),
      ]);

      setSummary(summaryData);
      setDevices(devicesData);
      setAlerts(alertsData);
      setIrregularities(irregularData);
      setFailedLogins(failedData);
      setSessions(sessionData);
      setBranches(branchData);
      setUsers(userData);
      setSelectedDeviceId((current) => current ?? devicesData[0]?.id ?? null);
      setDraft((current) => ({ ...current, minimumSupportedVersion: current.minimumSupportedVersion || summaryData.minimumSupportedVersion }));
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
    if (!query) return devices;
    return devices.filter((device) =>
      [device.id, device.name, device.status, device.lifecycleState, device.riskLevel, device.branchName, device.assignedStaffName, device.ipAddress, device.lastSeenUserName]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(query),
    );
  }, [devices, search]);

  const selectedDevice = filteredDevices.find((device) => device.id === selectedDeviceId) ?? filteredDevices[0] ?? null;
  const newDevices = devices.filter((device) => device.lifecycleState === 'NEW_OBSERVED').slice(0, 6);
  const suspiciousDevices = devices.filter((device) => device.lifecycleState === 'SUSPICIOUS' || device.riskLevel === 'HIGH').slice(0, 6);

  const resetDraft = () => setDraft({ ...defaultDraft, minimumSupportedVersion: summary?.minimumSupportedVersion || defaultDraft.minimumSupportedVersion, softwareVersion: summary?.minimumSupportedVersion || defaultDraft.softwareVersion });

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
    setSelectedDeviceId(device.id);
    setActiveTab('setup');
    setNotice(`Loaded ${device.name} into the terminal setup form.`);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const result = await securityService.registerDevice({
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
      setNotice(`Terminal ${result.id} saved successfully.`);
      resetDraft();
      await loadData();
      setSelectedDeviceId(result.id);
      setActiveTab('registry');
    } catch (submitError) {
      setError(getErrorMessage(submitError, 'Unable to save terminal setup.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeviceAction = async (deviceId: string, action: 'ALLOW' | 'MONITOR' | 'FLAG' | 'RESTRICT' | 'REVOKE') => {
    setBusyDeviceId(deviceId);
    setError(null);
    const defaultReasons: Record<string, string> = {
      ALLOW: 'Terminal reviewed and explicitly allowed.',
      MONITOR: 'Terminal moved to monitoring after review.',
      FLAG: 'Terminal flagged for more investigation.',
      RESTRICT: 'Terminal restricted pending security review.',
      REVOKE: 'Terminal access revoked by security operations.',
    };
    try {
      await securityService.executeDeviceAction(deviceId, action, { reason: defaultReasons[action] });
      setNotice(`${action} applied to ${deviceId}.`);
      await loadData();
      setSelectedDeviceId(deviceId);
    } catch (actionError) {
      setError(getErrorMessage(actionError, `Unable to ${action.toLowerCase()} terminal.`));
    } finally {
      setBusyDeviceId(null);
    }
  };

  const handleScanOutdated = async () => {
    setSubmitting(true);
    setError(null);
    try {
      const result = await securityService.scanOutdatedDevices();
      setNotice(`Software posture review completed. ${result.outdatedCount} terminals are outdated and ${result.flaggedCount} were escalated.`);
      await loadData();
    } catch (scanError) {
      setError(getErrorMessage(scanError, 'Unable to scan for outdated terminals.'));
    } finally {
      setSubmitting(false);
    }
  };

  const renderOverview = () => (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
        {[
          { label: 'Active Sessions', value: summary?.activeSessions ?? sessions.length, icon: <Activity className="h-5 w-5 text-emerald-600 dark:text-emerald-300" /> },
          { label: 'New Terminals', value: summary?.newlyObservedDevices ?? newDevices.length, icon: <Laptop className="h-5 w-5 text-blue-600 dark:text-blue-300" /> },
          { label: 'Suspicious', value: summary?.suspiciousDevices ?? suspiciousDevices.length, icon: <ShieldAlert className="h-5 w-5 text-amber-600 dark:text-amber-300" /> },
          { label: 'Failed Logins', value: summary?.failedLoginCount ?? failedLogins.length, icon: <Globe className="h-5 w-5 text-orange-600 dark:text-orange-300" /> },
          { label: 'Alerts', value: summary?.securityAlertCount ?? alerts.length, icon: <AlertTriangle className="h-5 w-5 text-rose-600 dark:text-rose-300" /> },
          { label: 'Irregular Activity', value: summary?.irregularActivityCount ?? irregularities.length, icon: <Shield className="h-5 w-5 text-violet-600 dark:text-violet-300" /> },
        ].map((card) => (
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

      <div className="grid gap-6 xl:grid-cols-2">
        <div className="screen-panel p-6">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Newly Observed Terminals</h3>
              <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Every first-time device is allowed automatically, logged, and queued here for review.</p>
            </div>
            {renderPill(`${newDevices.length} queued`, newDevices.length ? 'blue' : 'green')}
          </div>
          <div className="mt-5 space-y-3">
            {newDevices.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">No newly observed terminals need review.</div>
            ) : newDevices.map((device) => (
              <button key={device.id} type="button" onClick={() => { setSelectedDeviceId(device.id); setActiveTab('registry'); }} className="w-full rounded-2xl border border-slate-200 bg-slate-50/80 p-4 text-left transition hover:border-teal-300 hover:bg-white dark:border-slate-700 dark:bg-slate-800/55">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-semibold text-slate-900 dark:text-white">{device.name}</div>
                    <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{device.id} • {device.ipAddress || 'No IP captured'}</div>
                  </div>
                  {renderPill(device.lifecycleState.replace('_', ' '), lifecycleTone(device))}
                </div>
                <div className="mt-3 flex items-center justify-between gap-3">
                  <span className="text-xs text-slate-500 dark:text-slate-400">Seen {formatDateTime(device.lastSeenAt)}</span>
                  <span onClick={(event) => { event.stopPropagation(); handleDeviceAction(device.id, 'MONITOR'); }} className="rounded-xl bg-slate-950 px-3 py-1.5 text-xs font-semibold text-white dark:bg-white dark:text-slate-950">Move To Monitor</span>
                </div>
              </button>
            ))}
          </div>
        </div>

        <div className="screen-panel p-6">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Suspicious Queue</h3>
              <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Review outdated or high-risk devices before they are restricted or revoked.</p>
            </div>
            {renderPill(`${suspiciousDevices.length} queued`, suspiciousDevices.length ? 'amber' : 'green')}
          </div>
          <div className="mt-5 space-y-3">
            {suspiciousDevices.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">No suspicious terminals need review.</div>
            ) : suspiciousDevices.map((device) => (
              <button key={device.id} type="button" onClick={() => { setSelectedDeviceId(device.id); setActiveTab('registry'); }} className="w-full rounded-2xl border border-slate-200 bg-slate-50/80 p-4 text-left transition hover:border-amber-300 hover:bg-white dark:border-slate-700 dark:bg-slate-800/55">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-semibold text-slate-900 dark:text-white">{device.name}</div>
                    <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{device.id} • {device.softwareVersion} / {device.minimumSupportedVersion}</div>
                  </div>
                  {renderPill(device.riskLevel, riskTone(device.riskLevel))}
                </div>
                <div className="mt-3 flex items-center justify-between gap-3">
                  <span className="text-xs text-slate-500 dark:text-slate-400">{device.blockReason || 'Awaiting operator review'}</span>
                  <span onClick={(event) => { event.stopPropagation(); handleDeviceAction(device.id, 'RESTRICT'); }} className="rounded-xl bg-orange-600 px-3 py-1.5 text-xs font-semibold text-white">Restrict</span>
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );

  const renderRegistry = () => (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_420px]">
      <div className="screen-panel p-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Terminal Registry</h3>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Investigate lifecycle, branch ownership, access decision, and device history from one screen.</p>
          </div>
          <div className="flex flex-wrap gap-3">
            <div className="relative w-full max-w-sm">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search terminal, branch, staff, IP, or risk..." className="w-full rounded-2xl border border-slate-300/90 bg-white py-3 pl-10 pr-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" />
            </div>
            {canManageDevices && <button onClick={handleScanOutdated} disabled={submitting} className="inline-flex items-center gap-2 rounded-2xl bg-amber-400 px-4 py-3 text-sm font-semibold text-slate-950 transition hover:bg-amber-300 disabled:opacity-60"><ShieldAlert className="h-4 w-4" />Review Software Posture</button>}
          </div>
        </div>

        <div className="mt-5 space-y-3">
          {filteredDevices.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">No terminals matched the current search.</div>
          ) : filteredDevices.map((device) => (
            <button key={device.id} type="button" onClick={() => setSelectedDeviceId(device.id)} className={`w-full rounded-2xl border p-4 text-left transition ${selectedDevice?.id === device.id ? 'border-teal-300 bg-teal-50/70 dark:border-teal-500/40 dark:bg-teal-500/10' : 'border-slate-200 bg-slate-50/80 hover:border-slate-300 hover:bg-white dark:border-slate-700 dark:bg-slate-800/55'}`}>
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="font-semibold text-slate-900 dark:text-white">{device.name}</div>
                  <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{device.id} • {device.branchName || device.branchId || 'Unassigned branch'}</div>
                </div>
                <div className="flex gap-2">{renderPill(device.lifecycleState.replace('_', ' '), lifecycleTone(device))}{renderPill(device.riskLevel, riskTone(device.riskLevel))}</div>
              </div>
              <div className="mt-3 grid gap-2 text-xs text-slate-500 dark:text-slate-400 md:grid-cols-3">
                <div>Access: {device.accessDecision}</div>
                <div>Seen: {formatDateTime(device.lastSeenAt)}</div>
                <div>Observations: {device.observationCount}</div>
              </div>
            </button>
          ))}
        </div>
      </div>

      <div className="screen-panel p-6">
        {selectedDevice ? (
          <div className="space-y-5">
            <div className="flex items-start justify-between gap-4">
              <div>
                <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">{selectedDevice.name}</h3>
                <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{selectedDevice.id}</p>
              </div>
              <div className="flex flex-wrap gap-2">{renderPill(selectedDevice.lifecycleState.replace('_', ' '), lifecycleTone(selectedDevice))}{renderPill(selectedDevice.accessDecision, selectedDevice.accessDecision === 'ALLOWED' ? 'green' : selectedDevice.accessDecision === 'RESTRICTED' ? 'amber' : 'red')}</div>
            </div>
            <div className="grid gap-3 text-sm text-slate-600 dark:text-slate-300">
              <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
                <div className="text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Terminal Context</div>
                <div className="mt-3 space-y-2">
                  <div>Branch: {selectedDevice.branchName || selectedDevice.branchId || 'Not assigned'}</div>
                  <div>Operator: {selectedDevice.assignedStaffName || selectedDevice.assignedStaffId || selectedDevice.lastSeenUserName || 'Not linked'}</div>
                  <div>Source: {selectedDevice.detectionSource || (selectedDevice.autoObserved ? 'SESSION_LOGIN' : 'MANUAL_REGISTRATION')}</div>
                  <div>User agent: {selectedDevice.userAgent || 'Not captured'}</div>
                </div>
              </div>
              <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
                <div className="text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Timeline</div>
                <div className="mt-3 space-y-2">
                  <div>First observed: {formatDateTime(selectedDevice.firstObservedAt || selectedDevice.createdAt)}</div>
                  <div>Last seen: {formatDateTime(selectedDevice.lastSeenAt)}</div>
                  <div>Last action: {selectedDevice.lastAction || 'None'}{selectedDevice.lastActionAt ? ` • ${formatDateTime(selectedDevice.lastActionAt)}` : ''}</div>
                  <div>Software: {selectedDevice.softwareVersion} / minimum {selectedDevice.minimumSupportedVersion}</div>
                </div>
              </div>
            </div>
            <div className="flex flex-wrap gap-2">
              <button disabled={!canManageDevices || busyDeviceId === selectedDevice.id} onClick={() => handleDeviceAction(selectedDevice.id, 'ALLOW')} className="rounded-2xl bg-emerald-600 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-60">Allow</button>
              <button disabled={!canManageDevices || busyDeviceId === selectedDevice.id} onClick={() => handleDeviceAction(selectedDevice.id, 'MONITOR')} className="rounded-2xl bg-slate-900 px-3 py-2 text-xs font-semibold text-white hover:bg-slate-800 disabled:opacity-60 dark:bg-white dark:text-slate-950">Monitor</button>
              <button disabled={!canManageDevices || busyDeviceId === selectedDevice.id} onClick={() => handleDeviceAction(selectedDevice.id, 'FLAG')} className="rounded-2xl bg-amber-400 px-3 py-2 text-xs font-semibold text-slate-950 hover:bg-amber-300 disabled:opacity-60">Flag</button>
              <button disabled={!canManageDevices || busyDeviceId === selectedDevice.id} onClick={() => handleDeviceAction(selectedDevice.id, 'RESTRICT')} className="rounded-2xl bg-orange-600 px-3 py-2 text-xs font-semibold text-white hover:bg-orange-700 disabled:opacity-60">Restrict</button>
              <button disabled={!canManageDevices || busyDeviceId === selectedDevice.id} onClick={() => handleDeviceAction(selectedDevice.id, 'REVOKE')} className="rounded-2xl bg-rose-700 px-3 py-2 text-xs font-semibold text-white hover:bg-rose-800 disabled:opacity-60">Revoke</button>
              <button type="button" onClick={() => loadDeviceIntoForm(selectedDevice)} className="rounded-2xl border border-slate-300 bg-white px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900/70 dark:text-slate-200">Edit Setup</button>
            </div>
          </div>
        ) : <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">Select a terminal to inspect its lifecycle and actions.</div>}
      </div>
    </div>
  );

  const renderInvestigations = () => (
    <div className="grid gap-6 xl:grid-cols-3">
      <div className="screen-panel p-6">
        <div className="flex items-center justify-between gap-3">
          <div><h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Active Sessions</h3><p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Session visibility for device/user investigations.</p></div>
          {renderPill(`${sessions.length} live`, sessions.length ? 'green' : 'slate')}
        </div>
        <div className="mt-5 space-y-3">{sessions.slice(0, 8).map((session) => <div key={session.id} className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/55"><div className="font-semibold text-slate-900 dark:text-white">{session.staffName}</div><div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{session.email}</div><div className="mt-3 flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400"><Clock3 className="h-3.5 w-3.5" />{formatDateTime(session.lastActivity)}</div><div className="mt-2 text-xs text-slate-500 dark:text-slate-400">{session.ipAddress} • {session.userAgent || 'No user agent captured'}</div></div>)}{sessions.length === 0 && <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">No active sessions available for review.</div>}</div>
      </div>

      <div className="screen-panel p-6">
        <div className="flex items-center justify-between gap-3">
          <div><h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Failed Logins</h3><p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Recent authentication failures with IP and device signature hints.</p></div>
          {renderPill(`${failedLogins.length} recent`, failedLogins.length ? 'amber' : 'green')}
        </div>
        <div className="mt-5 space-y-3">{failedLogins.slice(0, 8).map((attempt) => <div key={attempt.id} className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/55"><div className="font-semibold text-slate-900 dark:text-white">{attempt.email}</div><div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{attempt.ipAddress}</div><p className="mt-3 text-sm text-slate-600 dark:text-slate-300">{attempt.failureReason || 'Authentication failed.'}</p><div className="mt-3 text-xs text-slate-500 dark:text-slate-400">{formatDateTime(attempt.attemptedAt)} • {attempt.userAgent || 'No device signature captured'}</div></div>)}{failedLogins.length === 0 && <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">No failed logins in the selected window.</div>}</div>
      </div>

      <div className="screen-panel p-6">
        <div className="flex items-center justify-between gap-3">
          <div><h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Alerts & Anomalies</h3><p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Security alerts and irregular activity in one investigation stream.</p></div>
          {renderPill(`${alerts.length + irregularities.length} total`, alerts.length + irregularities.length ? 'amber' : 'green')}
        </div>
        <div className="mt-5 space-y-3">
          {irregularities.slice(0, 4).map((item) => <div key={item.id} className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/55"><div className="font-semibold text-slate-900 dark:text-white">{item.summary}</div><div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{item.reference || item.transactionId}</div><div className="mt-3 text-xs text-slate-500 dark:text-slate-400">Risk {item.riskScore} • {formatDateTime(item.detectedAt)}</div></div>)}
          {alerts.slice(0, 4).map((alert) => <div key={alert.id} className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/55"><div className="font-semibold text-slate-900 dark:text-white">{alert.action.replace(/_/g, ' ')}</div><div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{alert.entityId || alert.entityType || 'Platform alert'}</div><div className="mt-3 text-xs text-slate-500 dark:text-slate-400">{formatDateTime(alert.createdAt)}</div></div>)}
          {alerts.length === 0 && irregularities.length === 0 && <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">No active alerts or irregular activity items.</div>}
        </div>
      </div>
    </div>
  );

  const renderSetup = () => (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
      <form onSubmit={handleSubmit} className="screen-panel p-6">
        <div className="flex items-center justify-between gap-3">
          <div><h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Terminal Setup</h3><p className="text-sm text-slate-500 dark:text-slate-400">Use manual setup to enrich a terminal after automatic observation.</p></div>
          {renderPill(`Min ${summary?.minimumSupportedVersion || draft.minimumSupportedVersion}`, 'blue')}
        </div>
        <div className="mt-5 grid gap-4 lg:grid-cols-2">
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Device ID<input value={draft.deviceId} onChange={(event) => setDraft((current) => ({ ...current, deviceId: event.target.value.toUpperCase() }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" placeholder="TERM-HO-01" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Display Name<input value={draft.name} onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" placeholder="Head Office Counter 1" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Branch<select value={draft.branchId} onChange={(event) => setDraft((current) => ({ ...current, branchId: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white"><option value="">Select branch</option>{branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Assigned Staff<select value={draft.assignedStaffId} onChange={(event) => setDraft((current) => ({ ...current, assignedStaffId: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white"><option value="">Select staff</option>{users.map((user) => <option key={user.id} value={user.id}>{user.name}</option>)}</select></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Software Version<input value={draft.softwareVersion} onChange={(event) => setDraft((current) => ({ ...current, softwareVersion: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Minimum Supported<input value={draft.minimumSupportedVersion} onChange={(event) => setDraft((current) => ({ ...current, minimumSupportedVersion: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">Serial Number<input value={draft.serialNumber} onChange={(event) => setDraft((current) => ({ ...current, serialNumber: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" /></label>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">IP Address<input value={draft.ipAddress} onChange={(event) => setDraft((current) => ({ ...current, ipAddress: event.target.value }))} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" /></label>
        </div>
        <label className="mt-4 block text-sm font-medium text-slate-700 dark:text-slate-300">Notes<textarea value={draft.notes} onChange={(event) => setDraft((current) => ({ ...current, notes: event.target.value }))} rows={4} className="mt-1 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 dark:border-slate-600 dark:bg-slate-950/80 dark:text-white" /></label>
        <div className="mt-5 flex flex-wrap gap-3">
          <button disabled={submitting || !canManageDevices} className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200">{submitting ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}Save Terminal</button>
          <button type="button" onClick={resetDraft} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900/70 dark:text-slate-200 dark:hover:bg-slate-800">Reset Form</button>
        </div>
      </form>

      <div className="screen-panel p-6">
        <h3 className="text-lg font-heading font-bold text-slate-900 dark:text-white">Production Readiness Notes</h3>
        <div className="mt-5 space-y-3 text-sm text-slate-600 dark:text-slate-300">
          <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">New devices are allowed by default, logged as new terminals, and surfaced for review instead of being blocked.</div>
          <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">Manual setup is for enrichment and terminal ownership, not for first-access approval.</div>
          <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">Operators should verify ownership, branch alignment, software posture, and whether a terminal should remain monitored or be restricted.</div>
        </div>
      </div>
    </div>
  );

  return (
    <div className="space-y-6 text-slate-900 dark:text-white">
      <section className="screen-hero p-6 sm:p-8">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-3xl">
            <div className="screen-badge inline-flex items-center gap-2"><Shield className="h-3.5 w-3.5" />Security operations</div>
            <h1 className="mt-4 text-3xl font-heading font-bold tracking-tight text-slate-950 dark:text-white sm:text-4xl">Security Operations Workbench</h1>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">Production-focused terminal governance with queue-based review, session visibility, and default-allow terminal observation.</p>
          </div>
          <div className="flex flex-wrap gap-3">
            {renderPill('default allow policy', 'blue')}
            <button onClick={loadData} disabled={loading} className="screen-button-secondary inline-flex items-center justify-center gap-2 disabled:cursor-not-allowed disabled:opacity-60"><RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />Refresh data</button>
          </div>
        </div>
      </section>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-200">{error}</div>}
      {notice && <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-200">{notice}</div>}

      <section className="screen-panel p-3">
        <div className="flex flex-wrap gap-2">
          {([
            { id: 'overview', label: 'Overview' },
            { id: 'registry', label: 'Terminal Registry' },
            { id: 'investigations', label: 'Investigations' },
            { id: 'setup', label: 'Terminal Setup' },
          ] as Array<{ id: SecurityTab; label: string }>).map((tab) => (
            <button key={tab.id} onClick={() => setActiveTab(tab.id)} className={`px-4 py-2.5 text-sm font-medium transition ${activeTab === tab.id ? 'screen-tab screen-tab-active' : 'screen-tab'}`}>{tab.label}</button>
          ))}
        </div>
      </section>

      {activeTab === 'overview' && renderOverview()}
      {activeTab === 'registry' && renderRegistry()}
      {activeTab === 'investigations' && renderInvestigations()}
      {activeTab === 'setup' && renderSetup()}
    </div>
  );
}
