using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerTransferHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerTransferHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SenderAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCharacterName = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    RecipientAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientCharacterName = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    AssetId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AssetName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SourceItemInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationItemInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTransferHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTransferHistory_DestinationItemInstanceId",
                table: "PlayerTransferHistory",
                column: "DestinationItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTransferHistory_Kind_OccurredAt",
                table: "PlayerTransferHistory",
                columns: new[] { "Kind", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTransferHistory_RecipientAccountId_OccurredAt",
                table: "PlayerTransferHistory",
                columns: new[] { "RecipientAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTransferHistory_RecipientAccountId_SenderAccountId_Oc~",
                table: "PlayerTransferHistory",
                columns: new[] { "RecipientAccountId", "SenderAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTransferHistory_RecipientCharacterId_OccurredAt",
                table: "PlayerTransferHistory",
                columns: new[] { "RecipientCharacterId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTransferHistory_SenderAccountId_OccurredAt",
                table: "PlayerTransferHistory",
                columns: new[] { "SenderAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTransferHistory_SenderCharacterId_OccurredAt",
                table: "PlayerTransferHistory",
                columns: new[] { "SenderCharacterId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTransferHistory_SourceItemInstanceId",
                table: "PlayerTransferHistory",
                column: "SourceItemInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerTransferHistory");
        }
    }
}
