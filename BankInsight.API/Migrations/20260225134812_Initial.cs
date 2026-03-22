using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    secondary_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    digital_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_address = table.Column<string>(type: "text", nullable: true),
                    kyc_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    risk_rating = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    ghana_card = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    nationality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    marital_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    spouse_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    employer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    job_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ssnit_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    business_reg_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    registration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    tin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sector = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    legal_form = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gl_accounts",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    balance = table.Column<decimal>(type: "numeric", nullable: false),
                    is_header = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gl_accounts", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    interest_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    interest_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    min_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    max_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    min_term = table.Column<int>(type: "integer", nullable: true),
                    max_term = table.Column<int>(type: "integer", nullable: true),
                    default_term = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    permissions = table.Column<string[]>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_config",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    trigger_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    steps = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    balance = table.Column<decimal>(type: "numeric", nullable: false),
                    lien_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_trans_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_accounts_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_accounts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_accounts_products_product_code",
                        column: x => x.product_code,
                        principalTable: "products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    branch_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    avatar_initials = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_staff_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "approval_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    workflow_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requester_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_step = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_approval_requests_staff_requester_id",
                        column: x => x.requester_id,
                        principalTable: "staff",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_approval_requests_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    officer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    meeting_day = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    formation_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_groups_staff_officer_id",
                        column: x => x.officer_id,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_entries_staff_posted_by",
                        column: x => x.posted_by,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    narration = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    teller_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transactions_staff_teller_id",
                        column: x => x.teller_id,
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_members", x => new { x.group_id, x.customer_id });
                    table.ForeignKey(
                        name: "FK_group_members_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_members_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    principal = table.Column<decimal>(type: "numeric", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    term_months = table.Column<int>(type: "integer", nullable: false),
                    disbursement_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    outstanding_balance = table.Column<decimal>(type: "numeric", nullable: true),
                    collateral_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    collateral_value = table.Column<decimal>(type: "numeric", nullable: true),
                    par_bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loans", x => x.id);
                    table.ForeignKey(
                        name: "FK_loans_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_loans_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_loans_products_product_code",
                        column: x => x.product_code,
                        principalTable: "products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journal_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    account_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    debit = table.Column<decimal>(type: "numeric", nullable: false),
                    credit = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_lines_gl_accounts_account_code",
                        column: x => x.account_code,
                        principalTable: "gl_accounts",
                        principalColumn: "code");
                    table.ForeignKey(
                        name: "FK_journal_lines_journal_entries_journal_id",
                        column: x => x.journal_id,
                        principalTable: "journal_entries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "loan_schedules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loan_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    period = table.Column<int>(type: "integer", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    principal = table.Column<decimal>(type: "numeric", nullable: true),
                    interest = table.Column<decimal>(type: "numeric", nullable: true),
                    total = table.Column<decimal>(type: "numeric", nullable: true),
                    balance = table.Column<decimal>(type: "numeric", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_schedules_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_branch_id",
                table: "accounts",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_customer_id",
                table: "accounts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_product_code",
                table: "accounts",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_requester_id",
                table: "approval_requests",
                column: "requester_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_workflow_id",
                table: "approval_requests",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_branches_code",
                table: "branches",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_members_customer_id",
                table: "group_members",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_officer_id",
                table: "groups",
                column: "officer_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_posted_by",
                table: "journal_entries",
                column: "posted_by");

            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_account_code",
                table: "journal_lines",
                column: "account_code");

            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_journal_id",
                table: "journal_lines",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_loan_schedules_loan_id",
                table: "loan_schedules",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "IX_loans_customer_id",
                table: "loans",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_loans_group_id",
                table: "loans",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_loans_product_code",
                table: "loans",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "IX_staff_branch_id",
                table: "staff",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_email",
                table: "staff",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_role_id",
                table: "staff",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_config_key",
                table: "system_config",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_account_id",
                table: "transactions",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_teller_id",
                table: "transactions",
                column: "teller_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_requests");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "journal_lines");

            migrationBuilder.DropTable(
                name: "loan_schedules");

            migrationBuilder.DropTable(
                name: "system_config");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "workflows");

            migrationBuilder.DropTable(
                name: "gl_accounts");

            migrationBuilder.DropTable(
                name: "journal_entries");

            migrationBuilder.DropTable(
                name: "loans");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
