using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Migrations
{
    public partial class AddInterBranchTransferCustodyLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "dispatched_at",
                table: "inter_branch_transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "received_by",
                table: "inter_branch_transfers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "inter_branch_transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sent_by",
                table: "inter_branch_transfers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_inter_branch_transfers_received_by",
                table: "inter_branch_transfers",
                column: "received_by");

            migrationBuilder.CreateIndex(
                name: "IX_inter_branch_transfers_sent_by",
                table: "inter_branch_transfers",
                column: "sent_by");

            migrationBuilder.AddForeignKey(
                name: "FK_inter_branch_transfers_staff_received_by",
                table: "inter_branch_transfers",
                column: "received_by",
                principalTable: "staff",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inter_branch_transfers_staff_sent_by",
                table: "inter_branch_transfers",
                column: "sent_by",
                principalTable: "staff",
                principalColumn: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inter_branch_transfers_staff_received_by",
                table: "inter_branch_transfers");

            migrationBuilder.DropForeignKey(
                name: "FK_inter_branch_transfers_staff_sent_by",
                table: "inter_branch_transfers");

            migrationBuilder.DropIndex(
                name: "IX_inter_branch_transfers_received_by",
                table: "inter_branch_transfers");

            migrationBuilder.DropIndex(
                name: "IX_inter_branch_transfers_sent_by",
                table: "inter_branch_transfers");

            migrationBuilder.DropColumn(
                name: "dispatched_at",
                table: "inter_branch_transfers");

            migrationBuilder.DropColumn(
                name: "received_by",
                table: "inter_branch_transfers");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "inter_branch_transfers");

            migrationBuilder.DropColumn(
                name: "sent_by",
                table: "inter_branch_transfers");
        }
    }
}
