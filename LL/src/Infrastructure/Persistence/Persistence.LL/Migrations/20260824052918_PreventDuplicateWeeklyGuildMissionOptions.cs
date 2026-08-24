using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateWeeklyGuildMissionOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GuildMissionOptions_GuildId_WeekKey",
                table: "GuildMissionOptions");

            // Preserve a selected option when repairing rows created by the former
            // check-then-insert race; otherwise retain the oldest generated copy.
            migrationBuilder.Sql("""
                WITH ranked_options AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "GuildId", "WeekKey", "MissionDefinitionId"
                            ORDER BY "IsSelected" DESC, "GeneratedAt", "Id"
                        ) AS row_number
                    FROM "GuildMissionOptions"
                )
                DELETE FROM "GuildMissionOptions" AS options
                USING ranked_options
                WHERE options."Id" = ranked_options."Id"
                  AND ranked_options.row_number > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionOptions_GuildId_WeekKey_MissionDefinitionId",
                table: "GuildMissionOptions",
                columns: new[] { "GuildId", "WeekKey", "MissionDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GuildMissionOptions_GuildId_WeekKey_MissionDefinitionId",
                table: "GuildMissionOptions");

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionOptions_GuildId_WeekKey",
                table: "GuildMissionOptions",
                columns: new[] { "GuildId", "WeekKey" });
        }
    }
}
