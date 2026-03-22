using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupLendingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_group_members",
                table: "group_members");

            migrationBuilder.AddColumn<bool>(
                name: "allow_batch_disbursement",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_member_level_disbursement_adjustment",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_reschedule_within_group",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_top_up_within_group",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "allowed_repayment_frequencies_json",
                table: "products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "arrears_eligibility_rule_type",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attendance_rule_type",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_repayment_frequency",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "graduated_cycle_limit_rules_json",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_delinquency_policy",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_guarantee_policy_type",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_officer_assignment_mode",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_penalty_policy",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_group_loan_enabled",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "lending_methodology",
                table: "products",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "loan_cycle_policy_type",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_cycle_number",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "maximum_group_size",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "meeting_collection_mode",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "minimum_group_size",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum_savings_to_loan_ratio",
                table: "products",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_center",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_compulsory_savings",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_group",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_group_approval_meeting",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "supports_joint_liability",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "supports_weekly_repayment",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "uses_group_level_approval",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "uses_member_level_underwriting",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "assigned_officer_id",
                table: "groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branch_id",
                table: "groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "center_id",
                table: "groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "chairperson_customer_id",
                table: "groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "groups",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "group_code",
                table: "groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_joint_liability_enabled",
                table: "groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_members",
                table: "groups",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "meeting_day_of_week",
                table: "groups",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "meeting_frequency",
                table: "groups",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "meeting_location",
                table: "groups",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "groups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secretary_customer_id",
                table: "groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "treasurer_customer_id",
                table: "groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "groups",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "id",
                table: "group_members",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE group_members
                SET id = CONCAT('GLM-', SUBSTRING(md5(COALESCE(group_id, '') || ':' || COALESCE(customer_id, '')) FROM 1 FOR 18))
                WHERE id = '';

                ALTER TABLE group_members ALTER COLUMN id DROP DEFAULT;
            ");

            migrationBuilder.AddColumn<bool>(
                name: "arrears_flag",
                table: "group_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "compulsory_savings_account_id",
                table: "group_members",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "current_exposure",
                table: "group_members",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "current_loan_cycle",
                table: "group_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "exit_date",
                table: "group_members",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "guarantor_indicator",
                table: "group_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_eligible_for_loan",
                table: "group_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_founding_member",
                table: "group_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "join_date",
                table: "group_members",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "kyc_status",
                table: "group_members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "member_no",
                table: "group_members",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "member_role",
                table: "group_members",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "share_contribution",
                table: "group_members",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "social_collateral_notes",
                table: "group_members",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "group_members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "voluntary_savings_account_id",
                table: "group_members",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_group_members",
                table: "group_members",
                column: "id");

            migrationBuilder.CreateTable(
                name: "cash_incidents",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    store_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    store_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    incident_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    narration = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reported_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    resolved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_incidents", x => x.id);
                    table.ForeignKey(
                        name: "FK_cash_incidents_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cash_incidents_staff_reported_by",
                        column: x => x.reported_by,
                        principalTable: "staff",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_cash_incidents_staff_resolved_by",
                        column: x => x.resolved_by,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "group_collection_batches",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_meeting_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    officer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    collection_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_collected_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_expected_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    variance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_collection_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_guarantee_links",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_loan_application_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    guarantee_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    liability_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    guarantee_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_guarantee_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_loan_accounts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    loan_account_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_loan_application_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    loan_cycle_no = table.Column<int>(type: "integer", nullable: false),
                    group_guarantee_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_under_joint_liability = table.Column<bool>(type: "boolean", nullable: false),
                    meeting_day_of_week = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    assigned_officer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    restructured_flag = table.Column<bool>(type: "boolean", nullable: false),
                    impairment_stage_hint = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_loan_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_loan_accounts_loan_accounts_loan_account_id",
                        column: x => x.loan_account_id,
                        principalTable: "loan_accounts",
                        principalColumn: "loan_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_loan_applications",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    loan_cycle_no = table.Column<int>(type: "integer", nullable: false),
                    application_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    product_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    officer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    total_approved_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_requested_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_disbursed_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    approval_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disbursement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    meeting_reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    group_resolution_reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    disclosed_terms_snapshot_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_loan_applications", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_loan_applications_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_loan_applications_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_loan_delinquency_snapshots",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    loan_account_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    snapshot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    days_past_due = table.Column<int>(type: "integer", nullable: false),
                    installments_in_arrears = table.Column<int>(type: "integer", nullable: false),
                    outstanding_principal = table.Column<decimal>(type: "numeric", nullable: false),
                    outstanding_interest = table.Column<decimal>(type: "numeric", nullable: false),
                    outstanding_penalty = table.Column<decimal>(type: "numeric", nullable: false),
                    par_bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_npl = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_loan_delinquency_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_meetings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    center_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    meeting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    meeting_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    officer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attendance_count = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_meetings", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_meetings_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lending_centers",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    center_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    center_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    meeting_day_of_week = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    meeting_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    assigned_officer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lending_centers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_eligibility_rules",
                columns: table => new
                {
                    product_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requires_kyc_complete = table.Column<bool>(type: "boolean", nullable: false),
                    block_on_severe_arrears = table.Column<bool>(type: "boolean", nullable: false),
                    max_allowed_exposure = table.Column<decimal>(type: "numeric", nullable: true),
                    min_membership_days = table.Column<int>(type: "integer", nullable: true),
                    min_attendance_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    require_credit_bureau_check = table.Column<bool>(type: "boolean", nullable: false),
                    credit_bureau_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    minimum_credit_score = table.Column<int>(type: "integer", nullable: true),
                    rule_json = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_eligibility_rules", x => x.product_id);
                    table.ForeignKey(
                        name: "FK_product_eligibility_rules_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_group_rules",
                columns: table => new
                {
                    product_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    min_members_required = table.Column<int>(type: "integer", nullable: false),
                    max_members_allowed = table.Column<int>(type: "integer", nullable: false),
                    min_weeks = table.Column<int>(type: "integer", nullable: true),
                    max_weeks = table.Column<int>(type: "integer", nullable: true),
                    requires_compulsory_savings = table.Column<bool>(type: "boolean", nullable: false),
                    min_savings_to_loan_ratio = table.Column<decimal>(type: "numeric", nullable: true),
                    requires_group_approval_meeting = table.Column<bool>(type: "boolean", nullable: false),
                    requires_joint_liability = table.Column<bool>(type: "boolean", nullable: false),
                    allow_top_up = table.Column<bool>(type: "boolean", nullable: false),
                    allow_reschedule = table.Column<bool>(type: "boolean", nullable: false),
                    max_cycle_number = table.Column<int>(type: "integer", nullable: true),
                    cycle_increment_rules_json = table.Column<string>(type: "text", nullable: true),
                    default_repayment_frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    default_interest_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    penalty_policy_json = table.Column<string>(type: "text", nullable: true),
                    attendance_rule_json = table.Column<string>(type: "text", nullable: true),
                    eligibility_rule_json = table.Column<string>(type: "text", nullable: true),
                    meeting_collection_rule_json = table.Column<string>(type: "text", nullable: true),
                    allocation_order_json = table.Column<string>(type: "text", nullable: true),
                    accounting_profile_json = table.Column<string>(type: "text", nullable: true),
                    disclosure_template = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_group_rules", x => x.product_id);
                    table.ForeignKey(
                        name: "FK_product_group_rules_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_collection_batch_lines",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    batch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    loan_account_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expected_installment = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_collected = table.Column<decimal>(type: "numeric", nullable: false),
                    principal_component = table.Column<decimal>(type: "numeric", nullable: false),
                    interest_component = table.Column<decimal>(type: "numeric", nullable: false),
                    penalty_component = table.Column<decimal>(type: "numeric", nullable: false),
                    savings_component = table.Column<decimal>(type: "numeric", nullable: false),
                    fee_component = table.Column<decimal>(type: "numeric", nullable: false),
                    arrears_recovered = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_collection_batch_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_collection_batch_lines_group_collection_batches_batch~",
                        column: x => x.batch_id,
                        principalTable: "group_collection_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_loan_application_members",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_loan_application_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requested_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    approved_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    disbursed_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tenure_weeks = table.Column<int>(type: "integer", nullable: false),
                    interest_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    interest_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    repayment_frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    loan_purpose = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    score_result = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    eligibility_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    credit_bureau_check_id = table.Column<Guid>(type: "uuid", nullable: true),
                    existing_exposure_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    savings_balance_at_application = table.Column<decimal>(type: "numeric", nullable: false),
                    guarantor_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_loan_application_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_loan_application_members_group_loan_applications_grou~",
                        column: x => x.group_loan_application_id,
                        principalTable: "group_loan_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_loan_application_members_group_members_group_member_id",
                        column: x => x.group_member_id,
                        principalTable: "group_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_meeting_attendance",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_meeting_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    attendance_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    arrival_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_meeting_attendance", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_meeting_attendance_group_meetings_group_meeting_id",
                        column: x => x.group_meeting_id,
                        principalTable: "group_meetings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_groups_assigned_officer_id",
                table: "groups",
                column: "assigned_officer_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_center_id",
                table: "groups",
                column: "center_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_group_code",
                table: "groups",
                column: "group_code");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_group_id_customer_id",
                table: "group_members",
                columns: new[] { "group_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_incidents_branch_id",
                table: "cash_incidents",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_incidents_reported_by",
                table: "cash_incidents",
                column: "reported_by");

            migrationBuilder.CreateIndex(
                name: "IX_cash_incidents_resolved_by",
                table: "cash_incidents",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "IX_group_collection_batch_lines_batch_id",
                table: "group_collection_batch_lines",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_loan_accounts_loan_account_id",
                table: "group_loan_accounts",
                column: "loan_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_loan_application_members_group_loan_application_id",
                table: "group_loan_application_members",
                column: "group_loan_application_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_loan_application_members_group_member_id",
                table: "group_loan_application_members",
                column: "group_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_loan_applications_group_id",
                table: "group_loan_applications",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_loan_applications_product_id",
                table: "group_loan_applications",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_loan_delinquency_snapshots_loan_account_id_snapshot_d~",
                table: "group_loan_delinquency_snapshots",
                columns: new[] { "loan_account_id", "snapshot_date" });

            migrationBuilder.CreateIndex(
                name: "IX_group_meeting_attendance_group_meeting_id",
                table: "group_meeting_attendance",
                column: "group_meeting_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_meetings_group_id",
                table: "group_meetings",
                column: "group_id");

            migrationBuilder.AddForeignKey(
                name: "FK_groups_lending_centers_center_id",
                table: "groups",
                column: "center_id",
                principalTable: "lending_centers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_groups_staff_assigned_officer_id",
                table: "groups",
                column: "assigned_officer_id",
                principalTable: "staff",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_groups_lending_centers_center_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "FK_groups_staff_assigned_officer_id",
                table: "groups");

            migrationBuilder.DropTable(
                name: "cash_incidents");

            migrationBuilder.DropTable(
                name: "group_collection_batch_lines");

            migrationBuilder.DropTable(
                name: "group_guarantee_links");

            migrationBuilder.DropTable(
                name: "group_loan_accounts");

            migrationBuilder.DropTable(
                name: "group_loan_application_members");

            migrationBuilder.DropTable(
                name: "group_loan_delinquency_snapshots");

            migrationBuilder.DropTable(
                name: "group_meeting_attendance");

            migrationBuilder.DropTable(
                name: "lending_centers");

            migrationBuilder.DropTable(
                name: "product_eligibility_rules");

            migrationBuilder.DropTable(
                name: "product_group_rules");

            migrationBuilder.DropTable(
                name: "group_collection_batches");

            migrationBuilder.DropTable(
                name: "group_loan_applications");

            migrationBuilder.DropTable(
                name: "group_meetings");

            migrationBuilder.DropIndex(
                name: "IX_groups_assigned_officer_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "IX_groups_center_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "IX_groups_group_code",
                table: "groups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_group_members",
                table: "group_members");

            migrationBuilder.DropIndex(
                name: "IX_group_members_group_id_customer_id",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "allow_batch_disbursement",
                table: "products");

            migrationBuilder.DropColumn(
                name: "allow_member_level_disbursement_adjustment",
                table: "products");

            migrationBuilder.DropColumn(
                name: "allow_reschedule_within_group",
                table: "products");

            migrationBuilder.DropColumn(
                name: "allow_top_up_within_group",
                table: "products");

            migrationBuilder.DropColumn(
                name: "allowed_repayment_frequencies_json",
                table: "products");

            migrationBuilder.DropColumn(
                name: "arrears_eligibility_rule_type",
                table: "products");

            migrationBuilder.DropColumn(
                name: "attendance_rule_type",
                table: "products");

            migrationBuilder.DropColumn(
                name: "default_repayment_frequency",
                table: "products");

            migrationBuilder.DropColumn(
                name: "graduated_cycle_limit_rules_json",
                table: "products");

            migrationBuilder.DropColumn(
                name: "group_delinquency_policy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "group_guarantee_policy_type",
                table: "products");

            migrationBuilder.DropColumn(
                name: "group_officer_assignment_mode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "group_penalty_policy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_group_loan_enabled",
                table: "products");

            migrationBuilder.DropColumn(
                name: "lending_methodology",
                table: "products");

            migrationBuilder.DropColumn(
                name: "loan_cycle_policy_type",
                table: "products");

            migrationBuilder.DropColumn(
                name: "max_cycle_number",
                table: "products");

            migrationBuilder.DropColumn(
                name: "maximum_group_size",
                table: "products");

            migrationBuilder.DropColumn(
                name: "meeting_collection_mode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "minimum_group_size",
                table: "products");

            migrationBuilder.DropColumn(
                name: "minimum_savings_to_loan_ratio",
                table: "products");

            migrationBuilder.DropColumn(
                name: "requires_center",
                table: "products");

            migrationBuilder.DropColumn(
                name: "requires_compulsory_savings",
                table: "products");

            migrationBuilder.DropColumn(
                name: "requires_group",
                table: "products");

            migrationBuilder.DropColumn(
                name: "requires_group_approval_meeting",
                table: "products");

            migrationBuilder.DropColumn(
                name: "supports_joint_liability",
                table: "products");

            migrationBuilder.DropColumn(
                name: "supports_weekly_repayment",
                table: "products");

            migrationBuilder.DropColumn(
                name: "uses_group_level_approval",
                table: "products");

            migrationBuilder.DropColumn(
                name: "uses_member_level_underwriting",
                table: "products");

            migrationBuilder.DropColumn(
                name: "assigned_officer_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "center_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "chairperson_customer_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "group_code",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "is_joint_liability_enabled",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "max_members",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "meeting_day_of_week",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "meeting_frequency",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "meeting_location",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "secretary_customer_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "treasurer_customer_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "id",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "arrears_flag",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "compulsory_savings_account_id",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "current_exposure",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "current_loan_cycle",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "exit_date",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "guarantor_indicator",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "is_eligible_for_loan",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "is_founding_member",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "join_date",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "kyc_status",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "member_no",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "member_role",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "share_contribution",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "social_collateral_notes",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "status",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "voluntary_savings_account_id",
                table: "group_members");

            migrationBuilder.AddPrimaryKey(
                name: "PK_group_members",
                table: "group_members",
                columns: new[] { "group_id", "customer_id" });
        }
    }
}




