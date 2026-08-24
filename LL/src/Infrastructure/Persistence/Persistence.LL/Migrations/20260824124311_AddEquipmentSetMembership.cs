using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentSetMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquipmentSetId",
                table: "ItemInstances",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentSetId",
                table: "EquipmentSnapshot",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstances_EquipmentSetId",
                table: "ItemInstances",
                column: "EquipmentSetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemInstances_EquipmentSetId",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "EquipmentSetId",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "EquipmentSetId",
                table: "EquipmentSnapshot");
        }
    }
}
