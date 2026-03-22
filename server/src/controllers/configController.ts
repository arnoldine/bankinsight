import { Request, Response } from 'express';
import { query } from '../db';

export const getConfig = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT key, value FROM system_config');
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching config:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const updateConfig = async (req: Request, res: Response) => {
    const configItems = req.body;
    // Expects array of { key: string, value: string }

    try {
        const client = await require('../db').getClient();
        await client.query('BEGIN');

        for (const item of configItems) {
            await client.query(
                `INSERT INTO system_config (id, key, value) 
                 VALUES ($1, $2, $3)
                 ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value`,
                [`CFG${Date.now().toString().slice(-4)}${Math.floor(Math.random() * 100)}`, item.key, item.value]
            );
        }

        await client.query('COMMIT');
        client.release();
        res.json({ message: 'Configuration updated successfully' });
    } catch (error) {
        console.error('Error updating config:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
