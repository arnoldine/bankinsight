import React, { useState, useEffect, useRef } from 'react';
import { Play, CheckCircle, AlertCircle, Loader2, Clock, Database, FileText, Shield, Lock, Calendar, Terminal } from 'lucide-react';
import { EodLog } from '../types';
import { operationsService } from '../src/services/operationsService';

interface EodConsoleProps {
    businessDate: string;
    onRunStep: (step: string) => Promise<any>;
    isLive?: boolean;
}

const STEPS = [
    { id: 'PRE_VALIDATION', label: 'Pre-Close Validation', icon: Shield, desc: 'Check pending transactions and unbalanced journals' },
    { id: 'SAVINGS_ACCRUAL', label: 'Interest Accrual', icon: FileText, desc: 'Run the backend savings accrual batch for active deposit accounts.' },
    { id: 'LOAN_AGING', label: 'Loan Accrual Batch', icon: Clock, desc: 'Run the backend loan accrual process for the current business date' },
    { id: 'GL_POSTING', label: 'GL Close Readiness', icon: Lock, desc: 'Check for draft journals still blocking close' },
    { id: 'BACKUP_DB', label: 'System Snapshot', icon: Database, desc: 'Database snapshot remains an infrastructure operation' },
    { id: 'CLOSE_DATE', label: 'Date Rollover', icon: Calendar, desc: 'Advance the configured system business date when close blockers are cleared' }
];

