using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ExplicitActionTiming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BlockedUntilUtc",
                table: "CharacterActions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextResolutionAtUtc",
                table: "CharacterActions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ScheduleGeneration",
                table: "CharacterActions",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            // ActionDetails uses TPH: ActionType 1 is combat and 3 is crafting.
            // Deleted rows no longer have details, so their legacy UpdatedAt is
            // retained only as a possible replacement-blocking deadline.
            migrationBuilder.Sql(
                """
                UPDATE "CharacterActions" AS ca
                SET "NextResolutionAtUtc" = CASE
                        WHEN ca."IsDeleted" THEN NULL
                        WHEN EXISTS (
                            SELECT 1 FROM "ActionDetails" AS ad
                            WHERE ad."CharacterActionId" = ca."CharacterId"
                              AND ad."ActionType" = 1)
                            THEN ca."UpdatedAt"
                        WHEN EXISTS (
                            SELECT 1 FROM "ActionDetails" AS ad
                            WHERE ad."CharacterActionId" = ca."CharacterId"
                              AND ad."ActionType" = 3)
                            THEN ca."UpdatedAt" + INTERVAL '10 seconds'
                        ELSE NULL
                    END,
                    "BlockedUntilUtc" = CASE
                        WHEN ca."IsDeleted" THEN ca."UpdatedAt"
                        ELSE NULL
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedUntilUtc",
                table: "CharacterActions");

            migrationBuilder.DropColumn(
                name: "NextResolutionAtUtc",
                table: "CharacterActions");

            migrationBuilder.DropColumn(
                name: "ScheduleGeneration",
                table: "CharacterActions");
        }
    }
}
