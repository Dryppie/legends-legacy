using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBalanceCalibrationCaches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DungeonPowerRecommendationCacheEntries");

            migrationBuilder.DropTable(
                name: "RaidPowerRecommendationCacheEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DungeonPowerRecommendationCacheEntries",
                columns: table => new
                {
                    DungeonId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AlgorithmVersion = table.Column<int>(type: "integer", nullable: false),
                    BenchmarkDefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                    CombatRulesVersion = table.Column<int>(type: "integer", nullable: false),
                    DungeonContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DungeonTier = table.Column<int>(type: "integer", nullable: false),
                    EquipmentBalanceVersion = table.Column<int>(type: "integer", nullable: false),
                    RecommendationJson = table.Column<string>(type: "jsonb", nullable: false),
                    RecommendationSeedSetVersion = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonPowerRecommendationCacheEntries", x => x.DungeonId);
                });

            migrationBuilder.CreateTable(
                name: "RaidPowerRecommendationCacheEntries",
                columns: table => new
                {
                    RaidBossId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    CombatRulesVersion = table.Column<int>(type: "integer", nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EquipmentBalanceVersion = table.Column<int>(type: "integer", nullable: false),
                    PowerRatingAlgorithmVersion = table.Column<int>(type: "integer", nullable: false),
                    RaidRulesVersion = table.Column<int>(type: "integer", nullable: false),
                    RecommendationJson = table.Column<string>(type: "jsonb", nullable: false),
                    SeedSetVersion = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidPowerRecommendationCacheEntries", x => new { x.RaidBossId, x.Tier });
                });
        }
    }
}
