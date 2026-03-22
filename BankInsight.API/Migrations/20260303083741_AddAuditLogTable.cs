using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    old_values = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    new_values = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_staff_user_id",
                        column: x => x.user_id,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "loan_bog_classification",
                columns: table => new
                {
                    classification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    days_past_due = table.Column<int>(type: "integer", nullable: false),
                    classification = table.Column<int>(type: "integer", nullable: false),
                    outstanding_principal = table.Column<decimal>(type: "numeric", nullable: false),
                    outstanding_interest = table.Column<decimal>(type: "numeric", nullable: false),
                    provisioning_amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_bog_classification", x => x.classification_id);
                });

            migrationBuilder.CreateTable(
                name: "loan_repayment_behavior",
                columns: table => new
                {
                    repayment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    payment_source = table.Column<int>(type: "integer", nullable: false),
                    payment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_paid = table.Column<decimal>(type: "numeric", nullable: false),
                    principal_allocated = table.Column<decimal>(type: "numeric", nullable: false),
                    interest_allocated = table.Column<decimal>(type: "numeric", nullable: false),
                    penalty_allocated = table.Column<decimal>(type: "numeric", nullable: false),
                    fees_allocated = table.Column<decimal>(type: "numeric", nullable: false),
                    days_past_due_upon_payment = table.Column<int>(type: "integer", nullable: false),
                    is_first_payment_default = table.Column<bool>(type: "boolean", nullable: false),
                    late_pay_trend_score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_repayment_behavior", x => x.repayment_id);
                });

            migrationBuilder.CreateTable(
                name: "teller_session",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    mid_day_cash_limit = table.Column<decimal>(type: "numeric", nullable: false),
                    opening_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    closing_balance = table.Column<decimal>(type: "numeric", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teller_session", x => x.session_id);
                });

            migrationBuilder.CreateTable(
                name: "vault_ledger",
                columns: table => new
                {
                    ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    debit_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    narration = table.Column<string>(type: "text", nullable: true),
                    reference_number = table.Column<string>(type: "text", nullable: false),
                    maker_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checker_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vault_ledger", x => x.ledger_id);
                });

            migrationBuilder.CreateTable(
                name: "vault_transaction_denomination",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    denomination = table.Column<int>(type: "integer", nullable: false),
                    pieces = table.Column<int>(type: "integer", nullable: false),
                    total_value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vault_transaction_denomination", x => x.id);
                    table.ForeignKey(
                        name: "FK_vault_transaction_denomination_vault_ledger_ledger_id",
                        column: x => x.ledger_id,
                        principalTable: "vault_ledger",
                        principalColumn: "ledger_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_vault_transaction_denomination_ledger_id",
                table: "vault_transaction_denomination",
                column: "ledger_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "loan_bog_classification");

            migrationBuilder.DropTable(
                name: "loan_repayment_behavior");

            migrationBuilder.DropTable(
                name: "teller_session");

            migrationBuilder.DropTable(
                name: "vault_transaction_denomination");

            migrationBuilder.DropTable(
                name: "vault_ledger");
        }
    }
}
