using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations;

[DbContext(typeof(LLDbContext))]
[Migration("20260809170000_RemoveSummonAttributes")]
public sealed class RemoveSummonAttributes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "EquipmentAttributeModifierSnapshot" WHERE "AttributeType" IN (17, 18);
            DELETE FROM "EntityAttributeSnapshot" WHERE "AttributeType" IN (17, 18);
            DELETE FROM "InstanceAttributeModifier" WHERE "AttributeType" IN (17, 18);
            DELETE FROM "ItemAttributeModifier" WHERE "AttributeType" IN (17, 18);
            DELETE FROM "StatOverride" WHERE "AttributeType" IN (17, 18);
            DELETE FROM "EntityAttributes" WHERE "AttributeType" IN (17, 18);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Retired attribute values cannot be reconstructed after they are removed.
    }
}
