using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentGroundsMilestoneRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlueprintSelectionBoxes",
                table: "TournamentRewardGrants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatalystSelectionCaches",
                table: "TournamentRewardGrants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SigilFragments",
                table: "TournamentRewardGrants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "TournamentRewardGrants"
                SET
                    "ArenaGlory" = CASE
                        WHEN "Placement" = 1 THEN 500
                        WHEN "Placement" <= 2 THEN 425
                        WHEN "Placement" <= 4 THEN 350
                        WHEN "Placement" <= 8 THEN 300
                        ELSE 250
                    END,
                    "Cinders" = 0,
                    "Soulstones" = CASE
                        WHEN "Placement" = 1 THEN 50
                        WHEN "Placement" <= 2 THEN 40
                        WHEN "Placement" <= 4 THEN 30
                        WHEN "Placement" <= 8 THEN 25
                        ELSE 20
                    END,
                    "CatalystSelectionCaches" = CASE WHEN "Placement" <= 8 THEN 1 ELSE 0 END,
                    "BlueprintSelectionBoxes" = CASE WHEN "Placement" <= 4 THEN 1 ELSE 0 END,
                    "SigilFragments" = CASE WHEN "Placement" <= 2 THEN 20 ELSE 0 END
                WHERE "Status" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlueprintSelectionBoxes",
                table: "TournamentRewardGrants");

            migrationBuilder.DropColumn(
                name: "CatalystSelectionCaches",
                table: "TournamentRewardGrants");

            migrationBuilder.DropColumn(
                name: "SigilFragments",
                table: "TournamentRewardGrants");
        }
    }
}
