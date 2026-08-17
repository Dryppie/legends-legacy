using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicatePersonalGuildOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonalGuildOrders_GuildId_CharacterId_PeriodType_PeriodKey",
                table: "PersonalGuildOrders");

            // Keep the most advanced copy when cleaning up rows created by the former
            // check-then-insert race. A claimed/completed copy must win over an active one.
            migrationBuilder.Sql("""
                WITH ranked_orders AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY
                                "GuildId",
                                "CharacterId",
                                "PeriodType",
                                "PeriodKey",
                                "MissionDefinitionId"
                            ORDER BY
                                CASE
                                    WHEN "RewardClaimedAt" IS NOT NULL OR "Status" = 3 THEN 0
                                    WHEN "Status" = 1 THEN 1
                                    ELSE 2
                                END,
                                "CurrentAmount" DESC,
                                "GeneratedAt",
                                "Id"
                        ) AS row_number
                    FROM "PersonalGuildOrders"
                )
                DELETE FROM "PersonalGuildOrders" AS orders
                USING ranked_orders
                WHERE orders."Id" = ranked_orders."Id"
                  AND ranked_orders.row_number > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalGuildOrders_GuildId_CharacterId_PeriodType_PeriodKe~",
                table: "PersonalGuildOrders",
                columns: new[] { "GuildId", "CharacterId", "PeriodType", "PeriodKey", "MissionDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonalGuildOrders_GuildId_CharacterId_PeriodType_PeriodKe~",
                table: "PersonalGuildOrders");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalGuildOrders_GuildId_CharacterId_PeriodType_PeriodKey",
                table: "PersonalGuildOrders",
                columns: new[] { "GuildId", "CharacterId", "PeriodType", "PeriodKey" });
        }
    }
}
