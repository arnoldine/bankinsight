import { httpClient } from './httpClient';
import { API_ENDPOINTS } from './apiConfig';

export interface GlAccount {
    code: string;
    name: string;
    category: string;
    currency?: string;
    balance: number;
    isHeader: boolean;
}

export interface CreateGlAccountRequest {
    code: string;
    name: string;
    category: string;
    currency?: string;
    isHeader: boolean;
}

export interface JournalLine {
    id?: number;
    journalId?: string;
    accountCode: string;
    accountName?: string;
    debit: number;
    credit: number;
}

export interface JournalEntry {
    id: string;
    date: string;
    reference?: string;
    description?: string;
    postedBy?: string;
    status: string;
    createdAt: string;
    totalDebit?: number;
    totalCredit?: number;
    lines: JournalLine[];
}

export interface PostJournalEntryRequest {
    reference?: string;
    description?: string;
    postedBy?: string;
    lines: JournalLine[];
}

class GlService {
    async getAccounts(): Promise<GlAccount[]> {
        return httpClient.get<GlAccount[]>(API_ENDPOINTS.gl.accounts);
    }

    async createAccount(data: CreateGlAccountRequest): Promise<GlAccount> {
        return httpClient.post<GlAccount>(API_ENDPOINTS.gl.accounts, data);
    }

    async getJournalEntries(): Promise<JournalEntry[]> {
        return httpClient.get<JournalEntry[]>(API_ENDPOINTS.gl.journalEntries);
    }

    async postJournalEntry(data: PostJournalEntryRequest): Promise<JournalEntry> {
        // Local validation to ensure Debits = Credits
        const totalDebit = data.lines.reduce((sum, line) => sum + (line.debit || 0), 0);
        const totalCredit = data.lines.reduce((sum, line) => sum + (line.credit || 0), 0);

        // We use a small epsilon for floating point comparison just in case, or exact since we are dealing with currency
        if (Math.abs(totalDebit - totalCredit) > 0.001) {
            throw new Error(`Debits (${totalDebit}) and Credits (${totalCredit}) must balance.`);
        }

        return httpClient.post<JournalEntry>(API_ENDPOINTS.gl.journalEntries, data);
    }
}

export const glService = new GlService();
