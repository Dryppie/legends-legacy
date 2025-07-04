using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ColosseumMatchResultTracksRatingChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharacterARatingAfter",
                table: "ColosseumMatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CharacterARatingBefore",
                table: "ColosseumMatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CharacterBRatingAfter",
                table: "ColosseumMatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CharacterBRatingBefore",
                table: "ColosseumMatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CharacterARatingAfter",
                table: "ColosseumMatches");

            migrationBuilder.DropColumn(
                name: "CharacterARatingBefore",
                table: "ColosseumMatches");

            migrationBuilder.DropColumn(
                name: "CharacterBRatingAfter",
                table: "ColosseumMatches");

            migrationBuilder.DropColumn(
                name: "CharacterBRatingBefore",
                table: "ColosseumMatches");
        }
    }
}
