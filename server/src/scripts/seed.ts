
import { query, getClient } from '../db';
import { v4 as uuidv4 } from 'uuid';

const clearDb = async () => {
    await query('TRUNCATE TABLE transactions, accounts, loans, group_members, groups, customers, staff, roles, branches CASCADE');
};

const seed = async () => {
    const client = await getClient();
    try {
        await client.query('BEGIN');

        console.log('Clearing database...');
        // Order matters due to foreign keys
        await client.query('TRUNCATE TABLE transactions, accounts, loans, group_members, groups, customers, staff, roles, branches, system_config, workflows, approval_requests CASCADE');

        console.log('Seeding Branches...');
        await client.query(`
            INSERT INTO branches (id, name, code, location, status) VALUES
            ('BR001', 'Head Office', '201', 'Accra High Street', 'ACTIVE'),
            ('BR002', 'Kumasi Main', '202', 'Adum, Kumasi', 'ACTIVE')
        `);

        console.log('Seeding Roles...');
        await client.query(`
            INSERT INTO roles (id, name, description, permissions) VALUES
            ('SUPER_ADMIN', 'Super Administrator', 'Full Access', ARRAY['SYSTEM_ADMIN', 'SYSTEM_DESIGN', 'GL_CONFIG', 'GL_WRITE', 'GL_READ', 'CLIENT_READ', 'CLIENT_WRITE', 'ACCOUNT_READ', 'ACCOUNT_WRITE', 'LOAN_READ', 'LOAN_WRITE', 'LOAN_APPROVE', 'TELLER_TRANSACTION', 'REPORT_VIEW', 'AUDIT_READ', 'DATA_MIGRATION', 'APPROVAL_TASK']),
            ('TELLER', 'Teller', 'Cash Ops', ARRAY['TELLER_TRANSACTION', 'ACCOUNT_READ', 'CLIENT_READ'])
        `);

        console.log('Seeding Staff...');
        // Password is 'password123' (not hashed for prototype)
        await client.query(`
            INSERT INTO staff (id, name, email, phone, password_hash, role_id, branch_id, status, avatar_initials) VALUES
            ('STF001', 'Kwame Admin', 'admin@bankinsight.local', '0200000001', 'password123', 'SUPER_ADMIN', 'BR001', 'Active', 'KA')
        `);

        console.log('Seeding GL Accounts...');
        await client.query(`
            INSERT INTO gl_accounts (code, name, category, currency, balance, is_header) VALUES
            ('10000', 'Assets', 'ASSET', 'GHS', 0, true),
            ('11000', 'Cash and Equivalents', 'ASSET', 'GHS', 0, false),
            ('12000', 'Loan Portfolio', 'ASSET', 'GHS', 0, false),
            ('20000', 'Liabilities', 'LIABILITY', 'GHS', 0, true),
            ('21000', 'Customer Deposits', 'LIABILITY', 'GHS', 0, false),
            ('30000', 'Equity', 'EQUITY', 'GHS', 0, true),
            ('31000', 'Retained Earnings', 'EQUITY', 'GHS', 0, false),
            ('40000', 'Income', 'INCOME', 'GHS', 0, true),
            ('41000', 'Interest Income from Loans', 'INCOME', 'GHS', 0, false),
            ('50000', 'Expenses', 'EXPENSE', 'GHS', 0, true),
            ('51000', 'Operating Expenses', 'EXPENSE', 'GHS', 0, false)
        `);

        console.log('Seeding Products...');
        await client.query(`
            INSERT INTO products (id, name, description, type, currency, interest_rate, interest_method, min_amount, max_amount, min_term, max_term, default_term, status) VALUES
            ('SA-100', 'Basic Savings Account', 'Standard savings account for individuals', 'SAVINGS', 'GHS', 5.0, 'COMPOUND', 50, null, null, null, null, 'ACTIVE'),
            ('CA-200', 'Business Current Account', 'Checking account for businesses', 'CURRENT', 'GHS', 0, 'NONE', 200, null, null, null, null, 'ACTIVE'),
            ('LN-300', 'Micro Business Loan', 'Short term loan for small businesses', 'LOAN', 'GHS', 24.0, 'REDUCING_BALANCE', 1000, 50000, 3, 24, 12, 'ACTIVE')
        `);

        console.log('Seeding System Config...');
        await client.query(`
            INSERT INTO system_config (id, key, value, description) VALUES
            ('CFG0001', 'amlThreshold', '10000', 'Anti-Money Laundering Reporting Threshold'),
            ('CFG0002', 'dbProvider', '"POSTGRESQL"', 'Database Provider Engine'),
            ('CFG0003', 'auth', '{"enabled":true,"provider":"LOCAL","tenantId":"","clientId":"","ldapServer":""}', 'Authentication Configuration')
        `);

        await client.query('COMMIT');
        console.log('Seeding completed successfully.');
    } catch (e) {
        await client.query('ROLLBACK');
        console.error('Seeding failed:', e);
    } finally {
        client.release();
        process.exit();
    }
};

seed();
