import { Request, Response } from 'express';
import { query, getClient } from '../db';

export const getLoans = async (req: Request, res: Response) => {
    try {
        const result = await query(`
            SELECT l.id, l.customer_id as cif, l.group_id as "groupId", 
                   l.product_code as "productCode", p.name as "productName",
                   l.principal, l.rate, l.term_months as "termMonths", 
                   l.disbursement_date as "disbursementDate", l.par_bucket as "parBucket", 
                   l.outstanding_balance as "outstandingBalance", 
                   l.collateral_type as "collateralType", l.status
            FROM loans l
            LEFT JOIN products p ON l.product_code = p.id
        `);
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching loans:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const disburseLoan = async (req: Request, res: Response) => {
    const { cif, groupId, productCode, principal, rate, termMonths, collateralType } = req.body;
    const client = await getClient();
    try {
        await client.query('BEGIN');

        const loanId = `LN${Date.now()}`;

        // 1. Insert Loan
        const loanResult = await client.query(
            `INSERT INTO loans (id, customer_id, group_id, product_code, principal, rate, term_months, disbursement_date, status, outstanding_balance, collateral_type, par_bucket)
             VALUES ($1, $2, $3, $4, $5, $6, $7, CURRENT_DATE, 'ACTIVE', $8, $9, '0') RETURNING *`,
            [loanId, cif, groupId || null, productCode, principal, rate, termMonths, principal, collateralType]
        );

        // 2. Fetch Customer's existing main account to credit it (or create logic depending on biz rules)
        // Note: For simplicity, we credit the first account owned by the CIF, or throw error if none
        const accResult = await client.query('SELECT id FROM accounts WHERE customer_id = $1 LIMIT 1', [cif]);
        if (accResult.rows.length === 0) {
            throw new Error('Customer has no account to disburse into.');
        }
        const accountId = accResult.rows[0].id;

        // 3. Create Disbursement Transaction
        const txnId = `TXN${Date.now()}`;
        await client.query(
            `INSERT INTO transactions (id, account_id, type, amount, narration, teller_id, status, reference) 
             VALUES ($1, $2, 'DEPOSIT', $3, $4, 'SYSTEM', 'POSTED', $5)`,
            [txnId, accountId, principal, `Loan Disbursement - ${loanId}`, `DSB-${loanId}`]
        );

        // 4. Update Account Balance
        await client.query(
            `UPDATE accounts SET balance = balance + $1, last_trans_date = CURRENT_DATE WHERE id = $2`,
            [principal, accountId]
        );

        // 5. Create basic Loan Schedule
        const monthlyPrincipal = Number(principal) / Number(termMonths);
        const monthlyInterest = (Number(principal) * (Number(rate) / 100)) / 12; // simplified straight line
        let currentBalance = Number(principal);

        const scheduleQueries = [];
        for (let i = 1; i <= termMonths; i++) {
            currentBalance -= monthlyPrincipal;
            scheduleQueries.push(
                client.query(
                    `INSERT INTO loan_schedules (loan_id, period, due_date, principal, interest, total, balance, status)
                     VALUES ($1, $2, CURRENT_DATE + interval '${i} month', $3, $4, $5, $6, 'DUE')`,
                    [loanId, i, monthlyPrincipal, monthlyInterest, monthlyPrincipal + monthlyInterest, Math.max(0, currentBalance)]
                )
            );
        }
        await Promise.all(scheduleQueries);

        await client.query('COMMIT');

        // Fetch the inserted loan with product name for response
        const newLoanResult = await query(`
            SELECT l.id, l.customer_id as cif, l.group_id as "groupId", 
                   l.product_code as "productCode", p.name as "productName",
                   l.principal, l.rate, l.term_months as "termMonths", 
                   l.disbursement_date as "disbursementDate", l.par_bucket as "parBucket", 
                   l.outstanding_balance as "outstandingBalance", 
                   l.collateral_type as "collateralType", l.status
            FROM loans l
            LEFT JOIN products p ON l.product_code = p.id
            WHERE l.id = $1
        `, [loanId]);

        res.status(201).json(newLoanResult.rows[0]);
    } catch (error: any) {
        await client.query('ROLLBACK');
        console.error('Error disbursing loan:', error);
        res.status(400).json({ message: error.message || 'Server error' });
    } finally {
        client.release();
    }
};
