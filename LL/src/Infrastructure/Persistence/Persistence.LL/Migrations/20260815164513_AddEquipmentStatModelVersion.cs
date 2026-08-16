using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentStatModelVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatModelVersion",
                table: "ItemInstances",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"ItemInstances\" SET \"StatModelVersion\" = 15 " +
                "WHERE \"ItemType\" = 0 AND \"StatModelVersion\" IS NULL;");

            // ItemInstances is the single TPH source row referenced by inventories,
            // equipment slots, marketplaces, crafting queues, guild vaults, and rewards.
            // Updating its persisted instance name therefore covers every live location.
            migrationBuilder.Sql(
                "UPDATE \"ItemInstances\" AS equipment " +
                "SET \"CraftedName\" = 'Broken ' || COALESCE(" +
                "NULLIF(BTRIM(equipment.\"CraftedName\"), ''), " +
                "CASE WHEN bases.\"EquipmentType\" = 9 THEN " +
                "CASE equipment.\"Rarity\" " +
                "WHEN 0 THEN 'Plain ' WHEN 1 THEN 'Sturdy ' WHEN 2 THEN 'Proven ' " +
                "WHEN 3 THEN 'Exquisite ' WHEN 4 THEN 'Fabled ' " +
                "WHEN 5 THEN 'Mythic ' WHEN 6 THEN 'Eternal ' ELSE '' END " +
                "|| bases.\"Name\" ELSE bases.\"Name\" END) " +
                "FROM \"ItemBases\" AS bases " +
                "WHERE equipment.\"ItemType\" = 0 " +
                "AND equipment.\"StatModelVersion\" = 15 " +
                "AND bases.\"Id\" = equipment.\"ItemBaseId\" " +
                "AND COALESCE(BTRIM(equipment.\"CraftedName\"), '') NOT ILIKE 'Broken %';");

            migrationBuilder.AddColumn<int>(
                name: "Quality",
                table: "EquipmentSnapshot",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "StatModelVersion",
                table: "EquipmentSnapshot",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "EquipmentSnapshot",
                type: "integer",
                nullable: false,
                defaultValue: 1);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatModelVersion",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "Quality",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "StatModelVersion",
                table: "EquipmentSnapshot");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "EquipmentSnapshot");

        }
    }
}
