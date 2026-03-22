using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class ExtendLoansModuleProductionCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_reversal",
                table: "loan_repayments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "reversal_reference",
                table: "loan_repayments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inquiry_reference",
                table: "credit_bureau_checks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_timeout",
                table: "credit_bureau_checks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "credit_bureau_checks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "recommendation",
                table: "credit_bureau_checks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_payload",
                table: "credit_bureau_checks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retry_count",
                table: "credit_bureau_checks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "risk_grade",
                table: "credit_bureau_checks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "credit_bureau_checks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "loan_accounting_profiles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    loan_product_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    loan_portfolio_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    interest_income_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    processing_fee_income_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    penalty_income_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    interest_receivable_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    penalty_receivable_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    impairment_expense_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    impairment_allowance_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recovery_income_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    disbursement_funding_gl = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    repayment_allocation_order = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_accounting_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_accounting_profiles_loan_products_loan_product_id",
                        column: x => x.loan_product_id,
                        principalTable: "loan_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_accounts",
                columns: table => new
                {
                    loan_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    appraisal_status = table.Column<int>(type: "integer", nullable: false),
                    appraisal_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    appraised_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    appraised_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_non_accrual = table.Column<bool>(type: "boolean", nullable: false),
                    is_suspended_interest = table.Column<bool>(type: "boolean", nullable: false),
                    delinquency_days = table.Column<int>(type: "integer", nullable: false),
                    arrears_bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    exposure_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    concentration_group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_accounts", x => x.loan_id);
                    table.ForeignKey(
                        name: "FK_loan_accounts_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_accruals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    accrual_date = table.Column<DateOnly>(type: "date", nullable: false),
                    interest_accrued = table.Column<decimal>(type: "numeric", nullable: false),
                    penalty_accrued = table.Column<decimal>(type: "numeric", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    journal_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_accruals", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_accruals_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_disclosures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    disclosure_text = table.Column<string>(type: "text", nullable: false),
                    accepted = table.Column<bool>(type: "boolean", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_disclosures", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_disclosures_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_impairments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stage = table.Column<int>(type: "integer", nullable: false),
                    allowance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    impairment_expense = table.Column<decimal>(type: "numeric", nullable: false),
                    is_written_off = table.Column<bool>(type: "boolean", nullable: false),
                    written_off_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recovery_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_impairments", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_impairments_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credit_bureau_checks_provider_name_inquiry_reference",
                table: "credit_bureau_checks",
                columns: new[] { "provider_name", "inquiry_reference" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_accounting_profiles_loan_product_id",
                table: "loan_accounting_profiles",
                column: "loan_product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_accounts_branch_id_delinquency_days",
                table: "loan_accounts",
                columns: new[] { "branch_id", "delinquency_days" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_accruals_loan_id_accrual_date",
                table: "loan_accruals",
                columns: new[] { "loan_id", "accrual_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_disclosures_loan_id_accepted",
                table: "loan_disclosures",
                columns: new[] { "loan_id", "accepted" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_impairments_loan_id_created_at",
                table: "loan_impairments",
                columns: new[] { "loan_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_accounting_profiles");

            migrationBuilder.DropTable(
                name: "loan_accounts");

            migrationBuilder.DropTable(
                name: "loan_accruals");

            migrationBuilder.DropTable(
                name: "loan_disclosures");

            migrationBuilder.DropTable(
                name: "loan_impairments");

            migrationBuilder.DropIndex(
                name: "IX_credit_bureau_checks_provider_name_inquiry_reference",
                table: "credit_bureau_checks");

            migrationBuilder.DropColumn(
                name: "is_reversal",
                table: "loan_repayments");

            migrationBuilder.DropColumn(
                name: "reversal_reference",
                table: "loan_repayments");

            migrationBuilder.DropColumn(
                name: "inquiry_reference",
                table: "credit_bureau_checks");

            migrationBuilder.DropColumn(
                name: "is_timeout",
                table: "credit_bureau_checks");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "credit_bureau_checks");

            migrationBuilder.DropColumn(
                name: "recommendation",
                table: "credit_bureau_checks");

            migrationBuilder.DropColumn(
                name: "request_payload",
                table: "credit_bureau_checks");

            migrationBuilder.DropColumn(
                name: "retry_count",
                table: "credit_bureau_checks");

            migrationBuilder.DropColumn(
                name: "risk_grade",
                table: "credit_bureau_checks");

            migrationBuilder.DropColumn(
                name: "status",
                table: "credit_bureau_checks");
        }
    }
}
