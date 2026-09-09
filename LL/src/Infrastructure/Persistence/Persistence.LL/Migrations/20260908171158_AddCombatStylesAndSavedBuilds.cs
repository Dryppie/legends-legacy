using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatStylesAndSavedBuilds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            // Character-only properties share the Entities table and therefore have nullable
            // columns. Existing characters still require real false values when materialized.
            migrationBuilder.Sql("""
                UPDATE "Entities"
                SET "BastionPracticeCompleted" = FALSE, "ConduitPracticeCompleted" = FALSE
                WHERE "EntityType" = 1;
                """);

            migrationBuilder.AddColumn<string>(
                name: "CombatStyle",
                table: "CharacterSnapshots",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CharacterBuildPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceActivity = table.Column<int>(type: "integer", nullable: false),
                    CombatStyleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RefinementId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UpgradeIds = table.Column<string[]>(type: "text[]", nullable: false),
                    FocusPlayerEssenceId = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "CharacterCombatStyles",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatStyleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CurrentXp = table.Column<long>(type: "bigint", nullable: false),
                    RefinementId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UpgradeIds = table.Column<string[]>(type: "text[]", nullable: false),
                    FocusPlayerEssenceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterCombatStyles", x => new { x.CharacterId, x.CombatStyleId });
                    table.CheckConstraint("CK_CharacterCombatStyles_Progression", "\"Level\" BETWEEN 1 AND 10 AND \"CurrentXp\" >= 0 AND (\"Level\" < 10 OR \"CurrentXp\" = 0)");
                    table.ForeignKey(
                        name: "FK_CharacterCombatStyles_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterCombatStyleSelections",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Activity = table.Column<int>(type: "integer", nullable: false),
                    CombatStyleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RefinementId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UpgradeIds = table.Column<string[]>(type: "text[]", nullable: false),
                    FocusPlayerEssenceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterCombatStyleSelections", x => new { x.CharacterId, x.Activity });
                    table.ForeignKey(
                        name: "FK_CharacterCombatStyleSelections_Entities_CharacterId",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterBuildEquipmentSelections");

            migrationBuilder.DropTable(
                name: "CharacterBuildEssenceSelections");

            migrationBuilder.DropTable(
                name: "CharacterCombatStyles");

            migrationBuilder.DropTable(
                name: "CharacterCombatStyleSelections");

            migrationBuilder.DropTable(
                name: "CharacterBuildPresets");

            migrationBuilder.DropIndex(
                name: "IX_EssenceLoadouts_CharacterId_IsDefault",
                table: "EssenceLoadouts");

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
                name: "CombatStyle",
                table: "CharacterSnapshots");
        }
    }
}
