using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Chat;

#nullable disable

namespace Persistence.Chat.Migrations;

[DbContext(typeof(ChatDbContext))]
[Migration("20260822101500_AddConversationEvidenceIndexes")]
public partial class AddConversationEvidenceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_DirectConversationForward",
            table: "ChatMessages",
            columns: new[]
            {
                "ChannelType", "SenderId", "TargetCharacterId",
                "IsSystemGenerated", "SentAt", "Id"
            });
        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_DirectConversationReverse",
            table: "ChatMessages",
            columns: new[]
            {
                "ChannelType", "TargetCharacterId", "SenderId",
                "IsSystemGenerated", "SentAt", "Id"
            });
        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_SharedChannelEvidence",
            table: "ChatMessages",
            columns: new[]
            {
                "ChannelType", "SenderId", "IsSystemGenerated",
                "SentAt", "ContextKey"
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ChatMessages_DirectConversationForward",
            table: "ChatMessages");
        migrationBuilder.DropIndex(
            name: "IX_ChatMessages_DirectConversationReverse",
            table: "ChatMessages");
        migrationBuilder.DropIndex(
            name: "IX_ChatMessages_SharedChannelEvidence",
            table: "ChatMessages");
    }
}
