import React, { useMemo, useState } from 'react';
import { CheckCircle2, FileText, NotebookPen, Search } from 'lucide-react';
import { tellerNotesService, TellerNoteRecord } from '../src/services/tellerNotesService';
import { Account, Customer } from '../types';

interface TellerNotesScreenProps {
  accounts: Account[];
  customers: Customer[];
}

const defaultDraft = {
  accountId: '',
  customerId: '',
  presenterName: '',
  presenterGhanaCard: '',
  relationship: '',
  phone: '',
  subject: '',
  details: '',
  status: 'OPEN' as const,
};

export default function TellerNotesScreen({ accounts, customers }: TellerNotesScreenProps) {
  const [notes, setNotes] = useState<TellerNoteRecord[]>(() => tellerNotesService.getNotes());
  const [query, setQuery] = useState('');
  const [draft, setDraft] = useState(defaultDraft);
  const [message, setMessage] = useState<string | null>(null);

  const filteredNotes = useMemo(() => {
    const fingerprint = query.trim().toLowerCase();
    if (!fingerprint) {
      return notes;
    }

    return notes.filter((note) => (
      [
        note.id,
        note.accountId,
        note.customerId,
        note.customerName,
        note.presenterName,
        note.presenterGhanaCard,
        note.relationship,
        note.subject,
        note.details,
        note.status,
      ]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(fingerprint))
    ));
  }, [notes, query]);

  const selectedAccount = accounts.find((account) => account.id === draft.accountId) || null;
  const selectedCustomer = customers.find((customer) => customer.id === draft.customerId)
    || (selectedAccount ? customers.find((customer) => customer.id === selectedAccount.cif) || null : null);

  const handleCreate = (event: React.FormEvent) => {
    event.preventDefault();
    if (!draft.presenterName.trim() || !draft.presenterGhanaCard.trim() || !draft.subject.trim() || !draft.details.trim()) {
      setMessage('Presenter name, Ghana Card, subject, and details are required.');
      return;
    }

    tellerNotesService.createNote({
      accountId: draft.accountId || undefined,
      customerId: draft.customerId || selectedCustomer?.id,
      customerName: selectedCustomer?.name,
      presenterName: draft.presenterName.trim(),
      presenterGhanaCard: draft.presenterGhanaCard.trim().toUpperCase(),
      relationship: draft.relationship.trim() || undefined,
      phone: draft.phone.trim() || undefined,
      subject: draft.subject.trim(),
      details: draft.details.trim(),
      status: 'OPEN',
    });

    setNotes(tellerNotesService.getNotes());
    setDraft(defaultDraft);
    setMessage('Teller note saved.');
  };

  const handleResolve = (id: string) => {
    tellerNotesService.updateStatus(id, 'RESOLVED');
    setNotes(tellerNotesService.getNotes());
  };

  return (
    <div className="space-y-6 p-6">
      <div className="rounded-[28px] border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Teller Notes</p>
            <h2 className="mt-2 text-2xl font-bold text-slate-900">Operational notes and third-party verification records</h2>
            <p className="mt-2 text-sm text-slate-600">Capture teller notes outside the posting screen and link them to customer or account activity when needed.</p>
          </div>
          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
            {notes.length} note{notes.length === 1 ? '' : 's'} recorded
          </div>
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <form onSubmit={handleCreate} className="rounded-[28px] border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center gap-2 text-slate-900">
            <NotebookPen className="h-5 w-5 text-slate-600" />
            <h3 className="text-lg font-bold">New note</h3>
          </div>
          <div className="mt-5 grid gap-4 md:grid-cols-2">
            <label className="text-sm font-medium text-slate-700">
              Account
              <select value={draft.accountId} onChange={(event) => setDraft((current) => ({ ...current, accountId: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm">
                <option value="">Optional</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>{account.id}</option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-slate-700">
              Customer
              <select value={draft.customerId} onChange={(event) => setDraft((current) => ({ ...current, customerId: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm">
                <option value="">Optional</option>
                {customers.map((customer) => (
                  <option key={customer.id} value={customer.id}>{customer.name} ({customer.id})</option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-slate-700">
              Presenter name
              <input value={draft.presenterName} onChange={(event) => setDraft((current) => ({ ...current, presenterName: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm" />
            </label>
            <label className="text-sm font-medium text-slate-700">
              Ghana Card
              <input value={draft.presenterGhanaCard} onChange={(event) => setDraft((current) => ({ ...current, presenterGhanaCard: event.target.value.toUpperCase() }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-mono" />
            </label>
            <label className="text-sm font-medium text-slate-700">
              Relationship
              <input value={draft.relationship} onChange={(event) => setDraft((current) => ({ ...current, relationship: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm" />
            </label>
            <label className="text-sm font-medium text-slate-700">
              Phone
              <input value={draft.phone} onChange={(event) => setDraft((current) => ({ ...current, phone: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm" />
            </label>
          </div>
          <label className="mt-4 block text-sm font-medium text-slate-700">
            Subject
            <input value={draft.subject} onChange={(event) => setDraft((current) => ({ ...current, subject: event.target.value }))} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm" />
          </label>
          <label className="mt-4 block text-sm font-medium text-slate-700">
            Details
            <textarea value={draft.details} onChange={(event) => setDraft((current) => ({ ...current, details: event.target.value }))} rows={5} className="mt-2 w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm" />
          </label>

          {message && (
            <div className="mt-4 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              {message}
            </div>
          )}

          <div className="mt-5 flex gap-3">
            <button type="submit" className="rounded-2xl bg-slate-950 px-4 py-3 text-sm font-semibold text-white">Save note</button>
            <button type="button" onClick={() => setDraft(defaultDraft)} className="rounded-2xl border border-slate-300 px-4 py-3 text-sm font-semibold text-slate-700">Clear</button>
          </div>
        </form>

        <div className="rounded-[28px] border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex items-center gap-2 text-slate-900">
              <FileText className="h-5 w-5 text-slate-600" />
              <h3 className="text-lg font-bold">Saved notes</h3>
            </div>
            <label className="relative block w-full max-w-sm">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search notes" className="w-full rounded-2xl border border-slate-300 bg-white py-3 pl-10 pr-4 text-sm" />
            </label>
          </div>

          <div className="mt-5 space-y-3">
            {filteredNotes.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500">
                No teller notes have been recorded yet.
              </div>
            ) : filteredNotes.map((note) => (
              <div key={note.id} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="font-semibold text-slate-900">{note.subject}</p>
                      <span className={`rounded-full px-2 py-1 text-xs font-semibold ${note.status === 'RESOLVED' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>{note.status}</span>
                    </div>
                    <p className="mt-1 text-sm text-slate-600">{note.presenterName} | {note.presenterGhanaCard}</p>
                    <p className="mt-1 text-sm text-slate-600">{note.accountId || 'No account'} | {note.customerName || note.customerId || 'No customer'}</p>
                  </div>
                  {note.status !== 'RESOLVED' && (
                    <button type="button" onClick={() => handleResolve(note.id)} className="inline-flex items-center gap-2 rounded-2xl border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-700">
                      <CheckCircle2 className="h-4 w-4" />
                      Mark resolved
                    </button>
                  )}
                </div>
                <p className="mt-3 text-sm text-slate-700">{note.details}</p>
                <div className="mt-3 text-xs text-slate-500">
                  Note ID: {note.id} | Updated {new Date(note.updatedAt).toLocaleString()}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
