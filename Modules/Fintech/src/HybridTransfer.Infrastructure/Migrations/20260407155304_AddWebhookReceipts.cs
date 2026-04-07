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
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "wallets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "transfer_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferOrderId",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "journal_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_receipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_ReversalOfJournalEntryId",
                table: "journal_entries",
                column: "ReversalOfJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_TransferOrderId",
                table: "journal_entries",
                column: "TransferOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_EntityType_EntityId_CreatedAtUtc",
                table: "audit_events",
                columns: new[] { "EntityType", "EntityId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_receipts_ProviderCode_ProviderReference_PayloadHash",
                table: "webhook_receipts",
                columns: new[] { "ProviderCode", "ProviderReference", "PayloadHash" },
                unique: true);
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
