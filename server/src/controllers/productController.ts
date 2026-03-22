import { Request, Response } from 'express';
import { query } from '../db';

export const getProducts = async (req: Request, res: Response) => {
    try {
        const result = await query(`
            SELECT id, name, description, type, currency, 
                   interest_rate as "interestRate", interest_method as "interestMethod",
                   min_amount as "minAmount", max_amount as "maxAmount",
                   min_term as "minTerm", max_term as "maxTerm", default_term as "defaultTerm",
                   status
            FROM products
        `);
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching products:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createProduct = async (req: Request, res: Response) => {
    const { id, name, description, type, currency, interestRate, interestMethod, minAmount, maxAmount, minTerm, maxTerm, defaultTerm, status } = req.body;
    try {
        const result = await query(
            `INSERT INTO products (id, name, description, type, currency, interest_rate, interest_method, min_amount, max_amount, min_term, max_term, default_term, status)
             VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13) RETURNING *`,
            [id, name, description, type, currency || 'GHS', interestRate, interestMethod, minAmount, maxAmount, minTerm, maxTerm, defaultTerm, status || 'ACTIVE']
        );
        res.status(201).json(result.rows[0]);
    } catch (error) {
        console.error('Error creating product:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const updateProduct = async (req: Request, res: Response) => {
    const { id } = req.params;
    const { name, description, type, currency, interestRate, interestMethod, minAmount, maxAmount, minTerm, maxTerm, defaultTerm, status } = req.body;
    try {
        const result = await query(
            `UPDATE products 
             SET name = $1, description = $2, type = $3, currency = $4, interest_rate = $5, interest_method = $6, min_amount = $7, max_amount = $8, min_term = $9, max_term = $10, default_term = $11, status = $12
             WHERE id = $13 RETURNING *`,
            [name, description, type, currency, interestRate, interestMethod, minAmount, maxAmount, minTerm, maxTerm, defaultTerm, status, id]
        );
        if (result.rows.length === 0) return res.status(404).json({ message: 'Product not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error updating product:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
