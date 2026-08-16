using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegacyEquipmentInstanceNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Compatibility repair for databases that applied an earlier draft of
            // AddEquipmentStatModelVersion. Equipment names live in CraftedName;
            // there is deliberately no separate prefix column in the runtime model.
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

            migrationBuilder.Sql(
                "ALTER TABLE \"ItemInstances\" DROP COLUMN IF EXISTS \"NamePrefix\";");

            migrationBuilder.Sql(
                "ALTER TABLE \"EquipmentSnapshot\" DROP COLUMN IF EXISTS \"NamePrefix\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The legacy-name marker is an intentional, irreversible data migration.
            // Restoring the abandoned prefix columns would not reconstruct their data.
        }
    }
}
