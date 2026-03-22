import { Request, Response } from 'express';
import { query, getClient } from '../db';
import { v4 as uuidv4 } from 'uuid';

export const getGroups = async (req: Request, res: Response) => {
    try {
        const result = await query(`
            SELECT g.id, g.name, g.officer_id as officer, g.meeting_day as "meetingDay", 
                   g.formation_date as "formationDate", g.status,
                   COALESCE(array_agg(gm.customer_id) FILTER (WHERE gm.customer_id IS NOT NULL), '{}') as members
            FROM groups g
            LEFT JOIN group_members gm ON g.id = gm.group_id
            GROUP BY g.id
        `);
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching groups:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createGroup = async (req: Request, res: Response) => {
    const { name, officer, meetingDay, formationDate, status } = req.body;
    try {
        const groupId = `GRP${Date.now().toString().slice(-4)}`;
        const result = await query(
            `INSERT INTO groups (id, name, officer_id, meeting_day, formation_date, status)
             VALUES ($1, $2, $3, $4, $5, $6) RETURNING *`,
            [groupId, name, officer, meetingDay, formationDate || new Date().toISOString().split('T')[0], status || 'ACTIVE']
        );
        res.status(201).json({ ...result.rows[0], members: [] });
    } catch (error) {
        console.error('Error creating group:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const addMember = async (req: Request, res: Response) => {
    const { id } = req.params;
    const { customerId } = req.body;
    try {
        await query(
            `INSERT INTO group_members (group_id, customer_id) VALUES ($1, $2)`,
            [id, customerId]
        );
        res.json({ message: 'Member added successfully' });
    } catch (error: any) {
        console.error('Error adding member:', error);
        if (error.code === '23505') { // Unique violation
            return res.status(400).json({ message: 'Member already exists in this group' });
        }
        res.status(500).json({ message: 'Server error' });
    }
};

export const removeMember = async (req: Request, res: Response) => {
    const { id, customerId } = req.params;
    try {
        await query(
            `DELETE FROM group_members WHERE group_id = $1 AND customer_id = $2`,
            [id, customerId]
        );
        res.json({ message: 'Member removed successfully' });
    } catch (error) {
        console.error('Error removing member:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
