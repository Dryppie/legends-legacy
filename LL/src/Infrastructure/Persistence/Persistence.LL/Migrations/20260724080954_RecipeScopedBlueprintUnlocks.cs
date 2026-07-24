using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RecipeScopedBlueprintUnlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_BlueprintId",
                table: "CharacterRecipeUnlocks");

            migrationBuilder.AddColumn<string>(
                name: "RecipeId",
                table: "CharacterRecipeUnlocks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_RecipeId_BlueprintId",
                table: "CharacterRecipeUnlocks",
                columns: new[] { "CharacterId", "RecipeId", "BlueprintId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_RecipeId_BlueprintId",
                table: "CharacterRecipeUnlocks");

            migrationBuilder.Sql(
                """
                DELETE FROM "CharacterRecipeUnlocks" duplicate
                USING "CharacterRecipeUnlocks" retained
                WHERE duplicate."CharacterId" = retained."CharacterId"
                  AND duplicate."BlueprintId" = retained."BlueprintId"
                  AND duplicate.ctid > retained.ctid;
                """);

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "CharacterRecipeUnlocks");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_BlueprintId",
                table: "CharacterRecipeUnlocks",
                columns: new[] { "CharacterId", "BlueprintId" },
                unique: true);
        }
    }
}
