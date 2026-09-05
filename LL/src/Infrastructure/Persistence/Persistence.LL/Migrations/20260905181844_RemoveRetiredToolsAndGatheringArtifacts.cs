using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRetiredToolsAndGatheringArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tool was the final retired gathering equipment type (EquipmentType = 9,
            // EquipmentSlotType = 8). Alpha data is intentionally discarded rather
            // than left behind as enum values that the current model cannot represent.
            migrationBuilder.Sql(
                """
                DELETE FROM "EquipmentSnapshot"
                WHERE "Slot" = 8
                   OR "ItemBaseId" IN (
                       SELECT "Id" FROM "ItemBases" WHERE "EquipmentType" = 9
                   );

                DELETE FROM "EquipmentSlots"
                WHERE "EquipmentSlotType" = 8
                   OR "EquipmentInstanceId" IN (
                       SELECT instance."Id"
                       FROM "ItemInstances" AS instance
                       INNER JOIN "ItemBases" AS base_item
                           ON base_item."Id" = instance."ItemBaseId"
                       WHERE base_item."EquipmentType" = 9
                   );

                DELETE FROM "GuildVaultItems"
                WHERE "EquipmentInstanceId" IN (
                    SELECT instance."Id"
                    FROM "ItemInstances" AS instance
                    INNER JOIN "ItemBases" AS base_item
                        ON base_item."Id" = instance."ItemBaseId"
                    WHERE base_item."EquipmentType" = 9
                );

                DELETE FROM "MarketPlaceBuyOrders"
                WHERE "ItemBaseId" IN (
                    SELECT "Id" FROM "ItemBases" WHERE "EquipmentType" = 9
                );

                DELETE FROM "MarketPlaceOrders"
                WHERE "ItemBaseId" IN (
                    SELECT "Id" FROM "ItemBases" WHERE "EquipmentType" = 9
                );

                DELETE FROM "ItemInstances"
                WHERE "ItemBaseId" IN (
                    SELECT "Id" FROM "ItemBases" WHERE "EquipmentType" = 9
                );

                DELETE FROM "ItemBases" WHERE "EquipmentType" = 9;
                """);

            migrationBuilder.DropTable(
                name: "ToolBonusModifier");

            migrationBuilder.DropColumn(
                name: "GatheringType",
                table: "ItemBases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GatheringType",
                table: "ItemBases",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ToolBonusModifier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentBaseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    BonusType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ScopeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolBonusModifier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolBonusModifier_ItemBases_EquipmentBaseId",
                        column: x => x.EquipmentBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToolBonusModifier_ItemInstances_EquipmentInstanceId",
                        column: x => x.EquipmentInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToolBonusModifier_EquipmentBaseId",
                table: "ToolBonusModifier",
                column: "EquipmentBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolBonusModifier_EquipmentInstanceId",
                table: "ToolBonusModifier",
                column: "EquipmentInstanceId");
        }
    }
}
