using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Idle;

namespace EssenceSystem.Tests;

public sealed class IdleCombatRewardFactBuilderTests
{
    [Fact]
    public async Task BuildAsync_allows_no_encounters_without_a_source_entity_catalog()
    {
        var now = DateTimeOffset.Parse("2026-07-16T12:00:00Z");
        var characterId = Guid.NewGuid();
        var action = new CharacterAction
        {
            CharacterId = characterId,
            ScheduleGeneration = 7,
            UpdatedAt = now.AddSeconds(10),
            ActionDetails = new CombatActionDetails(
                [characterId],
                new Area { Id = "test-area" })
        };
        var request = new IdleCombatOrchestrationRequest(action, now);
        var details = new IdleCombatOrchestrationDetails(
            From: action.UpdatedAt,
            RequestedTo: now,
            ProcessedUntil: action.UpdatedAt,
            PlannedEncounterCount: 0,
            EncounterCadence: TimeSpan.FromSeconds(10));
        var result = CombatOrchestrationResults.None(CombatMode.Idle, details);
        var context = new IdleCombatOutcomeContext(request, result, details);

        var facts = await new IdleCombatRewardFactBuilder().BuildAsync(
            context,
            CancellationToken.None);

        Assert.Empty(facts.Encounters);
        Assert.Equal(characterId, facts.CharacterId);
        Assert.Equal(7, facts.ScheduleGeneration);
    }
}
