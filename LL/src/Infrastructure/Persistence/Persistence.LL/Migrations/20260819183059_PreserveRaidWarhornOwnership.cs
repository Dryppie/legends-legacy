using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class PreserveRaidWarhornOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarhornOwnerCharacterId",
                table: "RaidRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "RaidRuns"
                SET "WarhornOwnerCharacterId" = "LeaderCharacterId"
                WHERE "WarhornOwnerCharacterId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "WarhornOwnerCharacterId",
                table: "RaidRuns",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarhornOwnerCharacterId",
                table: "RaidRuns");
        }
    }
}
