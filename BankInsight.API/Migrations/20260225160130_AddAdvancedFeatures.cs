using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    config_value = table.Column<string>(type: "text", nullable: true),
                    data_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_configs_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_hierarchy",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parent_branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_hierarchy", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_hierarchy_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_hierarchy_branches_parent_branch_id",
                        column: x => x.parent_branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "branch_limits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    limit_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    single_transaction_limit = table.Column<decimal>(type: "numeric", nullable: true),
                    daily_limit = table.Column<decimal>(type: "numeric", nullable: true),
                    monthly_limit = table.Column<decimal>(type: "numeric", nullable: true),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    approval_threshold = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_limits", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_limits_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_vaults",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    cash_on_hand = table.Column<decimal>(type: "numeric", nullable: false),
                    vault_limit = table.Column<decimal>(type: "numeric", nullable: true),
                    min_balance = table.Column<decimal>(type: "numeric", nullable: true),
                    last_count_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_count_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_vaults", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_vaults_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_vaults_staff_last_count_by",
                        column: x => x.last_count_by,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "inter_branch_transfers",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    from_branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    to_branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    initiated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inter_branch_transfers", x => x.id);
                    table.ForeignKey(
                        name: "FK_inter_branch_transfers_branches_from_branch_id",
                        column: x => x.from_branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inter_branch_transfers_branches_to_branch_id",
                        column: x => x.to_branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inter_branch_transfers_staff_approved_by",
                        column: x => x.approved_by,
                        principalTable: "staff",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_inter_branch_transfers_staff_initiated_by",
                        column: x => x.initiated_by,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "login_attempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_login_attempts_staff_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_activity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    logout_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_sessions_staff_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_activities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    before_value = table.Column<string>(type: "text", nullable: true),
                    after_value = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_activities_staff_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_activities_user_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "user_sessions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_configs_branch_id",
                table: "branch_configs",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_hierarchy_branch_id",
                table: "branch_hierarchy",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_hierarchy_parent_branch_id",
                table: "branch_hierarchy",
                column: "parent_branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_limits_branch_id",
                table: "branch_limits",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_vaults_branch_id",
                table: "branch_vaults",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_vaults_last_count_by",
                table: "branch_vaults",
                column: "last_count_by");

            migrationBuilder.CreateIndex(
                name: "IX_inter_branch_transfers_approved_by",
                table: "inter_branch_transfers",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_inter_branch_transfers_from_branch_id",
                table: "inter_branch_transfers",
                column: "from_branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_inter_branch_transfers_initiated_by",
                table: "inter_branch_transfers",
                column: "initiated_by");

            migrationBuilder.CreateIndex(
                name: "IX_inter_branch_transfers_to_branch_id",
                table: "inter_branch_transfers",
                column: "to_branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_attempts_staff_id",
                table: "login_attempts",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_activities_session_id",
                table: "user_activities",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_activities_staff_id",
                table: "user_activities",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_staff_id",
                table: "user_sessions",
                column: "staff_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_configs");

            migrationBuilder.DropTable(
                name: "branch_hierarchy");

            migrationBuilder.DropTable(
                name: "branch_limits");

            migrationBuilder.DropTable(
                name: "branch_vaults");

            migrationBuilder.DropTable(
                name: "inter_branch_transfers");

            migrationBuilder.DropTable(
                name: "login_attempts");

            migrationBuilder.DropTable(
                name: "user_activities");

            migrationBuilder.DropTable(
                name: "user_sessions");
        }
    }
}
