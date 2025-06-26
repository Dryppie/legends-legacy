using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class GuildResourceEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Temporarily rename the old column
            migrationBuilder.RenameColumn(
                name: "Resource",
                table: "GuildResource",
                newName: "Resource_Old");

            // Add a new column with the correct type
            migrationBuilder.AddColumn<int>(
                name: "Resource",
                table: "GuildResource",
                type: "integer",
                nullable: false,
                defaultValue: 0); // or whatever default makes sense

            // Copy and convert values (you MUST ensure these string values match enum names or indices)
            migrationBuilder.Sql(@"
                UPDATE ""GuildResource""
                SET ""Resource"" = 
                    CASE ""Resource_Old""
                        WHEN 'Cinder' THEN 0
                        WHEN 'Soulstone' THEN 1
                    END
            ");

            // Drop the old column
            migrationBuilder.DropColumn(
                name: "Resource_Old",
                table: "GuildResource");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add the old column
            migrationBuilder.AddColumn<string>(
                name: "Resource_Old",
                table: "GuildResource",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Convert enum int back to string
            migrationBuilder.Sql(@"
                UPDATE ""GuildResource""
                SET ""Resource_Old"" = 
                    CASE ""Resource""
                        WHEN 0 THEN 'Cinder'
                        WHEN 1 THEN 'Soulstone'
                    END
            ");

            // Drop the int column
            migrationBuilder.DropColumn(
                name: "Resource",
                table: "GuildResource");

            // Rename back
            migrationBuilder.RenameColumn(
                name: "Resource_Old",
                table: "GuildResource",
                newName: "Resource");
        }
    }
}
