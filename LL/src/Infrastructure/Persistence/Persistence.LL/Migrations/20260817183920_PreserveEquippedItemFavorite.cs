using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PreserveEquippedItemFavorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "ItemInstances",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "ItemInstances"
                SET "IsFavorite" = FALSE
                WHERE "ItemType" = 0 AND "IsFavorite" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "ItemInstances");
        }
    }
}
