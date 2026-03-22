export interface TellerNoteRecord {
  id: string;
  createdAt: string;
  updatedAt: string;
  accountId?: string;
  customerId?: string;
  customerName?: string;
  presenterName: string;
  presenterGhanaCard: string;
  relationship?: string;
  phone?: string;
  subject: string;
  details: string;
  status: 'OPEN' | 'RESOLVED';
}

const STORAGE_KEY = 'bankinsight_teller_notes';

function loadNotes(): TellerNoteRecord[] {
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

function persistNotes(notes: TellerNoteRecord[]) {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(notes));
}

export const tellerNotesService = {
  getNotes(): TellerNoteRecord[] {
    return loadNotes().sort((left, right) => right.updatedAt.localeCompare(left.updatedAt));
  },

  getNotesForContext(accountId?: string, customerId?: string): TellerNoteRecord[] {
    return this.getNotes().filter((note) => {
      const matchesAccount = accountId && note.accountId === accountId;
      const matchesCustomer = customerId && note.customerId === customerId;
      return Boolean(matchesAccount || matchesCustomer);
    });
  },

  createNote(input: Omit<TellerNoteRecord, 'id' | 'createdAt' | 'updatedAt'>): TellerNoteRecord {
    const now = new Date().toISOString();
    const record: TellerNoteRecord = {
      ...input,
      id: `TN-${Date.now()}`,
      createdAt: now,
      updatedAt: now,
    };

    const notes = [record, ...loadNotes()];
    persistNotes(notes);
    return record;
  },

  updateStatus(id: string, status: TellerNoteRecord['status']): TellerNoteRecord | null {
    const notes = loadNotes();
    const next = notes.map((note) => note.id === id ? { ...note, status, updatedAt: new Date().toISOString() } : note);
    persistNotes(next);
    return next.find((note) => note.id === id) || null;
  },
};
