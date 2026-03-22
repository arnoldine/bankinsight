import { Request, Response } from 'express';
import { query } from '../db';

export const getAccounts = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM accounts');
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching accounts:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const getAccountById = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM accounts WHERE id = $1', [req.params.id]);
        if (result.rows.length === 0) return res.status(404).json({ message: 'Account not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error fetching account:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const getAccountsByCustomerId = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM accounts WHERE customer_id = $1', [req.params.cif]);
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching customer accounts:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createAccount = async (req: Request, res: Response) => {
    const { customer_id, branch_id, type, currency, product_code } = req.body;
    try {
        // Generating a dummy 10-digit account number starting with branch code
        const branchCode = '201'; // Defaulting to Main Branch
        const id = `${branchCode}${Math.floor(1000000 + Math.random() * 9000000).toString().slice(0, 6)}01`;

        const result = await query(
            `INSERT INTO accounts (id, customer_id, branch_id, type, currency, balance, lien_amount, status, product_code, last_trans_date) 
             VALUES ($1, $2, $3, $4, $5, 0, 0, 'ACTIVE', $6, CURRENT_DATE) RETURNING *`,
            [id, customer_id, branch_id || 'BR001', type, currency || 'GHS', product_code]
        );
        res.status(201).json(result.rows[0]);
    } catch (error) {
        console.error('Error opening account:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
