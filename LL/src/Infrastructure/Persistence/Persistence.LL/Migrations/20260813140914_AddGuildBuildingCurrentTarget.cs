using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildBuildingCurrentTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentBuildingTargetLevel",
                table: "Guilds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentBuildingTargetType",
                table: "Guilds",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentBuildingTargetLevel",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "CurrentBuildingTargetType",
                table: "Guilds");
        }
    }
}
