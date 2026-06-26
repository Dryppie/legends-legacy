using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class SplitCharacterArenaProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterArenaProfiles",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    LifetimeHighestRating = table.Column<int>(type: "integer", nullable: false),
                    Glory = table.Column<int>(type: "integer", nullable: false),
                    CurrentAttackWinStreak = table.Column<int>(type: "integer", nullable: false),
                    BestAttackWinStreak = table.Column<int>(type: "integer", nullable: false),
                    AttackWins = table.Column<int>(type: "integer", nullable: false),
                    AttackDraws = table.Column<int>(type: "integer", nullable: false),
                    AttackLosses = table.Column<int>(type: "integer", nullable: false),
                    DefenseWins = table.Column<int>(type: "integer", nullable: false),
                    DefenseDraws = table.Column<int>(type: "integer", nullable: false),
                    DefenseLosses = table.Column<int>(type: "integer", nullable: false),
                    LastFirstWinBonusAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterArenaProfiles", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_CharacterArenaProfiles_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "CharacterArenaProfiles" (
                    "CharacterId",
                    "Rating",
                    "LifetimeHighestRating",
                    "Glory",
                    "CurrentAttackWinStreak",
                    "BestAttackWinStreak",
                    "AttackWins",
                    "AttackDraws",
                    "AttackLosses",
                    "DefenseWins",
                    "DefenseDraws",
                    "DefenseLosses",
                    "LastFirstWinBonusAt")
                SELECT
                    "Id",
                    COALESCE("ArenaRating", 1000),
                    COALESCE("ArenaLifetimeHighestRating", COALESCE("ArenaRating", 1000)),
                    COALESCE("ArenaGlory", 0),
                    COALESCE("ArenaCurrentAttackWinStreak", 0),
                    COALESCE("ArenaBestAttackWinStreak", 0),
                    COALESCE("ArenaAttackWins", 0),
                    COALESCE("ArenaAttackDraws", 0),
                    COALESCE("ArenaAttackLosses", 0),
                    COALESCE("ArenaDefenseWins", 0),
                    COALESCE("ArenaDefenseDraws", 0),
                    COALESCE("ArenaDefenseLosses", 0),
                    "ArenaLastFirstWinBonusAt"
                FROM "Entities"
                WHERE "EntityType" = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterArenaProfiles_Rating",
                table: "CharacterArenaProfiles",
                column: "Rating");

            migrationBuilder.DropColumn(
                name: "ArenaAttackDraws",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaAttackLosses",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaAttackWins",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaBestAttackWinStreak",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaCurrentAttackWinStreak",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaDailyDefenseCounterDate",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaDailyDefensiveGloryEarned",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaDailyIncomingRatedDefenseCount",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaDefenseDraws",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaDefenseLosses",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaDefenseWins",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaGlory",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaLastFirstWinBonusAt",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaLifetimeHighestRating",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "ArenaRating",
                table: "Entities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArenaAttackDraws",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaAttackLosses",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaAttackWins",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaBestAttackWinStreak",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaCurrentAttackWinStreak",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArenaDailyDefenseCounterDate",
                table: "Entities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaDailyDefensiveGloryEarned",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaDailyIncomingRatedDefenseCount",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaDefenseDraws",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaDefenseLosses",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaDefenseWins",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaGlory",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArenaLastFirstWinBonusAt",
                table: "Entities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaLifetimeHighestRating",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArenaRating",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Entities" AS e
                SET
                    "ArenaRating" = p."Rating",
                    "ArenaLifetimeHighestRating" = p."LifetimeHighestRating",
                    "ArenaGlory" = p."Glory",
                    "ArenaCurrentAttackWinStreak" = p."CurrentAttackWinStreak",
                    "ArenaBestAttackWinStreak" = p."BestAttackWinStreak",
                    "ArenaAttackWins" = p."AttackWins",
                    "ArenaAttackDraws" = p."AttackDraws",
                    "ArenaAttackLosses" = p."AttackLosses",
                    "ArenaDefenseWins" = p."DefenseWins",
                    "ArenaDefenseDraws" = p."DefenseDraws",
                    "ArenaDefenseLosses" = p."DefenseLosses",
                    "ArenaLastFirstWinBonusAt" = p."LastFirstWinBonusAt"
                FROM "CharacterArenaProfiles" AS p
                WHERE e."Id" = p."CharacterId";
                """);

            migrationBuilder.DropTable(
                name: "CharacterArenaProfiles");
        }
    }
}
