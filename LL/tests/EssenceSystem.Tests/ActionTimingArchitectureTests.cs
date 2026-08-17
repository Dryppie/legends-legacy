using Application.MediatR.Synchronization;
using Common.Randomness;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Options;
using Services.LL.CharacterActions;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Spawnings;

namespace EssenceSystem.Tests;

public sealed class ActionTimingArchitectureTests
{
    private static readonly DateTimeOffset Boundary = DateTimeOffset.Parse("2026-08-17T12:00:10Z");

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(9_999, 1)]
    [InlineData(10_000, 2)]
    [InlineData(20_001, 3)]
    public void Due_calculation_has_inclusive_boundaries_and_no_drift(int offsetMilliseconds, int expectedDue)
    {
        var plan = ActionScheduleCalculator.Calculate(
            Boundary,
            Boundary.AddMilliseconds(offsetMilliseconds),
            TimeSpan.FromSeconds(10),
            100);

        Assert.Equal(expectedDue, plan.DueCount);
        Assert.Equal(expectedDue, plan.ProcessCount);
        Assert.False(plan.HasMoreDueWork);
    }

    [Fact]
    public void Due_calculation_caps_work_and_preserves_continuation()
    {
        var plan = ActionScheduleCalculator.Calculate(
            Boundary,
            Boundary.AddSeconds(1_000),
            TimeSpan.FromSeconds(10),
            25);

        Assert.Equal(101, plan.DueCount);
        Assert.Equal(25, plan.ProcessCount);
        Assert.True(plan.HasMoreDueWork);
        Assert.Equal(Boundary.AddSeconds(250), Boundary.AddSeconds(plan.ProcessCount * 10));
    }

    [Fact]
    public void Stable_resolution_identity_replays_and_generation_changes_the_sequence()
    {
        var first = StableRandom.Seed("tempering-attempt-v1", "character", "7", Boundary.UtcTicks.ToString(), "queue");
        var replay = StableRandom.Seed("tempering-attempt-v1", "character", "7", Boundary.UtcTicks.ToString(), "queue");
        var replacement = StableRandom.Seed("tempering-attempt-v1", "character", "8", Boundary.UtcTicks.ToString(), "queue");

        Assert.Equal(first, replay);
        Assert.Equal(new Random(first).NextDouble(), new Random(replay).NextDouble());
        Assert.NotEqual(first, replacement);
    }

    [Fact]
    public void Idle_encounter_identity_and_order_replay_for_the_same_boundary()
    {
        var characterId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var action = new CharacterAction(characterId, new CombatActionDetails(
            [Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")],
            new Area
            {
                Id = "area",
                SpawnProbabilities = [0, 1],
                Creatures =
                [
                    new AreaCreature { CreatureId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), WeightedSpawnRate = 1 },
                    new AreaCreature { CreatureId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), WeightedSpawnRate = 1 }
                ]
            }), Boundary)
        {
            ScheduleGeneration = 42
        };
        var planner = new IdleCombatPlanner(
            new SpawningService(),
            Options.Create(new IdleCombatProgressionOptions()));
        var plan = planner.CreatePlan(new IdleCombatOrchestrationRequest(action, Boundary));

        var first = planner.CreateEncounterPlan(plan, 1, Boundary);
        var replay = planner.CreateEncounterPlan(plan, 1, Boundary);

        Assert.Equal(first.EncounterId, replay.EncounterId);
        Assert.Equal(first.RandomSeed, replay.RandomSeed);
        Assert.Equal(first.Participants, replay.Participants);
        Assert.Equal(
            first.FriendlyParticipants.OrderBy(x => x.SourceEntityId),
            first.FriendlyParticipants);
    }

    [Fact]
    public async Task Keyed_lock_serializes_one_key_and_releases_all_entries()
    {
        var keyedLock = new ReferenceCountedKeyedLock<Guid>();
        var key = Guid.NewGuid();
        var inside = 0;
        var maximumInside = 0;

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            using (await keyedLock.AcquireAsync(key, CancellationToken.None))
            {
                var current = Interlocked.Increment(ref inside);
                maximumInside = Math.Max(maximumInside, current);
                await Task.Yield();
                Interlocked.Decrement(ref inside);
            }
        });

        await Task.WhenAll(tasks);

        Assert.Equal(1, maximumInside);
        Assert.Equal(0, keyedLock.EntryCount);
    }

    [Fact]
    public async Task Keyed_lock_does_not_retain_unique_character_ids()
    {
        var keyedLock = new ReferenceCountedKeyedLock<Guid>();
        for (var index = 0; index < 1_000; index++)
        {
            using (await keyedLock.AcquireAsync(Guid.NewGuid(), CancellationToken.None))
            {
            }
        }

        Assert.Equal(0, keyedLock.EntryCount);
    }
}
