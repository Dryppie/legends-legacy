using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PauseTemperingQueues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CraftingActionDetailsId",
                table: "CraftingQueueItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PausedForCharacterId",
                table: "CraftingQueueItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftingQueueItems_PausedForCharacterId_Position",
                table: "CraftingQueueItems",
                columns: new[] { "PausedForCharacterId", "Position" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CraftingQueueItems_ActiveOrPaused",
                table: "CraftingQueueItems",
                sql: "(\"CraftingActionDetailsId\" IS NOT NULL AND \"PausedForCharacterId\" IS NULL) OR (\"CraftingActionDetailsId\" IS NULL AND \"PausedForCharacterId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_CraftingQueueItems_Entities_PausedForCharacterId",
                table: "CraftingQueueItems",
                column: "PausedForCharacterId",
                principalTable: "Entities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "InventoryItems" ("InventoryId", "ItemInstanceId", "Quantity", "IsFavorite")
                SELECT "PausedForCharacterId", "EquipmentInstanceId", 1, FALSE
                FROM "CraftingQueueItems"
                WHERE "PausedForCharacterId" IS NOT NULL
                ON CONFLICT ("InventoryId", "ItemInstanceId") DO NOTHING;

                DELETE FROM "CraftingQueueItems"
                WHERE "PausedForCharacterId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_CraftingQueueItems_Entities_PausedForCharacterId",
                table: "CraftingQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_CraftingQueueItems_PausedForCharacterId_Position",
                table: "CraftingQueueItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CraftingQueueItems_ActiveOrPaused",
                table: "CraftingQueueItems");

            migrationBuilder.DropColumn(
                name: "PausedForCharacterId",
                table: "CraftingQueueItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "CraftingActionDetailsId",
                table: "CraftingQueueItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
