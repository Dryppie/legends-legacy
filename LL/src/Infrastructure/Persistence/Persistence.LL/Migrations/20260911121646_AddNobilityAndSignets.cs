using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddNobilityAndSignets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PresetSlot",
                table: "EssenceLoadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PresetSlot",
                table: "EquipmentLoadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FreeRerollsUsed",
                table: "DailyProphecyRerollStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaidRerollsUsed",
                table: "DailyProphecyRerollStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                WITH slots AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "CharacterId" ORDER BY "CreatedAt", "Id") AS slot
                    FROM "EssenceLoadouts"
                ) UPDATE "EssenceLoadouts" AS target SET "PresetSlot" = slots.slot FROM slots WHERE target."Id" = slots."Id";
                WITH slots AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "CharacterId" ORDER BY "CreatedAt", "Id") AS slot
                    FROM "EquipmentLoadouts"
                ) UPDATE "EquipmentLoadouts" AS target SET "PresetSlot" = slots.slot FROM slots WHERE target."Id" = slots."Id";
                UPDATE "DailyProphecyRerollStates"
                SET "FreeRerollsUsed" = LEAST("RerollsUsed", 1), "PaidRerollsUsed" = GREATEST("RerollsUsed" - 1, 0);
                """);
            migrationBuilder.CreateTable(
                name: "NobilityMemberships",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    ShowBadge = table.Column<bool>(type: "boolean", nullable: false),
                    ShowHeader = table.Column<bool>(type: "boolean", nullable: false),
                    Ornament = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DailyRewardsThrough = table.Column<DateOnly>(type: "date", nullable: true),
                    NextDailyRewardAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NobilityMemberships", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_NobilityMemberships_Entities_RewardCharacterId",
                        column: x => x.RewardCharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NobilityMemberships_Users_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignetIssuances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Origin = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Refunded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignetIssuances", x => x.Id);
                    table.CheckConstraint("CK_SignetIssuance_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_SignetIssuances_Users_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignetRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    MembershipVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreviousExpiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignetRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignetRedemptions_Users_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NobilityCoverage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CalendarMonths = table.Column<int>(type: "integer", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NobilityCoverage", x => x.Id);
                    table.CheckConstraint("CK_NobilityCoverage_Duration", "\"EndsAt\" > \"StartsAt\" AND \"CalendarMonths\" > 0");
                    table.ForeignKey(
                        name: "FK_NobilityCoverage_NobilityMemberships_AccountId",
                        column: x => x.AccountId,
                        principalTable: "NobilityMemberships",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NobilityDailyGrants",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SigilFragments = table.Column<int>(type: "integer", nullable: false),
                    Soulstones = table.Column<int>(type: "integer", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NobilityDailyGrants", x => new { x.AccountId, x.GameDate });
                    table.ForeignKey(
                        name: "FK_NobilityDailyGrants_NobilityMemberships_AccountId",
                        column: x => x.AccountId,
                        principalTable: "NobilityMemberships",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignetUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    OwnerCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: true),
                    RedemptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignetUnits", x => x.Id);
                    table.CheckConstraint("CK_SignetUnit_Location", "(\"State\" = 1 AND \"ListingId\" IS NOT NULL AND \"RedemptionId\" IS NULL) OR (\"State\" = 2 AND \"ListingId\" IS NULL AND \"RedemptionId\" IS NOT NULL) OR (\"State\" IN (0, 3) AND \"ListingId\" IS NULL AND \"RedemptionId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_SignetUnits_Entities_OwnerCharacterId",
                        column: x => x.OwnerCharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignetUnits_SignetIssuances_IssuanceId",
                        column: x => x.IssuanceId,
                        principalTable: "SignetIssuances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignetMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    FromCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignetMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignetMovements_SignetUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "SignetUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NobilityCoverage_AccountId_StartsAt",
                table: "NobilityCoverage",
                columns: new[] { "AccountId", "StartsAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NobilityCoverage_EndsAt",
                table: "NobilityCoverage",
                column: "EndsAt");

            migrationBuilder.CreateIndex(
                name: "IX_NobilityMemberships_NextDailyRewardAt",
                table: "NobilityMemberships",
                column: "NextDailyRewardAt");

            migrationBuilder.CreateIndex(
                name: "IX_NobilityMemberships_RewardCharacterId",
                table: "NobilityMemberships",
                column: "RewardCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_SignetIssuances_AccountId_Origin",
                table: "SignetIssuances",
                columns: new[] { "AccountId", "Origin" });

            migrationBuilder.CreateIndex(
                name: "IX_SignetMovements_UnitId_OperationId_Kind",
                table: "SignetMovements",
                columns: new[] { "UnitId", "OperationId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignetRedemptions_AccountId_RedeemedAt",
                table: "SignetRedemptions",
                columns: new[] { "AccountId", "RedeemedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SignetUnits_IssuanceId_Ordinal",
                table: "SignetUnits",
                columns: new[] { "IssuanceId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignetUnits_ListingId",
                table: "SignetUnits",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_SignetUnits_OwnerCharacterId_State",
                table: "SignetUnits",
                columns: new[] { "OwnerCharacterId", "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NobilityCoverage");

            migrationBuilder.DropTable(
                name: "NobilityDailyGrants");

            migrationBuilder.DropTable(
                name: "SignetMovements");

            migrationBuilder.DropTable(
                name: "SignetRedemptions");

            migrationBuilder.DropTable(
                name: "NobilityMemberships");

            migrationBuilder.DropTable(
                name: "SignetUnits");

            migrationBuilder.DropTable(
                name: "SignetIssuances");

            migrationBuilder.DropColumn(
                name: "PresetSlot",
                table: "EssenceLoadouts");

            migrationBuilder.DropColumn(
                name: "PresetSlot",
                table: "EquipmentLoadouts");

            migrationBuilder.DropColumn(
                name: "FreeRerollsUsed",
                table: "DailyProphecyRerollStates");

            migrationBuilder.DropColumn(
                name: "PaidRerollsUsed",
                table: "DailyProphecyRerollStates");
        }
    }
}
