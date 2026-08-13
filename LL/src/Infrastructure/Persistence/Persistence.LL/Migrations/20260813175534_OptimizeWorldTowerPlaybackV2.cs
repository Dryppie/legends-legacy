using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeWorldTowerPlaybackV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimelineJson",
                table: "TowerCombatPlaybacks",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<string>(
                name: "BundleContentEncoding",
                table: "TowerCombatPlaybacks",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BundleContentType",
                table: "TowerCombatPlaybacks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BundleHash",
                table: "TowerCombatPlaybacks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BundleLength",
                table: "TowerCombatPlaybacks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TowerCombatPlaybackArtifacts",
                columns: table => new
                {
                    TowerAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleBytes = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerCombatPlaybackArtifacts", x => x.TowerAttemptId);
                    table.ForeignKey(
                        name: "FK_TowerCombatPlaybackArtifacts_TowerCombatPlaybacks_TowerAtte~",
                        column: x => x.TowerAttemptId,
                        principalTable: "TowerCombatPlaybacks",
                        principalColumn: "TowerAttemptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt",
                table: "TowerCombatPlaybacks",
                column: "PlaybackEndsAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TowerCombatPlaybackArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "BundleContentEncoding",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "BundleContentType",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "BundleHash",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "BundleLength",
                table: "TowerCombatPlaybacks");

            migrationBuilder.AlterColumn<string>(
                name: "TimelineJson",
                table: "TowerCombatPlaybacks",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
