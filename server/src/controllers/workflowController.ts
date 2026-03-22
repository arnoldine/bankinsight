import { Request, Response } from 'express';
import { query } from '../db';

export const getWorkflows = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM workflows');
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching workflows:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createWorkflow = async (req: Request, res: Response) => {
    const { name, trigger_type, steps, status } = req.body;
    try {
        const id = `WF${Date.now().toString().slice(-4)}`;
        const result = await query(
            `INSERT INTO workflows (id, name, trigger_type, steps, status) 
             VALUES ($1, $2, $3, $4, $5) RETURNING *`,
            [id, name, trigger_type, JSON.stringify(steps), status || 'ACTIVE']
        );
        res.status(201).json(result.rows[0]);
    } catch (error) {
        console.error('Error creating workflow:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const updateWorkflow = async (req: Request, res: Response) => {
    const { id } = req.params;
    const { name, trigger_type, steps, status } = req.body;
    try {
        const result = await query(
            `UPDATE workflows 
             SET name = $1, trigger_type = $2, steps = $3, status = $4
             WHERE id = $5 RETURNING *`,
            [name, trigger_type, JSON.stringify(steps), status, id]
        );
        if (result.rows.length === 0) return res.status(404).json({ message: 'Workflow not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error updating workflow:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
