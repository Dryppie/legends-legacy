using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceLootTablesWithRewardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AreaGatheringNode_LootTableEntry_LootTableId",
                table: "AreaGatheringNode");

            migrationBuilder.DropForeignKey(
                name: "FK_Entities_LootTableEntry_LootTableId",
                table: "Entities");

            migrationBuilder.DropTable(
                name: "LootTableEntry");

            migrationBuilder.DropIndex(
                name: "IX_Entities_LootTableId",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_AreaGatheringNode_LootTableId",
                table: "AreaGatheringNode");

            migrationBuilder.DropColumn(
                name: "LootTableId",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "LootTableId",
                table: "AreaGatheringNode");

            migrationBuilder.AddColumn<string>(
                name: "RewardTableId",
                table: "Entities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RewardTableId",
                table: "AreaGatheringNode",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RewardTableId",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "RewardTableId",
                table: "AreaGatheringNode");

            migrationBuilder.AddColumn<Guid>(
                name: "LootTableId",
                table: "Entities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LootTableId",
                table: "AreaGatheringNode",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "LootTableEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LootTableId = table.Column<Guid>(type: "uuid", nullable: true),
                    LootTableType = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: true),
                    IsRare = table.Column<bool>(type: "boolean", nullable: true),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true),
                    MinQuantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LootTableEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LootTableEntry_ItemBases_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LootTableEntry_LootTableEntry_LootTableId",
                        column: x => x.LootTableId,
                        principalTable: "LootTableEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entities_LootTableId",
                table: "Entities",
                column: "LootTableId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaGatheringNode_LootTableId",
                table: "AreaGatheringNode",
                column: "LootTableId");

            migrationBuilder.CreateIndex(
                name: "IX_LootTableEntry_ItemId",
                table: "LootTableEntry",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LootTableEntry_LootTableId",
                table: "LootTableEntry",
                column: "LootTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_AreaGatheringNode_LootTableEntry_LootTableId",
                table: "AreaGatheringNode",
                column: "LootTableId",
                principalTable: "LootTableEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Entities_LootTableEntry_LootTableId",
                table: "Entities",
                column: "LootTableId",
                principalTable: "LootTableEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
