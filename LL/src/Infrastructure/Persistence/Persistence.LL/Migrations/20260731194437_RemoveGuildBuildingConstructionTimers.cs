using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGuildBuildingConstructionTimers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "GuildBuildings"
                SET "Level" = GREATEST("Level", COALESCE("TargetLevel", "Level"))
                WHERE "Status" IN ('UnderConstruction', 'Upgrading');

                UPDATE "GuildActivityLogs"
                SET "Type" = 'BuildingConstructed',
                    "Message" = REPLACE("Message", ' construction started.', ' built to level 1.')
                WHERE "Type" = 'BuildingConstructionStarted';

                UPDATE "GuildActivityLogs"
                SET "Type" = 'BuildingUpgraded',
                    "Message" = REPLACE(
                        REPLACE("Message", ' upgrade to level ', ' upgraded to level '),
                        ' started.',
                        '.')
                WHERE "Type" = 'BuildingUpgradeStarted';
                """);

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "GuildBuildings");

            migrationBuilder.DropColumn(
                name: "CompletesAt",
                table: "GuildBuildings");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "GuildBuildings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GuildBuildings");

            migrationBuilder.DropColumn(
                name: "TargetLevel",
                table: "GuildBuildings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "GuildBuildings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletesAt",
                table: "GuildBuildings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "GuildBuildings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "GuildBuildings",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<int>(
                name: "TargetLevel",
                table: "GuildBuildings",
                type: "integer",
                nullable: true);
        }
    }
}
