using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class ExtendLoansModuleWorkflowAndCredit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "application_date",
                table: "loans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approved_by",
                table: "loans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "checker_id",
                table: "loans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "disbursed_at",
                table: "loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "interest_method",
                table: "loans",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "loan_product_id",
                table: "loans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "maker_id",
                table: "loans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repayment_frequency",
                table: "loans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "schedule_type",
                table: "loans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "paid_amount",
                table: "loan_schedules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "paid_date",
                table: "loan_schedules",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "credit_bureau_checks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bureau_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    risk_band = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    raw_response = table.Column<string>(type: "jsonb", nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_bureau_checks", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_bureau_checks_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "loan_products",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    product_type = table.Column<int>(type: "integer", nullable: false),
                    interest_method = table.Column<int>(type: "integer", nullable: false),
                    repayment_frequency = table.Column<int>(type: "integer", nullable: false),
                    term_in_periods = table.Column<int>(type: "integer", nullable: false),
                    annual_interest_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    min_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    max_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loan_repayments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    repayment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    principal_component = table.Column<decimal>(type: "numeric", nullable: false),
                    interest_component = table.Column<decimal>(type: "numeric", nullable: false),
                    penalty_component = table.Column<decimal>(type: "numeric", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    processed_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_repayments", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_repayments_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loans_loan_product_id",
                table: "loans",
                column: "loan_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_bureau_checks_customer_id_checked_at",
                table: "credit_bureau_checks",
                columns: new[] { "customer_id", "checked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_bureau_checks_loan_id",
                table: "credit_bureau_checks",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "IX_loan_products_code",
                table: "loan_products",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_repayments_loan_id",
                table: "loan_repayments",
                column: "loan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_loans_loan_products_loan_product_id",
                table: "loans",
                column: "loan_product_id",
                principalTable: "loan_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_loans_loan_products_loan_product_id",
                table: "loans");

            migrationBuilder.DropTable(
                name: "credit_bureau_checks");

            migrationBuilder.DropTable(
                name: "loan_products");

            migrationBuilder.DropTable(
                name: "loan_repayments");

            migrationBuilder.DropIndex(
                name: "IX_loans_loan_product_id",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "application_date",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "approved_by",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "checker_id",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "disbursed_at",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "interest_method",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "loan_product_id",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "maker_id",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "repayment_frequency",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "schedule_type",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "paid_amount",
                table: "loan_schedules");

            migrationBuilder.DropColumn(
                name: "paid_date",
                table: "loan_schedules");
        }
    }
}
