import React, { useEffect, useMemo, useState } from 'react';
import { BookOpen, Check, ChevronDown, ChevronRight, File, FileText, Filter, Folder, Layers, Plus, RefreshCw, Save, Search, ShieldCheck, X } from 'lucide-react';
import { CreateGlAccountRequest, GlAccount, JournalEntry } from '../src/services/glService';

type AccountingView = 'COA' | 'JV' | 'LEDGER';

interface AccountingEngineProps {
  accounts: GlAccount[];
  journalEntries: JournalEntry[];
  onPostJournal: (payload: { description: string; reference?: string; lines: { code: string; debit: number; credit: number }[] }) => Promise<void> | void;
  onCreateAccount: (account: CreateGlAccountRequest) => Promise<void> | void;
  onDirtyChange?: (dirty: boolean) => void;
  initialView?: AccountingView;
  canPostJournal?: boolean;
  canManageAccounts?: boolean;
  onRefresh?: () => Promise<void> | void;
  isLoading?: boolean;
}

const emptyLine = () => ({ accountCode: '', debit: 0, credit: 0 });

const amount = (value: number) =>
  Number(value || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const AccountingEngine: React.FC<AccountingEngineProps> = ({
  accounts,
  journalEntries,
  onPostJournal,
  onCreateAccount,
  onDirtyChange,
  initialView = 'COA',
  canPostJournal = false,
  canManageAccounts = false,
  onRefresh,
  isLoading = false,
}) => {
  const [view, setView] = useState<AccountingView>(initialView);
  const [glSearch, setGlSearch] = useState('');
  const [ledgerSearch, setLedgerSearch] = useState('');
  const [jvLines, setJvLines] = useState([emptyLine(), emptyLine()]);
  const [jvDescription, setJvDescription] = useState('');
  const [jvReference, setJvReference] = useState('');
  const [showAddGLModal, setShowAddGLModal] = useState(false);
  const [newGL, setNewGL] = useState<CreateGlAccountRequest>({ code: '', name: '', category: 'ASSET', currency: 'GHS', isHeader: false });
  const [feedback, setFeedback] = useState<{ tone: 'success' | 'error' | 'info'; message: string } | null>(null);
  const [busyAction, setBusyAction] = useState<'post' | 'create' | 'refresh' | null>(null);
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set(['1', '2', '3', '4', '5']));

  useEffect(() => {
    setView(initialView);
  }, [initialView]);

  const resetDraft = () => {
    setJvLines([emptyLine(), emptyLine()]);
    setJvDescription('');
    setJvReference('');
  };

  const nonHeaderAccounts = useMemo(
    () => accounts.filter((account) => !account.isHeader).sort((left, right) => left.code.localeCompare(right.code)),
    [accounts],
  );

  const treeData = useMemo(() => {
    const query = glSearch.trim().toLowerCase();
    const roots = accounts.filter((account) => account.isHeader);

    return roots
      .map((root) => ({
        ...root,
        children: nonHeaderAccounts.filter((child) => child.code.startsWith(root.code.charAt(0))),
      }))
      .map((root) => ({
        ...root,
        children: root.children.filter((child) => !query || [child.code, child.name, child.category].some((value) => String(value || '').toLowerCase().includes(query))),
      }))
      .filter((root) => !query || [root.code, root.name, root.category].some((value) => String(value || '').toLowerCase().includes(query)) || root.children.length > 0);
  }, [accounts, glSearch, nonHeaderAccounts]);

  const ledgerRows = useMemo(() => {
    const flattened = journalEntries.flatMap((entry) =>
      (entry.lines || []).map((line) => ({
        journalId: entry.id,
        date: entry.date,
        description: entry.description || 'Journal entry',
        reference: entry.reference || entry.id,
        postedBy: entry.postedBy || 'System',
        accountCode: line.accountCode,
        accountName: line.accountName || accounts.find((account) => account.code === line.accountCode)?.name || 'Unknown account',
        debit: Number(line.debit || 0),
        credit: Number(line.credit || 0),
      })),
    );

    const query = ledgerSearch.trim().toLowerCase();
    return flattened.filter((row) => !query || [row.journalId, row.description, row.accountCode, row.accountName, row.postedBy].some((value) => String(value || '').toLowerCase().includes(query)));
  }, [accounts, journalEntries, ledgerSearch]);

  const draftDebit = jvLines.reduce((sum, line) => sum + Number(line.debit || 0), 0);
  const draftCredit = jvLines.reduce((sum, line) => sum + Number(line.credit || 0), 0);
  const draftDifference = draftDebit - draftCredit;
  const hasJournalDraft = useMemo(
    () =>
      jvDescription.trim().length > 0 ||
      jvReference.trim().length > 0 ||
      jvLines.length > 2 ||
      jvLines.some((line) => line.accountCode.trim().length > 0 || Number(line.debit || 0) > 0 || Number(line.credit || 0) > 0),
    [jvDescription, jvLines, jvReference],
  );
  const hasNewGlDraft = useMemo(
    () =>
      newGL.code.trim().length > 0 ||
      newGL.name.trim().length > 0 ||
      newGL.category !== 'ASSET' ||
      (newGL.currency || 'GHS').trim().toUpperCase() !== 'GHS' ||
      Boolean(newGL.isHeader),
    [newGL],
  );

  useEffect(() => {
    onDirtyChange?.(hasJournalDraft || hasNewGlDraft);
  }, [hasJournalDraft, hasNewGlDraft, onDirtyChange]);

  const toggleNode = (code: string) => {
    const next = new Set(expandedNodes);
    if (next.has(code)) {
      next.delete(code);
    } else {
      next.add(code);
    }
    setExpandedNodes(next);
  };

  const updateLine = (index: number, field: 'accountCode' | 'debit' | 'credit', value: string | number) => {
    setJvLines((current) =>
      current.map((line, lineIndex) =>
        lineIndex === index
          ? {
              ...line,
              [field]: field === 'accountCode' ? value : Number(value || 0),
            }
          : line,
      ),
    );
  };

  const addLine = () => setJvLines((current) => [...current, emptyLine()]);

  const removeLine = (index: number) => {
    if (jvLines.length <= 2) {
      return;
    }
    setJvLines((current) => current.filter((_, lineIndex) => lineIndex !== index));
  };

  const validateDraft = () => {
    if (!jvDescription.trim()) {
      return 'Enter a journal description before posting.';
    }

    if (jvLines.length < 2) {
      return 'At least two journal lines are required.';
    }

    if (jvLines.some((line) => !line.accountCode.trim())) {
      return 'Every journal line must be mapped to a GL account.';
    }

    if (jvLines.some((line) => (Number(line.debit) > 0 && Number(line.credit) > 0) || (Number(line.debit) <= 0 && Number(line.credit) <= 0))) {
      return 'Each journal line must contain either a debit or a credit amount.';
    }

    if (draftDifference !== 0) {
      return `Journal is out of balance by ${amount(Math.abs(draftDifference))}.`;
    }

    return null;
  };

  const postJournal = async () => {
    const validationMessage = validateDraft();
    if (validationMessage) {
      setFeedback({ tone: 'error', message: validationMessage });
      return;
    }

    setBusyAction('post');
    setFeedback(null);
    try {
      await onPostJournal({
        description: jvDescription.trim(),
        reference: jvReference.trim() || undefined,
        lines: jvLines.map((line) => ({ code: line.accountCode, debit: Number(line.debit || 0), credit: Number(line.credit || 0) })),
      });
      resetDraft();
      setFeedback({ tone: 'success', message: 'Journal posted successfully and the ledger has been refreshed.' });
      setView('LEDGER');
    } catch (err) {
      setFeedback({ tone: 'error', message: (err as Error).message || 'Unable to post this journal entry right now.' });
    } finally {
      setBusyAction(null);
    }
  };

  const handleCreateGL = async () => {
    if (!newGL.name.trim()) {
      setFeedback({ tone: 'error', message: 'Account name is required before creating a GL account.' });
      return;
    }

    setBusyAction('create');
    setFeedback(null);
    try {
      await onCreateAccount({
        ...newGL,
        code: newGL.code.trim(),
        name: newGL.name.trim(),
        currency: (newGL.currency || 'GHS').trim().toUpperCase(),
      });
      setShowAddGLModal(false);
      setNewGL({ code: '', name: '', category: 'ASSET', currency: 'GHS', isHeader: false });
      setFeedback({ tone: 'success', message: 'GL account created successfully.' });
    } catch (err) {
      setFeedback({ tone: 'error', message: (err as Error).message || 'Unable to create the GL account right now.' });
    } finally {
      setBusyAction(null);
    }
  };

  const handleRefresh = async () => {
    if (!onRefresh) {
      return;
    }

    setBusyAction('refresh');
    setFeedback(null);
    try {
      await onRefresh();
      setFeedback({ tone: 'info', message: 'Accounting data refreshed from the live general ledger.' });
    } catch (err) {
      setFeedback({ tone: 'error', message: (err as Error).message || 'Unable to refresh accounting data right now.' });
    } finally {
      setBusyAction(null);
    }
  };

  return (
    <div className="simple-screen relative flex h-full flex-col overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm">
      <div className="screen-hero px-6 py-5">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs font-semibold text-slate-500">Finance and controls</p>
            <h2 className="mt-2 text-2xl font-semibold text-slate-900">General ledger operations with governed posting and live statement-ready data.</h2>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">GL accounts</p><p className="mt-1 text-xl font-semibold text-slate-900">{accounts.length}</p></div>
            <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">Journal entries</p><p className="mt-1 text-xl font-semibold text-slate-900">{journalEntries.length}</p></div>
            <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">Draft status</p><p className="mt-1 text-xl font-semibold text-slate-900">{draftDifference === 0 ? 'Balanced' : amount(draftDifference)}</p></div>
            <button onClick={handleRefresh} disabled={!onRefresh || busyAction === 'refresh'} className="rounded-xl border border-slate-200 px-4 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-60 flex items-center gap-2">
              <RefreshCw size={16} className={busyAction === 'refresh' || isLoading ? 'animate-spin' : ''} />
              Refresh
            </button>
          </div>
        </div>
      </div>

      {feedback && (
        <div className={`mx-6 mt-4 rounded-2xl border px-4 py-3 text-sm ${
          feedback.tone === 'success'
            ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
            : feedback.tone === 'info'
              ? 'border-blue-200 bg-blue-50 text-blue-800'
              : 'border-rose-200 bg-rose-50 text-rose-800'
        }`}>
          {feedback.message}
        </div>
      )}

      <div className="flex border-b border-gray-200 bg-gray-50">
        <button onClick={() => setView('COA')} className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'COA' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}>
          <Layers size={16} /> Chart of Accounts
        </button>
        <button onClick={() => setView('JV')} className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'JV' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}>
          <Plus size={16} /> Journal Posting
        </button>
        <button onClick={() => setView('LEDGER')} className={`px-6 py-4 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${view === 'LEDGER' ? 'border-blue-600 text-blue-600 bg-white' : 'border-transparent text-gray-500 hover:text-gray-700'}`}>
          <BookOpen size={16} /> Ledger Review
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-6 bg-gray-50/50">
        {view === 'COA' && (
          <div className="bg-white border border-gray-200 rounded-lg shadow-sm">
            <div className="p-4 border-b border-gray-200 flex flex-col gap-3 md:flex-row md:justify-between md:items-center bg-gray-50">
              <div>
                <h3 className="font-bold text-gray-800">General Ledger Structure</h3>
                <p className="text-xs text-gray-500 mt-1">Header accounts define the posting tree. Child accounts carry the actual balances.</p>
              </div>
              <div className="flex gap-2">
                <div className="relative">
                  <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                  <input type="text" value={glSearch} onChange={(e) => setGlSearch(e.target.value)} placeholder="Search GL code, name, or category" className="pl-9 pr-4 py-2 text-sm border rounded-lg focus:outline-none focus:border-blue-500" />
                </div>
                {canManageAccounts && (
                  <button onClick={() => setShowAddGLModal(true)} className="bg-blue-600 text-white px-3 py-2 rounded-lg text-xs font-bold hover:bg-blue-700 flex items-center gap-1">
                    <Plus size={14} /> New Account
                  </button>
                )}
              </div>
            </div>
            <div className="p-4">
              {treeData.length === 0 ? (
                <div className="rounded-2xl border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500">No accounts matched the current search.</div>
              ) : (
                treeData.map((root) => (
                  <div key={root.code} className="mb-4">
                    <div className="flex items-center gap-2 p-2 hover:bg-gray-50 rounded cursor-pointer select-none" onClick={() => toggleNode(root.code)}>
                      <span className="text-gray-400">{expandedNodes.has(root.code) ? <ChevronDown size={16} /> : <ChevronRight size={16} />}</span>
                      <Folder size={18} className="text-blue-500 fill-blue-100" />
                      <span className="font-bold text-gray-800">{root.code} - {root.name}</span>
                      <span className="ml-auto text-xs px-2 py-0.5 bg-gray-100 text-gray-600 rounded uppercase">{root.category}</span>
                    </div>
                    {expandedNodes.has(root.code) && (
                      <div className="ml-8 border-l-2 border-gray-100 pl-2 mt-1 space-y-1">
                        {root.children.map((child) => (
                          <div key={child.code} className="flex items-center justify-between p-2 hover:bg-blue-50 rounded group">
                            <div className="flex items-center gap-3">
                              <File size={16} className="text-gray-400 group-hover:text-blue-500" />
                              <div>
                                <div className="text-sm font-medium text-gray-700 group-hover:text-blue-700">{child.code} - {child.name}</div>
                                <div className="text-xs text-gray-500 uppercase tracking-[0.14em]">{child.category}</div>
                              </div>
                            </div>
                            <div className="font-mono text-sm text-gray-800 font-medium">{amount(child.balance)} {child.currency || 'GHS'}</div>
                          </div>
                        ))}
                        {root.children.length === 0 && <div className="text-xs text-gray-400 italic p-2">No sub-accounts</div>}
                      </div>
                    )}
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        {view === 'JV' && (
          <div className="max-w-5xl mx-auto bg-white border border-gray-200 rounded-lg shadow-sm p-6">
            <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between mb-6">
              <div>
                <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2">
                  <FileText className="text-blue-600" /> New Journal Entry
                </h3>
                <p className="text-sm text-gray-500 mt-1">Posting is controlled by finance permissions. Header accounts cannot be used as posting lines.</p>
              </div>
              <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700 flex items-center gap-2">
                <ShieldCheck size={16} />
                {canPostJournal ? 'Posting enabled for your role' : 'Read-only view: posting restricted'}
              </div>
            </div>

            <div className="mb-6 grid grid-cols-1 gap-4 md:grid-cols-3">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <input type="text" value={jvDescription} onChange={(e) => setJvDescription(e.target.value)} className="w-full border rounded p-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none" placeholder="e.g. Month-end accrual" disabled={!canPostJournal} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Reference</label>
                <input type="text" value={jvReference} onChange={(e) => setJvReference(e.target.value)} className="w-full border rounded p-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none" placeholder="Optional control reference" disabled={!canPostJournal} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Posting Date</label>
                <input type="date" className="w-full border rounded p-2 text-sm bg-gray-50" value={new Date().toISOString().split('T')[0]} readOnly />
              </div>
            </div>

            <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-3">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-500">Draft debit</p><p className="mt-2 text-xl font-bold text-slate-900">{amount(draftDebit)}</p></div>
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-500">Draft credit</p><p className="mt-2 text-xl font-bold text-slate-900">{amount(draftCredit)}</p></div>
              <div className={`rounded-2xl border ${draftDifference === 0 ? 'border-emerald-200 bg-emerald-50' : 'border-rose-200 bg-rose-50'} p-4`}><p className={`text-xs uppercase tracking-[0.18em] ${draftDifference === 0 ? 'text-emerald-600' : 'text-rose-600'}`}>Difference</p><p className="mt-2 text-xl font-bold text-slate-900">{amount(draftDifference)}</p></div>
            </div>

            <div className="border rounded-lg overflow-hidden mb-6">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 text-gray-600 text-xs uppercase">
                  <tr>
                    <th className="p-3 text-left">Account</th>
                    <th className="p-3 text-right">Debit</th>
                    <th className="p-3 text-right">Credit</th>
                    <th className="p-3 text-right">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {jvLines.map((line, idx) => (
                    <tr key={idx}>
                      <td className="p-2">
                        <select className="w-full p-2 border rounded bg-white" value={line.accountCode} onChange={(e) => updateLine(idx, 'accountCode', e.target.value)} disabled={!canPostJournal}>
                          <option value="">Select posting account...</option>
                          {nonHeaderAccounts.map((account) => (
                            <option key={account.code} value={account.code}>{account.code} - {account.name}</option>
                          ))}
                        </select>
                      </td>
                      <td className="p-2">
                        <input type="number" value={line.debit} onChange={(e) => updateLine(idx, 'debit', Number(e.target.value))} className="w-full p-2 border rounded text-right font-mono focus:ring-blue-500 outline-none" disabled={!canPostJournal} min="0" step="0.01" />
                      </td>
                      <td className="p-2">
                        <input type="number" value={line.credit} onChange={(e) => updateLine(idx, 'credit', Number(e.target.value))} className="w-full p-2 border rounded text-right font-mono focus:ring-blue-500 outline-none" disabled={!canPostJournal} min="0" step="0.01" />
                      </td>
                      <td className="p-2 text-right">
                        <button onClick={() => removeLine(idx)} disabled={!canPostJournal || jvLines.length <= 2} className="rounded-lg border border-slate-200 px-3 py-2 text-xs text-slate-600 hover:bg-slate-50 disabled:opacity-50">
                          Remove
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-gray-50 font-bold text-gray-700">
                  <tr>
                    <td className="p-3 text-right">Totals</td>
                    <td className="p-3 text-right font-mono">{amount(draftDebit)}</td>
                    <td className="p-3 text-right font-mono">{amount(draftCredit)}</td>
                    <td className="p-3" />
                  </tr>
                </tfoot>
              </table>
            </div>

            <div className="flex justify-between">
              <button onClick={addLine} disabled={!canPostJournal} className="text-blue-600 hover:bg-blue-50 px-4 py-2 rounded text-sm font-medium flex items-center gap-1 disabled:opacity-50">
                <Plus size={16} /> Add Line
              </button>
              <div className="flex gap-3">
                <button onClick={resetDraft} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded text-sm">Reset Draft</button>
                <button onClick={postJournal} disabled={!canPostJournal || busyAction === 'post'} className="px-6 py-2 bg-blue-600 text-white rounded font-medium hover:bg-blue-700 text-sm shadow-sm flex items-center gap-2 disabled:opacity-60">
                  {busyAction === 'post' ? <RefreshCw size={16} className="animate-spin" /> : <Check size={16} />} Post Entry
                </button>
              </div>
            </div>
          </div>
        )}

        {view === 'LEDGER' && (
          <div className="bg-white border border-gray-200 rounded-lg shadow-sm">
            <div className="p-4 border-b border-gray-200 flex flex-col gap-3 md:flex-row md:justify-between md:items-center">
              <div>
                <h3 className="font-bold text-gray-800">Transaction Journal</h3>
                <p className="text-xs text-gray-500 mt-1">Review journal activity, account impact, and posting ownership from one ledger queue.</p>
              </div>
              <div className="flex gap-2">
                <div className="relative">
                  <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                  <input type="text" value={ledgerSearch} onChange={(e) => setLedgerSearch(e.target.value)} placeholder="Search journal, account, or user" className="pl-9 pr-4 py-2 text-sm border rounded-lg focus:outline-none focus:border-blue-500" />
                </div>
                <span className="rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 flex items-center gap-2"><Filter size={14} /> Live ledger</span>
              </div>
            </div>
            {ledgerRows.length === 0 ? (
              <div className="p-10 text-center text-sm text-slate-500">No journal lines are available for the current filters.</div>
            ) : (
              <table className="w-full text-sm text-left">
                <thead className="bg-gray-50 text-gray-600 font-medium">
                  <tr>
                    <th className="p-3">Date</th>
                    <th className="p-3">Journal</th>
                    <th className="p-3">Description</th>
                    <th className="p-3">Account</th>
                    <th className="p-3">Posted By</th>
                    <th className="p-3 text-right">Debit</th>
                    <th className="p-3 text-right">Credit</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {ledgerRows.map((row) => (
                    <tr key={`${row.journalId}-${row.accountCode}-${row.debit}-${row.credit}`} className="hover:bg-gray-50">
                      <td className="p-3 text-gray-500">{row.date}</td>
                      <td className="p-3">
                        <p className="font-mono text-blue-600 text-xs">{row.journalId}</p>
                        <p className="text-xs text-gray-500">{row.reference}</p>
                      </td>
                      <td className="p-3 text-gray-800">{row.description}</td>
                      <td className="p-3">
                        <span className="bg-gray-100 text-gray-700 px-2 py-0.5 rounded text-xs font-mono">{row.accountCode}</span>
                        <p className="text-xs text-gray-500 mt-1">{row.accountName}</p>
                      </td>
                      <td className="p-3 text-gray-600">{row.postedBy}</td>
                      <td className="p-3 text-right font-mono text-gray-700">{row.debit > 0 ? amount(row.debit) : '-'}</td>
                      <td className="p-3 text-right font-mono text-gray-700">{row.credit > 0 ? amount(row.credit) : '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}

        {showAddGLModal && (
          <div className="absolute inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6">
              <div className="flex justify-between items-center mb-4">
                <h3 className="font-bold text-lg text-gray-800">Add GL Account</h3>
                <button onClick={() => setShowAddGLModal(false)}><X size={20} className="text-gray-400 hover:text-gray-600" /></button>
              </div>
              <div className="space-y-4">
                <div>
                  <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Account Code</label>
                  <input type="text" className="w-full border p-2 rounded text-sm font-mono" value={newGL.code} onChange={(e) => setNewGL({ ...newGL, code: e.target.value })} placeholder="Leave blank to auto-generate" />
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Account Name</label>
                  <input type="text" className="w-full border p-2 rounded text-sm" value={newGL.name} onChange={(e) => setNewGL({ ...newGL, name: e.target.value })} placeholder="e.g. Office Supplies" />
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Category</label>
                  <select className="w-full border p-2 rounded text-sm bg-white" value={newGL.category} onChange={(e) => setNewGL({ ...newGL, category: e.target.value })}>
                    <option value="ASSET">ASSET</option>
                    <option value="LIABILITY">LIABILITY</option>
                    <option value="EQUITY">EQUITY</option>
                    <option value="INCOME">INCOME</option>
                    <option value="EXPENSE">EXPENSE</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-500 uppercase mb-1">Currency</label>
                  <input type="text" className="w-full border p-2 rounded text-sm uppercase" value={newGL.currency || 'GHS'} onChange={(e) => setNewGL({ ...newGL, currency: e.target.value })} />
                </div>
                <div className="flex items-center gap-2 pt-2">
                  <input type="checkbox" id="isHeader" checked={newGL.isHeader} onChange={(e) => setNewGL({ ...newGL, isHeader: e.target.checked })} />
                  <label htmlFor="isHeader" className="text-sm text-gray-700">Create as header account</label>
                </div>
                <button onClick={handleCreateGL} disabled={busyAction === 'create'} className="w-full bg-blue-600 text-white font-bold py-2 rounded mt-4 hover:bg-blue-700 disabled:opacity-60 flex items-center justify-center gap-2">
                  {busyAction === 'create' ? <RefreshCw size={16} className="animate-spin" /> : <Save size={16} />}
                  Create Account
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default AccountingEngine;
