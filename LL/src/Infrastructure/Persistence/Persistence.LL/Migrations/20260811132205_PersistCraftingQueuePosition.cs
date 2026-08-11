using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PersistCraftingQueuePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CraftingQueueItems_CraftingActionDetailsId",
                table: "CraftingQueueItems");

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "CraftingQueueItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                WITH ranked_queue AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "CraftingActionDetailsId"
                            ORDER BY "AddedAt", "Id") - 1 AS "Position"
                    FROM "CraftingQueueItems"
                )
                UPDATE "CraftingQueueItems" AS queue_item
                SET "Position" = ranked_queue."Position"
                FROM ranked_queue
                WHERE queue_item."Id" = ranked_queue."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CraftingQueueItems_CraftingActionDetailsId_Position",
                table: "CraftingQueueItems",
                columns: new[] { "CraftingActionDetailsId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CraftingQueueItems_CraftingActionDetailsId_Position",
                table: "CraftingQueueItems");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "CraftingQueueItems");

            migrationBuilder.CreateIndex(
                name: "IX_CraftingQueueItems_CraftingActionDetailsId",
                table: "CraftingQueueItems",
                column: "CraftingActionDetailsId");
        }
    }
}
