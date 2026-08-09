using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTutorialWithQuestSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideWhenLocked",
                table: "Areas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiredActiveQuestId",
                table: "Areas",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredCompletedQuestId",
                table: "Areas",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CharacterQuestProgresses",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardsGrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterQuestProgresses", x => new { x.CharacterId, x.QuestId });
                });

            migrationBuilder.CreateTable(
                name: "QuestEventLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestEventLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterQuestObjectiveProgresses",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ObjectiveKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CurrentAmount = table.Column<long>(type: "bigint", nullable: false),
                    RequiredAmount = table.Column<long>(type: "bigint", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterQuestObjectiveProgresses", x => new { x.CharacterId, x.QuestId, x.ObjectiveKey });
                    table.ForeignKey(
                        name: "FK_CharacterQuestObjectiveProgresses_CharacterQuestProgresses_~",
                        columns: x => new { x.CharacterId, x.QuestId },
                        principalTable: "CharacterQuestProgresses",
                        principalColumns: new[] { "CharacterId", "QuestId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuestProgresses_CharacterId_IsPinned",
                table: "CharacterQuestProgresses",
                columns: new[] { "CharacterId", "IsPinned" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuestProgresses_CharacterId_Status",
                table: "CharacterQuestProgresses",
                columns: new[] { "CharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestEventLedgers_CharacterId_ProcessedAt",
                table: "QuestEventLedgers",
                columns: new[] { "CharacterId", "ProcessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestEventLedgers_OutboxMessageId",
                table: "QuestEventLedgers",
                column: "OutboxMessageId",
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE "Areas"
                SET "RequiredActiveQuestId" = 'quest.onboarding.training_day',
                    "HideWhenLocked" = TRUE
                WHERE "Id" = 'tutorial_area_training_grounds';

                UPDATE "Areas"
                SET "RequiredCompletedQuestId" = 'quest.onboarding.tools_of_trade'
                WHERE "Id" = 'region_01_area_01';

                UPDATE "Areas"
                SET "RequiredCompletedQuestId" = 'quest.region01.into_lumo_ruins'
                WHERE "Id" = 'region_01_area_02';
                """);

            migrationBuilder.Sql(
                """
                WITH legacy AS (
                    SELECT
                        e."Id" AS "CharacterId",
                        COALESCE(t."CreatedAt", NOW()) AS "CreatedAt",
                        COALESCE(t."UpdatedAt", NOW()) AS "UpdatedAt",
                        COALESCE(t."CompletedAt", NOW()) AS "LegacyCompletedAt",
                        CASE
                            WHEN t."CompletedAt" IS NOT NULL OR t."CurrentStep" IN ('complete', 'defeat_lumo_ruins') THEN 7
                            WHEN t."CurrentStep" = 'start_lumo_ruins' THEN 6
                            WHEN t."CurrentStep" = 'equip_gathering_tool' THEN 5
                            WHEN t."CurrentStep" = 'equip_equipment' THEN 4
                            WHEN t."CurrentStep" = 'craft_equipment' THEN 3
                            WHEN t."CurrentStep" = 'equip_essence' THEN 2
                            WHEN t."CurrentStep" = 'absorb_essence' THEN 1
                            WHEN t."CurrentStep" = 'defeat_training_creature' THEN 0
                            WHEN t."CharacterId" IS NULL THEN 7
                            ELSE 0
                        END AS stage
                    FROM "Entities" e
                    LEFT JOIN "CharacterTutorialProgresses" t
                        ON t."CharacterId" = e."Id"
                       AND t."TutorialId" = 'tutorial.first_steps'
                    WHERE e."EntityType" = 1
                ), quests AS (
                    SELECT * FROM (VALUES
                        ('quest.onboarding.training_day', 0, 1, 10),
                        ('quest.onboarding.soul_archive', 1, 3, 20),
                        ('quest.onboarding.first_weapon', 3, 5, 30),
                        ('quest.onboarding.tools_of_trade', 5, 6, 40),
                        ('quest.region01.into_lumo_ruins', 6, 7, 50)
                    ) AS q("QuestId", "AvailableStage", "CompletedStage", "SortOrder")
                )
                INSERT INTO "CharacterQuestProgresses" (
                    "CharacterId", "QuestId", "DefinitionVersion", "Status", "IsPinned",
                    "AcceptedAt", "CompletedAt", "RewardsGrantedAt", "CreatedAt", "UpdatedAt", "RowVersion")
                SELECT
                    l."CharacterId",
                    q."QuestId",
                    1,
                    CASE WHEN l.stage >= q."CompletedStage" THEN 3 ELSE 2 END,
                    l.stage >= q."AvailableStage" AND l.stage < q."CompletedStage",
                    l."CreatedAt",
                    CASE WHEN l.stage >= q."CompletedStage" THEN l."LegacyCompletedAt" ELSE NULL END,
                    CASE WHEN l.stage >= q."CompletedStage" THEN l."LegacyCompletedAt" ELSE NULL END,
                    l."CreatedAt",
                    l."UpdatedAt",
                    0
                FROM legacy l
                CROSS JOIN quests q
                WHERE l.stage >= q."AvailableStage"
                ON CONFLICT ("CharacterId", "QuestId") DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                WITH legacy AS (
                    SELECT
                        e."Id" AS "CharacterId",
                        COALESCE(t."UpdatedAt", NOW()) AS "UpdatedAt",
                        CASE
                            WHEN t."CompletedAt" IS NOT NULL OR t."CurrentStep" IN ('complete', 'defeat_lumo_ruins') THEN 7
                            WHEN t."CurrentStep" = 'start_lumo_ruins' THEN 6
                            WHEN t."CurrentStep" = 'equip_gathering_tool' THEN 5
                            WHEN t."CurrentStep" = 'equip_equipment' THEN 4
                            WHEN t."CurrentStep" = 'craft_equipment' THEN 3
                            WHEN t."CurrentStep" = 'equip_essence' THEN 2
                            WHEN t."CurrentStep" = 'absorb_essence' THEN 1
                            WHEN t."CurrentStep" = 'defeat_training_creature' THEN 0
                            WHEN t."CharacterId" IS NULL THEN 7
                            ELSE 0
                        END AS stage
                    FROM "Entities" e
                    LEFT JOIN "CharacterTutorialProgresses" t
                        ON t."CharacterId" = e."Id"
                       AND t."TutorialId" = 'tutorial.first_steps'
                    WHERE e."EntityType" = 1
                ), objectives AS (
                    SELECT * FROM (VALUES
                        ('quest.onboarding.training_day', 'win_training_encounter', 1),
                        ('quest.onboarding.soul_archive', 'absorb_goblin_essence', 2),
                        ('quest.onboarding.soul_archive', 'equip_goblin_essence', 3),
                        ('quest.onboarding.first_weapon', 'craft_tier_one_weapon', 4),
                        ('quest.onboarding.first_weapon', 'equip_crafted_weapon', 5),
                        ('quest.onboarding.tools_of_trade', 'equip_gathering_tool', 6),
                        ('quest.region01.into_lumo_ruins', 'win_lumo_ruins_encounter', 7)
                    ) AS o("QuestId", "ObjectiveKey", "CompletedStage")
                )
                INSERT INTO "CharacterQuestObjectiveProgresses" (
                    "CharacterId", "QuestId", "ObjectiveKey", "CurrentAmount",
                    "RequiredAmount", "CompletedAt", "UpdatedAt")
                SELECT
                    p."CharacterId",
                    p."QuestId",
                    o."ObjectiveKey",
                    CASE WHEN l.stage >= o."CompletedStage" THEN 1 ELSE 0 END,
                    1,
                    CASE WHEN l.stage >= o."CompletedStage" THEN l."UpdatedAt" ELSE NULL END,
                    l."UpdatedAt"
                FROM "CharacterQuestProgresses" p
                JOIN legacy l ON l."CharacterId" = p."CharacterId"
                JOIN objectives o ON o."QuestId" = p."QuestId"
                ON CONFLICT ("CharacterId", "QuestId", "ObjectiveKey") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterQuestObjectiveProgresses");

            migrationBuilder.DropTable(
                name: "QuestEventLedgers");

            migrationBuilder.DropTable(
                name: "CharacterQuestProgresses");

            migrationBuilder.DropColumn(
                name: "HideWhenLocked",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "RequiredActiveQuestId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "RequiredCompletedQuestId",
                table: "Areas");
        }
    }
}
