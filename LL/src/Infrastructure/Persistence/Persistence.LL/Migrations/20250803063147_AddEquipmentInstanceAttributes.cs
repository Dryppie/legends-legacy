using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentInstanceAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
