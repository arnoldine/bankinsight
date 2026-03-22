import { Request, Response } from 'express';
import { query, getClient } from '../db';
import { v4 as uuidv4 } from 'uuid';

export const getGlAccounts = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT code, name, category, currency, balance, is_header as "isHeader" FROM gl_accounts ORDER BY code ASC');
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching GL accounts:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createGlAccount = async (req: Request, res: Response) => {
    const { code, name, category, currency, isHeader } = req.body;
    try {
        const result = await query(
            `INSERT INTO gl_accounts (code, name, category, currency, balance, is_header) 
             VALUES ($1, $2, $3, $4, 0, $5) RETURNING *`,
            [code, name, category, currency || 'GHS', isHeader || false]
        );
        res.status(201).json(result.rows[0]);
    } catch (error) {
        console.error('Error creating GL account:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const getJournalEntries = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM journal_entries ORDER BY created_at DESC LIMIT 100');
        // Note: For full functionality, we should also fetch journal_lines, but keeping simple for now.
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching journal entries:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const postJournalEntry = async (req: Request, res: Response) => {
    const { reference, description, lines, postedBy } = req.body;
    const client = await getClient();
    try {
        await client.query('BEGIN');

        // Verify balance
        const totalDebit = lines.reduce((sum: number, line: any) => sum + Number(line.debit || 0), 0);
        const totalCredit = lines.reduce((sum: number, line: any) => sum + Number(line.credit || 0), 0);

        if (totalDebit !== totalCredit) {
            throw new Error('Debits and Credits must balance.');
        }

        const journalId = `JRN-${Date.now()}`;

        // Insert Journal Entry
        const jrnResult = await client.query(
            `INSERT INTO journal_entries (id, date, reference, description, posted_by, status)
             VALUES ($1, CURRENT_DATE, $2, $3, $4, 'POSTED') RETURNING *`,
            [journalId, reference, description, postedBy]
        );

        // Insert Lines and Update GL Balances
        for (const line of lines) {
            await client.query(
                `INSERT INTO journal_lines (journal_id, account_code, debit, credit)
                 VALUES ($1, $2, $3, $4)`,
                [journalId, line.accountCode, line.debit || 0, line.credit || 0]
            );

            // Update GL Balance based on normal balances (Asset/Expense = Debit increases, Liability/Equity/Income = Credit increases)
            // For simplicity in this demo, we'll just apply raw adjustments (requires actual category check in full app)
            // Assuming simplified model: Balance = Debit - Credit for ASSETS, Credit - Debit for LIABILITIES.
            // But we actually need to know the category to update correctly.
            // As a fallback, we fetch category first:
            const glResult = await client.query('SELECT category FROM gl_accounts WHERE code = $1', [line.accountCode]);
            if (glResult.rows.length > 0) {
                const category = glResult.rows[0].category;
                let balanceChange = 0;
                if (category === 'ASSET' || category === 'EXPENSE') {
                    balanceChange = Number(line.debit || 0) - Number(line.credit || 0);
                } else {
                    balanceChange = Number(line.credit || 0) - Number(line.debit || 0);
                }
                await client.query('UPDATE gl_accounts SET balance = balance + $1 WHERE code = $2', [balanceChange, line.accountCode]);
            }
        }

        await client.query('COMMIT');
        res.status(201).json(jrnResult.rows[0]);
    } catch (error: any) {
        await client.query('ROLLBACK');
        console.error('Error posting journal:', error);
        res.status(400).json({ message: error.message || 'Server error' });
    } finally {
        client.release();
    }
};
