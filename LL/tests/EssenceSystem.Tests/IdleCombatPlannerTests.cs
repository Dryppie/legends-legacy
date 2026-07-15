using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Options;
using Services.LL.CharacterActions;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace EssenceSystem.Tests;

public sealed class IdleCombatPlannerTests
{
    [Fact]
    public void CreatePlan_plans_first_encounter_immediately_when_action_is_due_now()
    {
        var now = DateTimeOffset.Parse("2026-06-23T12:00:00Z");
        var action = CreateCombatAction(now);
        var planner = CreatePlanner();

        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, now));

        Assert.Equal(1, plan.PlannedEncounterCount);
        Assert.Equal(now, plan.From);
        Assert.Equal(now.AddSeconds(10), plan.ExecutableUntil);
    }

    [Fact]
    public void CreatePlan_does_not_plan_next_encounter_before_due_time()
    {
        var nextEncounterAt = DateTimeOffset.Parse("2026-06-23T12:00:10Z");
        var now = nextEncounterAt.AddMilliseconds(-1);
        var action = CreateCombatAction(nextEncounterAt);
        var planner = CreatePlanner();

        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, now));

        Assert.Equal(0, plan.PlannedEncounterCount);
        Assert.Equal(nextEncounterAt, plan.ExecutableUntil);
    }

    [Fact]
    public void CreatePlan_catches_up_due_encounters_on_cadence_boundaries()
    {
        var firstEncounterAt = DateTimeOffset.Parse("2026-06-23T12:00:00Z");
        var now = firstEncounterAt.AddSeconds(25);
        var action = CreateCombatAction(firstEncounterAt);
        var planner = CreatePlanner();

        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, now));

        Assert.Equal(3, plan.PlannedEncounterCount);
        Assert.Equal(firstEncounterAt.AddSeconds(30), plan.ExecutableUntil);
    }

    [Fact]
    public void CreatePlan_discards_progress_older_than_the_offline_limit()
    {
        var now = DateTimeOffset.Parse("2026-06-23T12:00:00Z");
        var action = CreateCombatAction(now.AddHours(-48));
        var planner = CreatePlanner(maximumBatchSize: 10_000);

        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, now));

        Assert.Equal(now.AddHours(-24), plan.From);
        Assert.Equal(8_641, plan.PlannedEncounterCount);
    }

    [Fact]
    public void CreatePlan_caps_each_processing_batch_and_preserves_remaining_catch_up_time()
    {
        var firstEncounterAt = DateTimeOffset.Parse("2026-06-23T10:00:00Z");
        var now = firstEncounterAt.AddHours(2);
        var action = CreateCombatAction(firstEncounterAt);
        var planner = CreatePlanner(maximumBatchSize: 500);

        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, now));

        Assert.Equal(500, plan.PlannedEncounterCount);
        Assert.Equal(firstEncounterAt.AddSeconds(5_000), plan.ExecutableUntil);
        Assert.True(plan.ExecutableUntil < now);
    }

    private static IdleCombatPlanner CreatePlanner(int maximumBatchSize = 500) =>
        new(
            new FakeSpawningService(),
            Options.Create(new IdleCombatProgressionOptions
            {
                EncounterCadenceSeconds = 10,
                MaximumOfflineHours = 24,
                MaximumEncountersPerProcessingBatch = maximumBatchSize,
                ReferenceWinRateBasisPoints = 8_500
            }));

    private static CharacterAction CreateCombatAction(DateTimeOffset nextEncounterAt)
    {
        var characterId = Guid.NewGuid();
        return new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = nextEncounterAt,
            ActionDetails = new CombatActionDetails(
                [characterId],
                new Area
                {
                    Id = "test-area",
                    Creatures =
                    [
                        new AreaCreature
                        {
                            CreatureId = Guid.NewGuid(),
                            WeightedSpawnRate = 1
                        }
                    ],
                    SpawnProbabilities = [1]
                })
        };
    }

    private sealed class FakeSpawningService : ISpawningService
    {
        public int HowManyMonstersToSpawn(List<float> counterProbabilities) => 1;

        public List<AreaCreature> WhatAreaCreaturesToSpawn(List<AreaCreature> creatures, int count) =>
            creatures.Take(count).ToList();
    }
}
