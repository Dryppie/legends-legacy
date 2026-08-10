using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildVaultWithdrawPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanWithdrawVault",
                table: "GuildRolePermissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "GuildRolePermissions"
                SET "CanWithdrawVault" = TRUE
                WHERE "Role" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanWithdrawVault",
                table: "GuildRolePermissions");
        }
    }
}
