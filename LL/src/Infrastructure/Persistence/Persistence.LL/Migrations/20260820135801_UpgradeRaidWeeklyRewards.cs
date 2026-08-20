using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeRaidWeeklyRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RaidRewardClaims_RaidBossId_CharacterId_WeekKey",
                table: "RaidRewardClaims");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "RaidRewardClaims",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "RaidRewardClaims"
                SET "Kind" = 2
                WHERE "WasReduced" = true;
                """);

            migrationBuilder.DropColumn(
                name: "WasReduced",
                table: "RaidRewardClaims");

            migrationBuilder.DropColumn(
                name: "PayoutMultiplier",
                table: "RaidParticipantResults");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRewardClaims_RaidBossId_CharacterId_WeekKey",
                table: "RaidRewardClaims",
                columns: new[] { "RaidBossId", "CharacterId", "WeekKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RaidRewardClaims_RaidBossId_CharacterId_WeekKey",
                table: "RaidRewardClaims");

            migrationBuilder.AddColumn<bool>(
                name: "WasReduced",
                table: "RaidRewardClaims",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "RaidRewardClaims"
                SET "WasReduced" = true
                WHERE "Kind" <> 0;
                """);

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "RaidRewardClaims");

            migrationBuilder.AddColumn<decimal>(
                name: "PayoutMultiplier",
                table: "RaidParticipantResults",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateIndex(
                name: "IX_RaidRewardClaims_RaidBossId_CharacterId_WeekKey",
                table: "RaidRewardClaims",
                columns: new[] { "RaidBossId", "CharacterId", "WeekKey" },
                unique: true,
                filter: "\"WasReduced\" = false");
        }
    }
}
