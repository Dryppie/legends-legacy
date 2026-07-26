using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations;

[DbContext(typeof(LLDbContext))]
[Migration("20260725180000_AddDungeonPowerEquipmentBalanceVersion")]
public sealed class AddDungeonPowerEquipmentBalanceVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "EquipmentBalanceVersion",
            table: "DungeonPowerRecommendationCacheEntries",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EquipmentBalanceVersion",
            table: "DungeonPowerRecommendationCacheEntries");
    }
}
