using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class MakeCombatStylesGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the former default (Activity.None = 0) as the one global selection.
            // Activity-specific choices are retired rather than promoted when no default exists.
            // Earned style progression and captured combat snapshots remain untouched.
            migrationBuilder.Sql("""
                DELETE FROM "CharacterCombatStyleSelections" WHERE "Activity" <> 0;
                """);

            migrationBuilder.DropTable(
                name: "CharacterBuildEquipmentSelections");

            migrationBuilder.DropTable(
                name: "CharacterBuildEssenceSelections");

            migrationBuilder.DropTable(
                name: "CharacterBuildPresets");

            migrationBuilder.DropIndex(
                name: "IX_EssenceLoadouts_CharacterId_IsDefault",
                table: "EssenceLoadouts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CharacterCombatStyleSelections",
                table: "CharacterCombatStyleSelections");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "EssenceLoadouts");

            migrationBuilder.DropColumn(
                name: "BastionPracticeCompleted",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "CombatStylesIntroductionCompletedAt",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ConduitPracticeCompleted",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Activity",
                table: "CharacterCombatStyleSelections");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CharacterCombatStyleSelections",
                table: "CharacterCombatStyleSelections",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CharacterCombatStyleSelections",
                table: "CharacterCombatStyleSelections");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "EssenceLoadouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BastionPracticeCompleted",
                table: "Entities",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CombatStylesIntroductionCompletedAt",
                table: "Entities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConduitPracticeCompleted",
                table: "Entities",
                type: "boolean",
                nullable: true);

            // Character-only properties share the Entities table, but the old CLR model
            // requires non-null booleans for existing characters after a downgrade.
            migrationBuilder.Sql("""
                UPDATE "Entities"
                SET "BastionPracticeCompleted" = FALSE, "ConduitPracticeCompleted" = FALSE
                WHERE "EntityType" = 1;
                """);

            migrationBuilder.AddColumn<int>(
                name: "Activity",
                table: "CharacterCombatStyleSelections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CharacterCombatStyleSelections",
                table: "CharacterCombatStyleSelections",
                columns: new[] { "CharacterId", "Activity" });

            migrationBuilder.CreateTable(
                name: "CharacterBuildPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatStyleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FocusPlayerEssenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RefinementId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceActivity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpgradeIds = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBuildPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBuildPresets_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBuildEquipmentSelections",
                columns: table => new
                {
                    CharacterBuildPresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotType = table.Column<int>(type: "integer", nullable: false),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBuildEquipmentSelections", x => new { x.CharacterBuildPresetId, x.SlotType });
                    table.ForeignKey(
                        name: "FK_CharacterBuildEquipmentSelections_CharacterBuildPresets_Cha~",
                        column: x => x.CharacterBuildPresetId,
                        principalTable: "CharacterBuildPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBuildEssenceSelections",
                columns: table => new
                {
                    CharacterBuildPresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    PlayerEssenceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBuildEssenceSelections", x => new { x.CharacterBuildPresetId, x.SlotIndex });
                    table.ForeignKey(
                        name: "FK_CharacterBuildEssenceSelections_CharacterBuildPresets_Chara~",
                        column: x => x.CharacterBuildPresetId,
                        principalTable: "CharacterBuildPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EssenceLoadouts_CharacterId_IsDefault",
                table: "EssenceLoadouts",
                columns: new[] { "CharacterId", "IsDefault" },
                unique: true,
                filter: "\"IsDefault\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBuildPresets_CharacterId_Name",
                table: "CharacterBuildPresets",
                columns: new[] { "CharacterId", "Name" },
                unique: true);
        }
    }
}
