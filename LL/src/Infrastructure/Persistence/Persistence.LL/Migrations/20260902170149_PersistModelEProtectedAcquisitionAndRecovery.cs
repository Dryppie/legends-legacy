using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PersistModelEProtectedAcquisitionAndRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelEData",
                table: "RunRewards",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelECommitment",
                table: "DungeonRuns",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModelEBaselineRecoveryReceipts",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelEBaselineRecoveryReceipts", x => new { x.CharacterId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_ModelEBaselineRecoveryReceipts_Entities_CharacterId",
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
                    SelectedDefinitionId = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    CompletionsWithoutMatch = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
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
                    Outcome = table.Column<string>(type: "jsonb", nullable: false),
                    ClaimedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelEBaselineRecoveryReceipts");

            migrationBuilder.DropTable(
                name: "ModelEProtectionProgress");

            migrationBuilder.DropTable(
                name: "ModelEProtectionReceipts");

            migrationBuilder.DropColumn(
                name: "ModelEData",
                table: "RunRewards");

            migrationBuilder.DropColumn(
                name: "ModelECommitment",
                table: "DungeonRuns");
        }
    }
}
