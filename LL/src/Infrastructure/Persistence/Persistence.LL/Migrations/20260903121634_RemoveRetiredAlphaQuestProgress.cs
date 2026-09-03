using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRetiredAlphaQuestProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Frozen post-Alpha catalog: remove saved progress for deleted quests and versions.
            // Objective progress is removed by the existing cascading foreign key.
            migrationBuilder.Sql(
                """
                WITH retained_quests (quest_id, definition_version) AS (
                    VALUES
                        ('quest.character.a_name_in_shenic', 1),
                        ('quest.character.tested_wanderer', 1),
                        ('quest.character.warden_of_shenic', 1),
                        ('quest.colosseum.the_arena_calls', 1),
                        ('quest.colosseum.tournament_tested', 1),
                        ('quest.combat.blood_grove_veteran', 2),
                        ('quest.dungeons.into_the_depths', 1),
                        ('quest.dungeons.sigils_in_the_dust', 1),
                        ('quest.essences.a_second_soul', 1),
                        ('quest.essences.an_adaptable_archive', 1),
                        ('quest.essences.focused_pursuit', 1),
                        ('quest.essences.resonant_pair', 1),
                        ('quest.essences.the_archive_deepens', 1),
                        ('quest.onboarding.first_weapon', 2),
                        ('quest.onboarding.soul_archive', 3),
                        ('quest.onboarding.tools_of_trade', 2),
                        ('quest.onboarding.training_day', 4),
                        ('quest.prophecies.an_omen_fulfilled', 1),
                        ('quest.region01.into_lumo_ruins', 2),
                        ('quest.shenic.ash_beneath_the_earth', 4),
                        ('quest.shenic.between_day_and_night', 4),
                        ('quest.shenic.blood_in_the_grove', 4),
                        ('quest.shenic.crystal_currents', 4),
                        ('quest.shenic.heart_of_the_hollow', 4),
                        ('quest.shenic.last_light_in_duskmire', 4),
                        ('quest.shenic.restless_dead', 4),
                        ('quest.shenic.roots_remember', 4),
                        ('quest.shenic.trial_of_lumo', 4),
                        ('quest.shenic.veil_over_the_marsh', 4)
                )
                DELETE FROM "CharacterQuestProgresses"
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM retained_quests
                    WHERE retained_quests.quest_id = lower("CharacterQuestProgresses"."QuestId")
                        AND retained_quests.definition_version = "CharacterQuestProgresses"."DefinitionVersion"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted Alpha progress is intentionally not recoverable.
        }
    }
}
