import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  ArrowDownCircle,
  ArrowUpCircle,
  Briefcase,
  FileText,
  Landmark,
  RefreshCw,
  Scale,
  ShieldCheck,
  Wallet,
} from 'lucide-react';
import {
  CashDenominationLineDto,
  BranchVaultDto,
  CloseTillRequest,
  OpenTillRequest,
  TellerTillSummaryDto,
  TillCashTransferRequest,
  VaultCountRequest,
  VaultTransactionRequest,
  vaultService,
} from '../services/vaultService';
import {
  BranchCashSummaryDTO,
  CashControlAlertDTO,
  CashIncidentDTO,
  CashReconciliationSummaryDTO,
  CashTransitItemDTO,
  VaultCashPositionDTO,
  reportService,
} from '../services/reportService';
import { approvalService } from '../services/approvalService';
import type { ApprovalRequest } from '../../types';

const CURRENCY = 'GHS';

type ScreenTab = 'overview' | 'tills' | 'vault';

type MessageState = {
  type: 'success' | 'error';
  text: string;
} | null;

const GHANA_DENOMINATIONS = ['200', '100', '50', '20', '10', '5', '2', '1', '0.50', '0.20', '0.10', '0.05', '0.01'];

const createDenominationLines = (): CashDenominationLineDto[] =>
  GHANA_DENOMINATIONS.map((denomination) => ({
    denomination,
    pieces: 0,
    fitPieces: 0,
    unfitPieces: 0,
    suspectPieces: 0,
    totalValue: 0,
    suspectValue: 0,
  }));

const calculateDenominationTotal = (lines: CashDenominationLineDto[]) =>
  lines.reduce((sum, line) => {
    const denominationValue = Number(line.denomination);
    return sum + ((Number(line.fitPieces) || 0) + (Number(line.unfitPieces) || 0)) * denominationValue;
  }, 0);

const calculateSuspectTotal = (lines: CashDenominationLineDto[]) =>
  lines.reduce((sum, line) => sum + ((Number(line.suspectPieces) || 0) * Number(line.denomination)), 0);

const summarizeDenominations = (lines: CashDenominationLineDto[]) =>
  lines
    .filter((line) => (line.fitPieces || 0) > 0 || (line.unfitPieces || 0) > 0 || (line.suspectPieces || 0) > 0)
    .map((line) => {
      const buckets = [
        line.fitPieces ? `fit ${line.fitPieces}` : '',
        line.unfitPieces ? `unfit ${line.unfitPieces}` : '',
        line.suspectPieces ? `suspect ${line.suspectPieces}` : '',
      ].filter(Boolean).join(', ');
      return `${line.denomination} (${buckets})`;
    })
    .join(', ');

const money = (value: number) =>
  new Intl.NumberFormat('en-GH', {
    style: 'currency',
    currency: CURRENCY,
    minimumFractionDigits: 2,
  }).format(value || 0);

