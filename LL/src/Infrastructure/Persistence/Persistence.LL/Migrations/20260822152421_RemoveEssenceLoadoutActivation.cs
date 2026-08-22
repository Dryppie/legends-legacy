using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEssenceLoadoutActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "EssenceLoadouts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "EssenceLoadouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
