using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentInstanceAttributesNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemAttributeModifier_ItemInstances_EquipmentInstanceId",
                table: "ItemAttributeModifier");

            migrationBuilder.DropIndex(
                name: "IX_ItemAttributeModifier_EquipmentInstanceId",
                table: "ItemAttributeModifier");

            migrationBuilder.DropColumn(
                name: "EquipmentInstanceId",
                table: "ItemAttributeModifier");

            migrationBuilder.CreateTable(
                name: "InstanceAttributeModifier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    ModifierType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceAttributeModifier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstanceAttributeModifier_ItemInstances_EquipmentInstanceId",
                        column: x => x.EquipmentInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InstanceAttributeModifier_ItemInstances_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstanceAttributeModifier_EquipmentInstanceId",
                table: "InstanceAttributeModifier",
                column: "EquipmentInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_InstanceAttributeModifier_ItemInstanceId",
                table: "InstanceAttributeModifier",
                column: "ItemInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstanceAttributeModifier");

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentInstanceId",
                table: "ItemAttributeModifier",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemAttributeModifier_EquipmentInstanceId",
                table: "ItemAttributeModifier",
                column: "EquipmentInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemAttributeModifier_ItemInstances_EquipmentInstanceId",
                table: "ItemAttributeModifier",
                column: "EquipmentInstanceId",
                principalTable: "ItemInstances",
                principalColumn: "Id");
        }
    }
}
