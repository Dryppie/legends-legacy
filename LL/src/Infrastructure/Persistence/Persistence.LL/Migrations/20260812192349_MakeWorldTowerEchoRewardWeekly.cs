using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class MakeWorldTowerEchoRewardWeekly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TowerEchoClears_ServerId_FloorNumber_CharacterId_WeekKey",
                table: "TowerEchoClears");

            migrationBuilder.Sql(
                """
                DELETE FROM "TowerEchoClears"
                WHERE "Id" IN (
                    SELECT "Id"
                    FROM (
                        SELECT
                            "Id",
                            ROW_NUMBER() OVER (
                                PARTITION BY "ServerId", "CharacterId", "WeekKey"
                                ORDER BY "ClearedAt", "Id") AS "RewardOrder"
                        FROM "TowerEchoClears"
                    ) AS "RankedEchoRewards"
                    WHERE "RewardOrder" > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TowerEchoClears_ServerId_CharacterId_WeekKey",
                table: "TowerEchoClears",
                columns: new[] { "ServerId", "CharacterId", "WeekKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TowerEchoClears_ServerId_CharacterId_WeekKey",
                table: "TowerEchoClears");

            migrationBuilder.CreateIndex(
                name: "IX_TowerEchoClears_ServerId_FloorNumber_CharacterId_WeekKey",
                table: "TowerEchoClears",
                columns: new[] { "ServerId", "FloorNumber", "CharacterId", "WeekKey" },
                unique: true);
        }
    }
}
