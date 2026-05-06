using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Data;

public static class DatabaseSchemaBootstrapper
{
    public static async Task EnsureAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS accounts
    ADD COLUMN IF NOT EXISTS is_confidential boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS owner_staff_id character varying(50) NULL;

ALTER TABLE IF EXISTS loans
    ADD COLUMN IF NOT EXISTS is_confidential boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS owner_staff_id character varying(50) NULL,
    ADD COLUMN IF NOT EXISTS servicing_account_id character varying(50) NULL,
    ADD COLUMN IF NOT EXISTS collateral_account_id character varying(50) NULL;

ALTER TABLE IF EXISTS investments
    ADD COLUMN IF NOT EXISTS is_confidential boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS owner_staff_id character varying(50) NULL;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE INDEX IF NOT EXISTS ix_accounts_owner_staff_id ON accounts (owner_staff_id);
CREATE INDEX IF NOT EXISTS ix_accounts_is_confidential ON accounts (is_confidential);
CREATE INDEX IF NOT EXISTS ix_loans_owner_staff_id ON loans (owner_staff_id);
CREATE INDEX IF NOT EXISTS ix_loans_is_confidential ON loans (is_confidential);
CREATE INDEX IF NOT EXISTS ix_loans_servicing_account_id ON loans (servicing_account_id);
CREATE INDEX IF NOT EXISTS ix_loans_collateral_account_id ON loans (collateral_account_id);
CREATE INDEX IF NOT EXISTS ix_investments_owner_staff_id ON investments (owner_staff_id);
CREATE INDEX IF NOT EXISTS ix_investments_is_confidential ON investments (is_confidential);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_accounts_owner_staff') THEN
        ALTER TABLE accounts ADD CONSTRAINT fk_accounts_owner_staff FOREIGN KEY (owner_staff_id) REFERENCES staff (id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_loans_owner_staff') THEN
        ALTER TABLE loans ADD CONSTRAINT fk_loans_owner_staff FOREIGN KEY (owner_staff_id) REFERENCES staff (id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_loans_servicing_account') THEN
        ALTER TABLE loans ADD CONSTRAINT fk_loans_servicing_account FOREIGN KEY (servicing_account_id) REFERENCES accounts (id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_loans_collateral_account') THEN
        ALTER TABLE loans ADD CONSTRAINT fk_loans_collateral_account FOREIGN KEY (collateral_account_id) REFERENCES accounts (id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_investments_owner_staff') THEN
        ALTER TABLE investments ADD CONSTRAINT fk_investments_owner_staff FOREIGN KEY (owner_staff_id) REFERENCES staff (id) ON DELETE SET NULL;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS client_standing_orders (
    id character varying(50) PRIMARY KEY,
    customer_id character varying(50) NOT NULL,
    source_account_id character varying(50) NOT NULL,
    instruction_type character varying(30) NOT NULL DEFAULT 'INTERNAL_TRANSFER',
    merchant_code character varying(50) NULL,
    merchant_name character varying(200) NULL,
    destination_account_id character varying(50) NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(10) NOT NULL DEFAULT 'GHS',
    frequency character varying(20) NOT NULL DEFAULT 'MONTHLY',
    narration character varying(500) NOT NULL DEFAULT '',
    start_date timestamp with time zone NOT NULL,
    next_run_at timestamp with time zone NOT NULL,
    end_date timestamp with time zone NULL,
    last_run_at timestamp with time zone NULL,
    status character varying(20) NOT NULL DEFAULT 'ACTIVE',
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_client_standing_orders_customer_status_next_run
    ON client_standing_orders (customer_id, status, next_run_at);");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS client_merchant_profiles (
    id character varying(50) PRIMARY KEY,
    customer_id character varying(50) NOT NULL,
    settlement_account_id character varying(50) NOT NULL,
    merchant_code character varying(50) NOT NULL,
    display_name character varying(200) NOT NULL,
    category character varying(100) NOT NULL DEFAULT 'General',
    currency character varying(10) NOT NULL DEFAULT 'GHS',
    status character varying(20) NOT NULL DEFAULT 'ACTIVE',
    qr_scheme character varying(30) NOT NULL DEFAULT 'BANKINSIGHT_QR',
    qr_payload text NOT NULL,
    ghqr_ready boolean NOT NULL DEFAULT false,
    accepts_app_payments boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
    last_payment_at timestamp with time zone NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_client_merchant_profiles_merchant_code
    ON client_merchant_profiles (merchant_code);
CREATE INDEX IF NOT EXISTS ix_client_merchant_profiles_customer_status_updated
    ON client_merchant_profiles (customer_id, status, updated_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_client_standing_orders_customer') THEN
        ALTER TABLE client_standing_orders ADD CONSTRAINT fk_client_standing_orders_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_client_standing_orders_source_account') THEN
        ALTER TABLE client_standing_orders ADD CONSTRAINT fk_client_standing_orders_source_account
        FOREIGN KEY (source_account_id) REFERENCES accounts (id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_client_standing_orders_destination_account') THEN
        ALTER TABLE client_standing_orders ADD CONSTRAINT fk_client_standing_orders_destination_account
        FOREIGN KEY (destination_account_id) REFERENCES accounts (id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_client_merchant_profiles_customer') THEN
        ALTER TABLE client_merchant_profiles ADD CONSTRAINT fk_client_merchant_profiles_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_client_merchant_profiles_settlement_account') THEN
        ALTER TABLE client_merchant_profiles ADD CONSTRAINT fk_client_merchant_profiles_settlement_account
        FOREIGN KEY (settlement_account_id) REFERENCES accounts (id) ON DELETE RESTRICT;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS bulk_payment_batches (
    id character varying(50) PRIMARY KEY,
    batch_reference character varying(100) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'PENDING',
    currency character varying(10) NOT NULL DEFAULT 'GHS',
    narration character varying(500) NULL,
    total_amount numeric(18,2) NOT NULL DEFAULT 0,
    processed_amount numeric(18,2) NOT NULL DEFAULT 0,
    item_count integer NOT NULL DEFAULT 0,
    processed_count integer NOT NULL DEFAULT 0,
    failed_count integer NOT NULL DEFAULT 0,
    submitted_by character varying(50) NULL,
    processed_at timestamp with time zone NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_bulk_payment_batches_reference
    ON bulk_payment_batches (batch_reference);

CREATE TABLE IF NOT EXISTS bulk_payment_items (
    id character varying(50) PRIMARY KEY,
    batch_id character varying(50) NOT NULL,
    account_id character varying(50) NOT NULL,
    transaction_type character varying(30) NOT NULL,
    amount numeric(18,2) NOT NULL,
    narration character varying(500) NULL,
    teller_id character varying(50) NULL,
    client_reference character varying(100) NULL,
    status character varying(20) NOT NULL DEFAULT 'PENDING',
    posted_transaction_id character varying(50) NULL,
    error_message character varying(1000) NULL,
    processed_at timestamp with time zone NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_bulk_payment_items_batch_status
    ON bulk_payment_items (batch_id, status);

CREATE TABLE IF NOT EXISTS cheque_clearing_items (
    id character varying(50) PRIMARY KEY,
    account_id character varying(50) NOT NULL,
    transaction_type character varying(20) NOT NULL DEFAULT 'DEPOSIT',
    cheque_number character varying(50) NOT NULL,
    drawer_name character varying(200) NULL,
    drawer_account_number character varying(50) NULL,
    presenting_bank_code character varying(20) NOT NULL,
    drawee_bank_code character varying(20) NOT NULL,
    clearing_channel character varying(30) NOT NULL DEFAULT 'GHIPSS',
    bog_regulatory_class character varying(30) NOT NULL DEFAULT 'LOCAL',
    is_other_bank_cheque boolean NOT NULL DEFAULT false,
    amount numeric(18,2) NOT NULL,
    currency character varying(10) NOT NULL DEFAULT 'GHS',
    status character varying(20) NOT NULL DEFAULT 'LODGED',
    hold_days integer NOT NULL DEFAULT 0,
    lodged_by character varying(50) NULL,
    lodged_at timestamp with time zone NOT NULL DEFAULT NOW(),
    clearing_date date NOT NULL,
    cleared_at timestamp with time zone NULL,
    posted_transaction_id character varying(50) NULL,
    return_reason character varying(500) NULL,
    failure_reason character varying(1000) NULL,
    narration character varying(500) NULL
);

CREATE INDEX IF NOT EXISTS ix_cheque_clearing_items_status_date
    ON cheque_clearing_items (status, clearing_date);
CREATE INDEX IF NOT EXISTS ix_cheque_clearing_items_cheque_number
    ON cheque_clearing_items (cheque_number);");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS cheque_book_inventories (
    id character varying(50) PRIMARY KEY,
    book_reference character varying(100) NOT NULL,
    branch_id character varying(50) NOT NULL,
    series_prefix character varying(20) NOT NULL,
    start_serial_number bigint NOT NULL,
    end_serial_number bigint NOT NULL,
    leaf_count integer NOT NULL,
    available_leaf_count integer NOT NULL DEFAULT 0,
    used_leaf_count integer NOT NULL DEFAULT 0,
    cancelled_leaf_count integer NOT NULL DEFAULT 0,
    status character varying(20) NOT NULL DEFAULT 'IN_STOCK',
    account_id character varying(50) NULL,
    customer_id character varying(50) NULL,
    issued_at timestamp with time zone NULL,
    issued_by character varying(50) NULL,
    remarks character varying(500) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_cheque_book_inventories_reference
    ON cheque_book_inventories (book_reference);
CREATE INDEX IF NOT EXISTS ix_cheque_book_inventories_status_branch
    ON cheque_book_inventories (status, branch_id);

CREATE TABLE IF NOT EXISTS cheque_book_leaves (
    id character varying(50) PRIMARY KEY,
    book_id character varying(50) NOT NULL,
    serial_number bigint NOT NULL,
    cheque_number character varying(50) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'AVAILABLE',
    account_id character varying(50) NULL,
    used_transaction_id character varying(50) NULL,
    used_at timestamp with time zone NULL,
    cancel_reason character varying(500) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_cheque_book_leaves_cheque_number
    ON cheque_book_leaves (cheque_number);
CREATE INDEX IF NOT EXISTS ix_cheque_book_leaves_book_status
    ON cheque_book_leaves (book_id, status);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bulk_payment_items_batch') THEN
        ALTER TABLE bulk_payment_items ADD CONSTRAINT fk_bulk_payment_items_batch
        FOREIGN KEY (batch_id) REFERENCES bulk_payment_batches (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bulk_payment_items_account') THEN
        ALTER TABLE bulk_payment_items ADD CONSTRAINT fk_bulk_payment_items_account
        FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cheque_clearing_items_account') THEN
        ALTER TABLE cheque_clearing_items ADD CONSTRAINT fk_cheque_clearing_items_account
        FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cheque_book_inventories_account') THEN
        ALTER TABLE cheque_book_inventories ADD CONSTRAINT fk_cheque_book_inventories_account
        FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cheque_book_inventories_customer') THEN
        ALTER TABLE cheque_book_inventories ADD CONSTRAINT fk_cheque_book_inventories_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cheque_book_leaves_book') THEN
        ALTER TABLE cheque_book_leaves ADD CONSTRAINT fk_cheque_book_leaves_book
        FOREIGN KEY (book_id) REFERENCES cheque_book_inventories (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cheque_book_leaves_account') THEN
        ALTER TABLE cheque_book_leaves ADD CONSTRAINT fk_cheque_book_leaves_account
        FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE SET NULL;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS product_charge_definitions (
    id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    product_id character varying(50) NOT NULL,
    code character varying(50) NOT NULL,
    name character varying(100) NOT NULL,
    charge_type character varying(20) NOT NULL DEFAULT 'FEE',
    calculation_type character varying(20) NOT NULL DEFAULT 'FLAT',
    flat_amount numeric(18,2) NULL,
    rate numeric(18,6) NULL,
    minimum_amount numeric(18,2) NULL,
    maximum_amount numeric(18,2) NULL,
    apply_on character varying(30) NOT NULL DEFAULT 'MANUAL',
    income_gl_code character varying(50) NULL,
    status character varying(20) NOT NULL DEFAULT 'ACTIVE',
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_product_charge_definitions_product_code
    ON product_charge_definitions (product_id, code);
CREATE INDEX IF NOT EXISTS ix_product_charge_definitions_apply_on
    ON product_charge_definitions (apply_on);
CREATE INDEX IF NOT EXISTS ix_product_charge_definitions_status
    ON product_charge_definitions (status);");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS digital_investment_profiles (
    id character varying(50) PRIMARY KEY,
    account_id character varying(50) NOT NULL,
    customer_id character varying(50) NOT NULL,
    funding_account_id character varying(50) NOT NULL,
    product_code character varying(50) NOT NULL,
    tenor_days integer NOT NULL,
    rate numeric(18,6) NOT NULL,
    payout_option character varying(30) NOT NULL DEFAULT 'AT_MATURITY',
    auto_rollover boolean NOT NULL DEFAULT false,
    status character varying(20) NOT NULL DEFAULT 'ACTIVE',
    start_date timestamp with time zone NOT NULL,
    maturity_date timestamp with time zone NOT NULL,
    matured_at timestamp with time zone NULL,
    liquidated_at timestamp with time zone NULL,
    notes character varying(1000) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_digital_investment_profiles_account_id
    ON digital_investment_profiles (account_id);
CREATE INDEX IF NOT EXISTS ix_digital_investment_profiles_customer_status_maturity
    ON digital_investment_profiles (customer_id, status, maturity_date);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_digital_investment_profiles_account') THEN
        ALTER TABLE digital_investment_profiles ADD CONSTRAINT fk_digital_investment_profiles_account
        FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_digital_investment_profiles_customer') THEN
        ALTER TABLE digital_investment_profiles ADD CONSTRAINT fk_digital_investment_profiles_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_digital_investment_profiles_funding_account') THEN
        ALTER TABLE digital_investment_profiles ADD CONSTRAINT fk_digital_investment_profiles_funding_account
        FOREIGN KEY (funding_account_id) REFERENCES accounts (id) ON DELETE RESTRICT;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS collector_portfolio_assignments (
    id character varying(50) PRIMARY KEY,
    customer_id character varying(50) NOT NULL,
    account_id character varying(50) NOT NULL,
    collector_staff_id character varying(50) NULL,
    loan_product_id character varying(50) NULL,
    collection_type character varying(30) NOT NULL DEFAULT 'SUSU_SAVINGS',
    frequency character varying(20) NOT NULL DEFAULT 'DAILY',
    target_amount numeric(18,2) NOT NULL DEFAULT 0,
    minimum_contribution_amount numeric(18,2) NULL,
    route_name character varying(120) NULL,
    meeting_day character varying(20) NULL,
    status character varying(20) NOT NULL DEFAULT 'ACTIVE',
    next_collection_date date NULL,
    last_collection_at timestamp with time zone NULL,
    notes character varying(1000) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_collector_portfolio_assignments_collector_status_date
    ON collector_portfolio_assignments (collector_staff_id, status, next_collection_date);
CREATE INDEX IF NOT EXISTS ix_collector_portfolio_assignments_customer_account_type
    ON collector_portfolio_assignments (customer_id, account_id, collection_type);

CREATE TABLE IF NOT EXISTS field_collection_batches (
    id character varying(50) PRIMARY KEY,
    collector_staff_id character varying(50) NULL,
    branch_id character varying(50) NULL,
    batch_date date NOT NULL,
    route_name character varying(120) NULL,
    status character varying(20) NOT NULL DEFAULT 'OPEN',
    expected_amount numeric(18,2) NOT NULL DEFAULT 0,
    collected_amount numeric(18,2) NOT NULL DEFAULT 0,
    settled_amount numeric(18,2) NOT NULL DEFAULT 0,
    variance_amount numeric(18,2) NOT NULL DEFAULT 0,
    opening_float numeric(18,2) NOT NULL DEFAULT 0,
    notes character varying(1000) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    submitted_at timestamp with time zone NULL,
    settled_at timestamp with time zone NULL
);

CREATE INDEX IF NOT EXISTS ix_field_collection_batches_collector_date_status
    ON field_collection_batches (collector_staff_id, batch_date, status);

CREATE TABLE IF NOT EXISTS field_collection_batch_lines (
    id uuid PRIMARY KEY,
    batch_id character varying(50) NOT NULL,
    assignment_id character varying(50) NULL,
    customer_id character varying(50) NOT NULL,
    account_id character varying(50) NOT NULL,
    loan_id character varying(50) NULL,
    transaction_type character varying(30) NOT NULL DEFAULT 'SUSU_SAVINGS',
    amount numeric(18,2) NOT NULL DEFAULT 0,
    currency character varying(10) NOT NULL DEFAULT 'GHS',
    status character varying(20) NOT NULL DEFAULT 'POSTED',
    narration character varying(500) NOT NULL DEFAULT '',
    posted_transaction_id character varying(100) NULL,
    due_amount numeric(18,2) NULL,
    was_missed boolean NOT NULL DEFAULT false,
    collected_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_field_collection_batch_lines_batch_collected_at
    ON field_collection_batch_lines (batch_id, collected_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collector_portfolio_assignments_customer') THEN
        ALTER TABLE collector_portfolio_assignments ADD CONSTRAINT fk_collector_portfolio_assignments_customer
        FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collector_portfolio_assignments_account') THEN
        ALTER TABLE collector_portfolio_assignments ADD CONSTRAINT fk_collector_portfolio_assignments_account
        FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collector_portfolio_assignments_staff') THEN
        ALTER TABLE collector_portfolio_assignments ADD CONSTRAINT fk_collector_portfolio_assignments_staff
        FOREIGN KEY (collector_staff_id) REFERENCES staff(id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collector_portfolio_assignments_loan_product') THEN
        ALTER TABLE collector_portfolio_assignments ADD CONSTRAINT fk_collector_portfolio_assignments_loan_product
        FOREIGN KEY (loan_product_id) REFERENCES loan_products(id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_field_collection_batches_staff') THEN
        ALTER TABLE field_collection_batches ADD CONSTRAINT fk_field_collection_batches_staff
        FOREIGN KEY (collector_staff_id) REFERENCES staff(id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_field_collection_batch_lines_batch') THEN
        ALTER TABLE field_collection_batch_lines ADD CONSTRAINT fk_field_collection_batch_lines_batch
        FOREIGN KEY (batch_id) REFERENCES field_collection_batches(id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_field_collection_batch_lines_assignment') THEN
        ALTER TABLE field_collection_batch_lines ADD CONSTRAINT fk_field_collection_batch_lines_assignment
        FOREIGN KEY (assignment_id) REFERENCES collector_portfolio_assignments(id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_field_collection_batch_lines_customer') THEN
        ALTER TABLE field_collection_batch_lines ADD CONSTRAINT fk_field_collection_batch_lines_customer
        FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_field_collection_batch_lines_account') THEN
        ALTER TABLE field_collection_batch_lines ADD CONSTRAINT fk_field_collection_batch_lines_account
        FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_field_collection_batch_lines_loan') THEN
        ALTER TABLE field_collection_batch_lines ADD CONSTRAINT fk_field_collection_batch_lines_loan
        FOREIGN KEY (loan_id) REFERENCES loans(id) ON DELETE SET NULL;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_charge_definitions_product') THEN
        ALTER TABLE product_charge_definitions ADD CONSTRAINT fk_product_charge_definitions_product
        FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS products
    ADD COLUMN IF NOT EXISTS lifecycle_status character varying(30) NOT NULL DEFAULT 'DRAFT',
    ADD COLUMN IF NOT EXISTS version_number integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS effective_from timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS retired_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS last_simulation_json text NULL;

CREATE INDEX IF NOT EXISTS ix_products_lifecycle_status_type_effective_from
    ON products (lifecycle_status, type, effective_from);");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS collection_cases (
    id character varying(50) PRIMARY KEY,
    loan_id character varying(50) NOT NULL,
    customer_id character varying(50) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'OPEN',
    priority character varying(20) NOT NULL DEFAULT 'MEDIUM',
    recovery_stage character varying(30) NOT NULL DEFAULT 'EARLY_ARREARS',
    delinquency_days integer NOT NULL DEFAULT 0,
    outstanding_balance numeric(18,2) NOT NULL DEFAULT 0,
    amount_in_arrears numeric(18,2) NOT NULL DEFAULT 0,
    assigned_to character varying(50) NULL,
    next_action_date timestamp with time zone NULL,
    promise_to_pay_date timestamp with time zone NULL,
    promise_to_pay_amount numeric(18,2) NULL,
    last_contact_at timestamp with time zone NULL,
    last_payment_at timestamp with time zone NULL,
    next_escalation_date timestamp with time zone NULL,
    notes character varying(2000) NULL,
    recovery_strategy character varying(100) NULL,
    legal_status character varying(30) NULL,
    settlement_amount numeric(18,2) NULL,
    settlement_expiry_date timestamp with time zone NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_collection_cases_loan_id
    ON collection_cases (loan_id);
CREATE INDEX IF NOT EXISTS ix_collection_cases_status_priority_next_action
    ON collection_cases (status, priority, next_action_date);
CREATE INDEX IF NOT EXISTS ix_collection_cases_recovery_legal_escalation
    ON collection_cases (recovery_stage, legal_status, next_escalation_date);

CREATE TABLE IF NOT EXISTS collection_case_events (
    id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    case_id character varying(50) NOT NULL,
    event_type character varying(30) NOT NULL DEFAULT 'NOTE',
    performed_by character varying(50) NULL,
    detail character varying(2000) NOT NULL,
    metadata_json text NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_collection_case_events_case_created_at
    ON collection_case_events (case_id, created_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collection_cases_loan') THEN
        ALTER TABLE collection_cases ADD CONSTRAINT fk_collection_cases_loan
        FOREIGN KEY (loan_id) REFERENCES loans (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collection_cases_customer') THEN
        ALTER TABLE collection_cases ADD CONSTRAINT fk_collection_cases_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collection_case_events_case') THEN
        ALTER TABLE collection_case_events ADD CONSTRAINT fk_collection_case_events_case
        FOREIGN KEY (case_id) REFERENCES collection_cases (id) ON DELETE CASCADE;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS collection_cases
    ADD COLUMN IF NOT EXISTS last_payment_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS next_escalation_date timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS recovery_strategy character varying(100) NULL,
    ADD COLUMN IF NOT EXISTS legal_status character varying(30) NULL,
    ADD COLUMN IF NOT EXISTS settlement_amount numeric(18,2) NULL,
    ADD COLUMN IF NOT EXISTS settlement_expiry_date timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS assigned_agency character varying(120) NULL,
    ADD COLUMN IF NOT EXISTS repossession_status character varying(30) NULL,
    ADD COLUMN IF NOT EXISTS approval_status character varying(30) NULL,
    ADD COLUMN IF NOT EXISTS write_off_recommended_amount numeric(18,2) NULL,
    ADD COLUMN IF NOT EXISTS write_off_reason character varying(500) NULL;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS reconciliation_exceptions (
    id character varying(50) PRIMARY KEY,
    category character varying(30) NOT NULL,
    source_system character varying(50) NOT NULL,
    reference character varying(100) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'OPEN',
    severity character varying(20) NOT NULL DEFAULT 'MEDIUM',
    currency character varying(10) NOT NULL DEFAULT 'GHS',
    amount numeric(18,2) NOT NULL DEFAULT 0,
    owner_user_id character varying(50) NULL,
    summary character varying(255) NOT NULL,
    detail character varying(2000) NOT NULL,
    detected_at timestamp with time zone NOT NULL DEFAULT NOW(),
    due_at timestamp with time zone NULL,
    resolved_at timestamp with time zone NULL,
    retry_count integer NOT NULL DEFAULT 0,
    last_attempt_at timestamp with time zone NULL,
    workflow_stage character varying(40) NULL,
    resolution_code character varying(40) NULL,
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_reconciliation_exceptions_status_category_due
    ON reconciliation_exceptions (status, category, due_at);
CREATE INDEX IF NOT EXISTS ix_reconciliation_exceptions_workflow_stage
    ON reconciliation_exceptions (workflow_stage, status);

CREATE TABLE IF NOT EXISTS collateral_records (
    id character varying(50) PRIMARY KEY,
    loan_id character varying(50) NOT NULL,
    customer_id character varying(50) NOT NULL,
    collateral_type character varying(50) NOT NULL,
    description character varying(500) NOT NULL,
    registered_value numeric(18,2) NOT NULL DEFAULT 0,
    current_valuation numeric(18,2) NOT NULL DEFAULT 0,
    valuation_date timestamp with time zone NULL,
    valuation_expiry_date timestamp with time zone NULL,
    perfection_status character varying(30) NOT NULL DEFAULT 'PENDING',
    document_reference character varying(100) NULL,
    custody_location character varying(100) NULL,
    status character varying(20) NOT NULL DEFAULT 'ACTIVE',
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_collateral_records_loan_status_expiry
    ON collateral_records (loan_id, status, valuation_expiry_date);

CREATE TABLE IF NOT EXISTS covenant_records (
    id character varying(50) PRIMARY KEY,
    loan_id character varying(50) NOT NULL,
    name character varying(150) NOT NULL,
    covenant_type character varying(30) NOT NULL DEFAULT 'REPORTING',
    status character varying(20) NOT NULL DEFAULT 'PENDING',
    due_date timestamp with time zone NULL,
    last_reviewed_at timestamp with time zone NULL,
    detail character varying(1000) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_covenant_records_loan_status_due
    ON covenant_records (loan_id, status, due_date);");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS api_product_definitions (
    id character varying(50) PRIMARY KEY,
    name character varying(120) NOT NULL,
    slug character varying(80) NOT NULL,
    category character varying(40) NOT NULL,
    audience character varying(40) NOT NULL DEFAULT 'PARTNER',
    status character varying(20) NOT NULL DEFAULT 'PUBLISHED',
    version character varying(20) NOT NULL DEFAULT 'v1',
    auth_model character varying(40) NOT NULL DEFAULT 'BEARER_TOKEN',
    base_path character varying(120) NOT NULL,
    documentation_path character varying(255) NOT NULL,
    rate_limit_per_minute integer NOT NULL DEFAULT 120,
    supports_webhooks boolean NOT NULL DEFAULT FALSE,
    supports_sandbox boolean NOT NULL DEFAULT TRUE,
    scope_summary character varying(1000) NOT NULL,
    last_published_at timestamp with time zone NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_api_product_definitions_slug_version
    ON api_product_definitions (slug, version);

CREATE TABLE IF NOT EXISTS partner_applications (
    id character varying(50) PRIMARY KEY,
    name character varying(120) NOT NULL,
    partner_name character varying(120) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'SANDBOX',
    environment character varying(20) NOT NULL DEFAULT 'SANDBOX',
    callback_url character varying(255) NOT NULL,
    contact_email character varying(120) NOT NULL,
    api_product_ids_json text NOT NULL DEFAULT '[]',
    sandbox_key character varying(120) NOT NULL,
    production_key character varying(120) NULL,
    production_key_activated_at timestamp with time zone NULL,
    last_key_rotated_at timestamp with time zone NULL,
    last_activity_at timestamp with time zone NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_partner_applications_status_environment
    ON partner_applications (status, environment);

CREATE TABLE IF NOT EXISTS webhook_subscriptions (
    id character varying(50) PRIMARY KEY,
    partner_application_id character varying(50) NOT NULL,
    event_name character varying(80) NOT NULL,
    target_url character varying(255) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'ACTIVE',
    signing_secret character varying(120) NOT NULL,
    last_delivery_at timestamp with time zone NULL,
    last_delivery_status character varying(20) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_webhook_subscriptions_partner_event_status
    ON webhook_subscriptions (partner_application_id, event_name, status);");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS webhook_delivery_logs (
    id character varying(50) PRIMARY KEY,
    webhook_subscription_id character varying(50) NOT NULL,
    event_name character varying(80) NOT NULL,
    delivery_status character varying(20) NOT NULL DEFAULT 'PENDING',
    response_code integer NULL,
    attempt_number integer NOT NULL DEFAULT 1,
    failure_reason character varying(500) NULL,
    delivered_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_webhook_delivery_logs_subscription_delivered_at
    ON webhook_delivery_logs (webhook_subscription_id, delivered_at DESC);

CREATE TABLE IF NOT EXISTS settlement_instructions (
    id character varying(50) PRIMARY KEY,
    reconciliation_exception_id character varying(50) NOT NULL,
    instruction_type character varying(40) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'PENDING',
    currency character varying(10) NOT NULL DEFAULT 'GHS',
    amount numeric(18,2) NOT NULL DEFAULT 0,
    settlement_account character varying(80) NULL,
    counterparty character varying(120) NULL,
    due_at timestamp with time zone NULL,
    completed_at timestamp with time zone NULL,
    notes character varying(1000) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_settlement_instructions_exception_status
    ON settlement_instructions (reconciliation_exception_id, status);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_webhook_subscriptions_partner_application') THEN
        ALTER TABLE webhook_subscriptions ADD CONSTRAINT fk_webhook_subscriptions_partner_application
        FOREIGN KEY (partner_application_id) REFERENCES partner_applications (id) ON DELETE CASCADE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_webhook_delivery_logs_subscription') THEN
        ALTER TABLE webhook_delivery_logs ADD CONSTRAINT fk_webhook_delivery_logs_subscription
        FOREIGN KEY (webhook_subscription_id) REFERENCES webhook_subscriptions (id) ON DELETE CASCADE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_settlement_instructions_exception') THEN
        ALTER TABLE settlement_instructions ADD CONSTRAINT fk_settlement_instructions_exception
        FOREIGN KEY (reconciliation_exception_id) REFERENCES reconciliation_exceptions (id) ON DELETE CASCADE;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS reconciliation_exceptions
    ADD COLUMN IF NOT EXISTS retry_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS last_attempt_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS workflow_stage character varying(40) NULL,
    ADD COLUMN IF NOT EXISTS resolution_code character varying(40) NULL;

ALTER TABLE IF EXISTS partner_applications
    ADD COLUMN IF NOT EXISTS production_key_activated_at timestamp with time zone NULL;");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collateral_records_loan') THEN
        ALTER TABLE collateral_records ADD CONSTRAINT fk_collateral_records_loan
        FOREIGN KEY (loan_id) REFERENCES loans (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_collateral_records_customer') THEN
        ALTER TABLE collateral_records ADD CONSTRAINT fk_collateral_records_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_covenant_records_loan') THEN
        ALTER TABLE covenant_records ADD CONSTRAINT fk_covenant_records_loan
        FOREIGN KEY (loan_id) REFERENCES loans (id) ON DELETE CASCADE;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS inter_branch_transfers
    ADD COLUMN IF NOT EXISTS dispatched_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS sent_by character varying(50) NULL,
    ADD COLUMN IF NOT EXISTS received_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS received_by character varying(50) NULL;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE INDEX IF NOT EXISTS ix_inter_branch_transfers_sent_by ON inter_branch_transfers (sent_by);
CREATE INDEX IF NOT EXISTS ix_inter_branch_transfers_received_by ON inter_branch_transfers (received_by);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_inter_branch_transfers_staff_sent_by') THEN
        ALTER TABLE inter_branch_transfers ADD CONSTRAINT fk_inter_branch_transfers_staff_sent_by FOREIGN KEY (sent_by) REFERENCES staff (id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_inter_branch_transfers_staff_received_by') THEN
        ALTER TABLE inter_branch_transfers ADD CONSTRAINT fk_inter_branch_transfers_staff_received_by FOREIGN KEY (received_by) REFERENCES staff (id) ON DELETE RESTRICT;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS cash_incidents (
    id character varying(50) PRIMARY KEY,
    branch_id character varying(50) NOT NULL,
    store_type character varying(30) NOT NULL,
    store_id character varying(100) NOT NULL,
    incident_type character varying(30) NOT NULL,
    currency character varying(10) NOT NULL DEFAULT 'GHS',
    amount numeric(18,2) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'OPEN',
    reference character varying(100) NULL,
    narration character varying(1000) NULL,
    reported_by character varying(50) NULL,
    resolved_by character varying(50) NULL,
    reported_at timestamp with time zone NOT NULL DEFAULT NOW(),
    resolved_at timestamp with time zone NULL
);

CREATE INDEX IF NOT EXISTS ix_cash_incidents_branch_id ON cash_incidents (branch_id);
CREATE INDEX IF NOT EXISTS ix_cash_incidents_status ON cash_incidents (status);
CREATE INDEX IF NOT EXISTS ix_cash_incidents_reported_at ON cash_incidents (reported_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cash_incidents_branch') THEN
        ALTER TABLE cash_incidents ADD CONSTRAINT fk_cash_incidents_branch FOREIGN KEY (branch_id) REFERENCES branches(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cash_incidents_reported_by_staff') THEN
        ALTER TABLE cash_incidents ADD CONSTRAINT fk_cash_incidents_reported_by_staff FOREIGN KEY (reported_by) REFERENCES staff(id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cash_incidents_resolved_by_staff') THEN
        ALTER TABLE cash_incidents ADD CONSTRAINT fk_cash_incidents_resolved_by_staff FOREIGN KEY (resolved_by) REFERENCES staff(id) ON DELETE SET NULL;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS user_sessions
    ALTER COLUMN token TYPE text,
    ALTER COLUMN refresh_token TYPE text;");

        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS customer_credentials
    ADD COLUMN IF NOT EXISTS transaction_pin_hash character varying(255) NULL;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS report_favorites (
    id uuid PRIMARY KEY,
    staff_id character varying(50) NOT NULL,
    report_code character varying(100) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_report_favorites_staff_report ON report_favorites (staff_id, report_code);
CREATE INDEX IF NOT EXISTS ix_report_favorites_created_at ON report_favorites (created_at DESC);

CREATE TABLE IF NOT EXISTS report_filter_presets (
    id uuid PRIMARY KEY,
    staff_id character varying(50) NOT NULL,
    report_code character varying(100) NOT NULL,
    preset_name character varying(150) NOT NULL,
    parameters_json jsonb NOT NULL DEFAULT '{{}}'::jsonb,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_report_filter_presets_staff_report ON report_filter_presets (staff_id, report_code);
CREATE INDEX IF NOT EXISTS ix_report_filter_presets_updated_at ON report_filter_presets (updated_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_report_favorites_staff') THEN
        ALTER TABLE report_favorites ADD CONSTRAINT fk_report_favorites_staff FOREIGN KEY (staff_id) REFERENCES staff(id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_report_filter_presets_staff') THEN
        ALTER TABLE report_filter_presets ADD CONSTRAINT fk_report_filter_presets_staff FOREIGN KEY (staff_id) REFERENCES staff(id) ON DELETE CASCADE;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS customer_media_assets (
    id character varying(50) PRIMARY KEY,
    customer_id character varying(50) NOT NULL,
    media_type character varying(30) NOT NULL,
    media_side character varying(20) NULL,
    file_name character varying(255) NOT NULL,
    content_type character varying(100) NOT NULL,
    storage_mode character varying(20) NOT NULL DEFAULT 'inline',
    storage_path text NULL,
    data_url text NULL,
    file_size_bytes bigint NULL,
    status character varying(30) NOT NULL DEFAULT 'PENDING_SCAN',
    uploaded_by character varying(50) NULL,
    uploaded_at timestamp with time zone NOT NULL DEFAULT NOW(),
    scanned_at timestamp with time zone NULL,
    reviewed_at timestamp with time zone NULL,
    review_note character varying(1000) NULL
);

CREATE INDEX IF NOT EXISTS ix_customer_media_assets_customer_media_type
    ON customer_media_assets (customer_id, media_type);
CREATE INDEX IF NOT EXISTS ix_customer_media_assets_status
    ON customer_media_assets (status);

CREATE TABLE IF NOT EXISTS client_kyc_cases (
    id character varying(50) PRIMARY KEY,
    customer_id character varying(50) NOT NULL,
    reference character varying(50) NOT NULL,
    status character varying(30) NOT NULL DEFAULT 'SUBMITTED',
    reason character varying(100) NOT NULL DEFAULT 'PROFILE_REFRESH',
    summary character varying(500) NOT NULL DEFAULT '',
    submitted_at timestamp with time zone NOT NULL DEFAULT NOW(),
    reviewed_at timestamp with time zone NULL,
    reviewed_by character varying(50) NULL,
    decision_note character varying(1000) NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_client_kyc_cases_reference
    ON client_kyc_cases (reference);
CREATE INDEX IF NOT EXISTS ix_client_kyc_cases_customer_submitted_at
    ON client_kyc_cases (customer_id, submitted_at DESC);
CREATE INDEX IF NOT EXISTS ix_client_kyc_cases_status_submitted_at
    ON client_kyc_cases (status, submitted_at DESC);

CREATE TABLE IF NOT EXISTS client_kyc_case_events (
    id character varying(50) PRIMARY KEY,
    case_id character varying(50) NOT NULL,
    event_type character varying(50) NOT NULL,
    title character varying(200) NOT NULL,
    description character varying(1000) NOT NULL,
    actor_id character varying(50) NULL,
    actor_name character varying(200) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_client_kyc_case_events_case_created_at
    ON client_kyc_case_events (case_id, created_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS customer_media_assets
    ADD COLUMN IF NOT EXISTS storage_mode character varying(20) NOT NULL DEFAULT 'inline',
    ADD COLUMN IF NOT EXISTS storage_path text NULL,
    ADD COLUMN IF NOT EXISTS data_url text NULL,
    ADD COLUMN IF NOT EXISTS file_size_bytes bigint NULL,
    ADD COLUMN IF NOT EXISTS status character varying(30) NOT NULL DEFAULT 'PENDING_SCAN',
    ADD COLUMN IF NOT EXISTS uploaded_by character varying(50) NULL,
    ADD COLUMN IF NOT EXISTS uploaded_at timestamp with time zone NOT NULL DEFAULT NOW(),
    ADD COLUMN IF NOT EXISTS scanned_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS reviewed_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS review_note character varying(1000) NULL;

ALTER TABLE IF EXISTS client_kyc_cases
    ADD COLUMN IF NOT EXISTS reviewer_user_id character varying(50) NULL,
    ADD COLUMN IF NOT EXISTS reviewer_name character varying(100) NULL,
    ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT NOW();

ALTER TABLE IF EXISTS client_kyc_case_events
    ADD COLUMN IF NOT EXISTS actor_id character varying(50) NULL;");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_customer_media_assets_customer') THEN
        ALTER TABLE customer_media_assets ADD CONSTRAINT fk_customer_media_assets_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_client_kyc_cases_customer') THEN
        ALTER TABLE client_kyc_cases ADD CONSTRAINT fk_client_kyc_cases_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_client_kyc_cases_reviewer') THEN
        ALTER TABLE client_kyc_cases ADD CONSTRAINT fk_client_kyc_cases_reviewer
        FOREIGN KEY (reviewed_by) REFERENCES staff (id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_client_kyc_case_events_case') THEN
        ALTER TABLE client_kyc_case_events ADD CONSTRAINT fk_client_kyc_case_events_case
        FOREIGN KEY (case_id) REFERENCES client_kyc_cases (id) ON DELETE CASCADE;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS internal_credit_score_assessments (
    id uuid PRIMARY KEY,
    customer_id character varying(50) NOT NULL,
    loan_id character varying(50) NULL,
    score integer NOT NULL,
    probability_good numeric(9,6) NOT NULL DEFAULT 0,
    risk_band character varying(20) NOT NULL DEFAULT 'UNKNOWN',
    risk_grade character varying(20) NOT NULL DEFAULT 'UNKNOWN',
    decision character varying(20) NOT NULL DEFAULT 'REVIEW',
    recommendation character varying(200) NOT NULL DEFAULT 'Manual review',
    model_version character varying(50) NOT NULL DEFAULT 'ml-credit-v1',
    training_sample_count integer NOT NULL DEFAULT 0,
    feature_payload jsonb NOT NULL DEFAULT '{{}}'::jsonb,
    checked_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_internal_credit_score_assessments_customer_checked
    ON internal_credit_score_assessments (customer_id, checked_at DESC);
CREATE INDEX IF NOT EXISTS ix_internal_credit_score_assessments_loan_checked
    ON internal_credit_score_assessments (loan_id, checked_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_internal_credit_score_customer') THEN
        ALTER TABLE internal_credit_score_assessments ADD CONSTRAINT fk_internal_credit_score_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_internal_credit_score_loan') THEN
        ALTER TABLE internal_credit_score_assessments ADD CONSTRAINT fk_internal_credit_score_loan
        FOREIGN KEY (loan_id) REFERENCES loans (id) ON DELETE SET NULL;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS workspace_preferences (
    id character varying(50) PRIMARY KEY,
    staff_id character varying(50) NOT NULL,
    workspace_key character varying(100) NOT NULL,
    view_name character varying(150) NULL,
    route character varying(200) NULL,
    filter_json jsonb NULL,
    is_favorite boolean NOT NULL DEFAULT false,
    is_pinned boolean NOT NULL DEFAULT false,
    is_default boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_workspace_preferences_staff_updated
    ON workspace_preferences (staff_id, updated_at DESC);
CREATE INDEX IF NOT EXISTS ix_workspace_preferences_workspace_key
    ON workspace_preferences (workspace_key);
CREATE UNIQUE INDEX IF NOT EXISTS ix_workspace_preferences_staff_workspace_favorite
    ON workspace_preferences (staff_id, workspace_key)
    WHERE view_name IS NULL;");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_workspace_preferences_staff') THEN
        ALTER TABLE workspace_preferences ADD CONSTRAINT fk_workspace_preferences_staff
        FOREIGN KEY (staff_id) REFERENCES staff(id) ON DELETE CASCADE;
    END IF;
END $$;");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS regulatory_variance_resolutions (
    id character varying(50) PRIMARY KEY,
    reference character varying(100) NOT NULL,
    return_type character varying(100) NOT NULL,
    resolution_status character varying(20) NOT NULL DEFAULT 'OPEN',
    owner_user_id character varying(50) NULL,
    owner_name character varying(150) NULL,
    assigned_by_user_id character varying(50) NULL,
    assigned_by_name character varying(150) NULL,
    assigned_at timestamp with time zone NULL,
    resolution_note character varying(2000) NULL,
    resolved_at timestamp with time zone NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_regulatory_variance_resolutions_reference
    ON regulatory_variance_resolutions (reference, return_type);
CREATE INDEX IF NOT EXISTS ix_regulatory_variance_resolutions_status
    ON regulatory_variance_resolutions (resolution_status, updated_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE IF EXISTS regulatory_variance_resolutions
    ADD COLUMN IF NOT EXISTS assigned_by_user_id character varying(50) NULL,
    ADD COLUMN IF NOT EXISTS assigned_by_name character varying(150) NULL,
    ADD COLUMN IF NOT EXISTS assigned_at timestamp with time zone NULL;

CREATE TABLE IF NOT EXISTS regulatory_variance_events (
    id character varying(50) PRIMARY KEY,
    reference character varying(100) NOT NULL,
    return_type character varying(100) NOT NULL,
    event_type character varying(50) NOT NULL,
    performed_by_user_id character varying(50) NULL,
    performed_by_name character varying(150) NULL,
    detail character varying(2000) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_regulatory_variance_events_reference_created
    ON regulatory_variance_events (reference, return_type, created_at DESC);

CREATE TABLE IF NOT EXISTS relationship_ownership_assignments (
    id character varying(50) PRIMARY KEY,
    customer_id character varying(50) NOT NULL,
    owner_user_id character varying(50) NULL,
    owner_name character varying(150) NULL,
    assigned_by_user_id character varying(50) NULL,
    assigned_by_name character varying(150) NULL,
    assignment_note character varying(2000) NULL,
    assigned_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_relationship_ownership_assignments_customer
    ON relationship_ownership_assignments (customer_id);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_relationship_ownership_assignments_customer') THEN
        ALTER TABLE relationship_ownership_assignments ADD CONSTRAINT fk_relationship_ownership_assignments_customer
        FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;
    END IF;
END $$;");
    }
}



