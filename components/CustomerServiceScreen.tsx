import React, { useMemo } from 'react';
import { Account, Customer, Transaction } from '../types';
import { CheckCircle, FileSearch, Headphones, LifeBuoy, ReceiptText, Users } from 'lucide-react';
import PermissionGuard from './PermissionGuard';

interface CustomerServiceScreenProps {
  clients: Customer[];
  accounts: Account[];
  transactions: Transaction[];
  onResolveIssue?: (issueId: string, resolution: string) => void;
  onCreateTicket?: (ticket: any) => void;
}

export default function CustomerServiceScreen({
  clients,
  accounts,
  transactions,
}: CustomerServiceScreenProps) {
  const metrics = useMemo(() => {
    const activeCustomers = clients.filter((client) => client.status?.toUpperCase?.() === 'ACTIVE').length;
    const activeAccounts = accounts.filter((account) => (account.balance ?? 0) > 0).length;
    const recentTransactions = transactions.length;

    return {
      activeCustomers,
      activeAccounts,
      recentTransactions,
      serviceCoverage: clients.length > 0 ? Math.round((activeCustomers / clients.length) * 100) : 0,
    };
  }, [accounts, clients, transactions]);

  const guidance = [
    'Client servicing should be performed from Client Manager for profile, KYC, and account context.',
    'Use Statement Verification and Transaction Explorer for statement disputes and transaction research.',
    'Escalations and operational follow-up should be logged through governed workflow or approval queues.',
  ];

  return (
    <PermissionGuard permission={['ACCOUNT_READ', 'CLIENT_READ']} fallback={<div className="p-6 text-red-600">Access denied - Customer service permissions required</div>}>
      <div className="h-full rounded-[28px] border border-slate-200 bg-[linear-gradient(180deg,#f8fafc,#eef2f7)] p-6">
        <div className="flex flex-col gap-6">
          <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
            <div className="flex items-start justify-between gap-6">
              <div>
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-sky-100 text-sky-700">
                    <Headphones className="h-6 w-6" />
                  </div>
                  <div>
                    <h1 className="text-2xl font-bold text-slate-900">Customer Service Workspace</h1>
                    <p className="mt-1 text-sm text-slate-600">
                      Production servicing is anchored in client, statement, and transaction workbenches rather than a standalone support prototype.
                    </p>
                  </div>
                </div>
              </div>
              <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                <div className="font-semibold">Production-safe guidance</div>
                <div className="mt-1">This screen now acts as a routing and readiness summary only.</div>
              </div>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <MetricCard label="Active Customers" value={String(metrics.activeCustomers)} icon={<Users className="h-5 w-5 text-sky-700" />} />
            <MetricCard label="Active Accounts" value={String(metrics.activeAccounts)} icon={<CheckCircle className="h-5 w-5 text-emerald-700" />} />
            <MetricCard label="Recent Transactions" value={String(metrics.recentTransactions)} icon={<ReceiptText className="h-5 w-5 text-violet-700" />} />
            <MetricCard label="Service Coverage" value={`${metrics.serviceCoverage}%`} icon={<LifeBuoy className="h-5 w-5 text-amber-700" />} />
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
            <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
              <div className="flex items-center gap-3">
                <FileSearch className="h-5 w-5 text-slate-700" />
                <h2 className="text-lg font-semibold text-slate-900">Recommended Operator Flow</h2>
              </div>
              <div className="mt-5 space-y-3">
                {guidance.map((item) => (
                  <div key={item} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-700">
                    {item}
                  </div>
                ))}
              </div>
            </div>

            <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-900">Use These Workspaces</h2>
              <div className="mt-5 space-y-3">
                <DestinationCard title="Client Manager" description="Customer profile, KYC context, account inventory, and onboarding follow-up." />
                <DestinationCard title="Statement Verification" description="Statement dispute review, document validation, and audit-friendly evidence handling." />
                <DestinationCard title="Transaction Explorer" description="Investigate transaction history, teller references, and posting narratives." />
              </div>
            </div>
          </div>
        </div>
      </div>
    </PermissionGuard>
  );
}

function MetricCard({ label, value, icon }: { label: string; value: string; icon: React.ReactNode }) {
  return (
    <div className="rounded-[24px] border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{label}</div>
          <div className="mt-2 text-2xl font-bold text-slate-900">{value}</div>
        </div>
        <div className="rounded-2xl bg-slate-100 p-3">{icon}</div>
      </div>
    </div>
  );
}

function DestinationCard({ title, description }: { title: string; description: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
      <div className="font-semibold text-slate-900">{title}</div>
      <div className="mt-1 text-sm text-slate-600">{description}</div>
    </div>
  );
}
