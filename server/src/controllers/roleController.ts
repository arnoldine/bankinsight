import { Request, Response } from 'express';
import { query } from '../db';

export const getRoles = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM roles');
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching roles:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createRole = async (req: Request, res: Response) => {
    const { name, description, permissions } = req.body;
    try {
        const id = `ROL${Date.now().toString().slice(-4)}`;
        const result = await query(
            `INSERT INTO roles (id, name, description, permissions) 
             VALUES ($1, $2, $3, $4) RETURNING *`,
            [id, name, description, permissions]
        );
        res.status(201).json(result.rows[0]);
    } catch (error) {
        console.error('Error creating role:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const updateRole = async (req: Request, res: Response) => {
    const { id } = req.params;
    const { name, description, permissions } = req.body;
    try {
        const result = await query(
            `UPDATE roles 
             SET name = $1, description = $2, permissions = $3
             WHERE id = $4 RETURNING *`,
            [name, description, permissions, id]
        );
        if (result.rows.length === 0) return res.status(404).json({ message: 'Role not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error updating role:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
