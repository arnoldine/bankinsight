CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
  "MigrationId" character varying(150) NOT NULL PRIMARY KEY,
  "ProductVersion" character varying(32) NOT NULL
);

CREATE TABLE IF NOT EXISTS "ProcessDefinitions" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "Code" text NOT NULL,
  "Name" text NOT NULL,
  "Module" text NOT NULL,
  "EntityType" text NOT NULL,
  "TriggerType" text NOT NULL,
  "TriggerEventType" text NULL,
  "IsSystemProcess" boolean NOT NULL,
  "IsActive" boolean NOT NULL,
  "CreatedAtUtc" timestamp with time zone NOT NULL,
  "CreatedByUserId" text NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProcessDefinitions_Code" ON "ProcessDefinitions" ("Code");

CREATE TABLE IF NOT EXISTS "ProcessDefinitionVersions" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ProcessDefinitionId" uuid NOT NULL REFERENCES "ProcessDefinitions"("Id") ON DELETE CASCADE,
  "VersionNo" integer NOT NULL,
  "Status" text NOT NULL,
  "IsPublished" boolean NOT NULL,
  "Notes" text NULL,
  "CreatedAtUtc" timestamp with time zone NOT NULL,
  "CreatedByUserId" text NOT NULL,
  "PublishedAtUtc" timestamp with time zone NULL,
  "PublishedByUserId" text NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProcessDefinitionVersions_ProcessDefinitionId_VersionNo"
ON "ProcessDefinitionVersions" ("ProcessDefinitionId", "VersionNo");

CREATE TABLE IF NOT EXISTS "ProcessEventSubscriptions" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ProcessDefinitionId" uuid NOT NULL REFERENCES "ProcessDefinitions"("Id") ON DELETE CASCADE,
  "EventType" text NOT NULL,
  "IsActive" boolean NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_ProcessEventSubscriptions_ProcessDefinitionId"
ON "ProcessEventSubscriptions" ("ProcessDefinitionId");

CREATE TABLE IF NOT EXISTS "ProcessInstances" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ProcessDefinitionVersionId" uuid NOT NULL REFERENCES "ProcessDefinitionVersions"("Id") ON DELETE RESTRICT,
  "EntityType" text NOT NULL,
  "EntityId" text NOT NULL,
  "CurrentStepId" uuid NOT NULL,
  "Status" text NOT NULL,
  "CorrelationId" text NULL,
  "StartedByUserId" text NOT NULL,
  "StartedAtUtc" timestamp with time zone NOT NULL,
  "CompletedAtUtc" timestamp with time zone NULL
);

CREATE INDEX IF NOT EXISTS "IX_ProcessInstances_ProcessDefinitionVersionId"
ON "ProcessInstances" ("ProcessDefinitionVersionId");

CREATE TABLE IF NOT EXISTS "ProcessStepDefinitions" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ProcessDefinitionVersionId" uuid NOT NULL REFERENCES "ProcessDefinitionVersions"("Id") ON DELETE CASCADE,
  "StepCode" text NOT NULL,
  "StepName" text NOT NULL,
  "StepType" text NOT NULL,
  "OrderNo" integer NOT NULL,
  "IsStartStep" boolean NOT NULL,
  "IsEndStep" boolean NOT NULL,
  "AssignmentType" text NULL,
  "AssignedRoleCode" text NULL,
  "AssignedPermissionCode" text NULL,
  "AssignedUserFieldPath" text NULL,
  "SlaHours" integer NULL,
  "RequireMakerCheckerSeparation" boolean NOT NULL,
  "AutoActionConfigJson" text NULL
);

CREATE INDEX IF NOT EXISTS "IX_ProcessStepDefinitions_ProcessDefinitionVersionId"
ON "ProcessStepDefinitions" ("ProcessDefinitionVersionId");

CREATE TABLE IF NOT EXISTS "ProcessTransitionDefinitions" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ProcessDefinitionVersionId" uuid NOT NULL REFERENCES "ProcessDefinitionVersions"("Id") ON DELETE CASCADE,
  "FromStepId" uuid NOT NULL,
  "ToStepId" uuid NOT NULL,
  "TransitionName" text NOT NULL,
  "ConditionRuleCode" text NULL,
  "RequiredOutcome" text NULL,
  "IsDefault" boolean NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_ProcessTransitionDefinitions_ProcessDefinitionVersionId"
ON "ProcessTransitionDefinitions" ("ProcessDefinitionVersionId");

CREATE TABLE IF NOT EXISTS "ProcessInstanceHistories" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ProcessInstanceId" uuid NOT NULL REFERENCES "ProcessInstances"("Id") ON DELETE CASCADE,
  "ActionType" text NOT NULL,
  "FromStepCode" text NULL,
  "ToStepCode" text NULL,
  "Outcome" text NULL,
  "Remarks" text NULL,
  "ActionByUserId" text NULL,
  "ActionAtUtc" timestamp with time zone NOT NULL,
  "PayloadJson" text NULL
);

CREATE INDEX IF NOT EXISTS "IX_ProcessInstanceHistories_ProcessInstanceId"
ON "ProcessInstanceHistories" ("ProcessInstanceId");

CREATE TABLE IF NOT EXISTS "ProcessTasks" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ProcessInstanceId" uuid NOT NULL REFERENCES "ProcessInstances"("Id") ON DELETE CASCADE,
  "ProcessStepDefinitionId" uuid NOT NULL REFERENCES "ProcessStepDefinitions"("Id") ON DELETE CASCADE,
  "AssignedUserId" text NULL,
  "AssignedRoleCode" text NULL,
  "AssignedPermissionCode" text NULL,
  "Status" text NOT NULL,
  "CreatedAtUtc" timestamp with time zone NOT NULL,
  "ClaimedAtUtc" timestamp with time zone NULL,
  "ClaimedByUserId" text NULL,
  "DueAtUtc" timestamp with time zone NULL,
  "CompletedAtUtc" timestamp with time zone NULL,
  "Outcome" text NULL,
  "Remarks" text NULL,
  "CompletedByUserId" text NULL
);

CREATE INDEX IF NOT EXISTS "IX_ProcessTasks_ProcessInstanceId"
ON "ProcessTasks" ("ProcessInstanceId");

CREATE INDEX IF NOT EXISTS "IX_ProcessTasks_ProcessStepDefinitionId"
ON "ProcessTasks" ("ProcessStepDefinitionId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260310050904_AddWorkflowEngineSchema', '8.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310050904_AddWorkflowEngineSchema'
);
