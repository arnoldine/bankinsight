import { Request, Response } from 'express';
import { getClient, query } from '../db';

export const getTransactions = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM transactions ORDER BY date DESC LIMIT 100');
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching transactions:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const getTransactionById = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM transactions WHERE id = $1', [req.params.id]);
        if (result.rows.length === 0) return res.status(404).json({ message: 'Transaction not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error fetching transaction:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const postTransaction = async (req: Request, res: Response) => {
    const { account_id, type, amount, narration, teller_id } = req.body;
    const client = await getClient();
    try {
        await client.query('BEGIN');

        // Check Account Balance for Withdrawal
        if (type === 'WITHDRAWAL') {
            const accResult = await client.query('SELECT balance FROM accounts WHERE id = $1 FOR UPDATE', [account_id]);
            if (accResult.rows.length === 0) throw new Error('Account Not Found');
            if (Number(accResult.rows[0].balance) < Number(amount)) {
                await client.query('ROLLBACK');
                return res.status(400).json({ message: 'Insufficient Funds' });
            }
        }

        // Generate IDs
        const txnId = `TXN${Date.now()}`;
        const ref = `REF-${Math.floor(Math.random() * 10000)}`;

        // Insert Transaction
        const txnResult = await client.query(
            `INSERT INTO transactions (id, account_id, type, amount, narration, teller_id, status, reference) 
             VALUES ($1, $2, $3, $4, $5, $6, 'POSTED', $7) RETURNING *`,
            [txnId, account_id, type, amount, narration, teller_id, ref]
        );

        // Update Account Balance
        const balanceOp = type === 'WITHDRAWAL' ? '-' : '+';
        await client.query(
            `UPDATE accounts SET balance = balance ${balanceOp} $1, last_trans_date = CURRENT_DATE WHERE id = $2`,
            [amount, account_id]
        );

        await client.query('COMMIT');
        res.status(201).json(txnResult.rows[0]);
    } catch (error: any) {
        await client.query('ROLLBACK');
        console.error('Error posting transaction:', error);
        res.status(500).json({ message: error.message || 'Server error' });
    } finally {
        client.release();
    }
};
