using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRaidTiersWithPlusLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "RaidRuns"
                SET "Tier" = CASE
                    WHEN "RaidBossId" = 'raid-boss.hives-abyss' THEN GREATEST("Tier" - 1, 0)
                    WHEN "RaidBossId" = 'raid-boss.sanguine-horror' THEN GREATEST("Tier" - 2, 0)
                    ELSE GREATEST("Tier" - 1, 0)
                END;

                DELETE FROM "RaidPowerRecommendationCacheEntries";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "RaidRuns"
                SET "Tier" = CASE
                    WHEN "RaidBossId" = 'raid-boss.sanguine-horror' THEN "Tier" + 2
                    ELSE "Tier" + 1
                END;

                DELETE FROM "RaidPowerRecommendationCacheEntries";
                """);
        }
    }
}
