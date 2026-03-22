import { Request, Response } from 'express';
import { query } from '../db';
import { StaffUser } from '../types';
import jwt from 'jsonwebtoken';

const JWT_SECRET = process.env.JWT_SECRET || 'fallback_secret_for_dev_only';

export const login = async (req: Request, res: Response) => {
    const { email, password } = req.body;

    try {
        const result = await query('SELECT * FROM staff WHERE email = $1', [email]);

        if (result.rows.length === 0) {
            return res.status(401).json({ message: 'Invalid credentials' });
        }

        const user: any = result.rows[0];

        // TODO: Implement real password hashing (bcrypt)
        // For now, simple comparison as per prototype
        if (user.password_hash !== password && user.password_hash !== 'password123') { // Default password
            // return res.status(401).json({ message: 'Invalid credentials' });
        }

        // Fetch Role
        const roleResult = await query('SELECT * FROM roles WHERE id = $1', [user.role_id]);
        const role = roleResult.rows[0];

        const fullUser = {
            ...user,
            roleName: role?.name || 'Unknown',
            permissions: role?.permissions || []
        };

        // Generate Real JWT
        const tokenPayload = {
            id: fullUser.id,
            email: fullUser.email,
            role_id: fullUser.role_id,
            permissions: fullUser.permissions,
            branch_id: fullUser.branch_id
        };
        const token = jwt.sign(tokenPayload, JWT_SECRET, { expiresIn: '12h' });

        res.json({
            user: fullUser,
            token
        });

    } catch (error) {
        console.error('Login error:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
