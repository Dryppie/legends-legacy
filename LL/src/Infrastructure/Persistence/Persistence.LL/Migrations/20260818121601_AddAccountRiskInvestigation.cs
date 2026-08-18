using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountRiskInvestigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountRiskHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SignalsJson = table.Column<string>(type: "jsonb", nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRiskHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountRiskInvestigations",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UpdatedBySubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRiskInvestigations", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "AccountRiskNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRiskNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountRiskSnapshots",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CharacterLevel = table.Column<int>(type: "integer", nullable: false),
                    AccountCreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSessionUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PrimarySignalType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: true),
                    PrimaryReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ConnectedAccountCount = table.Column<int>(type: "integer", nullable: false),
                    IncomingCinders = table.Column<long>(type: "bigint", nullable: false),
                    OutgoingCinders = table.Column<long>(type: "bigint", nullable: false),
                    TransferCount = table.Column<int>(type: "integer", nullable: false),
                    FirstFlaggedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTriggeredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SignalsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RelationshipsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRiskSnapshots", x => x.AccountId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRiskHistory_AccountId_EvaluatedAt",
                table: "AccountRiskHistory",
                columns: new[] { "AccountId", "EvaluatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRiskInvestigations_Status_UpdatedAt",
                table: "AccountRiskInvestigations",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRiskNotes_AccountId_CreatedAt",
                table: "AccountRiskNotes",
                columns: new[] { "AccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRiskSnapshots_EvaluatedAt",
                table: "AccountRiskSnapshots",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRiskSnapshots_LastTriggeredAt",
                table: "AccountRiskSnapshots",
                column: "LastTriggeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRiskSnapshots_PrimarySignalType",
                table: "AccountRiskSnapshots",
                column: "PrimarySignalType");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRiskSnapshots_Severity_Score",
                table: "AccountRiskSnapshots",
                columns: new[] { "Severity", "Score" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountRiskHistory");

            migrationBuilder.DropTable(
                name: "AccountRiskInvestigations");

            migrationBuilder.DropTable(
                name: "AccountRiskNotes");

            migrationBuilder.DropTable(
                name: "AccountRiskSnapshots");
        }
    }
}
