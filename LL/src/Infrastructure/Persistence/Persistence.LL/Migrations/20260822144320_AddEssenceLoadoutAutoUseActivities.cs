using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEssenceLoadoutAutoUseActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoUseActivities",
                table: "EssenceLoadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoUseActivities",
                table: "EssenceLoadouts");
        }
    }
}
