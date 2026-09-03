using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEquipmentForge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelECharacterStyles");

            migrationBuilder.DropTable(
                name: "ModelEForgeReceipts");

            migrationBuilder.DropColumn(
                name: "BlueprintSelectionBoxes",
                table: "TournamentRewardGrants");

            migrationBuilder.DropColumn(
                name: "TemperedScrap",
                table: "TournamentRewardGrants");

            migrationBuilder.DropColumn(
                name: "ScrapRemainder",
                table: "ModelEOrdinaryProgress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlueprintSelectionBoxes",
                table: "TournamentRewardGrants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TemperedScrap",
                table: "TournamentRewardGrants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScrapRemainder",
                table: "ModelEOrdinaryProgress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ModelECharacterStyles",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    StyleId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FreeApplicationOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearnedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelECharacterStyles", x => new { x.CharacterId, x.StyleId });
                    table.ForeignKey(
                        name: "FK_ModelECharacterStyles_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelEForgeReceipts",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "jsonb", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelEForgeReceipts", x => new { x.CharacterId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_ModelEForgeReceipts_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
