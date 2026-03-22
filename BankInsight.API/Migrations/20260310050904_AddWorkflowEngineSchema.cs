using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankInsight.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowEngineSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Module = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    TriggerType = table.Column<string>(type: "text", nullable: false),
                    TriggerEventType = table.Column<string>(type: "text", nullable: true),
                    IsSystemProcess = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessDefinitionVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessDefinitionVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessDefinitionVersions_ProcessDefinitions_ProcessDefinit~",
                        column: x => x.ProcessDefinitionId,
                        principalTable: "ProcessDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessEventSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessEventSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessEventSubscriptions_ProcessDefinitions_ProcessDefinit~",
                        column: x => x.ProcessDefinitionId,
                        principalTable: "ProcessDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessDefinitionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    CurrentStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    StartedByUserId = table.Column<string>(type: "text", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessInstances_ProcessDefinitionVersions_ProcessDefinitio~",
                        column: x => x.ProcessDefinitionVersionId,
                        principalTable: "ProcessDefinitionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessStepDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessDefinitionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepCode = table.Column<string>(type: "text", nullable: false),
                    StepName = table.Column<string>(type: "text", nullable: false),
                    StepType = table.Column<string>(type: "text", nullable: false),
                    OrderNo = table.Column<int>(type: "integer", nullable: false),
                    IsStartStep = table.Column<bool>(type: "boolean", nullable: false),
                    IsEndStep = table.Column<bool>(type: "boolean", nullable: false),
                    AssignmentType = table.Column<string>(type: "text", nullable: true),
                    AssignedRoleCode = table.Column<string>(type: "text", nullable: true),
                    AssignedPermissionCode = table.Column<string>(type: "text", nullable: true),
                    AssignedUserFieldPath = table.Column<string>(type: "text", nullable: true),
                    SlaHours = table.Column<int>(type: "integer", nullable: true),
                    RequireMakerCheckerSeparation = table.Column<bool>(type: "boolean", nullable: false),
                    AutoActionConfigJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessStepDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessStepDefinitions_ProcessDefinitionVersions_ProcessDef~",
                        column: x => x.ProcessDefinitionVersionId,
                        principalTable: "ProcessDefinitionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessTransitionDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessDefinitionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransitionName = table.Column<string>(type: "text", nullable: false),
                    ConditionRuleCode = table.Column<string>(type: "text", nullable: true),
                    RequiredOutcome = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessTransitionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessTransitionDefinitions_ProcessDefinitionVersions_Proc~",
                        column: x => x.ProcessDefinitionVersionId,
                        principalTable: "ProcessDefinitionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessInstanceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    FromStepCode = table.Column<string>(type: "text", nullable: true),
                    ToStepCode = table.Column<string>(type: "text", nullable: true),
                    Outcome = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    ActionByUserId = table.Column<string>(type: "text", nullable: true),
                    ActionAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessInstanceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessInstanceHistories_ProcessInstances_ProcessInstanceId",
                        column: x => x.ProcessInstanceId,
                        principalTable: "ProcessInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessStepDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedUserId = table.Column<string>(type: "text", nullable: true),
                    AssignedRoleCode = table.Column<string>(type: "text", nullable: true),
                    AssignedPermissionCode = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimedByUserId = table.Column<string>(type: "text", nullable: true),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CompletedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessTasks_ProcessInstances_ProcessInstanceId",
                        column: x => x.ProcessInstanceId,
                        principalTable: "ProcessInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessTasks_ProcessStepDefinitions_ProcessStepDefinitionId",
                        column: x => x.ProcessStepDefinitionId,
                        principalTable: "ProcessStepDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessDefinitions_Code",
                table: "ProcessDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessDefinitionVersions_ProcessDefinitionId_VersionNo",
                table: "ProcessDefinitionVersions",
                columns: new[] { "ProcessDefinitionId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessEventSubscriptions_ProcessDefinitionId",
                table: "ProcessEventSubscriptions",
                column: "ProcessDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInstanceHistories_ProcessInstanceId",
                table: "ProcessInstanceHistories",
                column: "ProcessInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInstances_ProcessDefinitionVersionId",
                table: "ProcessInstances",
                column: "ProcessDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStepDefinitions_ProcessDefinitionVersionId",
                table: "ProcessStepDefinitions",
                column: "ProcessDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessTasks_ProcessInstanceId",
                table: "ProcessTasks",
                column: "ProcessInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessTasks_ProcessStepDefinitionId",
                table: "ProcessTasks",
                column: "ProcessStepDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessTransitionDefinitions_ProcessDefinitionVersionId",
                table: "ProcessTransitionDefinitions",
                column: "ProcessDefinitionVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessEventSubscriptions");

            migrationBuilder.DropTable(
                name: "ProcessInstanceHistories");

            migrationBuilder.DropTable(
                name: "ProcessTasks");

            migrationBuilder.DropTable(
                name: "ProcessTransitionDefinitions");

            migrationBuilder.DropTable(
                name: "ProcessInstances");

            migrationBuilder.DropTable(
                name: "ProcessStepDefinitions");

            migrationBuilder.DropTable(
                name: "ProcessDefinitionVersions");

            migrationBuilder.DropTable(
                name: "ProcessDefinitions");
        }
    }
}
