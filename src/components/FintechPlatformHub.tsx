
import React, { useEffect, useMemo, useState } from 'react';
import {
  Activity,
  AlertTriangle,
  ArrowRightLeft,
  BookOpen,
  Building2,
  CheckCircle2,
  Coins,
  ExternalLink,
  Landmark,
  RefreshCw,
  Shield,
  Smartphone,
  Wallet,
} from 'lucide-react';
import { useFintechPlatform } from '../hooks/useApi';
import type {
  FintechAlert,
  FintechApprovalQueueItem,
  FintechReconciliationItem,
  FintechTransferExplorerItem,
  FintechTransferInvestigation,
  FintechWalletSummary,
  FintechWorkspaceSnapshot,
} from '../services/fintechPlatformService';

type FintechTab = 'overview' | 'transfers' | 'compliance' | 'treasury' | 'reconciliation';
type InvestigationKind = 'transfer' | 'approval' | 'alert' | 'reconciliation';

interface FintechPlatformHubProps {
  initialTab?: FintechTab;
}

interface SummaryCard {
  label: string;
  value: string;
  helper: string;
  tone: string;
  icon: React.ComponentType<{ className?: string }>;
}

const tabOptions: Array<{ id: FintechTab; label: string }> = [
  { id: 'overview', label: 'Overview' },
  { id: 'transfers', label: 'Transfers' },
  { id: 'compliance', label: 'Compliance' },
  { id: 'treasury', label: 'Treasury' },
  { id: 'reconciliation', label: 'Reconciliation' },
];

const operationalPillars = [
  {
    title: 'Custodial wallet operations',
    description: 'Manage custodial crypto deposits, internal GHS wallet balances, conversion controls, and payout lifecycle visibility.',
    icon: Wallet,
  },
  {
    title: 'Hybrid payout rails',
    description: 'Orchestrate transfers to mobile money, bank accounts, internal wallets, and approved external crypto destinations.',
    icon: ArrowRightLeft,
  },
  {
    title: 'Compliance-first controls',
    description: 'Run tiered KYC, sanctions hooks, maker-checker approvals, holds, monitoring rules, and investigation workflows.',
    icon: Shield,
  },
];

const transferChannels = [
  { channel: 'Internal wallets', status: 'Ready', helper: 'Instant internal wallet-to-wallet transfers using the internal ledger.' },
  { channel: 'Mobile money', status: 'Connector-ready', helper: 'MTN, Telecel, and AirtelTigo payout orchestration with callbacks and retries.' },
  { channel: 'Bank transfers', status: 'Connector-ready', helper: 'Bank payout initiation, settlement monitoring, and account validation support.' },
  { channel: 'Crypto withdrawals', status: 'Controlled', helper: 'Hot-wallet queueing with approval workflow, allowlists, and fee handling.' },
];

const treasuryControls = [
  'Track hot-wallet inventory against operational thresholds and cold-storage top-up policy.',
  'Monitor partner clearing, suspense balances, and daily settlement obligations by rail.',
  'Publish GHS and crypto exposure views for treasury, finance, and compliance teams.',
  'Support sandbox and live connector modes independently for controlled rollout.',
];

const formatTone = (tone: string) => {
  switch (tone) {
    case 'emerald':
      return 'border-emerald-200 bg-emerald-50 text-emerald-900';
    case 'amber':
      return 'border-amber-200 bg-amber-50 text-amber-900';
    case 'blue':
      return 'border-blue-200 bg-blue-50 text-blue-900';
    case 'rose':
      return 'border-rose-200 bg-rose-50 text-rose-900';
    default:
      return 'border-slate-200 bg-white text-slate-900';
  }
};

const formatAmount = (amount: number, currency = 'GHS') => new Intl.NumberFormat('en-GH', {
  style: 'currency',
  currency,
  maximumFractionDigits: 2,
}).format(amount);

