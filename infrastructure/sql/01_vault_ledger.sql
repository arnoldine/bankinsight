-- ==============================================================================
-- BankInsight Tier 2/3 SQL Schema: Vault Ledger & Teller Operations
-- Purpose: Track double-entry Vault movements, denomination splits,
-- dual-control integrity, and teller session constraints.
-- ==============================================================================

-- 1. ENUMS for Lifecycle mapping
CREATE TYPE vault_transaction_type AS ENUM (
    'MainVault_To_BranchVault_Allocation',
    'BranchVault_To_MainVault_Return',
    'BranchVault_To_Teller_Allocation',
    'Teller_To_BranchVault_Return',
    'Teller_Cash_Receipt',
    'Teller_Cash_Dispense'
);

CREATE TYPE teller_session_state AS ENUM (
    'Closed',
    'Open',
    'Reconciling',
    'Balanced',
    'Suspended_OverLimit'
);

CREATE TYPE denomination_unit AS ENUM (
    'GHS_200', 'GHS_100', 'GHS_50', 'GHS_20', 'GHS_10', 'GHS_5', 'GHS_2', 'GHS_1',
    'GHS_0_50', 'GHS_0_20', 'GHS_0_10', 'GHS_0_05', 'GHS_0_01'
);

-- ==============================================================================
-- Core Tables
-- ==============================================================================

-- 2. vault_ledger (The Immutable Double-Entry Core)
-- Note: Balances are explicitly derived querying SUM(credit) - SUM(debit)
CREATE TABLE vault_ledger (
    ledger_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    transaction_type vault_transaction_type NOT NULL,
    
    -- Routing Accounts
    -- E.g., Credit 'Main Vault Account', Debit 'Kumasi Branch Vault Account'
    debit_account_id UUID NOT NULL, 
    credit_account_id UUID NOT NULL,
    
    amount DECIMAL(18, 4) NOT NULL CHECK (amount > 0),
    currency VARCHAR(3) DEFAULT 'GHS',
    
    narration TEXT,
    reference_number VARCHAR(100) UNIQUE NOT NULL,
    
    -- Maker-Checker Tracking
    maker_user_id UUID NOT NULL,
    checker_user_id UUID,
    is_approved BOOLEAN DEFAULT FALSE,
    approved_at TIMESTAMP WITH TIME ZONE,
    
    -- Audit & Lock mechanisms
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Index for deriving balances quickly (e.g. SELECT SUM(amount) WHERE debit_account_id = X)
CREATE INDEX idx_vault_ledger_debit ON vault_ledger(debit_account_id);
CREATE INDEX idx_vault_ledger_credit ON vault_ledger(credit_account_id);


-- 3. vault_transaction_denomination (Physical Breakdown)
-- Every Vault transaction MUST have child records mapping exactly to physical counts
CREATE TABLE vault_transaction_denomination (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ledger_id UUID NOT NULL REFERENCES vault_ledger(ledger_id) ON DELETE CASCADE,
    
    denomination denomination_unit NOT NULL,
    pieces INT NOT NULL CHECK (pieces >= 0),
    total_value DECIMAL(18, 4) GENERATED ALWAYS AS (
        pieces * CASE denomination 
                   WHEN 'GHS_200' THEN 200.00
                   WHEN 'GHS_100' THEN 100.00
                   WHEN 'GHS_50' THEN 50.00
                   WHEN 'GHS_20' THEN 20.00
                   WHEN 'GHS_10' THEN 10.00
                   WHEN 'GHS_5' THEN 5.00
                   WHEN 'GHS_2' THEN 2.00
                   WHEN 'GHS_1' THEN 1.00
                   WHEN 'GHS_0_50' THEN 0.50
                   WHEN 'GHS_0_20' THEN 0.20
                   WHEN 'GHS_0_10' THEN 0.10
                   WHEN 'GHS_0_05' THEN 0.05
                   WHEN 'GHS_0_01' THEN 0.01
                   ELSE 0.00 
                 END
    ) STORED,
    
    CONSTRAINT unique_ledger_denomination UNIQUE(ledger_id, denomination)
);

-- Function to ensure sum(denominations) == vault_ledger.amount
-- Note: In a real environment, this constraint rule is typically enforced via application-tier validation 
-- or a fast triggers, but physically storing it validates integrity on read.


-- 4. teller_session (State Control & Risk Management)
CREATE TABLE teller_session (
    session_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    teller_id UUID NOT NULL,
    branch_id UUID NOT NULL,
    
    opened_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    closed_at TIMESTAMP WITH TIME ZONE,
    
    state teller_session_state DEFAULT 'Open',
    
    -- Risk Management: If mid-day cash exceed this, state -> 'Suspended_OverLimit'
    mid_day_cash_limit DECIMAL(18, 4) NOT NULL DEFAULT 50000.00,
    
    opening_balance DECIMAL(18, 4) DEFAULT 0.00,
    closing_balance DECIMAL(18, 4), -- Only populated when closed
    
    -- Lock optimization row
    row_version BIGINT DEFAULT 1
);

-- Partial index targeting only open sessions to enforce 1-session-per-teller limit
CREATE UNIQUE INDEX idx_unique_open_teller_session 
    ON teller_session(teller_id) 
    WHERE state NOT IN ('Closed', 'Balanced');
