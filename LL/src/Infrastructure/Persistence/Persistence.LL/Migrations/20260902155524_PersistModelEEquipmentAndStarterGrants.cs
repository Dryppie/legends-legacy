using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PersistModelEEquipmentAndStarterGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelEData",
                table: "ItemInstances",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelEData",
                table: "EquipmentSnapshot",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModelEStarterGrants",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    GrantedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Equipment = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelEStarterGrants", x => new { x.CharacterId, x.Kind });
                    table.ForeignKey(
                        name: "FK_ModelEStarterGrants_Entities_CharacterId",
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
                name: "ModelEStarterGrants");

            migrationBuilder.DropColumn(
                name: "ModelEData",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "ModelEData",
                table: "EquipmentSnapshot");
        }
    }
}
