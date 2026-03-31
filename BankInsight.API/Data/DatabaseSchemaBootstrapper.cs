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
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_charge_definitions_product') THEN
        ALTER TABLE product_charge_definitions ADD CONSTRAINT fk_product_charge_definitions_product
        FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE;
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
    media_side character varying(10) NULL,
    file_name character varying(255) NOT NULL,
    content_type character varying(100) NOT NULL,
    data_url text NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'PENDING',
    file_size_bytes bigint NULL,
    uploaded_by character varying(100) NULL,
    uploaded_at timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_customer_media_assets_customer_slot
    ON customer_media_assets (customer_id, media_type, media_side, uploaded_at DESC);");

        await context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_customer_media_assets_customer') THEN
        ALTER TABLE customer_media_assets ADD CONSTRAINT fk_customer_media_assets_customer
        FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE;
    END IF;
END $$;");
    }
}



