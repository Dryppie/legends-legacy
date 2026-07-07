using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddGameEventOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameEventOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEventOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameEventOutboxDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Consumer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEventOutboxDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameEventOutboxDeliveries_GameEventOutboxMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "GameEventOutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxDeliveries_Consumer_Status_AvailableAt",
                table: "GameEventOutboxDeliveries",
                columns: new[] { "Consumer", "Status", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxDeliveries_MessageId_Consumer",
                table: "GameEventOutboxDeliveries",
                columns: new[] { "MessageId", "Consumer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxDeliveries_Status_AvailableAt_CreatedAt",
                table: "GameEventOutboxDeliveries",
                columns: new[] { "Status", "AvailableAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxMessages_AvailableAt_CreatedAt",
                table: "GameEventOutboxMessages",
                columns: new[] { "AvailableAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxMessages_CharacterId_CreatedAt",
                table: "GameEventOutboxMessages",
                columns: new[] { "CharacterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxMessages_IdempotencyKey",
                table: "GameEventOutboxMessages",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameEventOutboxDeliveries");

            migrationBuilder.DropTable(
                name: "GameEventOutboxMessages");
        }
    }
}
