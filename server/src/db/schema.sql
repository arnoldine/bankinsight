-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- BRANCHES
CREATE TABLE IF NOT EXISTS branches (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(20) UNIQUE NOT NULL,
    location VARCHAR(255),
    status VARCHAR(20) DEFAULT 'ACTIVE' -- ACTIVE, CLOSED
);

-- ROLES
CREATE TABLE IF NOT EXISTS roles (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    permissions TEXT[] -- Array of permission strings
);

-- STAFF
CREATE TABLE IF NOT EXISTS staff (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    phone VARCHAR(20),
    password_hash VARCHAR(255),
    role_id VARCHAR(50) REFERENCES roles(id),
    branch_id VARCHAR(50) REFERENCES branches(id),
    status VARCHAR(20) DEFAULT 'Active',
    last_login TIMESTAMP,
    avatar_initials VARCHAR(5)
);

-- CUSTOMERS (Unified Table for Individual & Corporate)
CREATE TABLE IF NOT EXISTS customers (
    id VARCHAR(50) PRIMARY KEY, -- CIF
    type VARCHAR(20) NOT NULL, -- INDIVIDUAL, CORPORATE
    name VARCHAR(255) NOT NULL,
    email VARCHAR(100),
    phone VARCHAR(20),
    secondary_phone VARCHAR(20),
    digital_address VARCHAR(100),
    postal_address TEXT,
    kyc_level VARCHAR(20) DEFAULT 'Tier 1',
    risk_rating VARCHAR(20) DEFAULT 'Low',
    
    -- Individual Specific
    gender VARCHAR(10),
    date_of_birth DATE,
    ghana_card VARCHAR(50),
    nationality VARCHAR(50),
    marital_status VARCHAR(20),
    spouse_name VARCHAR(100),
    employer VARCHAR(100),
    job_title VARCHAR(100),
    ssnit_no VARCHAR(50),

    -- Corporate Specific
    business_reg_no VARCHAR(50),
    registration_date DATE,
    tin VARCHAR(50),
    sector VARCHAR(50),
    legal_form VARCHAR(50),

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- GROUPS
CREATE TABLE IF NOT EXISTS groups (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    officer_id VARCHAR(50) REFERENCES staff(id),
    meeting_day VARCHAR(20),
    formation_date DATE,
    status VARCHAR(20) DEFAULT 'ACTIVE'
);

-- GROUP MEMBERS (Many-to-Many)
CREATE TABLE IF NOT EXISTS group_members (
    group_id VARCHAR(50) REFERENCES groups(id),
    customer_id VARCHAR(50) REFERENCES customers(id),
    PRIMARY KEY (group_id, customer_id)
);

-- PRODUCTS
CREATE TABLE IF NOT EXISTS products (
    id VARCHAR(50) PRIMARY KEY, -- Product Code
    name VARCHAR(100) NOT NULL,
    description TEXT,
    type VARCHAR(20) NOT NULL, -- SAVINGS, CURRENT, LOAN, FIXED_DEPOSIT
    currency VARCHAR(10) DEFAULT 'GHS',
    interest_rate DECIMAL(10, 4),
    interest_method VARCHAR(50),
    min_amount DECIMAL(15, 2),
    max_amount DECIMAL(15, 2),
    min_term INTEGER,
    max_term INTEGER,
    default_term INTEGER,
    status VARCHAR(20) DEFAULT 'ACTIVE'
);

-- ACCOUNTS
CREATE TABLE IF NOT EXISTS accounts (
    id VARCHAR(50) PRIMARY KEY, -- Account Number
    customer_id VARCHAR(50) REFERENCES customers(id),
    branch_id VARCHAR(50) REFERENCES branches(id),
    product_code VARCHAR(50) REFERENCES products(id),
    type VARCHAR(20) NOT NULL,
    currency VARCHAR(10) DEFAULT 'GHS',
    balance DECIMAL(15, 2) DEFAULT 0,
    lien_amount DECIMAL(15, 2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'ACTIVE',
    last_trans_date TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- LOANS
CREATE TABLE IF NOT EXISTS loans (
    id VARCHAR(50) PRIMARY KEY,
    customer_id VARCHAR(50) REFERENCES customers(id),
    group_id VARCHAR(50) REFERENCES groups(id),
    product_code VARCHAR(50) REFERENCES products(id),
    principal DECIMAL(15, 2) NOT NULL,
    rate DECIMAL(10, 4) NOT NULL,
    term_months INTEGER NOT NULL,
    disbursement_date DATE,
    status VARCHAR(20) DEFAULT 'PENDING',
    outstanding_balance DECIMAL(15, 2),
    collateral_type VARCHAR(50),
    collateral_value DECIMAL(15, 2),
    par_bucket VARCHAR(20) DEFAULT '0'
);

-- LOAN SCHEDULE
CREATE TABLE IF NOT EXISTS loan_schedules (
    id SERIAL PRIMARY KEY,
    loan_id VARCHAR(50) REFERENCES loans(id),
    period INTEGER,
    due_date DATE,
    principal DECIMAL(15, 2),
    interest DECIMAL(15, 2),
    total DECIMAL(15, 2),
    balance DECIMAL(15, 2),
    status VARCHAR(20) -- PAID, DUE, OVERDUE
);

-- TRANSACTIONS
CREATE TABLE IF NOT EXISTS transactions (
    id VARCHAR(50) PRIMARY KEY,
    account_id VARCHAR(50) REFERENCES accounts(id),
    type VARCHAR(30) NOT NULL, -- DEPOSIT, WITHDRAWAL
    amount DECIMAL(15, 2) NOT NULL,
    narration VARCHAR(255),
    date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reference VARCHAR(100),
    teller_id VARCHAR(50) REFERENCES staff(id),
    status VARCHAR(20) DEFAULT 'POSTED'
);

-- GL ACCOUNTS
CREATE TABLE IF NOT EXISTS gl_accounts (
    code VARCHAR(20) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    category VARCHAR(20) NOT NULL, -- ASSET, LIABILITY, etc.
    currency VARCHAR(10) DEFAULT 'GHS',
    balance DECIMAL(15, 2) DEFAULT 0,
    is_header BOOLEAN DEFAULT FALSE
);

-- JOURNAL ENTRIES
CREATE TABLE IF NOT EXISTS journal_entries (
    id VARCHAR(50) PRIMARY KEY,
    date DATE,
    reference VARCHAR(100),
    description TEXT,
    posted_by VARCHAR(50) REFERENCES staff(id),
    status VARCHAR(20) DEFAULT 'POSTED',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- JOURNAL CONLINES
CREATE TABLE IF NOT EXISTS journal_lines (
    id SERIAL PRIMARY KEY,
    journal_id VARCHAR(50) REFERENCES journal_entries(id),
    account_code VARCHAR(20) REFERENCES gl_accounts(code),
    debit DECIMAL(15, 2) DEFAULT 0,
    credit DECIMAL(15, 2) DEFAULT 0
);

-- SYSTEM CONFIGURATION
CREATE TABLE IF NOT EXISTS system_config (
    id VARCHAR(50) PRIMARY KEY,
    key VARCHAR(100) UNIQUE NOT NULL,
    value TEXT NOT NULL,
    description TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- WORKFLOWS
CREATE TABLE IF NOT EXISTS workflows (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    trigger_type VARCHAR(50) NOT NULL, -- e.g., 'LOAN_APPROVAL', 'HIGH_VALUE_TXN'
    steps JSONB NOT NULL,
    status VARCHAR(20) DEFAULT 'ACTIVE',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- APPROVAL REQUESTS
CREATE TABLE IF NOT EXISTS approval_requests (
    id VARCHAR(50) PRIMARY KEY,
    workflow_id VARCHAR(50) REFERENCES workflows(id),
    entity_type VARCHAR(50) NOT NULL, -- e.g., 'LOAN', 'TRANSACTION'
    entity_id VARCHAR(50) NOT NULL,
    requester_id VARCHAR(50) REFERENCES staff(id),
    status VARCHAR(20) DEFAULT 'PENDING', -- PENDING, APPROVED, REJECTED
    current_step INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
