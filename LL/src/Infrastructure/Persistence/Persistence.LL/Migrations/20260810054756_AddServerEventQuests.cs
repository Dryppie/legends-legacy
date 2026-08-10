using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations;

public partial class AddServerEventQuests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventQuestEventLedgers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventQuestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                ObjectiveKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                ContributionAmount = table.Column<long>(type: "bigint", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EventQuestEventLedgers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "EventQuestInstances",
            columns: table => new
            {
                EventQuestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                DefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ClaimEndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RowVersion = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EventQuestInstances", x => x.EventQuestId));

        migrationBuilder.CreateTable(
            name: "EventQuestCharacterContributions",
            columns: table => new
            {
                EventQuestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                TotalAmount = table.Column<long>(type: "bigint", nullable: false),
                LastContributedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventQuestCharacterContributions", x => new { x.EventQuestId, x.CharacterId });
                table.ForeignKey(
                    name: "FK_EventQuestCharacterContributions_EventQuestInstances_EventQ~",
                    column: x => x.EventQuestId,
                    principalTable: "EventQuestInstances",
                    principalColumn: "EventQuestId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EventQuestObjectiveProgresses",
            columns: table => new
            {
                EventQuestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                ObjectiveKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CurrentAmount = table.Column<long>(type: "bigint", nullable: false),
                RequiredAmount = table.Column<long>(type: "bigint", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventQuestObjectiveProgresses", x => new { x.EventQuestId, x.ObjectiveKey });
                table.ForeignKey(
                    name: "FK_EventQuestObjectiveProgresses_EventQuestInstances_EventQues~",
                    column: x => x.EventQuestId,
                    principalTable: "EventQuestInstances",
                    principalColumn: "EventQuestId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EventQuestRewardClaims",
            columns: table => new
            {
                EventQuestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventQuestRewardClaims", x => new { x.EventQuestId, x.CharacterId });
                table.ForeignKey(
                    name: "FK_EventQuestRewardClaims_EventQuestInstances_EventQuestId",
                    column: x => x.EventQuestId,
                    principalTable: "EventQuestInstances",
                    principalColumn: "EventQuestId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EventQuestCharacterContributions_EventQuestId_TotalAmount",
            table: "EventQuestCharacterContributions",
            columns: new[] { "EventQuestId", "TotalAmount" });
        migrationBuilder.CreateIndex(
            name: "IX_EventQuestEventLedgers_EventQuestId_ObjectiveKey_OutboxMess~",
            table: "EventQuestEventLedgers",
            columns: new[] { "EventQuestId", "ObjectiveKey", "OutboxMessageId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_EventQuestEventLedgers_ProcessedAt",
            table: "EventQuestEventLedgers",
            column: "ProcessedAt");
        migrationBuilder.CreateIndex(
            name: "IX_EventQuestInstances_StartsAtUtc_EndsAtUtc",
            table: "EventQuestInstances",
            columns: new[] { "StartsAtUtc", "EndsAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_EventQuestInstances_Status",
            table: "EventQuestInstances",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EventQuestCharacterContributions");
        migrationBuilder.DropTable(name: "EventQuestEventLedgers");
        migrationBuilder.DropTable(name: "EventQuestObjectiveProgresses");
        migrationBuilder.DropTable(name: "EventQuestRewardClaims");
        migrationBuilder.DropTable(name: "EventQuestInstances");
    }
}
