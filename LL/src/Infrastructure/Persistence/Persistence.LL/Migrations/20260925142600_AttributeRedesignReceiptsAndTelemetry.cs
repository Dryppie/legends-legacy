using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AttributeRedesignReceiptsAndTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttributeRulesVersion",
                table: "CharacterSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 17);

            migrationBuilder.CreateTable(
                name: "EquipmentMigrationReceipts",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<int>(type: "integer", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: false),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MappingReason = table.Column<string>(type: "text", nullable: false),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RolledBackAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ChoiceUsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ChoiceOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChosenDefinitionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ChoiceResultJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentMigrationReceipts", x => x.OperationId);
                });

            migrationBuilder.CreateTable(
                name: "ItemizationDailyReports",
                columns: table => new
                {
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemizationDailyReports", x => x.Day);
                });

            migrationBuilder.CreateTable(
                name: "ItemizationObservations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemizationObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemizationObservations_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMigrationReceipts_ItemId",
                table: "EquipmentMigrationReceipts",
                column: "ItemId",
                unique: true,
                filter: "\"RolledBackAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemizationObservations_CharacterId_OccurredAtUtc",
                table: "ItemizationObservations",
                columns: new[] { "CharacterId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemizationObservations_OccurredAtUtc",
                table: "ItemizationObservations",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentMigrationReceipts");

            migrationBuilder.DropTable(
                name: "ItemizationDailyReports");

            migrationBuilder.DropTable(
                name: "ItemizationObservations");

            migrationBuilder.DropColumn(
                name: "AttributeRulesVersion",
                table: "CharacterSnapshots");
        }
    }
}
