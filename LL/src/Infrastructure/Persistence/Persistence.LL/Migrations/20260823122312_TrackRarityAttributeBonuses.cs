using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class TrackRarityAttributeBonuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "RarityBonusAmount",
                table: "InstanceAttributeModifier",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RarityBonusAmount",
                table: "EquipmentAttributeModifierSnapshot",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RarityBonusAmount",
                table: "InstanceAttributeModifier");

            migrationBuilder.DropColumn(
                name: "RarityBonusAmount",
                table: "EquipmentAttributeModifierSnapshot");
        }
    }
}
