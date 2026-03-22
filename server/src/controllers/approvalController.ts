import { Request, Response } from 'express';
import { query } from '../db';

export const getApprovals = async (req: Request, res: Response) => {
    try {
        const result = await query(`
            SELECT a.*, w.name as workflow_name 
            FROM approval_requests a
            LEFT JOIN workflows w ON a.workflow_id = w.id
            ORDER BY a.created_at DESC
        `);
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching approvals:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createApproval = async (req: Request, res: Response) => {
    const { workflow_id, entity_type, entity_id, requester_id } = req.body;
    try {
        const id = `APP${Date.now().toString().slice(-4)}`;
        const result = await query(
            `INSERT INTO approval_requests (id, workflow_id, entity_type, entity_id, requester_id, status) 
             VALUES ($1, $2, $3, $4, $5, 'PENDING') RETURNING *`,
            [id, workflow_id, entity_type, entity_id, requester_id]
        );
        res.status(201).json(result.rows[0]);
    } catch (error) {
        console.error('Error creating approval:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const updateApproval = async (req: Request, res: Response) => {
    const { id } = req.params;
    const { status, current_step } = req.body;
    try {
        const result = await query(
            `UPDATE approval_requests 
             SET status = $1, current_step = $2, updated_at = CURRENT_TIMESTAMP
             WHERE id = $3 RETURNING *`,
            [status, current_step, id]
        );
        if (result.rows.length === 0) return res.status(404).json({ message: 'Approval request not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error updating approval:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
