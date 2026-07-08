using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountBindingIdentityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUsername",
                table: "Users",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Entities",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "Username" = btrim("Username"),
                    "NormalizedUsername" = upper(btrim("Username")),
                    "Email" = NULLIF(btrim("Email"), ''),
                    "NormalizedEmail" = upper(NULLIF(btrim("Email"), ''))
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Entities"
                SET "Name" = btrim("Name"),
                    "NormalizedName" = upper(btrim("Name"))
                WHERE "EntityType" = 1
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"NormalizedEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedUsername",
                table: "Users",
                column: "NormalizedUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entities_NormalizedName",
                table: "Entities",
                column: "NormalizedName",
                unique: true,
                filter: "\"EntityType\" = 1 AND \"NormalizedName\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedUsername",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Entities_NormalizedName",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedUsername",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Entities");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldNullable: true);
        }
    }
}
