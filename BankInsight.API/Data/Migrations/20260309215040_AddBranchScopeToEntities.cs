using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchScopeToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_staff_roles_role_id",
                table: "staff");

            migrationBuilder.DropIndex(
                name: "IX_staff_role_id",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "permissions",
                table: "roles");

            migrationBuilder.AddColumn<int>(
                name: "access_scope_type",
                table: "staff",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_system_role",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "branch_id",
                table: "loans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "branch_id",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "audit_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_success",
                table: "audit_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "payload_json",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at_utc",
                table: "approval_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_user_id",
                table: "approval_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payload_json",
                table: "approval_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_no",
                table: "approval_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "remarks",
                table: "approval_requests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_system_permission = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_staff_user_id",
                        column: x => x.user_id,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropColumn(
                name: "access_scope_type",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_system_role",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "is_success",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "payload_json",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "approved_at_utc",
                table: "approval_requests");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "approval_requests");

            migrationBuilder.DropColumn(
                name: "payload_json",
                table: "approval_requests");

            migrationBuilder.DropColumn(
                name: "reference_no",
                table: "approval_requests");

            migrationBuilder.DropColumn(
                name: "remarks",
                table: "approval_requests");

            migrationBuilder.AddColumn<string>(
                name: "role_id",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "permissions",
                table: "roles",
                type: "text[]",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_role_id",
                table: "staff",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_staff_roles_role_id",
                table: "staff",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id");
        }
    }
}
