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
    public void CreatePlan_repairs_a_deadline_farther_than_one_cadence_in_the_future()
    {
        var now = DateTimeOffset.Parse("2026-06-23T12:00:00Z");
        var action = CreateCombatAction(now.AddHours(19));
        var planner = CreatePlanner();

        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, now));

        Assert.Equal(1, plan.PlannedEncounterCount);
        Assert.Equal(now, plan.From);
        Assert.Equal(now.AddSeconds(10), plan.ExecutableUntil);
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
    public void CreatePlan_discards_progress_older_than_the_offline_limit_and_caps_the_batch()
    {
        var now = DateTimeOffset.Parse("2026-06-23T12:00:00Z");
        var action = CreateCombatAction(now.AddHours(-48));
        var planner = CreatePlanner();

        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, now));

        Assert.Equal(now.AddHours(-24), plan.From);
        Assert.Equal(100, plan.PlannedEncounterCount);
        Assert.Equal(now.AddHours(-24).AddSeconds(1_000), plan.ExecutableUntil);
    }

    [Fact]
    public void CreatePlan_limits_due_encounters_to_a_resumable_batch()
    {
        var firstEncounterAt = DateTimeOffset.Parse("2026-06-23T10:00:00Z");
        var now = firstEncounterAt.AddHours(2);
        var action = CreateCombatAction(firstEncounterAt);
        var planner = CreatePlanner();

        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, now));

        Assert.Equal(100, plan.PlannedEncounterCount);
        Assert.Equal(firstEncounterAt.AddSeconds(1_000), plan.ExecutableUntil);
    }

    [Fact]
    public void CreateEncounterPlan_captures_playback_only_for_the_requested_final_encounter()
    {
        var firstEncounterAt = DateTimeOffset.Parse("2026-06-23T12:00:00Z");
        var now = firstEncounterAt.AddSeconds(25);
        var planner = CreatePlanner();
        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(
            CreateCombatAction(firstEncounterAt),
            now,
            CaptureFinalEncounterLog: true));

        var first = planner.CreateEncounterPlan(plan, 1, firstEncounterAt);
        var second = planner.CreateEncounterPlan(plan, 2, firstEncounterAt.AddSeconds(10));
        var final = planner.CreateEncounterPlan(plan, 3, firstEncounterAt.AddSeconds(20));

        Assert.False(first.CaptureEventLog);
        Assert.False(second.CaptureEventLog);
        Assert.True(final.CaptureEventLog);
    }

    [Fact]
    public void CreateEncounterPlan_skips_playback_for_an_internal_nonfinal_batch()
    {
        var firstEncounterAt = DateTimeOffset.Parse("2026-06-23T12:00:00Z");
        var now = firstEncounterAt.AddSeconds(25);
        var planner = CreatePlanner();
        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(
            CreateCombatAction(firstEncounterAt),
            now,
            CaptureFinalEncounterLog: false));

        var final = planner.CreateEncounterPlan(plan, 3, firstEncounterAt.AddSeconds(20));

        Assert.False(final.CaptureEventLog);
    }

    [Fact]
    public void CreateEncounterPlan_skips_playback_when_the_safety_limited_batch_does_not_finish_catch_up()
    {
        var firstEncounterAt = DateTimeOffset.Parse("2026-06-23T12:00:00Z");
        var now = firstEncounterAt.AddHours(24);
        var planner = CreatePlanner();
        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(
            CreateCombatAction(firstEncounterAt),
            now,
            CaptureFinalEncounterLog: true));

        var lastEncounterInPartialBatch = planner.CreateEncounterPlan(
            plan,
            plan.PlannedEncounterCount,
            plan.ExecutableUntil - plan.EncounterCadence);

        Assert.Equal(100, plan.PlannedEncounterCount);
        Assert.True(plan.ExecutableUntil <= plan.RequestedTo);
        Assert.False(lastEncounterInPartialBatch.CaptureEventLog);
    }

    private static IdleCombatPlanner CreatePlanner() =>
        new(
            new FakeSpawningService(),
            Options.Create(new IdleCombatProgressionOptions
            {
                EncounterCadenceSeconds = 10,
                MaximumOfflineHours = 24,
                MaximumEncountersPerResolution = 100
            }));

    private static CharacterAction CreateCombatAction(DateTimeOffset nextEncounterAt)
    {
        var characterId = Guid.NewGuid();
        return new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = nextEncounterAt,
            NextResolutionAtUtc = nextEncounterAt,
            ScheduleGeneration = 7,
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
        public int HowManyMonstersToSpawn(List<float> counterProbabilities, Random? random = null) => 1;

        public List<AreaCreature> WhatAreaCreaturesToSpawn(List<AreaCreature> creatures, int count, Random? random = null) =>
            creatures.Take(count).ToList();
    }
}
