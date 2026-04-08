using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HybridTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferCorridorAndCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "transfer_orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinationCountryCode",
                table: "transfer_orders",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "transfer_orders");

            migrationBuilder.DropColumn(
                name: "DestinationCountryCode",
                table: "transfer_orders");
        }
    }
}
