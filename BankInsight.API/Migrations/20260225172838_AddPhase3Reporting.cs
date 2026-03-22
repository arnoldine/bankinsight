using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase3Reporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_extracts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    extract_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    extract_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    extract_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_extracts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "regulatory_returns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    return_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reporting_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reporting_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submission_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    submission_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bog_reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    submitted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_records = table.Column<int>(type: "integer", nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    validation_errors = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regulatory_returns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_definitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    report_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    report_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_source = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_content = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_parameters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_definition_id = table.Column<int>(type: "integer", nullable: false),
                    parameter_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parameter_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    default_value = table.Column<string>(type: "text", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    display_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_parameters", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_parameters_report_definitions_report_definition_id",
                        column: x => x.report_definition_id,
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_runs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_definition_id = table.Column<int>(type: "integer", nullable: false),
                    schedule_id = table.Column<int>(type: "integer", nullable: true),
                    run_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: false),
                    execution_time_ms = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_runs_report_definitions_report_definition_id",
                        column: x => x.report_definition_id,
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_schedules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_definition_id = table.Column<int>(type: "integer", nullable: false),
                    schedule_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    day_of_month = table.Column<int>(type: "integer", nullable: true),
                    time_of_day = table.Column<TimeSpan>(type: "interval", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hangfire_job_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_schedules_report_definitions_report_definition_id",
                        column: x => x.report_definition_id,
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_definition_id = table.Column<int>(type: "integer", nullable: false),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    delivery_frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_subscriptions_report_definitions_report_definition_id",
                        column: x => x.report_definition_id,
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_parameters_report_definition_id",
                table: "report_parameters",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_runs_report_definition_id",
                table: "report_runs",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_schedules_report_definition_id",
                table: "report_schedules",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_subscriptions_report_definition_id",
                table: "report_subscriptions",
                column: "report_definition_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_extracts");

            migrationBuilder.DropTable(
                name: "regulatory_returns");

            migrationBuilder.DropTable(
                name: "report_parameters");

            migrationBuilder.DropTable(
                name: "report_runs");

            migrationBuilder.DropTable(
                name: "report_schedules");

            migrationBuilder.DropTable(
                name: "report_subscriptions");

            migrationBuilder.DropTable(
                name: "report_definitions");
        }
    }
}
