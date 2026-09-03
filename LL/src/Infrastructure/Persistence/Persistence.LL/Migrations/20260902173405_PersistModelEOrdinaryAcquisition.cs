using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PersistModelEOrdinaryAcquisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelEOrdinaryProgress",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoolId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    HasEnteredRegion = table.Column<bool>(type: "boolean", nullable: false),
                    PlainVictories = table.Column<int>(type: "integer", nullable: false),
                    SigilVictories = table.Column<int>(type: "integer", nullable: false),
                    ScrapRemainder = table.Column<int>(type: "integer", nullable: false),
                    LastScheduleGeneration = table.Column<long>(type: "bigint", nullable: false),
                    LastEncounterAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Plain = table.Column<string>(type: "jsonb", nullable: true),
                    Sigil = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelEOrdinaryProgress", x => new { x.CharacterId, x.PoolId });
                    table.ForeignKey(
                        name: "FK_ModelEOrdinaryProgress_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelEOrdinarySelectionReceipts",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    SigilFamilyId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelEOrdinarySelectionReceipts", x => new { x.CharacterId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_ModelEOrdinarySelectionReceipts_Entities_CharacterId",
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
                name: "ModelEOrdinaryProgress");

            migrationBuilder.DropTable(
                name: "ModelEOrdinarySelectionReceipts");
        }
    }
}