const formatDateTime = (value: string) => {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? 'N/A' : parsed.toLocaleString('en-GH', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const severityTone = (severity: string) => {
  switch (severity.toUpperCase()) {
    case 'CRITICAL':
      return 'bg-rose-100 text-rose-900';
    case 'HIGH':
      return 'bg-amber-100 text-amber-900';
    default:
      return 'bg-slate-100 text-slate-700';
  }
};

const statusTone = (status: string) => {
  switch (status.toUpperCase()) {
    case 'POSTED':
    case 'CLEAR':
    case 'HEALTHY':
    case 'APPROVED':
      return 'bg-emerald-100 text-emerald-900';
    case 'PENDING':
    case 'ONHOLD':
      return 'bg-amber-100 text-amber-900';
    case 'FAILED':
    case 'UNAVAILABLE':
    case 'REJECTED':
      return 'bg-rose-100 text-rose-900';
    default:
      return 'bg-slate-100 text-slate-700';
  }
};

const EmptyState = ({ message }: { message: string }) => (
  <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-sm text-slate-500">{message}</div>
);

const getOperatorIdentity = () => {
  try {
    const raw = localStorage.getItem('auth_user');
    if (!raw) {
      return 'bankinsight-operator';
    }

    const parsed = JSON.parse(raw) as { email?: string; name?: string };
    return parsed.email || parsed.name || 'bankinsight-operator';
  } catch {
    return 'bankinsight-operator';
  }
};

export default function FintechPlatformHub({ initialTab = 'overview' }: FintechPlatformHubProps) {
  const [activeTab, setActiveTab] = useState<FintechTab>(initialTab);
  const [snapshot, setSnapshot] = useState<FintechWorkspaceSnapshot | null>(null);
  const [investigationKind, setInvestigationKind] = useState<InvestigationKind | null>(null);
  const [transferInvestigation, setTransferInvestigation] = useState<FintechTransferInvestigation | null>(null);
  const [selectedApproval, setSelectedApproval] = useState<FintechApprovalQueueItem | null>(null);
  const [selectedAlert, setSelectedAlert] = useState<FintechAlert | null>(null);
  const [selectedReconciliation, setSelectedReconciliation] = useState<FintechReconciliationItem | null>(null);
  const [investigationLoading, setInvestigationLoading] = useState(false);
  const [investigationError, setInvestigationError] = useState<string | null>(null);
  const [approvalNotes, setApprovalNotes] = useState('Reviewed in Bankinsight fintech module.');
  const [actionNotice, setActionNotice] = useState<string | null>(null);
  const [reconForm, setReconForm] = useState({
    reconciliationType: 'ManualAdjustment',
    externalReference: '',
    internalReference: '',
    amount: '0',
    currency: 'GHS',
    notes: '',
  });
  const {
    loading,
    error,
    getWorkspaceSnapshot,
    getTransferInvestigation,
    decideApproval,
    createReconciliationItem,
    getAdminUrl,
    getHealthUrl,
    getSwaggerUrl,
  } = useFintechPlatform();

  useEffect(() => {
    setActiveTab(initialTab);
  }, [initialTab]);

  const refreshSnapshot = async () => {
    const next = await getWorkspaceSnapshot();
    setSnapshot(next);
    return next;
  };

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const next = await getWorkspaceSnapshot();
        if (!cancelled) {
          setSnapshot(next);
        }
      } catch {
        if (!cancelled) {
          setSnapshot(null);
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [getWorkspaceSnapshot]);

  const quickLinks = useMemo(() => [
    {
      label: 'Open Fintech Admin Portal',
      href: getAdminUrl(),
      helper: 'Launch the operations and compliance console for the hybrid transfer platform.',
    },
    {
      label: 'Open Fintech API Health',
      href: getHealthUrl(),
      helper: 'Validate the ASP.NET Core API host, dependency wiring, and environment health.',
    },
    {
      label: 'Open Fintech Swagger',
      href: getSwaggerUrl(),
      helper: 'Inspect the versioned REST surface for wallets, transfers, compliance, and reconciliation.',
    },
  ], [getAdminUrl, getHealthUrl, getSwaggerUrl]);

  const totals = useMemo(() => {
    const wallets = snapshot?.wallets ?? [];
    const availableGhs = wallets.filter((wallet) => wallet.currency.toUpperCase() === 'GHS').reduce((sum, wallet) => sum + wallet.availableBalance, 0);
    const reservedGhs = wallets.filter((wallet) => wallet.currency.toUpperCase() === 'GHS').reduce((sum, wallet) => sum + wallet.reservedBalance, 0);
    const duplicateWebhookCount = snapshot?.operationsWatch.duplicateWebhookEvents.length ?? 0;
    const divergenceCount = snapshot?.operationsWatch.divergenceEvents.length ?? 0;
    const systemBreakCount = (snapshot?.reconciliationItems ?? []).filter((item) => item.reconciliationType === 'ProviderLedgerDivergence').length;

    return {
      availableGhs,
      reservedGhs,
      walletCount: wallets.length,
      alertCount: snapshot?.alerts.length ?? 0,
      approvalCount: snapshot?.approvals.length ?? 0,
      reconciliationCount: snapshot?.reconciliationItems.length ?? 0,
      transferCount: snapshot?.transfers.length ?? 0,
      duplicateWebhookCount,
      divergenceCount,
      systemBreakCount,
    };
  }, [snapshot]);

  const summaryCards = useMemo<SummaryCard[]>(() => [
    {
      label: 'Fintech API status',
      value: snapshot?.health.status || 'Checking',
      helper: snapshot?.health.checkedAt ? `Last checked ${formatDateTime(snapshot.health.checkedAt)}.` : 'Awaiting fintech API connectivity check.',
      tone: snapshot?.health.status === 'Healthy' ? 'emerald' : 'rose',
      icon: Activity,
    },
    {
      label: 'GHS wallet float',
      value: formatAmount(totals.availableGhs || 0),
      helper: `Reserved balance ${formatAmount(totals.reservedGhs || 0)} across ${totals.walletCount} tracked wallets.`,
      tone: 'emerald',
      icon: Landmark,
    },
    {
      label: 'Open compliance items',
      value: `${totals.alertCount + totals.approvalCount}`,
      helper: `${totals.alertCount} alerts and ${totals.approvalCount} pending approvals currently require review.`,
      tone: 'amber',
      icon: Shield,
    },
    {
      label: 'Settlement breaks',
      value: `${totals.reconciliationCount}`,
      helper: `${totals.transferCount} recent transfers loaded for operational follow-up.`,
      tone: 'blue',
      icon: CheckCircle2,
    },
    {
      label: 'Callback controls',
      value: `${totals.duplicateWebhookCount + totals.divergenceCount}`,
      helper: `${totals.duplicateWebhookCount} duplicate callbacks ignored and ${totals.systemBreakCount} provider-ledger divergence breaks tracked.`,
      tone: totals.duplicateWebhookCount + totals.divergenceCount > 0 ? 'rose' : 'emerald',
      icon: AlertTriangle,
    },
  ], [snapshot, totals]);

  const renderWalletCards = (wallets: FintechWalletSummary[]) => (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {wallets.map((wallet) => (
        <article key={wallet.walletId} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{wallet.currency} wallet</p>
              <p className="mt-2 text-lg font-semibold text-slate-900">{formatAmount(wallet.availableBalance, wallet.currency)}</p>
            </div>
            <span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusTone(wallet.status)}`}>{wallet.status}</span>
          </div>
          <p className="mt-3 text-sm text-slate-600">Reserved {formatAmount(wallet.reservedBalance, wallet.currency)}</p>
          <p className="mt-1 text-xs text-slate-500">Wallet {wallet.walletId}</p>
        </article>
      ))}
    </div>
  );

  const investigateTransfer = async (transferOrderId: string, kind: InvestigationKind, context?: { approval?: FintechApprovalQueueItem | null }) => {
    setInvestigationLoading(true);
    setInvestigationError(null);
    setActionNotice(null);
    setInvestigationKind(kind);
    setSelectedApproval(context?.approval ?? null);
    setSelectedAlert(null);
    setSelectedReconciliation(null);
    try {
      const result = await getTransferInvestigation(transferOrderId);
      setTransferInvestigation(result);
    } catch (err) {
      setTransferInvestigation(null);
      setInvestigationError((err as Error).message || 'Unable to load investigation details.');
    } finally {
      setInvestigationLoading(false);
    }
  };

  const investigateAlert = (alert: FintechAlert) => {
    setInvestigationKind('alert');
    setSelectedAlert(alert);
    setSelectedApproval(null);
    setSelectedReconciliation(null);
    setTransferInvestigation(null);
    setInvestigationError(null);
    setActionNotice(null);
  };

  const investigateReconciliation = (item: FintechReconciliationItem) => {
    setInvestigationKind('reconciliation');
    setSelectedReconciliation(item);
    setSelectedApproval(null);
    setSelectedAlert(null);
    setTransferInvestigation(null);
    setInvestigationError(null);
    setActionNotice(null);
  };

  const handleApprovalDecision = async (decision: 'approve' | 'reject') => {
    if (!selectedApproval) {
      return;
    }

    setActionNotice(null);
    await decideApproval(selectedApproval.approvalRequestId, {
      approvedBy: getOperatorIdentity(),
      decision,
      decisionNotes: approvalNotes,
    });
    await refreshSnapshot();
    setActionNotice(`Approval ${decision === 'approve' ? 'approved' : 'rejected'} successfully.`);
  };

  const handleCreateReconciliation = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setActionNotice(null);
    await createReconciliationItem({
      reconciliationType: reconForm.reconciliationType,
      externalReference: reconForm.externalReference,
      internalReference: reconForm.internalReference,
      amount: Number(reconForm.amount),
      currency: reconForm.currency,
      notes: reconForm.notes,
    });
    await refreshSnapshot();
    setReconForm({
      reconciliationType: 'ManualAdjustment',
      externalReference: '',
      internalReference: '',
      amount: '0',
      currency: 'GHS',
      notes: '',
    });
    setActionNotice('Reconciliation item created successfully.');
  };

  const renderOperationsWatch = () => {
    const duplicateWebhookEvents = snapshot?.operationsWatch.duplicateWebhookEvents ?? [];
    const divergenceEvents = snapshot?.operationsWatch.divergenceEvents ?? [];

    if (!duplicateWebhookEvents.length && !divergenceEvents.length) {
      return <EmptyState message="No callback replay or provider-ledger divergence signals are currently active." />;
    }

    return (
      <div className="grid gap-4 lg:grid-cols-2">
        <section className="rounded-2xl border border-amber-200 bg-amber-50 p-5">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-amber-700">Replay guard</p>
              <h4 className="mt-1 text-lg font-semibold text-amber-950">Duplicate callbacks ignored</h4>
            </div>
            <span className="rounded-full bg-white px-3 py-1 text-xs font-semibold text-amber-800 shadow-sm">{duplicateWebhookEvents.length}</span>
          </div>
          <div className="mt-4 space-y-3">
            {duplicateWebhookEvents.length ? duplicateWebhookEvents.map((item) => (
              <div key={item.auditEventId} className="rounded-2xl border border-amber-200 bg-white px-4 py-3">
                <p className="text-sm font-semibold text-slate-900">{item.entityType} {item.entityId}</p>
                <p className="mt-1 text-xs text-slate-500">{formatDateTime(item.createdAtUtc)} • {item.actorId}</p>
              </div>
            )) : <p className="text-sm text-amber-900">No duplicate callback receipts have been ignored in the current snapshot window.</p>}
          </div>
        </section>

        <section className="rounded-2xl border border-rose-200 bg-rose-50 p-5">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-rose-700">Divergence watch</p>
              <h4 className="mt-1 text-lg font-semibold text-rose-950">Provider-ledger mismatches</h4>
            </div>
            <span className="rounded-full bg-white px-3 py-1 text-xs font-semibold text-rose-800 shadow-sm">{divergenceEvents.length}</span>
          </div>
          <div className="mt-4 space-y-3">
            {divergenceEvents.length ? divergenceEvents.map((item) => (
              <div key={item.auditEventId} className="rounded-2xl border border-rose-200 bg-white px-4 py-3">
                <p className="text-sm font-semibold text-slate-900">{item.entityType} {item.entityId}</p>
                <p className="mt-1 text-xs text-slate-500">{formatDateTime(item.createdAtUtc)} • {item.actorId}</p>
              </div>
            )) : <p className="text-sm text-rose-900">No provider-ledger divergence events are currently active.</p>}
          </div>
        </section>
      </div>
    );
  };
  const renderInvestigationPanel = () => {
    if (!investigationKind) {
      return null;
    }

    return (
      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Investigation workspace</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">
              {investigationKind === 'approval' ? 'Approval review' : investigationKind === 'alert' ? 'Alert detail' : investigationKind === 'reconciliation' ? 'Reconciliation detail' : 'Transfer detail'}
            </h3>
          </div>
          <button type="button" onClick={() => { setInvestigationKind(null); setTransferInvestigation(null); setSelectedApproval(null); setSelectedAlert(null); setSelectedReconciliation(null); setInvestigationError(null); setActionNotice(null); }} className="rounded-full border border-slate-200 px-3 py-1 text-sm font-semibold text-slate-700">Close</button>
        </div>

        {investigationLoading ? <div className="mt-5 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-6 text-sm text-slate-500">Loading investigation...</div> : null}
        {investigationError ? <div className="mt-5 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">{investigationError}</div> : null}
        {actionNotice ? <div className="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900">{actionNotice}</div> : null}

        {selectedAlert ? (
          <div className="mt-5 rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <div className="flex items-center justify-between gap-3">
              <p className="text-base font-semibold text-slate-900">{selectedAlert.alertCode}</p>
              <span className={`rounded-full px-3 py-1 text-xs font-semibold ${severityTone(selectedAlert.severity)}`}>{selectedAlert.severity}</span>
            </div>
            <p className="mt-2 text-sm text-slate-600">{selectedAlert.summary}</p>
            <p className="mt-2 text-xs text-slate-500">Customer {selectedAlert.customerId} � Score {selectedAlert.score} � Status {selectedAlert.status}</p>
          </div>
        ) : null}

        {selectedReconciliation ? (
          <div className="mt-5 rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <div className="flex items-center justify-between gap-3">
              <p className="text-base font-semibold text-slate-900">{selectedReconciliation.reconciliationType}</p>
              <span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusTone(selectedReconciliation.status)}`}>{selectedReconciliation.status}</span>
            </div>
            <p className="mt-2 text-sm text-slate-600">External {selectedReconciliation.externalReference} � Internal {selectedReconciliation.internalReference}</p>
            <p className="mt-2 text-sm text-slate-600">{selectedReconciliation.notes}</p>
            <p className="mt-2 text-xs text-slate-500">Amount {formatAmount(selectedReconciliation.amount, selectedReconciliation.currency)}</p>
          </div>
        ) : null}

        {selectedApproval ? (
          <div className="mt-5 rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <div className="flex items-center justify-between gap-3">
              <p className="text-base font-semibold text-slate-900">{selectedApproval.actionCode}</p>
              <span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusTone(selectedApproval.status)}`}>{selectedApproval.status}</span>
            </div>
            <p className="mt-2 text-sm text-slate-600">{selectedApproval.reason}</p>
            <p className="mt-2 text-xs text-slate-500">Requested by {selectedApproval.requestedBy} � {formatDateTime(selectedApproval.createdAtUtc)}</p>
            <div className="mt-4 space-y-3">
              <textarea value={approvalNotes} onChange={(event) => setApprovalNotes(event.target.value)} rows={3} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" placeholder="Decision notes" />
              <div className="flex flex-wrap gap-3">
                <button type="button" onClick={() => void handleApprovalDecision('approve')} className="rounded-2xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white">Approve</button>
                <button type="button" onClick={() => void handleApprovalDecision('reject')} className="rounded-2xl bg-rose-600 px-4 py-2 text-sm font-semibold text-white">Reject</button>
              </div>
            </div>
          </div>
        ) : null}

        {transferInvestigation ? (
          <div className="mt-5 space-y-5">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                  <p className="text-base font-semibold text-slate-900">{transferInvestigation.transfer.channel} {transferInvestigation.transfer.type}</p>
                  <p className="mt-1 text-sm text-slate-600">Reference {transferInvestigation.transfer.partnerReference || transferInvestigation.transfer.transferOrderId}</p>
                  <p className="mt-1 text-xs text-slate-500">Source wallet {transferInvestigation.transfer.sourceWalletId}</p>
                </div>
                <div className="text-right">
                  <p className="text-lg font-semibold text-slate-900">{formatAmount(transferInvestigation.transfer.amount)}</p>
                  <p className="mt-1 text-xs text-slate-500">Fee {formatAmount(transferInvestigation.transfer.fee)}</p>
                </div>
              </div>
              {transferInvestigation.transfer.failureReason ? <p className="mt-3 text-sm text-rose-700">Failure reason: {transferInvestigation.transfer.failureReason}</p> : null}
            </div>

            <div>
              <h4 className="text-sm font-semibold uppercase tracking-[0.18em] text-slate-500">Linked journals</h4>
              <div className="mt-3 space-y-3">
                {transferInvestigation.journals.length ? transferInvestigation.journals.map((journal) => (
                  <div key={journal.journalEntryId} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-sm font-semibold text-slate-900">{journal.reference}</p>
                      <span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusTone(journal.status)}`}>{journal.status}</span>
                    </div>
                    <div className="mt-3 space-y-2">
                      {journal.lines.map((line, index) => (
                        <div key={`${journal.journalEntryId}-${index}`} className="rounded-xl bg-white px-3 py-2 text-xs text-slate-600">{line.narrative} � Dr {formatAmount(line.debit, line.currency)} � Cr {formatAmount(line.credit, line.currency)}</div>
                      ))}
                    </div>
                  </div>
                )) : <EmptyState message="No journals were returned for this transfer." />}
              </div>
            </div>
            <div>
              <h4 className="text-sm font-semibold uppercase tracking-[0.18em] text-slate-500">Audit timeline</h4>
              <div className="mt-3 space-y-3">
                {transferInvestigation.auditEvents.length ? transferInvestigation.auditEvents.map((item) => (
                  <div key={item.auditEventId} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-sm font-semibold text-slate-900">{item.action}</p>
                      <p className="text-xs text-slate-500">{formatDateTime(item.createdAtUtc)}</p>
                    </div>
                    <p className="mt-1 text-xs text-slate-500">Actor {item.actorId}</p>
                  </div>
                )) : <EmptyState message="No audit events were returned for this transfer." />}
              </div>
            </div>
          </div>
        ) : null}
      </section>
    );
  };

  const renderOverview = () => (
    <div className="space-y-6">
      <div className="grid gap-4 lg:grid-cols-4">
        {summaryCards.map((card) => {
          const Icon = card.icon;
          return (
            <div key={card.label} className={`rounded-2xl border p-5 shadow-sm ${formatTone(card.tone)}`}>
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] opacity-70">{card.label}</p>
                  <p className="mt-3 text-2xl font-semibold">{card.value}</p>
                </div>
                <span className="rounded-xl bg-white/70 p-3 shadow-sm"><Icon className="h-5 w-5" /></span>
              </div>
              <p className="mt-4 text-sm leading-6 opacity-80">{card.helper}</p>
            </div>
          );
        })}
      </div>

      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-amber-50 p-3"><Activity className="h-5 w-5 text-amber-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Operations watch</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Replay and divergence signals</h3>
          </div>
        </div>
        <div className="mt-5">
          {renderOperationsWatch()}
        </div>
      </section>

      <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center gap-3">
            <span className="rounded-2xl bg-slate-100 p-3"><BookOpen className="h-5 w-5 text-slate-700" /></span>
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Module scope</p>
              <h3 className="mt-1 text-xl font-semibold text-slate-900">Hybrid transfer operations inside Bankinsight</h3>
            </div>
          </div>
          <div className="mt-5 grid gap-4 md:grid-cols-3">
            {operationalPillars.map((pillar) => {
              const Icon = pillar.icon;
              return (
                <article key={pillar.title} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                  <span className="inline-flex rounded-xl bg-white p-2 shadow-sm"><Icon className="h-5 w-5 text-slate-700" /></span>
                  <h4 className="mt-4 text-lg font-semibold text-slate-900">{pillar.title}</h4>
                  <p className="mt-2 text-sm leading-6 text-slate-600">{pillar.description}</p>
                </article>
              );
            })}
          </div>
          <div className="mt-6">
            <h4 className="text-sm font-semibold uppercase tracking-[0.18em] text-slate-500">Tracked wallets</h4>
            <div className="mt-4">
              {snapshot?.wallets?.length ? renderWalletCards(snapshot.wallets) : <EmptyState message="No wallet projections are currently available from the fintech platform." />}
            </div>
          </div>
        </section>

        <aside className="rounded-3xl border border-slate-200 bg-slate-950 p-6 text-white shadow-sm">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-300">Launch posture</p>
          <h3 className="mt-2 text-2xl font-semibold">Production-grade fintech module</h3>
          <p className="mt-3 text-sm leading-6 text-slate-300">This module gives Bankinsight operators a governed launch point into the Ghana hybrid transfer platform without mixing wallet accounting into retail banking screens.</p>
          <div className="mt-5 space-y-3">
            {quickLinks.map((link) => (
              <a key={link.label} href={link.href} target="_blank" rel="noreferrer" className="flex items-start justify-between gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 transition hover:border-white/20 hover:bg-white/10">
                <div>
                  <p className="font-semibold text-white">{link.label}</p>
                  <p className="mt-1 text-sm text-slate-300">{link.helper}</p>
                </div>
                <ExternalLink className="mt-1 h-4 w-4 flex-shrink-0 text-slate-300" />
              </a>
            ))}
          </div>
        </aside>
      </div>
    </div>
  );

  const renderTransfers = () => (
    <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-blue-50 p-3"><ArrowRightLeft className="h-5 w-5 text-blue-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Transfer orchestration</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Recent fintech transfers</h3>
          </div>
        </div>
        <div className="mt-5 space-y-4">
          {snapshot?.transfers?.length ? snapshot.transfers.map((transfer: FintechTransferExplorerItem) => (
            <div key={transfer.transferOrderId} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                  <p className="text-base font-semibold text-slate-900">{transfer.channel} {transfer.type}</p>
                  <p className="mt-1 text-sm text-slate-600">Created by {transfer.createdBy} on {formatDateTime(transfer.createdAtUtc)}</p>
                  <p className="mt-2 text-sm text-slate-500">Reference {transfer.partnerReference || transfer.transferOrderId}</p>
                </div>
                <div className="text-right">
                  <p className="text-lg font-semibold text-slate-900">{formatAmount(transfer.amount)}</p>
                </div>
              </div>
              <div className="mt-4 flex justify-end">
                <button type="button" onClick={() => void investigateTransfer(transfer.transferOrderId, 'transfer')} className="rounded-2xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700">Investigate</button>
              </div>
            </div>
          )) : <EmptyState message="No transfer activity is currently available from the fintech platform." />}
        </div>
      </section>

      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-emerald-50 p-3"><Smartphone className="h-5 w-5 text-emerald-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Customer actions</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Supported fintech flows</h3>
          </div>
        </div>
        <div className="mt-5 space-y-3">
          {transferChannels.map((item) => (
            <div key={item.channel} className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-slate-50 p-4 md:flex-row md:items-center md:justify-between">
              <div>
                <p className="text-base font-semibold text-slate-900">{item.channel}</p>
                <p className="mt-1 text-sm text-slate-600">{item.helper}</p>
              </div>
              <span className="inline-flex items-center rounded-full bg-white px-3 py-1 text-xs font-semibold text-slate-700 shadow-sm">{item.status}</span>
            </div>
          ))}
        </div>
      </section>
    </div>
  );

  const renderCompliance = () => (
    <div className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-rose-50 p-3"><AlertTriangle className="h-5 w-5 text-rose-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">AML and fraud</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Open risk alerts</h3>
          </div>
        </div>
        <div className="mt-5 space-y-4">
          {snapshot?.alerts?.length ? snapshot.alerts.map((item: FintechAlert) => (
            <article key={item.alertId} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex items-center justify-between gap-3">
                <h4 className="text-base font-semibold text-slate-900">{item.alertCode}</h4>
                <span className={`rounded-full px-3 py-1 text-xs font-semibold ${severityTone(item.severity)}`}>{item.severity}</span>
              </div>
              <p className="mt-2 text-sm leading-6 text-slate-600">{item.summary}</p>
              <p className="mt-2 text-xs text-slate-500">Score {item.score} � Status {item.status}</p>
              <div className="mt-4 flex justify-end">
                <button type="button" onClick={() => investigateAlert(item)} className="rounded-2xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700">Investigate</button>
              </div>
            </article>
          )) : <EmptyState message="No open fintech alerts are currently available." />}
        </div>
      </section>

      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-slate-100 p-3"><Shield className="h-5 w-5 text-slate-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Approval queue</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Pending checker actions</h3>
          </div>
        </div>
        <div className="mt-5 space-y-3">
          {snapshot?.approvals?.length ? snapshot.approvals.map((item: FintechApprovalQueueItem) => (
            <div key={item.approvalRequestId} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex items-center justify-between gap-3">
                <p className="text-base font-semibold text-slate-900">{item.actionCode}</p>
                <span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusTone(item.status)}`}>{item.status}</span>
              </div>
              <p className="mt-2 text-sm leading-6 text-slate-600">{item.reason}</p>
              <p className="mt-2 text-xs text-slate-500">Requested by {item.requestedBy} � {formatDateTime(item.createdAtUtc)}</p>
              <div className="mt-4 flex justify-end">
                <button type="button" onClick={() => void investigateTransfer(item.transferOrderId, 'approval', { approval: item })} className="rounded-2xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700">Investigate</button>
              </div>
            </div>
          )) : <EmptyState message="No pending fintech approvals are currently waiting for review." />}
        </div>
      </section>
    </div>
  );

  const renderTreasury = () => (
    <div className="space-y-6">
      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-amber-50 p-3"><Building2 className="h-5 w-5 text-amber-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Treasury and liquidity</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Balance-sheet and inventory controls</h3>
          </div>
        </div>
        <div className="mt-5 grid gap-4 md:grid-cols-2">
          {treasuryControls.map((item) => (
            <div key={item} className="rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm leading-6 text-slate-600">{item}</div>
          ))}
        </div>
      </section>
      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-slate-100 p-3"><Coins className="h-5 w-5 text-slate-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Wallet inventory</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Tracked fintech balances</h3>
          </div>
        </div>
        <div className="mt-5">
          {snapshot?.wallets?.length ? renderWalletCards(snapshot.wallets) : <EmptyState message="No wallet inventory is currently available from the fintech platform." />}
        </div>
      </section>
    </div>
  );

  const renderReconciliation = () => (
    <div className="space-y-6">
      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-emerald-50 p-3"><CheckCircle2 className="h-5 w-5 text-emerald-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Reconciliation controls</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Create reconciliation break</h3>
          </div>
        </div>
        <form onSubmit={handleCreateReconciliation} className="mt-5 grid gap-4 md:grid-cols-2">
          <input value={reconForm.reconciliationType} onChange={(event) => setReconForm((current) => ({ ...current, reconciliationType: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" placeholder="Reconciliation type" />
          <input value={reconForm.currency} onChange={(event) => setReconForm((current) => ({ ...current, currency: event.target.value.toUpperCase() }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" placeholder="Currency" />
          <input value={reconForm.externalReference} onChange={(event) => setReconForm((current) => ({ ...current, externalReference: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" placeholder="External reference" />
          <input value={reconForm.internalReference} onChange={(event) => setReconForm((current) => ({ ...current, internalReference: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" placeholder="Internal reference" />
          <input value={reconForm.amount} onChange={(event) => setReconForm((current) => ({ ...current, amount: event.target.value }))} type="number" min="0" step="0.01" className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" placeholder="Amount" />
          <input value={reconForm.notes} onChange={(event) => setReconForm((current) => ({ ...current, notes: event.target.value }))} className="rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-700" placeholder="Notes" />
          <div className="md:col-span-2 flex justify-end">
            <button type="submit" className="rounded-2xl bg-slate-950 px-4 py-2 text-sm font-semibold text-white">Create reconciliation item</button>
          </div>
        </form>
      </section>

      <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-2xl bg-slate-100 p-3"><CheckCircle2 className="h-5 w-5 text-slate-700" /></span>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Open breaks</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Fintech reconciliation queue</h3>
          </div>
        </div>
        <div className="mt-5 space-y-4">
          {snapshot?.reconciliationItems?.length ? snapshot.reconciliationItems.map((item: FintechReconciliationItem) => (
            <div key={item.reconciliationItemId} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                  <p className="text-base font-semibold text-slate-900">{item.reconciliationType}</p>
                  <p className="mt-1 text-sm text-slate-600">External {item.externalReference} � Internal {item.internalReference}</p>
                  <p className="mt-2 text-sm text-slate-500">{item.notes}</p>
                </div>
                <div className="text-right">
                  <p className="text-lg font-semibold text-slate-900">{formatAmount(item.amount, item.currency)}</p>
                  <span className={`mt-2 inline-flex rounded-full px-3 py-1 text-xs font-semibold ${statusTone(item.status)}`}>{item.status}</span>
                </div>
              </div>
              <div className="mt-4 flex justify-end">
                <button type="button" onClick={() => investigateReconciliation(item)} className="rounded-2xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700">Investigate</button>
              </div>
            </div>
          )) : <EmptyState message="No open reconciliation items are currently available from the fintech platform." />}
        </div>
      </section>
    </div>
  );

  const renderBody = () => {
    switch (activeTab) {
      case 'transfers':
        return renderTransfers();
      case 'compliance':
        return renderCompliance();
      case 'treasury':
        return renderTreasury();
      case 'reconciliation':
        return renderReconciliation();
      case 'overview':
      default:
        return renderOverview();
    }
  };

  return (
    <div className="simple-screen space-y-6 p-6">
      <div className="screen-hero rounded-[28px] border border-slate-200 bg-gradient-to-br from-slate-950 via-slate-900 to-slate-800 p-6 text-white shadow-sm">
        <div className="flex flex-col gap-5 xl:flex-row xl:items-end xl:justify-between">
          <div className="max-w-3xl">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-cyan-200">Additional module</p>
            <h2 className="mt-3 text-3xl font-semibold">Fintech Platform</h2>
            <p className="mt-3 text-sm leading-7 text-slate-300">Bankinsight now includes a dedicated hybrid money transfer workspace for Ghana-focused custodial crypto deposits, fiat wallet operations, transfer orchestration, compliance, and settlement control.</p>
          </div>
          <button type="button" onClick={() => { void refreshSnapshot(); }} className="inline-flex items-center gap-2 rounded-2xl border border-white/15 bg-white/10 px-4 py-3 text-sm font-semibold text-white transition hover:bg-white/15">
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            {loading ? 'Refreshing' : 'Refresh data'}
          </button>
        </div>
      </div>

      {error ? <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">{error}</div> : null}

      <div className="flex flex-wrap gap-3">
        {tabOptions.map((tab) => {
          const isActive = activeTab === tab.id;
          return (
            <button key={tab.id} type="button" onClick={() => setActiveTab(tab.id)} className={isActive ? 'rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white shadow-sm' : 'rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:text-slate-950'}>
              {tab.label}
            </button>
          );
        })}
      </div>

      {loading && !snapshot ? <div className="rounded-2xl border border-slate-200 bg-white px-4 py-10 text-center text-sm text-slate-500">Loading fintech workspace data...</div> : renderBody()}

      {renderInvestigationPanel()}
    </div>
  );
}
