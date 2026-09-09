using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Colosseum;
using Application.UseCases.Colosseum.Tournaments;
using AutoMapper;
using Domain.Models.Combat;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class ColosseumPlaybackMappingTests
{
    [Theory]
    [InlineData(BattleOutcome.Victory)]
    [InlineData(BattleOutcome.Defeat)]
    [InlineData(BattleOutcome.Draw)]
    public void Maps_recorded_frames_to_compact_playback_without_revealing_the_outcome_early(BattleOutcome outcome)
    {
        var initial = Entity(100);
        var damaged = Entity(60);
        var final = Entity(0);
        var execution = new CombatExecutionWithCheckpoints(
            new CombatResult { Duration = 30, Outcome = outcome,
                CombatStyles = [new() { EntityId = "hero", CombatStyleId = "bastion", HealingConverted = 75 }] },
            [
                new CombatCheckpoint(0, 0, [initial], [], [], [], false),
                new CombatCheckpoint(1, 10, [damaged], [], [Stats(40)], [], false),
                new CombatCheckpoint(2, 20, [damaged], [], [Stats(40)], [], false),
                new CombatCheckpoint(3, 30, [final], [], [Stats(100)], [], true)
            ]);
        var mapper = new MapperConfiguration(
            config => config.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

        var bundle = mapper.Map<TournamentPlaybackBundleDto>(new ColosseumPlaybackResult(execution, 10, 10));

        Assert.Equal(3, bundle.SchemaVersion);
        Assert.Equal(10, bundle.TicksPerSecond);
        Assert.Equal(10, bundle.TicksPerFrame);
        Assert.Equal(30, bundle.TotalTicks);
        Assert.True(Assert.Single(bundle.Entities).IsFriendly);
        Assert.True(bundle.Frames[0].IsKeyframe);
        Assert.Equal(100, Assert.Single(bundle.Frames[0].EntityStates).Health);
        Assert.False(bundle.Frames[1].IsKeyframe);
        Assert.Equal(60, Assert.Single(bundle.Frames[1].EntityStates).Health);
        Assert.Equal(40, Assert.Single(bundle.Frames[1].EntityTotals).DamageTaken);
        Assert.Empty(bundle.Frames[2].EntityStates);
        Assert.Empty(bundle.Frames[2].EntityTotals);
        Assert.All(bundle.Frames.Take(3), frame => Assert.Null(frame.Outcome));
        Assert.True(bundle.Frames[^1].IsKeyframe);
        Assert.True(bundle.Frames[^1].IsFinal);
        Assert.Equal(outcome, bundle.Frames[^1].Outcome);
        Assert.Equal(0, Assert.Single(bundle.Frames[^1].EntityStates).Health);
        Assert.Equal(100, Assert.Single(bundle.Frames[^1].EntityTotals).DamageTaken);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(bundle, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.All(json.RootElement.GetProperty("frames").EnumerateArray(),
            frame => Assert.False(frame.TryGetProperty("combatStyles", out _)));
    }

    private static SimpleCombatEntity Entity(int health) => new()
    {
        Id = "hero", Name = "Hero", MaxHealth = 100, Health = health
    };

    private static EntityStats Stats(int damageTaken) => new("hero", "Hero", [], DamageTaken: damageTaken);
}
