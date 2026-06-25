using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    [DbContext(typeof(LLDbContext))]
    [Migration("20260625123000_AllowMultipleBlueprintUnlocksPerRecipe")]
    public partial class AllowMultipleBlueprintUnlocksPerRecipe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_RecipeId",
                table: "CharacterRecipeUnlocks");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_RecipeId_BlueprintId",
                table: "CharacterRecipeUnlocks",
                columns: new[] { "CharacterId", "RecipeId", "BlueprintId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_RecipeId_BlueprintId",
                table: "CharacterRecipeUnlocks");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_RecipeId",
                table: "CharacterRecipeUnlocks",
                columns: new[] { "CharacterId", "RecipeId" },
                unique: true);
        }
    }
}
