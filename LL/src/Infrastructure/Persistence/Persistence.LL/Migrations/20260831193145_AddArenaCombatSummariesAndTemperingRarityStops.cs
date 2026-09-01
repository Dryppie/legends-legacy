using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddArenaCombatSummariesAndTemperingRarityStops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RemoveAfterNextRarityUpgrade",
                table: "CraftingQueueItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CombatResultJson",
                table: "ColosseumMatches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemoveAfterNextRarityUpgrade",
                table: "CraftingQueueItems");

            migrationBuilder.DropColumn(
                name: "CombatResultJson",
                table: "ColosseumMatches");
        }
    }
}
