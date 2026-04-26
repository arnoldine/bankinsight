using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Migrations
{
    public partial class AddClientChannelCustomerAuth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_credentials",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    login_email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    mfa_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_credentials", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_credentials_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_media_assets",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    media_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    media_side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data_url = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    uploaded_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_media_assets", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_media_assets_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_complaints",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    submitted_by_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    summary = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    details = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_team = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sla_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_complaints", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_complaints_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_kyc_cases",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reviewer_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reviewer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    decision_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_kyc_cases", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_kyc_cases_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_channel_sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_credential_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_activity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    logout_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_channel_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_channel_sessions_customer_credentials_customer_credential_id",
                        column: x => x.customer_credential_id,
                        principalTable: "customer_credentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_channel_sessions_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_complaint_attachments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    complaint_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data_url = table.Column<string>(type: "text", nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_complaint_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_complaint_attachments_client_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "client_complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_complaint_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    complaint_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actor_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    actor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_complaint_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_complaint_events_client_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "client_complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_kyc_case_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    kyc_case_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    actor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_kyc_case_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_kyc_case_events_client_kyc_cases_kyc_case_id",
                        column: x => x.kyc_case_id,
                        principalTable: "client_kyc_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_credentials_customer_id",
                table: "customer_credentials",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_media_assets_customer_id_media_type_media_side_uploaded_at",
                table: "customer_media_assets",
                columns: new[] { "customer_id", "media_type", "media_side", "uploaded_at" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_credentials_login_email",
                table: "customer_credentials",
                column: "login_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_complaints_customer_id_status_updated_at",
                table: "client_complaints",
                columns: new[] { "customer_id", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_client_complaints_reference",
                table: "client_complaints",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_channel_sessions_customer_credential_id",
                table: "client_channel_sessions",
                column: "customer_credential_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_channel_sessions_customer_id_is_active_last_activity",
                table: "client_channel_sessions",
                columns: new[] { "customer_id", "is_active", "last_activity" });

            migrationBuilder.CreateIndex(
                name: "IX_client_complaint_attachments_complaint_id_uploaded_at",
                table: "client_complaint_attachments",
                columns: new[] { "complaint_id", "uploaded_at" });

            migrationBuilder.CreateIndex(
                name: "IX_client_complaint_events_complaint_id_created_at",
                table: "client_complaint_events",
                columns: new[] { "complaint_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_client_kyc_cases_customer_id_status_updated_at",
                table: "client_kyc_cases",
                columns: new[] { "customer_id", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_client_kyc_cases_reference",
                table: "client_kyc_cases",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_kyc_case_events_kyc_case_id_created_at",
                table: "client_kyc_case_events",
                columns: new[] { "kyc_case_id", "created_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "client_kyc_case_events");
            migrationBuilder.DropTable(name: "client_channel_sessions");
            migrationBuilder.DropTable(name: "client_complaint_attachments");
            migrationBuilder.DropTable(name: "client_complaint_events");
            migrationBuilder.DropTable(name: "customer_media_assets");
            migrationBuilder.DropTable(name: "client_kyc_cases");
            migrationBuilder.DropTable(name: "customer_credentials");
            migrationBuilder.DropTable(name: "client_complaints");
        }
    }
}
