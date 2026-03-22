export interface CashOpsNoteRecord {
  id: string;
  createdAt: string;
  updatedAt: string;
  branchId?: string;
  tellerId?: string;
  subject: string;
  details: string;
  category: 'TILL' | 'VAULT' | 'RECONCILIATION' | 'INCIDENT';
  status: 'OPEN' | 'RESOLVED';
}

const STORAGE_KEY = 'bankinsight_cash_ops_notes';

function loadNotes(): CashOpsNoteRecord[] {
  if (typeof window === 'undefined') {
    return [];
  }

  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return [];
    }

    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function persistNotes(notes: CashOpsNoteRecord[]) {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(notes));
}

export const cashOpsNotesService = {
  getNotes(): CashOpsNoteRecord[] {
    return loadNotes().sort((left, right) => right.updatedAt.localeCompare(left.updatedAt));
  },

  createNote(input: Omit<CashOpsNoteRecord, 'id' | 'createdAt' | 'updatedAt'>): CashOpsNoteRecord {
    const now = new Date().toISOString();
    const record: CashOpsNoteRecord = {
      ...input,
      id: `CN-${Date.now()}`,
      createdAt: now,
      updatedAt: now,
    };

    const notes = [record, ...loadNotes()];
    persistNotes(notes);
    return record;
  },

  updateStatus(id: string, status: CashOpsNoteRecord['status']): CashOpsNoteRecord | null {
    const notes = loadNotes();
    const next = notes.map((note) => note.id === id ? { ...note, status, updatedAt: new Date().toISOString() } : note);
    persistNotes(next);
    return next.find((note) => note.id === id) || null;
  },
};
