using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HybridTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'wallets'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'wallets' AND column_name = 'Version'
    ) THEN
        ALTER TABLE wallets ADD "Version" integer NOT NULL DEFAULT 0;
    END IF;
END $$;
""");

            migrationBuilder.Sql("""
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'transfer_orders'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'transfer_orders' AND column_name = 'Version'
    ) THEN
        ALTER TABLE transfer_orders ADD "Version" integer NOT NULL DEFAULT 0;
    END IF;
END $$;
""");

            migrationBuilder.Sql("""
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'journal_entries'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'journal_entries' AND column_name = 'TransferOrderId'
    ) THEN
        ALTER TABLE journal_entries ADD "TransferOrderId" uuid NULL;
    END IF;
END $$;
""");

            migrationBuilder.Sql("""
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'journal_entries'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'journal_entries' AND column_name = 'Version'
    ) THEN
        ALTER TABLE journal_entries ADD "Version" integer NOT NULL DEFAULT 0;
    END IF;
END $$;
""");

            migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS audit_events (
    "Id" uuid NOT NULL,
    "ActorId" character varying(128) NOT NULL,
    "ActorType" character varying(32) NOT NULL,
    "Action" character varying(128) NOT NULL,
    "EntityType" character varying(128) NOT NULL,
    "EntityId" character varying(128) NOT NULL,
    "BeforeJson" text,
    "AfterJson" text,
    "IpAddress" character varying(64),
    "DeviceId" character varying(128),
    "TraceId" character varying(128),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_audit_events" PRIMARY KEY ("Id")
);
""");

            migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS webhook_receipts (
    "Id" uuid NOT NULL,
    "ProviderCode" character varying(64) NOT NULL,
    "ProviderReference" character varying(128) NOT NULL,
    "PayloadHash" character varying(128) NOT NULL,
    "EventType" character varying(64) NOT NULL,
    "ProcessedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_webhook_receipts" PRIMARY KEY ("Id")
);
""");

            migrationBuilder.Sql("""
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'journal_entries' AND column_name = 'ReversalOfJournalEntryId'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public' AND indexname = 'IX_journal_entries_ReversalOfJournalEntryId'
    ) THEN
        CREATE INDEX "IX_journal_entries_ReversalOfJournalEntryId"
        ON journal_entries ("ReversalOfJournalEntryId");
    END IF;
END $$;
""");

            migrationBuilder.Sql("""
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'journal_entries' AND column_name = 'TransferOrderId'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public' AND indexname = 'IX_journal_entries_TransferOrderId'
    ) THEN
        CREATE INDEX "IX_journal_entries_TransferOrderId"
        ON journal_entries ("TransferOrderId");
    END IF;
END $$;
""");

            migrationBuilder.Sql("""
CREATE INDEX IF NOT EXISTS "IX_audit_events_EntityType_EntityId_CreatedAtUtc"
ON audit_events ("EntityType", "EntityId", "CreatedAtUtc");
""");

            migrationBuilder.Sql("""
CREATE UNIQUE INDEX IF NOT EXISTS "IX_webhook_receipts_ProviderCode_ProviderReference_PayloadHash"
ON webhook_receipts ("ProviderCode", "ProviderReference", "PayloadHash");
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "webhook_receipts");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_ReversalOfJournalEntryId",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_TransferOrderId",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "wallets");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "transfer_orders");

            migrationBuilder.DropColumn(
                name: "TransferOrderId",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "journal_entries");
        }
    }
}
