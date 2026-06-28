using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCombatStyles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerCombatStyleNodes");

            migrationBuilder.DropTable(
                name: "PlayerCombatStyles");

            migrationBuilder.DropColumn(
                name: "CombatStyle",
                table: "CharacterSnapshots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CombatStyle",
                table: "CharacterSnapshots",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlayerCombatStyleNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NodeId = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    StyleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCombatStyleNodes", x => x.Id);
                    table.CheckConstraint("CK_PlayerCombatStyleNodes_Rank", "\"Rank\" >= 0");
                    table.ForeignKey(
                        name: "FK_PlayerCombatStyleNodes_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerCombatStyles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    SelectedFocusId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StyleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCombatStyles", x => x.Id);
                    table.CheckConstraint("CK_PlayerCombatStyles_Experience", "\"Experience\" >= 0");
                    table.CheckConstraint("CK_PlayerCombatStyles_Level", "\"Level\" >= 1 AND \"Level\" <= 50");
                    table.ForeignKey(
                        name: "FK_PlayerCombatStyles_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCombatStyleNodes_CharacterId",
                table: "PlayerCombatStyleNodes",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCombatStyleNodes_CharacterId_StyleId_NodeId",
                table: "PlayerCombatStyleNodes",
                columns: new[] { "CharacterId", "StyleId", "NodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCombatStyles_CharacterId",
                table: "PlayerCombatStyles",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCombatStyles_CharacterId_StyleId",
                table: "PlayerCombatStyles",
                columns: new[] { "CharacterId", "StyleId" },
                unique: true);
        }
    }
}
