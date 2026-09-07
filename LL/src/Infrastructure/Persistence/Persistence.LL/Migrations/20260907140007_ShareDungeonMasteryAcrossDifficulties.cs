using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ShareDungeonMasteryAcrossDifficulties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MaxLevelRewardClaimed",
                table: "CharacterDungeonMasteries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Match DungeonDefinitionIdentity.GetFamilyId, preserving all earned XP and clears.
            // Consolidation and the curve change run atomically in the migration transaction.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE "DungeonMasteryFamilyMerge" ON COMMIT DROP AS
                SELECT "CharacterId",
                    regexp_replace("DungeonDefinitionId", '_(iii|ii|i)$', '', 'i') AS "DungeonDefinitionId",
                    SUM("Experience")::bigint AS "Experience",
                    SUM("CompletionCount")::integer AS "CompletionCount",
                    BOOL_OR("MaxLevelRewardClaimed" OR "Level" >= 10) AS "MaxLevelRewardClaimed",
                    (array_agg("LastAwardedRunId" ORDER BY "UpdatedAt" DESC, "DungeonDefinitionId")
                        FILTER (WHERE "LastAwardedRunId" IS NOT NULL))[1] AS "LastAwardedRunId",
                    MIN("CreatedAt") AS "CreatedAt",
                    MAX("UpdatedAt") AS "UpdatedAt"
                FROM "CharacterDungeonMasteries"
                GROUP BY "CharacterId", regexp_replace("DungeonDefinitionId", '_(iii|ii|i)$', '', 'i');

                DELETE FROM "CharacterDungeonMasteries";

                INSERT INTO "CharacterDungeonMasteries"
                    ("CharacterId", "DungeonDefinitionId", "Experience", "Level", "CompletionCount",
                     "MaxLevelRewardClaimed", "LastAwardedRunId", "CreatedAt", "UpdatedAt")
                SELECT "CharacterId", "DungeonDefinitionId", "Experience",
                    (SELECT COUNT(*)::integer FROM
                        unnest(ARRAY[1000, 2500, 5000, 9000, 14000, 21000, 30000, 42000, 56000, 75000]) AS threshold(xp)
                        WHERE "Experience" >= threshold.xp),
                    "CompletionCount", "MaxLevelRewardClaimed", "LastAwardedRunId", "CreatedAt", "UpdatedAt"
                FROM "DungeonMasteryFamilyMerge";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException(
                "Shared mastery cannot be split back into its original difficulty records. Restore a pre-migration backup to roll back.");
        }
    }
}
