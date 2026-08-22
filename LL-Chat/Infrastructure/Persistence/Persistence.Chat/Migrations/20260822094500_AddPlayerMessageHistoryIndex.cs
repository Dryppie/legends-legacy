using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Chat;

#nullable disable

namespace Persistence.Chat.Migrations;

[DbContext(typeof(ChatDbContext))]
[Migration("20260822094500_AddPlayerMessageHistoryIndex")]
public partial class AddPlayerMessageHistoryIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_SenderId_IsSystemGenerated_SentAt_Id",
            table: "ChatMessages",
            columns: new[] { "SenderId", "IsSystemGenerated", "SentAt", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ChatMessages_SenderId_IsSystemGenerated_SentAt_Id",
            table: "ChatMessages");
    }
}
