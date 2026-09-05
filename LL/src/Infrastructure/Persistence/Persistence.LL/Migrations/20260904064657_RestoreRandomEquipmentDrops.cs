using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RestoreRandomEquipmentDrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelEOrdinaryProgress");

            migrationBuilder.DropTable(
                name: "ModelEOrdinarySelectionReceipts");

            migrationBuilder.DropTable(
                name: "ModelEProtectionProgress");

            migrationBuilder.DropTable(
                name: "ModelEProtectionReceipts");

            migrationBuilder.DropColumn(
                name: "ModelECommitment",
                table: "DungeonRuns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelECommitment",
                table: "DungeonRuns",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModelEOrdinaryProgress",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoolId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    HasEnteredRegion = table.Column<bool>(type: "boolean", nullable: false),
                    LastEncounterAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastScheduleGeneration = table.Column<long>(type: "bigint", nullable: false),
                    Plain = table.Column<string>(type: "jsonb", nullable: true),
                    PlainVictories = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Sigil = table.Column<string>(type: "jsonb", nullable: true),
                    SigilVictories = table.Column<int>(type: "integer", nullable: false)
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
                    PoolId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "ModelEProtectionProgress",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoolId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CompletionsWithoutMatch = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    SelectedDefinitionId = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelEProtectionProgress", x => new { x.CharacterId, x.PoolId });
                    table.ForeignKey(
                        name: "FK_ModelEProtectionProgress_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelEProtectionReceipts",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelEProtectionReceipts", x => new { x.CharacterId, x.RunId });
                    table.ForeignKey(
                        name: "FK_ModelEProtectionReceipts_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
