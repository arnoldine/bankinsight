-- ==============================================================================
-- BankInsight Tier 2/3 SQL Schema: Loan Behavior & Classification
-- Purpose: Track granular repayment breakdown, Payment sources, DPD, BoG aging.
-- ==============================================================================

-- 1. ENUMS
CREATE TYPE payment_source_type AS ENUM (
    'Cash',
    'MoMo',      -- Mobile Money
    'ACH',       -- Automated Clearing House
    'Internal',  -- Direct Account Transfer
    'Cheque'
);

CREATE TYPE bog_classification_tier AS ENUM (
    'Current',       -- DPD 0
    'Oversight',     -- DPD 1-30
    'Substandard',   -- DPD 31-90
    'Doubtful',      -- DPD 91-180
    'Loss'           -- DPD 181+
);


-- ==============================================================================
-- Core Tables
-- ==============================================================================

-- 2. loan_repayment_behavior
-- Maps every physical repayment down to exactly where the funds were allocated
CREATE TABLE loan_repayment_behavior (
    repayment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    loan_id UUID NOT NULL, -- FK to Loans
    transaction_id UUID NOT NULL, -- FK to Transactions (Receipt)
    
    payment_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    payment_source payment_source_type NOT NULL,
    payment_reference VARCHAR(100), -- E.g. MoMo Transaction ID
    
    -- Exact Mathematical Breakdown of Payment
    total_paid DECIMAL(18, 4) NOT NULL CHECK (total_paid > 0),
    principal_allocated DECIMAL(18, 4) DEFAULT 0.00,
    interest_allocated DECIMAL(18, 4) DEFAULT 0.00,
    penalty_allocated DECIMAL(18, 4) DEFAULT 0.00,
    fees_allocated DECIMAL(18, 4) DEFAULT 0.00,
    
    -- Days Past Due (DPD) metrics AT THE TIME of this exact payment
    days_past_due_upon_payment INT DEFAULT 0,
    
    -- Behavioral Triggers (Calculated downstream initially or via Trigger)
    is_first_payment_default BOOLEAN DEFAULT FALSE, -- Did they miss the first ever payment?
    late_pay_trend_score INT DEFAULT 0 -- Arbitrary sliding track score
);

-- Integrity rule ensures components match the payment total
ALTER TABLE loan_repayment_behavior 
    ADD CONSTRAINT check_allocation_matches_total 
    CHECK (total_paid = (principal_allocated + interest_allocated + penalty_allocated + fees_allocated));

CREATE INDEX idx_repayment_behavior_loan ON loan_repayment_behavior(loan_id);
CREATE INDEX idx_repayment_behavior_date ON loan_repayment_behavior(payment_date);


-- 3. loan_bog_classification (Historical Aging Snapshot Array)
-- Used for Vintage Analysis and BoG Regulatory Reporting. 
-- Batch jobs typically insert a snapshot record here.
CREATE TABLE loan_bog_classification (
    classification_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    loan_id UUID NOT NULL,
    
    evaluation_date DATE NOT NULL,
    
    days_past_due INT NOT NULL DEFAULT 0,
    classification bog_classification_tier NOT NULL,
    
    outstanding_principal DECIMAL(18, 4) NOT NULL,
    outstanding_interest DECIMAL(18, 4) NOT NULL,
    provisioning_amount DECIMAL(18, 4) NOT NULL, -- Computed based on Tier%
    
    CONSTRAINT unique_loan_eval_date UNIQUE(loan_id, evaluation_date)
);

CREATE INDEX idx_bog_classification_date ON loan_bog_classification(evaluation_date);
CREATE INDEX idx_bog_classification_tier ON loan_bog_classification(classification);

-- ==============================================================================
-- Behavioral Helpers (Views)
-- ==============================================================================

-- Vintage Analysis View: Easy querying by Disbursal Month cohort
-- Assuming a basic structure of the loans table exists elsewhere:
/*
CREATE OR REPLACE VIEW view_loan_vintage_analysis AS
SELECT 
    date_trunc('month', l.disbursement_date) AS vintage_month,
    COUNT(l.id) as total_loans_issued,
    SUM(l.principal) as total_principal_issued,
    
    -- FPD Hit Rate
    SUM(CASE WHEN r.is_first_payment_default THEN 1 ELSE 0 END) AS count_fpd,
    
    -- Active DPD status derived from the latest snapshot
    SUM(CASE WHEN c.classification = 'Substandard' THEN 1 ELSE 0 END) as count_substandard,
    SUM(CASE WHEN c.classification = 'Loss' THEN l.outstanding_balance ELSE 0 END) as total_loss_value
FROM 
    loans l
LEFT JOIN 
    loan_repayment_behavior r ON l.id = r.loan_id AND r.is_first_payment_default = TRUE
LEFT JOIN LATERAL (
    -- Fetch the most recent BoG classification for this loan
    SELECT classification 
    FROM loan_bog_classification 
    WHERE loan_id = l.id 
    ORDER BY evaluation_date DESC 
    LIMIT 1
) c ON TRUE
GROUP BY 
    date_trunc('month', l.disbursement_date)
ORDER BY 
    vintage_month DESC;
*/
