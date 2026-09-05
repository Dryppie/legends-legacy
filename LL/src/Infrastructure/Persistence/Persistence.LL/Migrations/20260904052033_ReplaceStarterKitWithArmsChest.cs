using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStarterKitWithArmsChest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Characters who have not entered Lumo must replay the short Soul Archive
            // handoff so its new idempotent reward can place an Arms Chest in Inventory.
            // Existing equipment instances remain owned. Retire every armor/accessory
            // entitlement, and discard incomplete players' old weapon entitlement so
            // the chest can freeze their new choice.
            migrationBuilder.Sql(
                """
                DELETE FROM "ModelEStarterGrants"
                WHERE "Kind" = 1;

                DELETE FROM "ModelEStarterGrants" AS grants
                WHERE grants."Kind" = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "CharacterQuestProgresses" AS lumo
                      WHERE lumo."CharacterId" = grants."CharacterId"
                        AND lower(lumo."QuestId") = 'quest.region01.into_lumo_ruins'
                        AND lumo."Status" = 3
                  );

                DELETE FROM "CharacterQuestProgresses" AS progress
                WHERE lower(progress."QuestId") = 'quest.onboarding.tools_of_trade'
                   OR (
                       lower(progress."QuestId") IN (
                           'quest.onboarding.soul_archive',
                           'quest.onboarding.first_weapon'
                       )
                       AND NOT EXISTS (
                           SELECT 1
                           FROM "CharacterQuestProgresses" AS lumo
                           WHERE lumo."CharacterId" = progress."CharacterId"
                             AND lower(lumo."QuestId") = 'quest.region01.into_lumo_ruins'
                             AND lumo."Status" = 3
                       )
                   );

                UPDATE "Areas"
                SET "RequiredCompletedQuestId" = 'quest.onboarding.first_weapon'
                WHERE lower("RequiredCompletedQuestId") = 'quest.onboarding.tools_of_trade';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Areas"
                SET "RequiredCompletedQuestId" = 'quest.onboarding.tools_of_trade'
                WHERE "Id" = 'region_01_area_01'
                  AND lower("RequiredCompletedQuestId") = 'quest.onboarding.first_weapon';
                """);

            // Removed quest progress and starter entitlements cannot be reconstructed.
        }
    }
}
