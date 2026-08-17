using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Chat.Migrations
{
    /// <inheritdoc />
    public partial class AddChatModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatModerationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestrictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatModerationActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatRestrictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedBySubject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedBySubject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatRestrictions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatModerationActions_OccurredAt",
                table: "ChatModerationActions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatModerationActions_RestrictionId",
                table: "ChatModerationActions",
                column: "RestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatModerationActions_TargetCharacterId_OccurredAt",
                table: "ChatModerationActions",
                columns: new[] { "TargetCharacterId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatRestrictions_CreatedAt",
                table: "ChatRestrictions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRestrictions_TargetCharacterId_RevokedAt_ExpiresAt",
                table: "ChatRestrictions",
                columns: new[] { "TargetCharacterId", "RevokedAt", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatModerationActions");

            migrationBuilder.DropTable(
                name: "ChatRestrictions");
        }
    }
}
