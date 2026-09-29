using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class TrackLiveOpsOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdministrationOperationId",
                table: "GameEventOutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OperatorOperations",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Environment = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorOperations", x => new { x.ActorSubject, x.Environment, x.OperationId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxMessages_AdministrationOperationId",
                table: "GameEventOutboxMessages",
                column: "AdministrationOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorOperations_ActorSubject_Environment_UpdatedAt_Opera~",
                table: "OperatorOperations",
                columns: new[] { "ActorSubject", "Environment", "UpdatedAt", "OperationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperatorOperations");

            migrationBuilder.DropIndex(
                name: "IX_GameEventOutboxMessages_AdministrationOperationId",
                table: "GameEventOutboxMessages");

            migrationBuilder.DropColumn(
                name: "AdministrationOperationId",
                table: "GameEventOutboxMessages");
        }
    }
}
