using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations;

[DbContext(typeof(LLDbContext))]
[Migration("20260729120000_RemoveTutorialChest")]
public sealed class RemoveTutorialChest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "MarketPlaceBuyOrders"
            WHERE "ItemBaseId" = 'tutorial_chest';

            DELETE FROM "MarketPlaceOrders"
            WHERE "ItemBaseId" = 'tutorial_chest';

            UPDATE "EquipmentSlots"
            SET "EquipmentInstanceId" = NULL
            WHERE "EquipmentInstanceId" IN (
                SELECT "Id"
                FROM "ItemInstances"
                WHERE "ItemBaseId" = 'tutorial_chest'
            );

            DELETE FROM "ItemBases"
            WHERE "Id" = 'tutorial_chest';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Removed tutorial-chest instances and marketplace history cannot be
        // reconstructed safely. The item definition remains intentionally absent.
    }
}
