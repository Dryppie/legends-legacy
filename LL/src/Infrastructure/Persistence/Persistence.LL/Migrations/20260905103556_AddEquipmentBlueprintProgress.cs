using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentBlueprintProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentBlueprintProgress",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Misses = table.Column<int>(type: "integer", nullable: false),
                    LastRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentBlueprintProgress", x => new { x.CharacterId, x.FamilyId });
                    table.ForeignKey(
                        name: "FK_EquipmentBlueprintProgress_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentBlueprintProgress");
        }
    }
}
