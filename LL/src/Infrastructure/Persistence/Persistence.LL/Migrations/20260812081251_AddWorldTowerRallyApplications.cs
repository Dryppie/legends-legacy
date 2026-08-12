using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    [DbContext(typeof(LLDbContext))]
    [Migration("20260812081251_AddWorldTowerRallyApplications")]
    public partial class AddWorldTowerRallyApplications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TowerRallyApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TowerRallyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuildName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PowerRating = table.Column<int>(type: "integer", nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedByCharacterId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerRallyApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowerRallyApplications_CharacterSnapshots_CharacterSnapshotId",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TowerRallyApplications_TowerRallies_TowerRallyId",
                        column: x => x.TowerRallyId,
                        principalTable: "TowerRallies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyApplications_CharacterId",
                table: "TowerRallyApplications",
                column: "CharacterId");
            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyApplications_CharacterSnapshotId",
                table: "TowerRallyApplications",
                column: "CharacterSnapshotId");
            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyApplications_TowerRallyId_AccountId",
                table: "TowerRallyApplications",
                columns: new[] { "TowerRallyId", "AccountId" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyApplications_TowerRallyId_CharacterId",
                table: "TowerRallyApplications",
                columns: new[] { "TowerRallyId", "CharacterId" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyApplications_TowerRallyId_Status",
                table: "TowerRallyApplications",
                columns: new[] { "TowerRallyId", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(name: "TowerRallyApplications");
    }
}
