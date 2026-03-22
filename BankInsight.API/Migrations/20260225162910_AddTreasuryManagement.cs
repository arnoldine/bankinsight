using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasuryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fx_rates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    target_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    buy_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    sell_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    mid_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    official_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    rate_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fx_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fx_trades",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    deal_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    trade_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    value_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    trade_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    base_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    counter_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    counter_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    customer_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    spread = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    counterparty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    settlement_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    initiated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    profit_loss = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fx_trades", x => x.id);
                    table.ForeignKey(
                        name: "FK_fx_trades_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fx_trades_staff_approved_by",
                        column: x => x.approved_by,
                        principalTable: "staff",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fx_trades_staff_initiated_by",
                        column: x => x.initiated_by,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "investments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    investment_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    investment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    instrument = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    counterparty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    interest_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    discount_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    placement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    maturity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenor_days = table.Column<int>(type: "integer", nullable: false),
                    interest_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    maturity_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    purchase_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    yield_to_maturity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rollover_to = table.Column<int>(type: "integer", nullable: true),
                    initiated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    matured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    settlement_account = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    accrued_interest = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    last_accrual_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investments", x => x.id);
                    table.ForeignKey(
                        name: "FK_investments_investments_rollover_to",
                        column: x => x.rollover_to,
                        principalTable: "investments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_investments_staff_approved_by",
                        column: x => x.approved_by,
                        principalTable: "staff",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_investments_staff_initiated_by",
                        column: x => x.initiated_by,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    metric_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metric_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    metric_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    threshold = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    threshold_breached = table.Column<bool>(type: "boolean", nullable: false),
                    confidence_level = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    time_horizon_days = table.Column<int>(type: "integer", nullable: true),
                    calculation_method = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    position_snapshot = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    exposure_details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    calculated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    alert_triggered = table.Column<bool>(type: "boolean", nullable: false),
                    alert_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_metrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_risk_metrics_staff_calculated_by",
                        column: x => x.calculated_by,
                        principalTable: "staff",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_risk_metrics_staff_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "treasury_positions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    position_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    opening_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    deposits = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    withdrawals = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fx_gains_losses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    other_movements = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    closing_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    nostro_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    vault_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    overnight_placement = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    exposure_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    position_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reconciled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reconciled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treasury_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_treasury_positions_staff_reconciled_by",
                        column: x => x.reconciled_by,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_fx_trades_approved_by",
                table: "fx_trades",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_fx_trades_customer_id",
                table: "fx_trades",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_fx_trades_initiated_by",
                table: "fx_trades",
                column: "initiated_by");

            migrationBuilder.CreateIndex(
                name: "IX_investments_approved_by",
                table: "investments",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_investments_initiated_by",
                table: "investments",
                column: "initiated_by");

            migrationBuilder.CreateIndex(
                name: "IX_investments_rollover_to",
                table: "investments",
                column: "rollover_to");

            migrationBuilder.CreateIndex(
                name: "IX_risk_metrics_calculated_by",
                table: "risk_metrics",
                column: "calculated_by");

            migrationBuilder.CreateIndex(
                name: "IX_risk_metrics_reviewed_by",
                table: "risk_metrics",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_treasury_positions_reconciled_by",
                table: "treasury_positions",
                column: "reconciled_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fx_rates");

            migrationBuilder.DropTable(
                name: "fx_trades");

            migrationBuilder.DropTable(
                name: "investments");

            migrationBuilder.DropTable(
                name: "risk_metrics");

            migrationBuilder.DropTable(
                name: "treasury_positions");
        }
    }
}
