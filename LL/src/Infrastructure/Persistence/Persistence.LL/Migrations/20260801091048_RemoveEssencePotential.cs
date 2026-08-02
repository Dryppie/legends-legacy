using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEssencePotential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "MarketPlaceBuyOrders"
                WHERE "ItemBaseId" LIKE 'item.essence_potential_core.region_%';

                DELETE FROM "MarketPlaceOrders"
                WHERE "ItemBaseId" LIKE 'item.essence_potential_core.region_%';

                DELETE FROM "RunRewards"
                WHERE "ItemId" LIKE 'item.essence_potential_core.region_%';

                DELETE FROM "ItemBases"
                WHERE "Id" LIKE 'item.essence_potential_core.region_%';
                """);

            migrationBuilder.DropColumn(
                name: "NativeRegion",
                table: "PlayerEssences");

            migrationBuilder.DropColumn(
                name: "PotentialTier",
                table: "PlayerEssences");

            migrationBuilder.DropColumn(
                name: "NativeRegion",
                table: "EquippedEssenceSnapshots");

            migrationBuilder.DropColumn(
                name: "PotentialTier",
                table: "EquippedEssenceSnapshots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removed Potential Core definitions, holdings, marketplace records,
            // and pending rewards cannot be reconstructed safely.
            migrationBuilder.AddColumn<int>(
                name: "NativeRegion",
                table: "PlayerEssences",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PotentialTier",
                table: "PlayerEssences",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "NativeRegion",
                table: "EquippedEssenceSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PotentialTier",
                table: "EquippedEssenceSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }
    }
}
