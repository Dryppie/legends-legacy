using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEssenceSnapshotPotentialFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.Sql(
                """
                UPDATE "EquippedEssenceSnapshots" AS snapshot
                SET "NativeRegion" = essence."NativeRegion",
                    "PotentialTier" = essence."PotentialTier"
                FROM "PlayerEssences" AS essence
                WHERE snapshot."PlayerEssenceId" = essence."Id"
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NativeRegion",
                table: "EquippedEssenceSnapshots");

            migrationBuilder.DropColumn(
                name: "PotentialTier",
                table: "EquippedEssenceSnapshots");
        }
    }
}
