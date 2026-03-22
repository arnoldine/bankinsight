import React, { useMemo, useState } from 'react';
import { CheckCircle2, NotebookPen, Search } from 'lucide-react';
import { cashOpsNotesService, CashOpsNoteRecord } from '../src/services/cashOpsNotesService';
import { BranchVaultDto, TellerTillSummaryDto } from '../src/services/vaultService';

interface CashOpsNotesScreenProps {
  vaults?: BranchVaultDto[];
  tills?: TellerTillSummaryDto[];
}

const defaultDraft = {
  branchId: '',
  tellerId: '',
  subject: '',
  details: '',
  category: 'TILL' as const,
};

export default function CashOpsNotesScreen({ vaults = [], tills = [] }: CashOpsNotesScreenProps) {
  const [notes, setNotes] = useState<CashOpsNoteRecord[]>(() => cashOpsNotesService.getNotes());
  const [query, setQuery] = useState('');
  const [draft, setDraft] = useState(defaultDraft);
  const [message, setMessage] = useState<string | null>(null);

  const filteredNotes = useMemo(() => {
    const term = query.trim().toLowerCase();
    if (!term) {
      return notes;
    }

    return notes.filter((note) =>
      [note.id, note.branchId, note.tellerId, note.subject, note.details, note.category, note.status]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(term))
    );
  }, [notes, query]);

  const handleCreate = (event: React.FormEvent) => {
    event.preventDefault();
    if (!draft.subject.trim() || !draft.details.trim()) {
      setMessage('Subject and details are required.');
      return;
    }

    cashOpsNotesService.createNote({
      branchId: draft.branchId || undefined,
      tellerId: draft.tellerId || undefined,
      subject: draft.subject.trim(),
      details: draft.details.trim(),
      category: draft.category,
      status: 'OPEN',
    });

    setNotes(cashOpsNotesService.getNotes());
    setDraft(defaultDraft);
    setMessage('Cash operations note saved.');
  };

  const handleResolve = (id: string) => {
    cashOpsNotesService.updateStatus(id, 'RESOLVED');
    setNotes(cashOpsNotesService.getNotes());
  };

  return (
    <div className="space-y-6 p-6">
      <div className="rounded-[28px] border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Cash Operations Notes</p>
        <h2 className="mt-2 text-2xl font-bold text-slate-900">Branch cash notes and follow-up records</h2>
        <p className="mt-2 text-sm text-slate-600">Keep till, vault, reconciliation, and incident notes off the main cash operations screens.</p>
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <form onSubmit={handleCreate} className="rounded-[28px] border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center gap-2 text-slate-900">
            <NotebookPen className="h-5 w-5 text-slate-600" />
            <h3 className="text-lg font-bold">New note</h3>
          </div>
          <div className="mt-5 grid gap-4 md:grid-cols-2">
            <label className="text-sm font-medium text-slate-700">
              Branch
              <select value={draft.branchId} onChange={(event) => setDraft((current) => ({ ...current, branchId: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm">
                <option value="">Optional</option>
                {vaults.map((vault) => (
                  <option key={vault.branchId} value={vault.branchId}>{vault.branchName}</option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-slate-700">
              Teller
              <select value={draft.tellerId} onChange={(event) => setDraft((current) => ({ ...current, tellerId: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm">
                <option value="">Optional</option>
                {tills.map((till) => (
                  <option key={till.tellerId} value={till.tellerId}>{till.tellerName}</option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-slate-700">
              Category
              <select value={draft.category} onChange={(event) => setDraft((current) => ({ ...current, category: event.target.value as CashOpsNoteRecord['category'] }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm">
                <option value="TILL">Till</option>
                <option value="VAULT">Vault</option>
                <option value="RECONCILIATION">Reconciliation</option>
                <option value="INCIDENT">Incident</option>
              </select>
            </label>
            <label className="text-sm font-medium text-slate-700">
              Subject
              <input value={draft.subject} onChange={(event) => setDraft((current) => ({ ...current, subject: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm" />
            </label>
          </div>
          <label className="mt-4 block text-sm font-medium text-slate-700">
            Details
            <textarea value={draft.details} onChange={(event) => setDraft((current) => ({ ...current, details: event.target.value }))} rows={6} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm" />
          </label>
          {message && <div className="mt-4 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{message}</div>}
          <div className="mt-5 flex gap-3">
            <button type="submit" className="rounded-2xl bg-slate-950 px-4 py-3 text-sm font-semibold text-white">Save note</button>
            <button type="button" onClick={() => setDraft(defaultDraft)} className="rounded-2xl border border-slate-300 px-4 py-3 text-sm font-semibold text-slate-700">Clear</button>
          </div>
        </form>

        <div className="rounded-[28px] border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <h3 className="text-lg font-bold text-slate-900">Saved notes</h3>
            <label className="relative block w-full max-w-sm">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search notes" className="w-full rounded-2xl border border-slate-300 bg-white py-3 pl-10 pr-4 text-sm" />
            </label>
          </div>
          <div className="mt-5 space-y-3">
            {filteredNotes.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500">No cash operations notes recorded yet.</div>
            ) : filteredNotes.map((note) => (
              <div key={note.id} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="font-semibold text-slate-900">{note.subject}</p>
                      <span className={`rounded-full px-2 py-1 text-xs font-semibold ${note.status === 'RESOLVED' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>{note.status}</span>
                    </div>
                    <p className="mt-1 text-sm text-slate-600">{note.category} | {note.branchId || 'No branch'} | {note.tellerId || 'No teller'}</p>
                  </div>
                  {note.status !== 'RESOLVED' && (
                    <button type="button" onClick={() => handleResolve(note.id)} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-700">
                      <CheckCircle2 className="h-4 w-4" />
                      Mark resolved
                    </button>
                  )}
                </div>
                <p className="mt-3 text-sm text-slate-700">{note.details}</p>
                <div className="mt-3 text-xs text-slate-500">Note ID: {note.id} | Updated {new Date(note.updatedAt).toLocaleString()}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
