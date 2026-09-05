using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyEquipmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemInstances_EquipmentSetId",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "BaseRecipeId",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "BlueprintId",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "CraftedName",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "EquipmentSetId",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "IsLevelingItem",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "IsMasterpiece",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "ItemXp",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "MaxPotential",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "Potential",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "StatModelVersion",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "TemperingProgress",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "BaseRecipeId",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "BlueprintId",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "EquipmentSetId",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "IsLevelingItem",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "IsMasterpiece",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "ItemXp",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "Potential",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "StatModelVersion",
                table: "EquipmentSnapshot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseRecipeId",
                table: "ItemInstances",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlueprintId",
                table: "ItemInstances",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CraftedName",
                table: "ItemInstances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentSetId",
                table: "ItemInstances",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLevelingItem",
                table: "ItemInstances",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMasterpiece",
                table: "ItemInstances",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemXp",
                table: "ItemInstances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPotential",
                table: "ItemInstances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Potential",
                table: "ItemInstances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatModelVersion",
                table: "ItemInstances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemperingProgress",
                table: "ItemInstances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseRecipeId",
                table: "EquipmentSnapshot",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlueprintId",
                table: "EquipmentSnapshot",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentSetId",
                table: "EquipmentSnapshot",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLevelingItem",
                table: "EquipmentSnapshot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMasterpiece",
                table: "EquipmentSnapshot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ItemXp",
                table: "EquipmentSnapshot",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Potential",
                table: "EquipmentSnapshot",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatModelVersion",
                table: "EquipmentSnapshot",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstances_EquipmentSetId",
                table: "ItemInstances",
                column: "EquipmentSetId");
        }
    }
}
