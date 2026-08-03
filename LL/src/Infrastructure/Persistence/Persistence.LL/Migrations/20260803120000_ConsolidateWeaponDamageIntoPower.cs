using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.LL;

#nullable disable

namespace Persistence.LL.Migrations;

[DbContext(typeof(LLDbContext))]
[Migration("20260803120000_ConsolidateWeaponDamageIntoPower")]
public partial class ConsolidateWeaponDamageIntoPower : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        MergeEntityAttributeValues(migrationBuilder, "EntityAttributes", "EntityId");
        MergeEntityAttributeValues(
            migrationBuilder,
            "EntityAttributeSnapshot",
            "CharacterSnapshotId");

        ConvertModifierRows(migrationBuilder, "ItemAttributeModifier");
        ConvertModifierRows(migrationBuilder, "InstanceAttributeModifier");
        ConvertModifierRows(migrationBuilder, "EquipmentAttributeModifierSnapshot");
        migrationBuilder.Sql("""
            DELETE FROM "StatOverride"
            WHERE "AttributeType" = 5;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The original split cannot be reconstructed after Power values have been merged.
    }

    private static void MergeEntityAttributeValues(
        MigrationBuilder migrationBuilder,
        string table,
        string ownerColumn)
    {
        migrationBuilder.Sql($$"""
            INSERT INTO "{{table}}" ("{{ownerColumn}}", "AttributeType", "Value")
            SELECT "{{ownerColumn}}", 0, "Value"
            FROM "{{table}}"
            WHERE "AttributeType" = 5
            ON CONFLICT ("{{ownerColumn}}", "AttributeType")
            DO UPDATE SET "Value" = "{{table}}"."Value" + EXCLUDED."Value";

            DELETE FROM "{{table}}"
            WHERE "AttributeType" = 5;
            """);
    }

    private static void ConvertModifierRows(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql($$"""
            UPDATE "{{table}}"
            SET "AttributeType" = 0
            WHERE "AttributeType" = 5;
            """);
    }
}
