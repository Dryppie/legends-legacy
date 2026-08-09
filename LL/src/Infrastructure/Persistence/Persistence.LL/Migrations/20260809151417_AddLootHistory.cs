using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddLootHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LootHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LootHistoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LootHistoryEntries_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LootHistoryEntries_CharacterId_ReceivedAt",
                table: "LootHistoryEntries",
                columns: new[] { "CharacterId", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LootHistoryEntries");
        }
    }
}
