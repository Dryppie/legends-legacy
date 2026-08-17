using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveOpsAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountRestrictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestrictionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InternalNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedBySubject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedBySubject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRestrictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountRestrictions_Users_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdminActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Permission = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    TargetAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InternalNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRestrictions_AccountId_RestrictionType_RevokedAt_Exp~",
                table: "AccountRestrictions",
                columns: new[] { "AccountId", "RestrictionType", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRestrictions_CreatedAt",
                table: "AccountRestrictions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActions_OccurredAt",
                table: "AdminActions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActions_TargetAccountId_OccurredAt",
                table: "AdminActions",
                columns: new[] { "TargetAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminActions_TargetCharacterId_OccurredAt",
                table: "AdminActions",
                columns: new[] { "TargetCharacterId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminActions_TargetResourceId",
                table: "AdminActions",
                column: "TargetResourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountRestrictions");

            migrationBuilder.DropTable(
                name: "AdminActions");
        }
    }
}
