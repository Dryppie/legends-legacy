using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class StartCombatPathMasteryAtZero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CharacterCombatStyles_Progression",
                table: "CharacterCombatStyles");

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "CharacterCombatStyles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CharacterCombatStyles_Progression",
                table: "CharacterCombatStyles",
                sql: "\"Level\" BETWEEN 0 AND 10 AND \"CurrentXp\" >= 0 AND (\"Level\" < 10 OR \"CurrentXp\" = 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the former minimum while keeping accumulated XP.
            migrationBuilder.Sql("UPDATE \"CharacterCombatStyles\" SET \"Level\" = 1 WHERE \"Level\" = 0;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CharacterCombatStyles_Progression",
                table: "CharacterCombatStyles");

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "CharacterCombatStyles",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CharacterCombatStyles_Progression",
                table: "CharacterCombatStyles",
                sql: "\"Level\" BETWEEN 1 AND 10 AND \"CurrentXp\" >= 0 AND (\"Level\" < 10 OR \"CurrentXp\" = 0)");
        }
    }
}