const EodConsole: React.FC<EodConsoleProps> = ({ businessDate, onRunStep, isLive = false }) => {
    const [currentStepIndex, setCurrentStepIndex] = useState(-1);
    const [isRunning, setIsRunning] = useState(false);
    const [logs, setLogs] = useState<EodLog[]>([]);
    const [statusMap, setStatusMap] = useState<Record<string, 'WAITING' | 'RUNNING' | 'SUCCESS' | 'WARNING' | 'ERROR'>>({});
    const [resolvedBusinessDate, setResolvedBusinessDate] = useState(businessDate);
    const [businessStatus, setBusinessStatus] = useState('OPEN');
    const [summary, setSummary] = useState<{ pendingTransactions: number; draftJournalEntries: number; activeLoans: number; schedulerEnabled: boolean; schedulerTimeUtc: string; lastSchedulerRunDate?: string | null; nextScheduledRunUtc?: string | null; } | null>(null);
    const logsEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        logsEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [logs]);

    useEffect(() => {
        const loadStatus = async () => {
            if (!isLive) {
                setResolvedBusinessDate(businessDate);
                return;
            }

            try {
                const status = await operationsService.getEodStatus();
                setResolvedBusinessDate(status.businessDate || businessDate);
                setBusinessStatus(status.status || 'OPEN');
                setSummary({
                    pendingTransactions: status.pendingTransactions,
                    draftJournalEntries: status.draftJournalEntries,
                    activeLoans: status.activeLoans,
                    schedulerEnabled: status.schedulerEnabled,
                    schedulerTimeUtc: status.schedulerTimeUtc,
                    lastSchedulerRunDate: status.lastSchedulerRunDate,
                    nextScheduledRunUtc: status.nextScheduledRunUtc,
                });
            } catch (error) {
                console.error('Failed to load EOD status', error);
            }
        };

        loadStatus();
    }, [businessDate, isLive]);

    const addLog = (message: string, status: 'INFO' | 'SUCCESS' | 'WARNING' | 'ERROR' = 'INFO', step: string = 'SYSTEM') => {
        setLogs(prev => [...prev, {
            timestamp: new Date().toLocaleTimeString(),
            step,
            message,
            status
        }]);
    };

    const runBatch = async () => {
        if (isRunning) return;
        setIsRunning(true);
        setCurrentStepIndex(0);
        setLogs([]);
        addLog(`${isLive ? 'Starting' : 'Previewing'} EOD Batch for ${resolvedBusinessDate}`, 'INFO', 'INIT');

        const initialStatus: any = {};
        STEPS.forEach(s => initialStatus[s.id] = 'WAITING');
        setStatusMap(initialStatus);

        for (let i = 0; i < STEPS.length; i++) {
            const step = STEPS[i];
            setCurrentStepIndex(i);
            setStatusMap(prev => ({ ...prev, [step.id]: 'RUNNING' }));
            addLog(`Executing ${step.label}...`, 'INFO', step.id);

            try {
                const result = isLive
                    ? await operationsService.runEodStep(step.id, resolvedBusinessDate)
                    : await onRunStep(step.id);
                const stepStatus = (result.status || 'SUCCESS') as 'SUCCESS' | 'WARNING' | 'ERROR';
                addLog(result.message, stepStatus, step.id);
                setStatusMap(prev => ({ ...prev, [step.id]: stepStatus }));
                if ((result as any).businessDate) {
                    setResolvedBusinessDate((result as any).businessDate);
                }
            } catch (e: any) {
                addLog(e.message || 'Step Failed', 'ERROR', step.id);
                setStatusMap(prev => ({ ...prev, [step.id]: 'ERROR' }));
                setIsRunning(false);
                return;
            }
        }

        setIsRunning(false);
        addLog(isLive ? 'End of Day workflow completed.' : 'Runbook preview completed.', 'SUCCESS', 'COMPLETE');

        if (isLive) {
            try {
                const status = await operationsService.getEodStatus();
                setResolvedBusinessDate(status.businessDate || resolvedBusinessDate);
                setBusinessStatus(status.status || 'OPEN');
                setSummary({
                    pendingTransactions: status.pendingTransactions,
                    draftJournalEntries: status.draftJournalEntries,
                    activeLoans: status.activeLoans,
                    schedulerEnabled: status.schedulerEnabled,
                    schedulerTimeUtc: status.schedulerTimeUtc,
                    lastSchedulerRunDate: status.lastSchedulerRunDate,
                    nextScheduledRunUtc: status.nextScheduledRunUtc,
                });
            } catch (error) {
                console.error('Failed to refresh EOD status', error);
            }
        }
    };

    return (
        <div className="flex flex-col h-full bg-gray-100 rounded-xl overflow-hidden">
            <div className="bg-gray-900 text-white p-6 shadow-md border-b border-gray-800 flex justify-between items-center">
                <div>
                    <h1 className="text-2xl font-bold flex items-center gap-3">
                        <Terminal size={24} className="text-green-400" />
                        System Operations Console
                    </h1>
                    <div className="flex items-center gap-4 mt-2 text-gray-400 text-sm">
                        <span className="flex items-center gap-1"><Calendar size={14} /> Business Date: <span className="text-gray-900 dark:text-white font-mono">{resolvedBusinessDate}</span></span>
                        <span className="flex items-center gap-1"><Lock size={14} /> Mode: <span className={isLive ? 'text-green-400' : 'text-amber-400'}>{isLive ? 'LIVE' : 'PREVIEW'}</span></span>
                        <span className="flex items-center gap-1"><Clock size={14} /> Status: <span className="text-green-400">{businessStatus}</span></span>
                    </div>
                </div>
                <button
                    onClick={runBatch}
                    disabled={isRunning}
                    className="px-6 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-700 disabled:text-gray-500 rounded-lg font-bold shadow-lg flex items-center gap-2 transition-all"
                >
                    {isRunning ? <Loader2 className="animate-spin" size={20} /> : <Play size={20} />}
                    {isRunning ? 'Running Batch...' : (isLive ? 'Start End of Day' : 'Preview Runbook')}
                </button>
            </div>

            <div className={`${isLive ? 'bg-emerald-50 border-emerald-200 text-emerald-900' : 'bg-amber-50 border-amber-200 text-amber-900'} border-b px-6 py-3 text-sm`}>
                {isLive
                    ? 'Live mode. Supported steps run against backend services, and only unsupported infrastructure actions return explicit manual warnings. The scheduler can auto-run the batch once per business date.'
                    : 'Preview mode only. The sequence below is an operational runbook and does not execute backend batch jobs yet.'}
            </div>

            {summary && (
                <div className="grid gap-3 border-b border-gray-200 bg-white px-6 py-4 md:grid-cols-2 xl:grid-cols-5">
                    <div className="rounded-lg border border-gray-200 px-4 py-3">
                        <div className="text-xs uppercase tracking-wide text-gray-500">Pending Transactions</div>
                        <div className="mt-1 text-xl font-semibold text-gray-900">{summary.pendingTransactions}</div>
                    </div>
                    <div className="rounded-lg border border-gray-200 px-4 py-3">
                        <div className="text-xs uppercase tracking-wide text-gray-500">Draft Journals</div>
                        <div className="mt-1 text-xl font-semibold text-gray-900">{summary.draftJournalEntries}</div>
                    </div>
                    <div className="rounded-lg border border-gray-200 px-4 py-3">
                        <div className="text-xs uppercase tracking-wide text-gray-500">Active Loans</div>
                        <div className="mt-1 text-xl font-semibold text-gray-900">{summary.activeLoans}</div>
                    </div>
                    <div className="rounded-lg border border-gray-200 px-4 py-3">
                        <div className="text-xs uppercase tracking-wide text-gray-500">Scheduler</div>
                        <div className="mt-1 text-sm font-semibold text-gray-900">{summary.schedulerEnabled ? `Enabled at ${summary.schedulerTimeUtc} UTC` : 'Disabled'}</div>
                        <div className="mt-1 text-xs text-gray-500">Last run: {summary.lastSchedulerRunDate || 'Never'}</div>
                    </div>
                    <div className="rounded-lg border border-gray-200 px-4 py-3">
                        <div className="text-xs uppercase tracking-wide text-gray-500">Next Scheduled Run</div>
                        <div className="mt-1 text-sm font-semibold text-gray-900">{summary.nextScheduledRunUtc ? new Date(summary.nextScheduledRunUtc).toLocaleString() : 'Not scheduled'}</div>
                    </div>
                </div>
            )}

            <div className="flex-1 overflow-hidden flex flex-col md:flex-row">
                <div className="w-full md:w-1/3 bg-white border-r border-gray-200 overflow-y-auto p-6">
                    <h3 className="font-bold text-gray-800 mb-6 uppercase text-xs tracking-wider border-b pb-2">Batch Workflow Sequence</h3>
                    <div className="space-y-6 relative">
                        <div className="absolute left-6 top-4 bottom-4 w-0.5 bg-gray-100 -z-10"></div>

                        {STEPS.map((step, idx) => {
                            const status = statusMap[step.id] || 'WAITING';
                            const isActive = currentStepIndex === idx && isRunning;

                            return (
                                <div key={step.id} className={`flex items-start gap-4 p-3 rounded-lg transition-colors ${isActive ? 'bg-blue-50' : ''}`}>
                                    <div className={`w-12 h-12 rounded-full flex items-center justify-center flex-shrink-0 border-2 z-10 bg-white transition-colors
                                        ${status === 'SUCCESS' ? 'border-green-500 text-green-500' :
                                            status === 'WARNING' ? 'border-amber-500 text-amber-500' :
                                            status === 'ERROR' ? 'border-red-500 text-red-500' :
                                            status === 'RUNNING' ? 'border-blue-500 text-blue-500' : 'border-gray-200 text-gray-300'}
                                    `}>
                                        {status === 'RUNNING' ? <Loader2 size={20} className="animate-spin" /> :
                                            status === 'SUCCESS' ? <CheckCircle size={20} /> :
                                            status === 'WARNING' ? <AlertCircle size={20} /> :
                                            status === 'ERROR' ? <AlertCircle size={20} /> :
                                            <step.icon size={20} />}
                                    </div>
                                    <div>
                                        <h4 className={`font-bold text-sm ${status === 'WAITING' ? 'text-gray-500' : 'text-gray-800'}`}>{step.label}</h4>
                                        <p className="text-xs text-gray-500 mt-1 leading-relaxed">{step.desc}</p>
                                        {status === 'RUNNING' && <span className="text-[10px] text-blue-600 font-bold animate-pulse">Processing...</span>}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>

                <div className="flex-1 bg-[#1e1e1e] p-4 flex flex-col font-mono text-sm overflow-hidden">
                    <div className="flex items-center justify-between text-gray-500 text-xs mb-2 border-b border-gray-700 pb-2">
                        <span>SYS.LOG OUTPUT</span>
                        <div className="flex gap-2">
                            <div className="w-3 h-3 rounded-full bg-red-500"></div>
                            <div className="w-3 h-3 rounded-full bg-yellow-500"></div>
                            <div className="w-3 h-3 rounded-full bg-green-500"></div>
                        </div>
                    </div>
                    <div className="flex-1 overflow-y-auto space-y-1 pr-2">
                        {logs.length === 0 && (
                            <div className="text-gray-600 italic pt-10 text-center opacity-50">
                                &gt; System ready. Initiate batch process to view logs...
                            </div>
                        )}
                        {logs.map((log, i) => (
                            <div key={i} className="flex gap-3 hover:bg-[#2d2d2d] p-0.5 rounded">
                                <span className="text-gray-500 w-20 flex-shrink-0">{log.timestamp}</span>
                                <span className={`w-24 flex-shrink-0 font-bold ${log.status === 'SUCCESS' ? 'text-green-500' :
                                        log.status === 'WARNING' ? 'text-yellow-400' :
                                        log.status === 'ERROR' ? 'text-red-500' : 'text-blue-400'
                                    }`}>[{log.step}]</span>
                                <span className="text-gray-300">{log.message}</span>
                            </div>
                        ))}
                        <div ref={logsEndRef} />
                    </div>
                </div>
            </div>
        </div>
    );
};

export default EodConsole;
