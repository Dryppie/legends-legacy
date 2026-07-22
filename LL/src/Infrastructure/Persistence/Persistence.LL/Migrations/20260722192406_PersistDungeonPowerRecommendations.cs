using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PersistDungeonPowerRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DungeonPowerRecommendationCacheEntries",
                columns: table => new
                {
                    DungeonId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DungeonTier = table.Column<int>(type: "integer", nullable: false),
                    DungeonContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AlgorithmVersion = table.Column<int>(type: "integer", nullable: false),
                    CombatRulesVersion = table.Column<int>(type: "integer", nullable: false),
                    BenchmarkDefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                    RecommendationSeedSetVersion = table.Column<int>(type: "integer", nullable: false),
                    RecommendationJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonPowerRecommendationCacheEntries", x => x.DungeonId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DungeonPowerRecommendationCacheEntries");
        }
    }
}
