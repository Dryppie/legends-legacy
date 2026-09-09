using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatStyleUpgradeMastery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MasteredUpgradeId",
                table: "CharacterCombatStyleSelections",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasteredUpgradeId",
                table: "CharacterCombatStyles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MasteredUpgradeId",
                table: "CharacterCombatStyleSelections");

            migrationBuilder.DropColumn(
                name: "MasteredUpgradeId",
                table: "CharacterCombatStyles");
        }
    }
}
