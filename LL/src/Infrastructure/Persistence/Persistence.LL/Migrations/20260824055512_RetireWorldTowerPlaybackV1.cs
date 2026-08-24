using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RetireWorldTowerPlaybackV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "TowerCombatPlaybacks"
                        WHERE "SchemaVersion" < 2
                           OR "BundleHash" IS NULL
                           OR "BundleLength" IS NULL
                           OR "BundleContentType" IS NULL
                           OR "BundleContentEncoding" IS NULL
                           OR NOT EXISTS (
                               SELECT 1
                               FROM "TowerCombatPlaybackArtifacts" a
                               WHERE a."TowerAttemptId" = "TowerCombatPlaybacks"."TowerAttemptId"
                           )
                    ) THEN
                        RAISE EXCEPTION 'Cannot retire World Tower playback v1 while non-bundle playbacks remain.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_TowerCombatPlaybacks_DispatchLeaseUntil",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropIndex(
                name: "IX_TowerCombatPlaybacks_NextFrameDueAt_LastPublishedSequence",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt",
                table: "TowerCombatPlaybacks");

            migrationBuilder.RenameColumn(
                name: "DispatchLeaseUntil",
                table: "TowerCombatPlaybacks",
                newName: "FinalizationLeaseUntil");

            migrationBuilder.RenameColumn(
                name: "DispatchLeaseOwner",
                table: "TowerCombatPlaybacks",
                newName: "FinalizationLeaseOwner");

            migrationBuilder.DropColumn(
                name: "LastPublishedSequence",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "NextFrameDueAt",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "TimelineJson",
                table: "TowerCombatPlaybacks");

            migrationBuilder.AlterColumn<string>(
                name: "BundleHash",
                table: "TowerCombatPlaybacks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BundleLength",
                table: "TowerCombatPlaybacks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BundleContentType",
                table: "TowerCombatPlaybacks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BundleContentEncoding",
                table: "TowerCombatPlaybacks",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt_FinalizationLeaseUntil",
                table: "TowerCombatPlaybacks",
                columns: new[] { "PlaybackEndsAt", "FinalizationLeaseUntil" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt_FinalizationLeaseUntil",
                table: "TowerCombatPlaybacks");

            migrationBuilder.RenameColumn(
                name: "FinalizationLeaseUntil",
                table: "TowerCombatPlaybacks",
                newName: "DispatchLeaseUntil");

            migrationBuilder.RenameColumn(
                name: "FinalizationLeaseOwner",
                table: "TowerCombatPlaybacks",
                newName: "DispatchLeaseOwner");

            migrationBuilder.AlterColumn<string>(
                name: "BundleHash",
                table: "TowerCombatPlaybacks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<int>(
                name: "BundleLength",
                table: "TowerCombatPlaybacks",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "BundleContentType",
                table: "TowerCombatPlaybacks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "BundleContentEncoding",
                table: "TowerCombatPlaybacks",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<int>(
                name: "LastPublishedSequence",
                table: "TowerCombatPlaybacks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextFrameDueAt",
                table: "TowerCombatPlaybacks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero));

            migrationBuilder.AddColumn<string>(
                name: "TimelineJson",
                table: "TowerCombatPlaybacks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_DispatchLeaseUntil",
                table: "TowerCombatPlaybacks",
                column: "DispatchLeaseUntil");

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_NextFrameDueAt_LastPublishedSequence",
                table: "TowerCombatPlaybacks",
                columns: new[] { "NextFrameDueAt", "LastPublishedSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt",
                table: "TowerCombatPlaybacks",
                column: "PlaybackEndsAt");

        }
    }
}
