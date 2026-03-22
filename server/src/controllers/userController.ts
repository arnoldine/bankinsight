import { Request, Response } from 'express';
import { query } from '../db';
import { StaffUser } from '../types';

export const getUsers = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM staff');
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching users:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const getUserById = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM staff WHERE id = $1', [req.params.id]);
        if (result.rows.length === 0) return res.status(404).json({ message: 'User not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error fetching user:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createUser = async (req: Request, res: Response) => {
    const { name, email, phone, role_id, branch_id, avatar_initials, password } = req.body;
    try {
        const id = `STF${Date.now().toString().slice(-4)}`;
        const result = await query(
            `INSERT INTO staff (id, name, email, phone, password_hash, role_id, branch_id, status, avatar_initials) 
             VALUES ($1, $2, $3, $4, $5, $6, $7, 'Active', $8) RETURNING *`,
            [id, name, email, phone, password, role_id, branch_id, avatar_initials]
        );
        res.status(201).json(result.rows[0]);
    } catch (error) {
        console.error('Error creating user:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const updateUser = async (req: Request, res: Response) => {
    const { id } = req.params;
    const { name, email, phone, role_id, branch_id, status, password } = req.body;
    try {
        const result = await query(
            `UPDATE staff 
             SET name = COALESCE($1, name), 
                 email = COALESCE($2, email), 
                 phone = COALESCE($3, phone), 
                 role_id = COALESCE($4, role_id), 
                 branch_id = COALESCE($5, branch_id), 
                 status = COALESCE($6, status),
                 password_hash = COALESCE($7, password_hash)
             WHERE id = $8 RETURNING *`,
            [name ?? null, email ?? null, phone ?? null, role_id ?? null, branch_id ?? null, status ?? null, password ?? null, id]
        );
        if (result.rows.length === 0) return res.status(404).json({ message: 'User not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error updating user:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const deleteUser = async (req: Request, res: Response) => {
    try {
        const result = await query('DELETE FROM staff WHERE id = $1 RETURNING *', [req.params.id]);
        if (result.rows.length === 0) return res.status(404).json({ message: 'User not found' });
        res.json({ message: 'User deleted' });
    } catch (error) {
        console.error('Error deleting user:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
