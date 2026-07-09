using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatureArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterCreatureArchiveEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatureDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatureName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KillCount = table.Column<int>(type: "integer", nullable: false),
                    FirstDefeatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastDefeatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterCreatureArchiveEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCreatureArchiveEntries_CharacterId",
                table: "CharacterCreatureArchiveEntries",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCreatureArchiveEntries_CharacterId_CreatureDefinit~",
                table: "CharacterCreatureArchiveEntries",
                columns: new[] { "CharacterId", "CreatureDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterCreatureArchiveEntries");
        }
    }
}
