using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItemSeenAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SeenAtUtc",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: true);

            // Everything already in an inventory predates the feature. Treat it as seen so no
            // existing item is flagged as new on deploy.
            migrationBuilder.Sql(
                "UPDATE \"InventoryItems\" SET \"SeenAtUtc\" = NOW() WHERE \"SeenAtUtc\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InventoryId_SeenAtUtc",
                table: "InventoryItems",
                columns: new[] { "InventoryId", "SeenAtUtc" },
                filter: "\"SeenAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_InventoryId_SeenAtUtc",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SeenAtUtc",
                table: "InventoryItems");
        }
    }
}
