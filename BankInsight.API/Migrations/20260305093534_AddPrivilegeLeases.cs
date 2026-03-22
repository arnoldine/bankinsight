using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivilegeLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "privilege_leases",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    permission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privilege_leases", x => x.id);
                    table.ForeignKey(
                        name: "FK_privilege_leases_staff_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_privilege_leases_staff_id_permission_expires_at",
                table: "privilege_leases",
                columns: new[] { "staff_id", "permission", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "privilege_leases");
        }
    }
}