const dateTime = (value?: string) => {
  if (!value) {
    return 'Not available';
  }

  return new Date(value).toLocaleString('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const cardClass =
  'rounded-3xl border border-slate-200/80 bg-white/92 shadow-[0_24px_60px_rgba(15,23,42,0.08)] backdrop-blur dark:border-slate-800 dark:bg-slate-950/80';
const inputClass =
  'w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:focus:border-slate-400 dark:focus:ring-slate-800';
const labelClass =
  'mb-2 block text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400';
const actionButtonClass =
  'inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-100 dark:text-slate-950 dark:hover:bg-white';
const secondaryButtonClass =
  'inline-flex items-center justify-center rounded-2xl border border-slate-200 px-4 py-3 text-sm font-semibold text-slate-700 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-900';

function ScreenCard({
  title,
  value,
  tone,
  icon,
  meta,
}: {
  title: string;
  value: string;
  tone?: 'default' | 'warn' | 'good';
  icon: React.ReactNode;
  meta: string;
}) {
  const toneClass =
    tone === 'warn'
      ? 'text-amber-600 dark:text-amber-300'
      : tone === 'good'
        ? 'text-emerald-600 dark:text-emerald-300'
        : 'text-slate-900 dark:text-white';

  return (
    <div className={`${cardClass} p-5`}>
      <div className="mb-4 flex items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
            {title}
          </p>
          <p className={`mt-3 text-2xl font-semibold ${toneClass}`}>{value}</p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-3 text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
          {icon}
        </div>
      </div>
      <p className="text-sm text-slate-500 dark:text-slate-400">{meta}</p>
    </div>
  );
}

function SectionHeader({ title, detail }: { title: string; detail: string }) {
  return (
    <div className="mb-5 flex items-start justify-between gap-4">
      <div>
        <h3 className="text-lg font-semibold text-slate-900 dark:text-white">{title}</h3>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{detail}</p>
      </div>
    </div>
  );
}

export default function VaultManagementHub({ onOpenNotes }: { onOpenNotes?: () => void } = {}) {
  const [activeTab, setActiveTab] = useState<ScreenTab>('overview');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<MessageState>(null);
  const [vaults, setVaults] = useState<BranchVaultDto[]>([]);
  const [tills, setTills] = useState<TellerTillSummaryDto[]>([]);
  const [vaultPositions, setVaultPositions] = useState<VaultCashPositionDTO[]>([]);
  const [branchCashSummaries, setBranchCashSummaries] = useState<BranchCashSummaryDTO[]>([]);
  const [reconciliation, setReconciliation] = useState<CashReconciliationSummaryDTO | null>(null);
  const [cashIncidents, setCashIncidents] = useState<CashIncidentDTO[]>([]);
  const [cashTransitItems, setCashTransitItems] = useState<CashTransitItemDTO[]>([]);
  const [cashApprovals, setCashApprovals] = useState<ApprovalRequest[]>([]);
  const [incidentResolutionNote, setIncidentResolutionNote] = useState('Balanced and reviewed by branch operations.');

  const [openTillForm, setOpenTillForm] = useState<OpenTillRequest>({
    tellerId: '',
    branchId: '',
    currency: CURRENCY,
    openingBalance: 0,
    midDayCashLimit: 50000,
    notes: '',
    witnessOfficer: '',
  });
  const [allocateForm, setAllocateForm] = useState<TillCashTransferRequest>({
    tellerId: '',
    branchId: '',
    currency: CURRENCY,
    amount: 0,
    reference: '',
    narration: '',
    controlReference: '',
    witnessOfficer: '',
    sealNumber: '',
    denominations: createDenominationLines(),
  });
  const [returnForm, setReturnForm] = useState<TillCashTransferRequest>({
    tellerId: '',
    branchId: '',
    currency: CURRENCY,
    amount: 0,
    reference: '',
    narration: '',
    controlReference: '',
    witnessOfficer: '',
    sealNumber: '',
    denominations: createDenominationLines(),
  });
  const [closeForm, setCloseForm] = useState<CloseTillRequest>({
    tellerId: '',
    branchId: '',
    currency: CURRENCY,
    physicalCashCount: 0,
    notes: '',
    controlReference: '',
    witnessOfficer: '',
    sealNumber: '',
    denominations: createDenominationLines(),
  });
  const [vaultTxForm, setVaultTxForm] = useState<VaultTransactionRequest>({
    branchId: '',
    currency: CURRENCY,
    amount: 0,
    type: 'Deposit',
    reference: '',
    narration: '',
    controlReference: '',
    witnessOfficer: '',
    sealNumber: '',
    denominations: createDenominationLines(),
  });
  const [vaultCountForm, setVaultCountForm] = useState<VaultCountRequest>({
    branchId: '',
    currency: CURRENCY,
    amount: 0,
    controlReference: '',
    countReason: '',
    witnessOfficer: '',
    sealNumber: '',
    denominations: createDenominationLines(),
  });

  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [vaultData, tillData, vaultPositionData, branchSummaryData, reconciliationData, incidentData, transitItemData, approvalData] = await Promise.all([
        vaultService.getAllVaults(),
        vaultService.getTillSummaries(undefined, CURRENCY),
        reportService.getVaultCashPosition(undefined, CURRENCY),
        reportService.getBranchCashSummary(undefined, CURRENCY),
        reportService.getCashReconciliation(CURRENCY),
        reportService.getCashIncidents(undefined, 'OPEN'),
        reportService.getCashTransitItems(CURRENCY),
        approvalService.getApprovals(),
      ]);
      setVaults(Array.isArray(vaultData) ? vaultData : []);
      setTills(Array.isArray(tillData) ? tillData : []);
      setVaultPositions(Array.isArray(vaultPositionData) ? vaultPositionData : []);
      setBranchCashSummaries(Array.isArray(branchSummaryData) ? branchSummaryData : []);
      setReconciliation(reconciliationData ?? null);
      setCashIncidents(Array.isArray(incidentData) ? incidentData : []);
      setCashTransitItems(Array.isArray(transitItemData) ? transitItemData : []);
      setCashApprovals(
        (Array.isArray(approvalData) ? approvalData : []).filter((approval) => {
          const entityType = approval.payload.entityType.toUpperCase();
          return entityType.includes('CASH') || entityType.includes('TILL');
        }),
      );
    } catch (err) {
      const messageText = err instanceof Error ? err.message : 'Failed to load cash operations.';
      setError(messageText);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const updateDenominationLines = (
    lines: CashDenominationLineDto[] | undefined,
    denomination: string,
    bucket: 'fitPieces' | 'unfitPieces' | 'suspectPieces',
    pieces: number,
  ) =>
    (lines ?? []).map((line) => {
      if (line.denomination !== denomination) {
        return line;
      }

      const next = {
        ...line,
        [bucket]: pieces,
      };
      const fitPieces = Number(next.fitPieces) || 0;
      const unfitPieces = Number(next.unfitPieces) || 0;
      const suspectPieces = Number(next.suspectPieces) || 0;
      const denominationValue = Number(next.denomination);
      return {
        ...next,
        pieces: fitPieces + unfitPieces,
        totalValue: Number((denominationValue * (fitPieces + unfitPieces)).toFixed(2)),
        suspectValue: Number((denominationValue * suspectPieces).toFixed(2)),
      };
    });

  const syncTillTransferAmount = (payload: TillCashTransferRequest) => ({
    ...payload,
    amount: Number(calculateDenominationTotal(payload.denominations ?? []).toFixed(2)),
  });

  const syncCloseTillAmount = (payload: CloseTillRequest) => ({
    ...payload,
    physicalCashCount: Number(calculateDenominationTotal(payload.denominations ?? []).toFixed(2)),
  });

  const syncVaultCountAmount = (payload: VaultCountRequest) => ({
    ...payload,
    amount: Number(calculateDenominationTotal(payload.denominations ?? []).toFixed(2)),
  });

  const syncVaultMovementAmount = (payload: VaultTransactionRequest) => ({
    ...payload,
    amount: Number(calculateDenominationTotal(payload.denominations ?? []).toFixed(2)),
  });

  const formatCountSummary = (lines: CashDenominationLineDto[]) => {
    const accepted = calculateDenominationTotal(lines);
    const suspect = calculateSuspectTotal(lines);
    return {
      accepted,
      suspect,
      summary: summarizeDenominations(lines),
    };
  };

  const branchOptions = useMemo(() => {
    const branchMap = new Map<string, { id: string; name: string; code: string }>();

    vaults.forEach((vault) => {
      branchMap.set(vault.branchId, {
        id: vault.branchId,
        name: vault.branchName,
        code: vault.branchCode,
      });
    });

    tills.forEach((till) => {
      if (till.branchId && !branchMap.has(till.branchId)) {
        branchMap.set(till.branchId, {
          id: till.branchId,
          name: till.branchName,
          code: till.branchCode,
        });
      }
    });

    return Array.from(branchMap.values()).sort((left, right) => left.name.localeCompare(right.name));
  }, [tills, vaults]);

  const tellerOptions = useMemo(
    () =>
      tills.map((till) => ({
        tellerId: till.tellerId,
        tellerName: till.tellerName,
        branchId: till.branchId || '',
        branchName: till.branchName,
        isOpen: till.isOpen,
        status: till.status,
      })),
    [tills],
  );
  const selectedAllocationTill = useMemo(
    () => tills.find((till) => till.tellerId === allocateForm.tellerId) ?? null,
    [allocateForm.tellerId, tills],
  );
  const allocationProjectedBalance = useMemo(
    () => (selectedAllocationTill?.currentBalance ?? 0) + (allocateForm.amount || 0),
    [allocateForm.amount, selectedAllocationTill],
  );
  const allocationRequiresApproval = useMemo(
    () =>
      Boolean(
        selectedAllocationTill &&
        selectedAllocationTill.midDayCashLimit > 0 &&
        allocationProjectedBalance > selectedAllocationTill.midDayCashLimit,
      ),
    [allocationProjectedBalance, selectedAllocationTill],
  );

  const totalVaultCash = useMemo(
    () => reconciliation?.totalVaultCash ?? vaults.reduce((sum, vault) => sum + vault.cashOnHand, 0),
    [reconciliation, vaults],
  );
  const totalTillFloat = useMemo(
    () => reconciliation?.totalTillCash ?? tills.filter((till) => till.isOpen).reduce((sum, till) => sum + till.currentBalance, 0),
    [reconciliation, tills],
  );
  const openTillCount = useMemo(() => tills.filter((till) => till.isOpen).length, [tills]);
  const pendingCashApprovals = useMemo(
    () => cashApprovals.filter((approval) => approval.status === 'PENDING'),
    [cashApprovals],
  );
  const exceptionCount = useMemo(
    () =>
      (reconciliation?.alerts.length ?? tills.filter((till) => till.variance !== 0 || till.status !== 'OPEN').length) +
      cashIncidents.length +
      cashTransitItems.filter((item) => item.isStale).length +
      pendingCashApprovals.length,
    [cashIncidents.length, cashTransitItems, pendingCashApprovals.length, reconciliation, tills],
  );
  const vaultAlerts = useMemo(
    () =>
      vaults.filter(
        (vault) =>
          (vault.minBalance && vault.cashOnHand < vault.minBalance) ||
          (vault.vaultLimit && vault.cashOnHand > vault.vaultLimit),
      ),
    [vaults],
  );
  const activeTills = useMemo(() => tills.filter((till) => till.isOpen), [tills]);
  const highPriorityTransitItems = useMemo(
    () => cashTransitItems.filter((item) => item.isStale || item.transitStage !== 'RECEIVED').slice(0, 6),
    [cashTransitItems],
  );
  const openIncidentItems = useMemo(
    () => cashIncidents.filter((incident) => incident.status === 'OPEN').slice(0, 6),
    [cashIncidents],
  );
  const cashControlAlerts = useMemo<CashControlAlertDTO[]>(() => {
    if (reconciliation?.alerts?.length) {
      return reconciliation.alerts;
    }

    return vaultAlerts.map((vault) => ({
      alertType: 'VAULT_POLICY_ALERT',
      severity: 'MEDIUM',
      scopeType: 'BRANCH',
      scopeId: vault.branchId,
      scopeName: vault.branchName,
      message: 'Vault cash is outside the configured branch policy range.',
      observedAt: new Date().toISOString(),
    }));
  }, [reconciliation, vaultAlerts]);

  const announce = (type: 'success' | 'error', text: string) => {
    setMessage({ type, text });
    if (type === 'error') {
      setError(text);
    } else {
      setError(null);
    }
  };

  const runAction = async (
    action: () => Promise<unknown>,
    successText: string,
    onComplete?: () => void,
  ) => {
    setSubmitting(true);
    try {
      await action();
      announce('success', successText);
      if (onComplete) {
        onComplete();
      }
      await loadData();
    } catch (err) {
      const messageText = err instanceof Error ? err.message : 'Action failed.';
      announce('error', messageText);
    } finally {
      setSubmitting(false);
    }
  };

  const submitTillAllocationApproval = async () => {
    const entityId = `TILL-MOVE-${Date.now()}`;
    await approvalService.createApproval({
      entityType: 'TILL_CASH_MOVEMENT_APPROVAL',
      entityId,
      remarks: `Till allocation above limit for teller ${allocateForm.tellerId}`,
      referenceNo: allocateForm.reference || entityId,
      payloadJson: JSON.stringify({
        direction: 'ALLOCATE',
        request: allocateForm,
        requestedBy: allocateForm.tellerId,
      }),
    });
  };

  const submitIncidentResolutionApproval = async (incident: CashIncidentDTO) => {
    await approvalService.createApproval({
      entityType: 'CASH_INCIDENT_RESOLUTION',
      entityId: incident.id,
      remarks: `Cash incident resolution requires checker approval for ${incident.id}`,
      referenceNo: incident.reference || incident.id,
      payloadJson: JSON.stringify({
        incidentId: incident.id,
        resolutionNote: incidentResolutionNote,
        requestedBy: incident.reportedBy || incident.branchId,
      }),
    });
  };

  const handleApproveCashApproval = async (approval: ApprovalRequest) => {
    await runAction(
      () => approvalService.approveApproval(approval.id, approval.payload.currentStep ?? 0),
      `${approval.referenceNo || approval.id} approved and executed.`,
    );
  };

  const handleRejectCashApproval = async (approval: ApprovalRequest) => {
    await runAction(
      () => approvalService.rejectApproval(approval.id),
      `${approval.referenceNo || approval.id} rejected.`,
    );
  };

  const renderBranchOptions = () =>
    branchOptions.map((branch) => (
      <option key={branch.id} value={branch.id}>
        {branch.name} ({branch.code})
      </option>
    ));

  const renderTellerOptions = (mode: 'all' | 'open' | 'closed') =>
    tellerOptions
      .filter((teller) => mode === 'all' || (mode === 'open' ? teller.isOpen : !teller.isOpen))
      .map((teller) => (
        <option key={teller.tellerId} value={teller.tellerId}>
          {teller.tellerName} - {teller.branchName}
          {teller.isOpen ? ' (open)' : ''}
        </option>
      ));

  return (
    <div className="space-y-6 p-6">
      <div className={`${cardClass} overflow-hidden`}>
        <div className="border-b border-slate-200/80 bg-[radial-gradient(circle_at_top_left,_rgba(148,163,184,0.12),_transparent_42%),linear-gradient(135deg,rgba(255,255,255,0.92),rgba(248,250,252,0.92))] px-6 py-6 dark:border-slate-800 dark:bg-[radial-gradient(circle_at_top_left,_rgba(148,163,184,0.08),_transparent_42%),linear-gradient(135deg,rgba(2,6,23,0.86),rgba(15,23,42,0.9))]">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500 dark:text-slate-400">
                Cash Operations Control
              </p>
              <h2 className="mt-2 text-3xl font-semibold text-slate-900 dark:text-white">
                Teller, till and vault management
              </h2>
              <p className="mt-3 max-w-3xl text-sm text-slate-600 dark:text-slate-300">
                Coordinate branch vault posture, open and reconcile teller tills, and keep cash
                movements traceable across the branch.
              </p>
            </div>
            <div className="flex flex-wrap gap-3">
              <button
                type="button"
                onClick={onOpenNotes}
                className={secondaryButtonClass}
              >
                <FileText className="mr-2 h-4 w-4" />
                Cash notes
              </button>
              <button
                type="button"
                onClick={() => void loadData()}
                disabled={loading || submitting}
                className={secondaryButtonClass}
              >
                <RefreshCw className={`mr-2 h-4 w-4 ${(loading || submitting) ? 'animate-spin' : ''}`} />
                Refresh data
              </button>
            </div>
          </div>
        </div>
        <div className="flex flex-wrap gap-3 px-6 py-4">
          {([
            ['overview', 'Overview'],
            ['tills', 'Till management'],
            ['vault', 'Vault controls'],
          ] as Array<[ScreenTab, string]>).map(([tab, label]) => (
            <button
              key={tab}
              type="button"
              onClick={() => setActiveTab(tab)}
              className={`rounded-2xl px-4 py-2 text-sm font-semibold transition ${
                activeTab === tab
                  ? 'bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-950'
                  : 'border border-slate-200 text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-900'
              }`}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="rounded-3xl border border-amber-200 bg-amber-50/90 p-5 text-sm text-amber-900 shadow-sm dark:border-amber-900/60 dark:bg-amber-950/30 dark:text-amber-100">
        <div className="flex items-start gap-3">
          <AlertTriangle className="mt-0.5 h-5 w-5 flex-shrink-0" />
          <div className="space-y-2">
            <p className="font-semibold">Cash-count control guidance</p>
            <p>
              This workbench now separates fit notes, unfit notes, and suspect notes. Accepted cash is counted as fit plus unfit notes; suspect notes are logged separately and escalated for review instead of being posted as available cash.
            </p>
            <p>
              Operators should record a control reference, witness officer, and seal or bag reference for vault counts and large till movements. That aligns with Bank of Ghana-style expectations around transaction records, suspicious cash escalation, and segregation of questionable notes.
            </p>
          </div>
        </div>
      </div>

      {(error || message) && (
        <div
          className={`rounded-2xl border px-5 py-4 text-sm ${
            message?.type === 'success'
              ? 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300'
              : 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900 dark:bg-rose-950/40 dark:text-rose-300'
          }`}
        >
          {message?.text || error}
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-4">
        <ScreenCard title="Vault Cash" value={money(totalVaultCash)} icon={<Landmark className="h-5 w-5" />} meta="Total physical cash recorded across branch vaults." />
        <ScreenCard title="Till Float" value={money(totalTillFloat)} icon={<Wallet className="h-5 w-5" />} meta="Combined live till positions for all open tellers." />
        <ScreenCard title="Open Tills" value={`${openTillCount}`} tone={openTillCount > 0 ? 'good' : 'default'} icon={<Briefcase className="h-5 w-5" />} meta="Tellers currently carrying active till cash." />
        <ScreenCard title="Exceptions" value={`${exceptionCount}`} tone={exceptionCount > 0 ? 'warn' : 'good'} icon={<AlertTriangle className="h-5 w-5" />} meta="GL variances, stale transit items, and unresolved cash exceptions." />
      </div>

      <div className={`${cardClass} grid gap-4 p-5 lg:grid-cols-4`}>
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Branches tracked</p>
          <p className="mt-2 text-2xl font-semibold text-slate-900 dark:text-white">{branchCashSummaries.length}</p>
        </div>
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Out of balance</p>
          <p className="mt-2 text-2xl font-semibold text-amber-600 dark:text-amber-300">{reconciliation?.branchesOutOfBalance ?? 0}</p>
        </div>
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Stale transit items</p>
          <p className="mt-2 text-2xl font-semibold text-rose-600 dark:text-rose-300">{reconciliation?.staleCashInTransitItems ?? 0}</p>
        </div>
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Net GL variance</p>
          <p className={`mt-2 text-2xl font-semibold ${Math.abs(reconciliation?.totalVariance ?? 0) > 0.01 ? 'text-rose-600 dark:text-rose-300' : 'text-emerald-600 dark:text-emerald-300'}`}>
            {money(reconciliation?.totalVariance ?? 0)}
          </p>
        </div>
      </div>

      {loading ? (
        <div className={`${cardClass} flex min-h-[280px] items-center justify-center p-8`}>
          <div className="text-center text-sm text-slate-500 dark:text-slate-400">
            <RefreshCw className="mx-auto mb-3 h-8 w-8 animate-spin" />
            Loading cash operations...
          </div>
        </div>
      ) : (
        <>
          {activeTab === 'overview' && (
            <div className="grid gap-6 xl:grid-cols-[1.35fr_1fr]">
              <div className={`${cardClass} p-6`}>
                <SectionHeader
                  title="Vault posture"
                  detail="Branch vault limits, cash on hand, and latest count activity."
                />
                <div className="overflow-x-auto">
                  <table className="min-w-full text-left text-sm">
                    <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.18em] text-slate-500 dark:border-slate-800 dark:text-slate-400">
                      <tr>
                        <th className="pb-3 font-semibold">Branch</th>
                        <th className="pb-3 font-semibold text-right">Vault cash</th>
                        <th className="pb-3 font-semibold text-right">Till cash</th>
                        <th className="pb-3 font-semibold text-right">GL variance</th>
                        <th className="pb-3 font-semibold">Condition</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 dark:divide-slate-900">
                      {(vaultPositions.length > 0 ? vaultPositions : vaults.map((vault) => ({
                        branchId: vault.branchId,
                        branchCode: vault.branchCode,
                        branchName: vault.branchName,
                        currency: vault.currency,
                        vaultCash: vault.cashOnHand,
                        minBalance: vault.minBalance ?? 0,
                        maxHoldingLimit: vault.vaultLimit ?? 0,
                        tillCash: 0,
                        totalOperationalCash: vault.cashOnHand,
                        glCashBalance: 0,
                        glVariance: 0,
                        lastCountDate: vault.lastCountDate,
                        status: 'WITHIN_POLICY',
                      }))).map((vault) => {
                        const belowMin = vault.minBalance > 0 && vault.vaultCash < vault.minBalance;
                        const aboveLimit = vault.maxHoldingLimit > 0 && vault.vaultCash > vault.maxHoldingLimit;
                        const hasVariance = Math.abs(vault.glVariance) > 0.01;
                        const label = belowMin
                          ? 'Below minimum'
                          : aboveLimit
                            ? 'Over vault limit'
                            : hasVariance
                              ? 'GL variance'
                              : 'Within policy';
                        const tone =
                          belowMin || aboveLimit || hasVariance
                            ? 'text-amber-600 dark:text-amber-300'
                            : 'text-emerald-600 dark:text-emerald-300';

                        return (
                          <tr key={`${vault.branchId}-${vault.currency}`}>
                            <td className="py-4">
                              <div className="font-semibold text-slate-900 dark:text-white">
                                {vault.branchName}
                              </div>
                              <div className="text-xs text-slate-500 dark:text-slate-400">
                                {vault.branchCode} · {vault.currency}
                              </div>
                            </td>
                            <td className="py-4 text-right font-medium text-slate-900 dark:text-white">
                              {money(vault.vaultCash)}
                            </td>
                            <td className="py-4 text-right text-slate-500 dark:text-slate-400">
                              {money(vault.tillCash)}
                            </td>
                            <td className={`py-4 text-right font-medium ${hasVariance ? 'text-rose-600 dark:text-rose-300' : 'text-emerald-600 dark:text-emerald-300'}`}>
                              {money(vault.glVariance)}
                            </td>
                            <td className="py-4">
                              <div className={`font-medium ${tone}`}>{label}</div>
                              <div className="text-xs text-slate-500 dark:text-slate-400">
                                Counted {dateTime(vault.lastCountDate ?? undefined)}
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>

              <div className="space-y-6">
                <div className={`${cardClass} p-6`}>
                  <SectionHeader
                    title="Pending cash approvals"
                    detail="Checker queue for over-limit till movements and cash exception resolutions."
                  />
                  <div className="space-y-4">
                    {pendingCashApprovals.length === 0 && (
                      <p className="text-sm text-slate-500 dark:text-slate-400">
                        No pending cash approvals at the moment.
                      </p>
                    )}
                    {pendingCashApprovals.slice(0, 6).map((approval) => (
                      <div
                        key={approval.id}
                        className="rounded-2xl border border-slate-200 p-4 dark:border-slate-800"
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div>
                            <p className="font-semibold text-slate-900 dark:text-white">
                              {approval.referenceNo || approval.id}
                            </p>
                            <p className="text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                              {approval.payload.entityType.replaceAll('_', ' ')}
                            </p>
                          </div>
                          <span className="rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-amber-700 dark:bg-amber-950/50 dark:text-amber-300">
                            Pending
                          </span>
                        </div>
                        <p className="mt-3 text-sm text-slate-600 dark:text-slate-300">
                          {approval.description}
                        </p>
                        {approval.remarks && (
                          <p className="mt-2 text-xs text-slate-500 dark:text-slate-400">
                            Remarks: {approval.remarks}
                          </p>
                        )}
                        <p className="mt-2 text-xs text-slate-500 dark:text-slate-400">
                          Submitted {dateTime(approval.requestDate)}
                        </p>
                        <div className="mt-4 flex flex-wrap gap-3">
                          <button
                            type="button"
                            onClick={() => void handleApproveCashApproval(approval)}
                            disabled={submitting}
                            className={actionButtonClass}
                          >
                            Approve
                          </button>
                          <button
                            type="button"
                            onClick={() => void handleRejectCashApproval(approval)}
                            disabled={submitting}
                            className={secondaryButtonClass}
                          >
                            Reject
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className={`${cardClass} p-6`}>
                  <SectionHeader
                    title="Active tills"
                    detail="Open tills and their current expected balances."
                  />
                  <div className="space-y-4">
                    {activeTills.length === 0 && (
                      <p className="text-sm text-slate-500 dark:text-slate-400">
                        No teller till is currently open.
                      </p>
                    )}
                    {activeTills.map((till) => (
                      <div
                        key={till.tellerId}
                        className="rounded-2xl border border-slate-200 p-4 dark:border-slate-800"
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div>
                            <p className="font-semibold text-slate-900 dark:text-white">
                              {till.tellerName}
                            </p>
                            <p className="text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
                              {till.branchName}
                            </p>
                          </div>
                          <span className="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300">
                            {till.status}
                          </span>
                        </div>
                        <div className="mt-4 grid gap-3 sm:grid-cols-2">
                          <div>
                            <p className="text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                              Expected balance
                            </p>
                            <p className="mt-1 font-semibold text-slate-900 dark:text-white">
                              {money(till.currentBalance)}
                            </p>
                          </div>
                          <div>
                            <p className="text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                              Opened
                            </p>
                            <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
                              {dateTime(till.openedAt)}
                            </p>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className={`${cardClass} p-6`}>
                  <SectionHeader
                    title="Operational watchlist"
                    detail="Alerts, unresolved incidents, and stale cash in transit requiring branch follow-up."
                  />
                  <div className="space-y-4 text-sm">
                    {cashControlAlerts.length === 0 ? (
                      <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300">
                        Cash positions are within current branch policy thresholds.
                      </div>
                    ) : (
                      <>
                        {cashControlAlerts.slice(0, 6).map((alert) => (
                          <div
                            key={`${alert.alertType}-${alert.scopeId}-${alert.observedAt}`}
                            className={`rounded-2xl border px-4 py-3 ${
                              alert.severity === 'HIGH'
                                ? 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900 dark:bg-rose-950/40 dark:text-rose-300'
                                : 'border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-300'
                            }`}
                          >
                            <div className="font-semibold">{alert.scopeName}</div>
                            <div className="mt-1">{alert.message}</div>
                          </div>
                        ))}
                      </>
                    )}

                    <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-800">
                      <div className="mb-3 flex items-center justify-between gap-3">
                        <div className="font-semibold text-slate-900 dark:text-white">Open cash incidents</div>
                        <span className="rounded-full bg-rose-50 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-rose-700 dark:bg-rose-950/40 dark:text-rose-300">
                          {cashIncidents.length}
                        </span>
                      </div>
                      <textarea
                        value={incidentResolutionNote}
                        onChange={(event) => setIncidentResolutionNote(event.target.value)}
                        className={`${inputClass} mb-3 min-h-[72px]`}
                        placeholder="Resolution note used when resolving incidents from the queue"
                      />
                      <div className="space-y-3">
                        {openIncidentItems.length === 0 ? (
                          <div className="text-slate-500 dark:text-slate-400">No unresolved incident is currently open.</div>
                        ) : (
                          openIncidentItems.map((incident) => (
                            <div key={incident.id} className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-rose-800 dark:border-rose-900 dark:bg-rose-950/30 dark:text-rose-200">
                              <div className="flex items-start justify-between gap-3">
                                <div>
                                  <div className="font-semibold">{incident.incidentType} · {incident.branchName}</div>
                                  <div className="mt-1 text-xs uppercase tracking-[0.16em]">{incident.storeType} · {incident.storeId}</div>
                                  <div className="mt-2 text-sm">{money(incident.amount)} · {incident.reference || 'No reference captured'}</div>
                                </div>
                                <button
                                  type="button"
                                  disabled={submitting}
                                  onClick={() =>
                                    void runAction(
                                      () => submitIncidentResolutionApproval(incident),
                                      `Cash incident ${incident.id} submitted for approval.`,
                                    )
                                  }
                                  className={secondaryButtonClass}
                                >
                                  Submit Approval
                                </button>
                              </div>
                            </div>
                          ))
                        )}
                      </div>
                    </div>

                    <div className="rounded-2xl border border-slate-200 p-4 dark:border-slate-800">
                      <div className="mb-3 flex items-center justify-between gap-3">
                        <div className="font-semibold text-slate-900 dark:text-white">Cash in transit</div>
                        <span className="rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-amber-700 dark:bg-amber-950/40 dark:text-amber-300">
                          {cashTransitItems.length}
                        </span>
                      </div>
                      <div className="space-y-3">
                        {highPriorityTransitItems.length === 0 ? (
                          <div className="text-slate-500 dark:text-slate-400">No open cash transit item requires attention.</div>
                        ) : (
                          highPriorityTransitItems.map((item) => (
                            <div key={item.transferId} className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-amber-800 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-200">
                              <div className="font-semibold">{item.fromBranchName} to {item.toBranchName}</div>
                              <div className="mt-1 text-sm">{money(item.amount)} · {item.transitStage}</div>
                              <div className="mt-1 text-xs uppercase tracking-[0.16em]">
                                Open {item.hoursOpen.toFixed(1)} hours · {item.reference || 'No reference'}
                              </div>
                            </div>
                          ))
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'tills' && (
            <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
              <div className={`${cardClass} p-6`}>
                <SectionHeader
                  title="Till directory"
                  detail="Open, allocated, returned, and reconciled till positions across tellers."
                />
                <div className="overflow-x-auto">
                  <table className="min-w-full text-left text-sm">
                    <thead className="border-b border-slate-200 text-xs uppercase tracking-[0.18em] text-slate-500 dark:border-slate-800 dark:text-slate-400">
                      <tr>
                        <th className="pb-3 font-semibold">Teller</th>
                        <th className="pb-3 font-semibold">Status</th>
                        <th className="pb-3 font-semibold text-right">Balance</th>
                        <th className="pb-3 font-semibold text-right">Variance</th>
                        <th className="pb-3 font-semibold">Last action</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 dark:divide-slate-900">
                      {tills.map((till) => (
                        <tr key={`${till.tellerId}-${till.branchCode}`}>
                          <td className="py-4">
                            <div className="font-semibold text-slate-900 dark:text-white">
                              {till.tellerName}
                            </div>
                            <div className="text-xs text-slate-500 dark:text-slate-400">
                              {till.branchName}
                            </div>
                          </td>
                          <td className="py-4">
                            <span
                              className={`rounded-full px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] ${
                                till.isOpen
                                  ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300'
                                  : 'bg-slate-100 text-slate-600 dark:bg-slate-900 dark:text-slate-300'
                              }`}
                            >
                              {till.status}
                            </span>
                          </td>
                          <td className="py-4 text-right font-medium text-slate-900 dark:text-white">
                            {money(till.currentBalance)}
                          </td>
                          <td
                            className={`py-4 text-right font-medium ${
                              till.variance === 0
                                ? 'text-emerald-600 dark:text-emerald-300'
                                : 'text-rose-600 dark:text-rose-300'
                            }`}
                          >
                            {money(till.variance)}
                          </td>
                          <td className="py-4 text-slate-500 dark:text-slate-400">
                            {till.lastAction
                              ? `${till.lastAction} · ${dateTime(till.lastActionAt)}`
                              : 'No activity'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              <div className="space-y-6">
                <div className={`${cardClass} p-6`}>
                  <SectionHeader
                    title="Open till"
                    detail="Start a teller till and optionally seed the opening float from the branch vault."
                  />
                  <div className="space-y-4">
                    <div>
                      <label className={labelClass}>Teller</label>
                      <select
                        value={openTillForm.tellerId}
                        onChange={(event) =>
                          setOpenTillForm((current) => ({
                            ...current,
                            tellerId: event.target.value,
                          }))
                        }
                        className={inputClass}
                      >
                        <option value="">Select teller</option>
                        {renderTellerOptions('closed')}
                      </select>
                    </div>
                    <div>
                      <label className={labelClass}>Branch</label>
                      <select
                        value={openTillForm.branchId}
                        onChange={(event) =>
                          setOpenTillForm((current) => ({
                            ...current,
                            branchId: event.target.value,
                          }))
                        }
                        className={inputClass}
                      >
                        <option value="">Use teller branch</option>
                        {renderBranchOptions()}
                      </select>
                    </div>
                    <div className="grid gap-4 md:grid-cols-2">
                      <div>
                        <label className={labelClass}>Opening balance</label>
                        <input
                          type="number"
                          min="0"
                          step="0.01"
                          value={openTillForm.openingBalance}
                          onChange={(event) =>
                            setOpenTillForm((current) => ({
                              ...current,
                              openingBalance: Number(event.target.value),
                            }))
                          }
                          className={inputClass}
                        />
                      </div>
                      <div>
                        <label className={labelClass}>Mid-day limit</label>
                        <input
                          type="number"
                          min="0"
                          step="0.01"
                          value={openTillForm.midDayCashLimit ?? 0}
                          onChange={(event) =>
                            setOpenTillForm((current) => ({
                              ...current,
                              midDayCashLimit: Number(event.target.value),
                            }))
                          }
                          className={inputClass}
                        />
                      </div>
                    </div>
                    <input
                      type="text"
                      value={openTillForm.witnessOfficer ?? ''}
                      onChange={(event) =>
                        setOpenTillForm((current) => ({
                          ...current,
                          witnessOfficer: event.target.value,
                        }))
                      }
                      className={inputClass}
                      placeholder="Witness officer"
                    />
                    <button
                      type="button"
                      disabled={submitting || !openTillForm.tellerId}
                      onClick={() =>
                        void runAction(
                          () => vaultService.openTill(openTillForm),
                          'Till opened successfully.',
                        )
                      }
                      className={actionButtonClass}
                    >
                      Open till
                    </button>
                  </div>
                </div>

                <div className={`${cardClass} p-6`}>
                  <SectionHeader
                    title="Move cash to or from till"
                    detail="Allocate vault cash to a teller or return till cash back to the branch vault."
                  />
                  <div className="grid gap-6 md:grid-cols-2">
                    <div className="space-y-4 rounded-2xl border border-slate-200 p-4 dark:border-slate-800">
                      <div className="flex items-center gap-2 font-semibold text-slate-900 dark:text-white">
                        <ArrowDownCircle className="h-4 w-4" />
                        Allocate to till
                      </div>
                      <select
                        value={allocateForm.tellerId}
                        onChange={(event) =>
                          setAllocateForm((current) => ({
                            ...current,
                            tellerId: event.target.value,
                          }))
                        }
                        className={inputClass}
                      >
                        <option value="">Select open teller</option>
                        {renderTellerOptions('open')}
                      </select>
                      <select
                        value={allocateForm.branchId}
                        onChange={(event) =>
                          setAllocateForm((current) => ({
                            ...current,
                            branchId: event.target.value,
                          }))
                        }
                        className={inputClass}
                      >
                        <option value="">Use teller branch</option>
                        {renderBranchOptions()}
                      </select>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={allocateForm.amount}
                        onChange={(event) =>
                          setAllocateForm((current) => ({
                            ...current,
                            amount: Number(event.target.value),
                          }))
                        }
                        className={inputClass}
                        placeholder="Amount"
                        readOnly
                      />
                    <DenominationEditor
                      lines={allocateForm.denominations ?? []}
                      onChange={(denomination, bucket, pieces) =>
                        setAllocateForm((current) =>
                          syncTillTransferAmount({
                            ...current,
                            denominations: updateDenominationLines(current.denominations, denomination, bucket, pieces),
                          }),
                        )
                      }
                    />
                    <div className="rounded-xl bg-slate-50 px-3 py-2 text-xs text-slate-500 dark:bg-slate-900/60 dark:text-slate-400">
                      Count summary: {formatCountSummary(allocateForm.denominations ?? []).summary || 'No notes entered'}. Accepted cash {money(formatCountSummary(allocateForm.denominations ?? []).accepted)}. Suspect notes {money(formatCountSummary(allocateForm.denominations ?? []).suspect)}.
                    </div>
                    <ControlFields
                      controlReference={allocateForm.controlReference}
                      witnessOfficer={allocateForm.witnessOfficer}
                      sealNumber={allocateForm.sealNumber}
                      onChange={(field, value) =>
                        setAllocateForm((current) => ({
                          ...current,
                          [field]: value,
                        }))
                      }
                    />
                      {allocationRequiresApproval && selectedAllocationTill && (
                        <div className="rounded-xl border border-amber-200 bg-amber-50 px-3 py-3 text-xs text-amber-800 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-200">
                          Projected till balance {money(allocationProjectedBalance)} exceeds the configured mid-day limit of {money(selectedAllocationTill.midDayCashLimit)}. This cash move will be routed to approval.
                        </div>
                      )}
                      <input
                        type="text"
                        value={allocateForm.reference ?? ''}
                        onChange={(event) =>
                          setAllocateForm((current) => ({
                            ...current,
                            reference: event.target.value,
                          }))
                        }
                        className={inputClass}
                        placeholder="Reference"
                      />
                      <input
                        type="text"
                        value={allocateForm.narration ?? ''}
                        onChange={(event) =>
                          setAllocateForm((current) => ({
                            ...current,
                            narration: event.target.value,
                          }))
                        }
                        className={inputClass}
                        placeholder="Narration"
                      />
                      <button
                        type="button"
                        disabled={submitting || !allocateForm.tellerId || allocateForm.amount <= 0}
                        onClick={() =>
                          void runAction(
                            () => allocationRequiresApproval ? submitTillAllocationApproval() : vaultService.allocateTillCash(allocateForm),
                            allocationRequiresApproval ? 'Till cash movement submitted for approval.' : 'Till cash allocated successfully.',
                          )
                        }
                        className={actionButtonClass}
                      >
                        {allocationRequiresApproval ? 'Submit for approval' : 'Allocate cash'}
                      </button>
                    </div>

                    <div className="space-y-4 rounded-2xl border border-slate-200 p-4 dark:border-slate-800">
                      <div className="flex items-center gap-2 font-semibold text-slate-900 dark:text-white">
                        <ArrowUpCircle className="h-4 w-4" />
                        Return to vault
                      </div>
                      <select
                        value={returnForm.tellerId}
                        onChange={(event) =>
                          setReturnForm((current) => ({
                            ...current,
                            tellerId: event.target.value,
                          }))
                        }
                        className={inputClass}
                      >
                        <option value="">Select open teller</option>
                        {renderTellerOptions('open')}
                      </select>
                      <select
                        value={returnForm.branchId}
                        onChange={(event) =>
                          setReturnForm((current) => ({
                            ...current,
                            branchId: event.target.value,
                          }))
                        }
                        className={inputClass}
                      >
                        <option value="">Use teller branch</option>
                        {renderBranchOptions()}
                      </select>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={returnForm.amount}
                        onChange={(event) =>
                          setReturnForm((current) => ({
                            ...current,
                            amount: Number(event.target.value),
                          }))
                        }
                        className={inputClass}
                        placeholder="Amount"
                        readOnly
                      />
                    <DenominationEditor
                      lines={returnForm.denominations ?? []}
                      onChange={(denomination, bucket, pieces) =>
                        setReturnForm((current) =>
                          syncTillTransferAmount({
                            ...current,
                            denominations: updateDenominationLines(current.denominations, denomination, bucket, pieces),
                          }),
                        )
                      }
                    />
                    <div className="rounded-xl bg-slate-50 px-3 py-2 text-xs text-slate-500 dark:bg-slate-900/60 dark:text-slate-400">
                      Count summary: {formatCountSummary(returnForm.denominations ?? []).summary || 'No notes entered'}. Accepted cash {money(formatCountSummary(returnForm.denominations ?? []).accepted)}. Suspect notes {money(formatCountSummary(returnForm.denominations ?? []).suspect)}.
                    </div>
                    <ControlFields
                      controlReference={returnForm.controlReference}
                      witnessOfficer={returnForm.witnessOfficer}
                      sealNumber={returnForm.sealNumber}
                      onChange={(field, value) =>
                        setReturnForm((current) => ({
                          ...current,
                          [field]: value,
                        }))
                      }
                    />
                      <input
                        type="text"
                        value={returnForm.reference ?? ''}
                        onChange={(event) =>
                          setReturnForm((current) => ({
                            ...current,
                            reference: event.target.value,
                          }))
                        }
                        className={inputClass}
                        placeholder="Reference"
                      />
                      <input
                        type="text"
                        value={returnForm.narration ?? ''}
                        onChange={(event) =>
                          setReturnForm((current) => ({
                            ...current,
                            narration: event.target.value,
                          }))
                        }
                        className={inputClass}
                        placeholder="Narration"
                      />
                      <button
                        type="button"
                        disabled={submitting || !returnForm.tellerId || returnForm.amount <= 0}
                        onClick={() =>
                          void runAction(
                            () => vaultService.returnTillCash(returnForm),
                            'Till cash returned to vault.',
                          )
                        }
                        className={actionButtonClass}
                      >
                        Return cash
                      </button>
                    </div>
                  </div>
                </div>

                <div className={`${cardClass} p-6`}>
                  <SectionHeader
                    title="Close and reconcile till"
                    detail="Return physical cash, capture the close note, and lock the till session for the teller."
                  />
                  <div className="space-y-4">
                    <select
                      value={closeForm.tellerId}
                      onChange={(event) =>
                        setCloseForm((current) => ({
                          ...current,
                          tellerId: event.target.value,
                        }))
                      }
                      className={inputClass}
                    >
                      <option value="">Select open teller</option>
                      {renderTellerOptions('open')}
                    </select>
                    <select
                      value={closeForm.branchId}
                      onChange={(event) =>
                        setCloseForm((current) => ({
                          ...current,
                          branchId: event.target.value,
                        }))
                      }
                      className={inputClass}
                    >
                      <option value="">Use teller branch</option>
                      {renderBranchOptions()}
                    </select>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={closeForm.physicalCashCount}
                      onChange={(event) =>
                        setCloseForm((current) => ({
                          ...current,
                          physicalCashCount: Number(event.target.value),
                        }))
                      }
                      className={inputClass}
                      placeholder="Physical cash returned"
                      readOnly
                    />
                    <DenominationEditor
                      lines={closeForm.denominations ?? []}
                      onChange={(denomination, bucket, pieces) =>
                        setCloseForm((current) =>
                          syncCloseTillAmount({
                            ...current,
                            denominations: updateDenominationLines(current.denominations, denomination, bucket, pieces),
                          }),
                        )
                      }
                    />
                    <div className="rounded-xl bg-slate-50 px-3 py-2 text-xs text-slate-500 dark:bg-slate-900/60 dark:text-slate-400">
                      Close count summary: {formatCountSummary(closeForm.denominations ?? []).summary || 'No notes entered'}. Accepted cash {money(formatCountSummary(closeForm.denominations ?? []).accepted)}. Suspect notes {money(formatCountSummary(closeForm.denominations ?? []).suspect)}.
                    </div>
                    <ControlFields
                      controlReference={closeForm.controlReference}
                      witnessOfficer={closeForm.witnessOfficer}
                      sealNumber={closeForm.sealNumber}
                      onChange={(field, value) =>
                        setCloseForm((current) => ({
                          ...current,
                          [field]: value,
                        }))
                      }
                    />
                      <button
                      type="button"
                      disabled={submitting || !closeForm.tellerId}
                      onClick={() =>
                        void runAction(
                          () => vaultService.closeTill(closeForm),
                          'Till closed and reconciled.',
                        )
                      }
                      className={actionButtonClass}
                    >
                      Close till
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'vault' && (
            <div className="grid gap-6 xl:grid-cols-[1.15fr_1fr]">
              <div className={`${cardClass} p-6`}>
                <SectionHeader
                  title="Branch vault controls"
                  detail="Post direct vault deposits and withdrawals, and keep branch cash within the policy envelope."
                />
                <div className="grid gap-6 md:grid-cols-2">
                  <div className="space-y-4 rounded-2xl border border-slate-200 p-4 dark:border-slate-800">
                    <div className="flex items-center gap-2 font-semibold text-slate-900 dark:text-white">
                      <ShieldCheck className="h-4 w-4" />
                      Vault movement
                    </div>
                    <select
                      value={vaultTxForm.branchId}
                      onChange={(event) =>
                        setVaultTxForm((current) => ({
                          ...current,
                          branchId: event.target.value,
                        }))
                      }
                      className={inputClass}
                    >
                      <option value="">Select branch</option>
                      {renderBranchOptions()}
                    </select>
                    <select
                      value={vaultTxForm.type}
                      onChange={(event) =>
                        setVaultTxForm((current) => ({
                          ...current,
                          type: event.target.value,
                        }))
                      }
                      className={inputClass}
                    >
                      <option value="Deposit">Deposit to vault</option>
                      <option value="Withdrawal">Withdraw from vault</option>
                    </select>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={vaultTxForm.amount}
                      onChange={(event) =>
                        setVaultTxForm((current) => ({
                          ...current,
                          amount: Number(event.target.value),
                        }))
                      }
                      className={inputClass}
                      placeholder="Amount"
                      readOnly
                    />
                    <DenominationEditor
                      lines={vaultTxForm.denominations ?? []}
                      onChange={(denomination, bucket, pieces) =>
                        setVaultTxForm((current) =>
                          syncVaultMovementAmount({
                            ...current,
                            denominations: updateDenominationLines(current.denominations, denomination, bucket, pieces),
                          }),
                        )
                      }
                    />
                    <div className="rounded-xl bg-slate-50 px-3 py-2 text-xs text-slate-500 dark:bg-slate-900/60 dark:text-slate-400">
                      Movement count summary: {formatCountSummary(vaultTxForm.denominations ?? []).summary || 'No notes entered'}. Accepted cash {money(formatCountSummary(vaultTxForm.denominations ?? []).accepted)}. Suspect notes {money(formatCountSummary(vaultTxForm.denominations ?? []).suspect)}.
                    </div>
                    <ControlFields
                      controlReference={vaultTxForm.controlReference}
                      witnessOfficer={vaultTxForm.witnessOfficer}
                      sealNumber={vaultTxForm.sealNumber}
                      onChange={(field, value) =>
                        setVaultTxForm((current) => ({
                          ...current,
                          [field]: value,
                        }))
                      }
                    />
                    <input
                      type="text"
                      value={vaultTxForm.reference ?? ''}
                      onChange={(event) =>
                        setVaultTxForm((current) => ({
                          ...current,
                          reference: event.target.value,
                        }))
                      }
                      className={inputClass}
                      placeholder="Reference"
                    />
                    <textarea
                      value={vaultTxForm.narration ?? ''}
                      onChange={(event) =>
                        setVaultTxForm((current) => ({
                          ...current,
                          narration: event.target.value,
                        }))
                      }
                      className={`${inputClass} min-h-[88px]`}
                      placeholder="Narration"
                    />
                    <button
                      type="button"
                      disabled={submitting || !vaultTxForm.branchId || vaultTxForm.amount <= 0}
                      onClick={() =>
                        void runAction(
                          () => vaultService.processVaultTransaction(vaultTxForm),
                          'Vault transaction posted successfully.',
                        )
                      }
                      className={actionButtonClass}
                    >
                      Post vault movement
                    </button>
                  </div>

                  <div className="space-y-4 rounded-2xl border border-slate-200 p-4 dark:border-slate-800">
                    <div className="flex items-center gap-2 font-semibold text-slate-900 dark:text-white">
                      <Scale className="h-4 w-4" />
                      Record physical count
                    </div>
                    <select
                      value={vaultCountForm.branchId}
                      onChange={(event) =>
                        setVaultCountForm((current) => ({
                          ...current,
                          branchId: event.target.value,
                        }))
                      }
                      className={inputClass}
                    >
                      <option value="">Select branch</option>
                      {renderBranchOptions()}
                    </select>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={vaultCountForm.amount}
                      onChange={(event) =>
                        setVaultCountForm((current) => ({
                          ...current,
                          amount: Number(event.target.value),
                        }))
                      }
                      className={inputClass}
                      placeholder="Physical vault count"
                      readOnly
                    />
                    <DenominationEditor
                      lines={vaultCountForm.denominations ?? []}
                      onChange={(denomination, bucket, pieces) =>
                        setVaultCountForm((current) =>
                          syncVaultCountAmount({
                            ...current,
                            denominations: updateDenominationLines(current.denominations, denomination, bucket, pieces),
                          }),
                        )
                      }
                    />
                    <div className="rounded-xl bg-slate-50 px-3 py-2 text-xs text-slate-500 dark:bg-slate-900/60 dark:text-slate-400">
                      Count summary: {formatCountSummary(vaultCountForm.denominations ?? []).summary || 'No notes entered'}. Accepted cash {money(formatCountSummary(vaultCountForm.denominations ?? []).accepted)}. Suspect notes {money(formatCountSummary(vaultCountForm.denominations ?? []).suspect)}.
                    </div>
                    <ControlFields
                      controlReference={vaultCountForm.controlReference}
                      witnessOfficer={vaultCountForm.witnessOfficer}
                      sealNumber={vaultCountForm.sealNumber}
                      countReason={vaultCountForm.countReason}
                      onChange={(field, value) =>
                        setVaultCountForm((current) => ({
                          ...current,
                          [field]: value,
                        }))
                      }
                    />
                    <button
                      type="button"
                      disabled={submitting || !vaultCountForm.branchId || vaultCountForm.amount < 0}
                      onClick={() =>
                        void runAction(
                          () => vaultService.recordVaultCount(vaultCountForm),
                          'Vault count recorded.',
                        )
                      }
                      className={actionButtonClass}
                    >
                      Save count
                    </button>
                  </div>
                </div>
              </div>

              <div className={`${cardClass} p-6`}>
                <SectionHeader
                  title="Policy view"
                  detail="Quick reference for vault and till compliance posture by branch."
                />
                <div className="space-y-4">
                  {vaults.map((vault) => (
                    <div
                      key={`policy-${vault.branchId}`}
                      className="rounded-2xl border border-slate-200 p-4 dark:border-slate-800"
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div>
                          <p className="font-semibold text-slate-900 dark:text-white">
                            {vault.branchName}
                          </p>
                          <p className="text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                            {vault.branchCode}
                          </p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-slate-600 dark:bg-slate-900 dark:text-slate-300">
                          {vault.currency}
                        </span>
                      </div>
                      <div className="mt-4 grid gap-3 sm:grid-cols-2">
                        <div>
                          <p className="text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                            Cash on hand
                          </p>
                          <p className="mt-1 font-semibold text-slate-900 dark:text-white">
                            {money(vault.cashOnHand)}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                            Last count
                          </p>
                          <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
                            {dateTime(vault.lastCountDate)}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                            Minimum balance
                          </p>
                          <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
                            {vault.minBalance ? money(vault.minBalance) : 'Not set'}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                            Vault limit
                          </p>
                          <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
                            {vault.vaultLimit ? money(vault.vaultLimit) : 'Not set'}
                          </p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </>
      )}

    </div>
  );
}

function DenominationEditor({
  lines,
  onChange,
}: {
  lines: CashDenominationLineDto[];
  onChange: (denomination: string, bucket: 'fitPieces' | 'unfitPieces' | 'suspectPieces', pieces: number) => void;
}) {
  const acceptedTotal = calculateDenominationTotal(lines);
  const suspectTotal = calculateSuspectTotal(lines);

  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 dark:border-slate-800 dark:bg-slate-900/60">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
          Denomination count
        </div>
        <div className="flex flex-wrap gap-2 text-[11px] font-semibold uppercase tracking-[0.14em]">
          <span className="rounded-full bg-emerald-100 px-3 py-1 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-200">Accepted {money(acceptedTotal)}</span>
          <span className="rounded-full bg-amber-100 px-3 py-1 text-amber-700 dark:bg-amber-900/30 dark:text-amber-200">Suspect {money(suspectTotal)}</span>
        </div>
      </div>
      <div className="mb-3 rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs text-slate-500 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-400">
        Fit notes remain in circulation, unfit notes remain genuine but should be segregated for return, and suspect notes must be quarantined and investigated.
      </div>
      <div className="grid gap-3">
        {lines.map((line) => (
          <div key={line.denomination} className="rounded-xl border border-slate-200 bg-white px-3 py-3 text-sm dark:border-slate-700 dark:bg-slate-950">
            <div className="mb-3 flex items-center justify-between gap-2">
              <span className="font-semibold text-slate-900 dark:text-white">GHS {line.denomination}</span>
              <div className="text-right text-xs text-slate-500 dark:text-slate-400">
                <div>Accepted {money(line.totalValue)}</div>
                <div>Suspect {money(line.suspectValue || 0)}</div>
              </div>
            </div>
            <div className="grid gap-3 md:grid-cols-3">
              <label className="text-xs font-medium text-slate-600 dark:text-slate-300">
                Fit
                <input
                  type="number"
                  min="0"
                  step="1"
                  value={line.fitPieces || 0}
                  onChange={(event) => onChange(line.denomination, 'fitPieces', Number(event.target.value))}
                  className="mt-1 w-full rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-right font-mono text-sm text-slate-900 outline-none focus:border-slate-400 dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                />
              </label>
              <label className="text-xs font-medium text-slate-600 dark:text-slate-300">
                Unfit
                <input
                  type="number"
                  min="0"
                  step="1"
                  value={line.unfitPieces || 0}
                  onChange={(event) => onChange(line.denomination, 'unfitPieces', Number(event.target.value))}
                  className="mt-1 w-full rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-right font-mono text-sm text-slate-900 outline-none focus:border-slate-400 dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                />
              </label>
              <label className="text-xs font-medium text-slate-600 dark:text-slate-300">
                Suspect
                <input
                  type="number"
                  min="0"
                  step="1"
                  value={line.suspectPieces || 0}
                  onChange={(event) => onChange(line.denomination, 'suspectPieces', Number(event.target.value))}
                  className="mt-1 w-full rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-right font-mono text-sm text-slate-900 outline-none focus:border-amber-400 dark:border-amber-800 dark:bg-amber-950/30 dark:text-white"
                />
              </label>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function ControlFields({
  controlReference,
  witnessOfficer,
  sealNumber,
  onChange,
  countReason,
}: {
  controlReference?: string;
  witnessOfficer?: string;
  sealNumber?: string;
  countReason?: string;
  onChange: (field: 'controlReference' | 'witnessOfficer' | 'sealNumber' | 'countReason', value: string) => void;
}) {
  return (
    <div className="grid gap-3 md:grid-cols-2">
      <input
        type="text"
        value={controlReference ?? ''}
        onChange={(event) => onChange('controlReference', event.target.value)}
        className={inputClass}
        placeholder="Control reference"
      />
      <input
        type="text"
        value={witnessOfficer ?? ''}
        onChange={(event) => onChange('witnessOfficer', event.target.value)}
        className={inputClass}
        placeholder="Witness officer"
      />
      <input
        type="text"
        value={sealNumber ?? ''}
        onChange={(event) => onChange('sealNumber', event.target.value)}
        className={inputClass}
        placeholder="Bag or seal number"
      />
      {countReason !== undefined && (
        <input
          type="text"
          value={countReason ?? ''}
          onChange={(event) => onChange('countReason', event.target.value)}
          className={inputClass}
          placeholder="Count reason"
        />
      )}
    </div>
  );
}









