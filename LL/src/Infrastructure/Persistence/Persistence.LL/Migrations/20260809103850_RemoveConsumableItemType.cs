using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConsumableItemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "ItemBases"
                SET "ItemType" = 2
                WHERE "ItemType" = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "ItemBases"
                SET "ItemType" = 1
                WHERE "Id" = 'item.catalyst_selection_crate'
                  AND "ItemType" = 2;
                """);
        }
    }
}
