using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HybridTransfer.Infrastructure.Migrations;

public partial class InitialOperationalPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "alerts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                AlertCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Score = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_alerts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "journal_entries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ValueDate = table.Column<DateOnly>(type: "date", nullable: false),
                BookingDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                SourceModule = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExternalReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ReversalOfJournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_journal_entries", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "reconciliation_items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReconciliationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExternalReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                InternalReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                DetectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reconciliation_items", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "transfer_orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TransferType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                FundingSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                BeneficiaryId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                DestinationDetails = table.Column<string>(type: "jsonb", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                FxRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RiskStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ComplianceStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                PartnerReference = table.Column<string>(type: "text", nullable: true),
                FailureReason = table.Column<string>(type: "text", nullable: true),
                CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ApprovedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_transfer_orders", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "wallets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                WalletType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                AvailableBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                ReservedBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                LiabilityLedgerAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wallets", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "approval_requests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TransferOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                ActionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RequestedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ApprovedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_approval_requests", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "journal_lines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                LedgerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                ExchangeRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                Narrative = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_journal_lines", x => x.Id);
                table.ForeignKey(
                    name: "FK_journal_lines_journal_entries_JournalEntryId",
                    column: x => x.JournalEntryId,
                    principalTable: "journal_entries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_alerts_CustomerId", table: "alerts", column: "CustomerId");
        migrationBuilder.CreateIndex(name: "IX_alerts_Status", table: "alerts", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_approval_requests_Status", table: "approval_requests", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_approval_requests_TransferOrderId", table: "approval_requests", column: "TransferOrderId");
        migrationBuilder.CreateIndex(name: "IX_journal_entries_IdempotencyKey", table: "journal_entries", column: "IdempotencyKey", unique: true);
        migrationBuilder.CreateIndex(name: "IX_journal_lines_JournalEntryId", table: "journal_lines", column: "JournalEntryId");
        migrationBuilder.CreateIndex(name: "IX_journal_lines_LedgerAccountId", table: "journal_lines", column: "LedgerAccountId");
        migrationBuilder.CreateIndex(name: "IX_reconciliation_items_Status", table: "reconciliation_items", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_transfer_orders_IdempotencyKey", table: "transfer_orders", column: "IdempotencyKey", unique: true);
        migrationBuilder.CreateIndex(name: "IX_transfer_orders_Status", table: "transfer_orders", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_wallets_CustomerId_WalletType_Currency", table: "wallets", columns: new[] { "CustomerId", "WalletType", "Currency" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "alerts");
        migrationBuilder.DropTable(name: "approval_requests");
        migrationBuilder.DropTable(name: "journal_lines");
        migrationBuilder.DropTable(name: "reconciliation_items");
        migrationBuilder.DropTable(name: "transfer_orders");
        migrationBuilder.DropTable(name: "wallets");
        migrationBuilder.DropTable(name: "journal_entries");
    }
}
