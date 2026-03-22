using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffClerkFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "clerk_user_id",
                table: "staff",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "staff",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "clerk_user_id",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "staff");
        }
    }
}
