using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Data;

public static class DatabaseSchemaBootstrapper
{
    public static async Task EnsureAsync(ApplicationDbContext context)
    {
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
    }
}



