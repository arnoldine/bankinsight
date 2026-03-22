import { Request, Response } from 'express';
import { query } from '../db';

export const getCustomers = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM customers');
        res.json(result.rows);
    } catch (error) {
        console.error('Error fetching customers:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const getCustomerById = async (req: Request, res: Response) => {
    try {
        const result = await query('SELECT * FROM customers WHERE id = $1', [req.params.id]);
        if (result.rows.length === 0) return res.status(404).json({ message: 'Customer not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error fetching customer:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const createCustomer = async (req: Request, res: Response) => {
    const { name, ghana_card, digital_address, kyc_level, phone, email, risk_rating } = req.body;
    try {
        const result = await query(
            `INSERT INTO customers (id, name, ghana_card, digital_address, kyc_level, phone, email, risk_rating) 
             VALUES ($1, $2, $3, $4, $5, $6, $7, $8) RETURNING *`,
            [`CIF${Date.now().toString().slice(-6)}`, name, ghana_card, digital_address, kyc_level, phone, email, risk_rating]
        );
        res.status(201).json(result.rows[0]);
    } catch (error) {
        console.error('Error creating customer:', error);
        res.status(500).json({ message: 'Server error' });
    }
};

export const updateCustomer = async (req: Request, res: Response) => {
    const { id } = req.params;
    const { name, digital_address, phone, email, risk_rating } = req.body;
    try {
        const result = await query(
            `UPDATE customers 
             SET name = $1, digital_address = $2, phone = $3, email = $4, risk_rating = $5
             WHERE id = $6 RETURNING *`,
            [name, digital_address, phone, email, risk_rating, id]
        );
        if (result.rows.length === 0) return res.status(404).json({ message: 'Customer not found' });
        res.json(result.rows[0]);
    } catch (error) {
        console.error('Error updating customer:', error);
        res.status(500).json({ message: 'Server error' });
    }
};
