using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class VersionEquipmentRebalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RespecializationAllowance",
                table: "EquipmentMigrationReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "EquipmentMigrationReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing receipts represent the one-time 17 -> 18 conversion.
            migrationBuilder.Sql("UPDATE \"EquipmentMigrationReceipts\" SET \"RespecializationAllowance\" = 1");
            migrationBuilder.DropIndex(name: "IX_EquipmentMigrationReceipts_ItemId", table: "EquipmentMigrationReceipts");
            migrationBuilder.CreateIndex(name: "IX_EquipmentMigrationReceipts_ItemId_Revision", table: "EquipmentMigrationReceipts",
                columns: new[] { "ItemId", "Revision" }, unique: true, filter: "\"RolledBackAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Fail transactionally if later rebalances have not first been rolled back.
            migrationBuilder.DropIndex(name: "IX_EquipmentMigrationReceipts_ItemId_Revision", table: "EquipmentMigrationReceipts");
            migrationBuilder.CreateIndex(name: "IX_EquipmentMigrationReceipts_ItemId", table: "EquipmentMigrationReceipts",
                column: "ItemId", unique: true, filter: "\"RolledBackAtUtc\" IS NULL");
            migrationBuilder.DropColumn(
                name: "RespecializationAllowance",
                table: "EquipmentMigrationReceipts");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "EquipmentMigrationReceipts");
        }
    }
}
