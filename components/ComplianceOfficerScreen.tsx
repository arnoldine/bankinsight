import React, { useMemo } from 'react';
import { Customer } from '../types';
import { AlertTriangle, CheckCircle, Eye, Shield, UserCheck } from 'lucide-react';
import PermissionGuard from './PermissionGuard';

interface ComplianceOfficerScreenProps {
  clients: Customer[];
  onVerifyKYC?: (clientCIF: string, status: string, notes: string) => void;
  onFlagTransaction?: (transactionId: string, reason: string) => void;
  onUpdateRiskScore?: (clientCIF: string, score: number) => void;
}

export default function ComplianceOfficerScreen({ clients }: ComplianceOfficerScreenProps) {
  const metrics = useMemo(() => {
    const pendingKyc = clients.filter((client) => client.status === 'PENDING' || !client.kycVerified).length;
    const highRisk = clients.filter((client) => client.riskRating === 'High').length;
    const mediumRisk = clients.filter((client) => client.riskRating === 'Medium').length;
    const verified = clients.filter((client) => client.kycVerified).length;
    const kycCoverage = clients.length > 0 ? Math.round((verified / clients.length) * 100) : 0;

    return { pendingKyc, highRisk, mediumRisk, kycCoverage };
  }, [clients]);

  const reviewQueues = [
    {
      title: 'KYC Review',
      description: 'Run onboarding and customer due-diligence review from governed customer and onboarding workbenches.',
      tone: 'amber',
      icon: <UserCheck className="h-5 w-5 text-amber-700" />,
    },
    {
      title: 'Security Ops',
      description: 'Use Security Ops for suspicious sessions, device monitoring, and access anomalies.',
      tone: 'red',
      icon: <Shield className="h-5 w-5 text-red-700" />,
    },
    {
      title: 'Reporting and Audit',
      description: 'Use reporting and audit workspaces for control evidence, reviews, and regulatory follow-up.',
      tone: 'emerald',
      icon: <Eye className="h-5 w-5 text-emerald-700" />,
    },
  ];

  return (
    <PermissionGuard permission={['CLIENT_READ', 'AUDIT_READ']} fallback={<div className="p-6 text-red-600">Access denied - Compliance permissions required</div>}>
      <div className="h-full rounded-[28px] border border-slate-200 bg-slate-50 p-6">
        <div className="space-y-6">
          <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
            <div className="flex items-start justify-between gap-6">
              <div className="flex items-center gap-4">
                <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-slate-800">
                  <Shield className="h-6 w-6" />
                </div>
                <div>
                  <h1 className="text-2xl font-bold text-slate-900">Compliance Operations</h1>
                  <p className="mt-1 text-sm text-slate-600">
                    Compliance work is production-managed through KYC onboarding, Security Ops, reporting, and audit flows rather than a standalone prototype console.
                  </p>
                </div>
              </div>
              <div className="rounded-2xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-900">
                <div className="font-semibold">Governed routing</div>
                <div className="mt-1">This screen now summarizes live readiness and directs users to the right control workbench.</div>
              </div>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <MetricCard label="Pending KYC" value={String(metrics.pendingKyc)} icon={<AlertTriangle className="h-5 w-5 text-amber-700" />} />
            <MetricCard label="High Risk Clients" value={String(metrics.highRisk)} icon={<Shield className="h-5 w-5 text-red-700" />} />
            <MetricCard label="Medium Risk Clients" value={String(metrics.mediumRisk)} icon={<Eye className="h-5 w-5 text-violet-700" />} />
            <MetricCard label="KYC Coverage" value={`${metrics.kycCoverage}%`} icon={<CheckCircle className="h-5 w-5 text-emerald-700" />} />
          </div>

          <div className="grid gap-6 xl:grid-cols-[1fr_1fr]">
            <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-900">Recommended Compliance Flow</h2>
              <div className="mt-5 space-y-4">
                {reviewQueues.map((queue) => (
                  <div key={queue.title} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                    <div className="flex items-center gap-3">
                      <div className="rounded-2xl bg-white p-2 shadow-sm">{queue.icon}</div>
                      <div className="font-semibold text-slate-900">{queue.title}</div>
                    </div>
                    <div className="mt-2 text-sm text-slate-600">{queue.description}</div>
                  </div>
                ))}
              </div>
            </div>

            <div className="rounded-[24px] border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-900">Control Notes</h2>
              <div className="mt-5 space-y-3 text-sm text-slate-700">
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                  New-device review, suspicious-session monitoring, and device restrictions should be handled from Security Ops.
                </div>
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                  Customer due diligence should be completed from onboarding and customer maintenance flows so decisions stay auditable.
                </div>
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                  Regulatory evidence, returns, and audit packs should come from reporting and audit modules, not a disconnected side dashboard.
                </div>
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
