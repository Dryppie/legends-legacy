using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyProphecyReroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DailyRerollUsedAt",
                table: "PlayerProphecyInstances",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyRerollUsedAt",
                table: "PlayerProphecyInstances");
        }
    }
}
